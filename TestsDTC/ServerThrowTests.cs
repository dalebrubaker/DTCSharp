﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DTCServer;
using Xunit;
using Xunit.Abstractions;

namespace TestsDTC
{
    /// <summary>
    /// This tests needs to be in its own class or it causes others to fail
    /// </summary>
    public class ServerThrowTests : IDisposable
    {
        private readonly ITestOutputHelper _output;

        public ServerThrowTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public void Dispose()
        {
            _output.WriteLine("Disposing");
        }

        [Fact]
        public async Task StartDuplicateServerThrowsSocketExceptionTest()
        {
            var port = ClientServerTests.NextServerPort;
            using var server1 = new ExampleService(IPAddress.Loopback, port, 10, 20);
            await Task.Delay(200).ConfigureAwait(false);
            using var server2 = new ExampleService(IPAddress.Loopback, port, 10, 20);

            // Connecting to the same part, should throw a SocketException
            await Assert.ThrowsAsync<SocketException>(() => server2.RunAsync()).ConfigureAwait(false);
            await Task.Delay(100).ConfigureAwait(false);
            Assert.Equal(0, server1.NumberOfClientHandlers);
        }
    }
}