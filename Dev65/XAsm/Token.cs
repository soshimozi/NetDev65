namespace Dev65.XAsm;

/// <summary>
/// A <see cref="Token"/> instance represents a significant series of characters extracted from the source code.
/// </summary>
/// <author>Andrew Jacobs</author>
/// <version>$Id$</version>
public class Token
{
    /// <summary>
    /// Constructs a <see cref="Token"/> instance that has an associated value.
    /// </summary>
    /// <param name="kind">Identifies the type of <see cref="Token"/>.</param>
    /// <param name="text">The text string this was parsed from.</param>
    /// <param name="value">An associated value <see cref="object"/>.</param>
    public Token(TokenKind kind, string text, object? value = null)
    {
        Kind = kind;
        Text = text;
        Value = value;
    }

    public TokenKind Kind { get; }
    public string Text { get; }
    public object? Value { get; }

    /// <summary>
    /// Converts the state of the <see cref="Token"/> into a displayable string format.
    /// </summary>
    /// <returns>The <see cref="Token"/> state expressed as a string.</returns>
    public override string ToString()
    {
        return $"{GetType().FullName} {{ {ToDebug()} }}";
    }

    /// <summary>
    /// Converts the state of the instance to a printable string.
    /// </summary>
    /// <returns>The instance state as a debugging string.</returns>
    protected string ToDebug()
    {
        return $"kind={Kind} text={Text} value={(Value == null ? "null" : Value.ToString())}";
    }

}
