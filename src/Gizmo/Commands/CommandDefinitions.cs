using Gizmo.Configuration;
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
            connections.AddCommand(Add());
            connections.AddCommand(Remove());

            return connections;

            Command List() => new Command("list", "List connections",
                    handler: CommandHandler.Create(_connectionCommands.ListConnections)
                );

            Command Show() => new Command("show", "Show connection details",
                    argument: ConnectionNameArgument(),
                    handler: CommandHandler.Create<string>(_connectionCommands.ShowConnections)
                );

            Command Remove() => new Command("remove", "Remove connection(s)",
                    new Option[] {
                        GlobalOption()
                    },
                    argument: ConnectionNameArgument(),
                    handler: CommandHandler.Create<string, bool>(_connectionCommands.RemoveConnection)
                );


            Command Add() => new Command("add", "Add a Connection",
                    new Option[] {
                        new Option(new [] { "--global", "-g"}, "", new Argument<bool>(false)),
                        new Option(new [] { "--document-endpoint", "-c" }, "", new Argument<Uri>()),
                        new Option(new [] { "--gremlin-endpoint", "-t" }, "", new Argument<string>()),
                        new Option(new [] { "--gremlin-port", "-p" }, "", new Argument<int>(443)),
                        new Option(new [] { "--authkey", "-k" }, "", new Argument<string>()),
                        new Option(new [] { "--database", "-d" }, "", new Argument<string>()),
                        new Option(new [] { "--graph" }, "", new Argument<string>()),
                        new Option(new [] { "--partitionkey"}, "", new Argument<string>())
                    },
                    argument: new Argument<string>() { Name = "connectionName", Arity = ArgumentArity.ExactlyOne },
                    handler: CommandHandler.Create<string, CosmosDbConnection, bool>(_connectionCommands.AddConnection)
                );

        }


        public Command Interactive() => new Command("interactive", "start gizmo interactive shell",
            symbols: new Option[]
            {
                ConnectionNameOption(),
                QueryExecutorOption()
            },
            handler: CommandHandler.Create<string, ConnectionType>(
                async (connectionName, connectionType) =>
                await new GremlinConsole(_settings, _console).DoREPL(connectionName, connectionType))
        );

        public Command Execute() => new Command("execute", "Execute the query",
            new Option[] {
                ConnectionNameOption(),
                QueryExecutorOption()
            },
            argument: new Argument<string>() { Name = "query", Description = "gremlin graph query to execute" },
            handler: CommandHandler.Create<string, string, ConnectionType>(
                async (query, connectionName, queryExecutor) =>
                await new ExecuteCommands(_connectionManager, _console).ExecuteQuery(query, connectionName, queryExecutor)
            )
        );

        public Command LoadFile() => new Command("load", "Execute queries from files",
            new Option[] {
                ConnectionNameOption(),
                QueryExecutorOption(),
                new Option("--skip", "lines to skip from the file", new Argument<int>(0)),
                new Option("--take", "lines to take from the file", new Argument<int>(0)),
                new Option("--parallel", "number of threads to use", new Argument<int>(1))
            },
            argument: QueryFilesArgument(),        
            handler: CommandHandler.Create<string, ConnectionType, int, int, int, ParseResult>(
                async (connectionName, connectionType, skip, take, parallel, parse) =>
                await new ExecuteCommands(_connectionManager, _console).LoadFile(parse.CommandResult.GetValueOrDefault<FileInfo[]>(), connectionName, connectionType, skip, take, parallel)
            )
        );

        private Argument<string> ConnectionNameArgument() => new Argument<string>()
        {
            Name = "connectionName",
            Arity = ArgumentArity.ExactlyOne
        }
        .FromAmong(_settings?.CosmosDbConnections?.Keys.ToArray() ?? new string[] { });

        public Command BulkFile() => new Command("bulk", "Execute queries from multiple files",
            new Option[] {
                ConnectionNameOption(),
                QueryExecutorOption(),
                new Option("--parallel", "number of threads to use", new Argument<int>(1))
            },
            argument: BulkFileArgument(),
            handler: CommandHandler.Create<string, ConnectionType, int, ParseResult>(
                async (connectionName, connectionType, parallel, parse) =>
                await new ExecuteCommands(_connectionManager, _console).BulkFile(parse.CommandResult.GetValueOrDefault<FileInfo>(), connectionName, connectionType, parallel)
            )
        );

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

        private static Option GlobalOption() => new Option(new[] { "--global", "-g" }, "", new Argument<bool>(false));
        
        private Option ConnectionNameOption() => new Option(new[] { "--connection-name", "-c" }, "Name of the connection",
            new Argument<string>(defaultValue: () => _settings?.CosmosDbConnections?.Keys.First())
            {
                Arity = ArgumentArity.ExactlyOne
            }
        );

        private Option QueryExecutorOption() => new Option(new[] { "--query-executor", "-e" }, "Type of query executor to use",
            new Argument<ConnectionType>(defaultValue: ConnectionType.AzureGraphs)
            {
                Arity = ArgumentArity.ExactlyOne
            }
        );
    }

}