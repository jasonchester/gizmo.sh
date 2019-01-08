using System;
using System.Collections;
using System.Collections.Async;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Kurukuru;
using Microsoft.Extensions.Configuration;

namespace Brandmuscle.LocationData.Graph.GremlinConsole
{
    class Program
    {
        const string prompt = "gremlin> ";
        const string startupMsg = @"
         \,,,/
         (o o)
-----oOOo-(3)-oOOo-----";

        private static IQueryExecutor currentExecutor;

        private static IConfigurationRoot _builder;

        private static bool working = false;

        private static IConfigurationRoot GetConfig()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Program>()
                .Build();
        }

        private static Task GetKeypress()
        {
            return Task.Run(() =>
            {
                while (!Console.KeyAvailable)
                {
                    Thread.Sleep(10);
                }
                //https://stackoverflow.com/questions/3769770/clear-console-buffer
            });
        }
        private static CancellationTokenSource cts;
        private static void HandleCancelKeyPress(object s, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Cancel Pressed.");
            if (working && cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
                e.Cancel = true;
                Console.WriteLine("Cancelling Tasks.");
            }
        }

        static async Task Main(string[] args)
        {
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;

            bool connected = false;

            _builder = GetConfig();
            Console.CancelKeyPress += HandleCancelKeyPress;

            using (cts = new CancellationTokenSource())
            {
                await Spinner.StartAsync("Press a key or attatch debugger.", async spinner =>
                {
                    await Task.WhenAny(
                        Task.Delay(5000, cts.Token),
                        GetKeypress()
                    );
                    spinner.Text = $"Connecting with {nameof(AzureGraphsExecutor)}...";

                    try
                    {
                        await Task.Run(async () =>
                        {
                            currentExecutor = await AzureGraphsExecutor.GetExecutor(_builder, cts.Token);
                            spinner.Text = "Testing Connection.";
                            connected = await currentExecutor.TestConnection(cts.Token);
                        }, cts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        //intentionally blank.
                        Console.WriteLine("Task Cancelled 1...");
                    }
                });
            }


            if (connected)
            {
                Console.WriteLine(startupMsg);
                Console.WriteLine(currentExecutor.RemoteMessage);
                Console.WriteLine();

                while (Console.KeyAvailable)
                {
                    Console.ReadKey(false);
                }

                try
                {
                    await DoREPL();
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    Console.WriteLine("Quitting.");
                    currentExecutor.Dispose();
                }
            }
        }

        private static async Task DoREPL()
        {
            ReadLine.HistoryEnabled = true;

            string input;
            while ((input = ReadLine.Read(prompt)) != ":q")
            {
                working = true;

                if (!string.IsNullOrWhiteSpace(input))
                {
                    using (cts = new CancellationTokenSource())
                    {
                        var inputSubstring = input.Substring(0, Math.Min(Console.BufferWidth - 2, input.Length));//.PadRight(Console.BufferWidth - 2, ' ');
                        await Spinner.StartAsync(inputSubstring, async spinner =>
                        {
                            try
                            {
                                string result = "";
                                switch (input)
                                {
                                    case var command when input.StartsWith(":"):
                                        result = await DoCommand(command, spinner, cts.Token);
                                        break;

                                    case var query when !string.IsNullOrWhiteSpace(query):
                                        result = await currentExecutor.ExecuteQuery(query, cts.Token);
                                        break;
                                }

                                var message = string.Empty;

                                using (var reader = new StringReader(result))
                                {
                                    message = reader.ReadLine();

                                    spinner.Succeed(message);

                                    while (reader.Peek() > 0 && !cts.IsCancellationRequested)
                                    {
                                        Console.WriteLine(reader.ReadLine());
                                    }
                                    // Console.WriteLine(reader.ReadToEnd());
                                    Console.Out.Flush();
                                }
                            }
                            catch (Exception ex)
                            {
                                spinner.Fail(ex.Message);
                            }
                        });
                    }
                }
                working = false;
            }
        }

        ///<summary>
        ///<param name="command"> Just the command the user passed in </param>
        ///<param name="spinner"> Used to display a progress icon on the left </param>
        ///<param name="ct">Cancellation token to kill off threads if necessary </param>
        ///Processes user input for anything that's not a direct Gremlin query
        ///</summary>
        private static async Task<string> DoCommand(string command, Spinner spinner = null, CancellationToken ct = default(CancellationToken))
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
                    return sb.ToString();
                case ":clear":
                case ":c":
                    Console.Clear();
                    return "cleared";
                case ":mode":
                case ":m":
                    return await SwitchQueryExecutor(ct);
                case ":l":
                case ":load":
                    string result = await ProcessFile(spinner, args, ct); return result;
                case ":b":
                case ":bulk":
                    return await ProcessBulkFile(spinner, args, ct);
                default:
                    return $"{command} was not found.";
            }
        }

        ///<summary>
        ///Loads in a file containing the names of any other numeber of filenames in the same directory (or subdirectory) and loads them in to Gremlin
        ///Args should be as follows: :b filename numThreads
        ///</summary>
        private static async Task<string> ProcessBulkFile(Spinner spinner, string[] args, CancellationToken ct = default(CancellationToken))
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
        private static async Task<string> ProcessFile(Spinner spinner, string[] args, CancellationToken ct = default(CancellationToken))
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
            int.TryParse(args[4], out dop);

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
                    spinner.Text = output2.Substring(0, Math.Min(Console.BufferWidth, output2.Length) - 2).PadRight(Console.BufferWidth - 2, ' ');
                    var result = await currentExecutor.ExecuteQuery(line, ct);
                    using (var reader = new StringReader(result))
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

        private static void ConsoleWrite(string output)
        {
            Console.CursorLeft = 0;
            Console.WriteLine(output.Substring(0, Math.Min(Console.BufferWidth, output.Length)).PadRight(Console.BufferWidth, ' '));
        }

        private static async Task<string> SwitchQueryExecutor(CancellationToken ct = default(CancellationToken))
        {
            IQueryExecutor newExec = null;

            AppSettings settings = new AppSettings();

            _builder.Bind(settings);

            try
            {
                switch (currentExecutor)
                {
                    case GremlinExecutor e:
                        newExec = await AzureGraphsExecutor.GetExecutor(settings.CosmosDbConnections["default"], ct);
                        break;
                    case AzureGraphsExecutor e:
                        newExec = new GremlinExecutor(_builder);
                        break;
                }
            }
            catch (Exception ex)
            {
                return $"failed to switch. {ex.Message}";
            }
            if (await newExec.TestConnection(ct))
            {
                currentExecutor.Dispose();
                currentExecutor = newExec;
                return currentExecutor.RemoteMessage;
            }
            throw new ApplicationException("Failed to switch executors.");
        }
    }
}
