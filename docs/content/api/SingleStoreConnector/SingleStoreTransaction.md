# SingleStoreTransaction class

[`SingleStoreTransaction`](./SingleStoreTransaction.md) represents an in-progress transaction on a SingleStore Server.

```csharp
public sealed class SingleStoreTransaction : DbTransaction
```

## Public Members

| name | description |
| --- | --- |
| [Connection](SingleStoreTransaction/Connection.md) { get; } | Gets the [`SingleStoreConnection`](./SingleStoreConnection.md) that this transaction is associated with. |
| override [IsolationLevel](SingleStoreTransaction/IsolationLevel.md) { get; } | Gets the [`IsolationLevel`](./SingleStoreTransaction/IsolationLevel.md) of this transaction. This value is set from [`BeginTransaction`](./SingleStoreConnection/BeginTransaction.md) or any other overload that specifies an [`IsolationLevel`](./SingleStoreTransaction/IsolationLevel.md). |
| override [Commit](SingleStoreTransaction/Commit.md)() | Commits the database transaction. |
| override [CommitAsync](SingleStoreTransaction/CommitAsync.md)(…) | Asynchronously commits the database transaction. |
| override [DisposeAsync](SingleStoreTransaction/DisposeAsync.md)() | Asynchronously releases any resources associated with this transaction. If it was not committed, it will be rolled back. |
| override [Release](SingleStoreTransaction/Release.md)(…) | Removes the named transaction savepoint with the specified *savepointName*. No commit or rollback occurs. |
| override [ReleaseAsync](SingleStoreTransaction/ReleaseAsync.md)(…) | Asynchronously removes the named transaction savepoint with the specified *savepointName*. No commit or rollback occurs. |
| override [Rollback](SingleStoreTransaction/Rollback.md)() | Rolls back the database transaction. |
| override [Rollback](SingleStoreTransaction/Rollback.md)(…) | Rolls back the current transaction to the savepoint with the specified *savepointName* without aborting the transaction. |
| override [RollbackAsync](SingleStoreTransaction/RollbackAsync.md)(…) | Asynchronously rolls back the database transaction. (2 methods) |
| override [Save](SingleStoreTransaction/Save.md)(…) | Sets a named transaction savepoint with the specified *savepointName*. If the current transaction already has a savepoint with the same name, the old savepoint is deleted and a new one is set. |
| override [SaveAsync](SingleStoreTransaction/SaveAsync.md)(…) | Asynchronously sets a named transaction savepoint with the specified *savepointName*. If the current transaction already has a savepoint with the same name, the old savepoint is deleted and a new one is set. |

## Protected Members

| name | description |
| --- | --- |
| override [DbConnection](SingleStoreTransaction/DbConnection.md) { get; } | Gets the [`SingleStoreConnection`](./SingleStoreConnection.md) that this transaction is associated with. |
| override [Dispose](SingleStoreTransaction/Dispose.md)(…) | Releases any resources associated with this transaction. If it was not committed, it will be rolled back. |

## See Also

* namespace [SingleStoreConnector](../SingleStoreConnector.md)

<!-- DO NOT EDIT: generated by xmldocmd for SingleStoreConnector.dll -->