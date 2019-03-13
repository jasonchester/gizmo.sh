using Gizmo.Configuration;
using Gizmo.Console;
using Kurukuru;
using System;
using System.Collections;
using System.Collections.Async;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Gizmo.ConnectionManager;

namespace Gizmo.Interactive
{
    public class GremlinConsole
    {
        private readonly IInteractiveConsole _console;
        private readonly ConnectionManager _connection;
        private IQueryExecutor _currentExecutor => _connection.CurrentQueryExecutor;


        const string prompt = "gremlin> ";
        const string startupMsg = @"
         \,,,/
         (o o)
-----oOOo-(3)-oOOo-----";


        private bool working = false;
        private CancellationTokenSource cts;

        private readonly AppSettings _settings;

        public GremlinConsole(AppSettings settings, IInteractiveConsole console)
        {
            _settings = settings;
            _console = console;

            _connection = new ConnectionManager(_settings, _console);
        }

        public async Task DoREPL(string connectionName, ConnectionType connectionType)
        {

            _console.CancelKeyPress += HandleCancelKeyPress;

            bool connected = await TestConnection(connectionName, connectionType);

            if (connected)
            {
                _console.WriteLine(startupMsg);
                _console.WriteLine(_currentExecutor.RemoteMessage);
                _console.WriteLine();

                try
                {
                    await DoREPLLoop();
                }

                catch (Exception ex)
                {
                    _console.WriteLine(ex);
                }
                finally
                {
                    _console.WriteLine("Quitting.");
                    _connection.Dispose();
                }
            }

        }

        private async Task DoREPLLoop()
        {
            string input;
            while ((input = _console.Edit(prompt)).IsNotQuit())
            {
                working = true;

                if (!string.IsNullOrWhiteSpace(input))
                {
                    using (cts = new CancellationTokenSource())
                    {
                        var inputSubstring = input.Truncate(_console.BufferWidth - 3, ellipsis: "...");
                        await Spinner.StartAsync(inputSubstring, async spinner =>
                        {
                            try
                            {
                                IOperationResult result = CommandResult.Empty;
                                switch (input)
                                {
                                    case var command when input.StartsWith(":"):
                                        result = await DoCommand(command, spinner, cts.Token);
                                        break;

                                    case var query when !string.IsNullOrWhiteSpace(query):
                                        result = await _currentExecutor.ExecuteQuery<dynamic>(query, cts.Token);
                                        break;
                                }

                                var message = string.Empty;

                                spinner.Succeed(result.Message);

                                using (var reader = new StringReader(result.Details))
                                {
                                    spinner.Succeed(result.Message);

                                    //we do this to allow a user to ctrl-c break the spooling
                                    //of a lengthy response. 
                                    while (reader.Peek() > 0 && !cts.IsCancellationRequested)
                                    {
                                        _console.WriteLine(reader.ReadLine());
                                    }
                                    //_console.Out.Flush();
                                }
                            }
                            catch (Exception ex)
                            {
                                spinner.Fail(ex.Message);
                            }
                        });
                    }
                }
                else
                {
                    //remove all blank lines
                    // var history = ReadLine.GetHistory().Where(h => !string.IsNullOrWhiteSpace(h));
                    // ReadLine.ClearHistory();
                    // foreach(var h in history) 
                    // {
                    //     ReadLine.AddHistory(h);
                    // }
                }
                working = false;
            }
        }

        private async Task<bool> TestConnection(string connectionName, ConnectionType connectionType)
        {
            bool connected = false;
            using (cts = new CancellationTokenSource())
            {
                await Spinner.StartAsync("Starting Gizmo", async spinner =>
                {
                    spinner.Text = $"Connecting with {connectionType}...";
                    try
                    {
                        await Task.Run(async () =>
                        {
                            await _connection.Open(connectionName, connectionType, cts.Token);
                            spinner.Text = "Testing Connection.";
                            connected = await _currentExecutor.TestConnection(cts.Token);
                        }, cts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        //intentionally blank.
                    }
                });
            }

            return connected;
        }

        ///<summary>
        ///<param name="command"> Just the command the user passed in </param>
        ///<param name="spinner"> Used to display a progress icon on the left </param>
        ///<param name="ct">Cancellation token to kill off threads if necessary </param>
        ///Processes user input for anything that's not a direct Gremlin query
        ///</summary>
        private async Task<IOperationResult> DoCommand(string command, Spinner spinner = null, CancellationToken ct = default)
        {
            var args = command.Split(" ");

            switch (args[0])
            {
                case ":help":
                case ":h":
                    var sb = new StringBuilder();
                    sb.AppendLine(":help\t:h\tdisplay help");
                    sb.AppendLine(":quit\t:q\tquit");
                    sb.AppendLine(":clear\t:c\tclear console");
                    sb.AppendLine(":mode\t:m\tswitch query execution mode");
                    sb.AppendLine(":bulk\t:b\tUses a file containing a list of other files for uploads.");
                    sb.AppendLine(":load\t:l\tLoads a file containing Gremlin queries and uploads them.");
                    return new CommandResult(sb.ToString());
                case ":clear":
                case ":c":
                    _console.Clear();
                    return new CommandResult("cleared");
                case ":mode":
                case ":m":
                    return new CommandResult((await _connection.SwitchConnectionType(ct)).RemoteMessage);
                case ":l":
                case ":load":
                    return new CommandResult(await ProcessFile(spinner, args, ct));
                case ":b":
                case ":bulk":
                    return new CommandResult(await ProcessBulkFile(spinner, args, ct));
                default:
                    return new CommandResult($"{command} was not found.");
            }
        }

        ///<summary>
        ///Loads in a file containing the names of any other numeber of filenames in the same directory (or subdirectory) and loads them in to Gremlin
        ///Args should be as follows: :b filename numThreads
        ///</summary>
        private async Task<string> ProcessBulkFile(Spinner spinner, string[] args, CancellationToken ct = default)
        {
            if (!File.Exists(args[1]))
            {
                throw new ArgumentException("File not found");
            }

            IEnumerable<String> filesToLoad = File.ReadLines(args[1]);

            ArrayList results = new ArrayList();
            foreach (String fileName in filesToLoad)
            {
                try
                {
                    String regex = "([^/]+$)";

                    String[] newArgs = new String[5];
                    String newPath = Regex.Replace(args[1], regex, fileName).Trim();
                    if (File.Exists(newPath))
                    {
                        ConsoleWrite($"Starting to process {newPath}");
                        newArgs[0] = ""; //This would usually be :l or :load
                        newArgs[1] = newPath; //The new path for the file from the regex
                        newArgs[2] = "0"; //Start at the first line
                        newArgs[3] = File.ReadLines(newPath).Count().ToString(); //Run through the whole file
                        newArgs[4] = args.Length == 3 ? args.Last() : "8"; //If no specific number of threads requested, default to 8

                        results.Add(await ProcessFile(spinner, newArgs, ct));
                    }
                    else
                    {
                        results.Add($"The path of {newPath} is not a valid filepath");
                    }
                }
                catch (Exception)
                {
                    //Eat the error for now
                }
            }

            string r = "";
            foreach (String result in results)
            {
                r += (result + "\n");
            }
            return r;
        }

        /// <summary>
        /// <param name="spinner"> Displays a little animation on the far left of the output</param>
        /// <param name="args"> filename startOffset endOffset numberOfThreads </param>
        /// <param name="ct"> A CancellationToken to inform all threads to kill themselves if necessary </param>
        /// </summary> 
        private async Task<string> ProcessFile(Spinner spinner, string[] args, CancellationToken ct = default)
        {

            long globalCount = 0;
            var timer = new Stopwatch();
            if (!File.Exists(args[1]))
            {
                throw new ArgumentException("File Not Found");
            }
            timer.Start();

            var queries = File.ReadLines(args[1]).AsQueryable();

            int skip = 0;
            if (args.Length > 2 && int.TryParse(args[2], out skip))
            {
                queries = queries.Skip(skip);
            }

            int take = 0;
            if (args.Length > 3 && int.TryParse(args[3], out take))
            {
                queries = queries.Take(take);
            }

            int dop = 1;
            if (args.Length > 4) int.TryParse(args[4], out dop);

            var lines = queries.ToList();
            int totalQueries = lines.Count();

            var failedQueue = new ConcurrentQueue<String>();

            await lines.ParallelForEachAsync(async rawline =>
                {
                    string line;
                    if (rawline.StartsWith(":>"))
                    {
                        line = rawline.Substring(2).Trim();
                    }
                    else
                    {
                        line = rawline;
                    }
                    var count = Interlocked.Increment(ref globalCount);
                    var output2 = $"{count + skip,6}: {line}";
                    spinner.Text = output2.Truncate(_console.BufferWidth - 3, "...");
                    var result = await _currentExecutor.ExecuteQuery<dynamic>(line, ct);
                    using (var reader = new StringReader(result.ToString()))
                    {
                        var message = reader.ReadLine();
                        var resultRegex = new Regex(@"(\d+)( characters)");
                        var matches = resultRegex.Match(message);
                        var r = -1;
                        if (Int32.TryParse(matches.Groups[1].Value, out r))
                        {
                            if (r == 0)
                            {
                                failedQueue.Enqueue(message);
                            }
                        }
                        var output = $"{count + skip,8}: [{(double)count / totalQueries * 100:000.0}%] {((double)count) / timer.Elapsed.TotalSeconds:F2} q/s: {message}";
                        ConsoleWrite(output);
                    }
                },
                maxDegreeOfParalellism: dop,
                cancellationToken: ct
            );

            File.WriteAllLines("./failures.txt", failedQueue);

            string resultMessage = $"{globalCount}:[{skip} to {skip + take}] q's. {dop} threads. {timer.Elapsed} {((double)globalCount) / timer.Elapsed.TotalSeconds:F2} q/s";
            return resultMessage;
        }

        private void ConsoleWrite(string output)
        {
            _console.ClearLine();

            _console.WriteLine(output.Truncate(_console.BufferWidth - 1, "..."));
        }

        private void HandleCancelKeyPress(object s, ConsoleCancelEventArgs e)
        {
            _console.WriteLine("Cancel Pressed.");
            if (working && cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
                e.Cancel = true;
                _console.WriteLine("Cancelling Tasks.");
            }
        }
    }
}
