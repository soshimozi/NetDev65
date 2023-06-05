namespace Dev65.XObj;

/// <summary>
/// The <see cref="IEvaluate"/> interface is implemented by <see cref="Part"/>
/// types that are resolved during the linking process.
/// </summary>
public interface IEvaluate
{
    /// <summary>
    /// Provides access to the underlying expression.
    /// </summary>
    /// <returns>The underlying expression.</returns>
    Expr? GetExpr();
}
