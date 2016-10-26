using Google.Protobuf;

namespace DTCPB
{
    public interface IMessageSymbolId : IMessage
    {
        uint SymbolID { get; set; }
    }
}