using System;
using DTCPB;
using Google.Protobuf;

namespace DTCCommon;

public class MessageProto : IEquatable<MessageProto>
{
    public MessageProto(DTCSharpMessageType messageType, IMessage message)
    {
        MessageTypeExtended = messageType;
        Message = message;
        IsExtended = true;
    }

    public MessageProto(DTCMessageType messageType, IMessage message)
    {
        MessageType = messageType;
        Message = message;
    }

    public bool IsExtended { get; } // using MessageTypeExtended instead of MessageType

    /// <summary>
    ///     Pairs messageType with the corresponding Protobuf IMessage
    /// </summary>
    public DTCSharpMessageType MessageTypeExtended { get; }

    /// <summary>
    ///     Pairs messageType with the corresponding Protobuf IMessage
    /// </summary>
    public DTCMessageType MessageType { get; }

    /// <summary>
    ///     The message in Protocol Buffer form
    /// </summary>
    public IMessage Message { get; }

    public bool Equals(MessageProto other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return MessageType == other.MessageType
               && Equals(Message, other.Message);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }
        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        if (obj.GetType() != GetType())
        {
            return false;
        }
        return Equals((MessageProto)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)MessageType * 397) ^ (Message != null ? Message.GetHashCode() : 0);
        }
    }

    public static bool operator ==(MessageProto left, MessageProto right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(MessageProto left, MessageProto right)
    {
        return !Equals(left, right);
    }

    public override string ToString()
    {
        return IsExtended ? $"{MessageTypeExtended}: {Message}" : $"{MessageType}: {Message}";
    }
}