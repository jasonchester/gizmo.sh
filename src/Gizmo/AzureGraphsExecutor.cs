using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Graphs;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Brandmuscle.LocationData.Graph.GremlinConsole
{
    public class AzureGraphsExecutor : IQueryExecutor
    {
        private readonly DocumentClient _client;
        private readonly DocumentCollection _graph;

        public string RemoteMessage => $"cosmos: {_graph.AltLink}@{_client.ServiceEndpoint}";

        public AzureGraphsExecutor(DocumentClient client, DocumentCollection graph)
        {
            _client = client;
            _graph = graph;
        }

        public async Task<bool> TestConnection(CancellationToken ct = default(CancellationToken))
        {
            var connected = false;
            try
            {
                string q;
                if(_graph.PartitionKey.Paths.Any())
                {
                    var partitionKey = _graph.PartitionKey.Paths.FirstOrDefault().TrimStart('\\');
                    q = $"g.V('').has('{partitionKey}','').count();";
                }
                else 
                {
                    q = "g.V('').count();"; 
                }
                var query = _client.CreateGremlinQuery(_graph, q);
                while (query.HasMoreResults) {
                    await query.ExecuteNextAsync<dynamic>();
                }
                connected = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to connect to gremlin server. Please check you appsettings.json");
                Console.WriteLine(ex);
            }

            return connected;
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async Task<string> ExecuteQuery(string gremlinQuery, CancellationToken ct = default(CancellationToken))
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            var output = new StringBuilder();
            int count = 0;
            double cost = 0;
            var query = _client.CreateGremlinQuery<dynamic>(_graph, gremlinQuery);
            while (query.HasMoreResults && !ct.IsCancellationRequested)
            {
                var feedResponse = await query.ExecuteNextAsync(ct);
                cost += feedResponse.RequestCharge;
                foreach (dynamic result in feedResponse)
                {
                    output.AppendLine($"{JsonConvert.SerializeObject(result, Formatting.Indented)}");
                    count++;
                }
            }
            output.Insert(0, $"executed in {timer.Elapsed}. {cost:N2} RUs. {count} results. {output.Length} characters.{Environment.NewLine}");
            return output.ToString();
        }

        private static DocumentClient GetDocumentClient(CosmosDbConnection config)
        {
            // connection issues on osx
            // https://github.com/Azure/azure-documentdb-dotnet/issues/194
            ConnectionPolicy connectionPolicy = new ConnectionPolicy();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                connectionPolicy.ConnectionMode = ConnectionMode.Direct;
                connectionPolicy.ConnectionProtocol = Protocol.Tcp;
            }

            var client = new DocumentClient(
                config.DocumentEndpoint,
                config.AuthKey,
                connectionPolicy);

            return client;
        }

        // private static DocumentClient GetDocumentClient(IConfigurationRoot builder)
        // {
        //     var endpoint = builder["cosmosDBConnection:documentEndpoint"];
        //     var authKey = builder["cosmosDBConnection:authKey"];

        //     // connection issues on osx
        //     // https://github.com/Azure/azure-documentdb-dotnet/issues/194
        //     ConnectionPolicy connectionPolicy = new ConnectionPolicy();
        //     if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        //         connectionPolicy.ConnectionMode = ConnectionMode.Direct;
        //         connectionPolicy.ConnectionProtocol = Protocol.Tcp;
        //     }

        //     var client = new DocumentClient(
        //         new Uri(endpoint),
        //         authKey,
        //         connectionPolicy);

        //     return client;
        // }

        private static async Task<DocumentCollection> GetDocumentCollection(DocumentClient client, CosmosDbConnection config, CancellationToken ct= default(CancellationToken))
        {
            // var databaseId = builder["cosmosDBConnection:databaseId"];
            // var graphId = builder["cosmosDBConnection:graphId"];

            var graph = await client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(config.DatabaseId),
                new DocumentCollection {Id = config.GraphId});
            return graph;
        }

        // public static async Task<AzureGraphsExecutor> GetExecutor(IConfigurationRoot builder, CancellationToken ct= default(CancellationToken))
        // {
        //     DocumentClient client = AzureGraphsExecutor.GetDocumentClient(builder);
        //     var temp = new AzureGraphsExecutor(
        //         client,
        //         await AzureGraphsExecutor.GetDocumentCollection(client, builder)
        //     );
        //     return temp;
        // }

        public static async Task<AzureGraphsExecutor> GetExecutor(CosmosDbConnection config, CancellationToken ct= default(CancellationToken))
        {
            DocumentClient client = AzureGraphsExecutor.GetDocumentClient(config);
            var temp = new AzureGraphsExecutor(
                client,
                await AzureGraphsExecutor.GetDocumentCollection(client, config)
            );
            return temp;
        }
    }
}
