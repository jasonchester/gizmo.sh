using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;
using System.Threading.Tasks;
using Kurukuru;

namespace Gizmo.Console
{
    public interface IInteractiveConsole : IConsole
    {
        void EmptyKeyBuffer();
        void ClearLine();
        void Clear();
        int BufferWidth { get; }
        int BufferHeight { get; }

        event ConsoleCancelEventHandler CancelKeyPress;

        Task WaitForAnyKey();

        string Edit(string prompt, string initial = null);

    }
}
