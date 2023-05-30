namespace Dev65.XAsm;

public sealed class TokenKind
{
    // The name of the TokenKind
    private readonly string kind;

    // Constructs a TokenKind with a given name
    public TokenKind(string kind)
    {
        this.kind = kind;
    }

    // Overrides ToString() method
    public override string ToString()
    {
        return kind;
    }

    // Overrides GetHashCode() method
    public override int GetHashCode()
    {
        return kind.GetHashCode();
    }
}

