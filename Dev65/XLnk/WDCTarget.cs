using Dev65.XObj;

namespace Dev65.XLnk;

public class WDCTarget : CachedTarget
{
    public WDCTarget(long start, long end) : base(start, end, 8)
    {
    }

    public WDCTarget(long start, long end, int byteSize) : base(start, end, byteSize)
    {
    }

    public override void WriteTo(Stream file)
    {
        try
        {
            using var stream = new StreamWriter(file);

            var bytes = new byte[Code.Length * (ByteSize / 8) + 7];
            var offset = 0;

            bytes[offset++] = (byte)'Z';
            bytes[offset++] = (byte)(Start >> 0);
            bytes[offset++] = (byte)(Start >> 8);
            bytes[offset++] = (byte)(Start >> 16);
            bytes[offset++] = (byte)(Code.Length >> 0);
            bytes[offset++] = (byte)(Code.Length >> 8);
            bytes[offset++] = (byte)(Code.Length >> 16);

            switch (ByteSize)
            {
                case 8:
                    for (var index = 0; index < Code.Length; ++index)
                    {
                        bytes[offset++] = (byte)Code[index];
                    }
                    break;

                default:
                    Console.WriteLine("Error: Unsupported byte size");
                    break;
            }

            stream.Write(bytes);
            stream.Close();
        }
        catch (Exception)
        {
            Console.WriteLine("Error: A serious error occurred while writing the object file.");
        }
    }
}