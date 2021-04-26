﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DTCCommon;
using DTCCommon.Codecs;
using DTCCommon.Exceptions;
using DTCPB;
using Google.Protobuf;
using NLog;
using Timer = System.Timers.Timer;

namespace DTCClient
{
    public class ClientBase : IDisposable
    {
        protected static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        protected readonly int TimeoutNoActivity;
        private Timer _timerHeartbeat;
        private bool _isDisposed;
        private TcpClient _tcpClient;
        private DateTime _lastHeartbeatReceivedTime;
        private NetworkStream _networkStream;
        protected Codec _currentCodec;
        private CancellationTokenSource _cts;
        private int _nextRequestId;
        protected bool _useHeartbeat;
        private bool _isConnected;

        /// <summary>
        /// Holds messages received from the server
        /// </summary>
        private readonly BlockingCollection<MessageDTC> _serverMessageQueue;

        private Task _taskConsumer;

        private Task _taskProducer;
        //private bool _isProducerRunning;

        /// <summary>
        /// Constructor for a client
        /// </summary>
        /// <param name="serverAddress">the machine name or an IP address for the server to which we want to connect</param>
        /// <param name="serverPort">the port for the server to which we want to connect</param>
        /// <param name="timeoutNoActivity">milliseconds timeout to assume disconnected if no activity. Set to 0 for Infinite</param>
        protected ClientBase(string serverAddress, int serverPort, int timeoutNoActivity)
        {
            ServerAddress = serverAddress;
            TimeoutNoActivity = timeoutNoActivity;
            ServerPort = serverPort;
            _serverMessageQueue = new BlockingCollection<MessageDTC>();
        }

        public bool IsConnected => _isConnected;

        /// <summary>
        /// The most recent _logonResponse.
        /// Use this to check Server flags before doing SendRequest()
        /// </summary>
        public LogonResponse LogonResponse { get; protected set; }

        /// <summary>
        /// See http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#SymbolIDRequestIDRules
        /// This is auto-incrementing
        /// </summary>
        public int NextRequestId => ++_nextRequestId;

        public string ServerAddress { get; }

        public int ServerPort { get; }

        public string ClientName { get; protected set; }

        private void TimerHeartbeatElapsed(object sender, ElapsedEventArgs e)
        {
            if (!_useHeartbeat)
            {
                return;
            }
            var maxWaitForHeartbeatTime = TimeSpan.FromMilliseconds(Math.Max(_timerHeartbeat.Interval * 2, 5000));
            var timeSinceHeartbeat = (DateTime.Now - _lastHeartbeatReceivedTime);
            if (timeSinceHeartbeat > maxWaitForHeartbeatTime)
            {
                Disconnect(new Error("Too long since Server sent us a heartbeat."));
            }

            // Send a heartbeat to the server
            var heartbeat = new Heartbeat();
            SendRequest(DTCMessageType.Heartbeat, heartbeat);
        }

        #region events

        public event EventHandler Connected;

        private void OnConnected()
        {
            _isConnected = true;
            var temp = Connected;
            temp?.Invoke(this, new EventArgs());
        }

        public event EventHandler<Error> Disconnected;

        protected void OnDisconnected(Error error)
        {
            _isConnected = false;
            var temp = Disconnected;
            temp?.Invoke(this, error);
        }

        public event EventHandler<Heartbeat> HeartbeatEvent;
        public event EventHandler<Logoff> LogoffEvent;

        public event EventHandler<EncodingResponse> EncodingResponseEvent;
        public event EventHandler<LogonResponse> LogonResponseEvent;

        #endregion events

        /// <summary>
        /// Make the connection to server at port. 
        /// Start the listener that will throw events for messages received from the server.
        /// To Disconnect simply Dispose() of this class.
        /// </summary>
        /// <param name="requestedEncoding"></param>
        /// <param name="timeout">The time (in milliseconds) to wait for a response before giving up</param>
        /// <param name="clientName">optional name for this client</param>
        /// <returns><c>true</c> if successful. <c>false</c> means protocol buffers are not supported by server</returns>
        public async Task<EncodingResponse> ConnectAsync(EncodingEnum requestedEncoding, string clientName, int timeout = 1000)
        {
            if (_isDisposed)
            {
                return null;
            }
            ClientName = clientName;
            _tcpClient = new TcpClient
            {
                NoDelay = true,
                ReceiveBufferSize = int.MaxValue,
                LingerState = new LingerOption(true, 5)
            };
            var tmp = _tcpClient.SendBufferSize;
            if (TimeoutNoActivity != 0)
            {
                _tcpClient.ReceiveTimeout = TimeoutNoActivity;
            }
            try
            {
                await _tcpClient.ConnectAsync(ServerAddress, ServerPort).ConfigureAwait(false); // connect to the server
            }
            catch (SocketException sex)
            {
                OnDisconnected(new Error(sex.Message));
            }
            _networkStream = _tcpClient.GetStream();

            // The initial protocol must be Binary, and can switch only when receiving and EncodingResponse
            SetCurrentCodec(EncodingEnum.BinaryEncoding);
            Logger.Debug("Initial setting of _currentCodec is Binary");
            _cts = new CancellationTokenSource();
            _taskConsumer = Task.Factory.StartNew(ServerMessageProcessorAsync, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            _taskProducer = Task.Factory.StartNew(ServerMessageReaderAsync, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);

            // Set up the handler to capture the event
            EncodingResponse result = null;

            void Handler(object s, EncodingResponse e)
            {
                EncodingResponseEvent -= Handler; // unregister to avoid a potential memory leak
                result = e;
            }

            EncodingResponseEvent += Handler;

            // Request protocol buffers encoding
            var encodingRequest = new EncodingRequest
            {
                Encoding = requestedEncoding,
                ProtocolType = "DTC",
                ProtocolVersion = (int)DTCVersion.CurrentVersion
            };

            // Give the server a bit to be able to respond
            //await Task.Delay(100).ConfigureAwait(true);
            SendRequest(DTCMessageType.EncodingRequest, encodingRequest);

            // Wait until the response is received or until timeout
            var startTime = DateTime.Now;
            while (result == null && (DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                await Task.Delay(1).ConfigureAwait(false);
            }
            if (result != null)
            {
                OnConnected();
            }
            return result;
        }

        /// <summary>
        /// Call this method AFTER calling ConnectAsync() to make the connection
        /// Send a Logon request to the server. 
        /// If not useHeartbeat, won't do a heartbeat. See: http://www.sierrachart.com/index.php?page=doc/DTCServer.php#HistoricalPriceDataServer
        /// </summary>
        /// <param name="heartbeatIntervalInSeconds">The interval in seconds that each side, the Client and the Server, needs to use to send HEARTBEAT messages to the other side. This should be a value from anywhere from 5 to 60 seconds.</param>
        /// <param name="useHeartbeat"><c>true</c>no heartbeat sent to server and none checked from server</param>
        /// <param name="timeout">The time (in milliseconds) to wait for a response before giving up</param>
        /// <param name="userName">Optional user name for the server to authenticate the Client</param>
        /// <param name="password">Optional password for the server to authenticate the Client</param>
        /// <param name="generalTextData">Optional general-purpose text string. For example, this could be used to pass a license key that the Server may require</param>
        /// <param name="integer1">Optional. General-purpose integer</param>
        /// <param name="integer2">Optional. General-purpose integer</param>
        /// <param name="tradeMode">optional to indicate to the Server that the requested trading mode to be one of the following: Demo, Simulated, Live.</param>
        /// <param name="tradeAccount">optional identifier if that is required to login</param>
        /// <param name="hardwareIdentifier">optional computer hardware identifier</param>
        /// <returns>The LogonResponse, or null if not received before timeout</returns>
        public async Task<LogonResponse> LogonAsync(int heartbeatIntervalInSeconds = 1, bool useHeartbeat = true, int timeout = 1000, string userName = "",
            string password = "", string generalTextData = "", int integer1 = 0, int integer2 = 0, TradeModeEnum tradeMode = TradeModeEnum.TradeModeUnset,
            string tradeAccount = "", string hardwareIdentifier = "")
        {
            if (_isDisposed)
            {
                return null;
            }
            _useHeartbeat = useHeartbeat;
            if (_useHeartbeat)
            {
                // start the heartbeat
                _timerHeartbeat = new Timer(heartbeatIntervalInSeconds * 1000);
                _timerHeartbeat.Elapsed += TimerHeartbeatElapsed;
                _lastHeartbeatReceivedTime = DateTime.Now;
                _timerHeartbeat.Start();
            }

            // Set up the handler to capture the event
            LogonResponse result = null;

            void Handler(object s, LogonResponse e)
            {
                LogonResponseEvent -= Handler; // unregister to avoid a potential memory leak
                result = e;
            }

            LogonResponseEvent += Handler;

            // Send the request
            var logonRequest = new LogonRequest
            {
                ClientName = ClientName,
                GeneralTextData = generalTextData,
                HardwareIdentifier = hardwareIdentifier,
                HeartbeatIntervalInSeconds = _useHeartbeat ? heartbeatIntervalInSeconds : 0,
                Integer1 = integer1,
                Integer2 = integer2,
                Username = userName,
                Password = password,
                ProtocolVersion = (int)DTCVersion.CurrentVersion,
                TradeAccount = tradeAccount,
                TradeMode = tradeMode
            };
            SendRequest(DTCMessageType.LogonRequest, logonRequest);

            // Wait until the response is received or until timeout
            var startTime = DateTime.Now;
            while (result == null && (DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                await Task.Delay(1).ConfigureAwait(false);
            }
            return result;
        }

        protected void ThrowEvent<T>(T message, EventHandler<T> eventForMessage) where T : IMessage
        {
            var temp = eventForMessage;
            temp?.Invoke(this, message);
        }

        /// <summary>
        /// internal for unit tests
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        protected internal void SendRequest<T>(DTCMessageType messageType, T message) where T : IMessage
        {
            try
            {
                _currentCodec.Write(messageType, message);
            }
            catch (Exception ex)
            {
                var error = new Error("Unable to send request", ex);
                Disconnect(error);
            }
        }

        /// <summary>
        /// Producer, loads messages from the server into the _serverMessageQueue
        /// </summary>
        private async Task ServerMessageReaderAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    Logger.Debug($"Waiting in {nameof(Client)}.{nameof(ServerMessageReaderAsync)} to read a message using {_currentCodec}");
                    var message = ReadMessageDTC();
                    Logger.Debug($"Did in {nameof(Client)}.{nameof(ServerMessageReaderAsync)} read message={message} using {_currentCodec}");
                    if (_cts.IsCancellationRequested)
                    {
                        // Changed _currentCodec, so unblocked this way
                        _cts = new CancellationTokenSource();
                        continue;
                    }
                    if (message == null)
                    {
                        // Probably an exception
                        throw new DTCSharpException("Why?");
                    }
                    //Logger.Debug($"Waiting in {nameof(Client)}.{nameof(ServerMessageReaderAsync)} to add to messageQueue: {message}");
                    _serverMessageQueue.Add(message, _cts.Token);
                    //Logger.Debug($"{nameof(Client)}.{nameof(ServerMessageReaderAsync)} added to messageQueue: {message}");
                    
                    if (message.MessageType == DTCMessageType.EncodingResponse)
                    {
                        var encodingResponse = message.Message as EncodingResponse;
                        while (encodingResponse.Encoding != _currentCodec.Encoding)
                        {
                            // Wait for this message to be processed before reading messages with the new encoding
                            await Task.Delay(1).ConfigureAwait(false);
                        }
                        Debug.Assert(_serverMessageQueue.Count == 0, "Processor emptied the queue");
                    }
                    else if (message.MessageType == DTCMessageType.HistoricalPriceDataResponseHeader)
                    {
                        var historicalPriceDataResponseHeader = message.Message as HistoricalPriceDataResponseHeader;
                        while (historicalPriceDataResponseHeader.UseZLibCompressionBool && !_currentCodec.IsZippedStream)
                        {
                            // Wait for this message to be processed before reading zipped messages
                            await Task.Delay(1).ConfigureAwait(false);
                        }
                        Debug.Assert(_serverMessageQueue.Count == 0, "Processor emptied the queue");
                    }
                    else if (_currentCodec.IsZippedStream && message.MessageType == DTCMessageType.HistoricalPriceDataRecordResponse)
                    {
                        var historicalPriceDataRecordResponse = message.Message as HistoricalPriceDataRecordResponse;
                        if (historicalPriceDataRecordResponse.IsFinalRecordBool)
                        {
                            while (_currentCodec.IsZippedStream)
                            {
                                // Wait for this message to be processed (where the stream is switched back to not-zipped) before going back to reading not-zipped messages
                                await Task.Delay(1).ConfigureAwait(false);
                            }
                            Debug.Assert(_serverMessageQueue.Count == 0, "Processor emptied the queue");
                        }
                    }
                    else if (_currentCodec.IsZippedStream && message.MessageType == DTCMessageType.HistoricalPriceDataTickRecordResponse)
                    {
                        var historicalPriceDataTickRecordResponse = message.Message as HistoricalPriceDataTickRecordResponse;
                        if (historicalPriceDataTickRecordResponse.IsFinalRecordBool)
                        {
                            while (_currentCodec.IsZippedStream)
                            {
                                // Wait for this message to be processed (where the stream is switched back to not-zipped) before going back to reading not-zipped messages
                                await Task.Delay(1).ConfigureAwait(false);
                            }
                            Debug.Assert(_serverMessageQueue.Count == 0, "Processor emptied the queue");
                        }
                    }
                }
                _serverMessageQueue.CompleteAdding();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw;
            }
        }

        private MessageDTC ReadMessageDTC()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    //s_logger.Debug($"Waiting in {nameof(Client)}.{nameof(ResponseReader)} to read a message");
                    var message = _currentCodec.GetMessageDTC();
                    return message;
                }
                catch (IOException ex)
                {
                    // Ignore this if it results from disconnect (cancellation)
                    if (_cts.IsCancellationRequested)
                    {
                        Disconnect(new Error("Read error.", ex));
                        return null;
                    }
                    Logger.Error(ex, ex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    var typeName = ex.GetType().Name;
                    Disconnect(new Error($"Read error {typeName}.", ex));
                    Logger.Error(ex, ex.Message);
                    throw;
                }
            }
            return null;
        }

        /// <summary>
        /// Consumer, processes the messages from the _serverMessageQueue
        /// </summary>
        private void ServerMessageProcessorAsync()
        {
            while (!_serverMessageQueue.IsCompleted)
            {
                MessageDTC message = null;
                try
                {
                    message = _serverMessageQueue.Take(_cts.Token);
                    //Logger.Debug($"{nameof(Client)}.{nameof(ServerMessageProcessorAsync)} took from messageQueue: {message}");
                }
                catch (InvalidOperationException)
                {
                    // The Microsoft example says we can ignore this exception https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/blockingcollection-overview
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, ex.Message);
                    throw;
                }
                if (message != null)
                {
                    ProcessMessage(message);
                }
            }
        }

        /// <summary>
        /// Process the message represented by bytes.
        /// </summary>
        /// <param name="messageDTC"></param>
        protected virtual void ProcessMessage(MessageDTC messageDTC)
        {
            //s_logger.Debug($"{nameof(ProcessResponseBytes)} is processing {messageType}");
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (messageDTC.MessageType)
            {
                case DTCMessageType.LogonResponse:
                    ThrowEvent(messageDTC.Message as LogonResponse, LogonResponseEvent);
                    break;
                case DTCMessageType.Heartbeat:
                    _lastHeartbeatReceivedTime = DateTime.Now;
                    ThrowEvent(messageDTC.Message as Heartbeat, HeartbeatEvent);
                    break;
                case DTCMessageType.Logoff:
                    OnDisconnected(new Error("User logoff"));
                    ThrowEvent(messageDTC.Message as Logoff, LogoffEvent);
                    break;
                case DTCMessageType.EncodingResponse:
                    // Note that we must use binary encoding here on the FIRST usage after connect, 
                    //    per http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#EncodingRequest
                    var encodingResponse = messageDTC.Message as EncodingResponse;
                    SetCurrentCodec(encodingResponse.Encoding);
                    ThrowEvent(encodingResponse, EncodingResponseEvent);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unexpected Message {messageDTC} received by {ClientName} {nameof(ProcessMessage)}.");
            }
        }

        /// <summary>
        /// This must be called whenever the encoding changes, immediately AFTER the EncodingResponse is sent to the client
        /// </summary>
        /// <param name="encoding"></param>
        private void SetCurrentCodec(EncodingEnum encoding)
        {
            if (encoding == _currentCodec?.Encoding)
            {
                return;
            }
            switch (encoding)
            {
                case EncodingEnum.BinaryEncoding:
                    _currentCodec = new CodecBinary(_networkStream);
                    break;
                case EncodingEnum.BinaryWithVariableLengthStrings:
                case EncodingEnum.JsonEncoding:
                case EncodingEnum.JsonCompactEncoding:
                    throw new NotImplementedException($"Not implemented in {nameof(Client)}: {nameof(encoding)}");
                case EncodingEnum.ProtocolBuffers:
                    _currentCodec = new CodecProtobuf(_networkStream);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null);
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                //s_logger.Debug("Disposing Client");
                _isDisposed = true;
                _currentCodec.Write(DTCMessageType.Logoff, new Logoff
                {
                    DoNotReconnect = 1u,
                    Reason = "Client Disposed"
                });
                _currentCodec.Dispose();
                _cts.Cancel(true);
                Disconnect(new Error("Disposing"));
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Throw the Disconnect event and close down the connection (if any) to the server
        /// </summary>
        /// <param name="error"></param>
        protected void Disconnect(Error error)
        {
            OnDisconnected(error);
            //Dispose();
        }

        public override string ToString()
        {
            return $"{ClientName} {ServerAddress} {ServerPort}";
        }
    }
}