using System;
using DTCPB;

namespace DTCCommon.EventArgsF
{
    public class RawMessageEventArgs : EventArgs
    {
        public byte[] Packet { get; }

        public EncodingEnum MessageType { get; }

        public RawMessageEventArgs(byte[] packet, EncodingEnum messageType)
        {
            Packet = packet;
            MessageType = messageType;
        }
    }
}