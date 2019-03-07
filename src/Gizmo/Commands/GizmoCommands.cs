using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gizmo.Configuration;
using Microsoft.Extensions.Configuration;

namespace Gizmo.Commands
{
    public class GizmoCommands
    {

        private readonly AppSettings _settings;
        private readonly ConnectionCommands _connectionCommands;
        public GizmoCommands(AppSettings settings)
        {
            _settings = settings;
            _connectionCommands = new ConnectionCommands(settings);

        }

        internal Command Interactive()
        {
            return new Command("interactive", "start gizmo interactive shell",
                new Option[]
                {
                    new Option(new [] { "--connection-name", "-c"}, "Name of the connection",  ConnectionNameArgument())
                    // ToolPath()
                },
                handler: CommandHandler.Create<string>(Program.DoREPL)
            );
        }

        private Argument<string> ConnectionNameArgument()
        {
            var argument = new Argument<string>("name")
            {
                Arity = ArgumentArity.ExactlyOne
            };

            string[] connectionNames = _settings.CosmosDbConnections.Keys.ToArray();
            argument.FromAmong(connectionNames);
            argument.WithSuggestions(connectionNames);
            argument.SetDefaultValue(_settings.CosmosDbConnections.Keys.First());
            return argument;
        }

        internal Command Connection()
        {
            var connections = new Command("connection", "List, Add, Remove connections");

            connections.AddCommand(List());
            connections.AddCommand(Add());
            connections.AddCommand(Remove());

            return connections;

            Command List() =>
                new Command("list", "List connections",
                    handler: CommandHandler.Create(_connectionCommands.ListConnections)
                );

            Command Remove() =>
                new Command("remove", "Remove connection",
                    //new Option[] { },
                    handler: CommandHandler.Create<string[]> (_connectionCommands.RemoveConnection),
                    //argument: ConnectionNameArgument()
                    argument: new Argument<string[]>() {
                        Name = "connectionNames",
                        Arity = ArgumentArity.OneOrMore
                    }
                    .FromAmong(_settings.CosmosDbConnections.Keys.ToArray())
                );

            Command Add()
            {
                var cmd = new Command("add", "Add a Connection");
                cmd.ConfigureFromMethod(typeof(ConnectionCommands).GetMethod(nameof(ConnectionCommands.AddConnection)), _connectionCommands);
                return cmd;
            }

            //Command Add() =>
            //    new Command("add", "Add connection",
            //        handler: CommandHandler.Create<string, CosmosDbConnection>(_connectionCommands.AddConnection)
            //    );
            
        }
    }

    public class ConnectionCommands
    {
        private readonly AppSettings _settings;
        public ConnectionCommands(AppSettings settings)
        {
            _settings = settings;

        }

        public int RemoveConnection(string[] connectionNames)
        {
            foreach(var connectionName in connectionNames)
            {
                Console.WriteLine($"Removing {connectionName}");
            }

            return 0;
        }

        public void AddConnection(string connectionName, CosmosDbConnection connection) => Console.WriteLine($"Adding {connectionName}");

        public void ListConnections()
        {
            Console.WriteLine("List");
            foreach (var c in _settings.CosmosDbConnections.Keys)
            {
                Console.WriteLine($"{c}: {_settings.CosmosDbConnections[c].DocumentEndpoint}");
            }
        }
    }
}