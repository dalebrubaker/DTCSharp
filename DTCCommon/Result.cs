using System;

namespace DTCCommon;

/// <summary>
///     This class is for returning errors
/// </summary>
public class Result
{
    public Result()
    {
        IsError = false;
    }

    /// <summary>
    ///     ctor
    /// </summary>
    /// <param name="resultText">The result description</param>
    /// <param name="errorType">optional error name</param>
    /// <param name="exception">optional exception</param>
    /// <param name="resultObject">optional object</param>
    public Result(string resultText, string errorType = null, Exception exception = null, object resultObject = null) : this()
    {
        ResultText = resultText;
        Exception1 = exception;
        ErrorType = errorType;
        ResultObject = resultObject;
        IsError = true;
    }

    public string ResultText { get; }
    public Exception Exception1 { get; }
    public string ErrorType { get; }
    public object ResultObject { get; }

    /// <summary>
    ///     <c>true</c> with an empty ctor, of <c>false</c> if there is a ResultText etc.
    /// </summary>
    public bool IsError { get; }

    public override string ToString()
    {
        if (!IsError)
        {
            return "Result: No Error";
        }
        var str = $"Result:{ResultText}";
        if (Exception1 != null)
        {
            str += " Exception:{Exception1?.Message}";
        }
        if (!string.IsNullOrEmpty(ErrorType))
        {
            str += $" ErrorType:{ErrorType}";
        }
        if (ResultObject != null)
        {
            str += $" {ResultObject}";
        }
        return str;
    }
}