using Gizmo.Configuration;
using Gizmo.Console;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Async;
using System.Threading;
using System.Collections.Concurrent;

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
        

        public async Task<int> LoadFile(
            IEnumerable<FileInfo> files, 
            string connectionName, 
            ConnectionType connectionType, 
            int skip = 0, 
            int take = 0, 
            int maxThreads = 8, 
            CancellationToken ct = default)
        {
            var results = new ConcurrentQueue<QueryResultSet<dynamic>>();
            var exec = await _connectionManager.Open(connectionName, connectionType, ct);

            if(!await exec.TestConnection(ct))
            {
                _console.WriteLine($"Unable to connect.");
                return 1;
            }

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

                var process = queries.ParallelForEachAsync(
                    async q => {
                        results.Enqueue(await exec.ExecuteQuery<dynamic>(q, ct));
                    },
                    maxDegreeOfParalellism: maxThreads,
                    cancellationToken: ct
                );

                var report = Task.Run(async () => {
                    while(!process.IsCompleted)
                    {
                        if(results.TryDequeue(out var result))
                        {
                            _console.WriteLine(result.Message);
                        }
                        await Task.Delay(1000);
                    }
                });

                await Task.WhenAll(report, process);
            }
            return 0;


            // var failedQueue = new ConcurrentQueue<String>();

            // await lines.ParallelForEachAsync(async rawline =>
            //     {
            //         string line;
            //         if (rawline.StartsWith(":>"))
            //         {
            //             line = rawline.Substring(2).Trim();
            //         }
            //         else
            //         {
            //             line = rawline;
            //         }
            //         var count = Interlocked.Increment(ref globalCount);
            //         var output2 = $"{count + skip,6}: {line}";
            //         spinner.Text = output2.Truncate(_console.BufferWidth - 3, "...");
            //         var result = await _currentExecutor.ExecuteQuery<dynamic>(line, ct);
            //         using (var reader = new StringReader(result.ToString()))
            //         {
            //             var message = reader.ReadLine();
            //             var resultRegex = new Regex(@"(\d+)( characters)");
            //             var matches = resultRegex.Match(message);
            //             var r = -1;
            //             if (Int32.TryParse(matches.Groups[1].Value, out r))
            //             {
            //                 if (r == 0)
            //                 {
            //                     failedQueue.Enqueue(message);
            //                 }
            //             }
            //             var output = $"{count + skip,8}: [{(double)count / totalQueries * 100:000.0}%] {((double)count) / timer.Elapsed.TotalSeconds:F2} q/s: {message}";
            //             ConsoleWrite(output);
            //         }
            //     },
            //     maxDegreeOfParalellism: dop,
            //     cancellationToken: ct
            // );

            // File.WriteAllLines("./failures.txt", failedQueue);

            // string resultMessage = $"{globalCount}:[{skip} to {skip + take}] q's. {dop} threads. {timer.Elapsed} {((double)globalCount) / timer.Elapsed.TotalSeconds:F2} q/s";
            // return resultMessage;
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