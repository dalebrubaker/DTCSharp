using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DTCCommon;
using NLog;

namespace DTCServer
{
    public abstract class ListenerDTC : TcpListener, IDisposable
    {
        private static readonly ILogger s_logger = LogManager.GetCurrentClassLogger();

        private readonly IPAddress _localaddr;
        private readonly int _port;
        private readonly object _lock;
        private readonly List<ClientHandlerDTC> _clientHandlers; // parallel list to _ClientHandlerDTCTasks
        private bool _isDisposed;
        private readonly CancellationTokenSource _cts;

        public string Address { get; }

        protected ListenerDTC(IPAddress localaddr, int port) : base(localaddr, port)
        {
            _localaddr = localaddr;
            _port = port;
            _lock = new object();
            _cts = new CancellationTokenSource();
            _clientHandlers = new List<ClientHandlerDTC>();
            Address = new IPEndPoint(_localaddr, _port).ToString();
            //s_logger.Trace($"ctor {nameof(ListenerDTC)} {this}"); // {Environment.StackTrace}");
            Task.Factory.StartNew(RunAsync, TaskCreationOptions.LongRunning);
        }

        public int NumberOfClientHandlers
        {
            get
            {
                lock (_lock)
                {
                    return _clientHandlers.Count;
                }
            }
        }

        public int NumberOfClientHandlersConnected
        {
            get
            {
                lock (_lock)
                {
                    return _clientHandlers.Count(x => x.IsConnected);
                }
            }
        }

        public bool IsConnected { get; private set; }

        /// <summary>
        /// This method is called for every request received by a client connected to this server.
        /// </summary>
        /// <param name="clientHandler">The handler for a particular client connected to this server</param>
        /// <param name="messageProto">the message (a Google.Protobuf message)</param>
        /// <returns></returns>
        protected abstract Task HandleRequestAsync(ClientHandlerDTC clientHandler, MessageProto messageProto);

        /// <summary>
        /// Run until cancelled by Dispose()
        /// </summary>
        public async Task RunAsync()
        {
            try
            {
                s_logger.Trace($"Starting {nameof(ListenerDTC)} {this}"); // {Environment.StackTrace}");
                Start();
            }
            catch (SocketException ex)
            {
                s_logger.Error(ex, ex.Message);
                throw;
            }
            catch (ThreadAbortException)
            {
                // Dispose() has been called
                CloseAllClientHandlers();
            }
#pragma warning disable 168
            IsConnected = true;
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await AcceptTcpClientAsync().ConfigureAwait(false); // will be disposed when ClientHandlerDTC is disposed
                    tcpClient.NoDelay = true;
                    tcpClient.ReceiveBufferSize = tcpClient.SendBufferSize = 65536; // maximum DTC message size
                    var clientHandler = new ClientHandlerDTC(HandleRequestAsync, tcpClient);
                    lock (_lock)
                    {
                        _clientHandlers.Add(clientHandler);
                        clientHandler.Disconnected += ClientHandlerOnDisconnected;
                    }
                    OnClientConnected(clientHandler);
                }
                catch (ObjectDisposedException)
                {
                    DebugDTC.Assert(_isDisposed);
                }
                catch (InvalidOperationException)
                {
                    // Dispose() has been called
                    CloseAllClientHandlers();
                }
                catch (ThreadAbortException)
                {
                    // Dispose() has been called
                    CloseAllClientHandlers();
                }
#pragma warning disable 168
            }
            CloseAllClientHandlers();
        }

        private void CloseAllClientHandlers()
        {
            try
            {
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            lock (_lock)
            {
                // foreach can give collection modified error
                // ReSharper disable once ForCanBeConvertedToForeach
                for (int i = 0; i < _clientHandlers.Count; i++)
                {
                    try
                    {
                        var clientHandler = _clientHandlers[i];

                        // OnClientDisconnected removes the ClientHandlerDTC from _ClientHandlerDTCs
                        OnClientDisconnected(clientHandler);
                        clientHandler.Dispose();
                    }
                    catch (Exception ex)
                    {
                        s_logger.Warn($"Ignoring {ex.Message} during dispose of clientHandler");
                    }
                }
            }
        }

        protected List<ClientHandlerDTC> GetClientHandlers()
        {
            lock (_lock)
            {
                var result = new List<ClientHandlerDTC>(_clientHandlers);
                return result;
            }
        }

        #region events

        private void ClientHandlerOnDisconnected(object sender, Result result)
        {
            lock (_lock)
            {
                var clientHandler = (ClientHandlerDTC)sender;
                if (_clientHandlers.Contains(clientHandler))
                {
                    _clientHandlers.Remove(clientHandler);
                    clientHandler.OnDisconnected(result);
                }
            }
        }

        public event EventHandler<ClientHandlerDTC> ClientConnected;

        private void OnClientConnected(ClientHandlerDTC clientHandler)
        {
            clientHandler.OnConnected("Connected");
            var temp = ClientConnected;
            temp?.Invoke(this, clientHandler);
        }

        public event EventHandler<ClientHandlerDTC> ClientDisconnected;

        private void OnClientDisconnected(ClientHandlerDTC clientHandler)
        {
            lock (_lock)
            {
                _clientHandlers.Remove(clientHandler);
            }
            var temp = ClientDisconnected;
            temp?.Invoke(this, clientHandler);
        }

        #endregion events

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }
            _cts?.Cancel();
            _cts?.Dispose();
            IsConnected = false;
            _isDisposed = true;
            Stop();
            Server.Dispose();
            CloseAllClientHandlers();
            //s_logger.Trace($"Disposed {nameof(ListenerDTC)} {this}");
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public override string ToString()
        {
            return $"{_localaddr}:{_port}";
        }
    }
}