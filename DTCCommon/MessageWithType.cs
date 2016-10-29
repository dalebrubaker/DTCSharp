﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DTCPB;
using Google.Protobuf;

namespace DTCCommon
{
    public class MessageWithType<T> where T : IMessage
    {
        public MessageWithType(DTCMessageType messageType, T message)
        {
            MessageType = messageType;
            Message = message;
        }
        public DTCMessageType MessageType { get; }
        public T Message { get;  }
    }
}