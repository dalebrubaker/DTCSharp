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

namespace DTCServer
{
    public class Server
    {
        private readonly int _port;
        private readonly int _heartbeatIntervalInSeconds;
        private readonly bool _useHeartbeat;
        private readonly Action<ClientHandler, DTCMessageType, IMessage> _callback;
        private readonly IPAddress _ipAddress;

        /// <summary>
        /// Start a TCP Listener on port at ipAddress
        /// If not useHeartbeat, won't do a heartbeat. See: http://www.sierrachart.com/index.php?page=doc/DTCServer.php#HistoricalPriceDataServer
        /// </summary>
        /// <param name="callback">the callback for all client requests</param>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        /// <param name="heartbeatIntervalInSeconds">The initial interval in seconds that each side, the Client and the Server, needs to use to send HEARTBEAT messages to the other side. This should be a value from anywhere from 5 to 60 seconds.</param>
        /// <param name="useHeartbeat"><c>true</c>no heartbeat sent to server and none checked from server</param>
        public Server(Action<ClientHandler, DTCMessageType, IMessage> callback, IPAddress ipAddress, int port, int heartbeatIntervalInSeconds, bool useHeartbeat)
        {
            _callback = callback;
            _ipAddress = ipAddress;
            _port = port;
            _heartbeatIntervalInSeconds = heartbeatIntervalInSeconds;
            _useHeartbeat = useHeartbeat;
        }

        /// <summary>
        /// Run until cancelled  by CancellationTokenSource.Cancel()
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async void Run(CancellationToken cancellationToken)
        {
            var listener = new TcpListener(_ipAddress, _port);
            listener.Start();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await listener.AcceptTcpClientAsync().ConfigureAwait(true);
                    tcpClient.NoDelay = true;
                    using (var clientHandler = new ClientHandler(_callback, tcpClient, _useHeartbeat))
                    {
                        await clientHandler.RunAsync(cancellationToken).ConfigureAwait(true);
                    } // Dispose() also closes tcpClient
                }
                catch (Exception ex)
                {
                    
                    throw;
                }
            }
        }

        public override string ToString()
        {
            return $"{_ipAddress}:{_port}";
        }

    }
}
