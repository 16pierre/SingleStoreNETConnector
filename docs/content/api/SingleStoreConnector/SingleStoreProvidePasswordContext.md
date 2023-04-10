# SingleStoreProvidePasswordContext class

Provides context for the [`ProvidePasswordCallback`](./SingleStoreConnection/ProvidePasswordCallback.md) delegate.

```csharp
public sealed class SingleStoreProvidePasswordContext
```

## Public Members

| name | description |
| --- | --- |
| [Database](SingleStoreProvidePasswordContext/Database.md) { get; } | The optional initial database; this value may be the empty string. This corresponds to [`Database`](./SingleStoreConnectionStringBuilder/Database.md). |
| [Port](SingleStoreProvidePasswordContext/Port.md) { get; } | The server port. This corresponds to [`Port`](./SingleStoreConnectionStringBuilder/Port.md). |
| [Server](SingleStoreProvidePasswordContext/Server.md) { get; } | The server to which SingleStoreConnector is connecting. This is a host name from the [`Server`](./SingleStoreConnectionStringBuilder/Server.md) option. |
| [UserId](SingleStoreProvidePasswordContext/UserId.md) { get; } | The user ID being used for authentication. This corresponds to [`UserID`](./SingleStoreConnectionStringBuilder/UserID.md). |

## See Also

* namespace [SingleStoreConnector](../SingleStoreConnector.md)

<!-- DO NOT EDIT: generated by xmldocmd for SingleStoreConnector.dll -->