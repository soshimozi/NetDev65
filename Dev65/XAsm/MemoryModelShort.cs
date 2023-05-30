using Dev65.XObj;

namespace Dev65.XAsm;

/**
 * Implements a memory model where each memory location can contain a short.
 * 
 * author	Andrew Jacobs
 * version	$Id$
 */
public class MemoryModelShort : MemoryModel
{
  //  /**
	 //* Constructs a <CODE>MemoryModelShort</CODE> instance.
	 //* 
	 //* @param 	errorHandler		The error handler used to report problems.
	 //*/
  //  public MemoryModelShort(IErrorHandler errorHandler) : base(errorHandler)
  //  {
  //  }

    /**
	 * {@inheritDoc}
	 */
    public override int GetByte(int index)
    {
        return bytes[index];
    }

    /**
	 * {@inheritDoc}
	 */
    public override void AddByte(Module? module, Section? section, Expr? expr)
    {
        if (expr?.IsRelative == true)
        {
            if (section != null)
            {
                section.AddByte(expr);
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = 0;
            }
            else
            {
                Error(ErrorMessage.ERR_NO_SECTION);
            }
        }
        else
        {
            AddByte(module, section, expr?.Resolve() ?? 0);
        }
    }

    /**
	 * {@inheritDoc} 
	 */
    public override void AddWord(Module? module, Section? section, Expr? expr)
    {
        if (expr?.IsRelative == true)
        {
            if (section != null)
            {
                section.AddWord(expr);
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = 0;
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = 0;
            }
            else
            {
                Error(ErrorMessage.ERR_NO_SECTION);
            }
        }
        else
        {
            AddWord(module, section, expr?.Resolve() ?? 0);
        }
    }

    /**
	 * {@inheritDoc} 
	 */
    public override void AddLong(Module? module, Section? section, Expr? expr)
    {
        if (expr?.IsRelative == true)
        {
            if (section != null)
            {
                section.AddLong(expr);
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = 0;
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = 0;
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = 0;
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = 0;
            }
            else
            {
                Error(ErrorMessage.ERR_NO_SECTION);
            }
        }
        else
        {
            AddLong(module, section, expr?.Resolve() ?? 0);
        }
    }

    /**
	 * {@inheritDoc} 
	 */
    public override void AddByte(Module? module, Section? section, long value)
    {
        if (section != null)
        {
            section.AddByte(value);
            if (ByteCount < bytes.Length)
                bytes[ByteCount++] = (short)(value & 0xffff);
        }
        else
        {
            Error(ErrorMessage.ERR_NO_SECTION);
        }
    }

    /**
	 * {@inheritDoc} 
	 */
    public override void AddWord(Module? module, Section? section, long value)
    {
        if (section != null)
        {
            section.AddWord(value);
            if (module?.IsBigEndian() == true)
            {
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = (short)((value >> 16) & 0xffff);
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = (short)((value >> 0) & 0xffff);
            }
            else
            {
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = (short)((value >> 0) & 0xffff);
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = (short)((value >> 16) & 0xffff);
            }
        }
        else
        {
            Error(ErrorMessage.ERR_NO_SECTION);
        }
    }

    /**
	 * {@inheritDoc} 
	 */
    public override void AddLong(Module? module, Section? section, long value)
    {
        if (section != null)
        {
            section.AddLong(value);
            if (module?.IsBigEndian() == true)
            {
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = (short)((value >> 48) & 0xffff);
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = (short)((value >> 32) & 0xffff);
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = (short)((value >> 16) & 0xffff);
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = (short)((value >> 0) & 0xffff);
            }
            else
            {
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = (short)((value >> 0) & 0xffff);
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = (short)((value >> 16) & 0xffff);
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = (short)((value >> 32) & 0xffff);
                if (ByteCount < bytes.Length)
                    bytes[ByteCount++] = (short)((value >> 48) & 0xffff);
            }
        }
        else
        {
            Error(ErrorMessage.ERR_NO_SECTION);
        }
    }

    /**
	 * Captured data used to generate the listing.
	 */
    protected short[] bytes = new short[9];
}
