using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DTCCommon;
using DTCPB;
using Timer = System.Timers.Timer;

namespace DTCClient
{
    public class Client : IDisposable
    {
        private readonly string _server;
        private readonly int _port;
        private readonly Timer _heartbeatTimer;
        private bool _isDisposed;
        private CancellationToken _cancellationToken;
        private BinaryReader _binaryReader;
        private BinaryWriter _binaryWriter;
        private TcpClient _tcpClient;
        private const int HeartbeatInterval = 60 * 1000; // 1 minute in milliseconds

        public Client(string server, int port)
        {
            _server = server;
            _port = port;
            _heartbeatTimer = new Timer(HeartbeatInterval);
            _heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
        }

        private void HeartbeatTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Send a heartbeat to the server
            throw new NotImplementedException();
        }

        /// <summary>
        /// Make the connection to server at port. 
        /// Start the heartbeats.
        /// Start the listener that will throw events for messages received from the server.
        /// To Disconnect simply Dispose() of this class.
        /// </summary>
        /// <param name="cancellationToken">optional token to stop receiving messages</param>
        /// <returns><c>true</c> if successful. <c>false</c> means protocol buffers are not supported by server</returns>
        public async Task<bool> Connect(CancellationToken cancellationToken = default(CancellationToken))
        {
            _cancellationToken = cancellationToken;
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(_server, _port); // connect to the server
            NetworkStream networkStream = _tcpClient.GetStream();
            _binaryReader = new BinaryReader(networkStream);
            _binaryWriter = new BinaryWriter(networkStream);
            if (!IsProtobufSupported())
            {
                return false;
            }
            _heartbeatTimer.Start();
            await Task.Run(() => MessageReader(), _cancellationToken);
            return true;
        }

        /// <summary>
        /// Do the up-front binary-encoded request/response per 
        /// </summary>
        /// <returns><c>true</c> if the server can support protobuf encoding</returns>
        private bool IsProtobufSupported()
        {
            var size = 2 + 2 + 4 + 4 + 3 + 1;
            _binaryWriter.Write((short)size);
            _binaryWriter.Write((short)DTCMessageType.EncodingRequest); // enum size is 4
            _binaryWriter.Write((int)DTCVersion.CurrentVersion);
            _binaryWriter.Write((int)EncodingEnum.ProtocolBuffers);
            _binaryWriter.Write("DTC"); // 3 chars plus null terminator

            var sizeReceived = _binaryReader.ReadInt16();
            if (sizeReceived != size)
            {
                throw new ArgumentException("Unexpected size mismatch");
            }
            var typeReceived = (DTCMessageType)_binaryReader.ReadInt16();
            if (typeReceived != DTCMessageType.EncodingResponse)
            {
                throw new InvalidDataException("Unexpected message type");
            }
            var protocolVersion = _binaryReader.ReadInt32();
            var encoding = (EncodingEnum)_binaryReader.ReadInt32();
            if (encoding != EncodingEnum.ProtocolBuffers)
            {
                // server can't support it
                return false;
            }
            var protocolType = _binaryReader.ReadChars(3);
            return true;
        }

        public async Task Logon()
        {
            
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                _binaryReader.Dispose();
                _binaryWriter.Dispose();
                _tcpClient.Dispose();
                _heartbeatTimer.Dispose();
                _isDisposed = true;
            }
        }

        /// <summary>
        /// Send the message represented by bytes
        /// </summary>
        /// <param name="type"></param>
        /// <param name="bytes"></param>
        private void SendMessage(DTCMessageType type, byte[] bytes)
        {
            // Write header
            _binaryWriter.Write(bytes.Length);
            _binaryWriter.Write((short)type);

            _binaryWriter.Write(bytes);
        }

        /// <summary>
        /// This message runs in a continuous loop on its own thread, throwing events as messages are received.
        /// </summary>
        private void MessageReader()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                // Read the header
                var size = _binaryReader.ReadInt16();
                var type = (DTCMessageType)_binaryReader.ReadInt16();
                var bytes = _binaryReader.ReadBytes(size);
                switch (type)
                {
                    case DTCMessageType.MessageTypeUnset:
                        throw new NotImplementedException("Not expected client-side");
                    case DTCMessageType.LogonRequest:
                        throw new NotImplementedException("Not expected client-side");
                    case DTCMessageType.LogonResponse:
                        var logonResponse = LogonResponse.Parser.ParseFrom(bytes);
                        var tempLogonReponseEvent = LogonReponseEvent; // for thread safety
                        tempLogonReponseEvent?.Invoke(this, new EventArgs<LogonResponse>(logonResponse));
                        break;
                    case DTCMessageType.Heartbeat:
                        break;
                    case DTCMessageType.Logoff:
                        throw new NotImplementedException("Not expected client-side");
                    case DTCMessageType.EncodingRequest:
                        throw new NotImplementedException("Not expected client-side");
                    case DTCMessageType.EncodingResponse:
                        // Note that we must use binary encoding here, per http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#EncodingRequest
                        throw new NotImplementedException("We only do at Connect, using binary encoding");
                        //var encodingReponse = EncodingResponse.Parser.ParseFrom(bytes);
                        //var tempEncodingReponseEvent = EncodingReponseEvent; // for thread safety
                        //tempEncodingReponseEvent?.Invoke(this, new EventArgs<EncodingResponse>(encodingReponse));
                        //break;
                    case DTCMessageType.MarketDataRequest:
                        break;
                    case DTCMessageType.MarketDataReject:
                        break;
                    case DTCMessageType.MarketDataSnapshot:
                        break;
                    case DTCMessageType.MarketDataSnapshotInt:
                        break;
                    case DTCMessageType.MarketDataUpdateTrade:
                        break;
                    case DTCMessageType.MarketDataUpdateTradeCompact:
                        break;
                    case DTCMessageType.MarketDataUpdateTradeInt:
                        break;
                    case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                        break;
                    case DTCMessageType.MarketDataUpdateBidAsk:
                        break;
                    case DTCMessageType.MarketDataUpdateBidAskCompact:
                        break;
                    case DTCMessageType.MarketDataUpdateBidAskInt:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionOpen:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionOpenInt:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionHigh:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionHighInt:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionLow:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionLowInt:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionVolume:
                        break;
                    case DTCMessageType.MarketDataUpdateOpenInterest:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionSettlement:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionNumTrades:
                        break;
                    case DTCMessageType.MarketDataUpdateTradingSessionDate:
                        break;
                    case DTCMessageType.MarketDepthRequest:
                        break;
                    case DTCMessageType.MarketDepthReject:
                        break;
                    case DTCMessageType.MarketDepthSnapshotLevel:
                        break;
                    case DTCMessageType.MarketDepthSnapshotLevelInt:
                        break;
                    case DTCMessageType.MarketDepthUpdateLevel:
                        break;
                    case DTCMessageType.MarketDepthUpdateLevelCompact:
                        break;
                    case DTCMessageType.MarketDepthUpdateLevelInt:
                        break;
                    case DTCMessageType.MarketDepthFullUpdate10:
                        break;
                    case DTCMessageType.MarketDepthFullUpdate20:
                        break;
                    case DTCMessageType.MarketDataFeedStatus:
                        break;
                    case DTCMessageType.MarketDataFeedSymbolStatus:
                        break;
                    case DTCMessageType.SubmitNewSingleOrder:
                        break;
                    case DTCMessageType.SubmitNewSingleOrderInt:
                        break;
                    case DTCMessageType.SubmitNewOcoOrder:
                        break;
                    case DTCMessageType.SubmitNewOcoOrderInt:
                        break;
                    case DTCMessageType.CancelOrder:
                        break;
                    case DTCMessageType.CancelReplaceOrder:
                        break;
                    case DTCMessageType.CancelReplaceOrderInt:
                        break;
                    case DTCMessageType.OpenOrdersRequest:
                        break;
                    case DTCMessageType.OpenOrdersReject:
                        break;
                    case DTCMessageType.OrderUpdate:
                        break;
                    case DTCMessageType.HistoricalOrderFillsRequest:
                        break;
                    case DTCMessageType.HistoricalOrderFillResponse:
                        break;
                    case DTCMessageType.CurrentPositionsRequest:
                        break;
                    case DTCMessageType.CurrentPositionsReject:
                        break;
                    case DTCMessageType.PositionUpdate:
                        break;
                    case DTCMessageType.TradeAccountsRequest:
                        break;
                    case DTCMessageType.TradeAccountResponse:
                        break;
                    case DTCMessageType.ExchangeListRequest:
                        break;
                    case DTCMessageType.ExchangeListResponse:
                        break;
                    case DTCMessageType.SymbolsForExchangeRequest:
                        break;
                    case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                        break;
                    case DTCMessageType.SymbolsForUnderlyingRequest:
                        break;
                    case DTCMessageType.SecurityDefinitionForSymbolRequest:
                        break;
                    case DTCMessageType.SecurityDefinitionResponse:
                        break;
                    case DTCMessageType.SymbolSearchRequest:
                        break;
                    case DTCMessageType.SecurityDefinitionReject:
                        break;
                    case DTCMessageType.AccountBalanceRequest:
                        break;
                    case DTCMessageType.AccountBalanceReject:
                        break;
                    case DTCMessageType.AccountBalanceUpdate:
                        break;
                    case DTCMessageType.UserMessage:
                        break;
                    case DTCMessageType.GeneralLogMessage:
                        break;
                    case DTCMessageType.HistoricalPriceDataRequest:
                        break;
                    case DTCMessageType.HistoricalPriceDataResponseHeader:
                        break;
                    case DTCMessageType.HistoricalPriceDataReject:
                        break;
                    case DTCMessageType.HistoricalPriceDataRecordResponse:
                        break;
                    case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                        break;
                    case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                        break;
                    case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // Now read the message
                //string request = _streamReader.ReadBlock();

            }
        }

        //public event EventHandler<EventArgs<EncodingResponse>> EncodingReponseEvent;
        public event EventHandler<EventArgs<LogonResponse>> LogonReponseEvent;

    }
}
