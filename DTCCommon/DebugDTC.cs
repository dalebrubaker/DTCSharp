using System.Diagnostics;

namespace DTCCommon
{
    public static class DebugDTC
    {
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