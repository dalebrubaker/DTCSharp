using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DTCClient;
using DTCPB;
using DTCServer;
using FluentAssertions;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace TestsDTC
{
    public class ClientServerTests : IDisposable
    {
        // ReSharper disable once InconsistentNaming
        private static int _nextServerPort = 54321;

        private static readonly object s_lock = new object();
        private readonly ITestOutputHelper _output;

        public ClientServerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public static int NextServerPort
        {
            get
            {
                lock (s_lock)
                {
                    return _nextServerPort++;
                }
            }
        }

        public void Dispose()
        {
            _output.WriteLine("Disposing");
        }

        [Fact]
        public async Task StartServerTest()
        {
            using var server = StartExampleServer(NextServerPort);
            await Task.Delay(100); // give it time to start
            Assert.Equal(0, server.NumberOfClientHandlers);
        }

        private ExampleService StartExampleServer(int port)
        {
            var server = new ExampleService(IPAddress.Loopback, port, 100, 200);
            return server;
        }

        private ClientDTC ConnectClient(int port, EncodingEnum encoding = EncodingEnum.ProtocolBuffers)
        {
            var client = new ClientDTC();
            client.Start("localhost", port);
            return ConnectClient(encoding, client);
        }

        private static ClientDTC ConnectClient(EncodingEnum encoding, ClientDTC client)
        {
            var (loginResponse, error) = client.Logon("TestClient", requestedEncoding: encoding);
            Assert.NotNull(loginResponse);
            Assert.False(error.IsError);
            return client;
        }

        [Fact]
        public async Task StartServerAddRemoveOneClientTest()
        {
            var numConnects = 0;
            var numDisconnects = 0;
            var port = NextServerPort;
            using var server = StartExampleServer(port);

            // Set up the handler to capture the ClientHandlerConnected event
            void ClientHandlerConnected(object s, ClientHandlerDTC clientHandler)
            {
                _output.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} connected to {clientHandler}");
                numConnects++;
            }

            server.ClientConnected += ClientHandlerConnected;

            // Set up the handler to capture the ClientHandlerDisconnected event
            void ClientHandlerDisconnected(object s, ClientHandlerDTC clientHandler)
            {
                _output.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} disconnected from {clientHandler}");
                numDisconnects++;
            }

            server.ClientDisconnected += ClientHandlerDisconnected;

            var sw = Stopwatch.StartNew();
            using (var client1 = ConnectClient(port))
            {
                while (numConnects != 1 && sw.ElapsedMilliseconds < 10000)
                {
                    // Wait for the client to connect
                    await Task.Delay(1).ConfigureAwait(false);
                }
                Assert.Equal(1, server.NumberOfClientHandlers);
                Assert.Equal(1, server.NumberOfClientHandlersConnected);
                var elapsed1 = sw.ElapsedMilliseconds;
                _output.WriteLine($"Elapsed msecs:{elapsed1}");
            }
            while (server.NumberOfClientHandlersConnected > 0 && server.NumberOfClientHandlers > 0)
            {
                // Wait for the client to be disconnected by the server
                await Task.Delay(1);
            }
            var elapsed2 = sw.ElapsedMilliseconds;
            _output.WriteLine($"Elapsed msecs:{elapsed2}");
            Assert.Equal(0, server.NumberOfClientHandlers);
        }

        [Fact]
        public async Task StartServerAddRemoveTwoClientsTest()
        {
            var numConnects = 0;
            var numDisconnects = 0;
            var port = NextServerPort;
            using (var server = StartExampleServer(port))
            {
                // Set up the handler to capture the ClientHandlerConnected event
                void ClientHandlerConnected(object s, ClientHandlerDTC clientHandler)
                {
                    var msg = $"Server in {nameof(StartServerAddRemoveOneClientTest)} connected to {clientHandler}";
                    _output.WriteLine(msg);
                    numConnects++;
                }

                server.ClientConnected += ClientHandlerConnected;

                // Set up the handler to capture the ClientHandlerDisconnected event
                void ClientHandlerDisconnected(object s, ClientHandlerDTC clientHandler)
                {
                    var msg = $"Server in {nameof(StartServerAddRemoveOneClientTest)} disconnected from {clientHandler}";
                    _output.WriteLine(msg);
                    numDisconnects++;
                }

                server.ClientDisconnected += ClientHandlerDisconnected;
                var sw = Stopwatch.StartNew();
                using (var client1 = ConnectClient(port))
                using (var client2 = ConnectClient(port))
                {
                    //while (numConnects != 2 && sw.ElapsedMilliseconds < 1000)
                    while (server.NumberOfClientHandlersConnected != 2) // && sw.ElapsedMilliseconds < 1000)
                    {
                        // Wait for the clients to connect
                        await Task.Delay(10).ConfigureAwait(false);
                    }
                    Assert.Equal(2, server.NumberOfClientHandlers);
                    var elapsed1 = sw.ElapsedMilliseconds;
                    _output.WriteLine($"Elapsed msecs:{elapsed1}");
                }
                while (server.NumberOfClientHandlersConnected > 0)
                {
                    // Wait for the client to be disconnected by the server
                    await Task.Delay(10);
                }
                var elapsed2 = sw.ElapsedMilliseconds;
                _output.WriteLine($"Elapsed msecs:{elapsed2}");
                server.NumberOfClientHandlersConnected.Should().Be(0, "NumberOfClientHandlersConnected");
                while (server.NumberOfClientHandlers > 0)
                {
                    // Wait longer to be sure they really go away
                    await Task.Delay(10);
                }
                server.NumberOfClientHandlers.Should().Be(0, "NumberOfClientHandlers");
            }
            await Task.Delay(100);
            numConnects.Should().Be(2);
            numDisconnects.Should().Be(0);
        }

        [Fact]
        public async Task ClientLogonAndHeartbeatTest()
        {
            var numConnects = 0;
            var numDisconnects = 0;
            var port = NextServerPort;
            var signal = new ManualResetEvent(false);
            using var server = StartExampleServer(port);

            // Set up the handler to capture the ClientConnected event
            void ClientConnected(object s, ClientHandlerDTC clientHandler)
            {
                _output.WriteLine($"Server in {nameof(ClientLogonAndHeartbeatTest)} connected to {clientHandler}");
                numConnects++;
                signal.Set();
            }

            server.ClientConnected += ClientConnected;

            // Set up the handler to capture the ClientDisconnected event
            void ClientDisconnected(object s, ClientHandlerDTC clientHandler)
            {
                _output.WriteLine($"Server in {nameof(ClientLogonAndHeartbeatTest)} disconnected from {clientHandler}");
                numDisconnects++;
            }

            server.ClientDisconnected += ClientDisconnected;

            using var client1 = ConnectClient(port);
            var sw = Stopwatch.StartNew();
            while (numConnects != 1 && sw.ElapsedMilliseconds < 1000)
            {
                // Wait for the client to connect
                await Task.Delay(1).ConfigureAwait(false);
            }
            Assert.Equal(1, server.NumberOfClientHandlers);

            // Set up the handler to capture the HeartBeat event
            var numHeartbeats = 0;

            void HeartbeatEvent(object s, Heartbeat e)
            {
                _output.WriteLine($"Client1 received a heartbeat after {sw.ElapsedMilliseconds} msecs");
                numHeartbeats++;
                if (numHeartbeats == 2)
                {
                    signal.Set();
                }
            }

            client1.HeartbeatEvent += HeartbeatEvent;
            sw.Restart();
            signal.WaitOne(1000);
            var elapsed = sw.ElapsedMilliseconds;
            _output.WriteLine($"Client1 received first two heartbeats in {elapsed} msecs");
        }

        [Fact]
        public void ClientDisconnectedServerDownTest()
        {
            var port = NextServerPort;
            var server = StartExampleServer(port);
            var wasConnected = false;
            var wasDisconnected = false;
            var client = new ClientDTC();
            client.Start("localhost", port);
            client.ConnectedEvent += Connected;
            ConnectClient(EncodingEnum.ProtocolBuffers, client);

            var sw = Stopwatch.StartNew();
            var signal = new ManualResetEvent(false);

            // Set up the handler to capture the Connected event
            void Connected(object s, EventArgs e)
            {
                _output.WriteLine($"Client is connected to {server.Address}");
                wasConnected = true;
            }

            // Set up the handler to capture the Disconnected event
            void Disconnected(object s, EventArgs e)
            {
                _output.WriteLine($"Client is disconnected from {server.Address}");
                wasDisconnected = true;
            }

            client.DisconnectedEvent += Disconnected;

            // Set up the handler to capture the HeartBeat event
            var numHeartbeats = 0;

            void HeartbeatEvent(object s, Heartbeat e)
            {
                _output.WriteLine($"Client1 received a heartbeat after {sw.ElapsedMilliseconds} msecs after server shutdown.");
                numHeartbeats++;
                signal.Set();
            }

            client.HeartbeatEvent += HeartbeatEvent;

            client.Dispose(); // So it won't error trying to read

            // Now kill the server
            server.Dispose();

            signal.WaitOne(1000);
            wasConnected.Should().BeTrue("Did the connect");
            wasDisconnected.Should().BeTrue("Did the disconnect.");
            numHeartbeats.Should().Be(0, "Should not get a heartbeat after server goes doen");
        }

        /// <summary>
        ///     see ClientForm.btnSubscribeCallbacks1_Click() for a WinForms example
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task MarketDataCompactTest()
        {
            // Set up the exampleService responses
            var port = NextServerPort;
            using var exampleService = StartExampleServer(port);
            using var client1 = ConnectClient(port);
            var sw = Stopwatch.StartNew();
            Assert.Equal(1, exampleService.NumberOfClientHandlers);

            var numSnapshots = 0;
            var numBidAsks = 0;
            var numTrades = 0;

            // Set up the handler to capture the MarketDataSnapshot event
            void MarketDataSnapshotEvent(object s, MarketDataSnapshot snapshot)
            {
                _output.WriteLine($"Client1 received a MarketDataSnapshot after {sw.ElapsedMilliseconds} msecs");
                numSnapshots++;
            }

            client1.MarketDataSnapshotEvent += MarketDataSnapshotEvent;

            // Set up the handler to capture the MarketDataUpdateTradeCompact events
            void MarketDataUpdateTradeCompactEvent(object s, MarketDataUpdateTradeCompact trade)
            {
                numTrades++;
                //s_logger.ConditionalDebug("numTrades={numTrades}", numTrades);
            }

            client1.MarketDataUpdateTradeCompactEvent += MarketDataUpdateTradeCompactEvent;

            // Set up the handler to capture the MarketDataUpdateBidAskCompact events
            void MarketDataUpdateBidAskCompactEvent(object s, MarketDataUpdateBidAskCompact bidAsk)
            {
                numBidAsks++;
                //s_logger.ConditionalDebug("numBidAsks={numBidAsks}", numBidAsks);
            }

            client1.MarketDataUpdateBidAskCompactEvent += MarketDataUpdateBidAskCompactEvent;

            // Now subscribe to the data
            sw.Restart();
            var symbolId = client1.SubscribeMarketData(1, "ESZ6", "");
            Assert.Equal(1u, symbolId);
            while (numTrades < exampleService.NumTradesAndBidAsksToSend || numBidAsks < exampleService.NumTradesAndBidAsksToSend)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }
            var elapsed = sw.ElapsedMilliseconds;
            _output.WriteLine($"Client1 received all trades and bid/asks in {elapsed} msecs");

            Assert.Equal(1, numSnapshots);
            Assert.Equal(exampleService.NumTradesAndBidAsksToSend, numTrades);
            Assert.Equal(exampleService.NumTradesAndBidAsksToSend, numBidAsks);

            client1.UnsubscribeMarketData(symbolId, "ESZ6", "");
        }

        [Fact(Skip = "Manual")] // manual with SC running"
        public void TcpConnectTests()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var client = new ClientDTC();
                client.Start("localhost", 11099);
                var ms = sw.ElapsedMilliseconds;
                var isConnected = client.Connected;
                client.NoDelay = true;
                var timeout = client.ReceiveTimeout;
                var readBufferSize = client.ReceiveBufferSize;
                //tcpClient.ReceiveTimeout = 100;
                var networkStream = client.GetStream();
                //var b = networkStream.ReadByte();
            }
            catch (Exception ex)
            {
                var ms = sw.ElapsedMilliseconds;
                _output.WriteLine(ex.Message);
                throw;
            }
            //
            // using var server = StartExampleServer(1000, NextServerPort);
            // await Task.Delay(100); // give it time to start
            // Assert.Equal(0, server.NumberOfClientHandlers);
        }
    }
}