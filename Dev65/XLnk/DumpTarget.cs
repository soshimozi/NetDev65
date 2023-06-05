using Dev65.XObj;
using System.Drawing;

namespace Dev65.XLnk;

public class DumpTarget : CachedTarget
{
    public DumpTarget(long start, long end, int byteSize = 8) : base(start, end, byteSize)
    {
    }

    public override void WriteTo(Stream file)
    {
        try
        {
            using var writer = new StreamWriter(file);
            var span = ByteSize / 4;

            for (var index = 0; index < Size; index += 16)
            {
                writer.Write(':');
                writer.Write(Hex.ToHex(index, 4));

                for (var offset = 0; offset < 16; ++offset)
                {
                    if ((index + offset) < Size)
                    {
                        writer.Write(' ');
                        writer.Write(Hex.ToHex(Code[index + offset], span));
                    }
                    else
                    {
                        break;
                    }
                }
                writer.WriteLine();
            }
        }
        catch (Exception)
        {
            Console.Error.WriteLine("Error: A serious error occurred while writing the object module.");
        }
    }
}