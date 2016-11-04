namespace DTCCommon
{
    /// <summary>
    /// This class is for returning errors
    /// </summary>
    public class Error
    {
        /// <summary>
        /// Optional arbitrary message
        /// </summary>
        public string ErrorType { get; }

        public string ResultText { get; }

        /// <summary>
        /// Optional arbitrary object
        /// </summary>
        public object ResultObject { get; }

        public Error(string errorType, string resultText, object resultObject)
        {
            ErrorType = errorType;
            ResultText = resultText;
            ResultObject = resultObject;
        }
    }
}
