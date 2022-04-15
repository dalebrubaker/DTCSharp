using System;
using System.Net.Sockets;
using System.Timers;
using DTCCommon;
using DTCPB;
using Serilog;

namespace DTCClient
{
    /// <summary>
    /// Continuously retry connecting a client. Send events on Logon and Logoff
    /// </summary>
    public class ClientConnector : IDisposable
    {
        private readonly string _hostname;
        private readonly int _port;
        private readonly int _interval;
        private readonly EncodingEnum _requestedEncoding;
        private Timer _timer;
        private readonly LogonRequest _logonRequest;

        private ClientDTC _client;
        private LogonResponse _logonResponse;
        private bool _isDisposed;
        private readonly ILogger _logger;

        public string LastLogonResultText => _logonResponse?.ResultText;

        /// <summary>
        /// This ctor has the same parameters as the ClientDTC.LogonAsync method, because we will continually try to stay logged on.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="clientName"></param>
        /// <param name="interval">The interval (milliseconds) BETWEEN retry attempts, each one of which might take 2 or more seconds</param>
        /// <param name="heartbeatIntervalInSeconds"></param>
        /// <param name="requestedEncoding"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="generalTextData"></param>
        /// <param name="integer1"></param>
        /// <param name="integer2"></param>
        /// <param name="tradeAccount"></param>
        /// <param name="hardwareIdentifier"></param>
        /// <param name="hostname"></param>
        public ClientConnector(string hostname, int port, string clientName, int interval = 2000, int heartbeatIntervalInSeconds = 10, EncodingEnum requestedEncoding = EncodingEnum.ProtocolBuffers,
            string userName = "", string password = "", string generalTextData = "", int integer1 = 0, int integer2 = 0, string tradeAccount = "", string hardwareIdentifier = "")
        {
            _logger = Log.ForContext<ClientConnector>();
            _hostname = hostname;
            _port = port;
            _interval = interval;
            _requestedEncoding = requestedEncoding;
            _timer = new Timer(1); // Set interval very low for immediate check
            _timer.AutoReset = false; // raise the event only once
            _timer.Elapsed += TimerOnElapsed;
            _logonRequest = new LogonRequest
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
            _timer.Start();
            //s_logger.Debug($"Started timer in ctor of {this}");
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            DebugDTC.Assert(_client == null, "timer is turned off while we have a connected client");
            try
            {
                //s_logger.Debug($"Entered TimerOnElapsed, trying to connect {this}");
                _logonResponse = null;
                _client = new ClientDTC();
                _client.Start(_hostname, _port);
                (_logonResponse, var error) = _client.Logon(_logonRequest, _requestedEncoding);
                if (error.IsError || _logonResponse is not { Result: LogonStatusEnum.LogonSuccess })
                {
                    var resultText = error.IsError ? error.ResultText : LastLogonResultText;
                    _logger.Verbose($"Client {this} logon failed because {resultText}, timer started to keep trying");
                    // Tell the ClientHandler that we're done, rather than wait for this client to disappear
                    _client?.SendRequest(DTCMessageType.Logoff, new Logoff { Reason = "Unable to logon, giving up." });
                    _client = null;
                    if (_timer == null)
                    {
                        // disposed
                        return;
                    }
                    if ((int)_timer.Interval != _interval)
                    {
                        _timer.Interval = _interval; // Started very low in ctor so we get immediate check
                    }
                    _timer.Start();
                    return;
                }
                _client.DisconnectedEvent += ClientOnDisconnectedEvent;
                _logger.Verbose($"Sending OnClientConnected with {_client}");
                OnClientConnected(_client);
            }
            catch (SocketException sex)
            {
                // ignore exception, but keep trying unti the server appears
                _logger.Verbose($"Client {this} logon failed because {sex.Message}, timer started to keep trying.");
                _timer?.Start();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                throw;
            }
        }

        private void ClientOnDisconnectedEvent(object sender, EventArgs e)
        {
            if (_client != null)
            {
                OnClientDisconnected(_client);
                _client = null;
                _timer?.Start();
                //s_logger.Debug("Started timer after disconnect");
            }
        }

        /// <summary>
        /// This event occurs on successful logon to the client. Hook up events etc.
        /// </summary>
        public event EventHandler<ClientDTC> ClientConnected;

        private void OnClientConnected(ClientDTC client)
        {
            var tmp = ClientConnected;
            tmp?.Invoke(this, client);
        }

        /// <summary>
        /// This event occurs when client is lost due to Logoff, Server disappeared, etc. The client is disposed but is sent for informational purposes.
        /// You don't need to unhook events because it will soon be garbage collected.
        /// </summary>
        public event EventHandler<ClientDTC> ClientDisconnected;

        private void OnClientDisconnected(ClientDTC client)
        {
            var tmp = ClientDisconnected;
            tmp?.Invoke(this, client);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            if (_timer != null)
            {
                _timer.Elapsed -= TimerOnElapsed;
                _timer.Dispose();
                _timer = null;
            }
            if (_client != null)
            {
                OnClientDisconnected(_client);
                if (_client != null)
                {
                    _client.DisconnectedEvent -= ClientOnDisconnectedEvent;
                    _client.Dispose();
                    _client = null;
                }
            }
        }

        public override string ToString()
        {
            return $"{_logonRequest.ClientName}";
        }
    }
}