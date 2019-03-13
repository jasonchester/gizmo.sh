using Newtonsoft.Json;
using System.CommandLine;

namespace Gizmo.Console
{
    public static class ConsoleExtensions
    {
        public static void WriteLine(this IConsole console, object value)  => console.Out.WriteLine(value.ToString());

        public static void WriteLine(this IConsole console) => console.Out.WriteLine();

        public static void Dump(this IConsole console, object value) => console.Out.WriteLine(JsonConvert.SerializeObject(value, Formatting.Indented));
    }
}
