using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kurukuru;
using Mono.Terminal;

namespace Gizmo.Console
{
    public interface IInteractiveConsole : IConsole, IHandleCancel, IReadLine
    {
        void EmptyKeyBuffer();
        void ClearLine();
        void Clear();
        int BufferWidth { get; }
        int BufferHeight { get; }
    }

    public class InteractiveConsole : IInteractiveConsole
    {

        private readonly LineEditor _lineEditor;
        public InteractiveConsole(string appName = "gizmo", int histSize = 100)
        {
            Error = StandardStreamWriter.Create(System.Console.Error);
            Out = StandardStreamWriter.Create(System.Console.Out);

            _lineEditor = new LineEditor(appName, histSize);
        }

        public IStandardStreamWriter Error { get; }

        public bool IsErrorRedirected => System.Console.IsErrorRedirected;

        public IStandardStreamWriter Out { get; }

        public bool IsOutputRedirected => System.Console.IsOutputRedirected;

        public bool IsInputRedirected => System.Console.IsInputRedirected;

        public int BufferWidth => System.Console.BufferWidth;

        public int BufferHeight => System.Console.BufferHeight;

        public event ConsoleCancelEventHandler CancelKeyPress
        {
            add { System.Console.CancelKeyPress += value; }
            remove { System.Console.CancelKeyPress -= value; }
        }

        public string Edit(string prompt, string initial = null) => _lineEditor.Edit(prompt, initial);

        public void EmptyKeyBuffer()
        {
            while (!System.Console.IsInputRedirected && System.Console.KeyAvailable)
            {
                System.Console.ReadKey(false);
            }
        }

        public Task WaitForAnyKey()
        {
            return Task.Run(() =>
            {
                while (!System.Console.IsInputRedirected && !System.Console.KeyAvailable)
                {
                    Thread.Sleep(10);
                }
                //https://stackoverflow.com/questions/3769770/clear-console-buffer
            });
        }

        public void ClearLine()
        {
            int currentLineCursor = System.Console.CursorTop;
            System.Console.SetCursorPosition(0, System.Console.CursorTop);
            System.Console.Write(new string(' ', System.Console.WindowWidth));
            System.Console.SetCursorPosition(0, currentLineCursor);
        }

        public void Clear() => System.Console.Clear();
    }

    public interface IHandleCancel
    {
        Task WaitForAnyKey();

        event ConsoleCancelEventHandler CancelKeyPress;
    }

    public interface IReadLine
    {
        string Edit(string prompt, string initial = null);
    }

    public static class InteractiveConsoleExtensions
    {
        public static void WriteLine(this IConsole console, object value)  => console.Out.WriteLine(value.ToString());

        public static void WriteLine(this IConsole console) => console.Out.WriteLine();
    }
}
