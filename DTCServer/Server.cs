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
        private int _port;
        private readonly int _heartbeatIntervalInSeconds;
        private readonly bool _useHeartbeat;
        private readonly IServerStub _serverStub;
        private IPAddress _ipAddress;

        /// <summary>
        /// Start a TCP Listener on port at ipAddress
        /// If not useHeartbeat, won't do a heartbeat. See: http://www.sierrachart.com/index.php?page=doc/DTCServer.php#HistoricalPriceDataServer
        /// </summary>
        /// <param name="serverStub">the server implementation that provides responses to client requests</param>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        /// <param name="heartbeatIntervalInSeconds">The initial interval in seconds that each side, the Client and the Server, needs to use to send HEARTBEAT messages to the other side. This should be a value from anywhere from 5 to 60 seconds.</param>
        /// <param name="useHeartbeat"><c>true</c>no heartbeat sent to server and none checked from server</param>
        public Server(IServerStub serverStub, IPAddress ipAddress, int port, int heartbeatIntervalInSeconds, bool useHeartbeat)
        {
            _serverStub = serverStub;
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
                    using (var tcpClient = await listener.AcceptTcpClientAsync())
                    {
                        var stream = tcpClient.GetStream();
                        var clientHandler = new ClientHandler(_serverStub, tcpClient, _useHeartbeat);
                        await clientHandler.Run(cancellationToken);
//                        var temp = tcpClient;
//#pragma warning disable 4014
//                        Task.Run(() => Handle(temp, cancellationToken), cancellationToken);
//#pragma warning restore 4014
                    }
                }
                catch (Exception ex)
                {
                    
                    throw;
                }
            }
            
        }

        private async Task Handle(TcpClient tcpClient, CancellationToken cancellationToken)
        {
            var stream = tcpClient.GetStream();
            var clientHandler = new ClientHandler(_serverStub, tcpClient, _useHeartbeat);
            await clientHandler.Run(cancellationToken);
        }

        private void ThrowEvent<T>(T message, EventHandler<EventArgs<T>> eventForMessage) where T : IMessage
        {
            var temp = eventForMessage; // for thread safety
            temp?.Invoke(this, new EventArgs<T>(message));
        }

        public override string ToString()
        {
            return $"{_ipAddress}:{_port}";
        }

    }
}
