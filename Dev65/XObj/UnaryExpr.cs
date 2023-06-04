namespace Dev65.XObj;

/**
 * The <CODE>UnaryExpr</CODE> class is the common base of all mathematical and
 * logical operators that take a single argument.
 * 
 * @author Andrew Jacobs
 * @version	$Id$
 */
public abstract class UnaryExpr : Expr
{
    public Expr? Exp { get; }

    /**
     * The <CODE>Not</CODE> class implements the logical NOT operation.
     * 
     * @author Andrew Jacobs
     */
    public sealed class Not : UnaryExpr
    {
        /**
         * Constructs a <CODE>Not</CODE> instance which will invert the
         * associated logical expression.
         * 
         * @param exp		The expression to be inverted.
         */
        public Not(Expr? exp) : base(exp)
        {
        }

        /**
         * {@inheritDoc}
         */
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Exp?.Resolve(sections, symbols) != 0 ? 0 : 1;
        }

        /**
         * {@inheritDoc}
         */
        public override string ToString()
        {
            return ($"<not>{Exp}</not>");
        }
    }

    /**
     * The <CODE>Cpl</CODE> class implements the binary complement operation.
     * 
     * @author Andrew Jacobs
     */
    public new sealed class Cpl : UnaryExpr
    {
        /**
         * Constructs a <CODE>Cpl</CODE> instance which will complement the
         * associated binary expression.
         * 
         * @param exp		The expression to be complemented.
         */
        public Cpl(Expr? exp) : base(exp)
        {
        }

        /**
         * {@inheritDoc}
         */
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return ~(Exp?.Resolve(sections, symbols) ?? 0);
        }

        /**
         * {@inheritDoc}
         */
        public override string ToString()
        {
            return ($"<cpl>{Exp}</cpl>");
        }
    }

    /**
     * The <CODE>Neg</CODE> class implements the arithmetic negation
     * operation.
     * 
     * @author Andrew Jacobs
     */
    public new sealed class Neg : UnaryExpr
    {
        /**
         * Constructs a <CODE>Neg</CODE> instance which will complement the
         * associated expression.
         * 
         * @param exp		The expression to be complemented.
         */
        public Neg(Expr? exp) : base(exp)
        {
        }

        /**
         * {@inheritDoc}
         */
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return -(Exp?.Resolve(sections, symbols) ?? 0);
        }

        /**
         * {@inheritDoc}
         */
        public override string ToString()
        {
            return ($"<neg>{Exp}</neg>");
        }
    }

    /**
     * {@inheritDoc}
     */
    public sealed override bool IsAbsolute => Exp?.IsAbsolute == true;
    

    /**
     * {@inheritDoc}
     */
    public sealed override bool IsExternal(Section? section)
    {
        return Exp?.IsExternal(section) == true;
    }

    /**
     * {@inheritDoc}
     */
    public abstract override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null);

    /**
     * Constructs a <CODE>UnaryExpr</CODE> instance with the given underlying
     * expression.
     * 
     * @param exp			The underlying expression.
     */
    protected UnaryExpr(Expr? exp)
    {
        Exp = exp;
    }
}
