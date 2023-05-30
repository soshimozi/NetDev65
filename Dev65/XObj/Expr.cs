namespace Dev65.XObj;

/**
 * An instance of the abstract <CODE>Expr</CODE> class represents part of an
 * expression tree.
 * <P>
 * The <CODE>Expr</CODE> class implements a set of functions corresponding to
 * mathematical operations that build expression trees, optimizing to constant
 * values where possible.
 * 
 * @author 	Andrew Jacobs
 * @version	$Id$
 */
public abstract class Expr
{
    /**
     * Calculate the logical AND of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr LogicalAnd(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute == true && rhs?.IsAbsolute == true)
        {
            if ((lhs.Resolve() != 0) && (rhs.Resolve() != 0))
                return True;
            return False;
        }
        return new BinaryExpr.LAnd(lhs, rhs);
    }

    /**
     * Calculate the logical OR of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr LogicalOr(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute != true || rhs?.IsAbsolute != true) return new BinaryExpr.LOr(lhs, rhs);
        return (lhs.Resolve() != 0) || (rhs.Resolve() != 0) ? True : False;
    }

    /**
     * Calculate the logical NOT an expression.
     * 
     * @param 	exp				The input expression.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr LogicalNot(Expr? exp)
    {
        if (exp?.IsAbsolute != true) return new UnaryExpr.Not(exp);
        return exp.Resolve() != 0 ? False : True;
    }

    /**
     * Calculate the binary AND of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr And(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute == true && rhs?.IsAbsolute == true)
            return new Value(null, lhs.Resolve() & rhs.Resolve());

        return new BinaryExpr.And(lhs, rhs);
    }

    /**
     * Calculate the binary OR of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr Or(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute == true && rhs?.IsAbsolute == true)
            return new Value(null, lhs.Resolve() | rhs.Resolve());

        return new BinaryExpr.Or(lhs, rhs);
    }

    /**
     * Calculate the binary XOR of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr Xor(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute == true && rhs?.IsAbsolute == true)
            return new Value(null, lhs.Resolve() ^ rhs.Resolve());

        return new BinaryExpr.Xor(lhs, rhs);
    }

    /**
     * Calculate the binary complement of an expression.
     * 
     * @param 	exp				The sub-expression.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr Cpl(Expr? exp)
    {
        if (exp?.IsAbsolute == true)
            return new Value(null, ~exp.Resolve());

        return new UnaryExpr.Cpl(exp);
    }

    /**
     * Calculate the addition of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr Add(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute == true && rhs?.IsAbsolute == true)
            return new Value(null, lhs.Resolve() + rhs.Resolve());

        // Relative address optimizations
        if (lhs?.IsAbsolute == true && rhs is Value val)
            return new Value(val.GetSection(), lhs.Resolve() + val.GetValue());

        if (lhs is Value lhsValue && rhs?.IsAbsolute == true)
            return new Value(lhsValue.GetSection(), lhsValue.GetValue() + rhs.Resolve());

        return new BinaryExpr.Add(lhs, rhs);
    }

    /**
     * Calculate the subtraction of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr Sub(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute == true && rhs?.IsAbsolute == true)
            return new Value(null, lhs.Resolve() - rhs.Resolve());

        // Relative address optimizations
        if (lhs?.IsAbsolute == true && rhs is Value val)
            return new Value(val.GetSection(), lhs.Resolve() - val.GetValue());

        if (lhs is Value lhsValue && rhs?.IsAbsolute == true)
            return new Value(lhsValue.GetSection(), lhsValue.GetValue() - rhs.Resolve());

        // A useful relative branch optimization
        if (lhs is not Value lh || rhs is not Value rh) return new BinaryExpr.Sub(lhs, rhs);
        if (lh.GetSection() == rh.GetSection())
            return new Value(null, lh.GetValue() - rh.GetValue());

        return new BinaryExpr.Sub(lhs, rhs);
    }

    /**
     * Calculate the multiplication of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr Mul(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute == true && rhs?.IsAbsolute == true)
            return new Value(null, lhs.Resolve() * rhs.Resolve());

        return new BinaryExpr.Mul(lhs, rhs);
    }

    /**
     * Calculate the division of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr Div(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute == true && rhs?.IsAbsolute == true)
            return new Value(null, lhs.Resolve() / rhs.Resolve());

        return new BinaryExpr.Div(lhs, rhs);
    }

    /**
     * Calculate the modulus of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr Mod(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute == true && rhs?.IsAbsolute == true)
            return new Value(null, lhs.Resolve() % rhs.Resolve());

        return new BinaryExpr.Mod(lhs, rhs);
    }

    /**
     * Calculate the negation of an expression.
     * 
     * @param 	exp				The sub-expression.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr Neg(Expr? exp)
    {
        if (exp?.IsAbsolute == true)
            return new Value(null, -exp.Resolve());

        return new UnaryExpr.Neg(exp);
    }

    /**
     * Calculate the right shift of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr Shr(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute == true && rhs?.IsAbsolute == true)
            return new Value(null, lhs.Resolve() >> (int)rhs.Resolve());

        return new BinaryExpr.Shr(lhs, rhs);
    }

    /**
     * Calculate the left shift of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr Shl(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute == true && rhs?.IsAbsolute == true)
            return new Value(null, lhs.Resolve() << (int)rhs.Resolve());

        return new BinaryExpr.Shl(lhs, rhs);
    }

    /**
     * Calculate the equality of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr Eq(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute == true && rhs?.IsAbsolute == true)
            return (lhs.Resolve() == rhs.Resolve()) ? True : False;

        return new BinaryExpr.Eq(lhs, rhs);
    }

    /**
     * Calculate the inequality of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr Ne(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute == true && rhs?.IsAbsolute == true)
            return (lhs.Resolve() != rhs.Resolve()) ? True : False;

        return new BinaryExpr.Ne(lhs, rhs);
    }

    /**
     * Calculate the less than of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr Lt(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute == true && rhs?.IsAbsolute == true)
            return (lhs.Resolve() < rhs.Resolve()) ? True : False;

        return new BinaryExpr.Lt(lhs, rhs);
    }

    /**
     * Calculate the less or equal of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr Le(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute == true && rhs?.IsAbsolute == true)
            return (lhs.Resolve() <= rhs.Resolve()) ? True : False;

        return new BinaryExpr.Le(lhs, rhs);
    }

    /**
     * Calculate the greater than of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr Gt(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute == true && rhs?.IsAbsolute == true)
            return (lhs.Resolve() > rhs.Resolve()) ? True : False;

        return new BinaryExpr.Gt(lhs, rhs);
    }

    /**
     * Calculate the greater or equal of two expressions.
     * 
     * @param 	lhs				The left hand side.
     * @param 	rhs				The right hand side.
     * @return	The resulting value as an <CODE>Expr</CODE>.
     */
    public static Expr Ge(Expr? lhs, Expr? rhs)
    {
        if (lhs?.IsAbsolute == true && rhs?.IsAbsolute == true)
            return (lhs.Resolve() >= rhs.Resolve()) ? True : False;

        return new BinaryExpr.Ge(lhs, rhs);
    }

    /**
     * Determines if this <CODE>Expr</CODE> represents an absolute value.
     * 
     * @return	<CODE>true</CODE> if the value is absolute.
     */
    public abstract bool IsAbsolute { get; }

    /**
     * Determines if this <CODE>Expr</CODE> represents a relative value.
     * 
     * @return	<CODE>true</CODE> if the value is relative.
     */
    public bool IsRelative => !IsAbsolute;
    

    /**
     * Determines if this <CODE>Expr</CODE> represents an external value.
     * 
     * @return	<CODE>true</CODE> if the value is external.
     */
    public abstract bool IsExternal(Section? section);

    /**
     * Calculates the real value of an expression given the details of
     * the section mapping and symbol values.
     * 
     * @param 	sections		A structure showing where sections have been placed.
     * @param 	symbols			A structure showing where symbols are located.
     * @return	The target value of the expression.
     */
    public abstract long Resolve(SectionMap? sections = null, SymbolMap? symbols = null);

    /**
     * A constant <CODE>Value</CODE> representing a true state.
     */
    private static readonly Value True = new(null, 1);

    /**
     * A constant <CODE>Value</CODE> representing a false state.
     */
    private static readonly Value False = new(null, 0);
}
