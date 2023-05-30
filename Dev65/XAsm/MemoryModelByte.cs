using Dev65.XObj;

namespace Dev65.XAsm;

/**
 * The <CODE>MemoryModelByte</CODE> class implements a <CODE>MemoryModel</CODE>
 * for an 8-bit byte.
 * 
 * author	Andrew Jacobs
 * version	$Id$
 */
public class MemoryModelByte : MemoryModel
{
  //  /**
	 //* Constructs a <CODE>MemoryModelByte</CODE> that uses the indicated
	 //* <CODE>IErrorHandler</CODE> to report problems.
	 //* 
	 //* @param errorHandler	The <CODE>IErrorHandler</CODE> instance.
	 //*/
  //  public MemoryModelByte(IErrorHandler errorHandler) : base(errorHandler)
  //  {
  //  }

    /**
	 * {@inheritDoc}
	 */
    public override int GetByte(int index)
    {
        return Bytes[index];
    }

    /**
	 * {@inheritDoc}
	 */
    public override void AddByte(Module? module, Section? section, Expr? expr)
    {
        if (expr != null)
        {
            if (expr.IsRelative)
            {
                if (section != null)
                {
                    section.AddByte(expr);
                    if (ByteCount < Bytes.Length)
                        Bytes[ByteCount++] = -1;
                }
                else
                {
                    Error(ErrorMessage.ERR_NO_SECTION);
                }
            }
            else
            {
                AddByte(module, section, expr.Resolve(null, null));
            }
        }
        else
        {
            Error(ErrorMessage.ERR_INVALID_EXPRESSION);
        }
    }

    /**
	 * {@inheritDoc} 
	 */
    public override void AddWord(Module? module, Section? section, Expr? expr)
    {
        if (expr != null)
        {
            if (expr.IsRelative)
            {
                if (section != null)
                {
                    section.AddWord(expr);
                    if (ByteCount < Bytes.Length)
                        Bytes[ByteCount++] = -1;
                    if (ByteCount < Bytes.Length)
                        Bytes[ByteCount++] = -1;
                }
                else
                {
                    Error(ErrorMessage.ERR_NO_SECTION);
                }
            }
            else
            {
                AddWord(module, section, expr.Resolve(null, null));
            }
        }
        else
        {
            Error(ErrorMessage.ERR_INVALID_EXPRESSION);
        }
    }

    /**
	 * {@inheritDoc} 
	 */
    public override void AddLong(Module? module, Section? section, Expr? expr)
    {
        if (expr != null)
        {
            if (expr.IsRelative)
            {
                if (section != null)
                {
                    section.AddLong(expr);
                    if (ByteCount < Bytes.Length)
                        Bytes[ByteCount++] = -1;
                    if (ByteCount < Bytes.Length)
                        Bytes[ByteCount++] = -1;
                    if (ByteCount < Bytes.Length)
                        Bytes[ByteCount++] = -1;
                    if (ByteCount < Bytes.Length)
                        Bytes[ByteCount++] = -1;
                }
                else
                {
                    Error(ErrorMessage.ERR_NO_SECTION);
                }
            }
            else
            {
                AddLong(module, section, expr.Resolve(null, null));
            }
        }
        else
        {
            Error(ErrorMessage.ERR_INVALID_EXPRESSION);
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
            if (ByteCount < Bytes.Length)
                Bytes[ByteCount++] = (int)(value & 0xff);
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
                if (ByteCount < Bytes.Length)
                    Bytes[ByteCount++] = (int)((value >> 8) & 0xff);
                if (ByteCount < Bytes.Length)
                    Bytes[ByteCount++] = (int)((value >> 0) & 0xff);
            }
            else
            {
                if (ByteCount < Bytes.Length)
                    Bytes[ByteCount++] = (int)((value >> 0) & 0xff);
                if (ByteCount < Bytes.Length)
                    Bytes[ByteCount++] = (int)((value >> 8) & 0xff);
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
                if (ByteCount < Bytes.Length)
                    Bytes[ByteCount++] = (int)((value >> 24) & 0xff);
                if (ByteCount < Bytes.Length)
                    Bytes[ByteCount++] = (int)((value >> 16) & 0xff);
                if (ByteCount < Bytes.Length)
                    Bytes[ByteCount++] = (int)((value >> 8) & 0xff);
                if (ByteCount < Bytes.Length)
                    Bytes[ByteCount++] = (int)((value >> 0) & 0xff);
            }
            else
            {
                if (ByteCount < Bytes.Length)
                    Bytes[ByteCount++] = (int)((value >> 0) & 0xff);
                if (ByteCount < Bytes.Length)
                    Bytes[ByteCount++] = (int)((value >> 8) & 0xff);
                if (ByteCount < Bytes.Length)
                    Bytes[ByteCount++] = (int)((value >> 16) & 0xff);
                if (ByteCount < Bytes.Length)
                    Bytes[ByteCount++] = (int)((value >> 24) & 0xff);
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
    protected readonly int[] Bytes = new int[9];
}
