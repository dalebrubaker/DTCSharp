using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DTCCommon;
using DTCPB;
using Google.Protobuf;
using NLog;

namespace DTCServer
{
    public abstract class Server : IDisposable
    {
        private static readonly ILogger s_logger = LogManager.GetCurrentClassLogger();

        private readonly int _port;
        private readonly int _timeoutNoActivity;
        private readonly IPAddress _ipAddress;
        private TcpListener _tcpListener;
        private bool _isDisposed;
        private readonly CancellationTokenSource _cts;

        private readonly object _lock;
        private readonly List<ClientHandler> _clientHandlers; // parallel list to _clientHandlerTasks

        /// <summary>
        /// Start a TCP Listener on port at ipAddress
        /// </summary>
        /// <param name="port"></param>
        /// <param name="timeoutNoActivity">milliseconds timeout to assume disconnected if no activity</param>
        /// <param name="ipAddress"></param>
        protected Server(IPAddress ipAddress, int port, int timeoutNoActivity)
        {
            _ipAddress = ipAddress;
            _port = port;
            _timeoutNoActivity = timeoutNoActivity;
            _clientHandlers = new List<ClientHandler>();
            _lock = new object();
            _cts = new CancellationTokenSource();
            Address = new IPEndPoint(_ipAddress, _port).ToString();
        }

        public string Address { get; }

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
        /// <param name="messageType">the message type</param>
        /// <param name="message">the message (a Google.Protobuf message)</param>
        /// <returns></returns>
        protected abstract void HandleRequest(ClientHandler clientHandler, DTCMessageType messageType, IMessage message);

        #region events

        public event EventHandler<ClientHandler> ClientConnected;

        private void OnClientConnected(ClientHandler clientHandler)
        {
            clientHandler.OnConnected("Connected");
            var temp = ClientConnected;
            temp?.Invoke(this, clientHandler);
        }

        public event EventHandler<ClientHandler> ClientDisconnected;

        private void OnClientDisconnected(ClientHandler clientHandler)
        {
            lock (_lock)
            {
                _clientHandlers.Remove(clientHandler);
            }
            var temp = ClientDisconnected;
            temp?.Invoke(this, clientHandler);
        }

        #endregion events

        /// <summary>
        /// Run until cancelled by Dispose()
        /// </summary>
        public async Task RunAsync()
        {
            _tcpListener = new TcpListener(_ipAddress, _port);
            try
            {
                _tcpListener.Start();
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
                    var tcpClient = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(true); // will be disposed when clientHandler is disposed
                    tcpClient.NoDelay = true;
                    if (_timeoutNoActivity != 0)
                    {
                        tcpClient.ReceiveTimeout = _timeoutNoActivity;
                    }
                    var clientHandler = new ClientHandler(HandleRequest, tcpClient);
                    lock (_lock)
                    {
                        _clientHandlers.Add(clientHandler);
                        clientHandler.Disconnected += ClientHandlerOnDisconnected;
                    }
                    OnClientConnected(clientHandler);
                }
                catch (ObjectDisposedException)
                {
                    MyDebug.Assert(_isDisposed);
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

        private void ClientHandlerOnDisconnected(object sender, Error error)
        {
            var clientHandler = sender as ClientHandler;
            clientHandler.OnDisconnected(error);
            _clientHandlers.Remove(clientHandler);
        }

        private void CloseAllClientHandlers()
        {
            lock (_lock)
            {
                // foreach can give collection modified error
                for (int i = 0; i < _clientHandlers.Count; i++)
                {
                    var clientHandler = _clientHandlers[i];

                    // OnClientDisconnected removes the clientHandler from _clientHandlers
                    OnClientDisconnected(clientHandler);
                    clientHandler.Dispose();
                }
            }
        }

        public override string ToString()
        {
            return $"{_ipAddress}:{_port}";
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                //s_logger.Debug("Disposing Server");
                _cts.Cancel();
                IsConnected = false;
                _isDisposed = true;
                _tcpListener?.Stop();
                _tcpListener?.Server.Dispose();
                _tcpListener = null;
                CloseAllClientHandlers();
            }
            GC.SuppressFinalize(this);
        }
    }
}