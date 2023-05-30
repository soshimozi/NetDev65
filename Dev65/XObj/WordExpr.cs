namespace Dev65.XObj;

/// <summary>
/// The <see cref="WordExpr"/> class holds an expression which will be converted into
/// a word value during linking.
/// </summary>
public sealed class WordExpr : Part, IEvaluatable
{
    private readonly Expr expr;

    /// <summary>
    /// Constructs a <see cref="WordExpr"/> instance for the given expression.
    /// </summary>
    /// <param name="expr">The expression to be converted.</param>
    public WordExpr(Expr expr)
    {
        this.expr = expr;
    }

    /// <inheritdoc />
    public Expr GetExpr()
    {
        return expr;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return "<word>" + expr + "</word>";
    }
}
