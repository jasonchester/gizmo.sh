using Gizmo.Configuration;
using Gizmo.Console;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gizmo.Commands
{
    public class ConnectionCommands
    {
        private readonly GizmoConfig _settings;
        private readonly IConsole _console;
        public ConnectionCommands(GizmoConfig settings, IConsole console)
        {
            _settings = settings;
            _console = console;
        }

        public async Task<int> RemoveConnection(string connectionName, bool global)
        {
            string configPath = global ? GizmoConfig.ProfileConfigPath : GizmoConfig.LocalConfigPath;
            _console.WriteLine($"Removing {connectionName} from {configPath}");
                        
            var settings = await GizmoConfig.LoadConfig(configPath);
            settings.CosmosDbConnections.Remove(connectionName);
            await GizmoConfig.SaveConfig(configPath, settings);

            return 0;
        }

        public async Task<int> AddConnection(string connectionName, CosmosDbConnection connection, bool global = false)
        {
            string configPath = global ? GizmoConfig.ProfileConfigPath : GizmoConfig.LocalConfigPath;
            _console.WriteLine($"Adding {connectionName} to {configPath}");

            var settings = await GizmoConfig.LoadConfig(configPath);
            settings.CosmosDbConnections[connectionName] = connection;
            await GizmoConfig.SaveConfig(configPath, settings);

            return 0;
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