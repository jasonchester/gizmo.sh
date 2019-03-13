using Gizmo.Configuration;
using Gizmo.Console;
using System.Threading.Tasks;

namespace Gizmo.Commands
{
    public class ExecuteCommands
    {
        private readonly AppSettings _settings;
        private readonly IInteractiveConsole _console;

        public ExecuteCommands(AppSettings settings, IInteractiveConsole console)
        {
            _settings = settings;
            _console = console;
        }

        public async Task<int> ExecuteQuery(string query, string connectionName, ConnectionManager.ConnectionType queryExecutor)
        {
            _console.WriteLine($"Execution Query: {query} on {connectionName} using {queryExecutor}");
            return 0;
        }
    }

}