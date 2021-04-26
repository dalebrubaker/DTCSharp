using System.IO;
using DTCPB;
using uint8_t = System.Byte;
using int32_t = System.Int32;

// ReSharper disable InconsistentNaming

namespace DTCCommon.Codecs
{
    public class CodecBinary : Codec
    {
        public CodecBinary(Stream stream) : base(stream, new CodecBinaryConverter())
        {
        }

        // Text string lengths. Copied from DTCProtocol.h
        private const int USERNAME_PASSWORD_LENGTH = 32;
        private const int SYMBOL_EXCHANGE_DELIMITER_LENGTH = 4;
        private const int SYMBOL_LENGTH = 64;
        private const int EXCHANGE_LENGTH = 16;
        private const int UNDERLYING_SYMBOL_LENGTH = 32;
        private const int SYMBOL_DESCRIPTION_LENGTH = 64; //Previously 48
        private const int EXCHANGE_DESCRIPTION_LENGTH = 48;
        private const int ORDER_ID_LENGTH = 32;
        private const int TRADE_ACCOUNT_LENGTH = 32;
        private const int TEXT_DESCRIPTION_LENGTH = 96;
        private const int TEXT_MESSAGE_LENGTH = 256;
        private const int ORDER_FREE_FORM_TEXT_LENGTH = 48;
        private const int CLIENT_SERVER_NAME_LENGTH = 48;
        private const int GENERAL_IDENTIFIER_LENGTH = 64;

        public override EncodingEnum Encoding => EncodingEnum.BinaryEncoding;
    }
}