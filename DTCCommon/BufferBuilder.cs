// unset
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DTCCommon.Codecs;
using DTCPB;
using NLog;

namespace DTCCommon
{
    public class BufferBuilder : IDisposable
    {
        protected static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private readonly int _size;
        private readonly Codec _codec;
        private readonly MemoryStream _memoryStream;
        private readonly BinaryWriter _binaryWriter;

        /// <summary>
        /// Start building a buffer
        /// </summary>
        /// <param name="size"></param>
        /// <param name="codec">For debugging</param>
        public BufferBuilder(int size, Codec codec)
        {
            _size = size;
            _codec = codec;
            _memoryStream = new MemoryStream(size);
            _binaryWriter = new BinaryWriter(_memoryStream);
        }

        public byte[] Buffer => _memoryStream.ToArray();

        /// <summary>
        /// Write the header
        /// </summary>
        /// <param name="messageType"></param>
        /// <typeparam name="T"></typeparam>
        public void AddHeader(DTCMessageType messageType)
        {
            Add((short)_size);
            Add((short)messageType);
        }

        public void Add(short value)
        {
            _binaryWriter.Write(value);
        }

        public void Add(ushort value)
        {
            _binaryWriter.Write(value);
        }

        public void Add(int value)
        {
            _binaryWriter.Write(value);
        }

        public void Add(uint value)
        {
            _binaryWriter.Write(value);
        }

        public void Add(long value)
        {
            _binaryWriter.Write(value);
        }

        public void Add(ulong value)
        {
            _binaryWriter.Write(value);
        }

        public void Add(byte[] bytes)
        {
            _binaryWriter.Write(bytes);
        }

        public void Add(byte value)
        {
            _binaryWriter.Write(value);
        }

        public void Add(sbyte value)
        {
            _binaryWriter.Write(value);
        }

        public void Add(bool value)
        {
            _binaryWriter.Write(value);
        }

        public void Add(double value)
        {
            _binaryWriter.Write(value);
        }

        public void Add(float value)
        {
            _binaryWriter.Write(value);
        }

        public void Add(char[] value)
        {
            _binaryWriter.Write(value);
        }

        public async Task WriteAsync(Stream stream, CancellationToken cancellationToken)
        {
            try
            {
                //Logger.Debug($"{this} writing {Buffer.Length:N0} bytes to stream");
                if (Buffer.Length == 11)
                {
                }
                await stream.WriteAsync(Buffer, 0, Buffer.Length, cancellationToken).ConfigureAwait(false);
                //Logger.Debug($"{this} wrote {Buffer.Length:N0} bytes to stream");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void Dispose()
        {
            _memoryStream?.Dispose();
            _binaryWriter?.Dispose();
        }

        public override string ToString()
        {
            return $"BufferBuilder on {_codec}";
        }
    }
}