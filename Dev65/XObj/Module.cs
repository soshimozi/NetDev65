namespace Dev65.XObj;

using System.Collections.Generic;
using System.Text;

/// <summary>
/// A <see cref="Module"/> instance contains the code generated during the assembly of one source file.
/// </summary>
/// <author>Andrew Jacobs</author>
/// <version>$Id$</version>
public sealed class Module
{
    private string? _name;
    private readonly string? _target;
    private readonly bool _bigEndian;
    private readonly int _byteSize;
    private readonly long _byteMask;
    private readonly List<Section?> _sections = new();
    private readonly Dictionary<string, Expr?> _globals = new();

    /// <summary>
    /// Constructs a <see cref="Module"/> for the given target.
    /// </summary>
    /// <param Name="target">The target architecture.</param>
    /// <param Name="bigEndian">The endianness of the target.</param>
    /// <param Name="byteSize">Number of bits in a byte.</param>
    /// <param name="target"></param>
    /// <param name="bigEndian"></param>
    /// <param name="byteSize"></param>
    public Module(string? target, bool bigEndian, int byteSize = 8)
    {
        _target = target;
        _bigEndian = bigEndian;
        _byteSize = byteSize;

        _byteMask = (1L << byteSize) - 1;
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
        return _bigEndian;
    }

    /// <summary>
    /// Gets the number of bits in a byte.
    /// </summary>
    /// <returns>The number of bits in a byte.</returns>
    public int GetByteSize()
    {
        return _byteSize;
    }

    /// <summary>
    /// Gets the byte mask value.
    /// </summary>
    /// <returns>The byte mask value.</returns>
    public long GetByteMask()
    {
        return _byteMask;
    }

    /// <summary>
    /// Provides access to a list of global symbol names.
    /// </summary>
    /// <returns>The list of global symbol names.</returns>
    public List<string> GetGlobals()
    {
        return new List<string>(_globals.Keys);
    }

    /// <summary>
    /// Provides access to a list of sections.
    /// </summary>
    /// <returns>The list of sections.</returns>
    public List<Section?> GetSections()
    {
        return _sections;
    }

    /// <summary>
    /// Locates a <see cref="Section"/> with the given Name.
    /// </summary>
    /// <param Name="name">The required section Name.</param>
    /// <param name="name"></param>
    /// <returns>The matching section.</returns>
    public Section? FindSection(string name)
    {
        foreach (var section in _sections.Where(section => section?.IsRelative() == true && section.GetName() == name))
        {
            return section;
        }

        var newSection = new Section(this, name);
        _sections.Add(newSection);
        return newSection;
    }

    /// <summary>
    /// Locates a <see cref="Section"/> with the given Name and start address.
    /// </summary>
    /// <param Name="name">The required section Name.</param>
    /// <param Name="start">The start address of the section.</param>
    /// <param name="name"></param>
    /// <param name="start"></param>
    /// <returns>The matching section.</returns>
    public Section? FindSection(string name, long start)
    {
        foreach (var section in _sections.Where(section => section?.IsAbsolute() == true && section.GetStart() == start && section.GetName() == name))
        {
            return section;
        }

        var newSection = new Section(this, name, start);
        _sections.Add(newSection);
        return newSection;
    }

    /// <summary>
    /// Adds a global symbol to the module's export list.
    /// </summary>
    /// <param Name="name">The Name of the symbol.</param>
    /// <param Name="expr">The value of the symbol.</param>
    /// <param name="name"></param>
    /// <param name="expr"></param>
    public void AddGlobal(string name, Expr? expr)
    {
        _globals.Add(name, expr);
    }

    /// <summary>
    /// Fetches the expression defining a global symbol.
    /// </summary>
    /// <param Name="name">The Name of the symbol.</param>
    /// <param name="name"></param>
    /// <returns>The related expression.</returns>
    public Expr? GetGlobal(string name)
    {
        _globals.TryGetValue(name, out var expr);
        return expr;
    }

    /// <summary>
    /// Clears all the data from the sections in this module.
    /// </summary>
    public void Clear()
    {
        foreach (var section in _sections)
        {
            section?.Clear();
        }

        _globals.Clear();
    }

    /// <summary>
    /// Converts the module into an XML string.
    /// </summary>
    /// <returns>The XML representation of this module.</returns>
    public override string ToString()
    {
        var buffer = new StringBuilder();

        buffer.Append("<module");
        buffer.Append(" target='" + _target + "'");
        buffer.Append(" endian='" + (_bigEndian ? "big" : "little") + "'");
        buffer.Append(" byteSize='" + _byteSize + "'");
        buffer.Append(" Name='" + _name + "'>");

        foreach (var section in _sections)
        {
            buffer.Append(section);
        }

        foreach (var global in _globals)
        {
            buffer.Append("<gbl>" + global.Key + global.Value + "</gbl>");
        }

        buffer.Append("</module>");

        return buffer.ToString();
    }
}
