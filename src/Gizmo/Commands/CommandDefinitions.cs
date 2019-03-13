using Gizmo.Configuration;
using Gizmo.Console;
using Gizmo.Interactive;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Linq;
using static Gizmo.ConnectionManager;

namespace Gizmo.Commands
{
    public class CommandDefinitions
    {
        private readonly AppSettings _settings;
        private readonly IInteractiveConsole _console;
        private readonly ConnectionCommands _connectionCommands;

        public CommandDefinitions(AppSettings settings, IInteractiveConsole console)
        {
            _settings = settings;
            _console = console;
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
                    //symbols: new[] { new Option("", argument: ConnectionNamesArgument()) },
                    argument: ConnectionNameArgument(),
                    handler: CommandHandler.Create<string>(_connectionCommands.ShowConnections)
                );

            Command Remove() => new Command("remove", "Remove connection(s)",
                    //symbols: new[] { new Option("", argument: ConnectionNamesArgument()) }, 
                    argument: ConnectionNameArgument(),
                    handler: CommandHandler.Create<string>(_connectionCommands.RemoveConnection)
                );

            Command Add() => new Command("add", "Add a Connection",
                    new Option[] { },
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
            argument: new Argument<string>() { Name = "query", Description = "The query to execute" },
            handler: CommandHandler.Create<string, string, ConnectionType>(
                async (query, connectionName, queryExecutor) =>
                await new ExecuteCommands(_settings, _console).ExecuteQuery(query, connectionName, queryExecutor))
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