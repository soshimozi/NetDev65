using System.Text;
using Dev65.XApp;
using Dev65.XObj;

namespace Dev65.XAsm;


public abstract class Assembler : Application, IAssembler
{
    private static readonly TokenKind Operator = new("OPERATOR");
    protected static readonly TokenKind Symbol = new("SYMBOL");
    protected static readonly TokenKind Keyword = new("KEYWORD");
    protected static readonly TokenKind Number = new("NUMBER");
    protected static readonly TokenKind String = new("STRING");
    protected static readonly TokenKind Unknown = new("UNKNOWN");

    //protected Dictionary<string, Expr?> symbols = new();

    protected static readonly Token WhiteSpace = new(Unknown, "#SPACE");

    protected static readonly Opcode EOL = new(Unknown, "#EOL", _ => true);

    /// <summary>
    /// A <see cref="Token"/> representing the origin (e.g. $ or @).
    /// </summary>
    protected static readonly Token OriginToken = new(Keyword, "ORIGIN");

    /// <summary>
    /// A <see cref="Token"/> representing a comma.
    /// </summary>
    protected static readonly Token Comma = new(Keyword, ",");

    /// <summary>
    /// A <see cref="Token"/> representing a colon.
    /// </summary>
    protected static readonly Token Colon = new(Keyword, ":");

    /// <summary>
    /// A <see cref="Token"/> representing addition.
    /// </summary>
    protected static readonly Token Plus = new(Keyword, "+");

    /// <summary>
    /// A <see cref="Token"/> representing subtraction.
    /// </summary>
    protected static readonly Token Minus = new(Keyword, "-");

    /// <summary>
    /// A <see cref="Token"/> representing multiply.
    /// </summary>
    protected static readonly Token Times = new(Keyword, "*");

    // Token representing divide.
    protected static readonly Token Divide = new(Operator, "/");

    // Token representing modulo.
    protected static readonly Token Modulo = new(Operator, "%");

    // Token representing complement.
    protected static readonly Token Complement = new(Operator, "~");

    // Token representing binary and.
    protected static readonly Token BinaryAnd = new(Operator, "&");

    // Token representing binary or.
    protected static readonly Token BinaryOr = new(Operator, "|");

    // Token representing binary xor.
    protected static readonly Token BinaryXor = new(Operator, "^");

    // Token representing logical not.
    protected static readonly Token LogicalNot = new(Operator, "!");

    // Token representing logical and.
    protected static readonly Token LogicalAnd = new(Operator, "&&");

    // Token representing logical or.
    protected static readonly Token LogicalOr = new(Operator, "||");

    // Token representing equal.
    private static readonly Token EQ = new(Operator, "=");

    // Token representing not equal.
    private static readonly Token NE = new(Operator, "!=");

    // Token representing less than.
    protected static readonly Token Lt = new(Operator, "<");

    // Token representing less than or equal.
    protected static readonly Token Le = new(Operator, "<=");

    // Token representing greater than.
    protected static readonly Token Gt = new(Operator, ">");

    // Token representing greater than or equal.
    protected static readonly Token Ge = new(Operator, ">=");

    // Token representing a left shift.
    protected static readonly Token LShift = new(Operator, "<<");

    // Token representing a right shift.
    protected static readonly Token RShift = new(Operator, ">>");

    // Token representing an opening parenthesis.
    protected static readonly Token LParen = new(Operator, "(");

    // Token representing a closing parenthesis.
    protected static readonly Token RParen = new(Operator, ")");

    // Token representing the LO function.
    protected static readonly Token LO = new(Keyword, "LO");

    // Token representing the HI function.
    protected static readonly Token HI = new(Keyword, "HI");

    // Token representing the STRLEN function.
    protected static readonly Token STRLEN = new(Keyword, "STRLEN");

    // Token representing the BANK function.
    protected static readonly Token BANK = new(Keyword, "BANK");

    // Opcode that handles .INCLUDE directives.
    protected static readonly Token INCLUDE = new Opcode(Keyword, ".INCLUDE",
        assembler =>
        {
            assembler.CurrentToken = assembler.NextRealToken();
            if (assembler.CurrentToken?.Kind == String)
            {
                var filename = assembler.CurrentToken.Text;
                var stream = assembler.FindFile(filename, true);

                if (stream != null)
                {
                    assembler.Sources.Push(new FileSource(filename, stream));
                }
                else
                {
                    assembler.OnError($"{ErrorMessage.ERR_FAILED_TO_FIND_FILE}({filename})");
                }
            }
            else
            {
                assembler.OnError(ErrorMessage.ERR_EXPECTED_QUOTED_FILENAME);
            }

            return false;
        });


    /**
	 * An <CODE>Opcode</CODE> that handles .APPEND directives
	 */
    protected static readonly Token APPEND = new Opcode(Keyword, ".APPEND", assembler => 
    {
            if (assembler.CurrentToken?.Kind == String)
            {
                var filename = assembler.CurrentToken.Text;
                var stream = assembler.FindFile(filename, false);

                if (stream != null)
                {
                    assembler.Sources.Pop();
                    assembler.Sources.Push(new FileSource(filename, stream));
                }
                else
                    assembler.OnError($"{ErrorMessage.ERR_FAILED_TO_FIND_FILE} ({filename})");
            }
            else
                assembler.OnError(ErrorMessage.ERR_EXPECTED_QUOTED_FILENAME);

            return (false);
    });

    protected static readonly Token INSERT = new Opcode(Keyword, ".INSERT", assembler =>
    {
        if (assembler.CurrentToken?.Kind == String)
        {
            var filename = assembler.CurrentToken?.Text;
            var stream = assembler.FindFile(filename ?? string.Empty, false);

            if (stream != null)
            {
                using var buffer = new BufferedStream(stream);

                try
                {
                    int ch;
                    while ((ch = buffer.ReadByte()) != -1)
                    {
                        assembler.AddByte((byte)ch);
                    }
                }
                catch (IOException)
                {
                   assembler.OnError(ErrorMessage.ERR_INSERT_IO_ERROR);
                }
            }
            else
               assembler.OnError($"{ErrorMessage.ERR_FAILED_TO_FIND_FILE} ({filename})");
        }
        else
            assembler.OnError(ErrorMessage.ERR_EXPECTED_QUOTED_FILENAME);

        return (false);
    });

    protected static readonly Opcode End = new(Keyword, ".END", assembler => {
        assembler.Sources.Clear();
        return false;
    });

    protected static readonly Opcode Equ = new(Keyword, ".EQU", assembler =>
    {
        assembler.CurrentToken = assembler.NextRealToken();
        assembler.Addr = assembler.ParseExpression();

        if (assembler.Label != null)
        {
            if (assembler.Pass == Pass.FIRST)
            {
                if (assembler.Variable.Contains(assembler.Label.Text))
                {
                    assembler.OnError("Symbol has already been defined with .SET");
                    return (false);
                }
                if (assembler.Symbols.ContainsKey(assembler.Label.Text))
                {
                    assembler.OnError(ErrorMessage.ERR_LABEL_REDEFINED);
                    return (false);
                }

                if (assembler.Label.Text[0] == '.')
                    assembler.NotLocal.Add(assembler.Label.Text);
            }

            assembler.Symbols.SafeAdd(assembler.Label.Text, assembler.Addr);
        }
        else
            assembler.OnError("No symbol name defined for .EQU");

        return (false);
    });

    protected static readonly Opcode Set = new(Keyword, ".SET", assembler =>
    {
        assembler.CurrentToken = assembler.NextRealToken();
        assembler.Addr = assembler.ParseExpression();

        if (assembler.Label != null)
        {
            if (assembler.Pass == Pass.FIRST)
            {
                if (assembler.Symbols.ContainsKey(assembler.Label.Text) && !assembler.Variable.Contains(assembler.Label.Text))
                {
                    assembler.OnError("Symbol has already been defined with .EQU");
                    return (false);
                }

                if (assembler.Label.Text[0] == '.')
                    assembler.NotLocal.Add(assembler.Label.Text);

                assembler.Variable.Add(assembler.Label.Text);
            }

            assembler.Symbols.Add(assembler.Label.Text, assembler.Addr);
        }
        else
            assembler.OnError("No symbol name defined for .SET");

        return (false);
    });

    protected static readonly Opcode Space = new(Keyword, ".SPACE", assembler =>
    {
        assembler.CurrentToken = assembler.NextRealToken();
        var expr = assembler.ParseExpression();

        if (expr != null)
        {
            if (expr.IsAbsolute)
            {
                var value = expr.Resolve();

                for (var index = 0; index < value; ++index)
                    assembler.AddByte(0);
            }
            else
                assembler.OnError(ErrorMessage.ERR_CONSTANT_EXPR);
        }
        else
            assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);

        return (true);
    });

    protected static readonly Opcode Align = new(Keyword, ".ALIGN", assembler =>
    {
        assembler.CurrentToken = assembler.NextRealToken();
        var expr = assembler.ParseExpression();

        if (expr?.IsAbsolute == true)
        {
            var value = expr.Resolve();
            var count = assembler.Origin?.Resolve() % value;

            while ((count > 0) && (count++ != value))
                assembler?.AddByte(0);
        }
        else
            assembler.OnError(ErrorMessage.ERR_CONSTANT_EXPR);

        return (true);
    });

    protected static readonly Opcode Dcb = new(Keyword, ".DCB", assembler =>
    {
        assembler.CurrentToken = assembler.NextRealToken();
        var expr = assembler.ParseExpression();

        if (expr?.IsAbsolute == true)
        {
            var value = expr.Resolve();

            if (assembler.CurrentToken == Comma)
            {
                assembler.CurrentToken = assembler.NextRealToken();
                expr = assembler.ParseExpression();

                if (expr?.IsAbsolute == true)
                {
                    var fill = expr.Resolve();

                    for (var index = 0; index < value; ++index)
                        assembler.AddByte((byte)fill);
                }
                else
                    assembler.OnError(ErrorMessage.ERR_CONSTANT_EXPR);
            }
            else
                for (var index = 0; index < value; ++index)
                    assembler.AddByte(0);
        }
        else
            assembler.OnError(ErrorMessage.ERR_CONSTANT_EXPR);

        return (true);
    });

    protected static readonly Opcode Byte = new(Keyword, ".BYTE", assembler =>
    {
        do
        {
            assembler.CurrentToken = assembler.NextRealToken();
            if (assembler.CurrentToken?.Kind == String)
            {
                var value = assembler.CurrentToken.Text;

                foreach (var t in value)
                    assembler.AddByte((byte)t);

                assembler.CurrentToken = assembler.NextRealToken();
            }
            else
            {
                var expr = assembler.ParseExpression();

                if (expr != null)
                    assembler.AddByte(expr);
                else
                    assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
            }
        } while (assembler.CurrentToken == Comma);

        if (assembler.CurrentToken != EOL) assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        return (true);
    });

    protected static readonly Opcode DByte = new(Keyword, ".DBYTE", assembler =>
    {
        do
        {
            assembler.CurrentToken = assembler.NextRealToken();
            var expr = assembler.ParseExpression();

            if (expr != null)
            {
                assembler.AddByte(Expr.And(Expr.Shr(expr, EIGHT), MASK));
                assembler.AddByte(Expr.And(expr, MASK));
            }
            else
                assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        } while (assembler.CurrentToken == Comma);

        if (assembler.CurrentToken != EOL) assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        return (true);
    });

    protected static readonly Opcode Word = new(Keyword, ".WORD", assembler =>
    {
        do
        {
            assembler.CurrentToken = assembler.NextRealToken();
            var expr = assembler.ParseExpression();

            if (expr != null)
                assembler.AddWord(expr);
            else
                assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        } while (assembler.CurrentToken == Comma);

        if (assembler.CurrentToken != EOL) assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        return (true);
    });

    protected static readonly Opcode LONG = new(Keyword, ".LONG", assembler =>
    {
        do
        {
            assembler.CurrentToken = assembler.NextRealToken();
            var expr = assembler.ParseExpression();

            if (expr != null)
                assembler.AddLong(expr);
            else
                assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        } while (assembler.CurrentToken == Comma);

        if (assembler.CurrentToken != EOL) assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        return (true);
    });

    protected static readonly Opcode IF = new(Keyword, ".IF", assembler =>
    {
        if (assembler.IsActive)
        {
            assembler.CurrentToken = assembler.NextRealToken();

            var expr = assembler.ParseExpression();

            if (expr == null)
            {
                assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
                return (false);
            }

            if (expr.IsAbsolute)
            {
                var state = expr.Resolve() != 0;
                assembler.Status.Push((assembler.IsActive && state));
            }
            else
                assembler.OnError(ErrorMessage.ERR_CONSTANT_EXPR);
        }
        else
            assembler.Status.Push(false);

        return (false);
    }, true);

    protected static readonly Opcode IFABS = new(Keyword, ".IFABS", assembler =>
    {
        if (assembler.IsActive)
        {
            assembler.CurrentToken = assembler.NextRealToken();

            var expr = assembler.ParseExpression();

            if (expr == null)
            {
                assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
                return (false);
            }

            assembler.Status.Push(expr.IsAbsolute);
        }
        else
            assembler.Status.Push(false);

        return (false);
    }, true);

    protected static readonly Opcode IFNABS = new(Keyword, ".IFNABS", assembler =>
    {
        if (assembler.IsActive)
        {
            assembler.CurrentToken = assembler.NextRealToken();

            var expr = assembler.ParseExpression();

            if (expr == null)
            {
                assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
                return (false);
            }

            assembler.Status.Push(!expr.IsAbsolute);
        }
        else
            assembler.Status.Push(false);

        return (false);
    }, true);

    protected static readonly Opcode IFREL = new(Keyword, ".IFREL", assembler =>
    {
        if (assembler.IsActive)
        {
            assembler.CurrentToken = assembler.NextRealToken();

            var expr = assembler.ParseExpression();

            if (expr == null)
            {
                assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
                return (false);
            }

            assembler.Status.Push(expr.IsRelative);
        }
        else
            assembler.Status.Push(false);

        return (false);
    }, true);

    protected static readonly Opcode IFNREL = new(Keyword, ".IFNREL", assembler =>
    {
        if (assembler.IsActive)
        {
            assembler.CurrentToken = assembler.NextRealToken();

            var expr = assembler.ParseExpression();

            if (expr == null)
            {
                assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
                return (false);
            }

            assembler.Status.Push(!expr.IsRelative);
        }
        else
            assembler.Status.Push(false);

        return (false);
    }, true);

    protected static readonly Opcode IFDEF = new (Keyword, ".IFDEF", assembler =>
    {
        if (assembler.IsActive)
        {
            assembler.CurrentToken = assembler.NextRealToken();
            if (assembler.CurrentToken?.Kind != Symbol)
            {
                assembler.OnError("Expected a symbol");
                return false;
            }

            assembler.Status.Push(assembler.Symbols.ContainsKey(assembler.CurrentToken.Text));
        }
        else
            assembler.Status.Push(false);

        return false;

    }, true);


    protected static readonly Opcode IFNDEF = new(Keyword, ".IFNDEF", assembler =>
    {
        if (assembler.IsActive)
        {
            assembler.CurrentToken = assembler.NextRealToken();
            if (assembler.CurrentToken?.Kind != Symbol)
            {
                assembler.OnError("Expected a symbol");
                return false;
            }

            assembler.Status.Push(!assembler.Symbols.ContainsKey(assembler.CurrentToken.Text));
        }
        else
            assembler.Status.Push(false);

        return false;

    }, true);


    protected static readonly Opcode ELSE = new(Keyword, ".ELSE", assembler =>
    {
        if (assembler.Status.Count != 0)
        {
            var state = assembler.Status.Pop();
            assembler.Status.Push(assembler.IsActive && !state);
        }
        else
            assembler.OnError(ErrorMessage.ERR_NO_OPEN_IF);

        return (false);
    }, true);

    protected static readonly Opcode ENDIF = new(Keyword, ".ENDIF", assembler =>
    {
        if (assembler.Status.Count != 0)
            assembler.Status.Pop();
        else
            assembler.OnError(ErrorMessage.ERR_NO_OPEN_IF);

        return (false);
    }, true);


    protected static readonly Opcode ERROR = new(Keyword, ".ERROR", assembler =>
    {
        if (!assembler.IsActive) return (false);
        assembler.CurrentToken = assembler.NextRealToken();
        assembler.OnError(assembler.CurrentToken?.Kind == String
            ? assembler.CurrentToken.Text
            : ErrorMessage.ERR_EXPECTED_QUOTED_MESSAGE);

        return (false);
    }, true);


    protected static readonly Opcode WARN = new(Keyword, ".WARN", assembler =>
    {
        if (!assembler.IsActive) return (false);

        assembler.CurrentToken = assembler.NextRealToken();
        if (assembler.CurrentToken?.Kind == String)
        {
            assembler.OnWarning(assembler.CurrentToken.Text);
        }
        else
            assembler.OnError(ErrorMessage.ERR_EXPECTED_QUOTED_MESSAGE);

        return (false);
    }, true);

    protected static readonly Token MACRO = new Opcode(Keyword, ".MACRO", assembler =>
    {
        if ((assembler.Label != null) && ((assembler.MacroName = assembler.Label.Text) != null))
        {
            var arguments = new List<string>();

            for (; ; )
            {
                if ((assembler.CurrentToken = assembler.NextRealToken()) == EOL) break;

                if ((assembler.CurrentToken?.Kind == Symbol) || (assembler.CurrentToken?.Kind == Keyword))
                    arguments.Add(assembler.CurrentToken.Text);
                else
                {
                    assembler.OnError("Illegal macro argument");
                    break;
                }

                if ((assembler.CurrentToken = assembler.NextRealToken()) == EOL) break;

                if (assembler.CurrentToken == Comma) continue;

                assembler.OnError("Unexpected CurrentToken after macro argument");
                break;
            }

            assembler.SavedLines = new MacroSource(arguments);
        }
        else
            assembler.OnError("No macro name has been specified");

        return (false);

    });

    protected static readonly Token ENDM = new Opcode(Keyword, ".ENDM", assembler =>
    {
        if (assembler.SavedLines != null)
        {
            assembler.Macros.Add(assembler.MacroName ?? string.Empty, assembler.SavedLines);
            assembler.SavedLines = null;
        }
        else
            assembler.OnError(".ENDM without a preceding .MACRO");

        return (false);
    });

    protected static readonly Token EXITM = new Opcode(Keyword, ".EXITM", assembler =>
    {
        while (assembler.Sources.Peek() is MacroSource)
            assembler.Sources.Pop();
			
        return (false);
    });

    protected static readonly Token REPEAT = new Opcode(Keyword, ".REPEAT", assembler =>
    {
        assembler.CurrentToken = assembler.NextRealToken();
        var expr = assembler.ParseExpression();

        if (expr?.IsAbsolute == true)
        {
            assembler.SavedLines = new RepeatSource((int)expr.Resolve());
        }
        else
            assembler.OnError(ErrorMessage.ERR_CONSTANT_EXPR);

        return (false);
    });

    protected static readonly Token ENDR = new Opcode(Keyword, ".ENDR", assembler =>
    {
        if (assembler.SavedLines != null)
        {
            assembler.Sources.Push(assembler.SavedLines);
            assembler.SavedLines = null;
        }
        else
            assembler.OnError(".ENDR without preceding .REPEAT");

        return (false);
    });

    protected readonly Opcode CODE = new(Keyword, ".CODE", assembler =>
    {
        assembler.SetSection(".code");
        return (false);
    });

    protected readonly Opcode DATA = new (Keyword, ".DATA", assembler =>
    {
        assembler.SetSection(".data");
        return (false);
    });

    protected readonly Opcode BSS = new (Keyword, ".BSS", assembler =>
    {
        assembler.SetSection(".bss");
        return (false);
    });

    protected readonly Opcode ORG = new (Keyword, ".ORG", assembler =>
    {
        assembler.CurrentToken = assembler.NextRealToken();
        var expr = assembler.ParseExpression();

        if (expr is { IsAbsolute: true })
        {
            assembler.Sections.SafeAdd(assembler.SectionName ?? string.Empty, assembler.Section = assembler.Section?.SetOrigin(expr.Resolve()));
        }
        else
            assembler.OnError(ErrorMessage.ERR_CONSTANT_EXPR);
        return (true);
    });

    protected readonly Opcode EXTERN = new (Keyword, ".EXTERN", assembler =>
    {
        if (assembler.Pass== Pass.FIRST)
        {
            do
            {
                assembler.CurrentToken = assembler.NextRealToken();
                if (assembler.CurrentToken?.Kind != Symbol)
                {
                    assembler.OnError("Expected a list of symbols");
                    return (false);
                }

                var name = assembler.CurrentToken.Text;
                assembler.Externals.Add(name);
                if (!assembler.Symbols.ContainsKey(name))
                    assembler.Symbols.Add(name, new Extern(name));
                assembler.CurrentToken = assembler.NextRealToken();
            } while (assembler.CurrentToken == Comma);
        }

        return (false);
    });

    protected readonly Opcode GLOBAL = new(Keyword, ".GLOBAL", assembler =>
    {
        if (assembler.Pass == Pass.FIRST)
        {
            do
            {
                assembler.CurrentToken = assembler.NextRealToken();
                if (assembler.CurrentToken?.Kind != Symbol)
                {
                    assembler.OnError("Expected a list of symbols");
                    return (false);
                }

                var name = assembler.CurrentToken.Text;
                assembler.Globals.Add(name);

                assembler.CurrentToken = assembler.NextRealToken();
            } while (assembler.CurrentToken == Comma);
        }

        return (false);
    });

    protected readonly Opcode LIST = new(Keyword, ".LIST", assembler =>
    {
        assembler.Listing = true;
        return (false);
    });

    protected readonly Opcode NOLIST = new(Keyword, ".NOLIST", assembler => 
    {
            assembler.Listing = false;
            return (false);
    });

    protected readonly Opcode PAGE = new(Keyword, ".PAGE", assembler => 
    {
            assembler.ThrowPage = true;
            return (false);
    });

    protected readonly Opcode TITLE = new(Keyword, ".TITLE", assembler => 
    {
            assembler.CurrentToken = assembler.NextRealToken();

            assembler.Title = assembler.CurrentToken?.Text;
            return (false);
    });


    /**
 * Adds a byte value to the output memory area.
 * 
 * @param	expr		The expression defining the value.
 */
    public void AddByte(Expr? expr)
    {
        Memory?.AddByte(Module, Section, expr);
    }

    private void SetLabel(string name, Value value)
    {
        if ((Pass == Pass.FIRST) && Symbols.ContainsKey(name))
        {
            OnError($"{ErrorMessage.ERR_LABEL_REDEFINED}{name}");
        }
        else
        {
            Symbols.SafeAdd(name, value);
        }
    }
    /**
	 * Adds a byte value to the output memory area.
	 * 
	 * @param	expr		The expression defining the value.
	private void setLabel (final String name, Value value)
	{
		if ((pass == Pass.FIRST) && symbols.containsKey (name))
			error (Error.ERR_LABEL_REDEFINED + name);
		else
			symbols.put (name, value);
	}
    */
    public void AddByte(byte value)
    {
        Memory?.AddByte(Module, Section, value);
    }

    public abstract int ParseMode(int bank);
    public abstract void GenerateImmediate(int opcode, Expr? expr, int bits);
    public abstract void GenerateImplied(int opcode);
    public abstract Expr? Arg { get; set; }

    public void AddWord(Expr? expr)
    {
        Memory?.AddWord(Module, Section, expr);
    }

    public void AddLong(Expr? expr)
    {
        Memory?.AddLong(Module, Section, expr);
    }

    /**
	 * Determines if source lines are to be translated or skipped over
	 * depending on the current conditional compilation state.
	 * 
	 * @return <CODE>true</CODE> if source lines should be processed.
	 */
    public bool IsActive => Status.Count == 0 || Status.Peek();

    public Stack<bool> Status { get; } = new();
    public string? MacroName { get; set; }
    public TextSource? SavedLines { get; set; }
    public Dictionary<string, TextSource> Macros { get; } = new();
    public Dictionary<string, Section?> Sections { get; } = new();
    public bool ThrowPage { get; set; }
    protected Module? Module { get; }
    public Section? Section { get; set; }
    public HashSet<string> Globals { get; } = new();
    public string? SectionName { get; set; }
    public bool Listing { get; set; }
    public string? Title { get; set; }

    public Expr? ParseExpression()
    {
        try
        {
            return (ParseLogical());
        }
        catch (Exception)
        {
            OnError("Invalid expression");
        }

        return (ZERO);
    }

    public abstract int DataBank { get; set; }

    private Expr? ParseLogical()
    {
        var expr = ParseBinary();

        while ((CurrentToken == LogicalAnd) || (CurrentToken == LogicalOr))
        {
            if (CurrentToken == LogicalAnd)
            {
                CurrentToken = NextRealToken();
                expr = Expr.LogicalAnd(expr, ParseBinary());
            }
            else
            {
                CurrentToken = NextRealToken();
                expr = Expr.LogicalOr(expr, ParseBinary());
            }
        }
        return (expr);
    }

    private Expr? ParseBinary()
    {
        var expr = ParseEquality();

        while ((CurrentToken == BinaryAnd) || (CurrentToken == BinaryOr) || (CurrentToken == BinaryXor))
        {
            if (CurrentToken == BinaryAnd)
            {
                CurrentToken = NextRealToken();
                expr = Expr.And(expr, ParseEquality());
            }
            else if (CurrentToken == BinaryOr)
            {
                CurrentToken = NextRealToken();
                expr = Expr.Or(expr, ParseEquality());
            }
            else
            {
                CurrentToken = NextRealToken();
                expr = Expr.Xor(expr, ParseEquality());
            }
        }
        return (expr);
    }

    private Expr? ParseEquality()
    {
        var expr = ParseInequality();

        while ((CurrentToken == EQ) || (CurrentToken == NE))
        {
            if (CurrentToken == EQ)
            {
                CurrentToken = NextRealToken();
                expr = Expr.Eq(expr, ParseInequality());
            }
            else
            {
                CurrentToken = NextRealToken();
                expr = Expr.Ne(expr, ParseInequality());
            }
        }
        return (expr);
    }

    private Expr? ParseInequality()
    {
        var expr = ParseShift();

        while ((CurrentToken == Lt) || (CurrentToken == Le) || (CurrentToken == Gt) || (CurrentToken == Ge))
        {
            if (CurrentToken == Lt)
            {
                CurrentToken = NextRealToken();
                expr = Expr.Lt(expr, ParseShift());
            }
            else if (CurrentToken == Le)
            {
                CurrentToken = NextRealToken();
                expr = Expr.Le(expr, ParseShift());
            }
            else if (CurrentToken == Gt)
            {
                CurrentToken = NextRealToken();
                expr = Expr.Gt(expr, ParseShift());
            }
            else
            {
                CurrentToken = NextRealToken();
                expr = Expr.Ge(expr, ParseShift());
            }
        }
        return (expr);
    }

    private Expr? ParseShift()
    {
        var expr = ParseAddSub();

        while ((CurrentToken == RShift) || (CurrentToken == LShift))
        {
            if (CurrentToken == RShift)
            {
                CurrentToken = NextRealToken();
                expr = Expr.Shr(expr, ParseAddSub());
            }
            else
            {
                CurrentToken = NextRealToken();
                expr = Expr.Shl(expr, ParseAddSub());
            }
        }
        return (expr);
    }

    private Expr? ParseAddSub()
    {
        var expr = ParseMulDiv();

        while ((CurrentToken == Plus) || (CurrentToken == Minus))
        {
            if (CurrentToken == Plus)
            {
                CurrentToken = NextRealToken();
                expr = Expr.Add(expr, ParseMulDiv());
            }
            else
            {
                CurrentToken = NextRealToken();
                expr = Expr.Sub(expr, ParseMulDiv());
            }
        }
        return (expr);
    }

    private Expr? ParseMulDiv()
    {
        var expr = ParseUnary();

        while ((CurrentToken == Times) || (CurrentToken == Divide) || (CurrentToken == Modulo))
        {
            if (CurrentToken == Times)
            {
                CurrentToken = NextRealToken();
                expr = Expr.Mul(expr, ParseUnary());
            }
            else if (CurrentToken == Divide)
            {
                CurrentToken = NextRealToken();
                expr = Expr.Div(expr, ParseUnary());
            }
            else
            {
                CurrentToken = NextRealToken();
                expr = Expr.Mod(expr, ParseUnary());
            }
        }
        return (expr);
    }

    private Expr? ParseUnary()
    {
        if (CurrentToken == Minus)
        {
            CurrentToken = NextRealToken();
            return (Expr.Neg(ParseUnary()));
        }

        if (CurrentToken == Plus)
        {
            CurrentToken = NextRealToken();
            return (ParseUnary());
        }

        if (CurrentToken == Complement)
        {
            CurrentToken = NextRealToken();
            return (Expr.Cpl(ParseUnary()));
        }
        if (CurrentToken == LogicalNot)
        {
            CurrentToken = NextRealToken();
            return (Expr.LogicalNot(ParseUnary()));
        }
        if (CurrentToken == LO)
        {
            CurrentToken = NextRealToken();
            return (Expr.And(ParseUnary(), MASK));
        }
        if (CurrentToken == HI)
        {
            CurrentToken = NextRealToken();
            return (Expr.And(Expr.Shr(ParseUnary(), EIGHT), MASK));
        }
        if (CurrentToken == BANK)
        {
            CurrentToken = NextRealToken();
            return (Expr.Shr(ParseUnary(), SIXTEEN));
        }

        if (CurrentToken != STRLEN) return (ParseValue());

        CurrentToken = NextRealToken();
        if (CurrentToken != LParen)
        {
            OnError("Expected open parenthesis");
            return (null);
        }

        CurrentToken = NextRealToken();
        if ((CurrentToken == null) || (CurrentToken.Kind != String))
        {
            OnError("Expected string value in STRLEN");
            return (null);
        }

        var value = new Value(null, CurrentToken.Text.Length);

        CurrentToken = NextRealToken();
        if (CurrentToken != RParen)
        {
            OnError("Expected close parenthesis");
            return (null);
        }
        CurrentToken = NextRealToken();

        return (value);

    }

    /// <summary>
    /// Parse part of an expression that should result in a value.
    /// </summary>
    /// <returns>A compiled expression.</returns>
    private Expr? ParseValue()
    {
        Expr? expr = null;

        if (CurrentToken == OriginToken || CurrentToken == Times)
        {
            expr = Origin;
            CurrentToken = NextRealToken();
        }
        else if (CurrentToken == LParen)
        {
            CurrentToken = NextRealToken();
            expr = ParseExpression();
            if (CurrentToken != RParen)
                OnError(ErrorMessage.ERR_CLOSING_PAREN);
            else
                CurrentToken = NextRealToken();
        }
        else if (CurrentToken?.Kind == Number)
        {
            expr = new Value(null, ((int)((CurrentToken?.Value) ?? 0)));
            CurrentToken = NextRealToken();
        }
        else if (CurrentToken?.Kind == Symbol || CurrentToken?.Kind == Keyword)
        {
            if (CurrentToken.Text[0] == '.' && !NotLocal.Contains(CurrentToken.Text))
            {
                if (_lastLabel != null)
                    expr = Symbols[_lastLabel + CurrentToken.Text];
                else
                    OnError(ErrorMessage.ERR_NO_GLOBAL);
            }
            else
                Symbols.TryGetValue(CurrentToken.Text, out expr);

            if (expr == null)
            {
                if (Pass == Pass.FINAL)
                    OnError(ErrorMessage.ERR_UNDEF_SYMBOL + CurrentToken.Text);
                expr = ZERO;
            }

            CurrentToken = NextRealToken();
        }

        return expr;
    }

    /// <summary>
    /// Constructs an <see cref="Assembler"/> that adds code to the given module.
    /// </summary>
    /// <param name="module">The object module</param>
    protected Assembler(Module? module)
    {
        Module = module;
    }

    /// <summary>
    /// Set the <see cref="MemoryModel"/> instance that describes the target
    /// </summary>
    /// <param name="memoryModel">The <see cref="MemoryModel"/> instance</param>
    protected void SetMemoryModel(MemoryModel memoryModel)
    {
        Memory = memoryModel;

        memoryModel.AssemblerError += (_, args) => OnError(args.Message);
        memoryModel.AssemblerWarning += (_, args) => OnWarning(args.Message);
    }

    protected override void StartUp()
    {
        base.StartUp();

        if (_defineOption.IsPresent)
        {
            var defines = _defineOption.Value?.Split(",");

            for (var index = 0; index < defines?.Length; ++index)
            {
                var parts = defines?[index].Split("=");
                switch (parts?.Length)
                {
                    case 1: DoSet(parts[0], ONE);
                        break;

                    case 2:
                    {
                        long value;

                        switch (parts[1][0])
                        {
                            case '%':
                                value = Convert.ToInt64(parts[1][1..], 2);
                                break;
                            case '@':
                                value = Convert.ToInt64(parts[1][1..], 8);
                                break;
                            case '$':
                                value = Convert.ToInt64(parts[1][1..], 16);
                                break;
                            default:
                                value = long.Parse(parts[1]);
                                break;
                        }

                        DoSet(parts[0], new Value(null, value));
                        break;
                        }

                    default:
                        Console.WriteLine($"Error: Invalid define ({defines?[index]})");
                        IsFinished = true;
                        break;
                }
            }
        }

        switch (GetArguments()?.Length)
        {
            case 0:
                Console.WriteLine("Error: No source file name provided");
                IsFinished = true;
                break;

            case 1: break;

            default:
                Console.WriteLine("Error: Only one source file may be given");
                IsFinished = true;
                break;
        }
    }

    protected override string DescribeArguments()
    {
        return (" <source file>");
    }

    protected override void Execute()
    {
        Assemble(GetArguments()?[0] ?? string.Empty);
        IsFinished = true;
    }

    protected override void CleanUp()
    {
        if (_errors > 0) Environment.Exit(-1);
    }

    /// <summary>
    /// This method is called at the start of each pass to allow variables
    /// to be initialized
    /// </summary>
    protected virtual void StartPass()
    {
        _listing = true;
        Title = "";
        _lineCount = 0;
        ThrowPage = false;

        Sections.Clear();
        Sections.Add(".code", Module?.FindSection(".code"));
        Sections.Add(".data", Module?.FindSection(".data"));
        Sections.Add(".bss", Module?.FindSection(".bss"));
    }

    private void Process()
    {
        while ((Sources.Count != 0))
        {
            Line? nextLine;
            if ((nextLine = GetNextLine()) == null)
            {
                var source = Sources.Pop();
                source.Dispose();

                continue;
            }

            Process(nextLine);

            if (Pass == Pass.FINAL)
            {
                Paginate(FormatListing() + ExpandText());
            }
        }
    }


    /// <summary>
    /// Formats the source nextLine and generated code into a printable string.
    /// </summary>
    /// <returns>The string to add to the listing.</returns>
    protected abstract string FormatListing();

    private string ExpandText()
    {
        Buffer.Clear();

        var text = _text ?? Array.Empty<char>();
        for (var index = 0; index < _text?.Length; ++index)
        {
            if (text[index] == '\t')
            {
                do
                {
                    Buffer.Append(" ");
                } while (Buffer.Length % TabSize != 0);
            }
            else Buffer.Append(text[index]);
        }

        return (Buffer.ToString());
        
    }

    /// <summary>
    /// Initializes the tokenizer to process the given nextLine. This method is
    /// overloaded in derived classes.
    /// </summary>
    /// <param name="nextLine">The next <see cref="Line"/> to be processed.</param>
    private void Process(Line nextLine)
    {
        LineType = Sources.Peek() is TextSource ? '+' : ' ';

        Memory?.Clear();

        Label = null;
        _line = nextLine;
        _text = _line.Text.ToCharArray();
        _offset = 0;

        Addr = Origin = Section?.GetOrigin();

        if ((CurrentToken = NextToken()) == EOL) return;

        // Extract and save the labels
        if (CurrentToken != WhiteSpace)
        {
            Label = CurrentToken;
            if (Label?.Kind != Symbol)
            {
                if (Pass == Pass.FIRST)
                {
                    OnWarning($"{ErrorMessage.WRN_LABEL_IS_A_RESERVED_WORD} ({Label?.Text})");
                }
            }

            if ((CurrentToken = NextToken()) == Colon)
            {
                CurrentToken = NextToken();
            }
        }

        if (CurrentToken == WhiteSpace) CurrentToken = NextRealToken();

        // Map = to .SET when used as an opcode
        if (CurrentToken == EQ)
            CurrentToken = Set;

        if (CurrentToken is Opcode opcode)
        {
            if (opcode.IsAlwaysActive || IsActive)
            {
                if (_savedLines != null)
                {
                    switch (_savedLines)
                    {
                        case RepeatSource:
                        {
                            if (opcode == ENDR)
                            {
                                if (--_repeatDepth == 0)
                                {
                                    opcode.Compile(this);
                                    return;
                                }
                            }

                            if (opcode == REPEAT)
                                _repeatDepth++;
                            break;
                        }
                        case MacroSource:
                        {
                            if (opcode == ENDM)
                            {
                                if (--_macroDepth == 0)
                                {
                                    opcode.Compile(this);
                                    return;
                                }
                            }

                            if (opcode == MACRO)
                                _macroDepth++;
                            break;
                        }
                    }

                    _savedLines.AddLine(_line);
                    return;
                }

                if (opcode == MACRO)
                {
                    if (_macroDepth++ == 0)
                    {
                        opcode.Compile(this);
                        LineType = ' ';
                        return;
                    }
                }

                if (opcode == REPEAT)
                {
                    if (_repeatDepth++ == 0)
                    {
                        opcode.Compile(this);

                        if (Label == null) return;
                        if (Origin != null)
                        {
                            if (Label.Text[0] == '.')
                            {
                                if (_lastLabel != null)
                                    SetLabel(_lastLabel + Label.Text, Origin);
                            }
                        }
                        else
                        {
                            OnError(ErrorMessage.ERR_NO_SECTION);
                        }

                        if (LineType == ' ') LineType = ':';

                        return;
                    }
                }

                if (opcode == Equ || opcode == Set)
                {
                    opcode.Compile(this);
                    LineType = '=';
                    return;
                }

                if (Label != null)
                {
                    if (Origin != null)
                    {
                        if (Label.Text[0] == '.')
                        {
                            if (_lastLabel != null)
                            {
                                SetLabel($"{_lastLabel}{Label.Text}", Origin);
                            }
                            else
                            {
                                OnError(ErrorMessage.ERR_NO_GLOBAL);
                            }
                        }
                        else
                        {
                            _lastLabel = Label.Text;
                            SetLabel(_lastLabel, Origin);
                        }
                    }
                    else
                    {
                        OnError(ErrorMessage.ERR_NO_SECTION);
                    }
                }

                if (!opcode.Compile(this)) return;
                if (!(Memory?.ByteCount > 0)) return;
                LineType = Sources.Peek() is TextSource ? '+' : ':';
            }
            else
                LineType = '-';


            return;

        }

        // are we saving lines for later?
        if (_savedLines != null)
        {
            _savedLines.AddLine(_line);
            return;
        }

        var source = (MacroSource?)Macros.GetValueOrDefault(CurrentToken?.Text ?? string.Empty);
        if (source != null)
        {
            var values = new List<string>();
            int start;

            // Skip any leading whitespace
            do
            {
                start = _offset;
                CurrentToken = NextToken();
            } while (CurrentToken == WhiteSpace);

            while (CurrentToken != EOL)
            {
                int end;
                do
                {
                    end = _offset;
                    if ((CurrentToken = NextRealToken()) == EOL) break;
                } while (CurrentToken != Comma);

                values.Add(new string(_text, start, end - start));
                start = _offset;
            }

            if (Label != null)
            {
                if (Origin != null)
                {
                    if (Label.Text[0] == '.')
                    {
                        if (_lastLabel != null)
                        {
                            SetLabel($"{_lastLabel}{Label.Text}", Origin);
                        }
                        else
                        {
                            OnError(ErrorMessage.ERR_NO_GLOBAL);
                        }
                    }
                    else
                    {
                        _lastLabel = Label.Text;
                        SetLabel(_lastLabel, Origin);

                    }
                }
                else
                {
                    OnError(ErrorMessage.ERR_NO_SECTION);
                }
            }

            Sources.Push(source.Invoke(++_instance, values));
            return;
        }

        if (Label != null)
        {
            if (Origin != null)
            {
                if (Label.Text[0] == '.')
                {
                    if (_lastLabel != null)
                        SetLabel($"{_lastLabel}{Label.Text}", Origin);
                    else
                    {
                        OnError(ErrorMessage.ERR_NO_GLOBAL);
                    }
                }
                else
                {
                    _lastLabel = Label.Text;
                    SetLabel(_lastLabel, Origin);
                }
            }
            else
            {
                OnError(ErrorMessage.ERR_NO_SECTION);
            }

            if (LineType == ' ') LineType = ':';
        }

        if (IsActive) OnError(ErrorMessage.ERR_UNKNOWN_OPCODE);
    }

    /// <summary>
    /// Determines if the <see cref="Assembler"/> supports the given pass.
    /// </summary>
    /// <returns>True if pass is supported, False otherwise</returns>
    protected abstract bool IsSupportedPass(Pass pass);

    /// <summary>
    /// Executes the assembly process for the given file.
    /// </summary>
    /// <param name="fileName">The name of the file to process.</param>
    /// <returns>True if the assembly succeeded with no errors</returns>
    private void Assemble(string fileName)
    {
        if (!Assemble(Pass.FIRST, fileName)) return;
        if (!Assemble(Pass.INTERMEDIATE, fileName)) return;
        if (!Assemble(Pass.INTERMEDIATE, fileName)) return;
        if (!Assemble(Pass.INTERMEDIATE, fileName)) return;
        if (!Assemble(Pass.FINAL, fileName)) return;

        // Add globally define symbols to the object module. 
        foreach (var name in Globals)
        {
            var expr = Symbols.GetValueOrDefault(name);

            if (expr != null)
                Module?.AddGlobal(name, expr);
            else
                OnError($"Undefined global symbol: {name}");
        }

        // Write the object module
        if (_errors == 0)
        {
            try
            {
                var objectName = GetObjectFile(fileName);

                Module?.SetName(new FileInfo(objectName).Name);

                using var stream = new StreamWriter(objectName);
                stream.WriteLine("<?xml version='1.0'?>" + Module);

            }
            catch (Exception)
            {
                Console.Error.WriteLine("Error: Could not write object module");
                Environment.Exit(1);
            }
        }

        // Dump symbol table
        if (_lineCount != 0)
        {
            ThrowPage = true;
            Paginate("");
        }

        Paginate("Symbol Table");
        Paginate("");

        // Sort by name
        var keys = Symbols.Keys.ToArray();
        Array.Sort(keys);

        // Sort by value
        var values = (string[])keys.Clone();
        Array.Sort(values, Comparer<string>.Create((arg0, arg1) =>
        {
            var lhs = Symbols[arg0]?.Resolve();
            var rhs = Symbols[arg1]?.Resolve();

            if (lhs == rhs)
            {
                return string.Compare(arg0, arg1, StringComparison.Ordinal);
            }


            return lhs?.CompareTo(rhs) ?? 0;
        }));

        for (var index = 0; index < keys.Length; ++index)
        {
            string lhs;
            string rhs;

            // Format name slice
            var name = keys[index];

            var expr = Symbols.GetValueOrDefault(name);
            var value = expr?.Resolve() ?? 0;

            name = (name + "                                ")[..32];

            if (expr?.IsAbsolute == true)
            {
                lhs = name + " " + Hex.ToHex(value, 8) + " ";
            }
            else
            {
                lhs = name + " " + Hex.ToHex(value, 8) + "'";
            }

            // Format value side
            name = values[index];
            expr = Symbols.GetValueOrDefault(name);
            value = expr?.Resolve() ?? 0;

            name = (name + "                                ")[..32];

            if (expr?.IsAbsolute == true)
            {
                rhs = name + " " + Hex.ToHex(value, 8) + " ";
            }
            else
            {
                rhs = name + " " + Hex.ToHex(value, 8) + "'";
            }

            Paginate(lhs + " | " + rhs);
        }

        _listFile?.Close();
        _listFile = null;
    }

    /**
     * Configures the source stack to read from the given file and initiates
     * the processing for the given pass.
     * 
     * @param pass     The assembler pass.
     * @param fileName The initial source filename.
     * @return <c>true</c> if no errors were found during the pass.
     */
    private bool Assemble(Pass pass, string fileName)
    {
        if (!IsSupportedPass(Pass = pass))
        {
            return true;
        }

        StartPass();

        Module?.Clear();

        _errors = 0;
        _lastLabel = null;

        _savedLines = null;
        _repeatDepth = 0;
        _macroDepth = 0;
        _instance = 0;

        SetSection(".code");

        try
        {
            if (pass == Pass.FINAL)
            {
                _listFile = new StreamWriter(File.Create(GetListingFile(fileName)), Encoding.GetEncoding("ISO-8859-1"));
            }

            Sources.Push(new FileSource(fileName, new FileStream(fileName, FileMode.Open)));
            Process();
        }
        catch (FileNotFoundException)
        {
            Console.Error.WriteLine("Source file not found: " + fileName);
            Environment.Exit(2);
        }
        catch (IOException)
        {
            Console.Error.WriteLine("Could not create listing file");
            Environment.Exit(2);
        }

        EndPass();

        return (_errors == 0);
    }

    /// <summary>
    /// This method is called at the end of each pass to allow final
    /// actions to take place.
    /// </summary>
    protected virtual void EndPass()
    {
    }

    /// <summary>
    /// Fetches the next source nextLine from the current source.
    /// </summary>
    /// <returns>The next source nextLine.</returns>
    private Line? GetNextLine()
    {
        return Sources.Count == 0 ? null : Sources.Peek().NextLine();
    }


    /// <summary>
    /// Print an error message.
    /// </summary>
    /// <param name="message">The _text for the error message.</param>
    public void OnError(string message)
    {
        var msg = $"Error: {_line?.FileName} ({_line?.LineNumber}) {message}";

        Console.Error.WriteLine(msg);
        if (Pass == Pass.FINAL)
            Paginate(msg);

        _errors++;
    }

    /// <summary>
    /// Print a warning message.
    /// </summary>
    /// <param name="message">The _text for the warning message.</param>
    public void OnWarning(string message)
    {
        var msg = $"Warning: {_line?.FileName} ({_line?.LineNumber}) {message}";

        Console.Error.WriteLine(msg);
        if (Pass == Pass.FINAL)
            Paginate(msg);
    }

    public abstract int DirectPage { get; set; }
    public abstract void AddAddress(Expr? expr);
    public abstract int IfIndex { get; set; }
    public abstract int LoopIndex { get; set; }
    public abstract Stack<int> Ifs { get; }
    public abstract Stack<int> Loops { get; }
    public abstract List<Value?> ElseAddr { get; }
    public abstract List<Value?> EndIfAddr { get; }
    public abstract List<Value?> LoopAddr { get; }
    public abstract List<Value?> EndAddr { get; }

    public abstract void GenerateJump(Expr? target);
    public abstract void GenerateDirectPage(int opcode, Expr? expr);
    public abstract void GenerateBranch(Token condition, Expr? target);
    public abstract void GenerateAbsolute(int opcode, Expr? expr);
    public abstract void GenerateIndirect(int opcode, Expr? expr, bool isLong);
    public abstract void GenerateRelative(int opcode, Expr? expr, bool isLong);
    public abstract void GenerateLong(int opcode, Expr? expr);

    /// <summary>
    /// Output a _line of _text to the listing, trimming trailing spaces.
    /// </summary>
    /// <param name="message">The _text to output.</param>
    private void Paginate(string message)
    {
        if (_listFile == null || !_listing) return;

        if (_lineCount == 0)
        {
            _listFile.WriteLine();
            _listFile.WriteLine(Title);
            _listFile.WriteLine();

            _lineCount += 3;
        }

        int len;
        for (len = message.Length; len > 0; len--)
        {
            if (message[len - 1] != ' ')
                break;
        }

        if (len > 0)
        {
            _listFile.WriteLine(message[..len]);
        }
        else
        {
            _listFile.WriteLine();
        }

        if (!ThrowPage && (++_lineCount != (LinesPerPage - 3))) return;
        _listFile.Write('\f');
        _lineCount = 0;
        ThrowPage = false;
    }

    /// <summary>
    /// Returns the current pass
    /// </summary>
    /// <returns>The current pass</returns>
    public Pass? Pass
    {
        get;
        set;
    }

    public Token? CurrentToken { get; set; }

    /// <summary>
    /// Derives the name of the listing file from the source file.
    /// </summary>
    /// <param name="filename">The source file name</param>
    /// <returns>The name of the list file</returns>
    private static string GetListingFile(string filename)
    {
        return Path.ChangeExtension(filename, "lst");
    }

    /// <summary>
    /// Derives the name of the object module from the source file.
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    private static string GetObjectFile(string filename)
    {
        return Path.ChangeExtension(filename, "obj");
    }


    /// <summary>
    /// Fetches the next <see cref="Token"/> skipping any pseudo whitespace.
    /// </summary>
    /// <returns>The next <see cref="Token"/> to be processed.</returns>
    public Token? NextRealToken()
    {
        var token = NextToken();

        while (token == WhiteSpace)
            token = NextToken();

        return (token);
    }

    public int Processor { get; set; }

    /// <summary>
    /// Fetches the next <see cref="Token"/> consuming any that have been
    /// pushed back first
    /// </summary>
    /// <returns>The next <see cref="Token"/> to be processed.</returns>
    private Token? NextToken()
    {
        return Tokens.Count != 0 ? Tokens.Pop() : ReadToken();
    }

    /**
	 * Gets and consumes the next character on the source _line.
	 * 
	 * @return	The next character on the _line.
	 */
    protected char NextChar()
    {
        var ch = PeekChar();

        if (ch != '\0') ++_offset;
        return (ch);
    }

    /**
	 * Gets the next character for the source _line without consuming it.
	 * 
	 * @return	The next character on the _line.
	 */
    protected char PeekChar()
    {
        return ((_offset < _text?.Length) ? _text[_offset] : '\0');
    }

    /// <summary>
    /// Returns the current section origin.
    /// </summary>
    /// <returns>The origin for the current _line.</returns>
    public Value? Origin
    {
        get;
        set;
    }

    /// <summary>
    /// Determines if a character is whitespace.
    /// </summary>
    /// <param name="ch">The character to be tested.</param>
    /// <returns><c>true</c> if the character is whitespace, <c>false</c> otherwise.</returns>
    protected static bool IsSpace(char ch)
    {
        return ch is ' ' or '\t';
    }

    /// <summary>
    /// Determines if a character is a binary digit.
    /// </summary>
    /// <param name="ch">The character to be tested.</param>
    /// <returns><c>true</c> if the character is a digit, <c>false</c> otherwise.</returns>
    protected static bool IsBinary(char ch)
    {
        return ch is '0' or '1';
    }

    /// <summary>
    /// Determines if a character is an octal digit.
    /// </summary>
    /// <param name="ch">The character to be tested.</param>
    /// <returns><c>true</c> if the character is a digit, <c>false</c> otherwise.</returns>
    protected static bool IsOctal(char ch)
    {
        return ch is >= '0' and <= '7';
    }

    /// <summary>
    /// Determines if a character is a decimal digit.
    /// </summary>
    /// <param name="ch">The character to be tested.</param>
    /// <returns><c>true</c> if the character is a digit, <c>false</c> otherwise.</returns>
    protected static bool IsDecimal(char ch)
    {
        return ch is >= '0' and <= '9';
    }

    /// <summary>
    /// Determines if a character is a hexadecimal digit.
    /// </summary>
    /// <param name="ch">The character to be tested.</param>
    /// <returns><c>true</c> if the character is a digit, <c>false</c> otherwise.</returns>
    protected static bool IsHexadecimal(char ch)
    {
        return ch is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f';
    }

    /// <summary>
    /// Determines if a character is a letter.
    /// </summary>
    /// <param name="ch">The character to be tested.</param>
    /// <returns><c>true</c> if the character is a letter, <c>false</c> otherwise.</returns>
    protected static bool IsAlpha(char ch)
    {
        return ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    /// <summary>
    /// Determines if a character is alphanumeric.
    /// </summary>
    /// <param name="ch">The character to be tested.</param>
    /// <returns><c>true</c> if the character is alphanumeric, <c>false</c> otherwise.</returns>
    protected static bool IsAlphanumeric(char ch)
    {
        return ch is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    /// <summary>
    /// Sets the value of a symbol to the given expression value.
    /// </summary>
    /// <param name="label">The symbol name.</param>
    /// <param name="value">The associated value.</param>
    public void DoSet(string label, Expr? value)
    {
        if (Symbols.ContainsKey(label))
        {
            if (Variable.Contains(label))
                Symbols[label] = value;
            else
                OnError("Symbol has already been defined.");
        }
        else
        {
            Symbols[label] = value;
            Variable.Add(label);
        }
    }

    public abstract int BitsA { get; set; }
    public abstract int BitsI { get; set; }

    /**
    * Allows a derived class to modify the active section name.
    * 
    * @param 	name			The name of the section to activate.
    */
    public void SetSection(string name)
    {
        Section = Sections[SectionName = name];
    }

    private static readonly Value MASK = new(null, 0xFF);
    private static readonly Value SIXTEEN = new(null, 16);
    private static readonly Value EIGHT = new(null, 8);
    private static readonly Value ZERO = new(null, 0);
    private static readonly Value ONE = new(null, 1);

    private static readonly StringBuilder Buffer = new();

    private readonly Option _defineOption = new("-define", "Define symbols", "(symbol|symbol=value)(,..)*");
    private readonly Option _includeOption = new("-include", "Define include path", "path[,path]*");


    /**
	 * Extracts a <see cref="Token"/> by processing characters from the
	 * source _line.
	 * 
	 * @return	The next <see cref="Token"/> extracted from the source.
	 */
    protected abstract Token? ReadToken();


    private string? _lastLabel;

    // the current _label (if any)
    //protected Token? label = null;


    /**
	 * The type of _line we are compiling (for the listing).
	 */
    protected char? LineType;


    protected MemoryModel? Memory;

    // Tab expansion size.
    private const int TabSize = 8;

    // Flag determining listing on/off state
    private bool _listing;

    // The current output _line count
    private int _lineCount;

    // The number of lines on a page (A4 = 60)
    private const int LinesPerPage = 60;

    // Writer assigned to listing file in final pass.
    private StreamWriter? _listFile;

    // The current _line being assembled.
    private Line? _line;

    // The characters comprising the _line being assembled.
    private char[]? _text;

    // The offset of the next character in the current _line.
    private int _offset;

    // The number of errors seen during the current pass.
    private int _errors;

    // The number of warnings seen during the current pass.

    // The TextSource used to capture macro or repeat lines.
    private TextSource? _savedLines;

    // Count macro definition depth.
    private int _macroDepth;

    // Counts repeat section depth.
    private int _repeatDepth;

    // Macro instance counter.
    private int _instance;

    /// <summary>
    /// Locates a file with the given name, optionally searching the include path for it.
    /// </summary>
    /// <param name="filename">The required filename.</param>
    /// <param name="search">The search indicator.</param>
    /// <returns>A <see cref="FileStream"/> attached to the file or <c>null</c>.</returns>
    public FileStream? FindFile(string filename, bool search)
    {
        try
        {
            return new FileStream(filename, FileMode.Open);
        }
        catch (FileNotFoundException)
        {
            if (search && _includeOption.Value != null)
            {
                var paths = _includeOption.Value.Split(',');

                foreach (var path in paths)
                {
                    try
                    {
                        return new FileStream(Path.Combine(path, filename), FileMode.Open);
                    }
                    catch (Exception)
                    {
                        // Ignore and continue searching
                    }
                }
            }

            OnError("Could not find the specified file");
        }

        return null;
    }

    public Stack<ISource> Sources { get; } = new();

    public Expr? Addr { get; set; }

    public Token? Label { get; set; }
    public HashSet<string> Variable { get; } = new();

    public Dictionary<string, Expr?> Symbols { get; } = new();

    public HashSet<string> NotLocal { get; } = new();
    public HashSet<string> Externals { get; } = new();
    private Stack<Token> Tokens { get; } = new();

}
