NUGET_SOURCE_PATH=~/Code/nuget_local_source


VERSION=2.0.0
dotnet pack --configuration Release  -p:TreatWarningsAsErrors=false -o build  /p:PackageVersion=2.0.0

nuget add build/SchemaCollectionGenerator.${VERSION}.nupkg -Source ${NUGET_SOURCE_PATH}
nuget add build/SingleStoreConnector.${VERSION}.nupkg -Source ${NUGET_SOURCE_PATH}
nuget add build/SingleStoreConnector.Authentication.Ed25519.${VERSION}.nupkg -Source ${NUGET_SOURCE_PATH}
nuget add build/SingleStoreConnector.Logging.Microsoft.Extensions.Logging.${VERSION}.nupkg -Source ${NUGET_SOURCE_PATH}
nuget add build/SingleStoreConnector.Logging.NLog.${VERSION}.nupkg -Source ${NUGET_SOURCE_PATH}
nuget add build/SingleStoreConnector.Logging.Serilog.${VERSION}.nupkg -Source ${NUGET_SOURCE_PATH}
nuget add build/SingleStoreConnector.Logging.log4net.${VERSION}.nupkg -Source ${NUGET_SOURCE_PATH}



