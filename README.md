## gizmo.sh

Interactive Console application for executing gremlin queries in Azure Cosmos DB Graph collections.

## Setting User Secrets

Use the following to set up secrets for local development.

```
dotnet user-secrets set "cosmosDBConnection:documentEndpoint" "https://<yourcosmos>.documents.azure.com:443/
dotnet user-secrets set "cosmosDBConnection:gremlinEndpoint" "<yourcosmos>.gremlin.cosmosdb.azure.com"
dotnet user-secrets set "cosmosDBConnection:authKey" "<yourKey>"
dotnet user-secrets set "cosmosDBConnection:databaseId" "db1"
dotnet user-secrets set "cosmosDBConnection:graphId" "graph1"
dotnet user-secrets set "cosmosDBConnection:partitionKey" "<optional partition key>"
```