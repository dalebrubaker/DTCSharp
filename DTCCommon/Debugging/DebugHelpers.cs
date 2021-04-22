using System.Collections.Generic;
using System.Linq;
using DTCCommon.Codecs;
using DTCPB;

namespace DTCCommon
{
    public static class DebugHelpers
    {
#if DEBUG
        // ClientHelper (server-side)
        public static readonly List<DebugMessageRow> RequestsReceived = new List<DebugMessageRow>();
        public static readonly List<DebugMessageRow> ResponsesSent = new List<DebugMessageRow>();

        // Client
        public static readonly List<DebugMessageRow> ResponsesReceived = new List<DebugMessageRow>();
        public static readonly List<DebugMessageRow> RequestsSent = new List<DebugMessageRow>();

        public static void AddRequestReceived(DTCMessageType messageType, Codec currentCodec,int size)
        {
            var lastRow = RequestsReceived.LastOrDefault();
            if (lastRow == null || lastRow.MessageType != messageType)
            {
                RequestsReceived.Add(new DebugMessageRow(messageType, currentCodec, size));
            }
            else
            {
                lastRow.RepeatCount++;
            }
        }

        public static void AddRequestSent(DTCMessageType messageType, Codec currentCodec)
        {
            var lastRow = RequestsSent.LastOrDefault();
            if (lastRow == null || lastRow.MessageType != messageType)
            {
                RequestsSent.Add(new DebugMessageRow(messageType, currentCodec));
            }
            else
            {
                lastRow.RepeatCount++;
            }
        }

        public static void AddResponseReceived(DTCMessageType messageType, Codec currentCodec, int size)
        {
            var lastRow = ResponsesReceived.LastOrDefault();
            if (lastRow == null || lastRow.MessageType != messageType)
            {
                ResponsesReceived.Add(new DebugMessageRow(messageType, currentCodec, size));
            }
            else
            {
                lastRow.RepeatCount++;
            }
        }

        public static void AddResponseSent(DTCMessageType messageType, Codec currentCodec, bool isZipped = false)
        {
            var lastRow = ResponsesSent.LastOrDefault();
            if (lastRow == null || lastRow.MessageType != messageType)
            {
                ResponsesSent.Add(new DebugMessageRow(messageType, currentCodec));
            }
            else
            {
                lastRow.RepeatCount++;
            }
        }

#endif
    }
}