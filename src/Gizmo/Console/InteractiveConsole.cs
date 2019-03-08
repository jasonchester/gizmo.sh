using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Mono.Terminal;

namespace Gizmo.Console
{
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
            });
        }

        public void ClearLine()
        {
            //https://stackoverflow.com/questions/3769770/clear-console-buffer
            int currentLineCursor = System.Console.CursorTop;
            System.Console.SetCursorPosition(0, System.Console.CursorTop);
            System.Console.Write(new string(' ', System.Console.WindowWidth));
            System.Console.SetCursorPosition(0, currentLineCursor);
        }

        public void Clear() => System.Console.Clear();
    }
}
