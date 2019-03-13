using Gizmo.Configuration;
using Gizmo.Console;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

namespace Gizmo.Commands
{
    public class ConnectionCommands
    {
        private readonly AppSettings _settings;
        private readonly IConsole _console;
        public ConnectionCommands(AppSettings settings, IConsole console)
        {
            _settings = settings;
            _console = console;
        }

        public int RemoveConnection(string connectionName)
        {
            _console.WriteLine($"Removing {connectionName}");

            return 0;
        }

        public void AddConnection(string connectionName, CosmosDbConnection connection)
        {
            _console.WriteLine($"Adding {connectionName}");
            _console.Dump(connection);
        }

        public void ListConnections()
        {
            _console.WriteLine("List");
            foreach (KeyValuePair<string, CosmosDbConnection> c in _settings.CosmosDbConnections)
            {
                _console.WriteLine($"{c.Key}: {c.Value.DocumentEndpoint}");
            }
        }

        public void ShowConnections(string connectionName)
        {
            var selectedSettings = _settings.CosmosDbConnections
                .Where(c => c.Key == connectionName)
                .ToDictionary(c => c.Key, c => c.Value);

            _console.Dump(selectedSettings);
        }
    }

}