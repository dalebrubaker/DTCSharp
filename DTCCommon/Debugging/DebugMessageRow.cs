using System.Threading;
using DTCCommon.Codecs;
using DTCPB;

namespace DTCCommon
{
    public class DebugMessageRow
    {
        public DTCMessageType MessageType { get; }
        public Codec CurrentCodec { get; }
        public int Size { get; }

        public int ThreadId { get; }

        public DebugMessageRow(DTCMessageType messageType, Codec currentCodec, int size = int.MinValue)
        {
            MessageType = messageType;
            CurrentCodec = currentCodec;
            Size = size;
            RepeatCount = 1;
            ThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public int RepeatCount { get; set; }

        public override string ToString()
        {
            var result = $"{MessageType} {CurrentCodec} RepeatCount:{RepeatCount} ThreadId:{ThreadId}";
            if (Size != int.MinValue)
            {
                result += $" Size:{Size}";
            }
            return result;
        }
    }
}