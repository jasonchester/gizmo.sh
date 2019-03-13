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
using Mono.Terminal;
using Gizmo.Commands;
using Gizmo.Configuration;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine;
using System.CommandLine.Rendering;
using Gizmo.Console;

namespace Gizmo
{
    class Program
    {

        private static IInteractiveConsole _console;

        static async Task Main(string[] args)
        {
            var builder = GetConfig();
            var settings = new AppSettings();
            builder.Bind(settings);

            _console = new InteractiveConsole();

            var commands = new CommandDefinitions(settings, _console);

            var parser = new CommandLineBuilder(commands.Root())
                .AddCommand(commands.Connection())
                .AddCommand(commands.Interactive())
                .AddCommand(commands.Execute())
                //.UseDefaults()

                // middleware
                .UseVersionOption()
                .UseParseDirective()
                .UseDebugDirective()
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseParseErrorReporting()
                .UseExceptionHandler()
                .UseTypoCorrections()
                .UseHelp()

                .UseAnsiTerminalWhenAvailable()

                .Build();


            var parsedArgs = parser.Parse(args);
            await parser.InvokeAsync(parsedArgs);

        }

        private static IConfigurationRoot GetConfig()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Program>()
                // .AddCommandLine(args)
                .Build();
        }
    }
}
