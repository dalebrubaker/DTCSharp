using System;
using System.Threading.Tasks;
using DTCPB;
using DTCServer;

namespace TestServer
{
    /// <summary>
    /// The server implementation that provides responses to client requests
    /// </summary>
    public class ServerStub  : IServerStub
    {
        public event EventHandler<string> MessageEvent;

        private void OnMessage(string message)
        {
            var temp = MessageEvent;
            temp?.Invoke(this, message);
        }

        public Task<LogonResponse> LogonRequestAsync(string clientHandlerId, LogonRequest logonRequest)
        {
            OnMessage($"Received Login Request from client {logonRequest.ClientName} via {clientHandlerId}");
            var response = new LogonResponse
            {
                ProtocolVersion = logonRequest.ProtocolVersion,
                Result = LogonStatusEnum.LogonSuccess,
                ResultText = "Logon Successful",
                ServerName = Environment.MachineName,
                SecurityDefinitionsSupported = 1,
                HistoricalPriceDataSupported = 1,
                MarketDataSupported = 1,
            };
            return Task.FromResult(response);
        }

        public Task LogoffAsync(string clientHandlerId, Logoff logoff)
        {
            // Do nothing
            return null;
        }

        public Task<EncodingResponse> EncodingRequestAsync(string clientHandlerId, EncodingRequest encodingRequest)
        {
            OnMessage($"Received Encoding Request via {clientHandlerId}");
            switch (encodingRequest.Encoding)
            {
                case EncodingEnum.BinaryEncoding:
                    break;
                case EncodingEnum.BinaryWithVariableLengthStrings:
                case EncodingEnum.JsonEncoding:
                case EncodingEnum.JsonCompactEncoding:
                    throw new NotImplementedException($"Not implemented in {nameof(EncodingRequestAsync)}: {nameof(encodingRequest.Encoding)}");
                case EncodingEnum.ProtocolBuffers:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var response = new EncodingResponse
            {
                ProtocolType = encodingRequest.ProtocolType,
                ProtocolVersion = encodingRequest.ProtocolVersion,
                Encoding = encodingRequest.Encoding
            };
            return Task.FromResult(response);
        }

        public Task HeartbeatAsync(string clientHandlerId, Heartbeat heartbeat)
        {
            OnMessage($"Heartbeat received via {clientHandlerId}.");
            return null;
        }
    }
}
