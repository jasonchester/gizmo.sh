using System;

namespace Gizmo.Configuration
{
    public partial class CosmosDbConnection
    {
        public Uri DocumentEndpoint { get; set; }
        public string GremlinEndpoint { get; set; }
        public int GremlinPort { get; set; } = 443;
        public string AuthKey { get; set; }
        public string DatabaseId { get; set; }
        public string GraphId { get; set; }
        public string PartitionKey { get; set; }
    }
}