using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DTCPB;

namespace DTCServer
{
    /// <summary>
    /// The server implementation that provides responses to client requests
    /// </summary>
    public class ServerStub : IServerStub
    {
        public Task<LogonResponse> LogonRequest(LogonRequest logonRequest)
        {
            throw new NotImplementedException();
        }
    }
}
