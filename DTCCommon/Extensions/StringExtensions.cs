using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTCCommon.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Convert a message string to bytes of a fixed width
        /// </summary>
        /// <param name="str"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static byte[] ToFixedBytes(this string str, int width)
        {
            byte[] result = new byte[width];
            for (int i = 0; i < 3 && i < str.Length; i++)
            {
                result[i] = (byte)str[i];
            }
            return result;
        }
    }
}
