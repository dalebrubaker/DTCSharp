using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace DTCCommon.Codecs
{
    public static class ZipCodec
    {
        /// <summary>
        /// Deflate (zlib compress) the bytesToCompress.
        /// Zip is just about as fast as Snappy when it is set for Fastest, maybe twice as slow (90/45 msecs) for Optimal
        /// Note from: https://msdn.microsoft.com/en-us/library/system.io.compression.deflatestream(v=vs.110).aspx
        ///     "Starting with the .NET Framework 4.5, the DeflateStream class uses the zlib library. 
        ///     As a result, it provides a better compression algorithm and, in most cases, a smaller compressed file 
        ///     than it provides in earlier versions of the .NET Framework."
        /// </summary>
        /// <param name="bytesToCompress">the bytes to compress</param>
        /// <param name="compressionLevel">Optimal or Fastest</param>
        /// <param name="offset">the offset into bytesToCompress</param>
        /// <param name="count">the count for bytesToCompress. The default -1 means to use bytesToCompress.Length</param>
        /// <returns>the compressed bytes</returns>
        public static byte[] Compress(byte[] bytesToCompress, CompressionLevel compressionLevel = CompressionLevel.Optimal, int offset = 0, int count = -1)
        {
            if (count < 0)
            {
                count = bytesToCompress.Length;
            }
            using (var ms = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(ms, compressionLevel))
                {
                    deflateStream.Write(bytesToCompress, offset, count);
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Deflate (zlib compress) the bytesToCompress to filePath
        /// Zip is just about as fast as Snappy when it is set for Fastest, maybe twice as slow (90/45 msecs) for Optimal
        /// Note from: https://msdn.microsoft.com/en-us/library/system.io.compression.deflatestream(v=vs.110).aspx
        ///     "Starting with the .NET Framework 4.5, the DeflateStream class uses the zlib library. 
        ///     As a result, it provides a better compression algorithm and, in most cases, a smaller compressed file 
        ///     than it provides in earlier versions of the .NET Framework."
        /// </summary>
        /// <param name="bytesToCompress">the bytes to compress</param>
        /// <param name="filePath">the fully qualified name of the file to create or overwrite</param>
        /// <param name="compressionLevel">Optimal or Fastest</param>
        /// <returns>the compressed bytes</returns>
        public static void CompressToFile(byte[] bytesToCompress, string filePath, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            using (var stream = File.Create(filePath))
            {
                using (var deflateStream = new DeflateStream(stream, compressionLevel))
                {
                    deflateStream.Write(bytesToCompress, 0, bytesToCompress.Length);
                }
            }
        }

        /// <summary>
        /// Deflate (zlib compress) the bytesToCompress to filePath
        /// Zip is just about as fast as Snappy when it is set for Fastest, maybe twice as slow (90/45 msecs) for Optimal
        /// Note from: https://msdn.microsoft.com/en-us/library/system.io.compression.deflatestream(v=vs.110).aspx
        ///     "Starting with the .NET Framework 4.5, the DeflateStream class uses the zlib library. 
        ///     As a result, it provides a better compression algorithm and, in most cases, a smaller compressed file 
        ///     than it provides in earlier versions of the .NET Framework."
        /// </summary>
        /// <param name="bytesToCompress">the bytes to compress</param>
        /// <param name="stream">the stream to which we want to write compressed bytes</param>
        /// <param name="compressionLevel">Optimal or Fastest</param>
        /// <returns>the compressed bytes</returns>
        public static void CompressToStream(byte[] bytesToCompress, Stream stream, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            using (var deflateStream = new DeflateStream(stream, compressionLevel))
            {
                deflateStream.Write(bytesToCompress, 0, bytesToCompress.Length);
            }
        }

        /// <summary>
        /// Deflate (zlib compress) the bytesToCompress.
        /// </summary>
        /// <param name="compressedBytes">the compressed bytes</param>
        /// <returns>the compressed bytes</returns>
        public static byte[] Decompress(byte[] compressedBytes)
        {
            using (var msOut = new MemoryStream())
            {
                using (var ms = new MemoryStream(compressedBytes))
                {
                    using (var deflateStream = new DeflateStream(ms, CompressionMode.Decompress))
                    {
                        deflateStream.CopyTo(msOut);
                    }
                    return msOut.ToArray();
                }
            }
        }

        /// <summary>
        /// Deflate (zlib compress) the bytesToCompress.
        /// </summary>
        /// <param name="filePath">the fully qualified name of the file to read</param>
        /// <returns>the compressed bytes</returns>
        public static byte[] DecompressFromFile(string filePath)
        {
            using (var msOut = new MemoryStream())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    using (var deflateStream = new DeflateStream(stream, CompressionMode.Decompress))
                    {
                        deflateStream.CopyTo(msOut);
                    }
                    return msOut.ToArray();
                }
            }
        }

        /// <summary>
        /// Deflate (zlib compress) the bytesToCompress.
        /// Zip is just about as fast as Snappy when it is set for Fastest, maybe twice as slow (90/45 msecs) for Optimal
        /// </summary>
        /// <param name="bytesToCompress">the bytes to compress</param>
        /// <returns>the compressed bytes</returns>
        public static Task<byte[]> CompressAsync(byte[] bytesToCompress)
        {
            return Task.Run(() => Compress(bytesToCompress));
        }

        /// <summary>
        /// Deflate (zlib compress) the bytesToCompress to filePath
        /// Zip is just about as fast as Snappy when it is set for Fastest, maybe twice as slow (90/45 msecs) for Optimal
        /// </summary>
        /// <param name="bytesToCompress">the bytes to compress</param>
        /// <param name="filePath">the fully qualified name of the file to create or overwrite</param>
        /// <param name="compressionLevel">Optimal or Fastest</param>
        /// <returns>the compressed bytes</returns>
        public static Task CompressToFileAsync(byte[] bytesToCompress, string filePath, CompressionLevel compressionLevel)
        {
            return Task.Run(() => CompressToFile(bytesToCompress, filePath, compressionLevel));
        }

        /// <summary>
        /// Deflate (zlib compress) the bytesToCompress.
        /// </summary>
        /// <param name="compressedBytes">the compressed bytes</param>
        /// <returns>the compressed bytes</returns>
        public static Task<byte[]> DecompressAsync(byte[] compressedBytes)
        {
            return Task.Run(() => Decompress(compressedBytes));
        }

        /// <summary>
        /// Deflate (zlib compress) the bytesToCompress.
        /// </summary>
        /// <param name="filePath">the fully qualified name of the file to read</param>
        /// <returns>the compressed bytes</returns>
        public static Task<byte[]> DecompressFromFileAsync(string filePath)
        {
            return Task.Run(() => DecompressFromFile(filePath));
        }
    }
}