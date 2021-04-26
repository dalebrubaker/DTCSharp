using DTCCommon.Exceptions;
using NLog;

namespace DTCCommon
{
    public static class MyDebug
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
                message = $"Failed {nameof(MyDebug)}.{nameof(Assert)}";
            }
            throw new DTCSharpException(message);
#endif
        }
    }
}