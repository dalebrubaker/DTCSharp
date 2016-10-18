using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Google.Protobuf;
using Timer = System.Timers.Timer;

namespace DTCClient
{
    public partial class Client : IDisposable
    {
        private readonly string _server;
        private readonly int _port;
        private readonly Timer _heartbeatTimer;
        private bool _isDisposed;
        private BinaryWriter _binaryWriter;
        private TcpClient _tcpClient;
        private DateTime _lastHeartbeatReceivedTime;
        private NetworkStream _networkStream;
        private EncodingEnum _currentEncoding;
        private CancellationTokenSource _cts;

        /// <summary>
        /// The most recent _logonResponse.
        /// Use this to check Server flags before doing SendRequest()
        /// </summary>
        public LogonResponse LogonResponse { get; private set; }

        public Client(string server, int port)
        {
            _server = server;
            _port = port;
            _heartbeatTimer = new Timer(10000);
            _heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
            _currentEncoding = EncodingEnum.BinaryEncoding; // until we've set it to ProtocolBuffers
            HeartbeatEvent += Client_HeartbeatEvent;
        }

        private void Client_HeartbeatEvent(object sender, EventArgs<Heartbeat> e)
        {
            _lastHeartbeatReceivedTime = DateTime.Now;
        }

        private void HeartbeatTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var maxWaitForHeartbeatTime = TimeSpan.FromMilliseconds(Math.Max(_heartbeatTimer.Interval * 2, 5000));
            var timeSinceHeartbeat = (DateTime.Now - _lastHeartbeatReceivedTime);
            if (timeSinceHeartbeat > maxWaitForHeartbeatTime)
            {
                Dispose(true);
                throw new ApplicationException("Too long since Server sent us a heartbeat. Closing client.");
            }

            // Send a heartbeat to the server
            var heartBeat = new Heartbeat();
            heartBeat.Write(_binaryWriter, _currentEncoding);
        }

        #region events

        public event EventHandler<EventArgs<EncodingResponse>> EncodingResponseEvent;
        public event EventHandler<EventArgs<Heartbeat>> HeartbeatEvent;
        public event EventHandler<EventArgs<Logoff>> LogoffEvent;
        public event EventHandler<EventArgs<LogonResponse>> LogonReponseEvent;
        public event EventHandler<EventArgs<MarketDataReject>> MarketDataRejectEvent;
        public event EventHandler<EventArgs<MarketDataSnapshot>> MarketDataSnapshotEvent;
        public event EventHandler<EventArgs<MarketDataSnapshot_Int>> MarketDataSnapshotIntEvent;
        public event EventHandler<EventArgs<MarketDataUpdateTrade>> MarketDataUpdateTradeEvent;
        public event EventHandler<EventArgs<MarketDataUpdateTradeCompact>> MarketDataUpdateTradeCompactEvent;
        public event EventHandler<EventArgs<MarketDataUpdateTrade_Int>> MarketDataUpdateTradeIntEvent;
        public event EventHandler<EventArgs<MarketDataUpdateLastTradeSnapshot>> MarketDataUpdateLastTradeSnapshotEvent;
        public event EventHandler<EventArgs<MarketDataUpdateBidAsk>> MarketDataUpdateBidAskEvent;
        public event EventHandler<EventArgs<MarketDataUpdateBidAskCompact>> MarketDataUpdateBidAskCompactEvent;


        #endregion events


        /// <summary>
        /// Make the connection to server at port. 
        /// Start the heartbeats.
        /// Start the listener that will throw events for messages received from the server.
        /// To Disconnect simply Dispose() of this class.
        /// </summary>
        /// <param name="cancellationToken">optional token to stop receiving messages</param>
        /// <returns><c>true</c> if successful. <c>false</c> means protocol buffers are not supported by server</returns>
        private async Task ConnectAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            _tcpClient = new TcpClient {NoDelay = true};
            await _tcpClient.ConnectAsync(_server, _port); // connect to the server
            _networkStream = _tcpClient.GetStream();
            _binaryWriter = new BinaryWriter(_networkStream);
            _currentEncoding = EncodingEnum.BinaryEncoding;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            // Fire and forget
            Task.Run(MessageReader, _cts.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            // Write encoding request with binary encoding
            var encodingRequest = new EncodingRequest
            {
                Encoding = EncodingEnum.ProtocolBuffers,
                ProtocolType = "DTC",
                ProtocolVersion = (int)DTCVersion.CurrentVersion
            };
            encodingRequest.Write(_binaryWriter, _currentEncoding);
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 10000 && _currentEncoding != EncodingEnum.ProtocolBuffers)
            {
                await Task.Delay(1, cancellationToken);
            }
            _lastHeartbeatReceivedTime = DateTime.Now;
            _heartbeatTimer.Start();
        }

        /// <summary>
        /// Send a Logon request to the server. 
        /// The response will come back via the LogonResponseEvent
        /// </summary>
        /// <param name="heartbeatIntervalInSeconds">The interval in seconds that each side, the Client and the Server, needs to use to send HEARTBEAT messages to the other side. This should be a value from anywhere from 5 to 60 seconds.</param>
        /// <param name="userName">Optional user name for the server to authenticate the Client</param>
        /// <param name="password">Optional password for the server to authenticate the Client</param>
        /// <param name="generalTextData">Optional general-purpose text string. For example, this could be used to pass a license key that the Server may require</param>
        /// <param name="integer1">Optional. General-purpose integer</param>
        /// <param name="integer2">Optional. General-purpose integer</param>
        /// <param name="tradeMode">optional to indicate to the Server that the requested trading mode to be one of the following: Demo, Simulated, Live.</param>
        /// <param name="tradeAccount">optional identifier if that is required to login</param>
        /// <param name="hardwareIdentifier">optional computer hardware identifier</param>
        /// <param name="clientName">optional</param>
        public async Task LogonAsync(int heartbeatIntervalInSeconds, string clientName = "", string userName = "", string password = "", string generalTextData = "",
            int integer1 = 0, int integer2 = 0,
            TradeModeEnum tradeMode = TradeModeEnum.TradeModeUnset, string tradeAccount = "", string hardwareIdentifier = "")
        {
            _heartbeatTimer.Interval = heartbeatIntervalInSeconds * 1000;
            _cts = new CancellationTokenSource();
            await ConnectAsync(_cts.Token);

            var logonRequest = new LogonRequest
            {
                ClientName = clientName,
                GeneralTextData = generalTextData,
                HardwareIdentifier = hardwareIdentifier,
                HeartbeatIntervalInSeconds = heartbeatIntervalInSeconds,
                Integer1 = integer1,
                Integer2 = integer2,
                Password = password,
                ProtocolVersion = (int)DTCVersion.CurrentVersion,
                TradeAccount = tradeAccount,
                TradeMode = tradeMode
            };
            SendRequest(DTCMessageType.LogonRequest, logonRequest.ToByteArray());
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
                _cts.Cancel();
                _binaryWriter.Dispose();
                _tcpClient.Dispose();
                _heartbeatTimer.Dispose();
                _isDisposed = true;
            }
        }

        /// <summary>
        /// Send the message represented by bytes
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="bytes"></param>
        public void SendRequest(DTCMessageType messageType, byte[] bytes)
        {
            // Write header 
            Utility.WriteHeader(_binaryWriter, bytes.Length, messageType);
            _binaryWriter.Write(bytes);
        }

        private void ThrowEvent<T>(T message, EventHandler<EventArgs<T>> eventForMessage) where T:IMessage
        {
            var temp = eventForMessage; // for thread safety
            temp?.Invoke(this, new EventArgs<T>(message));
        }

        /// <summary>
        /// This message runs in a continuous loop on its own thread, throwing events as messages are received.
        /// </summary>
        private async Task MessageReader()
        {
            using (var binaryReader = new BinaryReader(_networkStream))
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    if (!_networkStream.DataAvailable)
                    {
                        await Task.Delay(1, _cts.Token);
                        continue;
                    }

                    // Read the header
                    int size;
                    try
                    {
                        size = binaryReader.ReadInt16();
                    }
                    catch (Exception ex)
                    {

                        throw;
                    }
                    var messageType = (DTCMessageType)binaryReader.ReadInt16();
                    var bytes = binaryReader.ReadBytes(size - 4); // size included the header size+type
                    switch (messageType)
                    {
                        case DTCMessageType.LogonResponse:
                            LogonResponse = LogonResponse.Parser.ParseFrom(bytes);
                            ThrowEvent(LogonResponse, LogonReponseEvent);
                            break;
                        case DTCMessageType.Heartbeat:
                            var heartbeat = new Heartbeat();
                            heartbeat.Load(bytes, _currentEncoding);
                            ThrowEvent(heartbeat, HeartbeatEvent);
                            break;
                        case DTCMessageType.Logoff:
                            ThrowEvent(Logoff.Parser.ParseFrom(bytes), LogoffEvent);
                            break;
                        case DTCMessageType.EncodingResponse:
                            // Note that we must use binary encoding here on the first usage after connect, 
                            //    per http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#EncodingRequest
                            var encodingResponse = new EncodingResponse();
                            encodingResponse.Load(bytes, _currentEncoding);
                            _currentEncoding = encodingResponse.Encoding;
                            ThrowEvent(encodingResponse, EncodingResponseEvent);
                            break;
                        case DTCMessageType.MarketDataReject:
                            ThrowEvent(MarketDataReject.Parser.ParseFrom(bytes), MarketDataRejectEvent);
                            break;
                        case DTCMessageType.MarketDataSnapshot:
                            ThrowEvent(MarketDataSnapshot.Parser.ParseFrom(bytes), MarketDataSnapshotEvent);
                            break;
                        case DTCMessageType.MarketDataSnapshotInt:
                            ThrowEvent(MarketDataSnapshot_Int.Parser.ParseFrom(bytes), MarketDataSnapshotIntEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateTrade:
                            ThrowEvent(MarketDataUpdateTrade.Parser.ParseFrom(bytes), MarketDataUpdateTradeEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateTradeCompact:
                            ThrowEvent(MarketDataUpdateTradeCompact.Parser.ParseFrom(bytes), MarketDataUpdateTradeCompactEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateTradeInt:
                            ThrowEvent(MarketDataUpdateTrade_Int.Parser.ParseFrom(bytes), MarketDataUpdateTradeIntEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                            ThrowEvent(MarketDataUpdateLastTradeSnapshot.Parser.ParseFrom(bytes), MarketDataUpdateLastTradeSnapshotEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateBidAsk:
                            ThrowEvent(MarketDataUpdateBidAsk.Parser.ParseFrom(bytes), MarketDataUpdateBidAskEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateBidAskCompact:
                            ThrowEvent(MarketDataUpdateBidAskCompact.Parser.ParseFrom(bytes), MarketDataUpdateBidAskCompactEvent);
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
                        case DTCMessageType.OpenOrdersReject:
                            break;
                        case DTCMessageType.OrderUpdate:
                            break;
                        case DTCMessageType.HistoricalOrderFillResponse:
                            break;
                        case DTCMessageType.CurrentPositionsReject:
                            break;
                        case DTCMessageType.PositionUpdate:
                            break;
                        case DTCMessageType.TradeAccountResponse:
                            break;
                        case DTCMessageType.ExchangeListResponse:
                            break;
                        case DTCMessageType.SecurityDefinitionResponse:
                            break;
                        case DTCMessageType.SecurityDefinitionReject:
                            break;
                        case DTCMessageType.AccountBalanceReject:
                            break;
                        case DTCMessageType.AccountBalanceUpdate:
                            break;
                        case DTCMessageType.UserMessage:
                            break;
                        case DTCMessageType.GeneralLogMessage:
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
                        case DTCMessageType.MessageTypeUnset:
                        case DTCMessageType.LogonRequest:
                        case DTCMessageType.EncodingRequest:
                        case DTCMessageType.MarketDataRequest:
                        case DTCMessageType.MarketDepthRequest:
                        case DTCMessageType.OpenOrdersRequest:
                        case DTCMessageType.HistoricalOrderFillsRequest:
                        case DTCMessageType.CurrentPositionsRequest:
                        case DTCMessageType.TradeAccountsRequest:
                        case DTCMessageType.ExchangeListRequest:
                        case DTCMessageType.SymbolsForExchangeRequest:
                        case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                        case DTCMessageType.SymbolsForUnderlyingRequest:
                        case DTCMessageType.SecurityDefinitionForSymbolRequest:
                        case DTCMessageType.SymbolSearchRequest:
                        case DTCMessageType.AccountBalanceRequest:
                        case DTCMessageType.HistoricalPriceDataRequest:
                        default:
                            throw new ArgumentOutOfRangeException($"Unexpected MessageType {messageType} received by client.");
                    }
                }
            }
        }

       

    }
}
