namespace Dev65.XObj;

/// <summary>
/// The <see cref="LongExpr"/> class holds an expression which will be converted into
/// a long value during linking.
/// </summary>
public sealed class LongExpr : Part, IEvaluatable
{
    private readonly Expr expr;

    /// <summary>
    /// Constructs a <see cref="LongExpr"/> instance for the given expression.
    /// </summary>
    /// <param name="expr">The expression to be converted.</param>
    public LongExpr(Expr expr)
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
        return "<long>" + expr + "</long>";
    }
}
