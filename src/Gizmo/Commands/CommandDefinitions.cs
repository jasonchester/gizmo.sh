using Gizmo.Configuration;
using Gizmo.Connection;
using Gizmo.Console;
using Gizmo.Interactive;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;

namespace Gizmo.Commands
{
    public class CommandDefinitions
    {
        private readonly GizmoConfig _settings;
        private readonly IInteractiveConsole _console;
        private readonly ConnectionCommands _connectionCommands;
        private readonly ConnectionManager _connectionManager;

        public CommandDefinitions(GizmoConfig settings, IInteractiveConsole console, ConnectionManager connectionManager)
        {
            _settings = settings;
            _console = console;
            _connectionManager = connectionManager;
            _connectionCommands = new ConnectionCommands(settings, console);
        }

        public RootCommand Root() => new RootCommand();

        public Command Connection()
        {
            Command connections = new Command("connection", "List, Add, Remove connections");

            connections.AddCommand(List());
            connections.AddCommand(Show());
            // connections.AddCommand(Add());
            connections.AddCommand(Remove());

            return connections;

            Command List() => new Command("list", "List connections")
            {
                Handler = CommandHandler.Create(_connectionCommands.ListConnections)
            };

            Command Show()
            {
                var cmd = new Command("show", "Show connection details")
                {
                    Handler = CommandHandler.Create<string>(_connectionCommands.ShowConnections)
                };

                cmd.Add(ConnectionNameArgument());

                return cmd;
            }


            Command Remove()
            {
                var cmd = new Command("remove", "Remove connection(s)")
                {
                    Handler = CommandHandler.Create<string, bool>(_connectionCommands.RemoveConnection)
                };

                cmd.AddOption(GlobalOption());
                cmd.AddArgument(ConnectionNameArgument());

                return cmd;
            }

            Command Add()
            {

                var cmd = new Command("add", "Add a Connection");
                cmd.AddOption(new Option(new[] { "--global", "-g" }) { Argument = new Argument<bool>() });
                cmd.AddOption(new Option(new[] { "--document-endpoint", "-c" }) { Argument = new Argument<Uri>() });
                cmd.AddOption(new Option(new[] { "--gremlin-endpoint", "-t" }) { Argument = new Argument<string>() });
                cmd.AddOption(new Option(new[] { "--gremlin-port", "-p" }) { Argument = new Argument<int>(() => 443) });
                cmd.AddOption(new Option(new[] { "--authkey", "-k" }) { Argument = new Argument<string>() });
                cmd.AddOption(new Option(new[] { "--database", "-d" }) { Argument = new Argument<string>() });
                cmd.AddOption(new Option(new[] { "--graph" }) { Argument = new Argument<string>() });
                cmd.AddOption(new Option(new[] { "--partitionkey" }) { Argument = new Argument<string>() });

                cmd.AddArgument(
                    new Argument<string>() { Name = "connectionName", Arity = ArgumentArity.ExactlyOne });
                cmd.Handler = CommandHandler.Create<string, CosmosDbConnection, bool>(_connectionCommands.AddConnection);

                return cmd;
            }

            public Command Interactive()
            {
                var cmd = new Command("interactive", "start gizmo interactive shell")
                {
                    Handler = CommandHandler.Create<string, ConnectionType>(
                    async (connectionName, connectionType) =>
                    await new GremlinConsole(_settings, _console).DoREPL(connectionName, connectionType))
                };

                cmd.Add(ConnectionNameArgument());
                return cmd;
            }

            public Command Execute()
            {
                var cmd = new Command("execute", "Execute the query")
                {
                    Handler = CommandHandler.Create<string, string, ConnectionType>(
                        async (query, connectionName, queryExecutor) =>
                        await new ExecuteCommands(_connectionManager, _console)
                            .ExecuteQuery(query, connectionName, queryExecutor)
                    )
                };
                cmd.AddArgument(
                    new Argument<string>()
                    {
                        Name = "query",
                        Description = "gremlin graph query to execute"
                    }
                );

                cmd.AddOption(ConnectionNameOption());
                cmd.AddOption(ConnectionTypeOption());

                return cmd;
            }

            public Command LoadFile()
            {

                Option Skip()
                {
                    var opt = new Option("--skip", "lines to skip from the file")
                    {
                        Argument = new Argument<int>()
                    };
                    opt.Argument.SetDefaultValue(0);
                    return opt;
                }

                Option Take()
                {
                    var opt = new Option("--take", "lines to take from the file")
                    {
                        Argument = new Argument<int>()
                    };
                    opt.Argument.SetDefaultValue(0);
                    return opt;
                }

                Option Parallel()
                {
                    var opt = new Option("--parallel", "number of threads to use")
                    {
                        Argument = new Argument<int>()
                    };
                    opt.Argument.SetDefaultValue(0);
                    return opt;
                }

                var cmd = new Command("load", "Execute queries from files");
                cmd.AddOption(ConnectionNameOption());
                cmd.AddOption(ConnectionTypeOption());
                cmd.AddOption(Skip());
                cmd.AddOption(Take());
                cmd.AddOption(Parallel());

                cmd.AddArgument(QueryFilesArgument());

                cmd.Handler = CommandHandler.Create<string, ConnectionType, int, int, int, ParseResult>(
                    async (connectionName, connectionType, skip, take, parallel, parse) =>
                    await new ExecuteCommands(_connectionManager, _console).LoadFile(
                        parse.CommandResult.GetArgumentValueOrDefault<FileInfo[]>("queries"), connectionName, connectionType, skip, take, parallel)
                );

                return cmd;
            }

            public Command BulkFile()
            {
                var cmd = new Command("bulk", "Execute queries from multiple files")
                {
                    Handler = CommandHandler.Create<string, ConnectionType, int, ParseResult>(
                        async (connectionName, connectionType, parallel, parse) =>
                        await new ExecuteCommands(_connectionManager, _console).BulkFile(
                            parse.CommandResult.GetArgumentValueOrDefault<FileInfo>("bulkFile"), connectionName, connectionType, parallel)
                    )
                };
                cmd.AddArgument(BulkFileArgument());
                cmd.AddOption(ConnectionNameOption());
                cmd.AddOption(ConnectionTypeOption());
                cmd.AddOption(new Option("--parallel", "number of threads to use")
                {
                    Argument = new Argument<int>(() => 1)
                });
                return cmd;
            }

            private Argument<string> ConnectionNameArgument() => new Argument<string>()
            {
                Name = "connectionName",
                Arity = ArgumentArity.ExactlyOne
            }
            .FromAmong(_settings?.CosmosDbConnections?.Keys.ToArray() ?? new string[] { });

            private static Argument<FileInfo> BulkFileArgument() => new Argument<FileInfo>()
            {
                Name = "bulkFile",
                Description = "File containing queries to execute",
                Arity = ArgumentArity.ExactlyOne
            }.ExistingOnly();

            private static Argument<FileInfo[]> QueryFilesArgument() => new Argument<FileInfo[]>()
            {
                Name = "queries",
                Description = "File containing queries to execute",
                Arity = ArgumentArity.OneOrMore
            }.ExistingOnly();

            private static Option GlobalOption() => new Option(new[] { "--global", "-g" }, "use global config file")
            {
                Argument = new Argument<bool>()
            };


            private Option ConnectionNameOption() => new Option(new[] { "--connection-name", "-c" }, "Name of the connection")
            {
                Argument =
                new Argument<string>(defaultValue: () => _settings?.CosmosDbConnections?.Keys.First())
                {
                    Arity = ArgumentArity.ExactlyOne
                }
            };

            private Option ConnectionTypeOption() => new Option(new[] { "--connection-type", "-t" }, "Type of query executor to use")
            {
                Argument = new Argument<ConnectionType>(defaultValue: () => ConnectionType.AzureGraphs)
                {
                    Arity = ArgumentArity.ExactlyOne
                }
            };
        }

    }