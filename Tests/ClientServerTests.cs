using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public void LogonTest()
        {
            var serverStub = new ServerStub();
        }
    }
}
