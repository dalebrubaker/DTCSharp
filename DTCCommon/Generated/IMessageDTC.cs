using System.IO;
using Google.Protobuf;

namespace DTCPB
{
    public interface IMessageDTC : IMessage
    {
        /// <summary>
        /// The message size EXCLUDING the 4-byte header (size + type)
        /// </summary>
        short Size { get; }

        DTCMessageType MessageType { get; }

        /// <summary>
        /// Write the properties in this message using the current encoding
        /// </summary>
        /// <param name="binaryWriter"></param>
        /// <param name="currentEncoding"></param>
        void Write(BinaryWriter binaryWriter, EncodingEnum currentEncoding);

        /// <summary>
        ///  Load the properties in this message from bytes, using the current encoding.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="currentEncoding"></param>
        void Load(byte[] bytes, EncodingEnum currentEncoding);
    }
}