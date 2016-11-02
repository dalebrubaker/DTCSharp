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
using Google.Protobuf;
using TestServer;
using Xunit;

namespace Tests
{
    public class BinaryEncodingTests : IDisposable
    {
        private CodecBinary _codecBinary;

        public BinaryEncodingTests()
        {
            _codecBinary = new CodecBinary();
        }

        public void Dispose()
        {
            Console.WriteLine("Disposing");
        }

        private void GenericTest<T>(DTCMessageType messageType, T message) where T : IMessage<T>, new()
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            _codecBinary.Write(messageType, message, bw);
            var bytes = ms.ToArray();
            int sizeExcludingHeader;
            DTCMessageType messageTypeHeader;
            Utility.ReadHeader(bytes, out sizeExcludingHeader, out messageTypeHeader);
            Assert.Equal(messageType, messageTypeHeader);
            var hb = _codecBinary.Load<T>(messageType, bytes, 4);
            Assert.Equal(message, hb);
        }

        [Fact]
        public void HeartbeatTest()
        {
            var now = DateTime.UtcNow;
            var heartbeat = new Heartbeat
            {
                CurrentDateTime = now.UtcToDtcDateTime(),
                NumDroppedMessages = 29
            };
            GenericTest(DTCMessageType.Heartbeat, heartbeat);
        }

        [Fact]
        public void EncodingRequestTest()
        {
            var encodingRequest = new EncodingRequest
            {
                ProtocolVersion = 1,
                Encoding = EncodingEnum.ProtocolBuffers,
                ProtocolType = "test"
            };
            GenericTest(DTCMessageType.EncodingRequest, encodingRequest);
        }
    }
}
