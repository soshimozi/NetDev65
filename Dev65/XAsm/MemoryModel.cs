using Dev65.XObj;

namespace Dev65.XAsm;

/**
 * The <CODE>MemoryModel</CODE> class defines standard methods for storing
 * data into a <CODE>Section?</CODE> within a <CODE>Module??</CODE> independent
 * of the actual byte size.
 * 
 * author	Andrew Jacobs
 * version	$Id$
 */
public abstract class MemoryModel
{
    public event EventHandler<AssemblerEventArgs>? AssemblerError;
	public event EventHandler<AssemblerEventArgs>? AssemblerWarning;
    /**
	 * Adds a byte value to the output memory area.
	 * 
	 * @param	module	The <CODE>Module?</CODE> containing the output.
	 * @param	section	The <CODE>Section?</CODE> containing the output.
	 * @param	expr	The expression defining the value.
	 */
    public abstract void AddByte(Module? module, Section? section, Expr? expr);

    /**
	 * Adds a word value to the output memory area.
	 * 
	 * @param	module	The <CODE>Module?</CODE> containing the output.
	 * @param	section	The <CODE>Section?</CODE> containing the output.
	 * @param	expr	The expression defining the value.
	 */
    public abstract void AddWord(Module? module, Section? section, Expr? expr);

    /**
	 * Adds a long value to the output memory area.
	 * 
	 * @param	module	The <CODE>Module?</CODE> containing the output.
	 * @param	section	The <CODE>Section?</CODE> containing the output.
	 * @param	expr	The expression defining the value.
	 */
    public abstract void AddLong(Module? module, Section? section, Expr? expr);

    /**
	 * Adds a literal byte value to the output memory area.
	 * 
	 * @param	module	The <CODE>Module?</CODE> containing the output.
	 * @param	section	The <CODE>Section?</CODE> containing the output.
	 * @param	value	The literal value.
	 */
    public abstract void AddByte(Module? module, Section? section, long value);

    /**
	 * Adds a literal word value to the output memory area.
	 * 
	 * @param	module	The <CODE>Module?</CODE> containing the output.
	 * @param	section	The <CODE>Section?</CODE> containing the output.
	 * @param	value	The literal value.
	 */
    public abstract void AddWord(Module? module, Section? section, long value);

    /**
	 * Adds a literal long value to the output memory area.
	 * 
	 * @param	module	The <CODE>Module?</CODE> containing the output.
	 * @param	section	The <CODE>Section?</CODE> containing the output.
	 * @param	value	The literal value.
	 */
    public abstract void AddLong(Module? module, Section? section, long value);

    /**
	 * Clears any stored data.
	 */
    public void Clear()
    {
        ByteCount = 0;
    }

    /**
	 * Returns the current byte count.
	 * 
	 * @return	The byte count.
	 */
    public int ByteCount { get; protected set; }

    /**
	 * Fetches the byte value at the specified index.
	 * 
	 * @param	index	The index into memory.
	 * @return	The value of the indexed byte.
	 */
    public abstract int GetByte(int index);

    /**
	 * Constructs a <CODE>MemoryModel</CODE> that uses the indicated
	 * <CODE>IErrorHandler</CODE> to report problems.
	 * 
	 * @param errorHandler	The <CODE>IErrorHandler</CODE> instance.
	 */
    protected MemoryModel()
    {
        //ErrorHandler = errorHandler;
        Clear();
    }

    /**
	 * Reports an error message.
	 * 
	 * @param message	The error message string.
	 */
    protected void Error(string message)
    {
        OnAssemblerError(new AssemblerEventArgs(message));
    }

    /**
	 * Reports a warning message.
	 * 
	 * @param message	The warning message string.
	 */
    protected void Warning(string message)
    {
        OnAssemblerWarning(new AssemblerEventArgs(message));
    }

    //private readonly IErrorHandler ErrorHandler;
    protected virtual void OnAssemblerError(AssemblerEventArgs e)
    {
        AssemblerError?.Invoke(this, e);
    }

    protected virtual void OnAssemblerWarning(AssemblerEventArgs e)
    {
        AssemblerWarning?.Invoke(this, e);
    }
}
