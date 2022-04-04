﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Net;
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

namespace DTCServer
{
    public partial class ClientHandlerDTC : IEquatable<ClientHandlerDTC>, IDisposable
    {
        private static readonly Logger s_logger = LogManager.GetCurrentClassLogger();
        private static int s_instanceId;

        public int InstanceId { get; }
        private readonly Func<ClientHandlerDTC, MessageProto, Task> _callback;
        private readonly TcpClient _tcpClient;
        private readonly object _lock = new object();
        private bool _isDisposed;

        /// <summary>
        /// Holds messages received from the server
        /// </summary>
        private readonly BlockingCollection<MessageProto> _requestsQueue;

        private readonly Timer _timerHeartbeat;
        private readonly CancellationTokenSource _cts;
        private Stream _currentStream;
        private Func<MessageProto, MessageEncoded> _encode;
        private Func<MessageEncoded, MessageProto> _decode;
        private EncodingEnum _currentEncoding;
        private DateTime _lastHeartbeatSentTimeUtc;
        private int _heartbeatIntervalInSeconds;
        private readonly EndPoint _localEndPoint; // persist for ToString to work during Dispose
        private readonly EndPoint _remoteEndPoint; // persist for ToString to work during Dispose

        public ClientHandlerDTC(Func<ClientHandlerDTC, MessageProto, Task> callback, TcpClient tcpClient)
        {
            _callback = callback;
            _tcpClient = tcpClient;
            InstanceId = s_instanceId++;
            _cts = new CancellationTokenSource();
            _requestsQueue = new BlockingCollection<MessageProto>(1024 * 1024);
            _currentStream = _tcpClient.GetStream();

            // Start off binary until changed
            _currentEncoding = EncodingEnum.BinaryEncoding;
            _encode = CodecBinaryConverter.EncodeBinary;
            _decode = CodecBinaryConverter.DecodeBinary;
            Task.Factory.StartNew(RequestsProcessor, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            Task.Factory.StartNew(RequestsReader, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            _remoteEndPoint = _tcpClient.Client.RemoteEndPoint;
            _localEndPoint = _tcpClient.Client.LocalEndPoint;
            _timerHeartbeat = new Timer(1000);
            _timerHeartbeat.Elapsed += TimerHeartbeatElapsed;
        }

        public DateTime LastMessageReceivedTimeUtc { get; private set; }

        public bool IsConnected { get; private set; }

        /// <summary>
        /// Producer, loads requests from the client into the _requestsQueue
        /// </summary>
        private void RequestsReader()
        {
            MessageProto messageProto = null;
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    //s_logger.ConditionalDebug($"Waiting to read a message in {this}.{nameof(ClientMessageReader)} using {_currentCodec}");
                    var messageEncoded = _currentStream.ReadMessageEncoded();
                    LastMessageReceivedTimeUtc = DateTime.UtcNow;
                    messageProto = _decode(messageEncoded);
                    if (_cts.IsCancellationRequested)
                    {
                        return;
                    }
                    if (messageProto.MessageType != DTCMessageType.Heartbeat)
                    {
                        //s_logger.ConditionalTrace($"Received message={messageProto} in {this}.{nameof(RequestsReader)} using encoding={_currentEncoding}");
                    }
                    if (PreProcessRequest(messageProto))
                    {
                        // Handled, don't add to queue
                        continue;
                    }
                    if (_cts.IsCancellationRequested)
                    {
                        return;
                    }
                    _requestsQueue.Add(messageProto, _cts.Token);
                }
                _requestsQueue?.CompleteAdding();
            }
            catch (InvalidProtocolBufferException ex)
            {
                s_logger.Error(ex, $"ClientHandler {this}: error decoding:{messageProto} {ex.Message} _currentEncoding={_currentEncoding}");
                Dispose();
            }
            catch (EndOfStreamException)
            {
                s_logger.Info($"ClientHandler {this} reached end of stream {_currentStream}");
                Dispose();
            }
            catch (IOException ex)
            {
                if (!_cts.IsCancellationRequested)
                {
                    s_logger.Warn(ex, $"Disposing {this} because {ex.Message}");
                }
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
        /// Consumer, processes the messages from the _clientMessageQueue
        /// </summary>
        private void RequestsProcessor()
        {
            while (_requestsQueue?.IsCompleted == false && !_cts.IsCancellationRequested)
            {
                try
                {
                    if (!_requestsQueue.TryTake(out var message, -1, _cts.Token))
                    {
                        continue;
                    }

                    //s_logger.ConditionalTrace($"{this}.{nameof(RequestsProcessor)} took from _requestsQueue: {message}");
                    ProcessRequest(message);
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

        public void SendResponse(DTCSharpMessageType messageType, IMessage message)
        {
            lock (_lock)
            {
                var messageProto = new MessageProto(messageType, message);
                SendResponse(messageProto);
            }
        }

        public void SendResponse(DTCMessageType messageType, IMessage message)
        {
            lock (_lock)
            {
                var messageProto = new MessageProto(messageType, message);
                SendResponse(messageProto);
            }
        }

        /// <summary>
        /// Write the response. If thenSwitchToZipped, then switch the write stream to zlib format
        /// </summary>
        /// <param name="messageProto"></param>
        public void SendResponse(MessageProto messageProto)
        {
            lock (_lock)
            {
                var messageType = messageProto.MessageType;
                var message = messageProto.Message;
                var messageEncoded = _encode(messageProto);
                if (_isDisposed)
                {
                    return;
                }
                _currentStream.WriteMessageEncoded(messageEncoded);
                if (messageProto.MessageType != DTCMessageType.Heartbeat)
                {
                    //s_logger.ConditionalTrace($"{this} {nameof(SendResponse)} wrote with {_currentEncoding} {messageProto}");
                }
                PostProcessSentMessage(messageType, message);
            }
        }

        private void PostProcessSentMessage(DTCMessageType messageType, IMessage message)
        {
            switch (messageType)
            {
                case DTCMessageType.LogonResponse:
                    var logonResponse = (LogonResponse)message;
                    if (logonResponse.Result == LogonStatusEnum.LogonSuccess)
                    {
                        // Logon is successful
                        OnConnected("Logon sucessful");
                    }
                    break;
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                    var historicalPriceDataResponseHeader = (HistoricalPriceDataResponseHeader)message;
                    if (historicalPriceDataResponseHeader.UseZLibCompressionBool)
                    {
                        // Switch to writing zipped
                        SwitchStreamToZipped();
                    }
                    break;
            }
        }

        public void EndZippedHistorical()
        {
            if (_currentStream.GetType() != typeof(DeflateStream))
            {
                return;
            }

            // Close the stream to flush the deflateStream
            _currentStream.Close();

            // Go back to the NetworkStream
            _currentStream = _tcpClient.GetStream();
            //s_logger.ConditionalDebug($"Ended zipped historical {this} ");
        }

        /// <summary>
        /// This timer is turned off once we start sending compressed records. We don't send heartbeats that would interfere with the DeflateStream,
        ///     and we don't need to monitor the client because we will Dispose as soon as we finish sending the compressed records
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerHeartbeatElapsed(object sender, ElapsedEventArgs e)
        {
            if (_currentStream.GetType() == typeof(DeflateStream))
            {
                // Stop sending heartbeats once Historical is requested, so we don't interfere with zipped data etc.
                // We also don't check secondsSinceLastMessageReceived, as the client may be silent during receipt of historical data
                return;
            }

            DebugDTC.Assert(_currentStream.GetType() != typeof(DeflateStream), "Timer should be turned off once we start sending compressed records.");

            // Disconnect after missing a heartbeats
            var secondsSinceLastMessageReceived = (DateTime.UtcNow - LastMessageReceivedTimeUtc).TotalSeconds;
            if (secondsSinceLastMessageReceived > 2 * _heartbeatIntervalInSeconds)
            {
                // The client has disappeared. This is normal
                s_logger.ConditionalTrace($"Client disappeared from {this}");
                Dispose();
                return;
            }
            var secondsSinceHeartbeatSent = (DateTime.UtcNow - _lastHeartbeatSentTimeUtc).TotalSeconds;
            if (!(secondsSinceHeartbeatSent > _heartbeatIntervalInSeconds))
            {
                return;
            }

            // Send a heartbeat to the client
            var heartbeat = new Heartbeat
            {
                CurrentDateTime = DateTime.UtcNow.ToUnixSeconds()
            };
            try
            {
                SendResponse(DTCMessageType.Heartbeat, heartbeat);
                _lastHeartbeatSentTimeUtc = DateTime.UtcNow;
            }
            catch (IOException ex)
            {
                // DTC disconnected? No point in continuing with heartbeats
                _timerHeartbeat.Enabled = false;
                s_logger.ConditionalDebug(ex, $"{ex.Message} in {this}");
            }
            catch (Exception ex)
            {
                s_logger.ConditionalDebug(ex, $"{ex.Message} in {this}");
                throw;
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                //s_logger.ConditionalDebug("Disposing ClientHandler");
                _isDisposed = true;
                OnDisconnected(new Result("Disposed"));
                _cts.Cancel();
                if (_currentStream is DeflateStream)
                {
                    // Close the deflateStream first, in order to flush to the underlying _tcpClient.NetworkStream
                    _currentStream?.Dispose();
                }
                _tcpClient?.Close(); // also does Dispose, including its NetworkStream and socket (Client)
                if (_timerHeartbeat != null)
                {
                    _timerHeartbeat.Elapsed -= TimerHeartbeatElapsed;
                    _timerHeartbeat?.Stop();
                    _timerHeartbeat?.Dispose();
                }
                if (_requestsQueue != null)
                {
                    _requestsQueue.CompleteAdding();
                    // while (!_clientMessageQueue.IsCompleted)
                    // {
                    //     Thread.Sleep(10);
                    // }
                    // _clientMessageQueue.Dispose(); // Dispose causes problems due to thread safety
                }
            }
            GC.SuppressFinalize(this);
            s_logger.ConditionalTrace($"Disposed {this}");
        }

        public bool Equals(ClientHandlerDTC other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return InstanceId == other.InstanceId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((ClientHandlerDTC)obj);
        }

        public override int GetHashCode()
        {
            return InstanceId;
        }

        public static bool operator ==(ClientHandlerDTC left, ClientHandlerDTC right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ClientHandlerDTC left, ClientHandlerDTC right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return $"ClientHandler #{InstanceId} for Client:{LogonRequest?.ClientName} at Remote:{_remoteEndPoint} Local:{_localEndPoint}";
        }
    }
}