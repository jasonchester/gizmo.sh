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
using Gizmo.Configuration;
using Gizmo.Console;
using System.CommandLine;
using Polly;

namespace Gizmo.Connection
{
    public class AzureGraphsExecutor : IQueryExecutor
    {
        private readonly CosmosDbConnection _config;
        private readonly IConsole _console;
        private DocumentClient _client;
        private DocumentCollection _graph;

        public string RemoteMessage => $"cosmos: {_graph.AltLink}@{_client.ServiceEndpoint}";

        public AzureGraphsExecutor(CosmosDbConnection config, IConsole console)
        {
            _config = config;
            _console = console;
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private async Task Initilize(CancellationToken ct = default)
        {
            _client = _client ?? GetDocumentClient(_config);
            _graph = _graph ?? await GetDocumentCollection(_client, _config);

            DocumentClient GetDocumentClient(CosmosDbConnection config)
            {
                // connection issues on osx
                // https://github.com/Azure/azure-documentdb-dotnet/issues/194
                ConnectionPolicy connectionPolicy = new ConnectionPolicy();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    connectionPolicy.ConnectionMode = ConnectionMode.Direct;
                    connectionPolicy.ConnectionProtocol = Protocol.Tcp;
                    connectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 0;
                }

                var client = new DocumentClient(
                    config.DocumentEndpoint,
                    config.AuthKey,
                    connectionPolicy);

                return client;
            }

            async Task<DocumentCollection> GetDocumentCollection(DocumentClient client, CosmosDbConnection config)
            {
                var graph = await client.CreateDocumentCollectionIfNotExistsAsync(
                    UriFactory.CreateDatabaseUri(config.DatabaseId),
                    new DocumentCollection { Id = config.GraphId });
                return graph;
            }
        }

        public async Task<bool> TestConnection(CancellationToken ct = default)
        {
            await Initilize(ct);

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
                _console.WriteLine("Unable to connect to gremlin server. Please check you appsettings.json");
                _console.WriteLine(ex);
            }

            return connected;
        }

        public async Task<QueryResultSet<T>> ExecuteQuery<T>(string query, CancellationToken ct = default)
        {
            await Initilize(ct);

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            // var output = new StringBuilder();
            // int count = 0;
            // double cost = 0;
            var q = _client.CreateGremlinQuery<T>(_graph, query);

            
            var results = new FeedResponseAggregator<T>(query);
            int totalRetries = 0;

            while (q.HasMoreResults && !ct.IsCancellationRequested)
            {
                int retryCount = 0;
                var feedResponse = await GizmoPolicies.CosmosRetryAfterWait(
                    new RetryOption()
                ).ExecuteAsync( (context) => {
                    retryCount = (int)context["retryCount"];
                    return q.ExecuteNextAsync<T>(ct);
                }, new Polly.Context
                {
                    {"retryCount", 0}
                });
                totalRetries += retryCount;
                results.AddResponse(feedResponse);
            }
            //output.Insert(0, $"executed in {timer.Elapsed}. {cost:N2} RUs. {count} results. {output.Length} characters.{Environment.NewLine}");
            //return output.ToString();
            return results.ToQueryResultSet(totalRetries);
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

            public QueryResultSet<T> ToQueryResultSet(int retryCount = 0)
            {
                timer.Stop();

                var data = responses.SelectMany(r => r).ToList();

                double requestCharge = responses.Sum(r => r.RequestCharge);
                var attributes = new Dictionary<string, object>
                {
                    ["RequestCharge"] = requestCharge,
                    ["ElapsedTime"] = timer.Elapsed
                };

                return new QueryResultSet<T>(_query, data.AsReadOnly(), timer.Elapsed, requestCharge, retryCount, attributes);
            }
        }
    }
}
