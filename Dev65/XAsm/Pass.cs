using System;
namespace Dev65.XAsm;

/// <summary>
/// Instances of the <see cref="Pass"/> class are used to indicate the phase the assembler is in.
/// </summary>
/// <author>Andrew Jacobs</author>
/// <version>$Id$</version>
public sealed class Pass
{
    /// <summary>
    /// A static instance used to indicate the FIRST pass.
    /// </summary>
    public static readonly Pass FIRST = new Pass(1);

    /// <summary>
    /// A static instance used to indicate the INTERMEDIATE pass.
    /// </summary>
    public static readonly Pass INTERMEDIATE = new Pass(2);

    /// <summary>
    /// A static instance used to indicate the FINAL pass.
    /// </summary>
    public static readonly Pass FINAL = new Pass(3);

    /// <summary>
    /// The pass number.
    /// </summary>
    private readonly int number;

    /// <summary>
    /// Constructs a <see cref="Pass"/> instance.
    /// </summary>
    /// <param name="number">The pass number.</param>
    private Pass(int number)
    {
        this.number = number;
    }

    /// <summary>
    /// Returns a number representing the pass.
    /// </summary>
    /// <returns>The pass number.</returns>
    public int GetNumber()
    {
        return number;
    }
}
