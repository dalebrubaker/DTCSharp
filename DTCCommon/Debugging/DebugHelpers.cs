using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public static void AddRequestReceived(DTCMessageType messageType, ICodecDTC currentCodec, bool isZipped, int size)
        {
            var lastRow = RequestsReceived.LastOrDefault();
            if (lastRow == null || lastRow.MessageType != messageType)
            {
                RequestsReceived.Add(new DebugMessageRow(messageType, currentCodec, isZipped, size));
            }
            else
            {
                lastRow.RepeatCount++;
            }
        }

        public static void AddRequestSent(DTCMessageType messageType, ICodecDTC currentCodec)
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

        public static void AddResponseReceived(DTCMessageType messageType, ICodecDTC currentCodec, bool isZipped, int size)
        {
            var lastRow = ResponsesReceived.LastOrDefault();
            if (lastRow == null || lastRow.MessageType != messageType)
            {
                ResponsesReceived.Add(new DebugMessageRow(messageType, currentCodec, isZipped, size));
            }
            else
            {
                lastRow.RepeatCount++;
            }
        }

        public static void AddResponseSent(DTCMessageType messageType, ICodecDTC currentCodec, bool isZipped)
        {
            var lastRow = ResponsesSent.LastOrDefault();
            if (lastRow == null || lastRow.MessageType != messageType)
            {
                ResponsesSent.Add(new DebugMessageRow(messageType, currentCodec, isZipped));
            }
            else
            {
                lastRow.RepeatCount++;
            }
        }


#endif
    }
}
