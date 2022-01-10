#if BASELINE
using MySqlConnectorFactory = MySql.Data.MySqlClient.MySqlClientFactory;
#endif

namespace SideBySide;

public class ClientFactoryTests
{
	[Fact]
	public void CreateCommand()
	{
		Assert.IsType<SingleStoreCommand>(MySqlConnectorFactory.Instance.CreateCommand());
	}

	[Fact]
	public void CreateConnection()
	{
		Assert.IsType<SingleStoreConnection>(MySqlConnectorFactory.Instance.CreateConnection());
	}

	[Fact]
	public void CreateConnectionStringBuilder()
	{
		Assert.IsType<SingleStoreConnectionStringBuilder>(MySqlConnectorFactory.Instance.CreateConnectionStringBuilder());
	}


	[Fact]
	public void CreateParameter()
	{
		Assert.IsType<SingleStoreParameter>(MySqlConnectorFactory.Instance.CreateParameter());
	}

	[Fact]
	public void CreateCommandBuilder()
	{
		Assert.IsType<SingleStoreCommandBuilder>(MySqlConnectorFactory.Instance.CreateCommandBuilder());
	}

	[Fact]
	public void CreateDataAdapter()
	{
		Assert.IsType<SingleStoreDataAdapter>(MySqlConnectorFactory.Instance.CreateDataAdapter());
	}

	[Fact]
	public void DbProviderFactoriesGetFactory()
	{
#if !NET452 && !NET461 && !NET472
		DbProviderFactories.RegisterFactory("SingleStoreConnector", MySqlConnectorFactory.Instance);
#endif
#if BASELINE
		var providerInvariantName = "MySql.Data.MySqlClient";
#else
		var providerInvariantName = "SingleStoreConnector";
#endif
		var factory = DbProviderFactories.GetFactory(providerInvariantName);
		Assert.NotNull(factory);
		Assert.Same(MySqlConnectorFactory.Instance, factory);

		using (var connection = new SingleStoreConnection())
		{
			factory = System.Data.Common.DbProviderFactories.GetFactory(connection);
			Assert.NotNull(factory);
			Assert.Same(MySqlConnectorFactory.Instance, factory);
		}
	}
}
