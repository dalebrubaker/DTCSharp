using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

        public ClientServerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public void Dispose()
        {
            _output.WriteLine("Disposing");
        }


        [Fact]
        public void StartServerTest()
        {
            using (var server = StartExampleServer(timeoutNoActivity: 1000))
            {
                Assert.Equal(0, server.NumberOfClientHandlers);
            }
        }

        private static Server StartExampleServer(int timeoutNoActivity, ExampleService exampleService = null)
        {
            if (exampleService == null)
            {
                exampleService = new ExampleService();
            }
            var server = new Server(exampleService.HandleRequest, IPAddress.Loopback, port: 54321, timeoutNoActivity: timeoutNoActivity, useHeartbeat: true);
            var ctsServer = new CancellationTokenSource();
#pragma warning disable 4014
            server.RunAsync(ctsServer.Token);
#pragma warning restore 4014
            return server;
        }

        private static async Task<Client> ConnectClientAsync(int timeoutNoActivity, int timeoutForConnect)
        {
            var client = new Client(IPAddress.Loopback.ToString(), serverPort: 54321, stayOnCallingThread: true, timeoutNoActivity: timeoutNoActivity);
            var encodingResponse = await client.ConnectAsync(EncodingEnum.ProtocolBuffers, timeoutForConnect).ConfigureAwait(false);
            Assert.Equal(EncodingEnum.ProtocolBuffers, encodingResponse.Encoding);
            return client;
        }

        [Fact]
        public async Task StartDuplicateServerThrowsSocketExceptionTest()
        {
            var ctsServer = new CancellationTokenSource();
            var exampleService = new ExampleService();
            using (var server = new Server(exampleService.HandleRequest, IPAddress.Loopback, port: 54321, timeoutNoActivity: 1000, useHeartbeat: true))
            {
#pragma warning disable 4014
                server.RunAsync(ctsServer.Token);
#pragma warning restore 4014
                await Task.Delay(100, ctsServer.Token).ConfigureAwait(false);
                using (var server2 = new Server(exampleService.HandleRequest, IPAddress.Loopback, port: 54321, timeoutNoActivity: 1000, useHeartbeat: true))
                {
                    await Assert.ThrowsAsync<SocketException>(() => server2.RunAsync(ctsServer.Token)).ConfigureAwait(false);
                    await Task.Delay(100, ctsServer.Token).ConfigureAwait(false);
                    Assert.Equal(0, server.NumberOfClientHandlers);
                }
            }
        }

        [Fact]
        public async Task StartServerAddRemoveOneClientTest()
        {
            int numConnects = 0;
            int numDisconnects = 0;
            const int timeoutNoActivity = 100;
            const int timeoutForConnect = 100;
            using (var server = StartExampleServer(timeoutNoActivity))
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
                using (var client1 = await ConnectClientAsync(timeoutNoActivity, timeoutForConnect).ConfigureAwait(false))
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
            using (var server = StartExampleServer(timeoutNoActivity))
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
                using (var client1 = await ConnectClientAsync(timeoutNoActivity, timeoutForConnect).ConfigureAwait(false))
                using (var client2 = await ConnectClientAsync(timeoutNoActivity, timeoutForConnect).ConfigureAwait(false))
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
            int numConnects = 0;
            int numDisconnects = 0;
            const int timeoutNoActivity = 2000;
            const int timeoutForConnect = 2000;
            using (var server = StartExampleServer(timeoutNoActivity))
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

                using (var client1 = await ConnectClientAsync(timeoutNoActivity, timeoutForConnect).ConfigureAwait(false))
                {
                    var sw = Stopwatch.StartNew();
                    while (numConnects != 1 && sw.ElapsedMilliseconds < 1000)
                    {
                        // Wait for the client to connect
                        await Task.Delay(1).ConfigureAwait(false);
                    }
                    Assert.Equal(1, server.NumberOfClientHandlers);

                    var loginResponse = await client1.LogonAsync(heartbeatIntervalInSeconds: 1, useHeartbeat: true, timeout: 5000, clientName: "TestClient1").ConfigureAwait(true);
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
            const int timeoutNoActivity = 10000;
            const int timeoutForConnect = 10000;
            var server = StartExampleServer(timeoutNoActivity);
            using (var client1 = await ConnectClientAsync(timeoutNoActivity, timeoutForConnect).ConfigureAwait(false))
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
                var loginResponse = await client1.LogonAsync(heartbeatIntervalInSeconds: 1, useHeartbeat: true, timeout: 5000, clientName: "TestClient1").ConfigureAwait(true);
                Assert.NotNull(loginResponse);

                while (isConnected)
                {
                    // wait for a client write failure
                    await Task.Delay(1).ConfigureAwait(true);
                }
                _output.WriteLine($"client disconnect took {sw.ElapsedMilliseconds} msecs");
            }
        }


        /// <summary>
        /// see ClientForm.btnSubscribeCallbacks1_Click() for a WinForms example
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task MarketDataCompactTest()
        {
            const int timeoutNoActivity = 2000;
            const int timeoutForConnect = 2000;

            // Set up the exampleService response
            var exampleService = new ExampleService();
            const int numTrades = 1000;
            for (int i = 0; i < numTrades; i++)
            {
                var trade = new MarketDataUpdateTradeCompact
                {
                    AtBidOrAsk = AtBidOrAskEnum.AtAsk,
                    DateTime = DateTime.UtcNow.UtcToDtcDateTime4Byte(),
                    Price = 2000f + i,
                    SymbolID = 1u,
                    Volume = i + 1,
                };
                exampleService.MarketDataUpdateTradeCompacts.Add(trade);
                var bidAsk = new MarketDataUpdateBidAskCompact
                {
                    AskPrice = 2000f + i,
                    BidPrice = 2000f + i - 0.25f,
                    AskQuantity = i,
                    BidQuantity = i + 1,
                    DateTime = DateTime.UtcNow.UtcToDtcDateTime4Byte(),
                    SymbolID = 1u,
                };
                exampleService.MarketDataUpdateBidAskCompacts.Add(bidAsk);
            }
            exampleService.MarketDataSnapshot = new MarketDataSnapshot
            {
                AskPrice = 1,
                AskQuantity = 2,
                BidAskDateTime = DateTime.UtcNow.UtcToDtcDateTime(),
                BidPrice = 3,
                BidQuantity = 4,
                LastTradeDateTime = DateTime.UtcNow.UtcToDtcDateTime(),
                LastTradePrice = 5,
                LastTradeVolume = 6,
                OpenInterest = 7,
                SessionHighPrice = 8,
            };


            using (var server = StartExampleServer(timeoutNoActivity, exampleService))
            {
                using (var client1 = await ConnectClientAsync(timeoutNoActivity, timeoutForConnect).ConfigureAwait(false))
                {
                    var sw = Stopwatch.StartNew();
                    while (!client1.IsConnected) // && sw.ElapsedMilliseconds < 1000)
                    {
                        // Wait for the client to connect
                        await Task.Delay(1).ConfigureAwait(false);
                    }
                    Assert.Equal(1, server.NumberOfClientHandlers);

                    var loginResponse = await client1.LogonAsync(heartbeatIntervalInSeconds: 1, useHeartbeat: true, timeout: 5000, clientName: "TestClient1").ConfigureAwait(true);
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


    }
}
