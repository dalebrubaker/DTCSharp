using System;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace DTCCommon;

[Serializable]
public class DTCSharpException : Exception
{
    public DTCSharpException()
    {
    }

    public DTCSharpException(string message) : base(message)
    {
    }

    public DTCSharpException(string message, Exception inner) : base(message, inner)
    {
    }

    // Ensure Exception is Serializable
    protected DTCSharpException(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt)
    {
    }
}