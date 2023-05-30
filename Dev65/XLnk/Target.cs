namespace Dev65.XLnk;


/**
 * Interface implemented by all output formats.
 * 
 * @author	Andrew Jacobs
 * @version	$Id$
 */
public abstract class Target
{
	/**
	 * Stores the given byte value at the indicated address.
	 * 
	 * @param	addr		Where to store.
	 * @param 	value		What to store.
	 */
	public abstract void Store (long address, long value);
	
	/**
	 * Write the store data content to the indicated file.
	 * 
	 * @param 	file		File to write output to.
	 */
	public abstract void WriteTo (Stream file);
	
	/**
	 * Constructs a <CODE>Target</CODE> with a given byte size.
	 * 
	 * @param	byteSize	The size of a byte in bits.
	 */
	protected Target (int byteSize)
	{
		this.byteSize = byteSize;
	}

    /**
	 * Returns the target's byte size (in bits).
	 * 
	 * @return	The byte size (in bits).
	 */
    protected int ByteSize => byteSize;
	
	/**
	 * The number of bits in a byte, normally 8.
	 */
	private readonly int byteSize;
}