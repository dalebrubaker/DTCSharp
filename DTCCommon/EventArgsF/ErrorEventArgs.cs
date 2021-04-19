using System;

namespace DTCCommon.EventArgsF
{
    public class ErrorEventArgs : EventArgs
    {
        public Exception Exception { get; set; }

        public string Message { get; set; }

        public ErrorEventArgs(Exception ex, string message)
        {
            Exception = ex;
            Message = message;
        }
    }
}