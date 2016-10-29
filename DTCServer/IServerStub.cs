using System.Threading.Tasks;
using DTCPB;

namespace DTCServer
{
    public interface IServerStub
    {
        Task<LogonResponse> LogonRequest(LogonRequest logonRequest);
    }
}