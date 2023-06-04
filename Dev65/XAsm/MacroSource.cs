using System;
namespace Dev65.XAsm;

using System.Collections.Generic;

/// <summary>
/// The <see cref="MacroSource"/> class provides macro expansion capability to <see cref="TextSource"/>.
/// </summary>
/// <author>Andrew Jacobs</author>
/// <version>$Id$</version>
public sealed class MacroSource : TextSource
{
    /// <summary>
    /// The macro argument names.
    /// </summary>
    private readonly List<string> arguments;

    /// <summary>
    /// The macro instance number.
    /// </summary>
    private readonly int instance;

    /// <summary>
    /// The macro argument values.
    /// </summary>
    private readonly List<string> values = new();

    /// <summary>
    /// Constructs a <see cref="MacroSource"/> template instance.
    /// </summary>
    /// <param name="arguments">The list of argument names.</param>
    public MacroSource(List<string> arguments)
    {
        this.arguments = arguments;
    }

    /// <summary>
    /// Creates a copy of a template with values to expand.
    /// </summary>
    /// <param name="instance">Macro instance counter.</param>
    /// <param name="values">Argument values.</param>
    /// <returns>A copy of the configured <see cref="MacroSource"/>.</returns>
    public MacroSource Invoke(int instance, List<string> values)
    {
        return new MacroSource(this, instance, values);
    }

    /// <summary>
    /// Fetches the next line of source text from the underlying storage, expands the macros, and bundles it with its origin details.
    /// </summary>
    /// <returns>The next line of source text or <c>null</c> if the end of this <see cref="ISource"/> has been reached.</returns>
    public override Line? NextLine()
    {
        var line = base.NextLine();

        if (line != null)
        {
            var text = line.Text;

            // Replace instance number
            text = text.Replace("\\?", instance.ToString());

            // Replace named arguments
            for (var index = 0; index < arguments.Count; ++index)
            {
                var arg = arguments[index];
                var val = "";

                if (index < values.Count)
                    val = values[index];

                text = text.Replace(arg, val);
            }

            // Replace numbered arguments 0-9
            if (text.IndexOf('\\') != -1)
            {
                for (var index = 0; index <= 9; ++index)
                {
                    var arg = "\\" + index;
                    var val = "";

                    if (index < values.Count)
                        val = values[index];

                    text = text.Replace(arg, val);
                }
            }

            line = new Line(line.FileName, line.LineNumber, text);
        }

        return line;
    }

    /// <summary>
    /// Constructs a <see cref="MacroSource"/> instance ready to be expanded.
    /// </summary>
    /// <param name="template">The template <see cref="MacroSource"/>.</param>
    /// <param name="instance">The macro instance counter.</param>
    /// <param name="values">The argument values.</param>
    private MacroSource(MacroSource template, int instance, List<string> values) : base(template)
    {
        arguments = template.arguments;
        this.instance = instance;
        this.values = values;
    }
}
