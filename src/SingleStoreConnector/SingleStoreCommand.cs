using System.Diagnostics.CodeAnalysis;
using SingleStoreConnector.Core;
using SingleStoreConnector.Logging;
using SingleStoreConnector.Protocol.Serialization;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector;

/// <summary>
/// <see cref="SingleStoreCommand"/> represents a SQL statement or stored procedure name
/// to execute against a MySQL database.
/// </summary>
public sealed class SingleStoreCommand : DbCommand, IMySqlCommand, ICancellableCommand, ICloneable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SingleStoreCommand"/> class.
	/// </summary>
	public SingleStoreCommand()
		: this(null, null, null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SingleStoreCommand"/> class, setting <see cref="CommandText"/> to <paramref name="commandText"/>.
	/// </summary>
	/// <param name="commandText">The text to assign to <see cref="CommandText"/>.</param>
	public SingleStoreCommand(string? commandText)
		: this(commandText, null, null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SingleStoreCommand"/> class with the specified <see cref="SingleStoreConnection"/> and <see cref="SingleStoreTransaction"/>.
	/// </summary>
	/// <param name="connection">The <see cref="SingleStoreConnection"/> to use.</param>
	/// <param name="transaction">The active <see cref="SingleStoreTransaction"/>, if any.</param>
	public SingleStoreCommand(SingleStoreConnection? connection, SingleStoreTransaction? transaction)
		: this(null, connection, transaction)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SingleStoreCommand"/> class with the specified command text and <see cref="SingleStoreConnection"/>.
	/// </summary>
	/// <param name="commandText">The text to assign to <see cref="CommandText"/>.</param>
	/// <param name="connection">The <see cref="SingleStoreConnection"/> to use.</param>
	public SingleStoreCommand(string? commandText, SingleStoreConnection? connection)
		: this(commandText, connection, null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SingleStoreCommand"/> class with the specified command text,<see cref="SingleStoreConnection"/>, and <see cref="SingleStoreTransaction"/>.
	/// </summary>
	/// <param name="commandText">The text to assign to <see cref="CommandText"/>.</param>
	/// <param name="connection">The <see cref="SingleStoreConnection"/> to use.</param>
	/// <param name="transaction">The active <see cref="SingleStoreTransaction"/>, if any.</param>
	public SingleStoreCommand(string? commandText, SingleStoreConnection? connection, SingleStoreTransaction? transaction)
	{
		GC.SuppressFinalize(this);
		m_commandId = ICancellableCommandExtensions.GetNextId();
		m_commandText = commandText ?? "";
		Connection = connection;
		Transaction = transaction;
		CommandType = CommandType.Text;
	}

	private SingleStoreCommand(SingleStoreCommand other)
		: this(other.CommandText, other.Connection, other.Transaction)
	{
		GC.SuppressFinalize(this);
		m_commandTimeout = other.m_commandTimeout;
		m_commandType = other.m_commandType;
		DesignTimeVisible = other.DesignTimeVisible;
		UpdatedRowSource = other.UpdatedRowSource;
		m_parameterCollection = other.CloneRawParameters();
		m_attributeCollection = other.CloneRawAttributes();
	}

	/// <summary>
	/// The collection of <see cref="SingleStoreParameter"/> objects for this command.
	/// </summary>
	public new SingleStoreParameterCollection Parameters => m_parameterCollection ??= new();

	SingleStoreParameterCollection? IMySqlCommand.RawParameters => m_parameterCollection;

	/// <summary>
	/// The collection of <see cref="SingleStoreAttribute"/> objects for this command.
	/// </summary>
	public SingleStoreAttributeCollection Attributes => m_attributeCollection ??= new();

	SingleStoreAttributeCollection? IMySqlCommand.RawAttributes => m_attributeCollection;

	public new SingleStoreParameter CreateParameter() => (SingleStoreParameter) base.CreateParameter();

	/// <inheritdoc/>
	public override void Cancel() => Connection?.Cancel(this, m_commandId, true);

	/// <inheritdoc/>
	public override int ExecuteNonQuery() => ExecuteNonQueryAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

	/// <inheritdoc/>
	public override object? ExecuteScalar() => ExecuteScalarAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

	public new SingleStoreDataReader ExecuteReader() => ExecuteReaderAsync(default, IOBehavior.Synchronous, default).GetAwaiter().GetResult();

	public new SingleStoreDataReader ExecuteReader(CommandBehavior commandBehavior) => ExecuteReaderAsync(commandBehavior, IOBehavior.Synchronous, default).GetAwaiter().GetResult();

	/// <inheritdoc/>
	public override void Prepare()
	{
		if (!NeedsPrepare(out var exception))
		{
			if (exception is not null)
				throw exception;
			return;
		}

		Connection!.Session.PrepareAsync(this, IOBehavior.Synchronous, default).GetAwaiter().GetResult();
	}

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
	public override Task PrepareAsync(CancellationToken cancellationToken = default) => PrepareAsync(AsyncIOBehavior, cancellationToken);
#else
	public Task PrepareAsync(CancellationToken cancellationToken = default) => PrepareAsync(AsyncIOBehavior, cancellationToken);
#endif

	internal SingleStoreParameterCollection? CloneRawParameters()
	{
		if (m_parameterCollection is null)
			return null;
		var parameters = new SingleStoreParameterCollection();
		foreach (var parameter in (IEnumerable<SingleStoreParameter>) m_parameterCollection)
			parameters.Add(parameter.Clone());
		return parameters;
	}

	private SingleStoreAttributeCollection? CloneRawAttributes()
	{
		if (m_attributeCollection is null)
			return null;
		var attributes = new SingleStoreAttributeCollection();
		foreach (var attribute in m_attributeCollection)
			attributes.Add(new SingleStoreAttribute(attribute.AttributeName, attribute.Value));
		return attributes;
	}

	bool IMySqlCommand.AllowUserVariables => AllowUserVariables;

	internal bool AllowUserVariables { get; set; }
	internal bool NoActivity { get; set; }

	private Task PrepareAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		if (!NeedsPrepare(out var exception))
			return exception is null ? Utility.CompletedTask : Utility.TaskFromException(exception);

		return Connection!.Session.PrepareAsync(this, ioBehavior, cancellationToken);
	}

	private bool NeedsPrepare(out Exception? exception)
	{
		exception = null;
		if (Connection is null)
			exception = new InvalidOperationException("Connection property must be non-null.");
		else if (Connection.State != ConnectionState.Open)
			exception = new InvalidOperationException("Connection must be Open; current state is {0}".FormatInvariant(Connection.State));
		else if (string.IsNullOrWhiteSpace(CommandText))
			exception = new InvalidOperationException("CommandText must be specified");
		else if (Connection?.HasActiveReader ?? false)
			exception = new InvalidOperationException("Cannot call Prepare when there is an open DataReader for this command's connection; it must be closed first.");

		if (exception is not null || Connection!.IgnorePrepare)
			return false;

		if (CommandType != CommandType.StoredProcedure && CommandType != CommandType.Text)
		{
			exception = new NotSupportedException("Only CommandType.Text and CommandType.StoredProcedure are currently supported by SingleStoreCommand.Prepare.");
			return false;
		}

		// don't prepare the same SQL twice
		return Connection.Session.TryGetPreparedStatement(CommandText!) is null;
	}

	/// <summary>
	/// Gets or sets the command text to execute.
	/// </summary>
	/// <remarks>If <see cref="CommandType"/> is <see cref="CommandType.Text"/>, this is one or more SQL statements to execute.
	/// If <see cref="CommandType"/> is <see cref="CommandType.StoredProcedure"/>, this is the name of the stored procedure; any
	/// special characters in the stored procedure name must be quoted or escaped.</remarks>
	[AllowNull]
	public override string CommandText
	{
		get => m_commandText;
		set
		{
			if (m_connection?.ActiveCommandId == m_commandId)
				throw new InvalidOperationException("Cannot set SingleStoreCommand.CommandText when there is an open DataReader for this command; it must be closed first.");
			m_commandText = value ?? "";
		}
	}

	public bool IsPrepared => ((IMySqlCommand) this).TryGetPreparedStatements() is not null;

	public new SingleStoreTransaction? Transaction { get; set; }

	public new SingleStoreConnection? Connection
	{
		get => m_connection;
		set
		{
			if (m_connection?.ActiveCommandId == m_commandId)
				throw new InvalidOperationException("Cannot set SingleStoreCommand.Connection when there is an open DataReader for this command; it must be closed first.");
			m_connection = value;
		}
	}

	/// <inheritdoc/>
	public override int CommandTimeout
	{
		get => Math.Min(m_commandTimeout ?? Connection?.DefaultCommandTimeout ?? 0, int.MaxValue / 1000);
		set => m_commandTimeout = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "CommandTimeout must be greater than or equal to zero.");
	}

	/// <inheritdoc/>
	public override CommandType CommandType
	{
		get => m_commandType;
		set
		{
			if (value != CommandType.Text && value != CommandType.StoredProcedure)
				throw new ArgumentException("CommandType must be Text or StoredProcedure.", nameof(value));
			m_commandType = value;
		}
	}

	/// <inheritdoc/>
	public override bool DesignTimeVisible { get; set; }

	/// <inheritdoc/>
	public override UpdateRowSource UpdatedRowSource { get; set; }

	/// <summary>
	/// Holds the first automatically-generated ID for a value inserted in an <c>AUTO_INCREMENT</c> column in the last statement.
	/// </summary>
	/// <remarks>
	/// See <a href="https://dev.mysql.com/doc/refman/8.0/en/information-functions.html#function_last-insert-id"><c>LAST_INSERT_ID()</c></a> for more information.
	/// </remarks>
	public long LastInsertedId { get; private set; }

	void IMySqlCommand.SetLastInsertedId(long lastInsertedId) => LastInsertedId = lastInsertedId;

	protected override DbConnection? DbConnection
	{
		get => Connection;
		set => Connection = (SingleStoreConnection?) value;
	}

	protected override DbParameterCollection DbParameterCollection => Parameters;

	protected override DbTransaction? DbTransaction
	{
		get => Transaction;
		set => Transaction = (SingleStoreTransaction?) value;
	}

	protected override DbParameter CreateDbParameter() => new SingleStoreParameter();

	protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
		ExecuteReaderAsync(behavior, IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

	public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) =>
		ExecuteNonQueryAsync(AsyncIOBehavior, cancellationToken);

	internal async Task<int> ExecuteNonQueryAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		Volatile.Write(ref m_commandTimedOut, false);
		this.ResetCommandTimeout();
		using var registration = ((ICancellableCommand) this).RegisterCancel(cancellationToken);
		using var reader = await ExecuteReaderNoResetTimeoutAsync(CommandBehavior.Default, ioBehavior, cancellationToken).ConfigureAwait(false);
		do
		{
			while (await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
			{
			}
		} while (await reader.NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false));
		return reader.RecordsAffected;
	}

	public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) =>
		ExecuteScalarAsync(AsyncIOBehavior, cancellationToken);

	internal async Task<object?> ExecuteScalarAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		Volatile.Write(ref m_commandTimedOut, false);
		this.ResetCommandTimeout();
		using var registration = ((ICancellableCommand) this).RegisterCancel(cancellationToken);
		var hasSetResult = false;
		object? result = null;
		using var reader = await ExecuteReaderNoResetTimeoutAsync(CommandBehavior.Default, ioBehavior, cancellationToken).ConfigureAwait(false);
		do
		{
			var hasResult = await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			if (!hasSetResult)
			{
				if (hasResult)
					result = reader.GetValue(0);
				hasSetResult = true;
			}
		} while (await reader.NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false));
		return result;
	}

	public new Task<SingleStoreDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default) =>
		ExecuteReaderAsync(default, AsyncIOBehavior, cancellationToken);

	public new Task<SingleStoreDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken = default) =>
		ExecuteReaderAsync(behavior, AsyncIOBehavior, cancellationToken);

	protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) =>
		await ExecuteReaderAsync(behavior, AsyncIOBehavior, cancellationToken).ConfigureAwait(false);

	internal async Task<SingleStoreDataReader> ExecuteReaderAsync(CommandBehavior behavior, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		Volatile.Write(ref m_commandTimedOut, false);
		this.ResetCommandTimeout();
		using var registration = ((ICancellableCommand) this).RegisterCancel(cancellationToken);
		return await ExecuteReaderNoResetTimeoutAsync(behavior, ioBehavior, cancellationToken).ConfigureAwait(false);
	}

	internal Task<SingleStoreDataReader> ExecuteReaderNoResetTimeoutAsync(CommandBehavior behavior, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		if (!IsValid(out var exception))
			return Utility.TaskFromException<SingleStoreDataReader>(exception);

		var activity = NoActivity ? null : Connection!.Session.StartActivity(ActivitySourceHelper.ExecuteActivityName,
			ActivitySourceHelper.DatabaseStatementTagName, CommandText);
		m_commandBehavior = behavior;
		return CommandExecutor.ExecuteReaderAsync(new IMySqlCommand[] { this }, SingleCommandPayloadCreator.Instance, behavior, activity, ioBehavior, cancellationToken);
	}

	public SingleStoreCommand Clone() => new(this);

	object ICloneable.Clone() => Clone();

	protected override void Dispose(bool disposing)
	{
		m_isDisposed = true;
		base.Dispose(disposing);
	}

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
	public override ValueTask DisposeAsync()
#else
	public Task DisposeAsync()
#endif
	{
		Dispose();
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
		return default;
#else
		return Utility.CompletedTask;
#endif
	}

	/// <summary>
	/// Registers <see cref="Cancel"/> as a callback with <paramref name="cancellationToken"/> if cancellation is supported.
	/// </summary>
	/// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
	/// <returns>An object that must be disposed to revoke the cancellation registration.</returns>
	/// <remarks>This method is more efficient than calling <code>token.Register(Command.Cancel)</code> because it avoids
	/// unnecessary allocations.</remarks>
	IDisposable? ICancellableCommand.RegisterCancel(CancellationToken cancellationToken)
	{
		if (!cancellationToken.CanBeCanceled)
			return null;

		m_cancelAction ??= Cancel;
		return cancellationToken.Register(m_cancelAction);
	}

	void ICancellableCommand.SetTimeout(int milliseconds)
	{
		if (m_cancelTimerId != 0)
			TimerQueue.Instance.Remove(m_cancelTimerId);

		if (milliseconds != Constants.InfiniteTimeout)
		{
			m_cancelForCommandTimeoutAction ??= CancelCommandForTimeout;
			m_cancelTimerId = TimerQueue.Instance.Add(milliseconds, m_cancelForCommandTimeoutAction);
		}
	}

	bool ICancellableCommand.IsTimedOut => Volatile.Read(ref m_commandTimedOut);

	int ICancellableCommand.CommandId => m_commandId;

	int ICancellableCommand.CancelAttemptCount { get; set; }

	ICancellableCommand IMySqlCommand.CancellableCommand => this;

	private IOBehavior AsyncIOBehavior => Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous;

	private void CancelCommandForTimeout()
	{
		Volatile.Write(ref m_commandTimedOut, true);
		Connection?.Cancel(this, m_commandId, false);
	}

	private bool IsValid([NotNullWhen(false)] out Exception? exception)
	{
		exception = null;
		if (m_isDisposed)
			exception = new ObjectDisposedException(GetType().Name);
		else if (Connection is null)
			exception = new InvalidOperationException("Connection property must be non-null.");
		else if (Connection.State != ConnectionState.Open && Connection.State != ConnectionState.Connecting)
			exception = new InvalidOperationException("Connection must be Open; current state is {0}".FormatInvariant(Connection.State));
		else if (!Connection.IgnoreCommandTransaction && Transaction != Connection.CurrentTransaction)
			exception = new InvalidOperationException("The transaction associated with this command is not the connection's active transaction; see https://fl.vu/mysql-trans");
		else if (string.IsNullOrWhiteSpace(CommandText))
			exception = new InvalidOperationException("CommandText must be specified");
		return exception is null;
	}

	PreparedStatements? IMySqlCommand.TryGetPreparedStatements() => CommandType == CommandType.Text && !string.IsNullOrWhiteSpace(CommandText) && m_connection is not null &&
		m_connection.State == ConnectionState.Open ? m_connection.Session.TryGetPreparedStatement(CommandText!) : null;

	CommandBehavior IMySqlCommand.CommandBehavior => m_commandBehavior;
	SingleStoreParameterCollection? IMySqlCommand.OutParameters { get; set; }
	SingleStoreParameter? IMySqlCommand.ReturnParameter { get; set; }

	static readonly ISingleStoreConnectorLogger Log = SingleStoreConnectorLogManager.CreateLogger(nameof(SingleStoreCommand));

	readonly int m_commandId;
	bool m_isDisposed;
	SingleStoreConnection? m_connection;
	string m_commandText;
	SingleStoreParameterCollection? m_parameterCollection;
	SingleStoreAttributeCollection? m_attributeCollection;
	int? m_commandTimeout;
	CommandType m_commandType;
	CommandBehavior m_commandBehavior;
	Action? m_cancelAction;
	Action? m_cancelForCommandTimeoutAction;
	uint m_cancelTimerId;
	bool m_commandTimedOut;
}
