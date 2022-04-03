// unset
using System;
using System.IO;

namespace DTCCommon
{
    public class BufferBuilder : IDisposable
    {
        private readonly int _size;
        private readonly MemoryStream _memoryStream;
        private readonly BinaryWriter _binaryWriter;

        /// <summary>
        /// Start building a buffer
        /// </summary>
        /// <param name="size"></param>
        public BufferBuilder(int size)
        {
            _size = size;
            _memoryStream = new MemoryStream(size);
            _binaryWriter = new BinaryWriter(_memoryStream);
        }

        public byte[] Buffer
        {
            get
            {
                if (_memoryStream.Length != _size)
                {
                    throw new DTCSharpException("Buffer length is not the expected size");
                }
                return _memoryStream.ToArray();
            }
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

        public void Add(string value)
        {
            _binaryWriter.Write(value);
        }

        public void Dispose()
        {
            _memoryStream?.Dispose();
            _binaryWriter?.Dispose();
        }
    }
}