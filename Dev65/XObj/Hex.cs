using System.Text;

namespace Dev65.XObj;

/// <summary>
/// A utility class for hexadecimal string conversion.
/// </summary>
public static class Hex
{
    private static readonly string HexChars = "0123456789ABCDEF";

    /// <summary>
    /// Converts a value to a hexadecimal string of a given length.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="length">The required length.</param>
    /// <returns>The hexadecimal string value.</returns>
    public static string ToHex(long value, int length)
    {
        var buffer = new StringBuilder();

        switch (length)
        {
            case 8:
                buffer.Append(HexChars[(int)((value >> 28) & 0x0F)]);
                goto case 7;
            case 7:
                buffer.Append(HexChars[(int)((value >> 24) & 0x0F)]);
                goto case 6;
            case 6:
                buffer.Append(HexChars[(int)((value >> 20) & 0x0F)]);
                goto case 5;
            case 5:
                buffer.Append(HexChars[(int)((value >> 16) & 0x0F)]);
                goto case 4;
            case 4:
                buffer.Append(HexChars[(int)((value >> 12) & 0x0F)]);
                goto case 3;
            case 3:
                buffer.Append(HexChars[(int)((value >> 8) & 0x0F)]);
                goto case 2;
            case 2:
                buffer.Append(HexChars[(int)((value >> 4) & 0x0F)]);
                goto case 1;
            case 1:
                buffer.Append(HexChars[(int)(value & 0x0F)]);
                break;
        }

        return buffer.ToString();
    }
}
