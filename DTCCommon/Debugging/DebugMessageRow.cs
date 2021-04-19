using System.Threading;
using DTCCommon.Codecs;
using DTCPB;

namespace DTCCommon
{
    public class DebugMessageRow
    {
        public DTCMessageType MessageType { get; }
        public ICodecDTC CurrentCodec { get; }
        public bool IsZipped { get; }
        public int Size { get; }

        public int ThreadId { get; }

        public DebugMessageRow(DTCMessageType messageType, ICodecDTC currentCodec, bool isZipped = false, int size = int.MinValue)
        {
            MessageType = messageType;
            CurrentCodec = currentCodec;
            IsZipped = isZipped;
            Size = size;
            RepeatCount = 1;
            ThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public int RepeatCount { get; set; }

        public override string ToString()
        {
            var result = $"{MessageType} {CurrentCodec} IsZipped:{IsZipped} RepeatCount:{RepeatCount} ThreadId:{ThreadId}";
            if (Size != int.MinValue)
            {
                result += $" Size:{Size}";
            }
            return result;
        }
    }
}