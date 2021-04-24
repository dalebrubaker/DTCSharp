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
    public class Server : IDisposable
    {
        private static readonly ILogger s_logger = LogManager.GetCurrentClassLogger();

        private readonly int _port;
        private readonly int _timeoutNoActivity;
        private readonly Action<ClientHandler, DTCMessageType, IMessage> _callback;
        private readonly IPAddress _ipAddress;
        private TcpListener _tcpListener;
        private bool _isDisposed;
        private readonly CancellationTokenSource _cts;

        private readonly object _lock;
        private readonly List<ClientHandler> _clientHandlers; // parallel list to _clientHandlerTasks

        /// <summary>
        /// Start a TCP Listener on port at ipAddress
        /// </summary>
        /// <param name="callback">the callback for all client requests</param>
        /// <param name="port"></param>
        /// <param name="timeoutNoActivity">milliseconds timeout to assume disconnected if no activity</param>
        /// <param name="ipAddress"></param>
        public Server(Action<ClientHandler, DTCMessageType, IMessage> callback, IPAddress ipAddress, int port, int timeoutNoActivity)
        {
            _callback = callback;
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
            var tasks = new List<Task>();
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
                    tcpClient.LingerState = new LingerOption(true, 5);
                    var clientHandler = new ClientHandler(_callback, tcpClient);
                    var task = Task.Factory.StartNew(clientHandler.RequestReaderLoopAsync, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
                    tasks.Add(task);
                    lock (_lock)
                    {
                        _clientHandlers.Add(clientHandler);
                        clientHandler.Disconnected += ClientHandlerOnDisconnected;
                    }
                    OnClientConnected(clientHandler);
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
            await Task.WhenAll(tasks).ConfigureAwait(false);
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