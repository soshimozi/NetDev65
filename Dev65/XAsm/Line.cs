namespace Dev65.XAsm;

using System;

/// <summary>
/// The <see cref="Line"/> class holds a line of source text and the details of where it originally came from.
/// </summary>
/// <author>Andrew Jacobs</author>
/// <version>$Id$</version>
public sealed class Line
{
    /// <summary>
    /// Constructs a <see cref="Line"/> instance.
    /// </summary>
    /// <param name="fileName">The name of the source file.</param>
    /// <param name="lineNumber">The corresponding line number.</param>
    /// <param name="text">The actual source text.</param>
    public Line(string fileName, int lineNumber, string text)
    {
        this.fileName = fileName;
        this.lineNumber = lineNumber;
        this.text = text;
    }

    /// <summary>
    /// Provides access to the originating filename.
    /// </summary>
    /// <returns>The filename.</returns>
    public string FileName => fileName;

    /// <summary>
    /// Provides access to the line number.
    /// </summary>
    /// <returns>The line number.</returns>
    public int LineNumber => lineNumber;

    /// <summary>
    /// Provides access to the source text.
    /// </summary>
    /// <returns>The source text.</returns>
    public string Text => text;

    /// <summary>
    /// The name of the file this line is from.
    /// </summary>
    private readonly string fileName;

    /// <summary>
    /// Its line number.
    /// </summary>
    private readonly int lineNumber;

    /// <summary>
    /// The actual line of text itself.
    /// </summary>
    private readonly string text;
}