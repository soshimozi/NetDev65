using Dev65.XObj;
using System.Drawing;

namespace Dev65.XLnk;

public class HexTarget : CachedTarget
{
    public HexTarget(long start, long end) : base(start, end, 8) {}
    public HexTarget(long start, long end, int byteSize) : base(start, end, byteSize)
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
                for (var offset = 0; offset < 16; ++offset)
                {
                    if ((index + offset) < Size)
                    {
                        writer.Write(Hex.ToHex(Code[index + offset], span));
                    }
                    else
                        break;
                }
                writer.WriteLine();
            }

            writer.Close();
        }
        catch (Exception)
        {
            Console.WriteLine("Error: A serious error occurred writing the object module.");
        }
    }
}