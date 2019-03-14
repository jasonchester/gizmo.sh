using FluentAssertions;
using Gizmo.Commands;
using Gizmo.Configuration;
using Gizmo.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;
using Xunit;

namespace Gizmo.Test
{
    public class CommandDefinitionTest
    {
        private readonly CommandDefinitions definitions;

        public CommandDefinitionTest()
        {
            var console = new InteractiveConsole();
            var settings = new AppSettings();
            var connectionManager = new ConnectionManager(settings, console);
            definitions = new CommandDefinitions(settings, console, connectionManager);
        }

        [Fact]
        public void Root_returns_a_RootCommand()
        {
            definitions.Root().Should().BeOfType<RootCommand>();
        }

        [Fact]
        public void Connection_returns_a_Command()
        {
            definitions.Connection().Should().BeOfType<Command>();
        }

        [Fact]
        public void Connection_returns_has_a_List_SubCommand()
        {
            definitions.Connection().Children.Should()
                .Contain(c => c.Name == "list" && c is ICommand);
        }

        [Fact]
        public void Connection_returns_has_a_Show_SubCommand()
        {
            definitions.Connection().Children.Should().Contain(c => c.Name == "show" && c is ICommand);
        }

        [Fact]
        public void Connection_returns_has_a_Remove_SubCommand()
        {
            definitions.Connection().Children.Should().Contain(c => c.Name == "remove" && c is ICommand);
        }

        [Fact]
        public void Connection_returns_has_a_Add_SubCommand()
        {
            definitions.Connection().Children.Should().Contain(c => c.Name == "add" && c is ICommand);
        }

        [Fact]
        public void Interactive_returns_a_Command()
        {
            definitions.Interactive().Should().BeOfType<Command>();
        }

        [Fact]
        public void Execute_returns_a_Command()
        {
            definitions.Execute().Should().BeOfType<Command>();
        }
    }
}
