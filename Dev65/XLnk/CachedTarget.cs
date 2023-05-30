using Dev65.XObj;
using System.Drawing;
using System.Runtime.InteropServices;
using static Dev65.XObj.BinaryExpr;

namespace Dev65.XLnk;

public abstract class CachedTarget : Target
{
    protected CachedTarget(long start, long end, int byteSize) : base(byteSize)
    {

        this.Start = start;
        this.End = end;
        Size = (int)(end - start + 1);

        Code = new int[Size];
    }

    public override void Store(long address, long value)
    {
        if ((Start <= address) && (address <= End))
        {
            if (address < Min) Min = address;
            if (address> Max) Max = address;
            Code[(int)(address - Start)] = (int)value;
        }
    }

    /**
	 * The start address of the memory area.
	 */
    protected long Start;

    /**
	 * The end address of the memory area.
	 */
    protected long End;

    /**
	 * The size of the memory area.
	 */
    protected int Size;

    /**
	 * The data comprising the linked code.
	 */
    protected int[] Code;

    /**
	 * The lowest address written.
	 */
    protected long Min = long.MaxValue;

    /**
	 * The highest address written.
	 */
    protected long Max = long.MinValue;

}