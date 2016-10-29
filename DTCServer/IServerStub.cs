using System.Threading.Tasks;
using DTCPB;

namespace DTCServer
{
    public interface IServerStub
    {
        Task<LogonResponse> LogonRequestAsync(string clientHandlerId, LogonRequest logonRequest);
        Task LogoffAsync(string clientHandlerId, Logoff logoff);
        Task<EncodingResponse> EncodingRequestAsync(string clientHandlerId, EncodingRequest encodingRequest);
        Task HeartbeatAsync(string clientHandlerId, Heartbeat heartbeat);
    }
}