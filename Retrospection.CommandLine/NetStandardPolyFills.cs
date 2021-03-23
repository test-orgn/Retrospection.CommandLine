
using System.ComponentModel;


namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}

namespace Retrospection.CommandLine
{
    internal static class ConsoleEx
    {
        internal static (int Left, int Top) GetCursorPos()
        {
            return (System.Console.CursorLeft, System.Console.CursorTop);
        }
    }
}