using System;
namespace Dev65.XAsm;
using System.Collections.Generic;

/// <summary>
/// The <see cref="TextSource"/> class provides the storage for text held for either macros or repeat sections.
/// </summary>
/// <author>Andrew Jacobs</author>
/// <version>$Id$</version>
public abstract class TextSource : Source
{
    /// <summary>
    /// A <see cref="List{T}"/> of stored source lines.
    /// </summary>
    private readonly List<Line> lines = new List<Line>();

    /// <summary>
    /// The index of the next line to be read.
    /// </summary>
    private int nextLine;

    /// <summary>
    /// Add a <see cref="Line"/> to the collection managed by this instance.
    /// </summary>
    /// <param name="line">The <see cref="Line"/> to be added.</param>
    public void AddLine(Line line)
    {
        lines.Add(line);
    }

    /// <summary>
    /// Fetches the next line of source text from the underlying storage and bundles it with its origin details.
    /// </summary>
    /// <returns>The next line of source text or <c>null</c> if the end of this <see cref="Source"/> has been reached.</returns>
    public virtual Line? NextLine()
    {
        if (nextLine < lines.Count)
            return lines[nextLine++];

        return null;
    }

    /// <summary>
    /// Constructs a <see cref="TextSource"/> instance.
    /// </summary>
    protected TextSource()
    {
        Reset();
    }

    /// <summary>
    /// Constructs a <see cref="TextSource"/> instance by copying a template.
    /// </summary>
    /// <param name="template">The template instance.</param>
    protected TextSource(TextSource template)
    {
        lines = template.lines;
        Reset();
    }

    /// <summary>
    /// Repositions the next line marker to the start of the collection.
    /// </summary>
    protected void Reset()
    {
        nextLine = 0;
    }
}
