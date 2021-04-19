using System;

namespace DTCCommon
{
    /// <summary>
    /// This class is for returning errors
    /// </summary>
    public class Error
    {
        public string ResultText { get; }
        public Exception Exception1 { get; }
        public string ErrorType { get; }
        public object ResultObject { get; }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="resultText">The result description</param>
        /// <param name="exception">optional exception</param>
        /// <param name="errorType">optional error name</param>
        /// <param name="resultObject">optional object</param>
        public Error(string resultText, Exception exception = null, string errorType = null, object resultObject = null)
        {
            ResultText = resultText;
            Exception1 = exception;
            ErrorType = errorType;
            ResultObject = resultObject;
        }

        public override string ToString()
        {
            return $"Result:{ResultText} Exception:{Exception1?.Message} ErrorType:{ErrorType}";
        }
    }
}