﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DTCServer;
using TestServer;
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
            using var server1 = new ExampleService(IPAddress.Loopback, port, 1000, 10, 20);
            try
            {
                var task = Task.Run(async () => await server1.RunAsync().ConfigureAwait(false));
            }
            catch (Exception exception)
            {
                var typeName = exception.GetType().Name;
                throw;
            }
            await Task.Delay(200).ConfigureAwait(false);
            using var server2 = new ExampleService(IPAddress.Loopback, port, 1000, 10, 20);
            await Assert.ThrowsAsync<SocketException>(() => server2.RunAsync()).ConfigureAwait(false);
            await Task.Delay(100).ConfigureAwait(false);
            Assert.Equal(0, server1.NumberOfClientHandlers);
        }
    }
}