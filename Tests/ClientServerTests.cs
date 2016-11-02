using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DTCClient;
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

        //[Fact]
        //public void LogonTest()
        //{
        //    var exampleServer = new ExampleServer();
        //    var server = new Server(exampleServer.HandleRequest, IPAddress.Loopback, port:54321, heartbeatIntervalInSeconds:60, useHeartbeat:false);
        //    var client = new Client(IPAddress.Loopback.ToString(), serverPort:54321, callbackToMainThread:true);
        //    var ctsServer = new CancellationTokenSource();
        //    server.Run(ctsServer.Token);

        //    client.EncodingResponseEvent


        //}
    }
}
