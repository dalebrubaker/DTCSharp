using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DTCCommon;
using DTCCommon.Codecs;
using DTCCommon.Exceptions;
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
        private ICodecDTC _currentCodec;
        private CancellationTokenSource _cts;
        private int _nextRequestId;
        private uint _nextSymbolId;
        private bool _isHistoricalClient;
        private string _clientName;

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
        public uint NextSymbolId => ++_nextSymbolId;

        /// <summary>
        /// Key is "Symbol|Exchange built by Get
        /// </summary>
        public ConcurrentDictionary<string, uint> SymbolIdBySymbolExchangeCombo { get; set; }

        public ConcurrentDictionary<uint, string> SymbolExchangeComboBySymbolId { get; set; }

        public string Server => _server;

        public int Port => _port;

        public string ClientName => _clientName;

        public Client(string server, int port)
        {
            _server = server;
            _port = port;
            SymbolIdBySymbolExchangeCombo = new ConcurrentDictionary<string, uint>();
            SymbolExchangeComboBySymbolId = new ConcurrentDictionary<uint, string>();
            _heartbeatTimer = new Timer(10000);
            _heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
            _currentCodec = new CodecBinary();
            HeartbeatEvent += Client_HeartbeatEvent;
        }

        private void Client_HeartbeatEvent(object sender, EventArgs<Heartbeat> e)
        {
            _lastHeartbeatReceivedTime = DateTime.Now;
        }

        private void HeartbeatTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_isHistoricalClient)
            {
                return;
            }
            var maxWaitForHeartbeatTime = TimeSpan.FromMilliseconds(Math.Max(_heartbeatTimer.Interval * 2, 5000));
            var timeSinceHeartbeat = (DateTime.Now - _lastHeartbeatReceivedTime);
            if (timeSinceHeartbeat > maxWaitForHeartbeatTime)
            {
                Dispose(true);
                throw new DTCSharpException("Too long since Server sent us a heartbeat. Closing client: " + _clientName);
            }

            // Send a heartbeat to the server
            var heartbeat = new Heartbeat();
            SendMessage(DTCMessageType.Heartbeat, heartbeat);
        }

        #region events

        public event EventHandler<EventArgs<EncodingResponse>> EncodingResponseEvent;
        public event EventHandler<EventArgs<Heartbeat>> HeartbeatEvent;
        public event EventHandler<EventArgs<Logoff>> LogoffEvent;
        public event EventHandler<EventArgs<LogonResponse>> LogonResponseEvent;
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
        /// <param name="requestedEncoding"></param>
        /// <param name="timeout">The time (in milliseconds) to wait for a response before giving up</param>
        /// <param name="cancellationToken">optional token to stop receiving messages</param>
        /// <returns><c>true</c> if successful. <c>false</c> means protocol buffers are not supported by server</returns>
        private async Task<EncodingResponse> ConnectAsync(EncodingEnum requestedEncoding, int timeout = 1000, CancellationToken cancellationToken = default(CancellationToken))
        {
            _tcpClient = new TcpClient {NoDelay = true};
            await _tcpClient.ConnectAsync(_server, _port); // connect to the server
            _networkStream = _tcpClient.GetStream();
            _binaryWriter = new BinaryWriter(_networkStream);
            _currentCodec = new CodecBinary();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            // Fire and forget
            Task.Run(MessageReader, _cts.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            // Set up the handler to capture the event
            var startTime = DateTime.Now;
            EncodingResponse result = null;
            EventHandler<EventArgs<EncodingResponse>> handler = null;
            handler = (s, e) =>
            {
                EncodingResponseEvent -= handler; // unregister to avoid a potential memory leak
                result = e.Data;
            };
            EncodingResponseEvent += handler;

            // Request protocol buffers encoding
            var encodingRequest = new EncodingRequest
            {
                Encoding = requestedEncoding,
                ProtocolType = "DTC",
                ProtocolVersion = (int)DTCVersion.CurrentVersion
            };
            SendMessage(DTCMessageType.EncodingRequest, encodingRequest);

            // Wait until the response is received or until timeout
            while (result == null && (DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                await Task.Delay(1, cancellationToken);
            }
            if (!_isHistoricalClient)
            {
                // start the heartbeat
                _lastHeartbeatReceivedTime = DateTime.Now;
                _heartbeatTimer.Start();
            }
            return result;
        }

        /// <summary>
        /// Start a TCP connection and send a Logon request to the server. 
        /// If isHistoricalClient, will only use BinaryEncoding and won't do a heartbeat. See: http://www.sierrachart.com/index.php?page=doc/DTCServer.php#HistoricalPriceDataServer
        /// </summary>
        /// <param name="requestedEncoding"></param>
        /// <param name="heartbeatIntervalInSeconds">The interval in seconds that each side, the Client and the Server, needs to use to send HEARTBEAT messages to the other side. This should be a value from anywhere from 5 to 60 seconds.</param>
        /// <param name="isHistoricalClient"><c>true</c> means binary encoding only, and no heartbeat</param>
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
        /// <param name="cancellationTokenSource"></param>
        /// <returns>The LogonResponse, or null if not received before timeout</returns>
        public async Task<LogonResponse> LogonAsync(EncodingEnum requestedEncoding, int heartbeatIntervalInSeconds, bool isHistoricalClient = false, int timeout = 1000, string clientName = "",
            string userName = "", string password = "", string generalTextData = "", int integer1 = 0, int integer2 = 0, TradeModeEnum tradeMode = TradeModeEnum.TradeModeUnset, string tradeAccount = "",
            string hardwareIdentifier = "", CancellationTokenSource cancellationTokenSource = null)
        {
            _isHistoricalClient = isHistoricalClient;
            _clientName = clientName;

            // Make a connection
            if (_isHistoricalClient)
            {
                _heartbeatTimer.Interval = heartbeatIntervalInSeconds * 1000;
            }
            _cts = cancellationTokenSource?? new CancellationTokenSource();
            var encodingResponse = await ConnectAsync(requestedEncoding, timeout, _cts.Token);

            // Set up the handler to capture the event
            var startTime = DateTime.Now;
            LogonResponse result = null;
            EventHandler<EventArgs<LogonResponse>> handler = null;
            handler = (s, e) =>
            {
                LogonResponseEvent -= handler; // unregister to avoid a potential memory leak
                result = e.Data;
            };
            LogonResponseEvent += handler;

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
            SendMessage(DTCMessageType.LogonRequest, logonRequest);

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
                _cts?.Cancel();
                _binaryWriter?.Dispose();
                _tcpClient?.Dispose();
                _heartbeatTimer?.Dispose();
                _isDisposed = true;
            }
        }

        /// <summary>
        /// Send the message
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        public void SendMessage<T>(DTCMessageType messageType, T message) where T : IMessage
        {
            try
            {
                _currentCodec.Write(messageType, message, _binaryWriter);
            }
            catch (Exception ex)
            {
                throw new DTCSharpException(ex.Message);
            }
        }

        private void ThrowEvent<T>(T message, EventHandler<EventArgs<T>> eventForMessage) where T : IMessage
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
                        size = binaryReader.ReadUInt16();
                    }
                    catch (Exception ex)
                    {

                        throw;
                    }
                    var messageType = (DTCMessageType)binaryReader.ReadUInt16();
                    var bytes = binaryReader.ReadBytes(size - 4); // size included the header size+type
                    switch (messageType)
                    {
                        case DTCMessageType.LogonResponse:
                            LogonResponse = _currentCodec.Load<LogonResponse>(messageType, bytes);
                            ThrowEvent(LogonResponse, LogonResponseEvent);
                            break;
                        case DTCMessageType.Heartbeat:
                            var heartbeat = _currentCodec.Load<Heartbeat>(messageType, bytes);
                            ThrowEvent(heartbeat, HeartbeatEvent);
                            break;
                        case DTCMessageType.Logoff:
                            var logoff = _currentCodec.Load<Logoff>(messageType, bytes);
                            ThrowEvent(logoff, LogoffEvent);
                            break;
                        case DTCMessageType.EncodingResponse:
                            // Note that we must use binary encoding here on the first usage after connect, 
                            //    per http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#EncodingRequest
                            var encodingResponse = _currentCodec.Load<EncodingResponse>(messageType, bytes);
                            switch (encodingResponse.Encoding)
                            {
                                case EncodingEnum.BinaryEncoding:
                                    _currentCodec = new CodecBinary();
                                    break;
                                case EncodingEnum.BinaryWithVariableLengthStrings:
                                    throw new NotImplementedException($"Not implemented in {nameof(MessageReader)}: {nameof(encodingResponse.Encoding)}"); ;
                                case EncodingEnum.JsonEncoding:
                                    throw new NotImplementedException($"Not implemented in {nameof(MessageReader)}: {nameof(encodingResponse.Encoding)}"); ;
                                case EncodingEnum.JsonCompactEncoding:
                                    throw new NotImplementedException($"Not implemented in {nameof(MessageReader)}: {nameof(encodingResponse.Encoding)}"); ;
                                case EncodingEnum.ProtocolBuffers:
                                    _currentCodec = new CodecProtobuf();
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            ThrowEvent(encodingResponse, EncodingResponseEvent);
                            break;
                        case DTCMessageType.MarketDataReject:
                            var marketDataReject = _currentCodec.Load<MarketDataReject>(messageType, bytes);
                            ThrowEvent(marketDataReject, MarketDataRejectEvent);
                            break;
                        case DTCMessageType.MarketDataSnapshot:
                            var marketDataSnapshot = _currentCodec.Load<MarketDataSnapshot>(messageType, bytes);
                            ThrowEvent(marketDataSnapshot, MarketDataSnapshotEvent);
                            break;
                        case DTCMessageType.MarketDataSnapshotInt:
                            var marketDataSnapshotInt = _currentCodec.Load<MarketDataSnapshot_Int>(messageType, bytes);
                            ThrowEvent(marketDataSnapshotInt, MarketDataSnapshotIntEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateTrade:
                            var marketDataUpdateTrade = _currentCodec.Load<MarketDataUpdateTrade>(messageType, bytes);
                            ThrowEvent(marketDataUpdateTrade, MarketDataUpdateTradeEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateTradeCompact:
                            var marketDataUpdateTradeCompact = _currentCodec.Load<MarketDataUpdateTradeCompact>(messageType, bytes);
                            ThrowEvent(marketDataUpdateTradeCompact, MarketDataUpdateTradeCompactEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateTradeInt:
                            var marketDataUpdateTradeInt = _currentCodec.Load<MarketDataUpdateTrade_Int>(messageType, bytes);
                            ThrowEvent(marketDataUpdateTradeInt, MarketDataUpdateTradeIntEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                            var marketDataUpdateLastTradeSnapshot = _currentCodec.Load<MarketDataUpdateLastTradeSnapshot>(messageType, bytes);
                            ThrowEvent(marketDataUpdateLastTradeSnapshot, MarketDataUpdateLastTradeSnapshotEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateBidAsk:
                            var marketDataUpdateBidAsk = _currentCodec.Load<MarketDataUpdateBidAsk>(messageType, bytes);
                            ThrowEvent(marketDataUpdateBidAsk, MarketDataUpdateBidAskEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateBidAskCompact:
                            var marketDataUpdateBidAskCompact = _currentCodec.Load<MarketDataUpdateBidAskCompact>(messageType, bytes);
                            ThrowEvent(marketDataUpdateBidAskCompact, MarketDataUpdateBidAskCompactEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateBidAskInt:
                            var marketDataUpdateBidAskInt = _currentCodec.Load<MarketDataUpdateBidAsk_Int>(messageType, bytes);
                            ThrowEvent(marketDataUpdateBidAskInt, MarketDataUpdateBidAskIntEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionOpen:
                            var marketDataUpdateSessionOpen = _currentCodec.Load<MarketDataUpdateSessionOpen>(messageType, bytes);
                            ThrowEvent(marketDataUpdateSessionOpen, MarketDataUpdateSessionOpenEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionOpenInt:
                            var marketDataUpdateSessionOpenInt = _currentCodec.Load<MarketDataUpdateSessionOpen_Int>(messageType, bytes);
                            ThrowEvent(marketDataUpdateSessionOpenInt, MarketDataUpdateSessionOpenIntEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionHigh:
                            var marketDataUpdateSessionHigh = _currentCodec.Load<MarketDataUpdateSessionHigh>(messageType, bytes);
                            ThrowEvent(marketDataUpdateSessionHigh, MarketDataUpdateSessionHighEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionHighInt:
                            var marketDataUpdateSessionHighInt = _currentCodec.Load<MarketDataUpdateSessionHigh_Int>(messageType, bytes);
                            ThrowEvent(marketDataUpdateSessionHighInt, MarketDataUpdateSessionHighIntEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionLow:
                            var marketDataUpdateSessionLow = _currentCodec.Load<MarketDataUpdateSessionLow>(messageType, bytes);
                            ThrowEvent(marketDataUpdateSessionLow, MarketDataUpdateSessionLowEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionLowInt:
                            var marketDataUpdateSessionLowInt = _currentCodec.Load<MarketDataUpdateSessionLow_Int>(messageType, bytes);
                            ThrowEvent(marketDataUpdateSessionLowInt, MarketDataUpdateSessionLowIntEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionVolume:
                            var marketDataUpdateSessionVolume = _currentCodec.Load<MarketDataUpdateSessionVolume>(messageType, bytes);
                            ThrowEvent(marketDataUpdateSessionVolume, MarketDataUpdateSessionVolumeEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateOpenInterest:
                            var marketDataUpdateOpenInterest = _currentCodec.Load<MarketDataUpdateOpenInterest>(messageType, bytes);
                            ThrowEvent(marketDataUpdateOpenInterest, MarketDataUpdateOpenInterestEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionSettlement:
                            var marketDataUpdateSessionSettlement = _currentCodec.Load<MarketDataUpdateSessionSettlement>(messageType, bytes);
                            ThrowEvent(marketDataUpdateSessionSettlement, MarketDataUpdateSessionSettlementEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                            var marketDataUpdateSessionSettlementInt = _currentCodec.Load<MarketDataUpdateSessionSettlement_Int>(messageType, bytes);
                            ThrowEvent(marketDataUpdateSessionSettlementInt, MarketDataUpdateSessionSettlementIntEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateSessionNumTrades:
                            var marketDataUpdateSessionNumTrades = _currentCodec.Load<MarketDataUpdateSessionNumTrades>(messageType, bytes);
                            ThrowEvent(marketDataUpdateSessionNumTrades, MarketDataUpdateSessionNumTradesEvent);
                            break;
                        case DTCMessageType.MarketDataUpdateTradingSessionDate:
                            var marketDataUpdateTradingSessionDate = _currentCodec.Load<MarketDataUpdateTradingSessionDate>(messageType, bytes);
                            ThrowEvent(marketDataUpdateTradingSessionDate, MarketDataUpdateTradingSessionDateEvent);
                            break;
                        case DTCMessageType.MarketDepthReject:
                            var marketDepthReject = _currentCodec.Load<MarketDepthReject>(messageType, bytes);
                            ThrowEvent(marketDepthReject, MarketDepthRejectEvent);
                            break;
                        case DTCMessageType.MarketDepthSnapshotLevel:
                            var marketDepthSnapshotLevel = _currentCodec.Load<MarketDepthSnapshotLevel>(messageType, bytes);
                            ThrowEvent(marketDepthSnapshotLevel, MarketDepthSnapshotLevelEvent);
                            break;
                        case DTCMessageType.MarketDepthSnapshotLevelInt:
                            var marketDepthSnapshotLevelInt = _currentCodec.Load<MarketDepthSnapshotLevel_Int>(messageType, bytes);
                            ThrowEvent(marketDepthSnapshotLevelInt, MarketDepthSnapshotLevelIntEvent);
                            break;
                        case DTCMessageType.MarketDepthUpdateLevel:
                            var marketDepthUpdateLevel = _currentCodec.Load<MarketDepthUpdateLevel>(messageType, bytes);
                            ThrowEvent(marketDepthUpdateLevel, MarketDepthUpdateLevelEvent);
                            break;
                        case DTCMessageType.MarketDepthUpdateLevelCompact:
                            var marketDepthUpdateLevelCompact = _currentCodec.Load<MarketDepthUpdateLevelCompact>(messageType, bytes);
                            ThrowEvent(marketDepthUpdateLevelCompact, MarketDepthUpdateLevelCompactEvent);
                            break;
                        case DTCMessageType.MarketDepthUpdateLevelInt:
                            var marketDepthUpdateLevelInt = _currentCodec.Load<MarketDepthUpdateLevel_Int>(messageType, bytes);
                            ThrowEvent(marketDepthUpdateLevelInt, MarketDepthUpdateLevelIntEvent);
                            break;
                        case DTCMessageType.MarketDepthFullUpdate10:
                            var marketDepthFullUpdate10 = _currentCodec.Load<MarketDepthFullUpdate10>(messageType, bytes);
                            ThrowEvent(marketDepthFullUpdate10, MarketDepthFullUpdate10Event);
                            break;
                        case DTCMessageType.MarketDepthFullUpdate20:
                            var marketDepthFullUpdate20 = _currentCodec.Load<MarketDepthFullUpdate20>(messageType, bytes);
                            ThrowEvent(marketDepthFullUpdate20, MarketDepthFullUpdate20Event);
                            break;
                        case DTCMessageType.MarketDataFeedStatus:
                            var marketDataFeedStatus = _currentCodec.Load<MarketDataFeedStatus>(messageType, bytes);
                            ThrowEvent(marketDataFeedStatus, MarketDataFeedStatusEvent);
                            break;
                        case DTCMessageType.MarketDataFeedSymbolStatus:
                            var marketDataFeedSymbolStatus = _currentCodec.Load<MarketDataFeedSymbolStatus>(messageType, bytes);
                            ThrowEvent(marketDataFeedSymbolStatus, MarketDataFeedSymbolStatusEvent);
                            break;
                        case DTCMessageType.OpenOrdersReject:
                            var openOrdersReject = _currentCodec.Load<OpenOrdersReject>(messageType, bytes);
                            ThrowEvent(openOrdersReject, OpenOrdersRejectEvent);
                            break;
                        case DTCMessageType.OrderUpdate:
                            var orderUpdate = _currentCodec.Load<OrderUpdate>(messageType, bytes);
                            ThrowEvent(orderUpdate, OrderUpdateEvent);
                            break;
                        case DTCMessageType.HistoricalOrderFillResponse:
                            var historicalOrderFillResponse = _currentCodec.Load<HistoricalOrderFillResponse>(messageType, bytes);
                            ThrowEvent(historicalOrderFillResponse, HistoricalOrderFillResponseEvent);
                            break;
                        case DTCMessageType.CurrentPositionsReject:
                            var currentPositionsReject = _currentCodec.Load<CurrentPositionsReject>(messageType, bytes);
                            ThrowEvent(currentPositionsReject, CurrentPositionsRejectEvent);
                            break;
                        case DTCMessageType.PositionUpdate:
                            var positionUpdate = _currentCodec.Load<PositionUpdate>(messageType, bytes);
                            ThrowEvent(positionUpdate, PositionUpdateEvent);
                            break;
                        case DTCMessageType.TradeAccountResponse:
                            var tradeAccountResponse = _currentCodec.Load<TradeAccountResponse>(messageType, bytes);
                            ThrowEvent(tradeAccountResponse, TradeAccountResponseEvent);
                            break;
                        case DTCMessageType.ExchangeListResponse:
                            var exchangeListResponse = _currentCodec.Load<ExchangeListResponse>(messageType, bytes);
                            ThrowEvent(exchangeListResponse, ExchangeListResponseEvent);
                            break;
                        case DTCMessageType.SecurityDefinitionResponse:
                            var securityDefinitionResponse = _currentCodec.Load<SecurityDefinitionResponse>(messageType, bytes);
                            ThrowEvent(securityDefinitionResponse, SecurityDefinitionResponseEvent);
                            break;
                        case DTCMessageType.SecurityDefinitionReject:
                            var securityDefinitionReject = _currentCodec.Load<SecurityDefinitionReject>(messageType, bytes);
                            ThrowEvent(securityDefinitionReject, SecurityDefinitionRejectEvent);
                            break;
                        case DTCMessageType.AccountBalanceReject:
                            var accountBalanceReject = _currentCodec.Load<AccountBalanceReject>(messageType, bytes);
                            ThrowEvent(accountBalanceReject, AccountBalanceRejectEvent);
                            break;
                        case DTCMessageType.AccountBalanceUpdate:
                            var accountBalanceUpdate = _currentCodec.Load<AccountBalanceUpdate>(messageType, bytes);
                            ThrowEvent(accountBalanceUpdate, AccountBalanceUpdateEvent);
                            break;
                        case DTCMessageType.UserMessage:
                            var userMessage = _currentCodec.Load<UserMessage>(messageType, bytes);
                            ThrowEvent(userMessage, UserMessageEvent);
                            break;
                        case DTCMessageType.GeneralLogMessage:
                            var generalLogMessage = _currentCodec.Load<GeneralLogMessage>(messageType, bytes);
                            ThrowEvent(generalLogMessage, GeneralLogMessageEvent);
                            break;
                        case DTCMessageType.HistoricalPriceDataResponseHeader:
                            var historicalPriceDataResponseHeader = _currentCodec.Load<HistoricalPriceDataResponseHeader>(messageType, bytes);
                            ThrowEvent(historicalPriceDataResponseHeader, HistoricalPriceDataResponseHeaderEvent);
                            break;
                        case DTCMessageType.HistoricalPriceDataReject:
                            var historicalPriceDataReject = _currentCodec.Load<HistoricalPriceDataReject>(messageType, bytes);
                            ThrowEvent(historicalPriceDataReject, HistoricalPriceDataRejectEvent);
                            break;
                        case DTCMessageType.HistoricalPriceDataRecordResponse:
                            var historicalPriceDataRecordResponse = _currentCodec.Load<HistoricalPriceDataRecordResponse>(messageType, bytes);
                            ThrowEvent(historicalPriceDataRecordResponse, HistoricalPriceDataRecordResponseEvent);
                            break;
                        case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                            var historicalPriceDataTickRecordResponse = _currentCodec.Load<HistoricalPriceDataTickRecordResponse>(messageType, bytes);
                            ThrowEvent(historicalPriceDataTickRecordResponse, HistoricalPriceDataTickRecordResponseEvent);
                            break;
                        case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                            var historicalPriceDataRecordResponseInt = _currentCodec.Load<HistoricalPriceDataRecordResponse_Int>(messageType, bytes);
                            ThrowEvent(historicalPriceDataRecordResponseInt, HistoricalPriceDataRecordResponseIntEvent);
                            break;
                        case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                            var historicalPriceDataTickRecordResponseInt = _currentCodec.Load<HistoricalPriceDataTickRecordResponse_Int>(messageType, bytes);
                            ThrowEvent(historicalPriceDataTickRecordResponseInt, HistoricalPriceDataTickRecordResponseIntEvent);
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
                            throw new ArgumentOutOfRangeException($"Unexpected MessageType {messageType} received by {_clientName} {nameof(MessageReader)}.");
                    }
                }
            }
        }

        /// <summary>
        /// Get the SecurityDefinitionResponse for symbol, or null if not received before timeout
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
            SendMessage(DTCMessageType.SecurityDefinitionForSymbolRequest, securityDefinitionForSymbolRequest);

            // Wait until the response is received or until timeout
            while (result == null && (DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                await Task.Delay(1);
            }
            return result;
        }

        /// <summary>
        /// Return symbol|exchange
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="exchange"></param>
        /// <returns></returns>
        public string CombineSymbolExchange(string symbol, string exchange)
        {
            return $"{symbol}|{exchange}";
        }

        /// <summary>
        /// Split symbol|exchange.
        /// </summary>
        /// <param name="combo"></param>
        /// <param name="symbol"></param>
        /// <param name="exchange"></param>
        public void SplitSymbolExchange(string combo, out string symbol, out string exchange)
        {
            if (string.IsNullOrEmpty(combo))
            {
                symbol = null;
                exchange = null;
                return;
            }
            var splits = combo.Split('|');
            symbol = splits[0];
            exchange = splits[1];
        }

        /// <summary>
        /// Get the symbol and exchange for symbolId, or null if not found
        /// </summary>
        /// <param name="symbolId"></param>
        /// <param name="symbol"></param>
        /// <param name="exchange"></param>
        public void GetSymbolExchangeForSymbolId(uint symbolId, out string symbol, out string exchange)
        {
            string combo;
            SymbolExchangeComboBySymbolId.TryGetValue(symbolId, out combo);
            SplitSymbolExchange(combo, out symbol, out exchange);
        }

        /// <summary>
        /// Get the symbolId for symbol and exchange, or 0 if not found
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="exchange"></param>
        /// <returns></returns>
        public uint GetSymbolId(string symbol, string exchange)
        {
            var combo = CombineSymbolExchange(symbol, exchange);
            uint symbolId;
            if (!SymbolIdBySymbolExchangeCombo.TryGetValue(combo, out symbolId))
            {
                return 0;
            }
            SplitSymbolExchange(combo, out symbol, out exchange);
            return symbolId;
        }

        /// <summary>
        /// Request market data for symbol|exchange
        /// Add a symbolId if not already assigned 
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="exchange"></param>
        /// <returns>the symbol ID for symbol|exchange, or 0 if not logged on or market data is not supported</returns>
        public uint SubscribeMarketData(string symbol, string exchange)
        {
            if (LogonResponse == null || LogonResponse.MarketDataSupported == 0)
            {
                return 0;
            }
            var combo = CombineSymbolExchange(symbol, exchange);
            uint symbolId;
            if (!SymbolIdBySymbolExchangeCombo.TryGetValue(combo, out symbolId))
            {
                symbolId = NextSymbolId;
                SymbolIdBySymbolExchangeCombo[combo] = symbolId;
                SymbolExchangeComboBySymbolId[symbolId] = combo;
            }
            var request = new MarketDataRequest
            {
                RequestAction = RequestActionEnum.Subscribe,
                SymbolID = symbolId,
                Symbol = symbol,
                Exchange = exchange
            };
            SendMessage(DTCMessageType.MarketDataRequest, request);
            return symbolId;
        }

        public void UnsubscribeMarketData(uint symbolId)
        {
            var request = new MarketDataRequest
            {
                RequestAction = RequestActionEnum.Unsubscribe,
                SymbolID = symbolId,
            };
            SendMessage(DTCMessageType.MarketDataRequest, request);
        }
    }
}
