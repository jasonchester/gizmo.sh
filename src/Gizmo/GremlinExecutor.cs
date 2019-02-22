using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gizmo;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Gizmo
{
    public class GremlinExecutor : IQueryExecutor
    {
        private readonly GremlinClient _client;
        private readonly GremlinServer _server;

        private readonly string _partitionKey;
        public string RemoteMessage => $"gremlin: {_server.Username}@{_server.Uri}";

        public GremlinExecutor(GremlinServer server, CosmosDbConnection config)
        {
            _server = server;
            _partitionKey = config.PartitionKey;
            _client = new GremlinClient(_server, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType);
        }

        public GremlinExecutor(CosmosDbConnection config) : this(GremlinExecutor.GetGremlinServer(config), config)
        {
            //intentionally blank
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async Task<bool> TestConnection(CancellationToken ct = default(CancellationToken))
        {
            var connected = false;
            try
            {
                connected = await _client.SubmitWithSingleResultAsync<bool>("g.inject(true);");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to connect to gremlin server. Please check you appsettings.json");
                Console.WriteLine(ex);
            }
            return connected;
        }

        public async Task<QueryResultSet<T>> ExecuteQuery<T>(string query, CancellationToken ct = default(CancellationToken))
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            var results = await _client.SubmitAsync<T>(query);
            timer.Stop();
            return new QueryResultSet<T>(query, results, timer.Elapsed, results.StatusAttributes);
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
