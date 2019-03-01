using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using Gizmo.Configuration;
using Microsoft.Extensions.Configuration;

namespace Gizmo.Commands
{
    public class GizmoCommands
    {

        private readonly AppSettings _settings;
        public GizmoCommands(AppSettings settings)
        {
            _settings = settings;
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

        private static Argument<string> ConnectionNameArgument()
        {
            var argument = new Argument<string>();
            argument.SetDefaultValue("default");
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
                    // new Option[]
                    // {
                    //     // Global(),
                    //     // ToolPath()
                    // },
                    handler: CommandHandler.Create(
                        async () => {
                            Console.WriteLine("List");
                            foreach(var c in _settings.CosmosDbConnections.Keys)
                            {
                                Console.WriteLine($"{c}: {_settings.CosmosDbConnections[c].DocumentEndpoint}");
                            }
                        }

                    )
                );

            Command Remove() =>
                new Command("remove", "Remove connection",
                    new Option[]
                    {
                        new Option(new [] { "--name", "-n"}, "Name of the connection",  new Argument<string>())
                        // Global(),
                        // ToolPath()
                    },
                    handler: CommandHandler.Create(
                        async () => Console.WriteLine("Remove")
                    ),
                    argument: new Argument<string>()
                );

            Command Add() =>
                new Command("add", "Add connection",
                    // new Option[]
                    // {
                    //     // Global(),
                    //     // ToolPath()
                    // },
                    handler: CommandHandler.Create(
                        async () => Console.WriteLine("Add")
                    )
                );
            
        }
    }
}