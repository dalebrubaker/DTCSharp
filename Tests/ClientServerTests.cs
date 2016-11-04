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

        private static Server StartExampleServer(int timeoutNoActivity)
        {
            var exampleService = new ExampleService();
            var server = new Server(exampleService.HandleRequest, IPAddress.Loopback, port: 54321, timeoutNoActivity: timeoutNoActivity, useHeartbeat: true);
            var ctsServer = new CancellationTokenSource();
#pragma warning disable 4014
            server.RunAsync(ctsServer.Token);
#pragma warning restore 4014
            return server;
        }

        private static async Task<Client> ConnectClientAsync(int timeoutNoActivity)
        {
            var client = new Client(IPAddress.Loopback.ToString(), serverPort: 54321, stayOnCallingThread: true, timeoutNoActivity: timeoutNoActivity);
            var encodingResponse = await client.ConnectAsync(EncodingEnum.ProtocolBuffers).ConfigureAwait(false);
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
            using (var server = StartExampleServer(timeoutNoActivity))
            {
                // Set up the handler to capture the ClientHandlerConnected event
                EventHandler<EventArgs<ClientHandler>> clientHandlerConnected = null;
                clientHandlerConnected = (s, e) =>
                {
                    var clientHandler = e.Data;
                    _output.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} connected to {clientHandler}");
                    numConnects++;
                };
                server.ClientConnected += clientHandlerConnected;

                // Set up the handler to capture the ClientHandlerDisconnected event
                EventHandler<EventArgs<ClientHandler>> clientHandlerDisconnected = null;
                clientHandlerDisconnected = (s, e) =>
                {
                    var clientHandler = e.Data;
                    _output.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} disconnected from {clientHandler}");
                    numDisconnects++;
                };
                server.ClientDisconnected += clientHandlerDisconnected;
                var sw = Stopwatch.StartNew();
                using (var client1 = await ConnectClientAsync(timeoutNoActivity: 1000).ConfigureAwait(false))
                {
                    while (numConnects != 1 && sw.ElapsedMilliseconds < 1000)
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
            using (var server = StartExampleServer(timeoutNoActivity))
            {
                // Set up the handler to capture the ClientHandlerConnected event
                EventHandler<EventArgs<ClientHandler>> clientHandlerConnected = null;
                clientHandlerConnected = (s, e) =>
                {
                    var clientHandler = e.Data;
                    _output.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} connected to {clientHandler}");
                    numConnects++;
                };
                server.ClientConnected += clientHandlerConnected;

                // Set up the handler to capture the ClientHandlerDisconnected event
                EventHandler<EventArgs<ClientHandler>> clientHandlerDisconnected = null;
                clientHandlerDisconnected = (s, e) =>
                {
                    var clientHandler = e.Data;
                    _output.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} disconnected from {clientHandler}");
                    numDisconnects++;
                };
                server.ClientDisconnected += clientHandlerDisconnected;
                var sw = Stopwatch.StartNew();
                using (var client1 = await ConnectClientAsync(timeoutNoActivity: 1000).ConfigureAwait(false))
                using (var client2 = await ConnectClientAsync(timeoutNoActivity: 1000).ConfigureAwait(false))
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
            using (var server = StartExampleServer(timeoutNoActivity))
            {
                // Set up the handler to capture the ClientHandlerConnected event
                EventHandler<EventArgs<ClientHandler>> clientHandlerConnected = null;
                clientHandlerConnected = (s, e) =>
                {
                    var clientHandler = e.Data;
                    _output.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} connected to {clientHandler}");
                    numConnects++;
                };
                server.ClientConnected += clientHandlerConnected;

                // Set up the handler to capture the ClientHandlerDisconnected event
                EventHandler<EventArgs<ClientHandler>> clientHandlerDisconnected = null;
                clientHandlerDisconnected = (s, e) =>
                {
                    var clientHandler = e.Data;
                    _output.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} disconnected from {clientHandler}");
                    numDisconnects++;
                };
                server.ClientDisconnected += clientHandlerDisconnected;
                using (var client1 = await ConnectClientAsync(timeoutNoActivity: timeoutNoActivity).ConfigureAwait(false))
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
            using (var server = StartExampleServer(timeoutNoActivity))
            {

            }
        }


    }
}
