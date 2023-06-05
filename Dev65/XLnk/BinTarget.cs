using Dev65.XObj;

namespace Dev65.XLnk;

public class BinTarget :CachedTarget
{
    public BinTarget(long start, long end, int byteSize = 8) : base(start, end, byteSize)
    {
    }

    public override void WriteTo(Stream file)
    {
        try
        {
            var bytes = new byte[Code.Length * (ByteSize / 8)];
            var offset = 0;

            switch (ByteSize)
            {
                case 8:
                    foreach (var byt in Code)
                    {
                        bytes[offset++] = (byte)byt;
                    }
                    break;

                case 16:
                    foreach (var byt in Code)
                    {
                        bytes[offset++] = (byte)((byt >> 8) & 0xff);
                        bytes[offset++] = (byte)((byt >> 0) & 0xff);
                    }
                    break;

                // TODO: Handle other byte sizes

                default:
                    throw new InvalidOperationException("Unsupported byte size.");
            }

            file.Write(bytes, 0, bytes.Length);
            file.Close();
        }
        catch (Exception)
        {
            Console.Error.WriteLine($"Error: A serious error occurred while writing the object file: {file}");
        }
    }
}