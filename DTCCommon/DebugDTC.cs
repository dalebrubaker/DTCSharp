using System.Diagnostics;
using NLog;

namespace DTCCommon
{
    public static class DebugDTC
    {
        private static readonly ILogger s_logger = LogManager.GetCurrentClassLogger();

        public static void Assert(bool condition, string message = null)
        {
#if DEBUG
            if (condition)
            {
                return;
            }
            if (string.IsNullOrEmpty(message))
            {
                message = $"Failed {nameof(DebugDTC)}.{nameof(Assert)}";
            }
            Debug.Assert(condition, message); // do the normal assert, launch debugger when not in the IDE
            throw new DTCSharpException(message);
#endif
        }
    }
}