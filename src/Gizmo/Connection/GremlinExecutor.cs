using System;
using System.CommandLine;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gizmo;
using Gizmo.Configuration;
using Gizmo.Console;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Polly;
using static Gizmo.Connection.GizmoPolicies;

namespace Gizmo.Connection
{
    public class GremlinExecutor : IQueryExecutor
    {
        private readonly IConsole _console;
        private readonly GremlinClient _client;
        private readonly GremlinServer _server;
        private readonly string _partitionKey;

        public string RemoteMessage => $"gremlin: {_server.Username}@{_server.Uri}";

        public GremlinExecutor(CosmosDbConnection config, IConsole console)
        {
            _console = console;
            _server = GremlinExecutor.GetGremlinServer(config);
            _partitionKey = config.PartitionKey;
            _client = new GremlinClient(_server, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async Task<bool> TestConnection(CancellationToken ct = default)
        {
            var connected = false;
            try
            {
                connected = await _client.SubmitWithSingleResultAsync<bool>("g.inject(true);");
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
            var retryCount = 0;
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            var results = await CosmosRetryAfterWait(
                new RetryOption() //{ RetryCount = 5, WaitTime = 30} these are the defaults
            ).ExecuteAsync(
                (context) =>
                {
                    retryCount = (int)context["retryCount"];
                    return _client.SubmitAsync<T>(query);
                }, new Polly.Context
                {
                    {"retryCount", 0}
                }
            );
            timer.Stop();

            var requestCharge = (double)results.StatusAttributes["x-ms-total-request-charge"];
            return new QueryResultSet<T>(query, results, timer.Elapsed, requestCharge, retryCount, results.StatusAttributes);

        }

        private static GremlinServer GetGremlinServer(CosmosDbConnection config)
        {
            var hostname = config.GremlinEndpoint;
            var port = config.GremlinPort;
            var authKey = config.AuthKey;
            var databaseId = config.DatabaseId;
            var graphId = config.GraphId;

            var gremlinServer = new GremlinServer(hostname, port, enableSsl: true, username: $"/dbs/{databaseId}/colls/{graphId}", password: authKey);
            return gremlinServer;
        }
    }
}
