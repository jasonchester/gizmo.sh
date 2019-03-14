using System.Collections.Generic;

namespace Gizmo.Configuration
{
    public partial class AppSettings
    {
        public Dictionary<string, CosmosDbConnection> CosmosDbConnections { get; set; }
    }
}