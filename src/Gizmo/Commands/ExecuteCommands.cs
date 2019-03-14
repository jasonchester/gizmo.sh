using Gizmo.Configuration;
using Gizmo.Console;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Gizmo.Commands
{
    public class ExecuteCommands
    {
        // private readonly AppSettings _settings;
        private readonly IInteractiveConsole _console;
        private readonly ConnectionManager _connectionManager;

        public ExecuteCommands(ConnectionManager connectionManager, IInteractiveConsole console)
        {
            // _settings = settings;
            _connectionManager = connectionManager;
            _console = console;
        }

        public async Task<int> ExecuteQuery(string query, string connectionName, ConnectionType queryExecutor)
        {
            _console.WriteLine($"Execution Query: {query} on {connectionName} using {queryExecutor}");
            var exec = await _connectionManager.Open(connectionName, queryExecutor);
            var results = await exec.ExecuteQuery<dynamic>(query);
            _console.WriteLine(results.Message);
            _console.Dump(results);
            return 0;
        }

        // public async Task<int> LoadFile(FileInfo file, string connectionName, ConnectionType queryExecutor, int skip = 0, int take = 0, int maxThreads = 8)
        // {

        //     _console.WriteLine($"Executing queries from {file} on {connectionName} using {queryExecutor} using {maxThreads} threads.");
        //     if( skip > 0) _console.WriteLine($"skipping {skip} lines.");
        //     if( take > 0) _console.WriteLine($"taking {take} lines.");

        //     return 0;
        // }

        public async Task<int> LoadFile(IEnumerable<FileInfo> files, string connectionName, ConnectionType connectionType, int skip = 0, int take = 0, int maxThreads = 8)
        {
            foreach(var file in files)
            {
                _console.WriteLine($"Executing queries from {file} on {connectionName} using {connectionType} using {maxThreads} threads.");
                
                IEnumerable<string> queries = await File.ReadAllLinesAsync(file.FullName);
                if(skip > 0) {
                    _console.WriteLine($"skipping {skip} lines.");
                    queries = queries.Skip(skip);
                }
                if(take > 0) 
                {
                    _console.WriteLine($"taking {take} lines.");
                    queries = queries.Take(take);
                }
                foreach(var q in queries)
                {
                    await ExecuteQuery(q, connectionName, connectionType);
                }
            }
            return 0;
        }

        public async Task<int> BulkFile(FileInfo bulkFile, string connectionName, ConnectionType connectionType, int maxThreads = 0)
        {
            _console.WriteLine($"Reading bulk file {bulkFile}");

            var bulk = await File.ReadAllLinesAsync(bulkFile.FullName);
            var bulkDir = bulkFile.Directory;

            var queryFiles = bulk.SelectMany(
                (fileName) => bulkDir.GetFiles(fileName)
            );

            return await LoadFile(queryFiles, connectionName, connectionType, maxThreads:maxThreads);
        }
    }

}