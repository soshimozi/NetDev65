namespace Dev65.XObj;

/// <summary>
/// The <see cref="BinaryExpr"/> class is the common base of all mathematical and logical operators that take two arguments.
/// </summary>
public abstract class BinaryExpr : Expr
{
    /// <summary>
    /// Get the left hand side sub-expression.
    /// </summary>
    public Expr? Lhs { get; }

    /// <summary>
    /// Get the right hand side sub-expression.
    /// </summary>
    public Expr? Rhs { get; }

    /// <summary>
    /// Constructs a <see cref="BinaryExpr"/> from its left and right sub-expressions.
    /// </summary>
    /// <param name="lhs">The left hand sub-expression.</param>
    /// <param name="rhs">The right hand sub-expression.</param>
    protected BinaryExpr(Expr? lhs, Expr? rhs)
    {
        Lhs = lhs;
        Rhs = rhs;
    }

    /// <inheritdoc />
    public override bool IsAbsolute => Lhs?.IsAbsolute == true && Rhs?.IsAbsolute == true;

    /// <inheritdoc />
    public override bool IsExternal(Section? section)
    {
        return Lhs?.IsExternal(section) == true || Rhs?.IsExternal(section) == true;
    }

    /// <inheritdoc />
    public abstract override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null);

    /// <summary>
    /// The <see cref="LAnd"/> class implements a logical AND expression tree node.
    /// </summary>
    public sealed class LAnd : BinaryExpr
    {
        /// <summary>
        /// Constructs a <see cref="LAnd"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public LAnd(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) != 0 && Rhs?.Resolve(sections, symbols) != 0 ? 1 : 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<land>{Lhs}{Rhs}</land>";
        }
    }

    /// <summary>
    /// The <see cref="LOr"/> class implements a logical OR expression tree node.
    /// </summary>
    public sealed class LOr : BinaryExpr
    {
        /// <summary>
        /// Constructs a <see cref="LOr"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public LOr(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) != 0 || Rhs?.Resolve(sections, symbols) != 0 ? 1 : 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<lor>{Lhs}{Rhs}</lor>";
        }
    }

    /// <summary>
    /// The <see cref="And"/> class implements a binary AND expression tree node.
    /// </summary>
    public new sealed class And : BinaryExpr
    {
        /// <summary>
        /// Constructs an <see cref="And"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public And(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) & Rhs?.Resolve(sections, symbols) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<and>{Lhs}{Rhs}</and>";
        }
    }

    /// <summary>
    /// The <see cref="Or"/> class implements a binary OR expression tree node.
    /// </summary>
    public new sealed class Or : BinaryExpr
    {
        /// <summary>
        /// Constructs an <see cref="Or"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public Or(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) | Rhs?.Resolve(sections, symbols) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<or>{Lhs}{Rhs}</or>";
        }
    }

    /// <summary>
    /// The <see cref="Xor"/> class implements a binary XOR expression tree node.
    /// </summary>
    public new sealed class Xor : BinaryExpr
    {
        /// <summary>
        /// Constructs an <see cref="Xor"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public Xor(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) ^ Rhs?.Resolve(sections, symbols) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<xor>{Lhs}{Rhs}</xor>";
        }
    }

    /// <summary>
    /// The <see cref="Add"/> class implements an addition expression tree node.
    /// </summary>
    public new sealed class Add : BinaryExpr
    {
        /// <summary>
        /// Constructs an <see cref="Add"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public Add(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) + Rhs?.Resolve(sections, symbols) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<add>{Lhs}{Rhs}</add>";
        }
    }

    /// <summary>
    /// The <see cref="Sub"/> class implements a subtraction expression tree node.
    /// </summary>
    public new sealed class Sub : BinaryExpr
    {
        /// <summary>
        /// Constructs a <see cref="Sub"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public Sub(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) - Rhs?.Resolve(sections, symbols) ?? 0;

        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<sub>{Lhs}{Rhs}</sub>";
        }
    }

    /// <summary>
    /// The <see cref="Mul"/> class implements a multiplication expression tree node.
    /// </summary>
    public new sealed class Mul : BinaryExpr
    {
        /// <summary>
        /// Constructs a <see cref="Mul"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public Mul(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) * Rhs?.Resolve(sections, symbols) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<mul>{Lhs}{Rhs}</mul>";
        }
    }

    /// <summary>
    /// The <see cref="Div"/> class implements a division expression tree node.
    /// </summary>
    public new sealed class Div : BinaryExpr
    {
        /// <summary>
        /// Constructs a <see cref="Div"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public Div(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) / Rhs?.Resolve(sections, symbols) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<div>{Lhs}{Rhs}</div>";
        }
    }

    /// <summary>
    /// The <see cref="Mod"/> class implements a modulus expression tree node.
    /// </summary>
    public new sealed class Mod : BinaryExpr
    {
        /// <summary>
        /// Constructs a <see cref="Mod"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public Mod(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) % Rhs?.Resolve(sections, symbols) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<mod>{Lhs}{Rhs}</mod>";
        }
    }

    /// <summary>
    /// The <see cref="Shr"/> class implements a right shift expression tree node.
    /// </summary>
    public new sealed class Shr : BinaryExpr
    {
        /// <summary>
        /// Constructs a <see cref="Shr"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public Shr(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) >> (int)(Rhs?.Resolve(sections, symbols) ?? 0) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<shr>{Lhs}{Rhs}</shr>";
        }
    }

    /// <summary>
    /// The <see cref="Shl"/> class implements a left shift expression tree node.
    /// </summary>
    public new sealed class Shl : BinaryExpr
    {
        /// <summary>
        /// Constructs a <see cref="Shl"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public Shl(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) << (int)(Rhs?.Resolve(sections, symbols) ?? 0) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<shl>{Lhs}{Rhs}</shl>";
        }
    }

    /// <summary>
    /// The <see cref="Eq"/> class implements an equals expression tree node.
    /// </summary>
    public new sealed class Eq : BinaryExpr
    {
        /// <summary>
        /// Constructs an <see cref="Eq"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public Eq(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) == Rhs?.Resolve(sections, symbols) ? 1 : 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<eq>{Lhs}{Rhs}</eq>";
        }
    }

    /// <summary>
    /// The <see cref="Ne"/> class implements a not equals expression tree node.
    /// </summary>
    public new sealed class Ne : BinaryExpr
    {
        /// <summary>
        /// Constructs a <see cref="Ne"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public Ne(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) != Rhs?.Resolve(sections, symbols) ? 1 : 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<ne>{Lhs}{Rhs}</ne>";
        }
    }

    /// <summary>
    /// The <see cref="Lt"/> class implements a less than expression tree node.
    /// </summary>
    public new sealed class Lt : BinaryExpr
    {
        /// <summary>
        /// Constructs an <see cref="Lt"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public Lt(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) < Rhs?.Resolve(sections, symbols) ? 1 : 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<lt>{Lhs}{Rhs}</lt>";
        }
    }

    /// <summary>
    /// The <see cref="Le"/> class implements a less than or equal expression tree node.
    /// </summary>
    public new sealed class Le : BinaryExpr
    {
        /// <summary>
        /// Constructs a <see cref="Le"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public Le(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) <= Rhs?.Resolve(sections, symbols) ? 1 : 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<le>{Lhs}{Rhs}</le>";
        }
    }

    /// <summary>
    /// The <see cref="Gt"/> class implements a greater than expression tree node.
    /// </summary>
    public new sealed class Gt : BinaryExpr
    {
        /// <summary>
        /// Constructs a <see cref="Gt"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public Gt(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) > Rhs?.Resolve(sections, symbols) ? 1 : 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<gt>{Lhs}{Rhs}</gt>";
        }
    }

    /// <summary>
    /// The <see cref="Ge"/> class implements a greater than or equal expression tree node.
    /// </summary>
    public new sealed class Ge : BinaryExpr
    {
        /// <summary>
        /// Constructs a <see cref="Ge"/> instance from its sub-expressions.
        /// </summary>
        /// <param name="lhs">The left hand sub-expression.</param>
        /// <param name="rhs">The right hand sub-expression.</param>
        public Ge(Expr? lhs, Expr? rhs)
            : base(lhs, rhs)
        {
        }

        /// <inheritdoc />
        public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
        {
            return Lhs?.Resolve(sections, symbols) >= Rhs?.Resolve(sections, symbols) ? 1 : 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<ge>{Lhs}{Rhs}</ge>";
        }
    }
}
