using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using Dev65.XApp;
using Dev65.XObj;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Dev65.XObj.BinaryExpr;
using Application = Dev65.XApp.Application;
using Module = Dev65.XObj.Module;
using Section = Dev65.XObj.Section;

namespace Dev65.XAsm;


public abstract class Assembler : Application, IAssembler
{
    protected static readonly TokenKind Operator = new("OPERATOR");
    protected static readonly TokenKind Symbol = new("SYMBOL");
    protected static readonly TokenKind Keyword = new("KEYWORD");
    protected static readonly TokenKind Number = new("NUMBER");
    protected static readonly TokenKind String = new("STRING");
    protected static readonly TokenKind Unknown = new("UNKNOWN");

    protected Dictionary<string, Expr?> symbols = new();

    protected static readonly Token WhiteSpace = new(Unknown, "#SPACE");

    protected static readonly Opcode<Assembler> EOL = new(Unknown, "#EOL", _ => true);

    /// <summary>
    /// A <see cref="Token"/> representing the origin (e.g. $ or @).
    /// </summary>
    protected static readonly Token Origin = new(Keyword, "ORIGIN");

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
    protected readonly Token Divide = new(Operator, "/");

    // Token representing modulo.
    protected readonly Token Modulo = new(Operator, "%");

    // Token representing complement.
    protected readonly Token Complement = new(Operator, "~");

    // Token representing binary and.
    protected readonly Token BinaryAnd = new(Operator, "&");

    // Token representing binary or.
    protected readonly Token BinaryOr = new(Operator, "|");

    // Token representing binary xor.
    protected readonly Token BinaryXor = new(Operator, "^");

    // Token representing logical not.
    protected readonly Token LogicalNot = new(Operator, "!");

    // Token representing logical and.
    protected readonly Token LogicalAnd = new(Operator, "&&");

    // Token representing logical or.
    protected readonly Token LogicalOr = new(Operator, "||");

    // Token representing equal.
    protected readonly Token EQ = new(Operator, "=");

    // Token representing not equal.
    protected readonly Token NE = new(Operator, "!=");

    // Token representing less than.
    protected readonly Token Lt = new(Operator, "<");

    // Token representing less than or equal.
    protected readonly Token Le = new(Operator, "<=");

    // Token representing greater than.
    protected readonly Token Gt = new(Operator, ">");

    // Token representing greater than or equal.
    protected readonly Token Ge = new(Operator, ">=");

    // Token representing a left shift.
    protected readonly Token LShift = new(Operator, "<<");

    // Token representing a right shift.
    protected readonly Token RShift = new(Operator, ">>");

    // Token representing an opening parenthesis.
    protected readonly Token LParen = new(Operator, "(");

    // Token representing a closing parenthesis.
    protected readonly Token RParen = new(Operator, ")");

    // Token representing the LO function.
    protected readonly Token LO = new(Keyword, "LO");

    // Token representing the HI function.
    protected readonly Token HI = new(Keyword, "HI");

    // Token representing the STRLEN function.
    protected readonly Token STRLEN = new(Keyword, "STRLEN");

    // Token representing the BANK function.
    protected readonly Token BANK = new(Keyword, "BANK");

    // Opcode that handles .INCLUDE directives.
    protected readonly Token INCLUDE = new Opcode<Assembler>(Keyword, ".INCLUDE",
        (assembler) =>
        {
            assembler.currentToken = assembler.NextRealToken();
            if (assembler.currentToken?.Kind == String)
            {
                var filename = assembler.currentToken.Text;
                var stream = assembler.FindFile(filename, true);

                if (stream != null)
                {
                    assembler.sources.Push(new FileSource(filename, stream));
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
    protected readonly Token APPEND = new Opcode<Assembler>(Keyword, ".APPEND", (assembler) => 
    {
            if (assembler.currentToken?.Kind == String)
            {
                var filename = assembler.currentToken.Text;
                var stream = assembler.FindFile(filename, false);

                if (stream != null)
                {
                    assembler.sources.Pop();
                    assembler.sources.Push(new FileSource(filename, stream));
                }
                else
                    assembler.OnError($"{ErrorMessage.ERR_FAILED_TO_FIND_FILE} ({filename})");
            }
            else
                assembler.OnError(ErrorMessage.ERR_EXPECTED_QUOTED_FILENAME);

            return (false);
    });

    protected readonly Token INSERT = new Opcode<Assembler>(Keyword, ".INSERT", assembler =>
    {
        if (assembler.currentToken?.Kind == String)
        {
            var filename = assembler.currentToken?.Text;
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

    protected readonly Opcode<Assembler> End = new(Keyword, ".END", assembler => {
        assembler.sources.Clear();
        return false;
    });

    protected readonly Opcode<Assembler> Equ = new(Keyword, ".EQU", assembler =>
    {
        assembler.currentToken = assembler.NextRealToken();
        assembler.addr = assembler.ParseExpression();

        if (assembler.label != null)
        {
            if (assembler.pass == Pass.FIRST)
            {
                if (assembler.variable.Contains(assembler.label.Text))
                {
                    assembler.OnError("Symbol has already been defined with .SET");
                    return (false);
                }
                if (assembler.symbols.ContainsKey(assembler.label.Text))
                {
                    assembler.OnError(ErrorMessage.ERR_LABEL_REDEFINED);
                    return (false);
                }

                if (assembler.label.Text[0] == '.')
                    assembler.notLocal.Add(assembler.label.Text);
            }

            assembler.symbols.Add(assembler.label.Text, assembler.addr);
        }
        else
            assembler.OnError("No symbol name defined for .EQU");

        return (false);
    });

    protected readonly Opcode<Assembler> Set = new(Keyword, ".SET", assembler =>
    {
        assembler.currentToken = assembler.NextRealToken();
        assembler.addr = assembler.ParseExpression();

        if (assembler.label != null)
        {
            if (assembler.pass == Pass.FIRST)
            {
                if (assembler.symbols.ContainsKey(assembler.label.Text) && !assembler.variable.Contains(assembler.label.Text))
                {
                    assembler.OnError("Symbol has already been defined with .EQU");
                    return (false);
                }

                if (assembler.label.Text[0] == '.')
                    assembler.notLocal.Add(assembler.label.Text);

                assembler.variable.Add(assembler.label.Text);
            }

            assembler.symbols.Add(assembler.label.Text, assembler.addr);
        }
        else
            assembler.OnError("No symbol name defined for .SET");

        return (false);
    });

    protected readonly Opcode<Assembler> Space = new(Keyword, ".SPACE", assembler =>
    {
        assembler.currentToken = assembler.NextRealToken();
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

    protected readonly Opcode<Assembler> Align = new(Keyword, ".ALIGN", assembler =>
    {
        assembler.currentToken = assembler.NextRealToken();
        var expr = assembler.ParseExpression();

        if (expr?.IsAbsolute == true)
        {
            var value = expr.Resolve();
            var count = assembler.origin?.Resolve() % value;

            while ((count > 0) && (count++ != value))
                assembler?.AddByte(0);
        }
        else
            assembler.OnError(ErrorMessage.ERR_CONSTANT_EXPR);

        return (true);
    });

    protected readonly Opcode<Assembler> Dcb = new(Keyword, ".DCB", assembler =>
    {
        assembler.currentToken = assembler.NextRealToken();
        var expr = assembler.ParseExpression();

        if (expr?.IsAbsolute == true)
        {
            var value = expr.Resolve();

            if (assembler.currentToken == Comma)
            {
                assembler.currentToken = assembler.NextRealToken();
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

    protected readonly Opcode<Assembler> Byte = new(Keyword, ".BYTE", assembler =>
    {
        do
        {
            assembler.currentToken = assembler.NextRealToken();
            if (assembler.currentToken?.Kind == String)
            {
                var value = assembler.currentToken.Text;

                foreach (var t in value)
                    assembler.AddByte((byte)t);

                assembler.currentToken = assembler.NextRealToken();
            }
            else
            {
                var expr = assembler.ParseExpression();

                if (expr != null)
                    assembler.AddByte(expr);
                else
                    assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
            }
        } while (assembler.currentToken == Comma);

        if (assembler.currentToken != EOL) assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        return (true);
    });

    protected readonly Opcode<Assembler> DByte = new(Keyword, ".DBYTE", assembler =>
    {
        do
        {
            assembler.currentToken = assembler.NextRealToken();
            var expr = assembler.ParseExpression();

            if (expr != null)
            {
                assembler.AddByte(Expr.And(Expr.Shr(expr, EIGHT), MASK));
                assembler.AddByte(Expr.And(expr, MASK));
            }
            else
                assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        } while (assembler.currentToken == Comma);

        if (assembler.currentToken != EOL) assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        return (true);
    });

    protected readonly Opcode<Assembler> Word = new(Keyword, ".WORD", assembler =>
    {
        do
        {
            assembler.currentToken = assembler.NextRealToken();
            var expr = assembler.ParseExpression();

            if (expr != null)
                assembler.AddWord(expr);
            else
                assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        } while (assembler.currentToken == Comma);

        if (assembler.currentToken != EOL) assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        return (true);
    });

    protected readonly Opcode<Assembler> LONG = new(Keyword, ".LONG", assembler =>
    {
        do
        {
            assembler.currentToken = assembler.NextRealToken();
            var expr = assembler.ParseExpression();

            if (expr != null)
                assembler.AddLong(expr);
            else
                assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        } while (assembler.currentToken == Comma);

        if (assembler.currentToken != EOL) assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        return (true);
    });

    protected readonly Opcode<Assembler> IF = new(Keyword, ".IF", assembler =>
    {
        if (assembler.IsActive)
        {
            assembler.currentToken = assembler.NextRealToken();

            var expr = assembler.ParseExpression();

            if (expr == null)
            {
                assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
                return (false);
            }

            if (expr.IsAbsolute)
            {
                var state = expr.Resolve() != 0;
                assembler.status.Push((assembler.IsActive && state));
            }
            else
                assembler.OnError(ErrorMessage.ERR_CONSTANT_EXPR);
        }
        else
            assembler.status.Push(false);

        return (false);
    }, true);

    protected readonly Opcode<Assembler> IFABS = new(Keyword, ".IFABS", assembler =>
    {
        if (assembler.IsActive)
        {
            assembler.currentToken = assembler.NextRealToken();

            var expr = assembler.ParseExpression();

            if (expr == null)
            {
                assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
                return (false);
            }

            assembler.status.Push(expr.IsAbsolute);
        }
        else
            assembler.status.Push(false);

        return (false);
    }, true);

    protected readonly Opcode<Assembler> IFNABS = new(Keyword, ".IFNABS", assembler =>
    {
        if (assembler.IsActive)
        {
            assembler.currentToken = assembler.NextRealToken();

            var expr = assembler.ParseExpression();

            if (expr == null)
            {
                assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
                return (false);
            }

            assembler.status.Push(!expr.IsAbsolute);
        }
        else
            assembler.status.Push(false);

        return (false);
    }, true);

    protected readonly Opcode<Assembler> IFREL = new(Keyword, ".IFREL", assembler =>
    {
        if (assembler.IsActive)
        {
            assembler.currentToken = assembler.NextRealToken();

            var expr = assembler.ParseExpression();

            if (expr == null)
            {
                assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
                return (false);
            }

            assembler.status.Push(expr.IsRelative);
        }
        else
            assembler.status.Push(false);

        return (false);
    }, true);

    protected readonly Opcode<Assembler> IFNREL = new(Keyword, ".IFNREL", assembler =>
    {
        if (assembler.IsActive)
        {
            assembler.currentToken = assembler.NextRealToken();

            var expr = assembler.ParseExpression();

            if (expr == null)
            {
                assembler.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
                return (false);
            }

            assembler.status.Push(!expr.IsRelative);
        }
        else
            assembler.status.Push(false);

        return (false);
    }, true);
    
    protected readonly Opcode<Assembler> IFDEF = new (Keyword, ".IFDEF", assembler =>
    {
        if (assembler.IsActive)
        {
            assembler.currentToken = assembler.NextRealToken();
            if (assembler.currentToken?.Kind != Symbol)
            {
                assembler.OnError("Expected a symbol");
                return false;
            }

            assembler.status.Push(assembler.symbols.ContainsKey(assembler.currentToken.Text));
        }
        else
            assembler.status.Push(false);

        return false;

    }, true);


    protected readonly Opcode<Assembler> IFNDEF = new(Keyword, ".IFNDEF", assembler =>
    {
        if (assembler.IsActive)
        {
            assembler.currentToken = assembler.NextRealToken();
            if (assembler.currentToken?.Kind != Symbol)
            {
                assembler.OnError("Expected a symbol");
                return false;
            }

            assembler.status.Push(!assembler.symbols.ContainsKey(assembler.currentToken.Text));
        }
        else
            assembler.status.Push(false);

        return false;

    }, true);


    protected readonly Opcode<Assembler> ELSE = new(Keyword, ".ELSE", assembler =>
    {
        if (assembler.status.Count != 0)
        {
            var state = assembler.status.Pop();
            assembler.status.Push((assembler.IsActive && !state) ? true : false);
        }
        else
            assembler.OnError(ErrorMessage.ERR_NO_OPEN_IF);

        return (false);
    }, true);

    protected readonly Opcode<Assembler> ENDIF = new(Keyword, ".ENDIF", assembler =>
    {
        if (assembler.status.Count != 0)
            assembler.status.Pop();
        else
            assembler.OnError(ErrorMessage.ERR_NO_OPEN_IF);

        return (false);
    }, true);


    protected readonly Opcode<Assembler> ERROR = new(Keyword, ".ERROR", assembler =>
    {
        if (assembler.IsActive)
        {
            assembler.currentToken = assembler.NextRealToken();
            if (assembler.currentToken?.Kind == String)
            {
                assembler.OnError(assembler.currentToken.Text);
            }
            else
                assembler.OnError(ErrorMessage.ERR_EXPECTED_QUOTED_MESSAGE);
        }

        return (false);
    }, true);


    protected readonly Opcode<Assembler> WARN = new(Keyword, ".WARN", assembler =>
    {
        if (!assembler.IsActive) return (false);

        assembler.currentToken = assembler.NextRealToken();
        if (assembler.currentToken?.Kind == String)
        {
            assembler.OnWarning(assembler.currentToken.Text);
        }
        else
            assembler.OnError(ErrorMessage.ERR_EXPECTED_QUOTED_MESSAGE);

        return (false);
    }, true);

    protected readonly Token MACRO = new Opcode<Assembler>(Keyword, ".MACRO", assembler =>
    {
        if ((assembler.label != null) && ((assembler.macroName = assembler.label.Text) != null))
        {
            var arguments = new List<string>();

            for (; ; )
            {
                if ((assembler.currentToken = assembler.NextRealToken()) == EOL) break;

                if ((assembler.currentToken?.Kind == Symbol) || (assembler.currentToken?.Kind == Keyword))
                    arguments.Add(assembler.currentToken.Text);
                else
                {
                    assembler.OnError("Illegal macro argument");
                    break;
                }

                if ((assembler.currentToken = assembler.NextRealToken()) == EOL) break;

                if (assembler.currentToken == Comma) continue;

                assembler.OnError("Unexpected currentToken after macro argument");
                break;
            }

            assembler.savedLines = new MacroSource(arguments);
        }
        else
            assembler.OnError("No macro name has been specified");

        return (false);

    });

    protected readonly Token ENDM = new Opcode<Assembler>(Keyword, ".ENDM", assembler =>
    {
        if (assembler.savedLines != null)
        {
            assembler.macros.Add(assembler.macroName ?? string.Empty, assembler.savedLines);
            assembler.savedLines = null;
        }
        else
            assembler.OnError(".ENDM without a preceding .MACRO");

        return (false);
    });

    protected readonly Token EXITM = new Opcode<Assembler>(Keyword, ".EXITM", assembler =>
    {
        while (assembler.sources.Peek() is MacroSource)
            assembler.sources.Pop();
			
        return (false);
    });

    protected readonly Token REPEAT = new Opcode<Assembler>(Keyword, ".REPEAT", assembler =>
    {
        assembler.currentToken = assembler.NextRealToken();
        var expr = assembler.ParseExpression();

        if (expr?.IsAbsolute == true)
        {
            assembler.savedLines = new RepeatSource((int)expr.Resolve());
        }
        else
            assembler.OnError(ErrorMessage.ERR_CONSTANT_EXPR);

        return (false);
    });

    protected readonly Token ENDR = new Opcode<Assembler>(Keyword, ".ENDR", assembler =>
    {
        if (assembler.savedLines != null)
        {
            assembler.sources.Push(assembler.savedLines);
            assembler.savedLines = null;
        }
        else
            assembler.OnError(".ENDR without preceding .REPEAT");

        return (false);
    });

    protected readonly Opcode<Assembler> CODE = new Opcode<Assembler>(Keyword, ".CODE", assembler =>
    {
        assembler.SetSection(".code");
        return (false);
    });

    protected readonly Opcode<Assembler> DATA = new Opcode<Assembler>(Keyword, ".DATA", assembler =>
    {
        assembler.SetSection(".data");
        return (false);
    });

    protected readonly Opcode<Assembler> BSS = new Opcode<Assembler>(Keyword, ".BSS", assembler =>
    {
        assembler.SetSection(".bss");
        return (false);
    });

    protected readonly Opcode<Assembler> ORG = new Opcode<Assembler>(Keyword, ".ORG", assembler =>
    {
        assembler.currentToken = assembler.NextRealToken();
        Expr? expr = assembler.ParseExpression();

        if (expr is { IsAbsolute: true })
        {
            assembler.sections.Add(assembler.sectionName ?? "", assembler.section = assembler.section?.SetOrigin(expr.Resolve()));
        }
        else
            assembler.OnError(ErrorMessage.ERR_CONSTANT_EXPR);
        return (true);
    });

    protected readonly Opcode<Assembler> EXTERN = new Opcode<Assembler>(Keyword, ".EXTERN", assembler =>
    {
        if (assembler.pass == Pass.FIRST)
        {
            do
            {
                assembler.currentToken = assembler.NextRealToken();
                if (assembler.currentToken?.Kind != Symbol)
                {
                    assembler.OnError("Expected a list of symbols");
                    return (false);
                }

                var name = assembler.currentToken.Text;
                assembler.externs.Add(name);
                if (!assembler.symbols.ContainsKey(name))
                    assembler.symbols.Add(name, new Extern(name));
                assembler.currentToken = assembler.NextRealToken();
            } while (assembler.currentToken == Comma);
        }

        return (false);
    });

    protected readonly Opcode<Assembler> GLOBAL = new Opcode<Assembler>(Keyword, ".GLOBAL", assembler =>
    {
        if (assembler.pass == Pass.FIRST)
        {
            do
            {
                assembler.currentToken = assembler.NextRealToken();
                if (assembler.currentToken?.Kind != Symbol)
                {
                    assembler.OnError("Expected a list of symbols");
                    return (false);
                }

                var name = assembler.currentToken.Text;
                assembler.globals.Add(name);

                assembler.currentToken = assembler.NextRealToken();
            } while (assembler.currentToken == Comma);
        }

        return (false);
    });

    protected readonly Opcode<Assembler> LIST = new Opcode<Assembler>(Keyword, ".LIST", assembler =>
    {
        assembler.listing = true;
        return (false);
    });

    protected readonly Opcode<Assembler> NOLIST = new Opcode<Assembler>(Keyword, ".NOLIST", assembler => 
    {
            assembler.listing = false;
            return (false);
    });

    protected readonly Opcode<Assembler> PAGE = new Opcode<Assembler>(Keyword, ".PAGE", assembler => 
    {
            assembler.throwPage = true;
            return (false);
    });

    protected Opcode<Assembler> TITLE = new Opcode<Assembler>(Keyword, ".TITLE", assembler => 
    {
            assembler.currentToken = assembler.NextRealToken();

            assembler.title = assembler.currentToken?.Text;
            return (false);
    });


    /**
 * Adds a byte value to the output memory area.
 * 
 * @param	expr		The expression defining the value.
 */
    public void AddByte(Expr? expr)
    {
        memory?.AddByte(module, section, expr);
    }

    private void SetLabel(string name, Value value)
    {
        if ((pass == Pass.FIRST) && symbols.ContainsKey(name))
        {
            OnError($"{ErrorMessage.ERR_LABEL_REDEFINED}{name}");
        }
        else
        {
            symbols.Add(name, value);
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
        memory?.AddByte(module, section, value);
    }

    protected void AddWord(Expr? expr)
    {
        memory?.AddWord(module, section, expr);
    }

    protected void AddWord(long value)
    {
        memory?.AddWord(module, section, value);
    }

    protected void AddLong(Expr? expr)
    {
        memory?.AddLong(module, section, expr);
    }

    protected void AddLong(long value)
    {
        memory?.AddLong(module, section, value);
    }

    /**
	 * Determines if source lines are to be translated or skipped over
	 * depending on the current conditional compilation state.
	 * 
	 * @return <CODE>true</CODE> if source lines should be processed.
	 */
    protected bool IsActive => status.Count == 0 || status.Peek();
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



    private Expr? ParseLogical()
    {
        var expr = ParseBinary();

        while ((currentToken == LogicalAnd) || (currentToken == LogicalOr))
        {
            if (currentToken == LogicalAnd)
            {
                currentToken = NextRealToken();
                expr = Expr.LogicalAnd(expr, ParseBinary());
            }
            else
            {
                currentToken = NextRealToken();
                expr = Expr.LogicalOr(expr, ParseBinary());
            }
        }
        return (expr);
    }

    private Expr? ParseBinary()
    {
        var expr = ParseEquality();

        while ((currentToken == BinaryAnd) || (currentToken == BinaryOr) || (currentToken == BinaryXor))
        {
            if (currentToken == BinaryAnd)
            {
                currentToken = NextRealToken();
                expr = Expr.And(expr, ParseEquality());
            }
            else if (currentToken == BinaryOr)
            {
                currentToken = NextRealToken();
                expr = Expr.Or(expr, ParseEquality());
            }
            else
            {
                currentToken = NextRealToken();
                expr = Expr.Xor(expr, ParseEquality());
            }
        }
        return (expr);
    }

    private Expr? ParseEquality()
    {
        Expr? expr = ParseInequality();

        while ((currentToken == EQ) || (currentToken == NE))
        {
            if (currentToken == EQ)
            {
                currentToken = NextRealToken();
                expr = Expr.Eq(expr, ParseInequality());
            }
            else
            {
                currentToken = NextRealToken();
                expr = Expr.Ne(expr, ParseInequality());
            }
        }
        return (expr);
    }

    private Expr? ParseInequality()
    {
        Expr? expr = ParseShift();

        while ((currentToken == Lt) || (currentToken == Le) || (currentToken == Gt) || (currentToken == Ge))
        {
            if (currentToken == Lt)
            {
                currentToken = NextRealToken();
                expr = Expr.Lt(expr, ParseShift());
            }
            else if (currentToken == Le)
            {
                currentToken = NextRealToken();
                expr = Expr.Le(expr, ParseShift());
            }
            else if (currentToken == Gt)
            {
                currentToken = NextRealToken();
                expr = Expr.Gt(expr, ParseShift());
            }
            else
            {
                currentToken = NextRealToken();
                expr = Expr.Ge(expr, ParseShift());
            }
        }
        return (expr);
    }

    private Expr? ParseShift()
    {
        Expr? expr = ParseAddSub();

        while ((currentToken == RShift) || (currentToken == LShift))
        {
            if (currentToken == RShift)
            {
                currentToken = NextRealToken();
                expr = Expr.Shr(expr, ParseAddSub());
            }
            else
            {
                currentToken = NextRealToken();
                expr = Expr.Shl(expr, ParseAddSub());
            }
        }
        return (expr);
    }

    private Expr? ParseAddSub()
    {
        Expr? expr = ParseMulDiv();

        while ((currentToken == Plus) || (currentToken == Minus))
        {
            if (currentToken == Plus)
            {
                currentToken = NextRealToken();
                expr = Expr.Add(expr, ParseMulDiv());
            }
            else
            {
                currentToken = NextRealToken();
                expr = Expr.Sub(expr, ParseMulDiv());
            }
        }
        return (expr);
    }

    private Expr? ParseMulDiv()
    {
        Expr? expr = ParseUnary();

        while ((currentToken == Times) || (currentToken == Divide) || (currentToken == Modulo))
        {
            if (currentToken == Times)
            {
                currentToken = NextRealToken();
                expr = Expr.Mul(expr, ParseUnary());
            }
            else if (currentToken == Divide)
            {
                currentToken = NextRealToken();
                expr = Expr.Div(expr, ParseUnary());
            }
            else
            {
                currentToken = NextRealToken();
                expr = Expr.Mod(expr, ParseUnary());
            }
        }
        return (expr);
    }

    private Expr? ParseUnary()
    {
        if (currentToken == Minus)
        {
            currentToken = NextRealToken();
            return (Expr.Neg(ParseUnary()));
        }

        if (currentToken == Plus)
        {
            currentToken = NextRealToken();
            return (ParseUnary());
        }

        if (currentToken == Complement)
        {
            currentToken = NextRealToken();
            return (Expr.Cpl(ParseUnary()));
        }
        if (currentToken == LogicalNot)
        {
            currentToken = NextRealToken();
            return (Expr.LogicalNot(ParseUnary()));
        }
        if (currentToken == LO)
        {
            currentToken = NextRealToken();
            return (Expr.And(ParseUnary(), MASK));
        }
        if (currentToken == HI)
        {
            currentToken = NextRealToken();
            return (Expr.And(Expr.Shr(ParseUnary(), EIGHT), MASK));
        }
        if (currentToken == BANK)
        {
            currentToken = NextRealToken();
            return (Expr.Shr(ParseUnary(), SIXTEEN));
        }

        if (currentToken != Strlen) return (ParseValue());

        currentToken = NextRealToken();
        if (currentToken != LParen)
        {
            OnError("Expected open parenthesis");
            return (null);
        }

        currentToken = NextRealToken();
        if ((currentToken == null) || (currentToken.Kind != String))
        {
            OnError("Expected string value in STRLEN");
            return (null);
        }

        var value = new Value(null, currentToken.Text.Length);

        currentToken = NextRealToken();
        if (currentToken != RParen)
        {
            OnError("Expected close parenthesis");
            return (null);
        }
        currentToken = NextRealToken();

        return (value);

    }

    /// <summary>
    /// Parse part of an expression that should result in a value.
    /// </summary>
    /// <returns>A compiled expression.</returns>
    private Expr? ParseValue()
    {
        Expr? expr = null;

        if (currentToken == Origin || currentToken == Times)
        {
            expr = origin;
            currentToken = NextRealToken();
        }
        else if (currentToken == LParen)
        {
            currentToken = NextRealToken();
            expr = ParseExpression();
            if (currentToken != RParen)
                OnError(ErrorMessage.ERR_CLOSING_PAREN);
            else
                currentToken = NextRealToken();
        }
        else if (currentToken?.Kind == Number)
        {
            expr = new Value(null, ((int)((currentToken?.Value) ?? 0)));
            currentToken = NextRealToken();
        }
        else if (currentToken?.Kind == Symbol || currentToken?.Kind == Keyword)
        {
            if (currentToken.Text[0] == '.' && !notLocal.Contains(currentToken.Text))
            {
                if (lastLabel != null)
                    expr = symbols[lastLabel + currentToken.Text];
                else
                    OnError(ErrorMessage.ERR_NO_GLOBAL);
            }
            else
                expr = symbols[currentToken.Text];

            if (expr == null)
            {
                if (pass == Pass.FINAL)
                    OnError(ErrorMessage.ERR_UNDEF_SYMBOL + currentToken.Text);
                expr = ZERO;
            }

            currentToken = NextRealToken();
        }

        return expr;
    }

    /// <summary>
    /// Constructs an <see cref="Assembler"/> that adds code to the given module.
    /// </summary>
    /// <param name="module">The object module</param>
    protected Assembler(Module module)
    {
        this.module = module;
    }

    /// <summary>
    /// Set the <see cref="MemoryModel"/> instance that describes the target
    /// </summary>
    /// <param name="memoryModel">The <see cref="MemoryModel"/> instance</param>
    protected void SetMemoryModel(MemoryModel memoryModel)
    {
        memory = memoryModel;

        memoryModel.AssemblerError += (sender, args) => OnError(args.Message);
        memoryModel.AssemblerWarning += (sender, args) => OnWarning(args.Message);
    }

    protected override void StartUp()
    {
        base.StartUp();

        if (defineOption.IsPresent)
        {
            var defines = defineOption.Value?.Split(",");

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
        if (errors > 0) System.Environment.Exit(-1);
    }

    /// <summary>
    /// This method is called at the start of each pass to allow variables
    /// to be initialized
    /// </summary>
    protected virtual void StartPass()
    {
        listing = true;
        title = "";
        lineCount = 0;
        throwPage = false;

        sections.Clear();
        sections.Add(".code", module?.FindSection(".code"));
        sections.Add(".data", module?.FindSection(".data"));
        sections.Add(".bss", module?.FindSection(".bss"));
    }

    protected void Process()
    {
        while ((sources.Count != 0))
        {
            Line? nextLine;
            if ((nextLine = GetNextLine()) == null)
            {
                sources.Pop();
                continue;
            }

            Process(nextLine);

            if (pass == Pass.FINAL)
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

    protected string ExpandText()
    {
        Buffer.Clear();
        for (int index = 0; index < text?.Length; ++index)
        {
            if (text[index] == '\t')
            {
                do
                {
                    Buffer.Append(" ");
                } while (Buffer.Length % tabSize != 0);
            }
            else
                Buffer.Append(text[index]);
        }
        return (Buffer.ToString());
    }

    /// <summary>
    /// Initialises the tokeniser to process the given nextLine. This method is
    /// overloaded in derived classes.
    /// </summary>
    /// <param name="nextLine">The next <see cref="Line"/> to be processed.</param>
    protected virtual void Process(Line nextLine)
    {
        if (sources.Peek() is TextSource)
            lineType = '+';
        else
            lineType = ' ';

        memory?.Clear();

        label = null;
        line = nextLine;
        text = line.Text.ToCharArray();
        offset = 0;

        addr = origin = section?.GetOrigin();

        if ((currentToken = NextToken()) == EOL) return;

        // Extract and save the labels
        if (currentToken != WhiteSpace)
        {
            label = currentToken;
            if (label?.Kind != Symbol)
            {
                if (pass == Pass.FIRST)
                {
                    OnWarning($"{ErrorMessage.WRN_LABEL_IS_A_RESERVED_WORD} ({label?.Text})");
                }
            }

            if ((currentToken = NextToken()) == Colon)
            {
                currentToken = NextToken();
            }
        }

        if (currentToken == WhiteSpace) currentToken = NextRealToken();

        // Map = to .SET when used as an opcode
        if (currentToken == EQ)
            currentToken = Set;

        if (currentToken is Opcode<Assembler> opcode)
        {
            if (opcode.IsAlwaysActive || IsActive)
            {
                if (savedLines != null)
                {
                    if (savedLines is RepeatSource)
                    {
                        if (opcode == ENDR)
                        {
                            if (--repeatDepth == 0)
                            {
                                opcode.Compile(this);
                                return;
                            }
                        }

                        if (opcode == REPEAT)
                            repeatDepth++;
                    }

                    if (savedLines is MacroSource)
                    {
                        if (opcode == ENDM)
                        {
                            if (--macroDepth == 0)
                            {
                                opcode.Compile(this);
                                return;
                            }
                        }

                        if (opcode == MACRO)
                            macroDepth++;
                    }

                    savedLines.AddLine(line);
                    return;
                }

                if (opcode == MACRO)
                {
                    if (macroDepth++ == 0)
                    {
                        opcode.Compile(this);
                        lineType = ' ';
                        return;
                    }
                }

                if (opcode == REPEAT)
                {
                    if (repeatDepth++ == 0)
                    {
                        opcode.Compile(this);

                        if (label != null)
                        {
                            if (origin != null)
                            {
                                if (label.Text[0] == '.')
                                {
                                    if (lastLabel != null)
                                        SetLabel(lastLabel + label.Text, origin);
                                }
                            }
                            else
                            {
                                OnError(ErrorMessage.ERR_NO_SECTION);
                            }

                            if (lineType == ' ') lineType = ':';
                        }

                        return;
                    }
                }

                if (opcode == Equ || opcode == Set)
                {
                    opcode.Compile(this);
                    lineType = '=';
                    return;
                }

                if (label != null)
                {
                    if (origin != null)
                    {
                        if (label.Text[0] == '.')
                        {
                            if (lastLabel != null)
                            {
                                SetLabel($"{lastLabel}{label.Text}", origin);
                            }
                            else
                            {
                                OnError(ErrorMessage.ERR_NO_GLOBAL);
                            }
                        }
                        else
                        {
                            lastLabel = label.Text;
                            SetLabel(lastLabel, origin);
                        }
                    }
                    else
                    {
                        OnError(ErrorMessage.ERR_NO_SECTION);
                    }
                }

                if (opcode.Compile(this))
                {
                    if (memory?.ByteCount > 0)
                    {
                        if (sources.Peek() is TextSource)
                            lineType = '+';
                        else
                            lineType = ':';
                    }
                }
            }
            else
                lineType = '-';


            return;

        }

        // are we saving lines for later?
        if (savedLines != null)
        {
            savedLines.AddLine(line);
            return;
        }

        var source = (MacroSource?)macros.GetValueOrDefault(currentToken?.Text ?? string.Empty);
        if (source != null)
        {
            var values = new List<string>();
            int start;
            int end;

            // Skip any leading whitespace
            do
            {
                start = offset;
                currentToken = NextToken();
            } while (currentToken == WhiteSpace);

            while (currentToken != EOL)
            {
                do
                {
                    end = offset;
                    if ((currentToken = NextRealToken()) == EOL) break;
                } while (currentToken != Comma);

                values.Add(new string(text, start, end - start));
                start = offset;
            }

            if (label != null)
            {
                if (origin != null)
                {
                    if (label.Text[0] == '.')
                    {
                        if (lastLabel != null)
                        {
                            SetLabel($"{lastLabel}{label.Text}", origin);
                        }
                        else
                        {
                            OnError(ErrorMessage.ERR_NO_GLOBAL);
                        }
                    }
                    else
                    {
                        lastLabel = label.Text;
                        SetLabel(lastLabel, origin);

                    }
                }
                else
                {
                    OnError(ErrorMessage.ERR_NO_SECTION);
                }
            }

            sources.Push(source.Invoke(++instance, values));
            return;
        }

        if (label != null)
        {
            if (origin != null)
            {
                if (label.Text[0] == '.')
                {
                    if (lastLabel != null)
                        SetLabel($"{lastLabel}{label.Text}", origin);
                    else
                    {
                        OnError(ErrorMessage.ERR_NO_GLOBAL);
                    }
                }
                else
                {
                    lastLabel = label.Text;
                    SetLabel(lastLabel, origin);
                }
            }
            else
            {
                OnError(ErrorMessage.ERR_NO_SECTION);
            }

            if (lineType == ' ') lineType = ':';
        }

        if (IsActive) OnError(ErrorMessage.ERR_UNKNOWN_OPCODE);
    }

    /// <summary>
    /// Determines if the <see cref="Assembler"/> supports the given pass.
    /// </summary>
    /// <param name="pass">The assembler pass.</param>
    /// <returns>True if pass is supported, False otherwise</returns>
    protected abstract bool IsSupportedPass(Pass pass);

    /// <summary>
    /// Executes the assembly process for the given file.
    /// </summary>
    /// <param name="fileName">The name of the file to process.</param>
    /// <returns>True if the assembly succeeded with no errors</returns>
    protected bool Assemble(string fileName)
    {
        if (!Assemble(Pass.FIRST, fileName)) return (false);
        if (!Assemble(Pass.INTERMEDIATE, fileName)) return (false);
        if (!Assemble(Pass.INTERMEDIATE, fileName)) return (false);
        if (!Assemble(Pass.INTERMEDIATE, fileName)) return (false);
        if (!Assemble(Pass.FINAL, fileName)) return (false);

        // Add globally define symbols to the object module. 
        foreach (var name in globals)
        {
            var expr = symbols.GetValueOrDefault(name);

            if (expr != null)
                module?.AddGlobal(name, expr);
            else
                OnError($"Undefined global symbol: {name}");
        }

        // Write the object module
        if (errors == 0)
        {
            try
            {
                var objectName = GetObjectFile(fileName);

                module?.SetName(new FileInfo(objectName).Name);

                using StreamWriter stream = new StreamWriter(objectName);
                stream.WriteLine("<?xml version='1.0'?>" + module);

            }
            catch (Exception)
            {
                Console.Error.WriteLine("Error: Could not write object module");
                Environment.Exit(1);
            }
        }

        // Dump symbol table
        if (lineCount != 0)
        {
            throwPage = true;
            Paginate("");
        }

        Paginate("Symbol Table");
        Paginate("");

        // Sort by name
        string[] keys = symbols.Keys.ToArray();
        Array.Sort(keys);

        // Sort by value
        string[] values = (string[])keys.Clone();
        Array.Sort(values, Comparer<string>.Create((arg0, arg1) =>
        {
            var lhs = symbols[arg0]?.Resolve();
            var rhs = symbols[arg1]?.Resolve();

            if (lhs == rhs)
            {
                return arg0?.CompareTo(arg1) ?? 0;
            }


            return lhs?.CompareTo(rhs) ?? 0;
        }));

        for (var index = 0; index < keys.Length; ++index)
        {
            string lhs;
            string rhs;
            string name;
            Expr? expr;
            long value;

            // Format name sice
            name = keys[index];

            expr = symbols.GetValueOrDefault(name);
            value = expr?.Resolve() ?? 0;

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
            expr = symbols.GetValueOrDefault(name);
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

        if (listFile != null)
        {
            listFile.Close();
        }

        return (errors == 0);
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
        if (!IsSupportedPass(this.pass = pass))
        {
            return true;
        }

        StartPass();

        module?.Clear();

        errors = 0;
        warnings = 0;
        lastLabel = null;

        savedLines = null;
        repeatDepth = 0;
        macroDepth = 0;
        instance = 0;

        SetSection(".code");

        try
        {
            if (pass == Pass.FINAL)
            {
                listFile = new StreamWriter(File.OpenWrite(GetListingFile(fileName)), System.Text.Encoding.GetEncoding("ISO-8859-1"));
            }

            sources.Push(new FileSource(fileName, new FileStream(fileName, FileMode.Open)));
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

        return (errors == 0);
    }

    /// <summary>
    /// This method is called at the end of each pass to allow final
    /// actions to take place.
    /// </summary>
    protected virtual void EndPass()
    { }

    /// <summary>
    /// Fetches the next source nextLine from the current source.
    /// </summary>
    /// <returns>The next source nextLine.</returns>
    private Line? GetNextLine()
    {
        return sources.Count == 0 ? null : sources.Peek().NextLine();
    }


    /// <summary>
    /// Print an error message.
    /// </summary>
    /// <param name="message">The _text for the error message.</param>
    protected void OnError(string message)
    {
        var msg = $"Error: {line?.FileName} ({line?.LineNumber}) {message}";

        Console.Error.WriteLine(msg);
        if (pass == Pass.FINAL)
            Paginate(msg);

        errors++;
    }

    /// <summary>
    /// Print a warning message.
    /// </summary>
    /// <param name="message">The _text for the warning message.</param>
    private void OnWarning(string message)
    {
        var msg = $"Warning: {line?.FileName} ({line?.LineNumber}) {message}";

        Console.Error.WriteLine(msg);
        if (pass == Pass.FINAL)
            Paginate(msg);

        warnings++;
    }

    /// <summary>
    /// Output a _line of _text to the listing, trimming trailing spaces.
    /// </summary>
    /// <param name="message">The _text to output.</param>
    private void Paginate(string message)
    {
        if (listFile == null || !listing) return;

        if (lineCount == 0)
        {
            listFile.WriteLine();
            listFile.WriteLine(title);
            listFile.WriteLine();

            lineCount += 3;
        }

        int len;
        for (len = message.Length; len > 0; len--)
        {
            if (message[len - 1] != ' ')
                break;
        }

        if (len > 0)
        {
            listFile.WriteLine(message[..len]);
        }
        else
        {
            listFile.WriteLine();
        }

        if (throwPage || (++lineCount == (linesPerPage - 3)))
        {
            listFile.Write('\f');
            lineCount = 0;
            throwPage = false;
        }
    }

    /// <summary>
    /// Returns the current pass
    /// </summary>
    /// <returns>The current pass</returns>
    protected Pass? GetPass()
    {
        return pass;
    }

    /// <summary>
    /// Derives the name of the listing file from the source file.
    /// </summary>
    /// <param name="filename">The source file name</param>
    /// <returns>The name of the list file</returns>
    protected string GetListingFile(string filename)
    {
        return Path.ChangeExtension(filename, "lst");
    }

    /// <summary>
    /// Derives the name of the object module from the source file.
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    protected string GetObjectFile(string filename)
    {
        return Path.ChangeExtension(filename, "obj");
    }


    /// <summary>
    /// Fetches the next <see cref="Token"/> skipping any pseudo whitespace.
    /// </summary>
    /// <returns>The next <see cref="Token"/> to be processed.</returns>
    protected Token? NextRealToken()
    {
        var token = NextToken();

        while (token == WhiteSpace)
            token = NextToken();

        return (token);
    }

    /// <summary>
    /// Fetches the next <see cref="Token"/> consuming any that have been
    /// pushed back first
    /// </summary>
    /// <returns>The next <see cref="Token"/> to be processed.</returns>
    private Token? NextToken()
    {
        if (tokens.Count != 0)
            return tokens.Pop();

        return (ReadToken());
    }

    /**
	 * Pushes a <CODE>Token</CODE> on the stack so that it can be reread.
	 * 
	 * @param 	currentToken			The <CODE>Token</CODE> to be reprocessed.
	 */
    protected void PushToken(Token token)
    {
        tokens.Push(token);
    }

    /**
	 * Gets and consumes the next character on the source _line.
	 * 
	 * @return	The next character on the _line.
	 */
    protected char NextChar()
    {
        var ch = PeekChar();

        if (ch != '\0') ++offset;
        return (ch);
    }

    /**
	 * Gets the next character for the source _line without consuming it.
	 * 
	 * @return	The next character on the _line.
	 */
    protected char PeekChar()
    {
        return ((offset < text?.Length) ? text[offset] : '\0');
    }

    /// <summary>
    /// Returns the current section origin.
    /// </summary>
    /// <returns>The origin for the current _line.</returns>
    protected Value? GetOrigin()
    {
        return origin;
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
    protected void DoSet(string label, Expr value)
    {
        if (symbols.ContainsKey(label))
        {
            if (variable.Contains(label))
                symbols[label] = value;
            else
                OnError("Symbol has already been defined.");
        }
        else
        {
            symbols[label] = value;
            variable.Add(label);
        }
    }

    /**
    * Allows a derived class to modify the active section name.
    * 
    * @param 	name			The name of the section to activate.
    */
    protected void SetSection(string name)
    {
        section = sections[sectionName = name];
    }

    private static readonly Value MASK = new(null, 0xFF);
    private static readonly Value SIXTEEN = new(null, 16);
    private static readonly Value EIGHT = new(null, 8);
    private static readonly Value ZERO = new(null, 0);
    private static readonly Value ONE = new(null, 1);

    private static readonly StringBuilder Buffer = new();

    private readonly Option defineOption = new("-define", "Define symbols", "(symbol|symbol=value)(,..)*");
    private readonly Option includeOption = new("-include", "Define include path", "path[,path]*");


    /**
	 * Extracts a <see cref="Token"/> by processing characters from the
	 * source _line.
	 * 
	 * @return	The next <see cref="Token"/> extracted from the source.
	 */
    protected abstract Token? ReadToken();


    private string? lastLabel = null;

    // the current _label (if any)
    private Token? label = null;


    /**
	 * The type of _line we are compiling (for the listing).
	 */
    protected char? lineType;

    /**
	 * The address of the _line.
	 */
    protected Expr? addr;

    /**
	 * Title string for listing output
	 */
    protected string? title;

    /**
 * The current <CODE>Token</CODE> under consideration.
 */
    protected Token? currentToken;


    protected MemoryModel? memory = null;


    /**
	 * The collection of named sections.
	 */
    protected Dictionary<string, Section?> sections = new();


    // Tab expansion size.
    private readonly int tabSize = 8;

    // Flag determining listing on/off state
    private bool listing;

    // The current output _line count
    private int lineCount;

    // The number of lines on a page (A4 = 60)
    private int linesPerPage = 60;

    // Writer assigned to listing file in final pass.
    private StreamWriter? listFile = null;

    // Indicates that a page should be thrown after the next output _line.
    private bool throwPage;

    // The module being generated.
    private Module? module;

    // The current sections.
    private Section? section;

    private string? sectionName = null;

    // The current pass.
    private Pass? pass;

    // Holds the origin of the current instruction.
    private Value? origin;

    // A Stack used to store the active code sources.
    private Stack<Source> sources = new();

    // A Stack used to store previously processed tokens
    private Stack<Token> tokens = new();

    // A Stack used record conditional status
    private Stack<bool> status = new();

    // The current _line being assembled.
    private Line? line = null;

    // The characters comprising the _line being assembled.
    private char[]? text;

    // The offset of the next character in the current _line.
    private int offset;

    // The number of errors seen during the current pass.
    private int errors;

    // The number of warnings seen during the current pass.
    private int warnings;

    // The subset of symbols that may be redefined.
    private HashSet<string> variable = new();

    // The set of symbols which will be exported.
    private HashSet<string> globals = new();

    // The set of symbols starting with '.' that are not local labels
    private HashSet<string> notLocal = new();

    // The set of symbol which have been imported.
    private HashSet<string> externs = new();

    // The set of defined macros.
    private readonly Dictionary<string, TextSource> macros = new();

    // The name of the current macro
    private string? macroName = null;

    // The TextSource used to capture macro or repeat lines.
    private TextSource? savedLines = null;

    // Count macro definition depth.
    private int macroDepth = 0;

    // Counts repeat section depth.
    private int repeatDepth = 0;

    // Macro instance counter.
    private int instance = 0;

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
            if (search && includeOption?.Value != null)
            {
                var paths = includeOption.Value?.Split(',') ?? Array.Empty<string>();

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
    
    /// <summary>
    /// Provides access to the output module.
    /// </summary>
    /// <returns>The current module.</returns>
    protected Module? GetModule()
    {
        return (module);
    }

}
