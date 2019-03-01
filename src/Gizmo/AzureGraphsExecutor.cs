using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Gizmo.Configuration;
namespace Gizmo
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
                string q = "g.inject(true);";
                var query = _client.CreateGremlinQuery(_graph, q);
                if (query.HasMoreResults)
                {
                    await query.ExecuteNextAsync<dynamic>();
                    connected = true;
                }
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

        public async Task<QueryResultSet<T>> ExecuteQuery<T>(string query, CancellationToken ct = default)
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            // var output = new StringBuilder();
            // int count = 0;
            // double cost = 0;
            var q = _client.CreateGremlinQuery<T>(_graph, query);


            var results = new FeedResponseAggregator<T>(query);
            while (q.HasMoreResults && !ct.IsCancellationRequested)
            {
                var feedResponse = await q.ExecuteNextAsync<T>(ct);
                results.AddResponse(feedResponse);
            }
            //output.Insert(0, $"executed in {timer.Elapsed}. {cost:N2} RUs. {count} results. {output.Length} characters.{Environment.NewLine}");
            //return output.ToString();
            return results.ToQueryResultSet();
        }

        private static DocumentClient GetDocumentClient(CosmosDbConnection config)
        {
            // connection issues on osx
            // https://github.com/Azure/azure-documentdb-dotnet/issues/194
            ConnectionPolicy connectionPolicy = new ConnectionPolicy();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                connectionPolicy.ConnectionMode = ConnectionMode.Direct;
                connectionPolicy.ConnectionProtocol = Protocol.Tcp;
            }

            var client = new DocumentClient(
                config.DocumentEndpoint,
                config.AuthKey,
                connectionPolicy);

            return client;
        }

        private static async Task<DocumentCollection> GetDocumentCollection(DocumentClient client, CosmosDbConnection config, CancellationToken ct = default(CancellationToken))
        {
            var graph = await client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(config.DatabaseId),
                new DocumentCollection { Id = config.GraphId });
            return graph;
        }

        public static async Task<AzureGraphsExecutor> GetExecutor(CosmosDbConnection config, CancellationToken ct = default(CancellationToken))
        {
            DocumentClient client = AzureGraphsExecutor.GetDocumentClient(config);
            var temp = new AzureGraphsExecutor(
                client,
                await AzureGraphsExecutor.GetDocumentCollection(client, config)
            );
            return temp;
        }
        private class FeedResponseAggregator<T>
        {
            private readonly string _query;
            private readonly List<FeedResponse<T>> responses = new List<FeedResponse<T>>();
            private readonly Stopwatch timer = Stopwatch.StartNew();

            public FeedResponseAggregator(string query)
            {
                _query = query;
            }

            public void AddResponse(FeedResponse<T> response)
            {
                responses.Add(response);
            }

            public QueryResultSet<T> ToQueryResultSet()
            {
                timer.Stop();

                var data = responses.SelectMany(r => r).ToList();

                double requestCharge = responses.Sum(r => r.RequestCharge);
                var attributes = new Dictionary<string, object>
                {
                    ["RequestCharge"] = requestCharge,
                    ["ElapsedTime"] = timer.Elapsed
                };

                return new QueryResultSet<T>(_query, data.AsReadOnly(), timer.Elapsed, requestCharge, attributes);
            }
        }
    }
}
