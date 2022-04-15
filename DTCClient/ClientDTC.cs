using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DTCCommon;
using DTCCommon.Codecs;
using DTCPB;
using Google.Protobuf;
using NLog;
using Timer = System.Timers.Timer;

namespace DTCClient
{
    public partial class ClientDTC : TcpClient
    {
        private static readonly Logger s_logger = LogManager.GetCurrentClassLogger();
        private static int s_instanceId;
        
        private const int TimeoutMs = 30 * 1000; // 5 is not enough // 10 is low for debugging // 30 is standard
        private const string TradeMessageLogging = "TradeMessage:";

        public int InstanceId { get; }

        /// <summary>
        /// See http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#SymbolIDRequestIDRules
        /// This is auto-incrementing
        /// </summary>
        private static uint s_nextRequestId;

        public static uint NextRequestId => ++s_nextRequestId;

        private string _clientName;
        private int _heartbeatIntervalInSeconds;
        private string _hostname;
        private int _port;

        /// <summary>
        /// SierraChart can't handle rapid requests for symbols, and they don't change over time. So we cache them here
        /// </summary>
        private static readonly Dictionary<string, SecurityDefinitionResponse> s_securityDefinitionsBySymbol = new Dictionary<string, SecurityDefinitionResponse>();

        /// <summary>
        /// Holds messages received from the server
        /// </summary>
        private readonly BlockingCollection<MessageProto> _responsesQueue;

        private readonly CancellationTokenSource _cts;
        private readonly object _lock = new object();

        private bool _isDisposed;

        private Timer _timerHeartbeat;

        private DateTime _lastMessageReceivedTimeUtc;
        private DateTime _lastHeartbeatSentTimeUtc;

        private Stream _currentStream;
        private Func<MessageProto, MessageEncoded> _encode;
        private Func<MessageEncoded, MessageProto> _decode;
        private EncodingEnum _currentEncoding;

        /// <summary>
        /// The most recent _logonResponse.
        /// Use this to check Server flags before doing SendRequest()
        /// </summary>
        public LogonResponse LogonResponse { get; set; }

        /// <summary>
        /// Do a separate connect, because base(hostname, port) is VERY slow due to IPV6 check/fail
        /// </summary>
        public ClientDTC()
        {
            InstanceId = s_instanceId++;
            _cts = new CancellationTokenSource();
            _responsesQueue = new BlockingCollection<MessageProto>(1024 * 1024);
        }

        /// <summary>
        /// Connect and start running this client
        /// </summary>
        /// <param name="hostname">the server name, e.g. could be localhost or an 127.0.0.1</param>
        /// <param name="port">the server port</param>
        public void Start(string hostname, int port)
        {
            _hostname = hostname;
            _port = port;
            
            // Do a separate connect, because base(hostname, port) is VERY slow due to IPV6 check/fail
            Connect(_hostname, _port);
            _currentStream = GetStream();
            
            NoDelay = true;
            ReceiveBufferSize = SendBufferSize = 65536; // maximum DTC message size
            _currentEncoding = EncodingEnum.BinaryEncoding;

            // Start off binary until changed
            _encode = CodecBinaryConverter.EncodeBinary;
            _decode = CodecBinaryConverter.DecodeBinary;

            Task.Factory.StartNew(ResponsesReader, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            Task.Factory.StartNew(ResponsesProcessor, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }
       
        /// <summary>
        /// <c>true</c> after a successful Logon
        /// </summary>
        public bool IsConnected { get; private set; }

        public (LogonResponse logonResponse, Result error) Logon(LogonRequest logonRequest, EncodingEnum requestedEncoding)
        {
            if (IsConnected)
            {
                throw new DTCSharpException($"{this} is already connected.");
            }
            _clientName = logonRequest.ClientName;
            LogonResponse = null;
            _heartbeatIntervalInSeconds = logonRequest.HeartbeatIntervalInSeconds;
            if (_heartbeatIntervalInSeconds < 1)
            {
                // This is what we use for Historical connections. 0 means infinite
                //throw new ArgumentException("heartbeatIntervalInSeconds must be at least 1");
            }
            s_logger.ConditionalTrace($"Starting encoding/logon for {_clientName} in {this}");
            var encodingResult = SetEncoding(requestedEncoding);
            if (encodingResult.IsError)
            {
                var msg = $"Client is not able to connect because {encodingResult.ResultText} in {this}";
                s_logger.Error(msg);
                var error = new Result(msg, ErrorTypes.LogonRefused);
                return (null, error);
            }
            var signal = new ManualResetEvent(false);

            // Set up the handler to capture the event
            void Handler(object s, LogonResponse e)
            {
                LogonResponseEvent -= Handler; // unregister to avoid a potential memory leak
                LogonResponse = e;
                signal.Set();
            }

            LogonResponseEvent += Handler;

            SendRequest(DTCMessageType.LogonRequest, logonRequest);

            // Wait until the response is received or the networkStream times out
            if (LogonResponse == null)
            {
                s_logger.ConditionalTrace($"Waiting for logon response in {this}");
                signal.WaitOne(TimeoutMs);
                if (LogonResponse == null)
                {
                    var msg = $"LogonAsync timed out after {TimeoutMs} milliseconds in {this}";
                    s_logger.Error(msg);
                    var error = new Result(msg, ErrorTypes.LogonRefused);
                    return (null, error);
                }
                s_logger.ConditionalTrace($"Done waiting for logon response in {this}, received {LogonResponse}");
            }
            if (LogonResponse.Result != LogonStatusEnum.LogonSuccess)
            {
                // unsuccessful logon
                s_logger.ConditionalTrace($"Unsuccessful logon for {_clientName} due to {LogonResponse.ResultText} in {this}");
                var error = new Result(LogonResponse.ResultText);
                return (null, error);
            }

            // Logon is successful
            if (_heartbeatIntervalInSeconds > 0)
            {
                // start the heartbeat timer. 0 means there is no heartbeat (used for historical data)
                _timerHeartbeat = new Timer(1000);
                _timerHeartbeat.Elapsed += TimerHeartbeatElapsed;
                _timerHeartbeat.Start();
            }
            IsConnected = true;
            OnConnected();
            //s_logger.ConditionalDebug($"Successful logon for {_clientName} in {this}");
            return (LogonResponse, new Result());
        }

        /// <summary>
        /// Send a Logon request to the server after agreeing to an encoding protocol 
        /// If not useHeartbeat, won't do a heartbeat. See: http://www.sierrachart.com/index.php?page=doc/DTCServer.php#HistoricalPriceDataServer
        /// </summary>
        /// <param name="clientName"></param>
        /// <param name="heartbeatIntervalInSeconds">The interval in seconds that each side, the Client and the Server, needs to use to send HEARTBEAT messages to the other side.
        /// This should be a value from anywhere from 5 to 60 seconds.default is 10. Less than 1 is not allowed</param>
        /// <param name="requestedEncoding"></param>
        /// <param name="userName">Optional user name for the server to authenticate the Client</param>
        /// <param name="password">Optional password for the server to authenticate the Client</param>
        /// <param name="generalTextData">Optional general-purpose text string. For example, this could be used to pass a license key that the Server may require</param>
        /// <param name="integer1">Optional. General-purpose integer</param>
        /// <param name="integer2">Optional. General-purpose integer</param>
        /// <param name="tradeAccount">optional identifier if that is required to login</param>
        /// <param name="hardwareIdentifier">optional computer hardware identifier</param>
        /// <returns>The LogonResponse, or null if not received before timeout</returns>
        public (LogonResponse logonResponse, Result error) Logon(string clientName, int heartbeatIntervalInSeconds = 10, EncodingEnum requestedEncoding = EncodingEnum.ProtocolBuffers,
            string userName = "", string password = "", string generalTextData = "", int integer1 = 0, int integer2 = 0, string tradeAccount = "", string hardwareIdentifier = "")
        {
            if (clientName == null)
            {
                throw new ArgumentNullException(nameof(clientName), "ClientName must not be null");
            }
            // Send the request
            var logonRequest = new LogonRequest
            {
                ClientName = clientName,
                GeneralTextData = generalTextData,
                HardwareIdentifier = hardwareIdentifier,
                HeartbeatIntervalInSeconds = heartbeatIntervalInSeconds,
                Integer1 = integer1,
                Integer2 = integer2,
                Username = userName,
                Password = password,
                ProtocolVersion = (int)DTCVersion.CurrentVersion,
                TradeAccount = tradeAccount
            };
            return Logon(logonRequest, requestedEncoding);
        }

        private Result SetEncoding(EncodingEnum requestedEncoding)
        {
            var error = new Result($"Client timed out after {TimeoutMs} seconds doing SetEncoding={requestedEncoding} in {this}", ErrorTypes.CannotConnect);
            var signal = new ManualResetEvent(false);
            
            void Handler(object s, EncodingResponse response)
            {
                EncodingResponseEvent -= Handler; // unregister to avoid a potential memory leak
                error = new Result();
                signal.Set();
            }

            // Set up the handler to capture the event
            EncodingResponseEvent += Handler;

            // Request protocol buffers encoding
            var encodingRequest = new EncodingRequest
            {
                Encoding = requestedEncoding,
                ProtocolType = "DTC",
                ProtocolVersion = (int)DTCVersion.CurrentVersion
            };

            SendRequest(DTCMessageType.EncodingRequest, encodingRequest);

            // Wait until the response is received
            signal.WaitOne(TimeoutMs);
            return error;
        }

        /// <summary>
        /// Write the request to the current stream
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        public void SendRequest<T>(DTCSharpMessageType messageType, T message) where T : IMessage
        {
            lock (_lock)
            {
                var messageProto = new MessageProto(messageType, message);
                SendRequest(messageProto);
            }
        }

        /// <summary>
        /// Write the request to the current stream
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        public void SendRequest<T>(DTCMessageType messageType, T message) where T : IMessage
        {
            lock (_lock)
            {
                var messageProto = new MessageProto(messageType, message);
                SendRequest(messageProto);
            }
        }

        /// <summary>
        /// Write the request to the current stream
        /// </summary>
        /// <param name="messageProto"></param>
        public void SendRequest(MessageProto messageProto)
        {
            try
            {
                lock (_lock)
                {
                    var messageEncoded = _encode(messageProto);
                    _currentStream.WriteMessageEncoded(messageEncoded);
                    if (messageProto.MessageType != DTCMessageType.Heartbeat)
                    {
                        s_logger.ConditionalTrace($"{this} {nameof(SendRequest)} sent with {_currentEncoding} {messageProto}");
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                s_logger.Error(ex, $"{ex.Message} in {this}");
                Dispose();
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, $"{ex.Message} in {this}");
                Dispose();
            }
        }

        private void TimerHeartbeatElapsed(object sender, ElapsedEventArgs e)
        {
            // Disconnect after missing a heartbeats
            var secondsSinceLastMessageReceived = (DateTime.UtcNow - _lastMessageReceivedTimeUtc).TotalSeconds;
            if (secondsSinceLastMessageReceived > 2 * _heartbeatIntervalInSeconds)
            {
                // The server has disappeared
                s_logger.Error($"Server disappeared from {this}");
                Dispose();
                return;
            }
            var secondsSinceHeartbeatSent = (DateTime.UtcNow - _lastHeartbeatSentTimeUtc).TotalSeconds;
            if (!(secondsSinceHeartbeatSent > _heartbeatIntervalInSeconds))
            {
                return;
            }
            if (_currentStream.GetType() == typeof(DeflateStream))
            {
                // We can't send heartbeats because the stream might be compressed
                return;
            }

            // Send a heartbeat to the server
            var heartbeat = new Heartbeat
            {
                CurrentDateTime = DateTime.UtcNow.ToUnixSeconds()
            };
            try
            {
                SendRequest(DTCMessageType.Heartbeat, heartbeat);
                _lastHeartbeatSentTimeUtc = DateTime.UtcNow;
            }
            catch (IOException ex)
            {
                // DTC disconnected? No point in continuing with heartbeats
                _timerHeartbeat.Enabled = false;
                s_logger.ConditionalDebug(ex, ex.Message);
            }
            catch (Exception ex)
            {
                s_logger.ConditionalDebug(ex, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Producer, loads responses from the server into the _responsesQueue
        /// </summary>
        private void ResponsesReader()
        {
            if (_currentStream == null)
            {
                throw new DTCSharpException($"{nameof(Start)} must be called");
            }
            MessageProto messageProto = null;
            MessageEncoded messageEncoded = null;
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    //s_logger.ConditionalDebug($"About to read a message in {nameof(ClientDTC)}.{nameof(ResponsesReader)} using {_currentEncoding}");
                    messageEncoded = _currentStream.ReadMessageEncoded();
                    _lastMessageReceivedTimeUtc = DateTime.UtcNow;
                    messageProto = null; // in case we throw an exception during decod
                    messageProto = _decode(messageEncoded);
                    if (messageProto.MessageType != DTCMessageType.Heartbeat)
                    {
                        //s_logger.ConditionalTrace($"Received messageEncoded={messageEncoded} - message={messageProto} in {this} {nameof(ResponsesReader)} using encoding={_currentEncoding}");
                    }
                    if (PreProcessResponse(messageProto))
                    {
                        // Handled, don't add to queue
                        continue;
                    }
                    if (_cts.IsCancellationRequested)
                    {
                        return;
                    }
                    _responsesQueue.Add(messageProto, _cts.Token);
                }
                _responsesQueue?.CompleteAdding();
            }
            catch (InvalidProtocolBufferException ex)
            {
                s_logger.Error(ex, $"Client {this}: error messageEncoded={messageEncoded} - {messageProto} {ex.Message} _currentEncoding={_currentEncoding} in {this}");
                Dispose();
            }
            catch (EndOfStreamException)
            {
                s_logger.ConditionalTrace($"Client {this} reached end of stream {_currentStream} in {this}");
                Dispose();
            }
            catch (IOException)
            {
                s_logger.ConditionalTrace($"Client {this} reached end of stream {_currentStream} in {this}");
                Dispose();
            }
            catch (Exception ex)
            {
                if (!_cts.IsCancellationRequested)
                {
                    s_logger.Error(ex, $"{ex.Message} in {this}");
                }
                Dispose();
            }
        }

        /// <summary>
        /// Consumer, processes the messages from the _serverMessageQueue
        /// </summary>
        private void ResponsesProcessor()
        {
            while (_responsesQueue?.IsCompleted == false && !_cts.IsCancellationRequested)
            {
                try
                {
                    if (!_responsesQueue.TryTake(out var messageProto, -1, _cts.Token))
                    {
                        continue;
                    }

                    //s_logger.ConditionalTrace($"{this}.{nameof(ResponsesProcessor)} took from _responsesQueue: {messageProto}");
                    if (messageProto.MessageType is DTCMessageType.OrderUpdate ) // or DTCMessageType.PositionUpdate) // or DTCMessageType.AccountBalanceUpdate)
                    {
                        s_logger.ConditionalTrace($"{TradeMessageLogging}{this}.{nameof(ResponsesProcessor)} took from _responsesQueue: {messageProto}");
                    }
                    ProcessResponse(messageProto);
                }
                catch (OperationCanceledException)
                {
                    // ignore
                    return;
                }
                catch (Exception ex)
                {
                    if (!_cts.IsCancellationRequested)
                    {
                        s_logger.Error(ex, $"{ex.Message} in {this}");
                    }
                    Dispose();
                    throw;
                }
            }
        }

        /// <summary>
        /// We send the Disconnect event then Close/Dispose
        ///     whenever there is any error, or whenever the server drops a heartbeat
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!disposing || _isDisposed)
            {
                return;
            }
            _isDisposed = true;
            _cts?.Cancel();
            if (_timerHeartbeat != null)
            {
                _timerHeartbeat.Enabled = false;
                _timerHeartbeat?.Dispose();
            }

            // Always disconnect at Dispose
            IsConnected = false;
            OnDisconnected();
            base.Dispose(disposing); // same as dispose
            s_logger.ConditionalTrace($"Disposed {this}");
        }

        public override string ToString()
        {
            var result = $"Client: #{InstanceId} {_hostname}:{_port} {_clientName}";
            //result += $"Client at Remote:{Client.RemoteEndPoint} Local:{Client.LocalEndPoint}";
            return result;
        }
    }
}