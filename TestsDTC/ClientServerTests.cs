using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DTCClient;
using DTCCommon;
using DTCPB;
using DTCServer;
using NLog;
using TestsDTCServer;
using Xunit;
using Xunit.Abstractions;

namespace TestsDTC
{
    public class ClientServerTests : IDisposable
    {
        private static readonly ILogger s_logger = LogManager.GetCurrentClassLogger();

        // ReSharper disable once InconsistentNaming
        private static int _nextServerPort = 54321;

        private static readonly object Lock = new object();
        private readonly ITestOutputHelper _output;

        public ClientServerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public static int NextServerPort
        {
            get
            {
                lock (Lock)
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
            using (var server = StartExampleServer(1000, NextServerPort))
            {
                await Task.Delay(100).ConfigureAwait(true); // give it time to start
                Assert.Equal(0, server.NumberOfClientHandlers);
            }
        }

        private Server StartExampleServer(int timeoutNoActivity, int port, ExampleService exampleService = null)
        {
            if (exampleService == null)
            {
                exampleService = new ExampleService(100, 200);
            }
            var server = new Server((clientHandler, messageType, message) => exampleService.HandleRequestAsync(clientHandler, messageType, message, CancellationToken.None), IPAddress.Loopback, port, timeoutNoActivity);
            try
            {
                //TaskHelper.RunBg(async () => await server.RunAsync().ConfigureAwait(true));
                var task = server.RunAsync().ConfigureAwait(true);
            }
            catch (AggregateException)
            {
            }
            catch (ThreadAbortException)
            {
                // normal
            }
            return server;
        }

        private async Task<Client> ConnectClientAsync(int timeoutNoActivity, int timeoutForConnect, int port,
            EncodingEnum encoding = EncodingEnum.ProtocolBuffers)
        {
            var client = new Client(IPAddress.Loopback.ToString(), port, timeoutNoActivity);
            var encodingResponse = await client.ConnectAsync(encoding, "TestClient" + port, timeoutForConnect).ConfigureAwait(false);
            Assert.Equal(encoding, encodingResponse.Encoding);
            return client;
        }

        [Fact]
        public async Task StartServerAddRemoveOneClientTest()
        {
            var numConnects = 0;
            var numDisconnects = 0;
            const int TimeoutNoActivity = 10000;
            const int TimeoutForConnect = 10000;
            var port = NextServerPort;
            using (var server = StartExampleServer(TimeoutNoActivity, port))
            {
                // Set up the handler to capture the ClientHandlerConnected event
                void ClientHandlerConnected(object s, ClientHandler clientHandler)
                {
                    _output.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} connected to {clientHandler}");
                    numConnects++;
                }

                server.ClientConnected += ClientHandlerConnected;

                // Set up the handler to capture the ClientHandlerDisconnected event
                void ClientHandlerDisconnected(object s, ClientHandler clientHandler)
                {
                    _output.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} disconnected from {clientHandler}");
                    numDisconnects++;
                }

                server.ClientDisconnected += ClientHandlerDisconnected;

                var sw = Stopwatch.StartNew();
                using (var client1 = await ConnectClientAsync(TimeoutNoActivity, TimeoutForConnect, port).ConfigureAwait(false))
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
                    await Task.Delay(1).ConfigureAwait(true);
                }
                var elapsed2 = sw.ElapsedMilliseconds;
                _output.WriteLine($"Elapsed msecs:{elapsed2}");
                Assert.Equal(0, server.NumberOfClientHandlers);
            }
        }

        [Fact]
        public async Task StartServerAddRemoveTwoClientsTest()
        {
            var numConnects = 0;
            var numDisconnects = 0;
            const int TimeoutNoActivity = 1000;
            const int TimeoutForConnect = 10000;
            var port = NextServerPort;
            using (var server = StartExampleServer(TimeoutNoActivity, port))
            {
                // Set up the handler to capture the ClientHandlerConnected event
                void ClientHandlerConnected(object s, ClientHandler clientHandler)
                {
                    _output.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} connected to {clientHandler}");
                    numConnects++;
                }

                server.ClientConnected += ClientHandlerConnected;

                // Set up the handler to capture the ClientHandlerDisconnected event
                void ClientHandlerDisconnected(object s, ClientHandler clientHandler)
                {
                    _output.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} disconnected from {clientHandler}");
                    numDisconnects++;
                }

                server.ClientDisconnected += ClientHandlerDisconnected;
                var sw = Stopwatch.StartNew();
                using (var client1 = await ConnectClientAsync(TimeoutNoActivity, TimeoutForConnect, port).ConfigureAwait(false))
                using (var client2 = await ConnectClientAsync(TimeoutNoActivity, TimeoutForConnect, port).ConfigureAwait(false))
                {
                    //while (numConnects != 2 && sw.ElapsedMilliseconds < 1000)
                    while (server.NumberOfClientHandlersConnected != 2) // && sw.ElapsedMilliseconds < 1000)
                    {
                        // Wait for the clients to connect
                        await Task.Delay(1).ConfigureAwait(false);
                    }
                    Assert.Equal(2, server.NumberOfClientHandlers);
                    var elapsed1 = sw.ElapsedMilliseconds;
                    _output.WriteLine($"Elapsed msecs:{elapsed1}");
                }
                while (server.NumberOfClientHandlersConnected > 0)
                {
                    // Wait for the client to be disconnected by the server
                    await Task.Delay(1).ConfigureAwait(true);
                }
                var elapsed2 = sw.ElapsedMilliseconds;
                _output.WriteLine($"Elapsed msecs:{elapsed2}");
                Assert.Equal(0, server.NumberOfClientHandlersConnected);
                while (server.NumberOfClientHandlers > 0)
                {
                    // Wait longer to be sure they really go away
                    await Task.Delay(1).ConfigureAwait(true);
                }
                Assert.Equal(0, server.NumberOfClientHandlers);
            }
        }

        [Fact]
        public async Task ClientLogonAndHeartbeatTest()
        {
            var numConnects = 0;
            var numDisconnects = 0;
            const int TimeoutNoActivity = int.MaxValue; // 30000;
            const int TimeoutForConnect = int.MaxValue; // 5000;
            var port = NextServerPort;
            using (var server = StartExampleServer(TimeoutNoActivity, port))
            {
                // Set up the handler to capture the ClientConnected event
                void ClientConnected(object s, ClientHandler clientHandler)
                {
                    _output.WriteLine($"Server in {nameof(ClientLogonAndHeartbeatTest)} connected to {clientHandler}");
                    numConnects++;
                }

                server.ClientConnected += ClientConnected;

                // Set up the handler to capture the ClientDisconnected event
                void ClientDisconnected(object s, ClientHandler clientHandler)
                {
                    _output.WriteLine($"Server in {nameof(ClientLogonAndHeartbeatTest)} disconnected from {clientHandler}");
                    numDisconnects++;
                }

                server.ClientDisconnected += ClientDisconnected;

                using (var client1 = await ConnectClientAsync(TimeoutNoActivity, TimeoutForConnect, port).ConfigureAwait(false))
                {
                    var sw = Stopwatch.StartNew();
                    while (numConnects != 1 && sw.ElapsedMilliseconds < 1000)
                    {
                        // Wait for the client to connect
                        await Task.Delay(1).ConfigureAwait(false);
                    }
                    Assert.Equal(1, server.NumberOfClientHandlers);

                    var loginResponse = await client1.LogonAsync(1, false, TimeoutForConnect).ConfigureAwait(true);
                    Assert.NotNull(loginResponse);

                    // Set up the handler to capture the HeartBeat event
                    var numHeartbeats = 0;

                    void HeartbeatEvent(object s, Heartbeat e)
                    {
                        _output.WriteLine($"Client1 received a heartbeat after {sw.ElapsedMilliseconds} msecs");
                        numHeartbeats++;
                    }

                    client1.HeartbeatEvent += HeartbeatEvent;
                    sw.Restart();
                    while (numHeartbeats < 2)
                    {
                        // Wait for the first two heartbeats
                        await Task.Delay(1).ConfigureAwait(false);
                    }
                    var elapsed = sw.ElapsedMilliseconds;
                    _output.WriteLine($"Client1 received first two heartbeats in {elapsed} msecs");
                }
            }
        }

        [Fact]
        public async Task ClientDisconnectedServerDownTest()
        {
            const int TimeoutNoActivity = 10000;
            const int TimeoutForConnect = 10000;
            var port = NextServerPort;
            var server = StartExampleServer(TimeoutNoActivity, port);
            using (var client1 = await ConnectClientAsync(TimeoutNoActivity, TimeoutForConnect, port).ConfigureAwait(false))
            {
#pragma warning disable 219
                var isConnected = false;
#pragma warning restore 219
                var sw = Stopwatch.StartNew();
                while (!client1.IsConnected && sw.ElapsedMilliseconds < 1000)
                {
                    // Wait for the client to connect
                    await Task.Delay(1).ConfigureAwait(false);
                }

                // Set up the handler to capture the Connected event
                void Connected(object s, EventArgs e)
                {
                    _output.WriteLine($"Client is connected to {server.Address}");
                    isConnected = true;
                }

                client1.Connected += Connected;

                // Set up the handler to capture the Disconnected event
                void Disconnected(object s, Error error)
                {
                    _output.WriteLine($"Client is disconnected from {server.Address} due to {error}");
                    isConnected = false;
                }

                client1.Disconnected += Disconnected;

                // Set up the handler to capture the HeartBeat event
                var numHeartbeats = 0;

                void HeartbeatEvent(object s, Heartbeat e)
                {
                    _output.WriteLine($"Client1 received a heartbeat after {sw.ElapsedMilliseconds} msecs after server shutdown.");
                    numHeartbeats++;
                }

                client1.HeartbeatEvent += HeartbeatEvent;

                client1.Dispose(); // So it won't error trying to read

                // Now kill the server
                server.Dispose();
                sw.Restart();
                var loginResponse = await client1.LogonAsync(1, true, 5000).ConfigureAwait(true);
                Assert.Null(loginResponse);
            }
        }

        /// <summary>
        ///     see ClientForm.btnSubscribeCallbacks1_Click() for a WinForms example
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task MarketDataCompactTest()
        {
            const int TimeoutNoActivity = 10000;
            const int TimeoutForConnect = 10000;

            // Set up the exampleService responses
            var exampleService = new ExampleService(10, 20);
            var port = NextServerPort;

            using (var server = StartExampleServer(TimeoutNoActivity, port, exampleService))
            {
                using (var client1 = await ConnectClientAsync(TimeoutNoActivity, TimeoutForConnect, port).ConfigureAwait(false))
                {
                    var sw = Stopwatch.StartNew();
                    while (!client1.IsConnected) // && sw.ElapsedMilliseconds < 1000)
                    {
                        // Wait for the client to connect
                        await Task.Delay(1).ConfigureAwait(false);
                    }
                    Assert.Equal(1, server.NumberOfClientHandlers);

                    var loginResponse = await client1.LogonAsync(useHeartbeat: false, timeout: TimeoutNoActivity).ConfigureAwait(true);
                    Assert.NotNull(loginResponse);

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
                        //s_logger.Debug("numTrades={numTrades}", numTrades);
                    }

                    client1.MarketDataUpdateTradeCompactEvent += MarketDataUpdateTradeCompactEvent;

                    // Set up the handler to capture the MarketDataUpdateBidAskCompact events
                    void MarketDataUpdateBidAskCompactEvent(object s, MarketDataUpdateBidAskCompact bidAsk)
                    {
                        numBidAsks++;
                        //s_logger.Debug("numBidAsks={numBidAsks}", numBidAsks);
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

                    client1.UnsubscribeMarketData(symbolId);
                }
            }
        }
    }
}