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

        public int RemoveConnection(string connectionName)
        {
            _console.WriteLine($"Removing {connectionName}");

            return 0;
        }

        public async Task<int> AddConnection(string connectionName, CosmosDbConnection connection, bool global = false)
        {
            _console.WriteLine($"Adding {connectionName}");

            // var config = new AppSettings();
            // var builder = new ConfigurationBuilder()

            string configPath = global ? GizmoConfig.ProfileConfig : GizmoConfig.LocalConfigPath;
            
            var settings = await GizmoConfig.LoadConfig(configPath);
            settings.CosmosDbConnections[connectionName] = connection;
            
            await GizmoConfig.SaveConfig(configPath, settings);

            _console.Dump(settings);
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