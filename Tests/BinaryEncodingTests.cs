using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DTCClient;
using DTCCommon;
using DTCCommon.Codecs;
using DTCCommon.Extensions;
using DTCPB;
using DTCServer;
using TestServer;
using Xunit;

namespace Tests
{
    public class BinaryEncodingTests : IDisposable
    {
        public BinaryEncodingTests()
        {
        }

        public void Dispose()
        {
            Console.WriteLine("Disposing");
        }

        [Fact]
        public void HeartbeatTest()
        {
            var codecBinary = new CodecBinary();
            var now = DateTime.UtcNow;
            var heartbeat = new Heartbeat
            {
                CurrentDateTime = now.UtcToDtcDateTime(),
                NumDroppedMessages = 29
            };
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            codecBinary.Write(DTCMessageType.Heartbeat, heartbeat, bw);
            var bytes = ms.ToArray();
            int sizeExcludingHeader;
            DTCMessageType messageType;
            Utility.ReadHeader(ref bytes, out sizeExcludingHeader, out messageType);
            Assert.Equal(DTCMessageType.Heartbeat, messageType);
            var hb = codecBinary.Load<Heartbeat>(DTCMessageType.Heartbeat, bytes);
            Assert.Equal(heartbeat, hb);
        }
    }
}
