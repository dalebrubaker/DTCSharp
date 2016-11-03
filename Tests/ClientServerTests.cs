using System;
using System.Collections.Generic;
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

namespace Tests
{
    public class ClientServerTests : IDisposable
    {
        public ClientServerTests()
        {
        }

        public void Dispose()
        {
            Console.WriteLine("Disposing");
        }
        
        [Fact]
        public async Task StartServerTest()
        {
            var exampleService = new ExampleService();
            var server = new Server(exampleService.HandleRequestAsync, IPAddress.Loopback, port: 54321, timeoutNoActivity:1000);
            var ctsServer = new CancellationTokenSource();
#pragma warning disable 4014
            server.RunAsync(ctsServer.Token);
#pragma warning restore 4014
            await Task.Delay(100, ctsServer.Token).ConfigureAwait(false);
            Assert.Equal(0, server.NumberOfClientHandlers);
            server.Stop();
        }

        [Fact]
        public async Task StartDuplicateServerThrowsSocketExceptionTest()
        {
            var ctsServer = new CancellationTokenSource();
            var exampleService = new ExampleService();
            var server = new Server(exampleService.HandleRequestAsync, IPAddress.Loopback, port: 54321, timeoutNoActivity: 1000);
#pragma warning disable 4014
            server.RunAsync(ctsServer.Token);
#pragma warning restore 4014
            await Task.Delay(100, ctsServer.Token).ConfigureAwait(false);
            var server2 = new Server(exampleService.HandleRequestAsync, IPAddress.Loopback, port: 54321, timeoutNoActivity: 1000);
            await Assert.ThrowsAsync<SocketException>(() => server2.RunAsync(ctsServer.Token)).ConfigureAwait(false);
            await Task.Delay(100, ctsServer.Token).ConfigureAwait(false);
            Assert.Equal(0, server.NumberOfClientHandlers);
            server.Stop();
            server2.Stop();
        }

        [Fact]
        public async Task StartServerAddRemoveOneClientTest()
        {

            var exampleService = new ExampleService();
            const int timeoutNoActivity = 500;
            var server = new Server(exampleService.HandleRequestAsync, IPAddress.Loopback, port: 54321, timeoutNoActivity: timeoutNoActivity);

            // Set up the handler to capture the ClientHandlerConnected event
            EventHandler<EventArgs<ClientHandler>> clientHandlerConnected = null;
            clientHandlerConnected = (s, e) =>
            {
                server.ClientHandlerConnected -= clientHandlerConnected; // unregister to avoid a potential memory leak
                var clientHandler = e.Data;
                Console.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} connected to {clientHandler}");
            };
            server.ClientHandlerConnected += clientHandlerConnected;

            // Set up the handler to capture the ClientHandlerDisconnected event
            EventHandler<EventArgs<ClientHandler>> clientHandlerDisconnected = null;
            clientHandlerDisconnected = (s, e) =>
            {
                server.ClientHandlerDisconnected -= clientHandlerDisconnected; // unregister to avoid a potential memory leak
                var clientHandler = e.Data;
                Console.WriteLine($"Server in {nameof(StartServerAddRemoveOneClientTest)} connected to {clientHandler}");
            };
            server.ClientHandlerDisconnected += clientHandlerDisconnected;


            var ctsServer = new CancellationTokenSource();
#pragma warning disable 4014
            server.RunAsync(ctsServer.Token);
#pragma warning restore 4014
            Assert.Equal(0, server.NumberOfClientHandlers);
            using (var client = new Client(IPAddress.Loopback.ToString(), serverPort: 54321, callbackToMainThread: true, timeoutNoActivity: timeoutNoActivity))
            {
                var encodingResponse = await client.ConnectAsync(EncodingEnum.ProtocolBuffers).ConfigureAwait(false);
                Assert.Equal(EncodingEnum.ProtocolBuffers, encodingResponse.Encoding);
                //await Task.Delay(10, ctsServer.Token).ConfigureAwait(false);
                Assert.Equal(1, server.NumberOfClientHandlers);
            }

            // Wait twice the timeoutNoActivity for the server to dispose of the clientHandler
            await Task.Delay(timeoutNoActivity * 2, ctsServer.Token).ConfigureAwait(false);
            Assert.Equal(0, server.NumberOfClientHandlers);
        }

        [Fact]
        public async Task StartServerAddRemoveTwoClientsTest()
        {
            var exampleService = new ExampleService();
            const int timeoutNoActivity = 100;
            var server = new Server(exampleService.HandleRequestAsync, IPAddress.Loopback, port: 54321, timeoutNoActivity: timeoutNoActivity);
            var ctsServer = new CancellationTokenSource();
#pragma warning disable 4014
            server.RunAsync(ctsServer.Token);
#pragma warning restore 4014
            //await Task.Delay(100, ctsServer.Token).ConfigureAwait(false);
            Assert.Equal(0, server.NumberOfClientHandlers);
            using (var client1 = new Client(IPAddress.Loopback.ToString(), serverPort: 54321, callbackToMainThread: true, timeoutNoActivity: timeoutNoActivity))
            using (var client2 = new Client(IPAddress.Loopback.ToString(), serverPort: 54321, callbackToMainThread: true, timeoutNoActivity: timeoutNoActivity))
            {
                var encodingResponse1 = await client1.ConnectAsync(EncodingEnum.ProtocolBuffers).ConfigureAwait(false);
                Assert.Equal(EncodingEnum.ProtocolBuffers, encodingResponse1.Encoding);
                var encodingResponse2 = await client2.ConnectAsync(EncodingEnum.ProtocolBuffers).ConfigureAwait(false);
                Assert.Equal(2, server.NumberOfClientHandlers);
            }

            // Wait twice the timeoutNoActivity for the server to dispose of the clientHandlers
            await Task.Delay(timeoutNoActivity * 2, ctsServer.Token).ConfigureAwait(false);
            Assert.Equal(0, server.NumberOfClientHandlers);
        }


    }
}
