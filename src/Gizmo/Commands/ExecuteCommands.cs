using Gizmo.Configuration;
using Gizmo.Console;
using System.Threading.Tasks;
using System.IO;

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

        public async Task<int> LoadFile(FileInfo file, string connectionName, ConnectionManager.ConnectionType queryExecutor, int skip = 0, int take = 0, int maxThreads = 8)
        {

            _console.WriteLine($"Executing queries from {file} on {connectionName} using {queryExecutor} using {maxThreads} threads.");
            if( skip > 0) _console.WriteLine($"skipping {skip} lines.");
            if( take > 0) _console.WriteLine($"taking {take} lines.");

            return 0;
        }

        public async Task<int> LoadFile(FileInfo[] files, string connectionName, ConnectionManager.ConnectionType queryExecutor, int skip = 0, int take = 0, int maxThreads = 8)
        {
            foreach(var file in files)
            {
                _console.WriteLine($"Executing queries from {file} on {connectionName} using {queryExecutor} using {maxThreads} threads.");
            }
            if( skip > 0) _console.WriteLine($"skipping {skip} lines.");
            if( take > 0) _console.WriteLine($"taking {take} lines.");

            return 0;
        }
    }

}