using System;
namespace Dev65.XAsm;

using System.IO;

/// <summary>
/// The <see cref="FileSource"/> class implements a <see cref="Source"/> that reads from a file.
/// </summary>
/// <author>Andrew Jacobs</author>
/// <version>$Id$</version>
public sealed class FileSource : Source
{
    private readonly StreamReader reader;
    private readonly string fileName;
    private int lineNumber;

    /// <summary>
    /// Constructs a <see cref="FileSource"/> instance.
    /// </summary>
    /// <param name="fileName">The name of the source file.</param>
    /// <param name="stream">The <see cref="FileStream"/> attached to it.</param>
    public FileSource(string fileName, FileStream stream)
    {
        reader = new StreamReader(stream, System.Text.Encoding.GetEncoding("ISO-8859-1"));
        this.fileName = fileName;
        lineNumber = 0;
    }

    /// <summary>
    /// Fetches the next line of source text from the file and bundles it with its origin details.
    /// </summary>
    /// <returns>The next line of source text or <c>null</c> if the end of the file has been reached.</returns>
    public Line? NextLine()
    {
        try
        {
            var text = reader.ReadLine();

            if (text != null)
                return new Line(fileName, ++lineNumber, text);
        }
        catch (IOException)
        {
            // Handle the exception if necessary
        }

        return null;
    }
}
