using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DTCCommon.Extensions
{
    public static class StreamExtensions
    {
        /// <summary>
        /// This is similar to code in BinaryReader. It does an async read from stream until count bytes become available
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="headerBuffer"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>the number of bytes read. Always is count except when end-of-stream is reached</returns>
        public static async Task<int> ReadBytesAsync(this Stream stream, byte[] headerBuffer, int count, CancellationToken cancellationToken)
        {
            var numRead = 0;
            do
            {
                int n = await stream.ReadAsync(headerBuffer, numRead, count, cancellationToken).ConfigureAwait(true);
                if (n == 0)
                {
                    // reached end of stream
                    break;
                }
                numRead += n;
                count -= n;
            } while (count > 0);
            return numRead;
        }


    }
}
