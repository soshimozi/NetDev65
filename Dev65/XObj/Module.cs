using System.Runtime.InteropServices.ComTypes;

namespace Dev65.XObj;

using System.Collections.Generic;
using System.Text;
using static System.Collections.Specialized.BitVector32;

/// <summary>
/// A <see cref="Module"/> instance contains the code generated during the assembly of one source file.
/// </summary>
/// <author>Andrew Jacobs</author>
/// <version>$Id$</version>
public sealed class Module
{
    private string? _name;
    private readonly string target;
    private readonly bool bigEndian;
    private readonly int byteSize;
    private readonly long byteMask;
    private readonly List<Section> sections = new List<Section>();
    private readonly Dictionary<string, Expr> globals = new Dictionary<string, Expr>();

    /// <summary>
    /// Constructs a <see cref="Module"/> for the given target.
    /// </summary>
    /// <param Name="target">The target architecture.</param>
    /// <param Name="bigEndian">The endianness of the target.</param>
    public Module(string target, bool bigEndian) : this(target, bigEndian, 8)
    {
    }

    /// <summary>
    /// Constructs a <see cref="Module"/> for the given target.
    /// </summary>
    /// <param Name="target">The target architecture.</param>
    /// <param Name="bigEndian">The endianness of the target.</param>
    /// <param Name="byteSize">Number of bits in a byte.</param>
    public Module(string target, bool bigEndian, int byteSize)
    {
        this.target = target;
        this.bigEndian = bigEndian;
        this.byteSize = byteSize;

        byteMask = (1L << byteSize) - 1;
    }


    public void SetName(string? name)
    {
        _name = name;
    }
    /// <summary>
    /// Name property
    /// </summary>
    public string? Name
    {
        get => _name;
        set => _name = value;
    }

    /// <summary>
    /// Determines the endianness of the module.
    /// </summary>
    /// <returns><c>true</c> if big endian.</returns>
    public bool IsBigEndian()
    {
        return bigEndian;
    }

    /// <summary>
    /// Gets the number of bits in a byte.
    /// </summary>
    /// <returns>The number of bits in a byte.</returns>
    public int GetByteSize()
    {
        return byteSize;
    }

    /// <summary>
    /// Gets the byte mask value.
    /// </summary>
    /// <returns>The byte mask value.</returns>
    public long GetByteMask()
    {
        return byteMask;
    }

    /// <summary>
    /// Provides access to a list of global symbol names.
    /// </summary>
    /// <returns>The list of global symbol names.</returns>
    public List<string> GetGlobals()
    {
        return new List<string>(globals.Keys);
    }

    /// <summary>
    /// Provides access to a list of sections.
    /// </summary>
    /// <returns>The list of sections.</returns>
    public List<Section> GetSections()
    {
        return sections;
    }

    /// <summary>
    /// Locates a <see cref="Section"/> with the given Name.
    /// </summary>
    /// <param Name="name">The required section Name.</param>
    /// <returns>The matching section.</returns>
    public Section FindSection(string name)
    {
        foreach (var section in sections)
        {
            if (section.IsRelative() && section.GetName() == name)
                return section;
        }

        var newSection = new Section(this, name);
        sections.Add(newSection);
        return newSection;
    }

    /// <summary>
    /// Locates a <see cref="Section"/> with the given Name and start address.
    /// </summary>
    /// <param Name="name">The required section Name.</param>
    /// <param Name="start">The start address of the section.</param>
    /// <returns>The matching section.</returns>
    public Section? FindSection(string name, long start)
    {
        foreach (var section in sections)
        {
            if (section.IsAbsolute() && section.GetStart() == start && section.GetName() == name)
                return section;
        }

        var newSection = new Section(this, name, start);
        sections.Add(newSection);
        return newSection;
    }

    /// <summary>
    /// Adds a global symbol to the module's export list.
    /// </summary>
    /// <param Name="name">The Name of the symbol.</param>
    /// <param Name="expr">The value of the symbol.</param>
    public void AddGlobal(string name, Expr expr)
    {
        globals.Add(name, expr);
    }

    /// <summary>
    /// Fetches the expression defining a global symbol.
    /// </summary>
    /// <param Name="name">The Name of the symbol.</param>
    /// <returns>The related expression.</returns>
    public Expr? GetGlobal(string name)
    {
        globals.TryGetValue(name, out var expr);
        return expr;
    }

    /// <summary>
    /// Clears all the data from the sections in this module.
    /// </summary>
    public void Clear()
    {
        foreach (var section in sections)
        {
            section.Clear();
        }

        globals.Clear();
    }

    /// <summary>
    /// Converts the module into an XML string.
    /// </summary>
    /// <returns>The XML representation of this module.</returns>
    public override string ToString()
    {
        var buffer = new StringBuilder();

        buffer.Append("<module");
        buffer.Append(" target='" + target + "'");
        buffer.Append(" endian='" + (bigEndian ? "big" : "little") + "'");
        buffer.Append(" byteSize='" + byteSize + "'");
        buffer.Append(" Name='" + _name + "'>");

        foreach (var section in sections)
        {
            buffer.Append(section);
        }

        foreach (var global in globals)
        {
            buffer.Append("<gbl>" + global.Key + global.Value + "</gbl>");
        }

        buffer.Append("</module>");

        return buffer.ToString();
    }
}
