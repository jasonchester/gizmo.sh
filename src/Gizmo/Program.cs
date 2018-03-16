﻿using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gremlin.Net;
using Gremlin.Net.Driver;
using Kurukuru;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Graphs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder;
using Newtonsoft.Json;

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

        static async Task Main(string[] args)
        {

            CancellationTokenSource cts = new CancellationTokenSource();

            Console.CancelKeyPress += (s,e) => {
                cts.Cancel();
                e.Cancel = true;
            };

            // Console.TreatControlCAsInput = true;

            // Console.CancelKeyPress += (s,e) => {
            //     if(!cts.IsCancellationRequested) {
            //         cts.Cancel();
            //         e.Cancel = true;
            //         //press ctrl-c again to cancel...
            //     } 
            //     else {
            //         Console.WriteLine("Quitting.");
            //     }
            // };

            _builder = GetConfig();

            bool connected = false;

            //temp = new GremlinExecutor(GremlinExecutor.GetGremlinServer(_builder));
            await Spinner.StartAsync("Press a key or attatch debugger.", async spinner =>
            {
                await Task.WhenAny(
                    Task.Delay(5000, cts.Token),
                    GetKeypress()
                );
                spinner.Text = $"Connecting with {nameof(AzureGraphsExecutor)}...";
                await Task.Run(async () =>
                {
                    currentExecutor = await AzureGraphsExecutor.GetExecutor(_builder, cts.Token);
                    spinner.Text = "Testing Connection.";
                    connected = await currentExecutor.TestConnection(cts.Token);
                });
            });

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
                    await DoREPL(cts.Token);
                }
                catch(TaskCanceledException)
                {
                    //intentionally blank.                
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    Console.WriteLine("Quitting...");
                    currentExecutor.Dispose();
                }
            }
        }

        private static async Task DoREPL(CancellationToken ct = default(CancellationToken))
        {
            ReadLine.HistoryEnabled = true;

            string input;
            while ((input = ReadLine.Read(prompt)) != ":q")
            {
                if (!string.IsNullOrWhiteSpace(input))
                {
                    var inputSubstring = input.Substring(0, Math.Min(Console.BufferWidth - 2, input.Length)).PadRight(Console.BufferWidth - 2, ' ');
                    await Spinner.StartAsync(inputSubstring, async spinner =>
                    {
                        try
                        {
                            string result = "";
                            switch (input)
                            {
                                case var command when input.StartsWith(":"):
                                    result = await DoCommand(command, spinner, ct);
                                    break;

                                case var query when !string.IsNullOrWhiteSpace(query):
                                    result = await currentExecutor.ExecuteQuery(query, ct);
                                    break;
                            }
                            using (var reader = new StringReader(result))
                            {
                                var message = reader.ReadLine();

                                while (reader.Peek() > 0)
                                {
                                    ConsoleWrite(reader.ReadLine());
                                }
                                spinner.Succeed(message);
                            }
                        }
                        catch (Exception ex)
                        {
                            spinner.Fail(ex.Message);
                        }
                    });
                }
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

                default:
                    return $"{command} was not found.";
            }
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
                     var output = $"{count + skip,8}: [{(double)count / totalQueries * 100:000.0}%] {((double)count) / timer.Elapsed.TotalSeconds:F2} q/s: {message}";
                     ConsoleWrite(output);
                 }
             },
            maxDegreeOfParalellism: dop,
            cancellationToken: ct
            );

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
            try
            {
                switch (currentExecutor)
                {
                    case GremlinExecutor e:
                        newExec = await AzureGraphsExecutor.GetExecutor(_builder, ct);
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
