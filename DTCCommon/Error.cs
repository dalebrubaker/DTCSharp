using System;

namespace DTCCommon
{
    /// <summary>
    /// This class is for returning errors
    /// </summary>
    public class Error
    {
        private readonly string _resultText;
        private readonly Exception _exception;
        private readonly string _errorType;
        private readonly object _resultObject;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="resultText">The result description</param>
        /// <param name="exception">optional exception</param>
        /// <param name="errorType">optional error name</param>
        /// <param name="resultObject">optional object</param>
        public Error(string resultText, Exception exception = null, string errorType = null, object resultObject = null)
        {
            _resultText = resultText;
            _exception = exception;
            _errorType = errorType;
            _resultObject = resultObject;
        }

        public override string ToString()
        {
            return $"Result:{_resultText} Exception:{_exception?.Message} ErrorType:{_errorType}";
        }
    }
}
