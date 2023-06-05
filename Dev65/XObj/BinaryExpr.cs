using System.Linq.Expressions;

namespace Dev65.XObj;

/// <summary>
/// The <see cref="BinaryExpr"/> class is the common base of all mathematical and logical operators that take two arguments.
/// </summary>
public abstract class BinaryExpr : Expr
{
    /// <summary>
    /// Get the left hand side sub-expression.
    /// </summary>
    private readonly Expr? _lhs;

    /// <summary>
    /// Get the right hand side sub-expression.
    /// </summary>
    private readonly Expr? _rhs;

    /// <summary>
    /// Constructs a <see cref="BinaryExpr"/> from its left and right sub-expressions.
    /// </summary>
    /// <param name="lhs">The left hand sub-expression.</param>
    /// <param name="rhs">The right hand sub-expression.</param>
    private BinaryExpr(Expr? lhs, Expr? rhs)
    {
        _lhs = lhs;
        _rhs = rhs;
    }

    /// <inheritdoc />
    public override bool IsAbsolute => _lhs?.IsAbsolute == true && _rhs?.IsAbsolute == true;

    /// <inheritdoc />
    public override bool IsExternal(Section? section)
    {
        return _lhs?.IsExternal(section) == true || _rhs?.IsExternal(section) == true;
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
            return _lhs?.Resolve(sections, symbols) != 0 && _rhs?.Resolve(sections, symbols) != 0 ? 1 : 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<land>{_lhs}{_rhs}</land>";
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
            return _lhs?.Resolve(sections, symbols) != 0 || _rhs?.Resolve(sections, symbols) != 0 ? 1 : 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<lor>{_lhs}{_rhs}</lor>";
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
            return _lhs?.Resolve(sections, symbols) & _rhs?.Resolve(sections, symbols) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<and>{_lhs}{_rhs}</and>";
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
            return _lhs?.Resolve(sections, symbols) | _rhs?.Resolve(sections, symbols) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<or>{_lhs}{_rhs}</or>";
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
            return _lhs?.Resolve(sections, symbols) ^ _rhs?.Resolve(sections, symbols) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<xor>{_lhs}{_rhs}</xor>";
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
            return _lhs?.Resolve(sections, symbols) + _rhs?.Resolve(sections, symbols) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<add>{_lhs}{_rhs}</add>";
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
            return _lhs?.Resolve(sections, symbols) - _rhs?.Resolve(sections, symbols) ?? 0;

        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<sub>{_lhs}{_rhs}</sub>";
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
            return _lhs?.Resolve(sections, symbols) * _rhs?.Resolve(sections, symbols) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<mul>{_lhs}{_rhs}</mul>";
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
            return _lhs?.Resolve(sections, symbols) / _rhs?.Resolve(sections, symbols) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<div>{_lhs}{_rhs}</div>";
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
            return _lhs?.Resolve(sections, symbols) % _rhs?.Resolve(sections, symbols) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<mod>{_lhs}{_rhs}</mod>";
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
            return _lhs?.Resolve(sections, symbols) >> (int)(_rhs?.Resolve(sections, symbols) ?? 0) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<shr>{_lhs}{_rhs}</shr>";
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
            return _lhs?.Resolve(sections, symbols) << (int)(_rhs?.Resolve(sections, symbols) ?? 0) ?? 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<shl>{_lhs}{_rhs}</shl>";
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
            return _lhs?.Resolve(sections, symbols) == _rhs?.Resolve(sections, symbols) ? 1 : 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<eq>{_lhs}{_rhs}</eq>";
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
            return _lhs?.Resolve(sections, symbols) != _rhs?.Resolve(sections, symbols) ? 1 : 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<ne>{_lhs}{_rhs}</ne>";
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
            return _lhs?.Resolve(sections, symbols) < _rhs?.Resolve(sections, symbols) ? 1 : 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<lt>{_lhs}{_rhs}</lt>";
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
            return _lhs?.Resolve(sections, symbols) <= _rhs?.Resolve(sections, symbols) ? 1 : 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<le>{_lhs}{_rhs}</le>";
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
            return _lhs?.Resolve(sections, symbols) > _rhs?.Resolve(sections, symbols) ? 1 : 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<gt>{_lhs}{_rhs}</gt>";
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
            return _lhs?.Resolve(sections, symbols) >= _rhs?.Resolve(sections, symbols) ? 1 : 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"<ge>{_lhs}{_rhs}</ge>";
        }
    }
}

public static class BinaryExpressionFactory
{
    public static Expr? LogicalOr(Expr? lhs, Expr? rhs)
    {
        return new BinaryExpr.LOr(lhs, rhs);
    }

    public static Expr? LogicalAnd(Expr? lhs, Expr? rhs)
    {
        return new BinaryExpr.LAnd(lhs, rhs);
    }

    public static Expr? Mod(Expr? lhs, Expr? rhs)
    {
        return new BinaryExpr.Mod(lhs, rhs);
    }

    public static Expr? NotEqual(Expr? lhs, Expr? rhs)
    {
        return new BinaryExpr.Ne(lhs, rhs);
    }

    public static Expr? Subtract(Expr? lhs, Expr? rhs)
    {
        return new BinaryExpr.Sub(lhs, rhs);
    }
}
