namespace Dev65.XObj;

/**
 * An <CODE>Extern</CODE> instance represents a names external symbol that
 * could reside in any section.
 * 
 * author Andrew Jacobs
 * version	$Id$
 */
public sealed class Extern : Expr
{
    public string Name { get; }

    /**
     * Constructs an <CODE>Extern</CODE> instance.
     * 
     * param name	The name of the symbol.
     */
    public Extern(string name)
    {
        Name = name;
    }

    /**
     * Provides access to the symbol name.
     * 
     * return	The symbol name.
     */
    public string GetName()
    {
        return Name;
    }

    /**
     * Determines whether the expression is absolute.
     */
    public override bool IsAbsolute => false;

    /**
     * Determines whether the expression is external to the given section.
     */
    public override bool IsExternal(Section? section)
    {
        return true;
    }

    /**
     * Resolves the value of the external symbol.
     */
    public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
    {
        if (sections == null && symbols == null)
            return 0;

        return symbols?.AddressOf(Name) ?? 0;
    }

    /**
     * Returns the XML representation of the external symbol.
     */
    public override string ToString()
    {
        return ($"<ext>{Name}</ext>");
    }
}
