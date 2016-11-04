using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DTCCommon;
using DTCPB;
using Google.Protobuf;
using Timer = System.Timers.Timer;

namespace DTCServer
{
    public class Server : IDisposable
    {
        private readonly int _port;
        private readonly int _timeoutNoActivity;
        private readonly bool _useHeartbeat;
        private readonly Action<ClientHandler, DTCMessageType, IMessage> _callback;
        private readonly IPAddress _ipAddress;
        private TcpListener _tcpListener;
        private readonly Timer _timerCheckForDisconnects;
        private bool _isDisposed;
        private readonly CancellationTokenSource _cts;

        private readonly object _lock;
        private readonly List<Task> _clientHandlerTasks;
        private readonly List<ClientHandler> _clientHandlers; // parallel list to _clientHandlerTasks

        /// <summary>
        /// Start a TCP Listener on port at ipAddress
        /// </summary>
        /// <param name="callback">the callback for all client requests</param>
        /// <param name="port"></param>
        /// <param name="timeoutNoActivity">milliseconds timeout to assume disconnected if no activity</param>
        /// <param name="useHeartbeat">Don't send heartbeats. Used for sending zipped historical data</param>
        /// <param name="ipAddress"></param>
        public Server(Action<ClientHandler, DTCMessageType, IMessage> callback, IPAddress ipAddress, int port, int timeoutNoActivity, bool useHeartbeat)
        {
            _callback = callback;
            _ipAddress = ipAddress;
            _port = port;
            _timeoutNoActivity = timeoutNoActivity;
            _useHeartbeat = useHeartbeat;
            _clientHandlerTasks = new List<Task>();
            _clientHandlers = new List<ClientHandler>();
            _lock = new object();
            _timerCheckForDisconnects = new Timer(1000);
            _timerCheckForDisconnects.Elapsed += TimerCheckForDisconnects_Elapsed;
            _cts = new CancellationTokenSource();
            Address = new IPEndPoint(_ipAddress, _port).ToString();
        }

        public string Address { get;  }

        private void TimerCheckForDisconnects_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            RemoveDisconnectedClientHandlers();
        }

        private void RemoveDisconnectedClientHandlers()
        {
            lock (_lock)
            {
                for (int i = 0; i < _clientHandlerTasks.Count; i++)
                {
                    var task = _clientHandlerTasks[i];
                    if (!task.IsCompleted)
                    {
                        continue;
                    }
                    var clientHandler = _clientHandlers[i];
                    clientHandler.Dispose(); // ClientHandler.Dispose() also closes tcpClient
                    OnClientDisconnected(clientHandler);
                    _clientHandlerTasks.RemoveAt(i);
                    _clientHandlers.RemoveAt(i);
                    i--;
                }
            }
        }
        public int NumberOfClientHandlers
        {
            get
            {
                RemoveDisconnectedClientHandlers();
                return _clientHandlerTasks.Count;
            }
        }

        public bool IsConnected { get; private set; }

        #region events


        public event EventHandler<EventArgs<ClientHandler>> ClientConnected;

        private void OnClientConnected(ClientHandler clientHandler)
        {
            var temp = ClientConnected;
            temp?.Invoke(this, new EventArgs<ClientHandler>(clientHandler));
        }

        public event EventHandler<EventArgs<ClientHandler>> ClientDisconnected;

        private void OnClientDisconnected(ClientHandler clientHandler)
        {
            var temp = ClientDisconnected;
            temp?.Invoke(this, new EventArgs<ClientHandler>(clientHandler));
        }


        public event EventHandler<EventArgs<Heartbeat, DTCMessageType, ClientHandler>> HeartbeatEvent;
        public event EventHandler<EventArgs<Logoff, DTCMessageType, ClientHandler>> LogoffEvent;

        /// <summary>
        /// This event is only thrown for informational purposes. 
        /// HandleMessage() takes care of changing the current encoding and responding.
        /// So do NOT respond to this event.
        /// </summary>
        public event EventHandler<EventArgs<EncodingRequest>> EncodingRequestEvent;


        public event EventHandler<EventArgs<LogonRequest, DTCMessageType, ClientHandler>> LogonRequestEvent;
        public event EventHandler<EventArgs<MarketDataRequest, DTCMessageType, ClientHandler>> MarketDataRequestEvent;
        public event EventHandler<EventArgs<MarketDepthRequest, DTCMessageType, ClientHandler>> MarketDepthRequestEvent;
        public event EventHandler<EventArgs<SubmitNewSingleOrder, DTCMessageType, ClientHandler>> SubmitNewSingleOrderEvent;
        public event EventHandler<EventArgs<SubmitNewSingleOrderInt, DTCMessageType, ClientHandler>> SubmitNewSingleOrderIntEvent;
        public event EventHandler<EventArgs<SubmitNewOCOOrder, DTCMessageType, ClientHandler>> SubmitNewOcoOrderEvent;
        public event EventHandler<EventArgs<SubmitNewOCOOrderInt, DTCMessageType, ClientHandler>> SubmitNewOcoOrderIntEvent;
        public event EventHandler<EventArgs<CancelOrder, DTCMessageType, ClientHandler>> CancelOrderEvent;
        public event EventHandler<EventArgs<CancelReplaceOrder, DTCMessageType, ClientHandler>> CancelReplaceOrderEvent;
        public event EventHandler<EventArgs<CancelReplaceOrderInt, DTCMessageType, ClientHandler>> CancelReplaceOrderIntEvent;
        public event EventHandler<EventArgs<OpenOrdersRequest, DTCMessageType, ClientHandler>> OpenOrdersRequestEvent;
        public event EventHandler<EventArgs<HistoricalOrderFillsRequest, DTCMessageType, ClientHandler>> HistoricalOrderFillsRequestEvent;
        public event EventHandler<EventArgs<CurrentPositionsRequest, DTCMessageType, ClientHandler>> CurrentPositionsRequestEvent;
        public event EventHandler<EventArgs<TradeAccountsRequest, DTCMessageType, ClientHandler>> TradeAccountsRequestEvent;
        public event EventHandler<EventArgs<ExchangeListRequest, DTCMessageType, ClientHandler>> ExchangeListRequestEvent;
        public event EventHandler<EventArgs<SymbolsForExchangeRequest, DTCMessageType, ClientHandler>> SymbolsForExchangeRequestEvent;
        public event EventHandler<EventArgs<UnderlyingSymbolsForExchangeRequest, DTCMessageType, ClientHandler>> UnderlyingSymbolsForExchangeRequestEvent;
        public event EventHandler<EventArgs<SymbolsForUnderlyingRequest, DTCMessageType, ClientHandler>> SymbolsForUnderlyingRequestEvent;
        public event EventHandler<EventArgs<SecurityDefinitionForSymbolRequest, DTCMessageType, ClientHandler>> SecurityDefinitionForSymbolRequestEvent;
        public event EventHandler<EventArgs<SymbolSearchRequest, DTCMessageType, ClientHandler>> SymbolSearchRequestEvent;
        public event EventHandler<EventArgs<AccountBalanceRequest, DTCMessageType, ClientHandler>> AccountBalanceRequestEvent;
        public event EventHandler<EventArgs<HistoricalPriceDataRequest, DTCMessageType, ClientHandler>> HistoricalPriceDataRequestEvent;

        #endregion events

        private void ThrowEvent<T>(T message, EventHandler<EventArgs<T, DTCMessageType, ClientHandler>> eventForMessage, DTCMessageType messageType, ClientHandler clientHandler) where T : IMessage
        {
            var temp = eventForMessage; // for thread safety
            temp?.Invoke(this, new EventArgs<T, DTCMessageType, ClientHandler>(message, messageType, clientHandler));
        }


        /// <summary>
        /// This method is called for every request received by a client connected to this server.
        /// You must not block this thread for long, as further requests can't be received until you return from this method.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clientHandler">The handler for a particular client connected to this server</param>
        /// <param name="messageType">the message type</param>
        /// <param name="message">the message (a Google.Protobuf message)</param>
        /// <returns></returns>
        public void HandleRequest<T>(ClientHandler clientHandler, DTCMessageType messageType, T message) where T : IMessage
        {
            switch (messageType)
            {
                case DTCMessageType.LogonRequest:
                    var logonRequest = message as LogonRequest;
                    ThrowEvent(logonRequest, LogonRequestEvent, messageType, clientHandler);
                    break;
                case DTCMessageType.Heartbeat:
                    var heartbeat = message as Heartbeat;
                    ThrowEvent(heartbeat, HeartbeatEvent, messageType, clientHandler);
                    break;
                case DTCMessageType.Logoff:
                    var logoff = message as Logoff;
                    ThrowEvent(logoff, LogoffEvent, messageType, clientHandler);
                    break;
                case DTCMessageType.EncodingRequest:
                    // This is an exception where we don't make a callback. 
                    //     This requires an immediate response using BinaryEncoding then set the _currentCodec before another message can be processed
                    var encodingRequest = _currentCodec.Load<EncodingRequest>(messageType, messageBytes);
                    var newEncoding = EncodingEnum.BinaryEncoding;
                    switch (encodingRequest.Encoding)
                    {
                        case EncodingEnum.BinaryEncoding:
                            break;
                        case EncodingEnum.BinaryWithVariableLengthStrings:
                        case EncodingEnum.JsonEncoding:
                        case EncodingEnum.JsonCompactEncoding:
                            // not supported. Ignore
                            break;
                        case EncodingEnum.ProtocolBuffers:
                            newEncoding = EncodingEnum.ProtocolBuffers;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    var encodingResponse = new EncodingResponse
                    {
                        ProtocolType = encodingRequest.ProtocolType,
                        ProtocolVersion = encodingRequest.ProtocolVersion,
                        Encoding = newEncoding
                    };
                    SendResponse(DTCMessageType.EncodingResponse, encodingResponse);

                    // BE SURE to set this immediately AFTER the SendResponse line above
                    SetCurrentCodec(encodingResponse.Encoding);

                    // send this to the callback for informational purposes
                    ThrowEvent(encodingRequest, EncodingRequestEvent);
                    break;
                case DTCMessageType.MarketDataRequest:
                    var marketDataRequest = _currentCodec.Load<MarketDataRequest>(messageType, messageBytes);
                    ThrowEvent(marketDataRequest, MarketDataRequestEvent);
                    break;
                case DTCMessageType.MarketDepthRequest:
                    var marketDepthRequest = _currentCodec.Load<MarketDepthRequest>(messageType, messageBytes);
                    ThrowEvent(marketDepthRequest, MarketDepthRequestEvent);
                    break;
                case DTCMessageType.SubmitNewSingleOrder:
                    var submitNewSingleOrder = _currentCodec.Load<SubmitNewSingleOrder>(messageType, messageBytes);
                    ThrowEvent(submitNewSingleOrder, SubmitNewSingleOrderEvent);
                    break;
                case DTCMessageType.SubmitNewSingleOrderInt:
                    var submitNewSingleOrderInt = _currentCodec.Load<SubmitNewSingleOrderInt>(messageType, messageBytes);
                    ThrowEvent(submitNewSingleOrderInt, SubmitNewSingleOrderIntEvent);
                    break;
                case DTCMessageType.SubmitNewOcoOrder:
                    var submitNewOcoOrder = _currentCodec.Load<SubmitNewOCOOrder>(messageType, messageBytes);
                    ThrowEvent(submitNewOcoOrder, SubmitNewOcoOrderEvent);
                    break;
                case DTCMessageType.SubmitNewOcoOrderInt:
                    var submitNewOcoOrderInt = _currentCodec.Load<SubmitNewOCOOrderInt>(messageType, messageBytes);
                    ThrowEvent(submitNewOcoOrderInt, SubmitNewOcoOrderIntEvent);
                    break;
                case DTCMessageType.CancelOrder:
                    var cancelOrder = _currentCodec.Load<CancelOrder>(messageType, messageBytes);
                    ThrowEvent(cancelOrder, CancelOrderEvent);
                    break;
                case DTCMessageType.CancelReplaceOrder:
                    var cancelReplaceOrder = _currentCodec.Load<CancelReplaceOrder>(messageType, messageBytes);
                    ThrowEvent(cancelReplaceOrder, CancelReplaceOrderEvent);
                    break;
                case DTCMessageType.CancelReplaceOrderInt:
                    var cancelReplaceOrderInt = _currentCodec.Load<CancelReplaceOrderInt>(messageType, messageBytes);
                    ThrowEvent(cancelReplaceOrderInt, CancelReplaceOrderIntEvent);
                    break;
                case DTCMessageType.OpenOrdersRequest:
                    var openOrdersRequest = _currentCodec.Load<OpenOrdersRequest>(messageType, messageBytes);
                    ThrowEvent(openOrdersRequest, OpenOrdersRequestEvent);
                    break;
                case DTCMessageType.HistoricalOrderFillsRequest:
                    var historicalOrderFillsRequest = _currentCodec.Load<HistoricalOrderFillsRequest>(messageType, messageBytes);
                    ThrowEvent(historicalOrderFillsRequest, HistoricalOrderFillsRequestEvent);
                    break;
                case DTCMessageType.CurrentPositionsRequest:
                    var currentPositionsRequest = _currentCodec.Load<CurrentPositionsRequest>(messageType, messageBytes);
                    ThrowEvent(currentPositionsRequest, CurrentPositionsRequestEvent);
                    break;
                case DTCMessageType.TradeAccountsRequest:
                    var tradeAccountsRequest = _currentCodec.Load<TradeAccountsRequest>(messageType, messageBytes);
                    ThrowEvent(tradeAccountsRequest, TradeAccountsRequestEvent);
                    break;
                case DTCMessageType.ExchangeListRequest:
                    var exchangeListRequest = _currentCodec.Load<ExchangeListRequest>(messageType, messageBytes);
                    ThrowEvent(exchangeListRequest, ExchangeListRequestEvent);
                    break;
                case DTCMessageType.SymbolsForExchangeRequest:
                    var symbolsForExchangeRequest = _currentCodec.Load<SymbolsForExchangeRequest>(messageType, messageBytes);
                    ThrowEvent(symbolsForExchangeRequest, SymbolsForExchangeRequestEvent);
                    break;
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                    var underlyingSymbolsForExchangeRequest = _currentCodec.Load<UnderlyingSymbolsForExchangeRequest>(messageType, messageBytes);
                    ThrowEvent(underlyingSymbolsForExchangeRequest, UnderlyingSymbolsForExchangeRequestEvent);
                    break;
                case DTCMessageType.SymbolsForUnderlyingRequest:
                    var symbolsForUnderlyingRequest = _currentCodec.Load<SymbolsForUnderlyingRequest>(messageType, messageBytes);
                    ThrowEvent(symbolsForUnderlyingRequest, SymbolsForUnderlyingRequestEvent);
                    break;
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                    var securityDefinitionForSymbolRequest = _currentCodec.Load<SecurityDefinitionForSymbolRequest>(messageType, messageBytes);
                    ThrowEvent(securityDefinitionForSymbolRequest, SecurityDefinitionForSymbolRequestEvent);
                    break;
                case DTCMessageType.SymbolSearchRequest:
                    var symbolSearchRequest = _currentCodec.Load<SymbolSearchRequest>(messageType, messageBytes);
                    ThrowEvent(symbolSearchRequest, SymbolSearchRequestEvent);
                    break;
                case DTCMessageType.AccountBalanceRequest:
                    var accountBalanceRequest = _currentCodec.Load<AccountBalanceRequest>(messageType, messageBytes);
                    ThrowEvent(accountBalanceRequest, AccountBalanceRequestEvent);
                    break;
                case DTCMessageType.HistoricalPriceDataRequest:
                    var historicalPriceDataRequest = _currentCodec.Load<HistoricalPriceDataRequest>(messageType, messageBytes);
                    ThrowEvent(historicalPriceDataRequest, HistoricalPriceDataRequestEvent);
                    break;
                case DTCMessageType.MessageTypeUnset:
                case DTCMessageType.LogonResponse:
                case DTCMessageType.EncodingResponse:
                case DTCMessageType.MarketDataReject:
                case DTCMessageType.MarketDataSnapshot:
                case DTCMessageType.MarketDataSnapshotInt:
                case DTCMessageType.MarketDataUpdateTrade:
                case DTCMessageType.MarketDataUpdateTradeCompact:
                case DTCMessageType.MarketDataUpdateTradeInt:
                case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                case DTCMessageType.MarketDataUpdateBidAsk:
                case DTCMessageType.MarketDataUpdateBidAskCompact:
                case DTCMessageType.MarketDataUpdateBidAskInt:
                case DTCMessageType.MarketDataUpdateSessionOpen:
                case DTCMessageType.MarketDataUpdateSessionOpenInt:
                case DTCMessageType.MarketDataUpdateSessionHigh:
                case DTCMessageType.MarketDataUpdateSessionHighInt:
                case DTCMessageType.MarketDataUpdateSessionLow:
                case DTCMessageType.MarketDataUpdateSessionLowInt:
                case DTCMessageType.MarketDataUpdateSessionVolume:
                case DTCMessageType.MarketDataUpdateOpenInterest:
                case DTCMessageType.MarketDataUpdateSessionSettlement:
                case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                case DTCMessageType.MarketDataUpdateSessionNumTrades:
                case DTCMessageType.MarketDataUpdateTradingSessionDate:
                case DTCMessageType.MarketDepthReject:
                case DTCMessageType.MarketDepthSnapshotLevel:
                case DTCMessageType.MarketDepthSnapshotLevelInt:
                case DTCMessageType.MarketDepthUpdateLevel:
                case DTCMessageType.MarketDepthUpdateLevelCompact:
                case DTCMessageType.MarketDepthUpdateLevelInt:
                case DTCMessageType.MarketDepthFullUpdate10:
                case DTCMessageType.MarketDepthFullUpdate20:
                case DTCMessageType.MarketDataFeedStatus:
                case DTCMessageType.MarketDataFeedSymbolStatus:
                case DTCMessageType.OpenOrdersReject:
                case DTCMessageType.OrderUpdate:
                case DTCMessageType.HistoricalOrderFillResponse:
                case DTCMessageType.CurrentPositionsReject:
                case DTCMessageType.PositionUpdate:
                case DTCMessageType.TradeAccountResponse:
                case DTCMessageType.ExchangeListResponse:
                case DTCMessageType.SecurityDefinitionResponse:
                case DTCMessageType.SecurityDefinitionReject:
                case DTCMessageType.AccountBalanceReject:
                case DTCMessageType.AccountBalanceUpdate:
                case DTCMessageType.UserMessage:
                case DTCMessageType.GeneralLogMessage:
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                case DTCMessageType.HistoricalPriceDataReject:
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                default:
                    throw new ArgumentOutOfRangeException($"Unexpected MessageType {messageType} received by {this} {nameof(HandleRequest)}.");
            }
        }

        /// <summary>
        /// Run until cancelled  by CancellationTokenSource.Cancel() or by Dispose()
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                _tcpListener = new TcpListener(_ipAddress, _port);
            }
            catch (Exception ex)
            {
                throw;
            }
            try
            {
                _tcpListener.Start();
            }
            catch (ThreadAbortException)
            {
                // ignore this. It means Stop() was called
            }
            catch (Exception ex)
            {
                // A SocketException might be thrown from here
                throw;
            }
            IsConnected = true;
            while (!cancellationToken.IsCancellationRequested && !_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(true);
                    tcpClient.NoDelay = true;
                    var clientHandler = new ClientHandler(_callback, tcpClient, _timeoutNoActivity, _useHeartbeat);
                    try
                    {
                        var task = clientHandler.RunAsync(cancellationToken);
                        lock (_lock)
                        {
                            _clientHandlerTasks.Add(task);
                            _clientHandlers.Add(clientHandler);
                        }
                        OnClientConnected(clientHandler);
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
                catch (InvalidOperationException)
                {
                    // ignore this. It means Stop() was called
                }
                catch (ThreadAbortException)
                {
                    // ignore this. It means Stop() was called
                }
                catch (Exception ex)
                {

                    throw;
                }
            }
            await Task.WhenAll(_clientHandlerTasks).ConfigureAwait(false);
        }

        public override string ToString()
        {
            return $"{_ipAddress}:{_port}";
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
                _tcpListener.Stop();
                IsConnected = false;
                _cts.Cancel();
                _timerCheckForDisconnects?.Dispose();
                _isDisposed = true;
            }
        }
    }
}
