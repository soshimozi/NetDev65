using System.Collections.Generic;
using System.Text;

namespace Dev65.XObj;

/// <summary>
/// The <see cref="Section"/> class represents a target memory area (e.g. PAGE0, BSS, CODE or DATA) in the object module.
/// <see cref="Section"/> instances can be relative or absolute and are internally comprised of <see cref="Part"/> instances
/// that describe byte values or expressions.
/// </summary>
/// <author>Andrew Jacobs</author>
/// <version>$Id$</version>
public class Section
{
    private readonly Module module;
    private readonly string name;
    private bool relative;
    private long start;
    private int size;
    private readonly List<Part> parts = new();

    /// <summary>
    /// Constructs a <see cref="Section"/> instance with the given name.
    /// </summary>
    /// <param name="module">The owning <see cref="Module"/>.</param>
    /// <param name="name">The section name.</param>
    public Section(Module module, string name)
    {
        this.module = module;
        this.name = name;
        relative = true;
        Clear();
    }

    /// <summary>
    /// Constructs a <see cref="Section"/> instance with the given name and start address.
    /// </summary>
    /// <param name="module">The owning <see cref="Module"/>.</param>
    /// <param name="name">The section name.</param>
    /// <param name="start">The start address.</param>
    public Section(Module module, string name, long start)
    {
        this.module = module;
        this.name = name;
        this.start = start;
        relative = false;
        Clear();
    }

    /// <summary>
    /// Gets the <see cref="Module"/> that contains this section.
    /// </summary>
    /// <returns>The <see cref="Module"/> that contains this section.</returns>
    public Module GetModule()
    {
        return module;
    }

    /// <summary>
    /// Provides access to the section name.
    /// </summary>
    /// <returns>The section name.</returns>
    public string GetName()
    {
        return name;
    }

    /// <summary>
    /// Determines if the section is relative.
    /// </summary>
    /// <returns><c>true</c> if the section is relative; otherwise, <c>false</c>.</returns>
    public bool IsRelative()
    {
        return relative;
    }

    /// <summary>
    /// Determines if the section is absolute.
    /// </summary>
    /// <returns><c>true</c> if the section is absolute; otherwise, <c>false</c>.</returns>
    public bool IsAbsolute()
    {
        return !relative;
    }

    /// <summary>
    /// Provides access to the start address of the section.
    /// </summary>
    /// <returns>The start address of an absolute section.</returns>
    public long GetStart()
    {
        return start;
    }

    /// <summary>
    /// Provides access to the section size.
    /// </summary>
    /// <returns>The size of the section.</returns>
    public int GetSize()
    {
        return size;
    }

    /// <summary>
    /// Calculates the origin (address) of the next byte to be added to the section.
    /// The origin will either be an absolute address or a relative offset.
    /// </summary>
    /// <returns>The origin for the next byte.</returns>
    public Value GetOrigin()
    {
        return new Value(relative ? this : null, start + size);
    }

    /// <summary>
    /// Sets the origin of the current section to a specific address.
    /// This operation creates a new section based on the current section but starting at a specific address.
    /// </summary>
    /// <param name="origin">The starting address for the section.</param>
    /// <returns>The section representing the target address.</returns>
    public Section? SetOrigin(long origin)
    {
        return module.FindSection(name, origin);
    }

    /// <summary>
    /// Resets a section and clears out all its contents.
    /// </summary>
    public void Clear()
    {
        size = 0;
        parts.Clear();
    }

    /// <summary>
    /// Adds a byte to the current section.
    /// </summary>
    /// <param name="expr">An expression yielding a byte value.</param>
    public void AddByte(Expr expr)
    {
        if (expr.IsAbsolute)
        {
            AddByte(expr.Resolve(null, null));
        }
        else
        {
            parts.Add(new ByteExpr(expr));
            size++;
        }
    }

    /// <summary>
    /// Adds a word to the current section.
    /// </summary>
    /// <param name="expr">An expression yielding a word value.</param>
    public void AddWord(Expr expr)
    {
        if (expr.IsAbsolute)
        {
            AddWord(expr.Resolve(null, null));
        }
        else
        {
            parts.Add(new WordExpr(expr));
            size += 2;
        }
    }

    /// <summary>
    /// Adds a long to the current section.
    /// </summary>
    /// <param name="expr">An expression yielding a long value.</param>
    public void AddLong(Expr expr)
    {
        if (expr.IsAbsolute)
        {
            AddLong(expr.Resolve(null, null));
        }
        else
        {
            parts.Add(new LongExpr(expr));
            size += 4;
        }
    }

    /// <summary>
    /// Adds a constant byte value to the current section.
    /// </summary>
    /// <param name="value">The byte value to add.</param>
    public void AddByte(long value)
    {
        if (parts.Count == 0 || !(parts[^1] is Code))
        {
            parts.Add(new Code(module));
        }

        ((Code)parts[^1]).AddByte(value);
        size++;
    }

    /// <summary>
    /// Adds a constant word value to the current section.
    /// </summary>
    /// <param name="value">The word value to add.</param>
    public void AddWord(long value)
    {
        var byteSize = module.GetByteSize();
        var byteMask = module.GetByteMask();

        if (parts.Count == 0 || !(parts[^1] is Code))
        {
            parts.Add(new Code(module));
        }

        if (module.IsBigEndian())
        {
            ((Code)parts[^1]).AddByte((value >> (1 * byteSize)) & byteMask);
            ((Code)parts[^1]).AddByte((value >> (0 * byteSize)) & byteMask);
        }
        else
        {
            ((Code)parts[^1]).AddByte((value >> (0 * byteSize)) & byteMask);
            ((Code)parts[^1]).AddByte((value >> (1 * byteSize)) & byteMask);
        }

        size += 2;
    }

    /// <summary>
    /// Adds a constant long value to the current section.
    /// </summary>
    /// <param name="value">The long value to add.</param>
    public void AddLong(long value)
    {
        var byteSize = module.GetByteSize();
        var byteMask = module.GetByteMask();

        if (parts.Count == 0 || !(parts[^1] is Code))
        {
            parts.Add(new Code(module));
        }

        if (module.IsBigEndian())
        {
            ((Code)parts[^1]).AddByte((value >> (3 * byteSize)) & byteMask);
            ((Code)parts[^1]).AddByte((value >> (2 * byteSize)) & byteMask);
            ((Code)parts[^1]).AddByte((value >> (1 * byteSize)) & byteMask);
            ((Code)parts[^1]).AddByte((value >> (0 * byteSize)) & byteMask);
        }
        else
        {
            ((Code)parts[^1]).AddByte((value >> (0 * byteSize)) & byteMask);
            ((Code)parts[^1]).AddByte((value >> (1 * byteSize)) & byteMask);
            ((Code)parts[^1]).AddByte((value >> (2 * byteSize)) & byteMask);
            ((Code)parts[^1]).AddByte((value >> (3 * byteSize)) & byteMask);
        }

        size += 4;
    }

    /// <summary>
    /// Converts the section into an XML string. Sections containing no data return an empty string.
    /// </summary>
    /// <returns>The XML representation of this section.</returns>
    public override string ToString()
    {
        if (parts.Count > 0)
        {
            var buffer = new StringBuilder();
            buffer.Append("<section name='").Append(name).Append("'");
            if (!relative)
            {
                buffer.Append(" addr='").Append(Hex.ToHex(start, 8)).Append("'");
            }
            buffer.Append(" size='").Append(size).Append("'>");
            foreach (var part in parts)
            {
                buffer.Append(part);
            }
            buffer.Append("</section>");
            return buffer.ToString();
        }
        return "";
    }

    /// <summary>
    /// Provides access to the parts of the section.
    /// </summary>
    /// <returns>A list of section parts.</returns>
    public List<Part> GetParts()
    {
        return parts;
    }

    /// <summary>
    /// Sets the start address of the section and marks it as absolute.
    /// </summary>
    /// <param name="start">The start address of the section.</param>
    protected internal void SetStart(int start)
    {
        this.start = start;
        relative = false;
    }

    /// <summary>
    /// Sets the size of the section.
    /// </summary>
    /// <param name="size">The section size.</param>
    protected internal void SetSize(int size)
    {
        this.size = size;
    }
}
