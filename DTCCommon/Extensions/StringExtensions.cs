using System;
using System.Text;

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
            for (int i = 0; i < width && i < str.Length; i++)
            {
                result[i] = (byte)str[i];
            }
            return result;
        }

        /// <summary>
        /// Return the string starting at startIndex
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static string StringFromNullTerminatedBytes(this byte[] bytes, int startIndex)
        {
            var endIndex = Array.IndexOf(bytes, (byte)0, startIndex);
            if (endIndex < 0)
            {
                endIndex = bytes.Length;
            }
            var length = endIndex - startIndex;
            var result = Encoding.UTF8.GetString(bytes, startIndex, length);
            return result;
        }

        public static int ToInt32(this string str)
        {
            int result;
            if (int.TryParse(str, out result))
            {
                return result;
            }
            return 0;
        }
    }
}
