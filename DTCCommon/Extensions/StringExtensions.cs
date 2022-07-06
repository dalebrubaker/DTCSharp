using System;
using System.Text;
using Serilog;

// ReSharper disable once CheckNamespace
namespace DTCCommon;

public static class StringExtensions
{
    private static readonly ILogger s_logger = Log.ForContext(typeof(StringExtensions));

    /// <summary>
    ///     Convert a message string to bytes of a fixed width
    /// </summary>
    /// <param name="str"></param>
    /// <param name="width"></param>
    /// <returns></returns>
    public static byte[] ToFixedBytes(this string str, int width)
    {
        var result = new byte[width];
        for (var i = 0; i < width && i < str.Length; i++)
        {
            result[i] = (byte)str[i];
        }
        return result;
    }

    /// <summary>
    ///     Return the string starting at startIndex
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="startIndex"></param>
    /// <returns></returns>
    public static string StringFromNullTerminatedBytes(this byte[] bytes, int startIndex)
    {
        try
        {
            if (startIndex >= bytes.Length)
            {
                throw new DTCSharpException("Why?");
            }
            var endIndex = Array.IndexOf(bytes, (byte)0, startIndex);
            if (endIndex < 0)
            {
                endIndex = bytes.Length;
            }
            var length = endIndex - startIndex;
            var result = Encoding.UTF8.GetString(bytes, startIndex, length);
            return result;
        }
        catch (Exception ex)
        {
            s_logger.Error(ex, "{Message}", ex.Message);
            throw;
        }
    }

    public static int ToInt(this string str)
    {
        if (int.TryParse(str, out var result))
        {
            return result;
        }
        return 0;
    }
}