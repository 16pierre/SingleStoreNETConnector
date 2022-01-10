#if BASELINE
using MySql.Data.MySqlClient;
#endif
using Xunit;

namespace SingleStoreConnector.Tests;

public class MySqlExceptionTests
{
	[Fact]
	public void Data()
	{
		var exception = new SingleStoreException(MySqlErrorCode.No, "two", "three");
		Assert.Equal(1002, exception.Data["Server Error Code"]);
		Assert.Equal("two", exception.Data["SqlState"]);
	}
}
