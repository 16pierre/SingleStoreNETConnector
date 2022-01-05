## About

This package integrates SingleStoreConnector logging with [NLog](https://www.nuget.org/packages/NLog/).

## How to Use

Add the following line of code to your application startup routine (before any `SingleStoreConnector` objects have been used):

```csharp
MySqlConnectorLogManager.Provider = new NLogLoggerProvider();
```
