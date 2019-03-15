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
using Autofac;
using Microsoft.Extensions.Configuration.Json;

namespace Gizmo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IContainer container = BuildContainer();

            var commands = container.Resolve<CommandDefinitions>();

            var parser = new CommandLineBuilder(commands.Root())
                .AddCommand(commands.Connection())
                .AddCommand(commands.Interactive())
                .AddCommand(commands.Execute())
                .AddCommand(commands.LoadFile())
                .AddCommand(commands.BulkFile())
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
        private static IContainer BuildContainer()
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterType<InteractiveConsole>().As<IInteractiveConsole>().As<IConsole>().SingleInstance();
            containerBuilder.Register(c =>
            {
                var builder = GetConfig();
                var settings = new GizmoConfig();
                builder.Bind(settings);
                return settings;
            }).As<GizmoConfig>();

            containerBuilder.RegisterType<CommandDefinitions>().AsSelf();
            containerBuilder.RegisterType<ConnectionManager>().AsSelf().SingleInstance();

            var container = containerBuilder.Build();
            return container;
        }
        private static IConfigurationRoot GetConfig()
        {
            // System.Console.WriteLine($"Loding ProfileConfig from: {GizmoConfig.ProfileConfigPath}");
            // if (File.Exists(GizmoConfig.ProfileConfigPath))
            // {
            //     System.Console.WriteLine(File.ReadAllText(GizmoConfig.ProfileConfigPath));
            // }

            // System.Console.WriteLine($"Loding LocalConfig from: {GizmoConfig.LocalConfigPath}");
            // if (File.Exists(GizmoConfig.LocalConfigPath))
            // {
            //     System.Console.WriteLine(File.ReadAllText(GizmoConfig.LocalConfigPath));
            // }

            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile(GizmoConfig.ProfileConfigPath, optional: true)
                .AddJsonFile(GizmoConfig.LocalConfigPath, optional: true);

            return builder.Build();
        }
    }
}
