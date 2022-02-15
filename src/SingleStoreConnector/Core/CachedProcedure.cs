using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SingleStoreConnector.Logging;
using SingleStoreConnector.Protocol.Serialization;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector.Core;

internal sealed class CachedProcedure
{
	public static async Task<CachedProcedure?> FillAsync(IOBehavior ioBehavior, SingleStoreConnection connection, string schema, string component, CancellationToken cancellationToken)
	{
		if (connection.Session.MySqlCompatVersion.Version < ServerVersions.SupportsProcedureCache)
		{
			Log.Info("Session{0} ServerVersion={1} does not support cached procedures", connection.Session.Id, connection.Session.MySqlCompatVersion.OriginalString);
			return null;
		}

		var parameters = new List<CachedParameter>();
		int routineCount;
		using (var cmd = connection.CreateCommand())
		{
			cmd.Transaction = connection.CurrentTransaction;
			cmd.CommandText = @"SELECT COUNT(*)
				FROM information_schema.routines
				WHERE ROUTINE_SCHEMA = @schema AND ROUTINE_NAME = @component;
				SELECT ORDINAL_POSITION, PARAMETER_MODE, PARAMETER_NAME, DTD_IDENTIFIER
				FROM information_schema.parameters
				WHERE SPECIFIC_SCHEMA = @schema AND SPECIFIC_NAME = @component
				ORDER BY ORDINAL_POSITION";
			cmd.Parameters.AddWithValue("@schema", schema);
			cmd.Parameters.AddWithValue("@component", component);

			using var reader = await cmd.ExecuteReaderNoResetTimeoutAsync(CommandBehavior.Default, ioBehavior, cancellationToken).ConfigureAwait(false);
			await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			routineCount = reader.GetInt32(0);
			await reader.NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

			while (await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
			{
				var dataType = ParseDataType(reader.GetString(3), out var unsigned, out var length);
				parameters.Add(new(
					reader.GetInt32(0),
					!reader.IsDBNull(1) ? reader.GetString(1) : null,
					!reader.IsDBNull(2) ? reader.GetString(2) : "",
					dataType,
					unsigned,
					length
				));
			}
		}

		if (Log.IsTraceEnabled())
			Log.Trace("Procedure for Schema={0} Component={1} has RoutineCount={2}, ParameterCount={3}", schema, component, routineCount, parameters.Count);
		return routineCount == 0 ? null : new CachedProcedure(schema, component, parameters);
	}

	public IReadOnlyList<CachedParameter> Parameters { get; }

	private CachedProcedure(string schema, string component, IReadOnlyList<CachedParameter> parameters)
	{
		m_schema = schema;
		m_component = component;
		Parameters = parameters;
	}

	internal SingleStoreParameterCollection AlignParamsWithDb(SingleStoreParameterCollection? parameterCollection)
	{
		var alignedParams = new SingleStoreParameterCollection();
		var returnParam = parameterCollection?.FirstOrDefault(static x => x.Direction == ParameterDirection.ReturnValue);

		foreach (var cachedParam in Parameters)
		{
			SingleStoreParameter alignParam;
			if (cachedParam.Direction == ParameterDirection.ReturnValue)
			{
				alignParam = returnParam ?? throw new InvalidOperationException($"Attempt to call stored function {FullyQualified} without specifying a return parameter");
			}
			else
			{
				var index = parameterCollection?.NormalizedIndexOf(cachedParam.Name) ?? -1;
				alignParam = index >= 0 ? parameterCollection![index] : throw new ArgumentException($"Parameter '{cachedParam.Name}' not found in the collection.");
			}

			if (!alignParam.HasSetDirection)
				alignParam.Direction = cachedParam.Direction;
			if (!alignParam.HasSetDbType)
				alignParam.SingleStoreDbType = cachedParam.SingleStoreDbType;

			// cached parameters are oredered by ordinal position
			alignedParams.Add(alignParam);
		}

		return alignedParams;
	}

	internal static List<CachedParameter> ParseParameters(string parametersSql)
	{
		// strip comments
		parametersSql = s_cStyleComments.Replace(parametersSql, "");
		parametersSql = s_singleLineComments.Replace(parametersSql, "");

		// normalize spaces
		parametersSql = s_multipleSpaces.Replace(parametersSql, " ");

		if (string.IsNullOrWhiteSpace(parametersSql))
			return new List<CachedParameter>();

		// strip precision specifier containing comma
		parametersSql = s_numericTypes.Replace(parametersSql, @"$1");

		// strip enum values containing commas (these would have been stripped by ParseDataType anyway)
		parametersSql = s_enum.Replace(parametersSql, "ENUM");

		var parameters = parametersSql.Split(',');
		var cachedParameters = new List<CachedParameter>(parameters.Length);
		for (var i = 0; i < parameters.Length; i++)
		{
			var parameter = parameters[i].Trim();
			var originalString = parameter;
			string direction = "IN";
			if (parameter.StartsWith("INOUT ", StringComparison.OrdinalIgnoreCase))
			{
				direction = "INOUT";
				parameter = parameter.Substring(6);
			}
			else if (parameter.StartsWith("OUT ", StringComparison.OrdinalIgnoreCase))
			{
				direction = "OUT";
				parameter = parameter.Substring(4);
			}
			else if (parameter.StartsWith("IN ", StringComparison.OrdinalIgnoreCase))
			{
				direction = "IN";
				parameter = parameter.Substring(3);
			}

			var parts = s_parameterName.Match(parameter);
			var name = parts.Groups[1].Success ? parts.Groups[1].Value.Replace("``", "`") : parts.Groups[2].Value;

			var dataType = ParseDataType(parts.Groups[3].Value, out var unsigned, out var length);
			cachedParameters.Add(CreateCachedParameter(i + 1, direction, name, dataType, unsigned, length, originalString));
		}

		return cachedParameters;
	}

	internal static string ParseDataType(string sql, out bool unsigned, out int length)
	{
		sql = s_characterSet.Replace(sql, "");
		sql = s_collate.Replace(sql, "");
		sql = s_enum.Replace(sql, "ENUM");

		length = 0;
		var match = s_length.Match(sql);
		if (match.Success)
		{
			length = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
			sql = s_length.Replace(sql, "");
		}

		var list = sql.Trim().Split(new[] { ' ' });
		var type = string.Empty;

		if (list.Length < 2 || !s_typeMapping.TryGetValue(list[0] + ' ' + list[1], out type))
		{
			if (s_typeMapping.TryGetValue(list[0], out type))
			{
				if (list[0].StartsWith("BOOL", StringComparison.OrdinalIgnoreCase))
				{
					length = 1;
				}
			}
		}

		unsigned = list.Contains("UNSIGNED", StringComparer.OrdinalIgnoreCase);
		return type ?? list[0];
	}

	private static CachedParameter CreateCachedParameter(int ordinal, string? direction, string name, string dataType, bool unsigned, int length, string originalSql)
	{
		try
		{
			return new CachedParameter(ordinal, direction, name, dataType, unsigned, length);
		}
		catch (NullReferenceException ex)
		{
			throw new SingleStoreException("Failed to parse stored procedure parameter '{0}'; extracted data type was {1}".FormatInvariant(originalSql, dataType), ex);
		}
	}

	string FullyQualified => $"`{m_schema}`.`{m_component}`";

	static readonly ISingleStoreConnectorLogger Log = SingleStoreConnectorLogManager.CreateLogger(nameof(CachedProcedure));
	static readonly IReadOnlyDictionary<string, string> s_typeMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{ "BOOL", "TINYINT" },
		{ "BOOLEAN", "TINYINT" },
		{ "INTEGER", "INT" },
		{ "NUMERIC", "DECIMAL" },
		{ "FIXED", "DECIMAL" },
		{ "REAL", "DOUBLE" },
		{ "DOUBLE PRECISION", "DOUBLE" },
		{ "NVARCHAR", "VARCHAR" },
		{ "CHARACTER VARYING", "VARCHAR" },
		{ "NATIONAL VARCHAR", "VARCHAR" },
		{ "NCHAR", "CHAR" },
		{ "CHARACTER", "CHAR" },
		{ "NATIONAL CHAR", "CHAR" },
		{ "CHAR BYTE", "BINARY" },
	};

	static readonly Regex s_cStyleComments = new(@"/\*.*?\*/", RegexOptions.Singleline);
	static readonly Regex s_singleLineComments = new(@"(^|\s)--.*?$", RegexOptions.Multiline);
	static readonly Regex s_multipleSpaces = new(@"\s+");
	static readonly Regex s_numericTypes = new(@"(DECIMAL|DEC|FIXED|NUMERIC|FLOAT|DOUBLE PRECISION|DOUBLE|REAL)\s*\([0-9]+(,\s*[0-9]+)\)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
	static readonly Regex s_enum = new(@"ENUM\s*\([^)]+\)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
	static readonly Regex s_parameterName = new(@"^(?:`((?:[\u0001-\u005F\u0061-\uFFFF]+|``)+)`|([A-Za-z0-9$_\u0080-\uFFFF]+)) (.*)$");
	static readonly Regex s_characterSet = new(" (CHARSET|CHARACTER SET) [A-Za-z0-9_]+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
	static readonly Regex s_collate = new(" (COLLATE) [A-Za-z0-9_]+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
	static readonly Regex s_length = new(@"\s*\(\s*([0-9]+)\s*(?:,\s*[0-9]+\s*)?\)");

	readonly string m_schema;
	readonly string m_component;
}