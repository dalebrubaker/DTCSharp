using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DTCClient;
using DTCCommon;
using DTCCommon.Extensions;
using DTCPB;
using DTCServer;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    public class ClientServerTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private int _nextServerPort;

        public ClientServerTests(ITestOutputHelper output)
        {
            _output = output;
            _nextServerPort = 54321;
        }

        public void Dispose()
        {
            _output.WriteLine("Disposing");
        }

        [Fact]
        public void StartServerTest()
        {
            using (var server = StartExampleServer(1000, _nextServerPort++))
            {
                Assert.Equal(0, server.NumberOfClientHandlers);
            }
        }

        private Server StartExampleServer(int timeoutNoActivity, int port, ExampleService exampleService = null, bool useHeartbeat = true)
        {
            if (exampleService == null)
            {
                exampleService = new ExampleService();
            }
            var server = new Server(exampleService.HandleRequest, IPAddress.Loopback, port : port, timeoutNoActivity: timeoutNoActivity, useHeartbeat: useHeartbeat);
            TaskHelper.RunBg(async () => await server.RunAsync().ConfigureAwait(false));
            return server;
        }

        private async Task<Client> ConnectClientAsync(int timeoutNoActivity, int timeoutForConnect, int port, EncodingEnum encoding = EncodingEnum.ProtocolBuffers)
        {
            var client = new Client(IPAddress.Loopback.ToString(), serverPort: port, timeoutNoActivity: timeoutNoActivity);
            var encodingResponse = await client.ConnectAsync(encoding, "TestClient" + port, timeoutForConnect).ConfigureAwait(false);
            Assert.Equal(encoding, encodingResponse.Encoding);
            return client;
        }

        [Fact]
        public async Task StartServerAddRemoveOneClientTest()
        {
            int numConnects = 0;
            int numDisconnects = 0;
            const int timeoutNoActivity = 1000;
            const int timeoutForConnect = 1000;
            var port = _nextServerPort++;
            using (var server = StartExampleServer(timeoutNoActivity, port))
            {
                // Set up the handler to capture the ClientHandlerConnected event
                EventHandler<EventArgs<ClientHandler>> clientHandlerConnected = (s, e) =>
                {
                    var clientHandler = e.Data;
                    _output.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} connected to {clientHandler}");
                    numConnects++;
                };
                server.ClientConnected += clientHandlerConnected;

                // Set up the handler to capture the ClientHandlerDisconnected event
                EventHandler<EventArgs<ClientHandler>> clientHandlerDisconnected = (s, e) =>
                {
                    var clientHandler = e.Data;
                    _output.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} disconnected from {clientHandler}");
                    numDisconnects++;
                };
                server.ClientDisconnected += clientHandlerDisconnected;
                var sw = Stopwatch.StartNew();
                using (var client1 = await ConnectClientAsync(timeoutNoActivity, timeoutForConnect, port).ConfigureAwait(false))
                {
                    while (numConnects != 1 && sw.ElapsedMilliseconds < 10000)
                    {
                        // Wait for the client to connect
                        await Task.Delay(1).ConfigureAwait(false);
                    }
                    Assert.Equal(1, server.NumberOfClientHandlers);
                    var elapsed1 = sw.ElapsedMilliseconds;
                    _output.WriteLine($"Elapsed msecs:{elapsed1}");
                }
                while (server.NumberOfClientHandlers > 0)
                {
                    // Wait for the client to be disconnected by the server
                    await Task.Delay(1).ConfigureAwait(true);
                }
                var elapsed2 = sw.ElapsedMilliseconds;
                _output.WriteLine($"Elapsed msecs:{elapsed2}");
                Assert.Equal(numDisconnects, numConnects);
                Assert.Equal(0, server.NumberOfClientHandlers);
            }
        }

        [Fact]
        public async Task StartServerAddRemoveTwoClientsTest()
        {
            int numConnects = 0;
            int numDisconnects = 0;
            const int timeoutNoActivity = 100;
            const int timeoutForConnect = 1000;
            var port = _nextServerPort++;
            using (var server = StartExampleServer(timeoutNoActivity, port))
            {
                // Set up the handler to capture the ClientHandlerConnected event
                EventHandler<EventArgs<ClientHandler>> clientHandlerConnected = (s, e) =>
                {
                    var clientHandler = e.Data;
                    _output.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} connected to {clientHandler}");
                    numConnects++;
                };
                server.ClientConnected += clientHandlerConnected;

                // Set up the handler to capture the ClientHandlerDisconnected event
                EventHandler<EventArgs<ClientHandler>> clientHandlerDisconnected = (s, e) =>
                {
                    var clientHandler = e.Data;
                    _output.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} disconnected from {clientHandler}");
                    numDisconnects++;
                };
                server.ClientDisconnected += clientHandlerDisconnected;
                var sw = Stopwatch.StartNew();
                using (var client1 = await ConnectClientAsync(timeoutNoActivity, timeoutForConnect, port).ConfigureAwait(false))
                using (var client2 = await ConnectClientAsync(timeoutNoActivity, timeoutForConnect, port).ConfigureAwait(false))
                {
                    //while (numConnects != 2 && sw.ElapsedMilliseconds < 1000)
                    while (server.NumberOfClientHandlers != 2) // && sw.ElapsedMilliseconds < 1000)
                    {
                        // Wait for the clients to connect
                        await Task.Delay(1).ConfigureAwait(false);
                    }
                    Assert.Equal(2, server.NumberOfClientHandlers);
                    var elapsed1 = sw.ElapsedMilliseconds;
                    _output.WriteLine($"Elapsed msecs:{elapsed1}");
                }
                while (server.NumberOfClientHandlers > 0)
                {
                    // Wait for the client to be disconnected by the server
                    await Task.Delay(1).ConfigureAwait(true);
                }
                var elapsed2 = sw.ElapsedMilliseconds;
                _output.WriteLine($"Elapsed msecs:{elapsed2}");
                Assert.Equal(numDisconnects, numConnects);
                Assert.Equal(0, server.NumberOfClientHandlers);
            }
        }


        [Fact]
        public async Task ClientLogonAndHeartbeatTest()
        {
            await Task.Delay(1000).ConfigureAwait(false);
            int numConnects = 0;
            int numDisconnects = 0;
            const int timeoutNoActivity = 30000;
            const int timeoutForConnect = 5000;
            var port = _nextServerPort++;
            using (var server = StartExampleServer(timeoutNoActivity, port))
            {
                // Set up the handler to capture the ClientConnected event
                EventHandler<EventArgs<ClientHandler>> clientConnected = (s, e) =>
                {
                    var clientHandler = e.Data;
                    _output.WriteLine($"Server in {nameof(ClientLogonAndHeartbeatTest)} connected to {clientHandler}");
                    numConnects++;
                };
                server.ClientConnected += clientConnected;

                // Set up the handler to capture the ClientDisconnected event
                EventHandler<EventArgs<ClientHandler>> clientDisconnected = (s, e) =>
                {
                    var clientHandler = e.Data;
                    _output.WriteLine($"Server in {nameof(ClientLogonAndHeartbeatTest)} disconnected from {clientHandler}");
                    numDisconnects++;
                };
                server.ClientDisconnected += clientDisconnected;

                using (var client1 = await ConnectClientAsync(timeoutNoActivity, timeoutForConnect, port).ConfigureAwait(false))
                {
                    var sw = Stopwatch.StartNew();
                    while (numConnects != 1 && sw.ElapsedMilliseconds < 1000)
                    {
                        // Wait for the client to connect
                        await Task.Delay(1).ConfigureAwait(false);
                    }
                    Assert.Equal(1, server.NumberOfClientHandlers);

                    var loginResponse = await client1.LogonAsync(heartbeatIntervalInSeconds: 1, useHeartbeat: true, timeout: 5000).ConfigureAwait(true);
                    Assert.NotNull(loginResponse);

                    // Set up the handler to capture the HeartBeat event
                    var numHeartbeats = 0;
                    EventHandler<EventArgs<Heartbeat>> heartbeatEvent = null;
                    heartbeatEvent = (s, e) =>
                    {
                        var heartbeat = e.Data;
                        _output.WriteLine($"Client1 received a heartbeat after {sw.ElapsedMilliseconds} msecs");
                        numHeartbeats++;
                    };
                    client1.HeartbeatEvent += heartbeatEvent;
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
            const int timeoutNoActivity = 1000;
            const int timeoutForConnect = 1000;
            var port = _nextServerPort++;
            var server = StartExampleServer(timeoutNoActivity, port);
            using (var client1 = await ConnectClientAsync(timeoutNoActivity, timeoutForConnect, port).ConfigureAwait(false))
            {
                bool isConnected = false;
                var sw = Stopwatch.StartNew();
                while (!client1.IsConnected && sw.ElapsedMilliseconds < 1000)
                {
                    // Wait for the client to connect
                    await Task.Delay(1).ConfigureAwait(false);
                }

                // Set up the handler to capture the Connected event
                EventHandler  connected = (s, e) =>
                {
                    _output.WriteLine($"Client is connected to {server.Address}");
                    isConnected = true;
                };
                client1.Connected += connected;

                // Set up the handler to capture the Disconnected event
                EventHandler<EventArgs<Error>> disconnected = (s, e) =>
                {
                    var error = e.Data;
                    _output.WriteLine($"Client is disconnected from {server.Address} due to {error}");
                    isConnected = false;
                };
                client1.Disconnected += disconnected;

                // Set up the handler to capture the HeartBeat event
                var numHeartbeats = 0;
                EventHandler<EventArgs<Heartbeat>> heartbeatEvent = null;
                heartbeatEvent = (s, e) =>
                {
                    var heartbeat = e.Data;
                    _output.WriteLine($"Client1 received a heartbeat after {sw.ElapsedMilliseconds} msecs after server shutdown.");
                    numHeartbeats++;
                };
                client1.HeartbeatEvent += heartbeatEvent;

                // Now kill the server
                server.Dispose();

                sw.Restart();
                var loginResponse = await client1.LogonAsync(heartbeatIntervalInSeconds: 1, useHeartbeat: true, timeout: 5000).ConfigureAwait(true);
                Assert.Null(loginResponse);
            }
        }


        /// <summary>
        /// see ClientForm.btnSubscribeCallbacks1_Click() for a WinForms example
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task MarketDataCompactTest()
        {
            const int timeoutNoActivity = 10000;
            const int timeoutForConnect = 10000;

            // Set up the exampleService responses
            var exampleService = new ExampleService();
            var port = _nextServerPort++;
            
            using (var server = StartExampleServer(timeoutNoActivity, port, exampleService))
            {
                using (var client1 = await ConnectClientAsync(timeoutNoActivity, timeoutForConnect, port).ConfigureAwait(false))
                {
                    var sw = Stopwatch.StartNew();
                    while (!client1.IsConnected) // && sw.ElapsedMilliseconds < 1000)
                    {
                        // Wait for the client to connect
                        await Task.Delay(1).ConfigureAwait(false);
                    }
                    Assert.Equal(1, server.NumberOfClientHandlers);

                    var loginResponse = await client1.LogonAsync(heartbeatIntervalInSeconds: 1, useHeartbeat: true, timeout: 5000).ConfigureAwait(true);
                    Assert.NotNull(loginResponse);

                    var numSnapshots = 0;
                    var numBidAsks = 0;
                    var numTrades = 0;

                    // Set up the handler to capture the MarketDataSnapshot event
                    EventHandler<EventArgs<MarketDataSnapshot>> marketDataSnapshotEvent = (s, e) =>
                    {
                        var snapshot = e.Data;
                        _output.WriteLine($"Client1 received a MarketDataSnapshot after {sw.ElapsedMilliseconds} msecs");
                        numSnapshots++;
                    };
                    client1.MarketDataSnapshotEvent += marketDataSnapshotEvent;

                    // Set up the handler to capture the MarketDataUpdateTradeCompact events
                    EventHandler<EventArgs<MarketDataUpdateTradeCompact>> marketDataUpdateTradeCompactEvent = (s, e) =>
                    {
                        var trade = e.Data;
                        numTrades++;
                    };
                    client1.MarketDataUpdateTradeCompactEvent += marketDataUpdateTradeCompactEvent;

                    // Set up the handler to capture the MarketDataUpdateBidAskCompact events
                    EventHandler<EventArgs<MarketDataUpdateBidAskCompact>> marketDataUpdateBidAskCompactEvent = (s, e) =>
                    {
                        var bidAsk = e.Data;
                        numBidAsks++;
                    };
                    client1.MarketDataUpdateBidAskCompactEvent += marketDataUpdateBidAskCompactEvent;

                    // Now subscribe to the data
                    sw.Restart();
                    var symbolId = client1.SubscribeMarketData("ESZ6", "");
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

        /// <summary>
        /// See ClientForm.btnGetHistoricalTicks_Click() for a WinForms example
        /// Also see Client.GetHistoricalPriceDataRecordResponsesAsync() which does something very similar
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HistoricalPriceDataRecordResponseTickNotZippedTest()
        {
            const int timeoutNoActivity = 10000;
            const int timeoutForConnect = 10000;
            const bool useZLibCompression = false;
            var isFinalRecordReceived = false;

            // Set up the exampleService responses
            var exampleService = new ExampleService();
            var port = _nextServerPort++;

            using (var server = StartExampleServer(timeoutNoActivity, port, exampleService, useHeartbeat:false))
            {
                using (var clientHistorical = await ConnectClientAsync(timeoutNoActivity, timeoutForConnect, port, EncodingEnum.BinaryEncoding).ConfigureAwait(false))
                {
                    var sw = Stopwatch.StartNew();
                    while (!clientHistorical.IsConnected) // && sw.ElapsedMilliseconds < 1000)
                    {
                        // Wait for the client to connect
                        await Task.Delay(1).ConfigureAwait(false);
                    }
                    Assert.Equal(1, server.NumberOfClientHandlers);

                    var loginResponse = await clientHistorical.LogonAsync(heartbeatIntervalInSeconds: 1, useHeartbeat: false, timeout: 5000).ConfigureAwait(true);
                    Assert.NotNull(loginResponse);

                    var numHistoricalPriceDataResponseHeader = 0;
                    var numTrades = 0;

                    // Set up the handler to capture the HistoricalPriceDataResponseHeader event
                    EventHandler<EventArgs<HistoricalPriceDataResponseHeader>> responseHeaderEvent = (s, e) =>
                    {
                        var header = e.Data;
                        _output.WriteLine($"Client1 received a HistoricalPriceDataResponseHeader after {sw.ElapsedMilliseconds} msecs");
                        numHistoricalPriceDataResponseHeader++;
                    };
                    clientHistorical.HistoricalPriceDataResponseHeaderEvent += responseHeaderEvent;

                    // Set up the handler to capture the HistoricalPriceDataRecordResponse events
                    EventHandler<EventArgs<HistoricalPriceDataRecordResponse>> historicalPriceDataRecordResponseEvent = (s, e) =>
                    {
                        var trade = e.Data;
                        numTrades++;
                        if (trade.IsFinalRecord != 0)
                        {
                            isFinalRecordReceived = true;
                        }
                    };
                    clientHistorical.HistoricalPriceDataRecordResponseEvent += historicalPriceDataRecordResponseEvent;

                    // Now request the data
                    var request = new HistoricalPriceDataRequest
                    {
                        RequestID = 1,
                        Symbol = "ESZ6",
                        Exchange = "",
                        RecordInterval = HistoricalDataIntervalEnum.IntervalTick,
                        StartDateTime = DateTime.UtcNow.UtcToDtcDateTime(), // ignored in this test
                        EndDateTime = DateTime.UtcNow.UtcToDtcDateTime(), // ignored in this test
                        MaxDaysToReturn = 1, // ignored in this test
                        UseZLibCompression = useZLibCompression ? 1 : 0,
                        RequestDividendAdjustedStockData = 0,
                        Flag1 = 0,
                    };
                    sw.Restart();
                    clientHistorical.SendRequest(DTCMessageType.HistoricalPriceDataRequest, request);
                    while (!isFinalRecordReceived)
                    {
                        await Task.Delay(100).ConfigureAwait(false);
                    }
                    var elapsed = sw.ElapsedMilliseconds;
                    _output.WriteLine($"Client1 received all {numTrades} historical trades in {elapsed} msecs");

                    Assert.Equal(1, numHistoricalPriceDataResponseHeader);
                    Assert.Equal(exampleService.NumHistoricalPriceDataRecordsToSend, numTrades);
                }
            }
        }

        /// <summary>
        /// See ClientForm.btnGetHistoricalTicks_Click() for a WinForms example
        /// Also see Client.GetHistoricalPriceDataRecordResponsesAsync() which does something very similar
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HistoricalPriceDataRecordResponseTickZippedTest()
        {
            const int timeoutNoActivity = 10000;
            const int timeoutForConnect = 10000;
            const bool useZLibCompression = true;
            var isFinalRecordReceived = false;

            // Set up the exampleService responses
            var exampleService = new ExampleService();
            var port = _nextServerPort++;

            using (var server = StartExampleServer(timeoutNoActivity, port, exampleService, useHeartbeat: false))
            {
                using (var clientHistorical = await ConnectClientAsync(timeoutNoActivity, timeoutForConnect, port, EncodingEnum.BinaryEncoding).ConfigureAwait(false))
                {
                    var sw = Stopwatch.StartNew();
                    while (!clientHistorical.IsConnected) // && sw.ElapsedMilliseconds < 1000)
                    {
                        // Wait for the client to connect
                        await Task.Delay(1).ConfigureAwait(false);
                    }
                    Assert.Equal(1, server.NumberOfClientHandlers);

                    var loginResponse = await clientHistorical.LogonAsync(heartbeatIntervalInSeconds: 1, useHeartbeat: false, timeout: 5000).ConfigureAwait(true);
                    Assert.NotNull(loginResponse);

                    var numHistoricalPriceDataResponseHeader = 0;
                    var numTrades = 0;

                    // Set up the handler to capture the HistoricalPriceDataResponseHeader event
                    EventHandler<EventArgs<HistoricalPriceDataResponseHeader>> responseHeaderEvent = (s, e) =>
                    {
                        var header = e.Data;
                        _output.WriteLine($"Client1 received a HistoricalPriceDataResponseHeader after {sw.ElapsedMilliseconds} msecs");
                        numHistoricalPriceDataResponseHeader++;
                    };
                    clientHistorical.HistoricalPriceDataResponseHeaderEvent += responseHeaderEvent;

                    // Set up the handler to capture the HistoricalPriceDataRecordResponse events
                    EventHandler<EventArgs<HistoricalPriceDataRecordResponse>> historicalPriceDataRecordResponseEvent = (s, e) =>
                    {
                        var trade = e.Data;
                        numTrades++;
                        if (trade.IsFinalRecord != 0)
                        {
                            isFinalRecordReceived = true;
                        }
                    };
                    clientHistorical.HistoricalPriceDataRecordResponseEvent += historicalPriceDataRecordResponseEvent;

                    // Now request the data
                    var request = new HistoricalPriceDataRequest
                    {
                        RequestID = 1,
                        Symbol = "ESZ6",
                        Exchange = "",
                        RecordInterval = HistoricalDataIntervalEnum.IntervalTick,
                        StartDateTime = DateTime.UtcNow.UtcToDtcDateTime(), // ignored in this test
                        EndDateTime = DateTime.UtcNow.UtcToDtcDateTime(), // ignored in this test
                        MaxDaysToReturn = 1, // ignored in this test
                        UseZLibCompression = useZLibCompression ? 1 : 0,
                        RequestDividendAdjustedStockData = 0,
                        Flag1 = 0,
                    };
                    sw.Restart();
                    clientHistorical.SendRequest(DTCMessageType.HistoricalPriceDataRequest, request);
                    while (!isFinalRecordReceived)
                    {
                        await Task.Delay(100).ConfigureAwait(false);
                    }
                    var elapsed = sw.ElapsedMilliseconds;
                    _output.WriteLine($"Client1 received all {numTrades} historical trades in {elapsed} msecs");

                    Assert.Equal(1, numHistoricalPriceDataResponseHeader);
                    Assert.Equal(exampleService.NumHistoricalPriceDataRecordsToSend, numTrades);
                }
            }
        }

    }
}
