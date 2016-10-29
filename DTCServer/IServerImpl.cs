using System.Threading.Tasks;
using DTCPB;

namespace DTCServer
{
    /// <summary>
    /// Interface to be implemented to make a server
    /// </summary>
    public interface IServerImpl
    {
        Task<LogonResponse> LogonRequestAsync(string clientHandlerId, LogonRequest logonRequest);
        Task LogoffAsync(string clientHandlerId, Logoff logoff);
        Task<EncodingResponse> EncodingRequestAsync(string clientHandlerId, EncodingRequest encodingRequest);
        Task HeartbeatAsync(string clientHandlerId, Heartbeat heartbeat);
    }
}