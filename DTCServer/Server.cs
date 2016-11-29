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
        /// <param name="ipAddress"></param>
        public Server(Action<ClientHandler, DTCMessageType, IMessage> callback, IPAddress ipAddress, int port, int timeoutNoActivity)
        {
            _callback = callback;
            _ipAddress = ipAddress;
            _port = port;
            _timeoutNoActivity = timeoutNoActivity;
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

        #endregion events

        /// <summary>
        /// Run until cancelled by Dispose()
        /// </summary>
        public async Task RunAsync()
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
                // Dispose() has been called
                CloseAllClientHandlers();
            }
            catch (Exception ex)
            {
                // A SocketException might be thrown from here
                throw;
            }
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
                    var clientHandler = new ClientHandler(_callback, tcpClient);
                    var task = Task.Factory.StartNew(() => clientHandler.RequestReaderAsync(), TaskCreationOptions.LongRunning);
                    lock (_lock)
                    {
                        _clientHandlerTasks.Add(task);
                        _clientHandlers.Add(clientHandler);
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
                catch (Exception ex)
                {
                    throw;
                }
            }
            await Task.WhenAll(_clientHandlerTasks).ConfigureAwait(false);
            CloseAllClientHandlers();
        }

        private void CloseAllClientHandlers()
        {
            lock (_lock)
            {
                foreach (var clientHandler in _clientHandlers)
                {
                    clientHandler.Dispose();
                }
                _clientHandlerTasks.Clear();
                _clientHandlers.Clear();
            }
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
                CloseAllClientHandlers();
                _tcpListener?.Stop();
                _tcpListener?.Server.Dispose();
                _tcpListener = null;
                IsConnected = false;
                _cts.Cancel();
                _timerCheckForDisconnects?.Dispose();
                _isDisposed = true;
            }
        }
    }
}
