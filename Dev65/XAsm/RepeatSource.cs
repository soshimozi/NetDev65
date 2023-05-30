namespace Dev65.XAsm;

/// <summary>
/// The <see cref="RepeatSource"/> holds a collection of lines that will be repeated a specified number of times.
/// </summary>
/// <author>Andrew Jacobs</author>
public sealed class RepeatSource : TextSource
{
    /// <summary>
    /// The number of times to repeat.
    /// </summary>
    private readonly int limit;

    /// <summary>
    /// The repetition count.
    /// </summary>
    private int count;

    /// <summary>
    /// Constructs a <see cref="RepeatSource"/> instance.
    /// </summary>
    /// <param name="limit">The number of times to repeat.</param>
    public RepeatSource(int limit)
    {
        count = 0;
        this.limit = limit;
    }

    /// <summary>
    /// Fetches the next line of source text from the collection and bundles it with its origin details.
    /// </summary>
    /// <returns>The next line of source text or <c>null</c> if the end of the repetition has been reached.</returns>
    public override Line? NextLine()
    {
        while (count < limit)
        {
            var line = base.NextLine();

            if (line != null)
            {
                var text = line.Text.Replace("\\!", (count + 1).ToString());
                return new Line(line.FileName, line.LineNumber, text);
            }

            count++;
            Reset();
        }

        return null;
    }
}
