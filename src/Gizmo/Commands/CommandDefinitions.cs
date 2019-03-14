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
        private readonly AppSettings _settings;
        private readonly IInteractiveConsole _console;
        private readonly ConnectionCommands _connectionCommands;
        private readonly ConnectionManager _connectionManager;

        public CommandDefinitions(AppSettings settings, IInteractiveConsole console, ConnectionManager connectionManager)
        {
            _settings = settings;
            _console = console;
            _connectionManager = connectionManager;
            _connectionCommands = new ConnectionCommands(settings, console);
        }

        public RootCommand Root() => new RootCommand(
        // symbols: new Option[]
        // {
        //     ConnectionNameOption(),
        //     QueryExecutorOption()
        // },
        // handler: CommandHandler.Create<string, ConnectionType>(
        //     async (connectionName, connectionType) =>
        //     await new GremlinConsole(_settings, _console).DoREPL(connectionName, connectionType))
        );

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
                    //symbols: new[] { new Option("", argument: ConnectionNamesArgument()) },
                    argument: ConnectionNameArgument(),
                    handler: CommandHandler.Create<string>(_connectionCommands.ShowConnections)
                );

            Command Remove() => new Command("remove", "Remove connection(s)",
                    //symbols: new[] { new Option("", argument: ConnectionNamesArgument()) }, 
                    argument: ConnectionNameArgument(),
                    handler: CommandHandler.Create<string>(_connectionCommands.RemoveConnection)
                );

            // public Uri DocumentEndpoint { get; set; }
            // public string GremlinEndpoint { get; set; }
            // public int GremlinPort { get; set; } = 443;
            // public string AuthKey { get; set; }
            // public string DatabaseId { get; set; }
            // public string GraphId { get; set; }
            // public string PartitionKey { get; set; }

            Command Add() => new Command("add", "Add a Connection",
                    new Option[] {
                        new Option(new [] { "--document-endpoint", "-c" }, "", new Argument<Uri>()),
                        new Option(new [] { "--gremlin-endpoint", "-g" }, "", new Argument<string>()),
                        new Option(new [] { "--gremlin-port", "-p" }, "", new Argument<int>(443)),
                        new Option(new [] { "--authkey", "-k" }, "", new Argument<string>()),
                        new Option(new [] { "--database", "-d" }, "", new Argument<string>()),
                        new Option(new [] { "--graph" }, "", new Argument<string>()),
                        new Option(new [] { "--partitionkey"}, "", new Argument<string>())
                    },
                    argument: new Argument<string>(),
                    handler: CommandHandler.Create<string, CosmosDbConnection>(_connectionCommands.AddConnection)
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
            argument: new Argument<FileInfo[]>()
            {
                Name = "queries",
                Description = "File containing queries to execute",
                Arity = ArgumentArity.OneOrMore
            }.ExistingOnly(),
            // handler: CommandHandler.Create<ParseResult>( p => 
            //     _console.WriteLine(p.Diagram())
            // )            
            handler: CommandHandler.Create<string, ConnectionType, int, int, int, ParseResult>(
                async (connectionName, connectionType, skip, take, parallel, parse) =>
                await new ExecuteCommands(_connectionManager, _console).LoadFile(parse.CommandResult.GetValueOrDefault<FileInfo[]>(), connectionName, connectionType, skip, take, parallel)
            )
        // handler: CommandHandler.Create<string, ConnectionManager.ConnectionType, int, int, int, FileInfo >( 
        //     async (connectionName, connectionType, skip, take, parallel, queries) => 
        //     await new ExecuteCommands(_settings, _console).LoadFile(queries, connectionName, connectionType, skip, take, parallel)
        // )

        // handler: CommandHandler.Create<FileInfo, string, ConnectionManager.ConnectionType, int, int, int >( 
        //     async (file, connectionName, connectionType, skip, take, parallel) => 
        //     await new ExecuteCommands(_settings, _console).LoadFile(file, connectionName, connectionType, skip, take, parallel)
        // )
        );

        public Command BulkFile() => new Command("bulk", "Execute queries from multiple files",
            new Option[] {
                ConnectionNameOption(),
                QueryExecutorOption(),
                new Option("--parallel", "number of threads to use", new Argument<int>(1))
            },
            argument: new Argument<FileInfo>()
            {
                Name = "bulkFile",
                Description = "File containing queries to execute",
                Arity = ArgumentArity.ExactlyOne
            }.ExistingOnly(),
            // handler: CommandHandler.Create<ParseResult>( p => 
            //     _console.WriteLine(p.Diagram())
            // )            
            handler: CommandHandler.Create<string, ConnectionType, int, ParseResult>(
                async (connectionName, connectionType, parallel, parse) =>
                await new ExecuteCommands(_connectionManager, _console).BulkFile(parse.CommandResult.GetValueOrDefault<FileInfo>(), connectionName, connectionType, parallel)
            )
        // handler: CommandHandler.Create<string, ConnectionManager.ConnectionType, int, int, int, FileInfo >( 
        //     async (connectionName, connectionType, skip, take, parallel, queries) => 
        //     await new ExecuteCommands(_settings, _console).LoadFile(queries, connectionName, connectionType, skip, take, parallel)
        // )

        // handler: CommandHandler.Create<FileInfo, string, ConnectionManager.ConnectionType, int, int, int >( 
        //     async (file, connectionName, connectionType, skip, take, parallel) => 
        //     await new ExecuteCommands(_settings, _console).LoadFile(file, connectionName, connectionType, skip, take, parallel)
        // )
        );


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

        private Argument<string> ConnectionNameArgument() => new Argument<string>()
        {
            Name = "connectionName",
            Arity = ArgumentArity.ExactlyOne
        }
        .FromAmong(_settings?.CosmosDbConnections?.Keys.ToArray() ?? new string[] { });
    }

}