# SingleStoreCommand constructor (1 of 5)

Initializes a new instance of the [`SingleStoreCommand`](../SingleStoreCommand.md) class.

```csharp
public SingleStoreCommand()
```

## See Also

* class [SingleStoreCommand](../SingleStoreCommand.md)
* namespace [SingleStoreConnector](../../SingleStoreConnector.md)

---

# SingleStoreCommand constructor (2 of 5)

Initializes a new instance of the [`SingleStoreCommand`](../SingleStoreCommand.md) class, setting [`CommandText`](./CommandText.md) to *commandText*.

```csharp
public SingleStoreCommand(string? commandText)
```

| parameter | description |
| --- | --- |
| commandText | The text to assign to [`CommandText`](./CommandText.md). |

## See Also

* class [SingleStoreCommand](../SingleStoreCommand.md)
* namespace [SingleStoreConnector](../../SingleStoreConnector.md)

---

# SingleStoreCommand constructor (3 of 5)

Initializes a new instance of the [`SingleStoreCommand`](../SingleStoreCommand.md) class with the specified [`SingleStoreConnection`](../SingleStoreConnection.md) and [`SingleStoreTransaction`](../SingleStoreTransaction.md).

```csharp
public SingleStoreCommand(SingleStoreConnection? connection, SingleStoreTransaction? transaction)
```

| parameter | description |
| --- | --- |
| connection | The [`SingleStoreConnection`](../SingleStoreConnection.md) to use. |
| transaction | The active [`SingleStoreTransaction`](../SingleStoreTransaction.md), if any. |

## See Also

* class [SingleStoreConnection](../SingleStoreConnection.md)
* class [SingleStoreTransaction](../SingleStoreTransaction.md)
* class [SingleStoreCommand](../SingleStoreCommand.md)
* namespace [SingleStoreConnector](../../SingleStoreConnector.md)

---

# SingleStoreCommand constructor (4 of 5)

Initializes a new instance of the [`SingleStoreCommand`](../SingleStoreCommand.md) class with the specified command text and [`SingleStoreConnection`](../SingleStoreConnection.md).

```csharp
public SingleStoreCommand(string? commandText, SingleStoreConnection? connection)
```

| parameter | description |
| --- | --- |
| commandText | The text to assign to [`CommandText`](./CommandText.md). |
| connection | The [`SingleStoreConnection`](../SingleStoreConnection.md) to use. |

## See Also

* class [SingleStoreConnection](../SingleStoreConnection.md)
* class [SingleStoreCommand](../SingleStoreCommand.md)
* namespace [SingleStoreConnector](../../SingleStoreConnector.md)

---

# SingleStoreCommand constructor (5 of 5)

Initializes a new instance of the [`SingleStoreCommand`](../SingleStoreCommand.md) class with the specified command text,[`SingleStoreConnection`](../SingleStoreConnection.md), and [`SingleStoreTransaction`](../SingleStoreTransaction.md).

```csharp
public SingleStoreCommand(string? commandText, SingleStoreConnection? connection, 
    SingleStoreTransaction? transaction)
```

| parameter | description |
| --- | --- |
| commandText | The text to assign to [`CommandText`](./CommandText.md). |
| connection | The [`SingleStoreConnection`](../SingleStoreConnection.md) to use. |
| transaction | The active [`SingleStoreTransaction`](../SingleStoreTransaction.md), if any. |

## See Also

* class [SingleStoreConnection](../SingleStoreConnection.md)
* class [SingleStoreTransaction](../SingleStoreTransaction.md)
* class [SingleStoreCommand](../SingleStoreCommand.md)
* namespace [SingleStoreConnector](../../SingleStoreConnector.md)

<!-- DO NOT EDIT: generated by xmldocmd for SingleStoreConnector.dll -->