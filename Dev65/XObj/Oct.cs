using System.Text;

namespace Dev65.XObj;

/**
 * A utility class for octal string conversion.
 * 
 * author Andrew Jacobs
 */
public abstract class Oct
{
    /**
     * Converts a value to an octal string of a given length.
     * 
     * param value  The value to convert.
     * param length The required length.
     * return The octal string value.
     */
    public static string ToOct(long value, int length)
    {
        var buffer = new StringBuilder();

        switch (length)
        {
            case 8:
                buffer.Append(OCT[(int)((value >> 21) & 0x07)]);
                goto case 7;
            case 7:
                buffer.Append(OCT[(int)((value >> 18) & 0x07)]);
                goto case 6;
            case 6:
                buffer.Append(OCT[(int)((value >> 15) & 0x07)]);
                goto case 5;
            case 5:
                buffer.Append(OCT[(int)((value >> 12) & 0x07)]);
                goto case 4;
            case 4:
                buffer.Append(OCT[(int)((value >> 9) & 0x07)]);
                goto case 3;
            case 3:
                buffer.Append(OCT[(int)((value >> 6) & 0x07)]);
                goto case 2;
            case 2:
                buffer.Append(OCT[(int)((value >> 3) & 0x07)]);
                goto case 1;
            case 1:
                buffer.Append(OCT[(int)((value >> 0) & 0x07)]);
                break;
        }

        return buffer.ToString();
    }

    /**
     * Constant string used in octal conversion.
     */
    private static readonly string OCT = "01234567";
}
