namespace Dev65.XObj;

/// <summary>
/// The <see cref="ByteExpr"/> class holds an expression which will be converted into
/// a byte value during linking.
/// </summary>
public sealed class ByteExpr : Part, IEvaluatable
{
    /// <summary>
    /// Constructs a <see cref="ByteExpr"/> instance for the given expression.
    /// </summary>
    /// <param name="expression">The expression to be converted.</param>
    public ByteExpr(Expr expression)
    {
        Expr = expression;
    }

    /// <inheritdoc/>
    public Expr GetExpr()
    {
        return Expr;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"<byte>{Expr}</byte>";
    }

    /// <summary>
    /// The underlying expression.
    /// </summary>
    private readonly Expr Expr;
}
