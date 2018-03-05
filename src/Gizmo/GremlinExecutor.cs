using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gremlin.Net.Driver;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Brandmuscle.LocationData.Graph.GremlinConsole
{
    public class GremlinExecutor : IQueryExecutor
    {
        private readonly GremlinClient _client;
        private readonly GremlinServer _server;

        private readonly string _partitionKey;
        public string RemoteMessage => $"gremlin: {_server.Username}@{_server.Uri}";

        public GremlinExecutor(GremlinServer server, IConfigurationRoot builder)
        {
            _server = server;
            _partitionKey = builder["cosmosDBConnection:partitionKey"];            
            _client = new GremlinClient(_server);
        }

        public GremlinExecutor(IConfigurationRoot builder) : this(GremlinExecutor.GetGremlinServer(builder), builder)
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
                string q;
                if(string.IsNullOrWhiteSpace(_partitionKey))
                {             
                    q = "g.V('').count();";
                }
                else
                {
                    q = $"g.V('').has('{_partitionKey}','').count();";
                }
                await _client.SubmitWithSingleResultAsync<dynamic>(q);
                connected = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to connect to gremlin server. Please check you appsettings.json");
                Console.WriteLine(ex);
            }

            return connected;
        }

        public async Task<string> ExecuteQuery(string query, CancellationToken ct = default(CancellationToken))
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            var output = new StringBuilder();

            var results = await _client.SubmitAsync<dynamic>(query);
            
            output.AppendLine($"executed in {timer.Elapsed}. {results.Count} results. {output.Length} characters.");
            output.AppendLine(JsonConvert.SerializeObject(results, Formatting.Indented));
            return output.ToString();
        }

        private static GremlinServer GetGremlinServer(IConfigurationRoot builder)
        {
            var hostname = builder["cosmosDBConnection:gremlinEndpoint"];
            var port = builder.GetValue<int>("cosmosDBConnection:gremlinPort", 443);
            var authKey = builder["cosmosDBConnection:authKey"];
            var databaseId = builder["cosmosDBConnection:databaseId"];
            var graphId = builder["cosmosDBConnection:graphId"];

            var gremlinServer = new GremlinServer(hostname, port, enableSsl: true, username: $"/dbs/{databaseId}/colls/{graphId}", password: authKey);
            return gremlinServer;
        }
    }
}
