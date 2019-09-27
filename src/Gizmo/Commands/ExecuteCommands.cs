using Gizmo.Console;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Dasync.Collections;
using System.Threading;
using System.Collections.Concurrent;
using Gizmo.Connection;
using System.Diagnostics;
using System;

namespace Gizmo.Commands
{
    public class ExecuteCommands
    {
        private readonly IInteractiveConsole _console;
        private readonly ConnectionManager _connectionManager;

        public ExecuteCommands(ConnectionManager connectionManager, IInteractiveConsole console)
        {
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

        public async Task<int> LoadFile(
            IEnumerable<FileInfo> files,
            string connectionName,
            ConnectionType connectionType,
            int skip = 0,
            int take = 0,
            int maxThreads = 8,
            CancellationToken ct = default)
        {
            using (var results = new BlockingCollection<IOperationResult>(new ConcurrentQueue<IOperationResult>()))
            {
                var bulkTimer =  Stopwatch.StartNew();
                var exec = await _connectionManager.Open(connectionName, connectionType, ct);

                if (!await exec.TestConnection(ct))
                {
                    _console.WriteLine($"Unable to connect.");
                    return 1;
                }

                var loadTasks = new List<Task>();

                var reporter = Task.Run(() =>
                {
                    while (results.TryTake(out var result, -1))
                    {
                        _console.WriteLine(result.Message);
                    }
                });

                foreach (var file in files)
                {
                    var fileTimer =  Stopwatch.StartNew();

                    _console.WriteLine($"Executing queries from {file} on {connectionName} using {connectionType} using {maxThreads} threads.");

                    IEnumerable<string> queries = await File.ReadAllLinesAsync(file.FullName);
                    
                    if (skip > 0)
                    {
                        _console.WriteLine($"skipping {skip} lines.");
                        queries = queries.Skip(skip);
                    }
                    if (take > 0)
                    {
                        _console.WriteLine($"taking {take} lines.");
                        queries = queries.Take(take);
                    }

                    // var lineNumber = skip + 1;
                    var queriesProcessed = 0;
                    var totalQueries = queries.Count();

                    // Parallel.ForEach(queries, async (queries,loop, i) => )

                    // this looks like it might be better 
                    //https://gist.github.com/0xced/94f6c50d620e582e19913742dbd76eb6


                    IProgress<IOperationResult> progress = new Progress<IOperationResult>( 
                        op => {
                            queriesProcessed++;
                            _console.WriteLine(op.Message);
                        }
                    );

                    // Parallel.ForEach(Partitioner.Create(queries), () => 0, async (q, loopState, partitionCount) => 
                    // {
                    //     partitionCount ++;
                    //     var result = new BulkResult<dynamic>(
                    //         await exec.ExecuteQuery<dynamic>(q, ct), 
                    //         Thread.CurrentThread.ManagedThreadId, file.Name,  skip, queriesProcessed, totalQueries, fileTimer.Elapsed, bulkTimer.Elapsed);
                    //     return partitionCount;
                    // }, 
                    // (partitionCount) => Interlocked.Add(ref queriesProcessed, partitionCount) 
                    // );
                    
                    
                    // await queries.ForEachAsync(maxThreads, async (q) => {
                    //     var result = new BulkResult<dynamic>(
                    //         await exec.ExecuteQuery<dynamic>(q, ct), 
                    //         Thread.CurrentThread.ManagedThreadId, file.Name, 0 + skip, queriesProcessed, totalQueries, fileTimer.Elapsed, bulkTimer.Elapsed);
                    //     //results.Add(result);
                    //     return result;
                    // }, progress);

                    // Partitioner.Create(queries).GetPartitions(maxThreads)
                    //     .Select( 

                    await queries.ParallelForEachAsync(
                        async (q, queryNumber) =>
                        {
                            Interlocked.Increment(ref queriesProcessed);

                            IOperationResult result;

                            try
                            {
                                var qResult = await exec.ExecuteQuery<dynamic>(q, ct);

                                result = new BulkResult<dynamic>(
                                    qResult,
                                    Thread.CurrentThread.ManagedThreadId, 
                                    file.Name, 
                                    queryNumber + skip, 
                                    queriesProcessed, 
                                    totalQueries, 
                                    fileTimer.Elapsed, 
                                    bulkTimer.Elapsed);
                            }
                            catch (System.Exception ex)
                            {
                                result = new ErrorResult(ex);
                            }

                            results.Add(result);
                        }, maxDegreeOfParallelism: maxThreads,
                        cancellationToken: ct
                    );
                }

                results.CompleteAdding();

                await reporter;
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

            return await LoadFile(queryFiles, connectionName, connectionType, maxThreads: maxThreads);
        }
    }

}