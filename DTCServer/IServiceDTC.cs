using DTCPB;
using Google.Protobuf;

namespace DTCServer
{
    public interface IServiceDTC
    {
        /// <summary>
        /// This method is called for every request received by a client connected to this server.
        /// WARNING! You must not block this thread for long, as further requests can't be received until you return from this method.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clientHandler">The handler for a particular client connected to this server</param>
        /// <param name="messageType">the message type</param>
        /// <param name="message">the message (a Google.Protobuf message)</param>
        /// <returns></returns>
        void HandleRequest<T>(ClientHandler clientHandler, DTCMessageType messageType, T message) where T : IMessage;
    }
}