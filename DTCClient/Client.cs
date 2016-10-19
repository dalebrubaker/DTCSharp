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
        private int _nextRequestId;
        private int _nextSymbolId;

        /// <summary>
        /// The most recent _logonResponse.
        /// Use this to check Server flags before doing SendRequest()
        /// </summary>
        public LogonResponse LogonResponse { get; private set; }

        /// <summary>
        /// See http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#SymbolIDRequestIDRules
        /// </summary>
        public int NextRequestId => ++_nextRequestId;

        /// <summary>
        /// See http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#SymbolIDRequestIDRules
        /// </summary>
        public int NextSymbolId => ++_nextSymbolId;
        
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
        public event EventHandler<EventArgs<MarketDataUpdateBidAsk_Int>> MarketDataUpdateBidAskIntEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionOpen>> MarketDataUpdateSessionOpenEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionOpen_Int>> MarketDataUpdateSessionOpenIntEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionHigh>> MarketDataUpdateSessionHighEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionHigh_Int>> MarketDataUpdateSessionHighIntEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionLow>> MarketDataUpdateSessionLowEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionLow_Int>> MarketDataUpdateSessionLowIntEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionVolume>> MarketDataUpdateSessionVolumeEvent;
        public event EventHandler<EventArgs<MarketDataUpdateOpenInterest>> MarketDataUpdateOpenInterestEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionSettlement>> MarketDataUpdateSessionSettlementEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionSettlement_Int>> MarketDataUpdateSessionSettlementIntEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionNumTrades>> MarketDataUpdateSessionNumTradesEvent;
        public event EventHandler<EventArgs<MarketDataUpdateTradingSessionDate>> MarketDataUpdateTradingSessionDateEvent;
        public event EventHandler<EventArgs<MarketDepthReject>> MarketDepthRejectEvent;
        public event EventHandler<EventArgs<MarketDepthSnapshotLevel>> MarketDepthSnapshotLevelEvent;
        public event EventHandler<EventArgs<MarketDepthSnapshotLevel_Int>> MarketDepthSnapshotLevelIntEvent;
        public event EventHandler<EventArgs<MarketDepthUpdateLevel>> MarketDepthUpdateLevelEvent;
        public event EventHandler<EventArgs<MarketDepthUpdateLevelCompact>> MarketDepthUpdateLevelCompactEvent;
        public event EventHandler<EventArgs<MarketDepthUpdateLevel_Int>> MarketDepthUpdateLevelIntEvent;
        public event EventHandler<EventArgs<MarketDepthFullUpdate10>> MarketDepthFullUpdate10Event;
        public event EventHandler<EventArgs<MarketDepthFullUpdate20>> MarketDepthFullUpdate20Event;
        public event EventHandler<EventArgs<MarketDataFeedStatus>> MarketDataFeedStatusEvent;
        public event EventHandler<EventArgs<MarketDataFeedSymbolStatus>> MarketDataFeedSymbolStatusEvent;
        public event EventHandler<EventArgs<OpenOrdersReject>> OpenOrdersRejectEvent;
        public event EventHandler<EventArgs<OrderUpdate>> OrderUpdateEvent;
        public event EventHandler<EventArgs<HistoricalOrderFillResponse>> HistoricalOrderFillResponseEvent;
        public event EventHandler<EventArgs<CurrentPositionsReject>> CurrentPositionsRejectEvent;
        public event EventHandler<EventArgs<PositionUpdate>> PositionUpdateEvent;
        public event EventHandler<EventArgs<TradeAccountResponse>> TradeAccountResponseEvent;
        public event EventHandler<EventArgs<ExchangeListResponse>> ExchangeListResponseEvent;
        public event EventHandler<EventArgs<SecurityDefinitionResponse>> SecurityDefinitionResponseEvent;
        public event EventHandler<EventArgs<SecurityDefinitionReject>> SecurityDefinitionRejectEvent;
        public event EventHandler<EventArgs<AccountBalanceReject>> AccountBalanceRejectEvent;
        public event EventHandler<EventArgs<AccountBalanceUpdate>> AccountBalanceUpdateEvent;
        public event EventHandler<EventArgs<UserMessage>> UserMessageEvent;
        public event EventHandler<EventArgs<GeneralLogMessage>> GeneralLogMessageEvent;
        public event EventHandler<EventArgs<HistoricalPriceDataResponseHeader>> HistoricalPriceDataResponseHeaderEvent;
        public event EventHandler<EventArgs<HistoricalPriceDataReject>> HistoricalPriceDataRejectEvent;
        public event EventHandler<EventArgs<HistoricalPriceDataRecordResponse>> HistoricalPriceDataRecordResponseEvent;
        public event EventHandler<EventArgs<HistoricalPriceDataTickRecordResponse>> HistoricalPriceDataTickRecordResponseEvent;
        public event EventHandler<EventArgs<HistoricalPriceDataRecordResponse_Int>> HistoricalPriceDataRecordResponseIntEvent;
        public event EventHandler<EventArgs<HistoricalPriceDataTickRecordResponse_Int>> HistoricalPriceDataTickRecordResponseIntEvent;


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
        /// The response will come back via the LogonResponseEvent
        /// </summary>
        public async Task LogonAsync(int heartbeatIntervalInSeconds, string clientName = "", string userName = "", string password = "", string generalTextData = "",
            int integer1 = 0, int integer2 = 0, TradeModeEnum tradeMode = TradeModeEnum.TradeModeUnset, string tradeAccount = "", 
            string hardwareIdentifier = "")
        {
        }

        /// <summary>
        /// Start a TCP connection and send a Logon request to the server. 
        /// </summary>
        /// <param name="heartbeatIntervalInSeconds">The interval in seconds that each side, the Client and the Server, needs to use to send HEARTBEAT messages to the other side. This should be a value from anywhere from 5 to 60 seconds.</param>
        /// <param name="timeout">The time (in milliseconds) to wait for a response before giving up</param>
        /// <param name="clientName">optional name for this client</param>
        /// <param name="userName">Optional user name for the server to authenticate the Client</param>
        /// <param name="password">Optional password for the server to authenticate the Client</param>
        /// <param name="generalTextData">Optional general-purpose text string. For example, this could be used to pass a license key that the Server may require</param>
        /// <param name="integer1">Optional. General-purpose integer</param>
        /// <param name="integer2">Optional. General-purpose integer</param>
        /// <param name="tradeMode">optional to indicate to the Server that the requested trading mode to be one of the following: Demo, Simulated, Live.</param>
        /// <param name="tradeAccount">optional identifier if that is required to login</param>
        /// <param name="hardwareIdentifier">optional computer hardware identifier</param>
        /// <returns>The LogonResponse, or null if not received before timeout</returns>
        public async Task<LogonResponse> LogonAsync(int heartbeatIntervalInSeconds, int timeout = 1000,  string clientName = "", string userName = "", string password = "", string generalTextData = "",
          int integer1 = 0, int integer2 = 0, TradeModeEnum tradeMode = TradeModeEnum.TradeModeUnset, string tradeAccount = "", 
          string hardwareIdentifier = "")
        {
            // Make a connection
            _heartbeatTimer.Interval = heartbeatIntervalInSeconds * 1000;
            _cts = new CancellationTokenSource();
            await ConnectAsync(_cts.Token);

            // Set up the handler to capture the event
            var startTime = DateTime.Now;
            LogonResponse result = null;
            EventHandler<EventArgs<LogonResponse>> handler = null;
            handler = (s, e) =>
            {
                LogonReponseEvent -= handler; // unregister to avoid a potential memory leak
                result = e.Data;
            };
            LogonReponseEvent += handler;

            // Send the request
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
            SendRequest(DTCMessageType.LogonRequest, logonRequest);

            // Wait until the response is received or until timeout
            while (result == null && (DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                await Task.Delay(1);
            }
            return result;
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
        /// <param name="message"></param>
        public void SendRequest<T>(DTCMessageType messageType, T message) where T:IMessage
        {
            // Write header 
            var bytes = message.ToByteArray();
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
                            ThrowEvent(MarketDataUpdateBidAsk_Int.Parser.ParseFrom(bytes), MarketDataUpdateBidAskIntEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionOpen:
                            ThrowEvent(MarketDataUpdateSessionOpen.Parser.ParseFrom(bytes), MarketDataUpdateSessionOpenEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionOpenInt:
                            ThrowEvent(MarketDataUpdateSessionOpen_Int.Parser.ParseFrom(bytes), MarketDataUpdateSessionOpenIntEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionHigh:
                            ThrowEvent(MarketDataUpdateSessionHigh.Parser.ParseFrom(bytes), MarketDataUpdateSessionHighEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionHighInt:
                            ThrowEvent(MarketDataUpdateSessionHigh_Int.Parser.ParseFrom(bytes), MarketDataUpdateSessionHighIntEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionLow:
                            ThrowEvent(MarketDataUpdateSessionLow.Parser.ParseFrom(bytes), MarketDataUpdateSessionLowEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionLowInt:
                            ThrowEvent(MarketDataUpdateSessionLow_Int.Parser.ParseFrom(bytes), MarketDataUpdateSessionLowIntEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionVolume:
                            ThrowEvent(MarketDataUpdateSessionVolume.Parser.ParseFrom(bytes), MarketDataUpdateSessionVolumeEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateOpenInterest:
                            ThrowEvent(MarketDataUpdateOpenInterest.Parser.ParseFrom(bytes), MarketDataUpdateOpenInterestEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionSettlement:
                            ThrowEvent(MarketDataUpdateSessionSettlement.Parser.ParseFrom(bytes), MarketDataUpdateSessionSettlementEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                            ThrowEvent(MarketDataUpdateSessionSettlement_Int.Parser.ParseFrom(bytes), MarketDataUpdateSessionSettlementIntEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionNumTrades:
                            ThrowEvent(MarketDataUpdateSessionNumTrades.Parser.ParseFrom(bytes), MarketDataUpdateSessionNumTradesEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateTradingSessionDate:
                            ThrowEvent(MarketDataUpdateTradingSessionDate.Parser.ParseFrom(bytes), MarketDataUpdateTradingSessionDateEvent);
                            break;
                        case DTCMessageType.MarketDepthReject:
                            ThrowEvent(MarketDepthReject.Parser.ParseFrom(bytes), MarketDepthRejectEvent);
                            break;
                        case DTCMessageType.MarketDepthSnapshotLevel:
                            ThrowEvent(MarketDepthSnapshotLevel.Parser.ParseFrom(bytes), MarketDepthSnapshotLevelEvent);
                            break;
                        case DTCMessageType.MarketDepthSnapshotLevelInt:
                            ThrowEvent(MarketDepthSnapshotLevel_Int.Parser.ParseFrom(bytes), MarketDepthSnapshotLevelIntEvent);
                            break;
                        case DTCMessageType.MarketDepthUpdateLevel:
                            ThrowEvent(MarketDepthUpdateLevel.Parser.ParseFrom(bytes), MarketDepthUpdateLevelEvent);
                            break;
                        case DTCMessageType.MarketDepthUpdateLevelCompact:
                            ThrowEvent(MarketDepthUpdateLevelCompact.Parser.ParseFrom(bytes), MarketDepthUpdateLevelCompactEvent);
                            break;
                        case DTCMessageType.MarketDepthUpdateLevelInt:
                            ThrowEvent(MarketDepthUpdateLevel_Int.Parser.ParseFrom(bytes), MarketDepthUpdateLevelIntEvent);
                            break;
                        case DTCMessageType.MarketDepthFullUpdate10:
                            ThrowEvent(MarketDepthFullUpdate10.Parser.ParseFrom(bytes), MarketDepthFullUpdate10Event);
                            break;
                        case DTCMessageType.MarketDepthFullUpdate20:
                            ThrowEvent(MarketDepthFullUpdate20.Parser.ParseFrom(bytes), MarketDepthFullUpdate20Event);
                            break;
                        case DTCMessageType.MarketDataFeedStatus:
                            ThrowEvent(MarketDataFeedStatus.Parser.ParseFrom(bytes), MarketDataFeedStatusEvent);
                            break;
                        case DTCMessageType.MarketDataFeedSymbolStatus:
                            ThrowEvent(MarketDataFeedSymbolStatus.Parser.ParseFrom(bytes), MarketDataFeedSymbolStatusEvent);
                            break;
                        case DTCMessageType.OpenOrdersReject:
                            ThrowEvent(OpenOrdersReject.Parser.ParseFrom(bytes), OpenOrdersRejectEvent);
                            break;
                        case DTCMessageType.OrderUpdate:
                            ThrowEvent(OrderUpdate.Parser.ParseFrom(bytes), OrderUpdateEvent);
                            break;
                        case DTCMessageType.HistoricalOrderFillResponse:
                            ThrowEvent(HistoricalOrderFillResponse.Parser.ParseFrom(bytes), HistoricalOrderFillResponseEvent);
                            break;
                        case DTCMessageType.CurrentPositionsReject:
                            ThrowEvent(CurrentPositionsReject.Parser.ParseFrom(bytes), CurrentPositionsRejectEvent);
                            break;
                        case DTCMessageType.PositionUpdate:
                            ThrowEvent(PositionUpdate.Parser.ParseFrom(bytes), PositionUpdateEvent);
                            break;
                        case DTCMessageType.TradeAccountResponse:
                            ThrowEvent(TradeAccountResponse.Parser.ParseFrom(bytes), TradeAccountResponseEvent);
                            break;
                        case DTCMessageType.ExchangeListResponse:
                            ThrowEvent(ExchangeListResponse.Parser.ParseFrom(bytes), ExchangeListResponseEvent);
                            break;
                        case DTCMessageType.SecurityDefinitionResponse:
                            ThrowEvent(SecurityDefinitionResponse.Parser.ParseFrom(bytes), SecurityDefinitionResponseEvent);
                            break;
                        case DTCMessageType.SecurityDefinitionReject:
                            ThrowEvent(SecurityDefinitionReject.Parser.ParseFrom(bytes), SecurityDefinitionRejectEvent);
                            break;
                        case DTCMessageType.AccountBalanceReject:
                            ThrowEvent(AccountBalanceReject.Parser.ParseFrom(bytes), AccountBalanceRejectEvent);
                            break;
                        case DTCMessageType.AccountBalanceUpdate:
                            ThrowEvent(AccountBalanceUpdate.Parser.ParseFrom(bytes), AccountBalanceUpdateEvent);
                            break;
                        case DTCMessageType.UserMessage:
                            ThrowEvent(UserMessage.Parser.ParseFrom(bytes), UserMessageEvent);
                            break;
                        case DTCMessageType.GeneralLogMessage:
                            ThrowEvent(GeneralLogMessage.Parser.ParseFrom(bytes), GeneralLogMessageEvent);
                            break;
                        case DTCMessageType.HistoricalPriceDataResponseHeader:
                            ThrowEvent(HistoricalPriceDataResponseHeader.Parser.ParseFrom(bytes), HistoricalPriceDataResponseHeaderEvent);
                            break;
                        case DTCMessageType.HistoricalPriceDataReject:
                            ThrowEvent(HistoricalPriceDataReject.Parser.ParseFrom(bytes), HistoricalPriceDataRejectEvent);
                            break;
                        case DTCMessageType.HistoricalPriceDataRecordResponse:
                            ThrowEvent(HistoricalPriceDataRecordResponse.Parser.ParseFrom(bytes), HistoricalPriceDataRecordResponseEvent);
                            break;
                        case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                            ThrowEvent(HistoricalPriceDataTickRecordResponse.Parser.ParseFrom(bytes), HistoricalPriceDataTickRecordResponseEvent);
                            break;
                        case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                            ThrowEvent(HistoricalPriceDataRecordResponse_Int.Parser.ParseFrom(bytes), HistoricalPriceDataRecordResponseIntEvent);
                            break;
                        case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                            ThrowEvent(HistoricalPriceDataTickRecordResponse_Int.Parser.ParseFrom(bytes), HistoricalPriceDataTickRecordResponseIntEvent);
                            break;
                        case DTCMessageType.MessageTypeUnset:
                        case DTCMessageType.LogonRequest:
                        case DTCMessageType.EncodingRequest:
                        case DTCMessageType.MarketDataRequest:
                        case DTCMessageType.MarketDepthRequest:
                        case DTCMessageType.SubmitNewSingleOrder:
                        case DTCMessageType.SubmitNewSingleOrderInt:
                        case DTCMessageType.SubmitNewOcoOrder:
                        case DTCMessageType.SubmitNewOcoOrderInt:
                        case DTCMessageType.CancelOrder:
                        case DTCMessageType.CancelReplaceOrder:
                        case DTCMessageType.CancelReplaceOrderInt:
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="timeout">The time (in milliseconds) to wait for a response before giving up</param>
        /// <returns>the SecurityDefinitionResponse, or null if not received before timeout</returns>
        public async Task<SecurityDefinitionResponse> GetSecurityDefinitionAsync(string symbol, int timeout = 1000)
        {
            // Set up the handler to capture the event
            var startTime = DateTime.Now;
            SecurityDefinitionResponse result = null;
            EventHandler<EventArgs<SecurityDefinitionResponse>> handler = null;
            handler = (s, e) =>
            {
                SecurityDefinitionResponseEvent -= handler; // unregister to avoid a potential memory leak
                result = e.Data;
            };
            SecurityDefinitionResponseEvent += handler;

            // Send the request
            var securityDefinitionForSymbolRequest = new SecurityDefinitionForSymbolRequest
            {
                RequestID = NextRequestId,
                Symbol = symbol
            };
            SendRequest(DTCMessageType.SecurityDefinitionForSymbolRequest, securityDefinitionForSymbolRequest);

            // Wait until the response is received or until timeout
            while (result == null && (DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                await Task.Delay(1);
            }
            return result;
        }
    }
}
