using Dev65.XAsm;
using Dev65.XObj;
using System.Text;
using Module = Dev65.XObj.Module;
using Dev65.XApp;
// ReSharper disable InconsistentNaming

namespace Dev65.W65XX;

/// <summary>
/// The <c>As65</c> provides the base <c>Assembler</c> with an
/// understanding of 65xx family assembler conventions.
/// </summary>
public sealed class As65 : Assembler
{
    // Bit mask for 6501 processor.
    private const int M6501 = 1 << 0;

    // Bit mask for 6502 processor.
    private const int M6502 = 1 << 1;

    // Bit mask for 65C02 processor.
    private const int M65C02 = 1 << 2;

    // Bit mask for 65SC02 processor.
    private const int M65SC02 = 1 << 3;

    // Bit mask for 65C186 processor.
    private const int M65816 = 1 << 4;

    // Bit mask for 65C832 processor.
    private const int M65832 = 1 << 5;

    // Patern to match any processor type
    private const int ANY = M6501 | M6502 | M65C02 | M65SC02 | M65816 | M65832;

    // Indicates that address mode is being parsed for bank 0
    private const int BANK0 = 0;

    // Indicates that address mode is being parsed for the current data back.
    private const int DBANK = 1;

    // Indicates that address mode is being parsed for the current program bank.
    private const int PBANK = 2;


    /// <summary>
    /// An <see cref="Opcode&lt;As65&gt;"/> that handles .6501 directives.
    /// </summary>
    private readonly Opcode<As65> P6501 = new(Keyword, ".6501",
        assembler =>
        {
            assembler.processor = M6501;

            assembler.DoSet("__6501__", True);
            assembler.DoSet("__6502__", False);
            assembler.DoSet("__65C02__", False);
            assembler.DoSet("__65SC02__", False);
            assembler.DoSet("__65816__", False);
            assembler.DoSet("__65832__", False);

            assembler.bitsA = 8;
            assembler.bitsI = 8;

            return false;
        });

    /// <summary>
    /// An <see cref="Opcode&lt;As65&gt;"/> that handles .6502 directives.
    /// </summary>
    private readonly Opcode<As65> P6502 = new(Keyword, ".65O2", as65 =>
    {
        as65.processor = M6502;

        as65.DoSet("__6501__", False);
        as65.DoSet("__6502__", True);
        as65.DoSet("__65C02__", False);
        as65.DoSet("__65SC02__", False);
        as65.DoSet("__65816__", False);
        as65.DoSet("__65832__", False);

        as65.bitsA = 8;
        as65.bitsI = 8;

        return (false);
    });


    /// <summary>
    /// An <see cref="Opcode&lt;As65&gt;"/> that handles .65C02 directives.
    /// </summary>
    private readonly Opcode<As65> P65C02 = new(Keyword, ".65C02", as65 =>
    {
        as65.processor = M65C02;

        as65.DoSet("__6501__", False);
        as65.DoSet("__6502__", False);
        as65.DoSet("__65C02__", True);
        as65.DoSet("__65SC02__", False);
        as65.DoSet("__65816__", False);
        as65.DoSet("__65832__", False);

        as65.bitsA = 8;
        as65.bitsI = 8;

        return (false);
    });

    /// <summary>
    /// An <see cref="Opcode&lt;As65&gt;"/> that handles .65CS02 directives.
    /// </summary>
    private readonly Opcode<As65> P65SC02 = new(Keyword, ".65SC02", as65 =>
    {
        as65.processor = M65SC02;

        as65.DoSet("__6501__", False);
        as65.DoSet("__6502__", False);
        as65.DoSet("__65C02__", False);
        as65.DoSet("__65SC02__", True);
        as65.DoSet("__65816__", False);
        as65.DoSet("__65832__", False);

        as65.bitsA = 8;
        as65.bitsI = 8;

        return (false);

    });

    /// <summary>
    /// An <see cref="Opcode&lt;As65&gt;"/> that handles .65816 directives.
    /// </summary>
    private readonly Opcode<As65> P65816 = new(Keyword, ".65816", as65 =>
    {
        as65.processor = M65816;
        as65.DoSet("__6501__", False);
        as65.DoSet("__6502__", False);
        as65.DoSet("__65C02__", False);
        as65.DoSet("__65SC02__", False);
        as65.DoSet("__65816__", True);
        as65.DoSet("__65832__", False);

        as65.bitsA = 8;
        as65.bitsI = 8;

        return (false);
    });


    /// <summary>
    /// An <see cref="Opcode&lt;As65&gt;"/> that handles .65832 directives.
    /// </summary>
    private readonly Opcode<As65> P65832 = new(Keyword, ".65832", as65 =>
    {
        as65.processor = M65816;
        as65.DoSet("__6501__", False);
        as65.DoSet("__6502__", False);
        as65.DoSet("__65C02__", False);
        as65.DoSet("__65SC02__", False);
        as65.DoSet("__65816__", False);
        as65.DoSet("__65832__", True);

        as65.bitsA = 8;
        as65.bitsI = 8;

        return (false);
    });

    /// <summary>
    /// An <see cref="Opcode&lt;As65&gt;"/> that handles .PAGE0 directives.
    /// </summary>
    private readonly Opcode<As65> PAGE0 = new(Keyword, ".PAGE0", as65 =>
    {
        as65.SetSection(".page0");
        return false;
    });

    /// <summary>
    /// An <see cref="Opcode&lt;As65&gt;"/> that handles .DBREG directives.
    /// </summary>
    private readonly Opcode<As65> DBREG = new(Keyword, ".DBREG", as65 =>
    {
        if ((as65.processor & (M65816 | M65832)) != 0)
        {
            as65.currentToken = as65.NextRealToken();
            var expr = as65.ParseExpression();

            if (expr?.IsAbsolute == true)
                as65.dataBank = (int)expr.Resolve() & 0xff;
            else
                as65.OnError(ErrorMessage.ERR_CONSTANT_EXPR);
        }
        else
            as65.OnError(ERR_65816_ONLY);

        return (false);
    });

    /// <summary>
    /// An <see cref="Opcode&lt;As65&gt;"/> that handles .DPAGE directives.
    /// </summary>
    private readonly Opcode<As65> DPAGE = new(Keyword, ".DPAGE", as65 =>
    {
        if ((as65.processor & (M65816 | M65832)) != 0)
        {
            as65.currentToken = as65.NextRealToken();
            var expr = as65.ParseExpression();

            if (expr?.IsAbsolute == true)
                as65.directPage = (int)expr.Resolve() & 0xffff;
            else
                as65.OnError(ErrorMessage.ERR_CONSTANT_EXPR);
        }
        else
            as65.OnError(ERR_65816_ONLY);

        return (false);
    });

    /// <summary>
    /// An <see cref="Opcode&lt;As65&gt;"/> that handles .LONGA directives.
    /// </summary>
    private readonly Opcode<As65> LONGA = new(Keyword, ".LONGA", as65 =>
    {
        if ((as65.processor & (M65816 | M65832)) != 0)
        {
            as65.currentToken = as65.NextRealToken();
            if (as65.currentToken == as65.QUESTION)
            {
                as65.bitsA = -1;
                as65.currentToken = as65.NextRealToken();
            }
            else if ((as65.currentToken == as65.ON) || (as65.currentToken == as65.Off))
            {
                as65.bitsA = (as65.currentToken == as65.ON) ? 16 : 8;
                as65.currentToken = as65.NextRealToken();
            }
            else
                as65.OnError(ERR_EXPECTED_ON_OR_OFF);
        }
        else
            as65.OnError(ERR_65816_ONLY);

        return (false);

    });

    /// <summary>
    /// An <see cref="Opcode&lt;As65&gt;"/> that handles .LONGI directives.
    /// </summary>
    private readonly Opcode<As65> LONGI = new(Keyword, ".LONGI", as65 =>
    {
        if ((as65.processor & (M65816 | M65832)) != 0)
        {
            as65.currentToken = as65.NextRealToken();
            if (as65.currentToken == as65.QUESTION)
            {
                as65.bitsI = -1;
                as65.currentToken = as65.NextRealToken();
            }
            else if ((as65.currentToken == as65.ON) || (as65.currentToken == as65.Off))
            {
                as65.bitsI = (as65.currentToken == as65.ON) ? 16 : 8;
                as65.currentToken = as65.NextRealToken();
            }
            else
                as65.OnError(ERR_EXPECTED_ON_OR_OFF);
        }
        else
            as65.OnError(ERR_65816_ONLY);

        return (false);

    });

    /// <summary>
    /// An <see cref="Opcode&lt;As65&gt;"/> that handles .WIDEA directives.
    /// </summary>
    private readonly Opcode<As65> WIDEA = new(Keyword, ".WIDEA", as65 =>
    {
        if (as65.processor == M65832)
        {
            as65.currentToken = as65.NextRealToken();
            if (as65.currentToken == as65.QUESTION)
            {
                as65.bitsA = -1;
                as65.currentToken = as65.NextRealToken();
            }

            if ((as65.currentToken == as65.ON) || (as65.currentToken == as65.Off))
            {
                as65.bitsA = (as65.currentToken == as65.ON) ? 32 : 8;
                as65.currentToken = as65.NextRealToken();
            }
            else
                as65.OnError(ERR_EXPECTED_ON_OR_OFF);
        }
        else
            as65.OnError(ERR_65832_ONLY);

        return (false);
    });

    /// <summary>
    /// An <see cref="Opcode&lt;As65&gt;"/> that handles .WIDEI directives.
    /// </summary>
    private readonly Opcode<As65> WIDEI = new(Keyword, ".WIDEI", as65 =>
    {
        if (as65.processor == M65832)
        {
            as65.currentToken = as65.NextRealToken();
            if (as65.currentToken == as65.QUESTION)
            {
                as65.bitsA = -1;
                as65.currentToken = as65.NextRealToken();
            }
            else if (as65.currentToken == as65.ON || as65.currentToken == as65.Off)
            {
                as65.bitsI = (as65.currentToken == as65.ON) ? 32 : 8;
                as65.currentToken = as65.NextRealToken();
            }
            else
            {
                as65.OnError(ERR_EXPECTED_ON_OR_OFF);
            }
        }
        else
        {
            as65.OnError(ERR_65832_ONLY);
        }

        return false;
    });

    /// <summary>
    /// An <see cref="Opcode&lt;As65&gt;"/> that handles .ADDR directives.
    /// </summary>
    private readonly Opcode<As65> ADDR = new(Keyword, ".ADDR", as65 =>
    {
        do
        {
            as65.currentToken = as65.NextRealToken();
            var expr = as65.ParseExpression();

            if (expr != null)
                as65.AddAddress(expr);
            else
                as65.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        } while (as65.currentToken == Comma);

        if (as65.currentToken != EOL) as65.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        return (true);
    });

    /// <summary>
    /// A <see cref="Token"/> representing the '?' character.
    /// </summary>
    private readonly Token QUESTION = new(Keyword, "?");

    /// <summary>
    /// A <see cref="Token"/> representing the '#' character.
    /// </summary>
    private readonly Token HASH = new(Keyword, "#");

    /// <summary>
    /// A <see cref="Token"/> representing the A register.
    /// </summary>
    private readonly Token A = new(Keyword, "A");

    /// <summary>
    /// A <see cref="Token"/> representing the S register.
    /// </summary>
    private readonly Token S = new(Keyword, "S");

    /// <summary>
    /// A <see cref="Token"/> representing the X register.
    /// </summary>
    private readonly Token X = new(Keyword, "X");

    /// <summary>
    /// A <see cref="Token"/> representing the Y register.
    /// </summary>
    private readonly Token Y = new(Keyword, "Y");

    /// <summary>
    /// A <see cref="Token"/> representing the ON keyword.
    /// </summary>
    private readonly Token ON = new(Keyword, "ON");

    /// <summary>
    /// A <see cref="Token"/> representing the OFF keyword.
    /// </summary>
    private readonly Token Off = new(Keyword, "OFF");

    /// <summary>
    /// A <see cref="Token"/> representing the '[' character.
    /// </summary>
    private readonly Token LBRACKET = new(Keyword, "[");

    /// <summary>
    /// A <see cref="Token"/> representing the ']' character.
    /// </summary>
    private readonly Token RBRACKET = new(Keyword, "]");

    /// <summary>
    /// A <see cref="Token"/> representing the EQ keyword.
    /// </summary>
    private new readonly Token EQ = new(Keyword, "EQ");

    /// <summary>
    /// A <see cref="Token"/> representing the NE keyword.
    /// </summary>
    private new readonly Token NE = new(Keyword, "NE");

    /// <summary>
    /// A <see cref="Token"/> representing the CC keyword.
    /// </summary>
    private readonly Token CC = new (Keyword, "CC");

    /// <summary>
    /// A <see cref="Token"/> representing the CS keyword.
    /// </summary>
    private readonly Token CS = new (Keyword, "CS");

    /// <summary>
    /// A <see cref="Token"/> representing the PL keyword.
    /// </summary>
    private readonly Token PL = new (Keyword, "PL");

    /// <summary>
    /// A <see cref="Token"/> representing the MI keyword.
    /// </summary>
    private readonly Token MI = new(Keyword, "MI");

    /// <summary>
    /// A <see cref="Token"/> representing the VC keyword.
    /// </summary>
    private readonly Token VC = new(Keyword, "VC");

    /// <summary>
    /// A <see cref="Token"/> representing the VS keyword.
    /// </summary>
    private readonly Token VS = new(Keyword, "VS");

    private sealed class Jump : Opcode<As65>
    {
        private readonly Token? _flag;

        public Jump(string mnemonic, Token? flag): base(Keyword, mnemonic)
        {
            _flag = flag;
        }

        public override bool Compile(As65 assembler)
        {
            assembler.currentToken = assembler.NextRealToken();

            var expr = assembler.ParseExpression();
            if (expr != null)
                if (_flag != null)
                    assembler.GenerateBranch(_flag, expr);
                else
                    assembler.GenerateJump(expr);
            else
                assembler.OnError(ERR_MISSING_EXPRESSION);

            return (true);
        }
    }

    /// <summary>
    /// An extended Opcode class used to compile RMB and SMB instructions
    /// </summary>
    private sealed class BitOperation : Opcode<As65>
    {
        private readonly int _opcode;

        public BitOperation(TokenKind kind, string text, int opcode) : base(kind, text)
        {
            _opcode = opcode;
        }

        public override bool Compile(As65 assembler)
        {
            if ((assembler.processor & (M6501 | M65C02)) != 0)
            {
                assembler.currentToken = assembler.NextRealToken();
                var addr = assembler.ParseExpression();
                if (assembler.GetOrigin() != null)
                {
                    assembler.AddByte((byte)_opcode);
                    assembler.AddByte(addr);
                }
                else
                {
                    assembler.OnError("No active section");
                }
            }
            else
            {
                assembler.OnError(ERR_OPCODE_NOT_SUPPORTED);
            }

            return true;
        }
    }

    /// <summary>
    /// An extended Opcode class used to compile BBR and BBS instructions
    /// </summary>
    private sealed class BitBranch : Opcode<As65>
    {
        private readonly int _opcode;

        public BitBranch(TokenKind kind, string text, int opcode) : base(kind, text)
        {
            _opcode = opcode;
        }

        public override bool Compile(As65 assembler)
        {
            if ((assembler.processor & (M6501 | M65C02)) != 0)
            {
                assembler.currentToken = assembler.NextRealToken();

                var addr = assembler.ParseExpression();

                if (assembler.currentToken == Comma)
                {
                    assembler.currentToken = assembler.NextRealToken();
                }
                else
                {
                    assembler.OnError("Expected comma");
                }

                var jump = assembler.ParseExpression();
                var origin = assembler.GetOrigin();

                if (origin != null)
                {
                    assembler.AddByte((byte)_opcode);
                    assembler.AddByte(addr);
                    assembler.AddByte(Expr.Sub(jump, Expr.Add(origin, THREE)));
                }
                else
                {
                    assembler.OnError("No active section");
                }

            }
            else
            {
                assembler.OnError(ERR_OPCODE_NOT_SUPPORTED);
            }

            return true;
        }
    }

    /// <summary>
    /// An <code>Opcode</code> that handles the ADC instruction.
    /// </summary>
    private readonly Opcode<As65> ADC = new(Keyword, "ADC", as65 =>
    {
        switch (as65.ParseMode(DBANK))
        {
            case IMMD:
                as65.GenerateImmediate(0x69, as65.arg, as65.bitsA);
                break;
            case DPAG:
                as65.GenerateDirectPage(0x65, as65.arg);
                break;
            case ABSL:
                as65.GenerateAbsolute(0x6d, as65.arg);
                break;
            case DPGX:
                as65.GenerateDirectPage(0x75, as65.arg);
                break;
            case ABSX:
                as65.GenerateAbsolute(0x7d, as65.arg);
                break;
            case DPGY:
            case ABSY:
                as65.GenerateAbsolute(0x79, as65.arg);
                break;
            case INDX:
                as65.GenerateDirectPage(0x61, as65.arg);
                break;
            case INDY:
                as65.GenerateDirectPage(0x71, as65.arg);
                break;
            case INDI:
                if ((as65.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    as65.GenerateDirectPage(0x72, as65.arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALNG:
                if ((as65.processor & (M65816 | M65832)) != 0)
                    as65.GenerateLong(0x6f, as65.arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALGX:
                if ((as65.processor & (M65816 | M65832)) != 0)
                    as65.GenerateLong(0x7f, as65.arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LIND:
                if ((as65.processor & (M65816 | M65832)) != 0)
                    as65.GenerateDirectPage(0x67, as65.arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LINY:
                if ((as65.processor & (M65816 | M65832)) != 0)
                    as65.GenerateDirectPage(0x77, as65.arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STAC:
                if ((as65.processor & (M65816 | M65832)) != 0)
                    as65.GenerateImmediate(0x63, as65.arg, 8);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STKI:
                if ((as65.processor & (M65816 | M65832)) != 0)
                    as65.GenerateImmediate(0x73, as65.arg, 8);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            default:
                as65.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return (true);
    });

    /// <summary>
    /// An <code>Opcode</code> that handles the AND instruction.
    /// </summary>
    private readonly Opcode<As65> AND = new(Keyword, "AND", as65 =>
    {
        switch (as65.ParseMode(DBANK))
        {
            case IMMD:
                as65.GenerateImmediate(0x29, as65.arg, as65.bitsA);
                break;
            case DPAG:
                as65.GenerateDirectPage(0x25, as65.arg);
                break;
            case ABSL:
                as65.GenerateAbsolute(0x2d, as65.arg);
                break;
            case DPGX:
                as65.GenerateDirectPage(0x35, as65.arg);
                break;
            case ABSX:
                as65.GenerateAbsolute(0x3d, as65.arg);
                break;
            case DPGY:
            case ABSY:
                as65.GenerateAbsolute(0x39, as65.arg);
                break;
            case INDX:
                as65.GenerateDirectPage(0x21, as65.arg);
                break;
            case INDY:
                as65.GenerateDirectPage(0x31, as65.arg);
                break;
            case INDI:
                if ((as65.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    as65.GenerateDirectPage(0x32, as65.arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALNG:
                if ((as65.processor & (M65816 | M65832)) != 0)
                    as65.GenerateLong(0x2f, as65.arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALGX:
                if ((as65.processor & (M65816 | M65832)) != 0)
                    as65.GenerateLong(0x3f, as65.arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LIND:
                if ((as65.processor & (M65816 | M65832)) != 0)
                    as65.GenerateDirectPage(0x27, as65.arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LINY:
                if ((as65.processor & (M65816 | M65832)) != 0)
                    as65.GenerateDirectPage(0x37, as65.arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STAC:
                if ((as65.processor & (M65816 | M65832)) != 0)
                    as65.GenerateImmediate(0x23, as65.arg, 8);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STKI:
                if ((as65.processor & (M65816 | M65832)) != 0)
                    as65.GenerateImmediate(0x33, as65.arg, 8);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            default:
                as65.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return (true);
    });

    /// <summary>
    /// An <code>Opcode</code> that handles the ASL instruction.
    /// </summary>
    private readonly Opcode<As65> ASL = new(Keyword, "ASL", as65 =>
    {
        switch (as65.ParseMode(DBANK))
        {
            case IMPL:
            case ACCM:
                as65.GenerateImplied(0x0a);
                break;
            case DPAG:
                as65.GenerateDirectPage(0x06, as65.arg);
                break;
            case ABSL:
                as65.GenerateAbsolute(0x0e, as65.arg);
                break;
            case DPGX:
                as65.GenerateDirectPage(0x16, as65.arg);
                break;
            case ABSX:
                as65.GenerateAbsolute(0x1e, as65.arg);
                break;

            default:
                as65.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return (true);
    });

    /// <summary>
    /// An <code>Opcode</code> that handles the BBR0 instruction.
    /// </summary>
    private readonly Opcode<As65> BBR0 = new BitBranch(Keyword, "BBR0", 0x0f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBR1 instruction.
    /// </summary>
    private readonly Opcode<As65> BBR1 = new BitBranch(Keyword, "BBR1", 0x1f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBR2 instruction.
    /// </summary>
    private readonly Opcode<As65> BBR2 = new BitBranch(Keyword, "BBR2", 0x2f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBR3 instruction.
    /// </summary>
    private readonly Opcode<As65> BBR3 = new BitBranch(Keyword, "BBR3", 0x3f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBR4 instruction.
    /// </summary>
    private readonly Opcode<As65> BBR4 = new BitBranch(Keyword, "BBR4", 0x4f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBR5 instruction.
    /// </summary>
    private readonly Opcode<As65> BBR5 = new BitBranch(Keyword, "BBR5", 0x5f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBR6 instruction.
    /// </summary>
    private readonly Opcode<As65> BBR6 = new BitBranch(Keyword, "BBR6", 0x6f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBR7 instruction.
    /// </summary>
    private readonly Opcode<As65> BBR7 = new BitBranch(Keyword, "BBR7", 0x7f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBS0 instruction.
    /// </summary>
    private readonly Opcode<As65> BBS0 = new BitBranch(Keyword, "BBS0", 0x8f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBS1 instruction.
    /// </summary>
    private readonly Opcode<As65> BBS1 = new BitBranch(Keyword, "BBS1", 0x9F);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBS2 instruction.
    /// </summary>
    private readonly Opcode<As65> BBS2 = new BitBranch(Keyword, "BBS2", 0xAF);

    /// <summary>
    /// An<code> Opcode</code> that handles the BBS4 instruction. 
    /// </summary>
    private readonly Opcode<As65> BBS3 = new BitBranch(Keyword, "BBS3", 0xBF);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBS4 instruction.
    /// </summary>
    private readonly Opcode<As65> BBS4 = new BitBranch(Keyword, "BBS4", 0xCF);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBS5 instruction.
    /// </summary>
    private readonly Opcode<As65> BBS5 = new BitBranch(Keyword, "BBS5", 0xDF);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBS6 instruction.
    /// </summary>
    private readonly Opcode<As65> BBS6 = new BitBranch(Keyword, "BBS6", 0xEF);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBS7 instruction.
    /// </summary>
    private readonly Opcode<As65> BBS7 = new BitBranch(Keyword, "BBS7", 0xFF);


    /// <summary>
    /// An <code>Opcode</code> that handles the BCC instruction.
    /// </summary>
    private readonly Opcode<As65> BCC = new(Keyword, "BCC", as65 =>
    {
        switch (as65.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG:
                as65.GenerateRelative(0x90, as65.arg, false);
                break;
            default:
                as65.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return (true);
    });

    /// <summary>
    /// An <code>Opcode</code> that handles the BCS instruction.
    /// </summary>
    private readonly Opcode<As65> BCS = new(Keyword, "BCS", as65 =>
    {
        switch (as65.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG:
                as65.GenerateRelative(0xb0, as65.arg, false);
                break;
            default:
                as65.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return (true);
    });

    /// <summary>
    /// An <code>Opcode</code> that handles the BEQ instruction.
    /// </summary>
    private readonly Opcode<As65> BEQ = new(Keyword, "BEQ", as65 =>
    {
        switch (as65.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG:
                as65.GenerateRelative(0xf0, as65.arg, false);
                break;
            default:
                as65.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return (true);
    });

    /// <summary>
    /// An <code>Opcode</code> that handles the BIT instruction.
    /// </summary>
    private readonly Opcode<As65> BIT = new(Keyword, "BIT", as65 =>
    {
        switch (as65.ParseMode(DBANK))
        {
            case DPAG:
                as65.GenerateDirectPage(0x24, as65.arg);
                break;
            case ABSL:
                as65.GenerateAbsolute(0x2c, as65.arg);
                break;
            case IMMD:
                if ((as65.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    as65.GenerateImmediate(0x89, as65.arg, as65.bitsA);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case DPGX:
                if ((as65.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    as65.GenerateDirectPage(0x34, as65.arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ABSX:
                if ((as65.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    as65.GenerateAbsolute(0x3c, as65.arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            default:
                as65.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return (true);
    });

    /// <summary>
    /// An <code>Opcode</code> that handles the BMI instruction.
    /// </summary>
    private readonly Opcode<As65> BMI = new(Keyword, "BMI", as65 =>
    {
        switch (as65.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG:
                as65.GenerateRelative(0x30, as65.arg, false);
                break;
            default:
                as65.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the BNE instruction.
    /// </summary>
    private readonly Opcode<As65> BNE = new(Keyword, "BNE", as65 =>
    {
        switch (as65.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG:
                as65.GenerateRelative(0xd0, as65.arg, false);
                break;
            default:
                as65.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });

    /// <summary>
    /// An <code>Opcode</code> that handles the BPL instruction.
    /// </summary>
    private readonly Opcode<As65> BPL = new(Keyword, "BPL", as65 =>
    {
        switch (as65.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG:
                as65.GenerateRelative(0x10, as65.arg, false);
                break;
            default:
                as65.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });

    /// <summary>
    /// An <code>Opcode</code> that handles the BRA instruction.
    /// </summary>
    private readonly Opcode<As65> BRA = new(Keyword, "BRA", as65 =>
    {
        if ((as65.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
        {
            switch (as65.ParseMode(PBANK))
            {
                case DPAG:
                case ABSL:
                case ALNG:
                    as65.GenerateRelative(0x80, as65.arg, false);
                    break;
                default:
                    as65.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            as65.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the BRK instruction.
    /// </summary>
    private readonly Opcode<As65> BRK = new(Keyword, "BRK", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0x00, asm.arg, 8); break;
            case IMPL: asm.GenerateImplied(0x00); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the BRL instruction.
    /// </summary>
    private readonly Opcode<As65> BRL = new(Keyword, "BRL", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(PBANK))
            {
                case DPAG:
                case ABSL:
                case ALNG:
                    asm.GenerateRelative(0x82, asm.arg, true);
                    break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);
        return (true);
    });



    /// <summary>
    /// An <code>Opcode</code> that handles the BVC instruction.
    /// </summary>				
    private readonly Opcode<As65> BVC = new(Keyword, "BVC", asm =>
    {
        switch (asm.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG:
                asm.GenerateRelative(0x50, asm.arg, false);
                break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });




    /// <summary>
    /// An <code>Opcode</code> that handles the BVS instruction.
    /// </summary>				
    private readonly Opcode<As65> BVS = new(Keyword, "BVS", asm =>
    {
        switch (asm.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG:
                asm.GenerateRelative(0x70, asm.arg, false);
                break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });



    /// <summary>
    /// An <code>Opcode</code> that handles the CLC instruction.
    /// </summary>				
    private readonly Opcode<As65> CLC = new(Keyword, "CLC", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL: asm.GenerateImplied(0x18); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });

    
    /// <summary>
    /// An <code>Opcode</code> that handles the CLD instruction.
    /// </summary>				
    private readonly Opcode<As65> CLD = new(Keyword, "CLD", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL: asm.GenerateImplied(0xd8); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });

    
    /// <summary>
    /// An <code>Opcode</code> that handles the CLI instruction.
    /// </summary>				
    private readonly Opcode<As65> CLI = new(Keyword, "CLI", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL: asm.GenerateImplied(0x58); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });



    /// <summary>
    /// An <code>Opcode</code> that handles the CLV instruction.
    /// </summary>				
    private readonly Opcode<As65> CLV = new(Keyword, "CLV", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL: asm.GenerateImplied(0xb8); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });

    /// <summary>
    /// An <code>Opcode</code> that handles the CMP instruction.
    /// </summary>				
    private readonly Opcode<As65> CMP = new(Keyword, "CMP", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0xc9, asm.arg, asm.bitsA); break;
            case DPAG: asm.GenerateDirectPage(0xc5, asm.arg); break;
            case ABSL: asm.GenerateAbsolute(0xcd, asm.arg); break;
            case DPGX: asm.GenerateDirectPage(0xd5, asm.arg); break;
            case ABSX: asm.GenerateAbsolute(0xdd, asm.arg); break;
            case DPGY:
            case ABSY: asm.GenerateAbsolute(0xd9, asm.arg); break;
            case INDX: asm.GenerateDirectPage(0xc1, asm.arg); break;
            case INDY: asm.GenerateDirectPage(0xd1, asm.arg); break;
            case INDI:
                if ((asm.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0xd2, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALNG:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0xcf, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALGX:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0xdf, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LIND:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0xc7, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LINY:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0xd7, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STAC:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateImmediate(0xc3, asm.arg, 8);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STKI:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateImmediate(0xd3, asm.arg, 8);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });



    /// <summary>
    /// An <code>Opcode</code> that handles the COP instruction.
    /// </summary>				
    private readonly Opcode<As65> COP = new(Keyword, "COP", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMMD: asm.GenerateImmediate(0x02, asm.arg, 8); break;
                case IMPL: asm.GenerateImplied(0x02); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });

    /// <summary>
    /// An <code>Opcode</code> that handles the CPX instruction.
    /// </summary>				
    private readonly Opcode<As65> CPX = new(Keyword, "CPX", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0xe0, asm.arg, asm.bitsI); break;
            case DPAG: asm.GenerateDirectPage(0xe4, asm.arg); break;
            case ABSL: asm.GenerateAbsolute(0xec, asm.arg); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });

    /// <summary>
    /// An <code>Opcode</code> that handles the CPY instruction.
    /// </summary>				
    private readonly Opcode<As65> CPY = new(Keyword, "CPY", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0xc0, asm.arg, asm.bitsI); break;
            case DPAG: asm.GenerateDirectPage(0xc4, asm.arg); break;
            case ABSL: asm.GenerateAbsolute(0xcc, asm.arg); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });
    
    /// <summary>
    /// An <code>Opcode</code> that handles the DEC instruction.
    /// </summary>				
    private readonly Opcode<As65> DEC = new(Keyword, "DEC", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case DPAG: asm.GenerateDirectPage(0xC6, asm.arg); break;
            case ABSL: asm.GenerateAbsolute(0xCE, asm.arg); break;
            case DPGX: asm.GenerateDirectPage(0xD6, asm.arg); break;
            case ABSX: asm.GenerateAbsolute(0xDE, asm.arg); break;
            case ACCM:
                if ((asm.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    asm.GenerateImplied(0x3a);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });

    /// <summary>
    /// An <code>Opcode</code> that handles the DEX instruction.
    /// </summary>				
    private readonly Opcode<As65> DEX = new(Keyword, "DEX", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL: asm.GenerateImplied(0xca); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });

    
    /// <summary>
    /// An <code>Opcode</code> that handles the DEY instruction.
    /// </summary>				
    private readonly Opcode<As65> DEY = new(Keyword, "DEY", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL: asm.GenerateImplied(0x88); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });



    /// <summary>
    /// An <code>Opcode</code> that handles the EOR instruction.
    /// </summary>				
    private readonly Opcode<As65> EOR = new(Keyword, "EOR", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0x49, asm.arg, asm.bitsA); break;
            case DPAG: asm.GenerateDirectPage(0x45, asm.arg); break;
            case ABSL: asm.GenerateAbsolute(0x4d, asm.arg); break;
            case DPGX: asm.GenerateDirectPage(0x55, asm.arg); break;
            case ABSX: asm.GenerateAbsolute(0x5d, asm.arg); break;
            case DPGY:
            case ABSY: asm.GenerateAbsolute(0x59, asm.arg); break;
            case INDX: asm.GenerateDirectPage(0x41, asm.arg); break;
            case INDY: asm.GenerateDirectPage(0x51, asm.arg); break;
            case INDI:
                if ((asm.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0x52, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALNG:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0x4f, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALGX:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0x5f, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LIND:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0x47, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LINY:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0x57, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STAC:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateImmediate(0x43, asm.arg, 8);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STKI:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateImmediate(0x53, asm.arg, 8);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });




    /// <summary>
    /// An <code>Opcode</code> that handles the INC instruction.
    /// </summary>				
    private readonly Opcode<As65> INC = new(Keyword, "INC", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case DPAG: asm.GenerateDirectPage(0xe6, asm.arg); break;
            case ABSL: asm.GenerateAbsolute(0xee, asm.arg); break;
            case DPGX: asm.GenerateDirectPage(0xf6, asm.arg); break;
            case ABSX: asm.GenerateAbsolute(0xfe, asm.arg); break;
            case ACCM:
                if ((asm.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    asm.GenerateImplied(0x1a);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    
    });
    
    /// <summary>
    /// An <code>Opcode</code> that handles the INX instruction.
    /// </summary>				
    private readonly Opcode<As65> INX = new(Keyword, "INX", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL: asm.GenerateImplied(0xe8); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the INY instruction.
    /// </summary>				
    private readonly Opcode<As65> INY = new(Keyword, "INY", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL: asm.GenerateImplied(0xc8); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the JML instruction.
    /// </summary>				
    private readonly Opcode<As65> JML = new(Keyword, "JML", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(PBANK))
            {
                case DPAG:
                case ABSL:
                case ALNG: asm.GenerateLong(0x5c, asm.arg); break;
                case LIND: asm.GenerateIndirect(0xdc, asm.arg, true); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the JMP instruction.
    /// </summary>				
    private readonly Opcode<As65> JMP = new(Keyword, "JMP", asm =>
    {
        switch (asm.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL: asm.GenerateAbsolute(0x4c, asm.arg); break;
            case INDI: asm.GenerateIndirect(0x6c, asm.arg, true); break;
            case INDX:
                if ((asm.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    asm.GenerateAbsolute(0x7c, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;
            case ALNG:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0x5c, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;
            case LIND:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateIndirect(0xdc, asm.arg, true);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the JSL instruction.
    /// </summary>				
    private readonly Opcode<As65> JSL = new(Keyword, "JSL", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(PBANK))
            {
                case DPAG:
                case ABSL:
                case ALNG: asm.GenerateLong(0x22, asm.arg); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the JSR instruction.
    /// </summary>				
    private readonly Opcode<As65> JSR = new(Keyword, "JSR", asm =>
    {
        switch (asm.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG: asm.GenerateAbsolute(0x20, asm.arg); break;

            case INDX:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateAbsolute(0xfc, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the LDA instruction.
    /// </summary>				
    private readonly Opcode<As65> LDA = new(Keyword, "LDA", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0xa9, asm.arg, asm.bitsA); break;
            case DPAG: asm.GenerateDirectPage(0xa5, asm.arg); break;
            case ABSL: asm.GenerateAbsolute(0xad, asm.arg); break;
            case DPGX: asm.GenerateDirectPage(0xb5, asm.arg); break;
            case ABSX: asm.GenerateAbsolute(0xbd, asm.arg); break;
            case DPGY:
            case ABSY: asm.GenerateAbsolute(0xb9, asm.arg); break;
            case INDX: asm.GenerateDirectPage(0xa1, asm.arg); break;
            case INDY: asm.GenerateDirectPage(0xb1, asm.arg); break;
            case INDI:
                if ((asm.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0xb2, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALNG:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0xaf, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALGX:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0xbf, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LIND:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0xa7, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LINY:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0xb7, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STAC:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateImmediate(0xa3, asm.arg, 8);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STKI:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateImmediate(0xb3, asm.arg, 8);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the LDX instruction.
    /// </summary>				
    private readonly Opcode<As65> LDX = new(Keyword, "LDX", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0xa2, asm.arg, asm.bitsI); break;
            case DPAG: asm.GenerateDirectPage(0xa6, asm.arg); break;
            case ABSL: asm.GenerateAbsolute(0xae, asm.arg); break;
            case DPGY: asm.GenerateDirectPage(0xb6, asm.arg); break;
            case ABSY: asm.GenerateAbsolute(0xbe, asm.arg); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the LDY instruction.
    /// </summary>				
    private readonly Opcode<As65> LDY = new(Keyword, "LDY", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0xa0, asm.arg, asm.bitsI); break;
            case DPAG: asm.GenerateDirectPage(0xa4, asm.arg); break;
            case ABSL: asm.GenerateAbsolute(0xac, asm.arg); break;
            case DPGX: asm.GenerateDirectPage(0xb4, asm.arg); break;
            case ABSX: asm.GenerateAbsolute(0xbc, asm.arg); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the LSR instruction.
    /// </summary>				
    private readonly Opcode<As65> LSR = new(Keyword, "LSR", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL:
            case ACCM: asm.GenerateImplied(0x4a); break;
            case DPAG: asm.GenerateDirectPage(0x46, asm.arg); break;
            case ABSL: asm.GenerateAbsolute(0x4e, asm.arg); break;
            case DPGX: asm.GenerateDirectPage(0x56, asm.arg); break;
            case ABSX: asm.GenerateAbsolute(0x5e, asm.arg); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the MVN instruction.
    /// </summary>				
    private readonly Opcode<As65> MVN = new(Keyword, "MVN", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            asm.currentToken = asm.NextRealToken();
            var dst = asm.ParseExpression();

            if (asm.currentToken == Comma)
            {
                asm.currentToken = asm.NextRealToken();
                var src = asm.ParseExpression();

                asm.AddByte(0x54);
                asm.AddByte(dst);
                asm.AddByte(src);
            }
            else
                asm.OnError(ERR_EXPECTED_COMMA);
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the MVP instruction.
    /// </summary>				
    private readonly Opcode<As65> MVP = new(Keyword, "MVP", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            asm.currentToken = asm.NextRealToken();
            var dst = asm.ParseExpression();

            if (asm.currentToken == Comma)
            {
                asm.currentToken = asm.NextRealToken();
                var src = asm.ParseExpression();

                asm.AddByte(0x44);
                asm.AddByte(dst);
                asm.AddByte(src);
            }
            else
                asm.OnError(ERR_EXPECTED_COMMA);
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the NOP instruction.
    /// </summary>				
    private readonly Opcode<As65> NOP = new(Keyword, "NOP", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL: asm.GenerateImplied(0xea); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the ORA instruction.
    /// </summary>				
    private readonly Opcode<As65> ORA = new(Keyword, "ORA", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0x09, asm.arg, asm.bitsA); break;
            case DPAG: asm.GenerateDirectPage(0x05, asm.arg); break;
            case ABSL: asm.GenerateAbsolute(0x0f, asm.arg); break;
            case DPGX: asm.GenerateDirectPage(0x15, asm.arg); break;
            case ABSX: asm.GenerateAbsolute(0x1f, asm.arg); break;
            case DPGY:
            case ABSY: asm.GenerateAbsolute(0x19, asm.arg); break;
            case INDX: asm.GenerateDirectPage(0x01, asm.arg); break;
            case INDY: asm.GenerateDirectPage(0x11, asm.arg); break;
            case INDI:
                if ((asm.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0x12, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALNG:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0x0f, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALGX:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0x1f, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LIND:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0x07, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LINY:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0x17, asm.arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STAC:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateImmediate(0x03, asm.arg, 8);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STKI:
                if ((asm.processor & (M65816 | M65832)) != 0)
                    asm.GenerateImmediate(0x13, asm.arg, 8);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the PEA instruction.
    /// </summary>				
    private readonly Opcode<As65> PEA = new(Keyword, "PEA", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case DPAG:
                case ABSL:
                case IMMD: asm.GenerateImmediate(0xf4, asm.arg, 16); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the PEI instruction.
    /// </summary>				
    private readonly Opcode<As65> PEI = new(Keyword, "PEI", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case INDI: asm.GenerateImmediate(0xd4, asm.arg, 8); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the PER instruction.
    /// </summary>				
    private readonly Opcode<As65> PER = new(Keyword, "PER", asm =>
    {
        // put your code here
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(PBANK))
            {
                case DPAG:
                case ABSL:
                    asm.GenerateRelative(0x62, asm.arg, true); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the PHA instruction.
    /// </summary>				
    private readonly Opcode<As65> PHA = new(Keyword, "PHA", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL: asm.GenerateImplied(0x48); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the PHB instruction.
    /// </summary>				
    private readonly Opcode<As65> PHB = new(Keyword, "PHB", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL: asm.GenerateImplied(0x8b); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the PHD instruction.
    /// </summary>				
    private readonly Opcode<As65> PHD = new(Keyword, "PHD", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL: asm.GenerateImplied(0x0b); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the PHK instruction.
    /// </summary>				
    private readonly Opcode<As65> PHK = new(Keyword, "PHK", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL: asm.GenerateImplied(0x4b); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the PHP instruction.
    /// </summary>				
    private readonly Opcode<As65> PHP = new(Keyword, "PHP", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL: asm.GenerateImplied(0x08);
                break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return true;
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the PHX instruction.
    /// </summary>				
    private readonly Opcode<As65> PHX = new(Keyword, "PHX", asm =>
    {
        if ((asm.processor & (M6502 | M65SC02 | M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL:
                    asm.GenerateImplied(0xda);
                    break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
        {
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);
        }

        return true;

    });


    /// <summary>
    /// An <code>Opcode</code> that handles the PHY instruction.
    /// </summary>				
    private readonly Opcode<As65> PHY = new(Keyword, "PHY", asm =>
    {
        if ((asm.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL: asm.GenerateImplied(0x5a);
                    break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
        {
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);
        }

        return true;
    });



    /// <summary>
    /// An <code>Opcode</code> that handles the PLA instruction.
    /// </summary>				
    private readonly Opcode<As65> PLA = new(Keyword, "PLA", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL: asm.GenerateImplied(0x68);
                break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return true;
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the PLB instruction.
    /// </summary>				
    private readonly Opcode<As65> PLB = new(Keyword, "PLB", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL: asm.GenerateImplied(0xab);
                    break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR); break;

            }
            
        }
        else
        {
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);
        }

        return true;
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the PLD instruction.
    /// </summary>				
    private readonly Opcode<As65> PLD = new(Keyword, "PLD", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL:
                    asm.GenerateImplied(0x2b);
                    break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR); break;

            }

        }
        else
        {
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);
        }

        return true;
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the PLP instruction.
    /// </summary>				
    private readonly Opcode<As65> PLP = new(Keyword, "PLP", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL:
                asm.GenerateImplied(0x28);
                break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;

        }


        return true;
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the PLX instruction.
    /// </summary>				
    private readonly Opcode<As65> PLX = new(Keyword, "PLX", asm =>
    {
        if ((asm.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL:
                    asm.GenerateImplied(0xfa);
                    break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR); break;

            }

        }
        else
        {
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);
        }

        return true;
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the PLY instruction.
    /// </summary>				
    private readonly Opcode<As65> PLY = new(Keyword, "PLY", asm =>
    {
        if ((asm.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL:
                    asm.GenerateImplied(0x7a);
                    break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR); break;

            }

        }
        else
        {
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);
        }

        return true;
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the REP instruction.
    /// </summary>				        
    private readonly Opcode<As65> REP = new(Keyword, "REP", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL:
                    asm.GenerateImmediate(0xc2, asm.arg, 8);
                    break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;

            }
        }
        else
        {
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);
        }

        return true;
    });

    /// <summary>
    /// An <CODE>Opcode</CODE> that handles the RMB0 instruction.
    /// </summary>
    private readonly Opcode<As65> RMB0 = new BitOperation(Keyword, "RMB0", 0x07);

    /// <summary>
    /// An <CODE>Opcode</CODE> that handles the RMB1 instruction.
    /// </summary>
    private readonly Opcode<As65> RMB1 = new BitOperation(Keyword, "RMB1", 0x17);

    /// <summary>
    /// An <CODE>Opcode</CODE> that handles the RMB2 instruction.
    /// </summary>
    private readonly Opcode<As65> RMB2 = new BitOperation(Keyword, "RMB2", 0x27);

    /// <summary>
    /// An <CODE>Opcode</CODE> that handles the RMB3 instruction.
    /// </summary>
    private readonly Opcode<As65> RMB3 = new BitOperation(Keyword, "RMB3", 0x37);

    /// <summary>
    /// An <CODE>Opcode</CODE> that handles the RMB4 instruction.
    /// </summary>
    private readonly Opcode<As65> RMB4 = new BitOperation(Keyword, "RMB4", 0x47);

    /// <summary>
    /// An <CODE>Opcode</CODE> that handles the RMB5 instruction.
    /// </summary>
    private readonly Opcode<As65> RMB5 = new BitOperation(Keyword, "RMB5", 0x57);

    /// <summary>
    /// An <CODE>Opcode</CODE> that handles the RMB6 instruction.
    /// </summary>
    private readonly Opcode<As65> RMB6 = new BitOperation(Keyword, "RMB6", 0x67);

    /// <summary>
    /// An <CODE>Opcode</CODE> that handles the RMB7 instruction.
    /// </summary>
    private readonly Opcode<As65> RMB7 = new BitOperation(Keyword, "RMB7", 0x77);

    /// <summary>
    /// The <code>Opcode</code> to handle the ROL instruction.
    /// </summary>
    private readonly Opcode<As65> ROL = new (Keyword, "ROL", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL:
            case ACCM:
                asm.GenerateImplied(0x2a);
                break;
            case DPAG:
                asm.GenerateDirectPage(0x26, asm.arg);
                break;
            case ABSL:
                asm.GenerateAbsolute(0x2e, asm.arg);
                break;
            case DPGX:
                asm.GenerateDirectPage(0x36, asm.arg);
                break;
            case ABSX:
                asm.GenerateAbsolute(0x3e, asm.arg);
                break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;

        }

        return true;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the ROR instruction.
    /// </summary>
    private readonly Opcode<As65> ROR = new (Keyword, "ROR", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL:
            case ACCM:
                asm.GenerateImplied(0x6A);
                break;
            case DPAG:
                asm.GenerateDirectPage(0x66, asm.arg);
                break;
            case ABSL:
                asm.GenerateAbsolute(0x6e, asm.arg);
                break;
            case DPGX:
                asm.GenerateDirectPage(0x76, asm.arg);
                break;
            case ABSX:
                asm.GenerateAbsolute(0x7e, asm.arg);
                break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;

        }

        return true;

    });

    /// <summary>
    /// The <code>Opcode</code> to handle the RTI instruction.
    /// </summary>
    private readonly Opcode<As65> RTI = new(Keyword, "RTI", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL:
                asm.GenerateImplied(0x40);
                break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return true;
    });



    /// <summary>
    /// The <code>Opcode</code> to handle the RTL instruction.
    /// </summary>
    private readonly Opcode<As65> RTL = new (Keyword, "RTL", asm => {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL:
                    asm.GenerateImplied(0x6b);
                    break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
        {
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);
        }

        return true;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the RTS instruction.
    /// </summary>
    private readonly Opcode<As65> RTS = new(Keyword, "RTS", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL:
                asm.GenerateImplied(0x60);
                break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return true;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the SBC instruction.
    /// </summary>
    private readonly Opcode<As65> SBC = new(Keyword, "SBC", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD:
                asm.GenerateImmediate(0xe9, asm.arg, asm.bitsA);
                break;
            case DPAG:
                asm.GenerateDirectPage(0xe5, asm.arg);
                break;
            case ABSL:
                asm.GenerateAbsolute(0xed, asm.arg);
                break;
            case DPGX:
                asm.GenerateDirectPage(0xf5, asm.arg);
                break;
            case ABSX:
                asm.GenerateAbsolute(0xfd, asm.arg);
                break;
            case DPGY:
            case ABSY:
                asm.GenerateAbsolute(0xf8, asm.arg);
                break;
            case INDX:
                asm.GenerateDirectPage(0xe1, asm.arg);
                break;
            case INDY:
                asm.GenerateDirectPage(0xf1, asm.arg);
                break;
            case INDI:
                if ((asm.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                {
                    asm.GenerateDirectPage(0xf2, asm.arg);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }

                break;
            case ALNG:
                if ((asm.processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateLong(0xef, asm.arg);
                }
                else
                {

                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }

                break;

            case ALGX:
                if ((asm.processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateLong(0xff, asm.arg);
                }
                else
                {

                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }

                break;

            case LIND:
                if ((asm.processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateDirectPage(0xe7, asm.arg);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }

                return true;
            case LINY:
                if ((asm.processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateDirectPage(0xf7, asm.arg);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }

                return true;
            case STAC:
                if ((asm.processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateImmediate(0xe3, asm.arg, 8);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }

                return true;

            case STKI:
                if ((asm.processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateImmediate(0xf3, asm.arg, 8);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }

                return true;

        }

        return true;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the SEC instruction.
    /// </summary>
    private readonly Opcode<As65> SEC = new(Keyword, "SEC", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL:
                asm.GenerateImplied(0x38);
                break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return true;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the SED instruction.
    /// </summary>
    private readonly Opcode<As65> SED = new(Keyword, "SED", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL:
                asm.GenerateImplied(0xf8);
                break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return true;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the SEI instruction.
    /// </summary>
    private readonly Opcode<As65> SEI = new(Keyword, "SEI", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL:
                asm.GenerateImplied(0x78);
                break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return true;
    });


    /// <summary>
    /// The <code>Opcode</code> to handle the SEP instruction.
    /// </summary>
    private readonly Opcode<As65> SEP = new(Keyword, "SEP", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMMD: asm.GenerateImmediate(0xe2, asm.arg, 8);
                    break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
        {
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);
        }

        return true;
    });

    private readonly Opcode<As65> SMB0 = new BitOperation(Keyword, "SMB0", 0x87);
    private readonly Opcode<As65> SMB1 = new BitOperation(Keyword, "SMB1", 0X97);
    private readonly Opcode<As65> SMB2 = new BitOperation(Keyword, "SMB2", 0xA7);
    private readonly Opcode<As65> SMB3 = new BitOperation(Keyword, "SMB3", 0xB7);
    private readonly Opcode<As65> SMB4 = new BitOperation(Keyword, "SMB4", 0xC7);
    private readonly Opcode<As65> SMB5 = new BitOperation(Keyword, "SMB5", 0xD7);
    private readonly Opcode<As65> SMB6 = new BitOperation(Keyword, "SMB6", 0xE7);
    private readonly Opcode<As65> SMB7 = new BitOperation(Keyword, "SMB7", 0xF7);

    /// <summary>
    /// The <code>Opcode</code> to handle the STA instruction.
    /// </summary>
    private readonly Opcode<As65> STA = new(Keyword, "STA", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case DPAG: asm.GenerateDirectPage(0x85, asm.arg);
                break;

            case ABSL:
                asm.GenerateAbsolute(0x8d, asm.arg);
                break;

            case DPGX:
                asm.GenerateDirectPage(0x95, asm.arg);
                break;

            case ABSX:
                asm.GenerateAbsolute(0x9d, asm.arg);
                break;

            case DPGY:
            case ABSY:
                asm.GenerateAbsolute(0x99, asm.arg);
                break;
            case INDX:
                asm.GenerateDirectPage(0x81, asm.arg);
                break;

            case INDI:
                if ((asm.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                {
                    asm.GenerateDirectPage(0x92, asm.arg);
                }
                else
                {
                    
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }

                break;

            case ALNG:
                if ((asm.processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateLong(0x8f, asm.arg);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }
                break;

            case ALGX:
                if ((asm.processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateLong(0x9f, asm.arg);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }
                break;

            case LIND:
                if ((asm.processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateDirectPage(0x87, asm.arg);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }
                break;

            case LINY:
                if ((asm.processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateDirectPage(0x97, asm.arg);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }
                break;

            case STAC:
                if ((asm.processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateImmediate(0x83, asm.arg, 8);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }
                break;

            case STKI:
                if ((asm.processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateImmediate(0x93, asm.arg, 8);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }
                break;

            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return true;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the STP instruction.
    /// </summary>
    private readonly Opcode<As65> STP = new(Keyword, "STP", asm =>
    {
        if ((asm.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL: asm.GenerateImplied(0xdb);
                    break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
        {
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);
        }
            
        return true;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the STX instruction.
    /// </summary>
    private readonly Opcode<As65> STX = new(Keyword, "STX", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case DPAG: asm.GenerateDirectPage(0x86, asm.arg);
                break;

            case ABSL: asm.GenerateAbsolute(0x8e, asm.arg);
                break;

            case DPGY:
                asm.GenerateDirectPage(0x96, asm.arg);
                break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return true;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the STY instruction.
    /// </summary>
    private readonly Opcode<As65> STY = new(Keyword, "STY", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case DPAG:
                asm.GenerateDirectPage(0x84, asm.arg);
                break;

            case ABSL:
                asm.GenerateAbsolute(0x8c, asm.arg);
                break;

            case DPGX:
                asm.GenerateDirectPage(0x94, asm.arg);
                break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return true;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the STZ instruction.
    /// </summary>
    private readonly Opcode<As65> STZ = new(Keyword, "STZ", asm =>
    {
        if ((asm.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case DPAG:
                    asm.GenerateDirectPage(0x64, asm.arg);
                    break;
                case ABSL:
                    asm.GenerateAbsolute(0x9c, asm.arg);
                    break;
                case DPGX:
                    asm.GenerateDirectPage(0x74, asm.arg);
                    break;
                case ABSX:
                    asm.GenerateAbsolute(0X9e, asm.arg);
                    break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
        {
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);
        }

        return true;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the TAX instruction.
    /// </summary>
    private readonly Opcode<As65> TAX = new(Keyword, "TAX", asm =>
    {
        switch (asm.ParseMode((DBANK)))
        {
            case IMPL:
                asm.GenerateImplied(0xaa);
                break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return true;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the TAY instruction.
    /// </summary>
    private readonly Opcode<As65> TAY = new(Keyword, "TAY", asm =>
    {
        switch (asm.ParseMode((DBANK)))
        {
            case IMPL: asm.GenerateImplied(0xa8);
                break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }

        return true;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the TCD instruction.
    /// </summary>
    private readonly Opcode<As65> TCD = new(Keyword, "TCD", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL: asm.GenerateImplied(0x5B); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the TCS instruction.
    /// </summary>
    private readonly Opcode<As65> TCS = new(Keyword, "TCS", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL: asm.GenerateImplied(0x1B); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the TDC instruction.
    /// </summary>
    private readonly Opcode<As65> TDC = new(Keyword, "TDC", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL: asm.GenerateImplied(0x7B); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });


    /// <summary>
    /// The <code>Opcode</code> to handle the TRB instruction.
    /// </summary>
    private readonly Opcode<As65> TRB = new(Keyword, "TRB", asm =>
    {
        if ((asm.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case DPAG: asm.GenerateDirectPage(0x14, asm.arg); break;
                case ABSL: asm.GenerateAbsolute(0x1C, asm.arg); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the TSB instruction.
    /// </summary>
    private readonly Opcode<As65> TSB = new(Keyword, "TSB", asm =>
    {
        if ((asm.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case DPAG: asm.GenerateDirectPage(0x04, asm.arg); break;
                case ABSL: asm.GenerateAbsolute(0x0C, asm.arg); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the TSC instruction.
    /// </summary>
    private readonly Opcode<As65> TSC = new(Keyword, "TSC", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL: asm.GenerateImplied(0x3b); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the TSX instruction.
    /// </summary>
    private readonly Opcode<As65> TSX = new(Keyword, "TSX", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL: asm.GenerateImplied(0xba); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the TXA instruction.
    /// </summary>
    private readonly Opcode<As65> TXA = new(Keyword, "TXA", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL: asm.GenerateImplied(0x8a); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the TXS instruction.
    /// </summary>
    private readonly Opcode<As65> TXS = new(Keyword, "TXS", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL: asm.GenerateImplied(0x9a); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the TXY instruction.
    /// </summary>
    private readonly Opcode<As65> TXY = new(Keyword, "TXY", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL: asm.GenerateImplied(0x9B); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });


    /// <summary>
    /// The <code>Opcode</code> to handle the TYA instruction.
    /// </summary>
    private readonly Opcode<As65> TYA = new(Keyword, "TYA", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL: asm.GenerateImplied(0x98); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the TYX instruction.
    /// </summary>
    private readonly Opcode<As65> TYX = new(Keyword, "TYX", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL: asm.GenerateImplied(0xbb); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the WAI instruction.
    /// </summary>
    private readonly Opcode<As65> WAI = new(Keyword, "WAI", asm =>
    {
        if ((asm.processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL: asm.GenerateImplied(0xcb); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the WDM instruction.
    /// </summary>
    private readonly Opcode<As65> WDM = new(Keyword, "WDM", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMMD: asm.GenerateImmediate(0x42, asm.arg, 8); break;
                case IMPL: asm.GenerateImplied(0x42); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
            return (true);
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the XBA instruction.
    /// </summary>
    private readonly Opcode<As65> XBA = new(Keyword, "XBA", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL: asm.GenerateImplied(0xeb); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the XCE instruction.
    /// </summary>
    private readonly Opcode<As65> XCE = new(Keyword, "XCE", asm =>
    {
        if ((asm.processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL: asm.GenerateImplied(0xfb); break;
                default:
                    asm.OnError(ERR_ILLEGAL_ADDR);
                    break;
            }
        }
        else
            asm.OnError(ERR_OPCODE_NOT_SUPPORTED);

        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the IF structured assembly command..
    /// </summary>				
    private new readonly Opcode<As65> IF = new(Keyword, "IF", asm =>
    {
        var index = asm.ifIndex++;

        asm.ifs.Push(index);

        if (asm.GetPass() == Pass.FIRST)
        {
            asm.elseAddr.Add(null);
            asm.endifAddr.Add(null);
        }

        asm.currentToken = asm.NextRealToken();

        var target = asm.elseAddr.ElementAt(index);

        if (target == null)
        {
            target = asm.endifAddr.ElementAt(index) ?? asm.GetOrigin();
        }

        if (asm.currentToken == asm.EQ) asm.GenerateBranch(asm.NE, target);
        else if (asm.currentToken == asm.NE) asm.GenerateBranch(asm.EQ, target);
        else if (asm.currentToken == asm.CC) asm.GenerateBranch(asm.CS, target);
        else if (asm.currentToken == asm.CS) asm.GenerateBranch(asm.CC, target);
        else if (asm.currentToken == asm.PL) asm.GenerateBranch(asm.MI, target);
        else if (asm.currentToken == asm.MI) asm.GenerateBranch(asm.PL, target);
        else if (asm.currentToken == asm.VC) asm.GenerateBranch(asm.VS, target);
        else if (asm.currentToken == asm.VS) asm.GenerateBranch(asm.VC, target);
        else
            asm.OnError(ERR_INVALID_CONDITIONAL);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the ELSE structured assembly
    /// command.
    /// </summary>
    private new readonly Opcode<As65> ELSE = new(Keyword, "ELSE", asm =>
    {
        if (asm.ifs.Count > 0)
        {
            var index = asm.ifs.Peek();

            var target = asm.endifAddr[index] ?? asm.GetOrigin();

            asm.GenerateJump(target);
            asm.elseAddr[index] = asm.section?.GetOrigin();
        }
        else
            asm.OnError(ERR_NO_ACTIVE_IF);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the ENDIF structured assembly
    /// command.
    /// </summary>
    private new readonly Opcode<As65> ENDIF = new(Keyword, "ENDIF", asm =>
    {
        if (asm.ifs.Count > 0)
        {
            var index = asm.ifs.Pop();

            asm.endifAddr[index] = asm.section?.GetOrigin();
        }
        else
            asm.OnError(ERR_NO_ACTIVE_IF);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the REPEAT structured assemlby
    /// command.
    /// </summary>
    private new readonly Opcode<As65> REPEAT = new(Keyword, "REPEAT", asm =>
    {
        int index = asm.loopIndex++;

        asm.loops.Push(index);

        if (asm.GetPass() == Pass.FIRST)
        {
            asm.loopAddr.Add(asm.section?.GetOrigin());
            asm.endAddr.Add(null);
        }
        else
            asm.loopAddr[index] = asm.section?.GetOrigin();

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the UNTIL structured assembly
    /// command.
    /// </summary>
    private readonly Opcode<As65> UNTIL = new(Keyword, "UNTIL", asm =>
    {
        if (asm.loops.Count > 0)
        {
            var index = asm.loops.Pop();

            asm.currentToken = asm.NextRealToken();

            var target = asm.loopAddr[index] ?? asm.GetOrigin();

            if (asm.currentToken == asm.EQ) asm.GenerateBranch(asm.NE, target);
            else if (asm.currentToken == asm.NE) asm.GenerateBranch(asm.EQ, target);
            else if (asm.currentToken == asm.CC) asm.GenerateBranch(asm.CS, target);
            else if (asm.currentToken == asm.CS) asm.GenerateBranch(asm.CC, target);
            else if (asm.currentToken == asm.PL) asm.GenerateBranch(asm.MI, target);
            else if (asm.currentToken == asm.MI) asm.GenerateBranch(asm.PL, target);
            else if (asm.currentToken == asm.VC) asm.GenerateBranch(asm.VS, target);
            else if (asm.currentToken == asm.VS) asm.GenerateBranch(asm.VC, target);
            else
                asm.OnError(ERR_INVALID_CONDITIONAL);

            asm.endAddr[index] = asm.section?.GetOrigin();
        }
        else
            asm.OnError(ERR_NO_ACTIVE_REPEAT);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the FOREVER structured assmbly
    /// command.
    /// </summary>
    private readonly Opcode<As65> FOREVER = new(Keyword, "FOREVER", asm =>
    {
        if (asm.loops.Count > 0)
        {
            var index = asm.loops.Pop();

            var target = asm.loopAddr[index];
            if (target == null) target = asm.GetOrigin();

            asm.GenerateJump(target);

            asm.endAddr[index] = asm.section.GetOrigin();
        }
        else
            asm.OnError(ERR_NO_ACTIVE_REPEAT);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the WHILE structured assembly
    /// command.
    /// </summary>
    private readonly Opcode<As65> WHILE = new(Keyword, "WHILE", asm =>
    {
        // your code here
        return false;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the ENDW structured assembly
    /// command.
    /// </summary>
    private readonly Opcode<As65> ENDW = new(Keyword, "ENDW", asm =>
    {
        // your code here
        return false;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the CONT structured assembly
    /// command.
    /// </summary>
    private readonly Opcode<As65> CONT = new(Keyword, "CONT", asm =>
    {
        // your code here
        return false;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the BREAK structured assembly
    /// command.
    /// </summary>
    private readonly Opcode<As65> BREAK = new(Keyword, "BREAK", asm =>
    {
        // your code here
        return false;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the A2STR instruction.
    /// </summary>
    private readonly Opcode<As65> A2STR = new(Keyword, "A2STR", asm =>
    {
        // your code here
        return false;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the HSTR instruction.
    /// </summary>
    private readonly Opcode<As65> HSTR = new(Keyword, "HSTR", asm =>
    {
        // your code here
        return false;
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the PSTR instruction.
    /// </summary>
    private readonly Opcode<As65> PSTR = new(Keyword, "PSTR", asm =>
    {
        // your code here
        return false;
    });

    private readonly Opcode<As65> JCC;
    private readonly Opcode<As65> JCS;
    private readonly Opcode<As65> JEQ;
    private readonly Opcode<As65> JMI;
    private readonly Opcode<As65> JNE;
    private readonly Opcode<As65> JPL;
    private readonly Opcode<As65> JVC;
    private readonly Opcode<As65> JVS;
    private readonly Opcode<As65> JPA;

    private As65() : base(new Module("65XX", false))
    {
        SetMemoryModel(new MemoryModelByte());

        JCC = new Jump("JCC", CC);
        JCS = new Jump("JCS", CS);
        JEQ = new Jump("JEQ", EQ);
        JMI = new Jump("JMI", MI);
        JNE = new Jump("JNE", NE);
        JPL = new Jump("JPL", PL);
        JVC = new Jump("JVC", VC);
        JVS = new Jump("JVS", VS);
        JPA = new Jump("JPA", null);
    }

    protected override void StartUp()
    {
        // Directives
        AddToken(P6501);
        AddToken(P6502);
        AddToken(P65C02);
        AddToken(P65SC02);
        AddToken(P65816);
        AddToken(P65832);
        AddToken(DBREG);
        AddToken(DPAGE);
        AddToken(ADDR);
        AddToken(BSS);
        AddToken(Byte);
        AddToken(DByte);
        AddToken(Word);
        AddToken(LONG);
        AddToken(Space);
        AddToken(Align);
        AddToken(Dcb);
        AddToken(CODE);
        AddToken(DATA);
        AddToken(PAGE0);
        AddToken(ORG);
        AddToken(base.ELSE);
        AddToken(End);
        AddToken(base.ENDIF);
        AddToken(ENDM);
        AddToken(ENDR);
        AddToken(Equ);
        AddToken(EXITM);
        AddToken(EXTERN);
        AddToken(GLOBAL);
        AddToken(base.IF);
        AddToken(IFABS);
        AddToken(IFNABS);
        AddToken(IFREL);
        AddToken(IFNREL);
        AddToken(IFDEF);
        AddToken(IFNDEF);
        AddToken(INCLUDE);
        AddToken(APPEND);
        AddToken(INSERT);
        AddToken(LONGA);
        AddToken(LONGI);
        AddToken(WIDEA);
        AddToken(WIDEI);
        AddToken(MACRO);
        AddToken(ON);
        AddToken(Off);
        AddToken(base.REPEAT);
        AddToken(Set);
        AddToken(LIST);
        AddToken(NOLIST);
        AddToken(PAGE);
        AddToken(TITLE);
        AddToken(ERROR);
        AddToken(WARN);

        AddToken(A2STR);
        AddToken(HSTR);
        AddToken(PSTR);

        // Functions
        AddToken(STRLEN);
        AddToken(HI);
        AddToken(LO);
        AddToken(base.BANK);

        // Opcodes & Registers
        AddToken(A);
        AddToken(ADC);
        AddToken(AND);
        AddToken(ASL);
        AddToken(BBR0);
        AddToken(BBR1);
        AddToken(BBR2);
        AddToken(BBR3);
        AddToken(BBR4);
        AddToken(BBR5);
        AddToken(BBR6);
        AddToken(BBR7);
        AddToken(BBS0);
        AddToken(BBS1);
        AddToken(BBS2);
        AddToken(BBS3);
        AddToken(BBS4);
        AddToken(BBS5);
        AddToken(BBS6);
        AddToken(BBS7);
        AddToken(BCC);
        AddToken(BCS);
        AddToken(BEQ);
        AddToken(BIT);
        AddToken(BMI);
        AddToken(BNE);
        AddToken(BPL);
        AddToken(BRA);
        AddToken(BRK);
        AddToken(BRL);
        AddToken(BVC);
        AddToken(BVS);
        AddToken(CLC);
        AddToken(CLD);
        AddToken(CLI);
        AddToken(CLV);
        AddToken(CMP);
        AddToken(COP);
        AddToken(CPX);
        AddToken(CPY);
        AddToken(DEC);
        AddToken(DEX);
        AddToken(DEY);
        AddToken(EOR);
        AddToken(HI);
        AddToken(INC);
        AddToken(INX);
        AddToken(INY);
        AddToken(JML);
        AddToken(JMP);
        AddToken(JSL);
        AddToken(JSR);
        AddToken(LO);
        AddToken(LDA);
        AddToken(LDX);
        AddToken(LDY);
        AddToken(LSR);
        AddToken(MVN);
        AddToken(MVP);
        AddToken(NOP);
        AddToken(ORA);
        AddToken(PEA);
        AddToken(PEI);
        AddToken(PER);
        AddToken(PHA);
        AddToken(PHB);
        AddToken(PHD);
        AddToken(PHK);
        AddToken(PHP);
        AddToken(PHX);
        AddToken(PHY);
        AddToken(PLA);
        AddToken(PLB);
        AddToken(PLD);
        AddToken(PLP);
        AddToken(PLX);
        AddToken(PLY);
        AddToken(REP);
        AddToken(RMB0);
        AddToken(RMB1);
        AddToken(RMB2);
        AddToken(RMB3);
        AddToken(RMB4);
        AddToken(RMB5);
        AddToken(RMB6);
        AddToken(RMB7);
        AddToken(ROL);
        AddToken(ROR);
        AddToken(RTI);
        AddToken(RTL);
        AddToken(RTS);
        AddToken(S);
        AddToken(SBC);
        AddToken(SEC);
        AddToken(SED);
        AddToken(SEI);
        AddToken(SEP);
        AddToken(SMB0);
        AddToken(SMB1);
        AddToken(SMB2);
        AddToken(SMB3);
        AddToken(SMB4);
        AddToken(SMB5);
        AddToken(SMB6);
        AddToken(SMB7);
        AddToken(STA);
        AddToken(STP);
        AddToken(STX);
        AddToken(STY);
        AddToken(STZ);
        AddToken(TAX);
        AddToken(TAY);
        AddToken(TCD);
        AddToken(TCS);
        AddToken(TDC);
        AddToken(TRB);
        AddToken(TSB);
        AddToken(TSC);
        AddToken(TSX);
        AddToken(TXA);
        AddToken(TXS);
        AddToken(TXY);
        AddToken(TYA);
        AddToken(TYX);
        AddToken(WAI);
        AddToken(WDM);
        AddToken(XBA);
        AddToken(XCE);
        AddToken(X);
        AddToken(Y);

        // Structured Assembly
        if (!traditionalOption.IsPresent)
        {
            AddToken(IF);
            AddToken(ELSE);
            AddToken(ENDIF);
            AddToken(REPEAT);
            AddToken(UNTIL);
            AddToken(FOREVER);
            AddToken(WHILE);
            AddToken(ENDW);
            AddToken(CONT);
            AddToken(BREAK);
            AddToken(EQ);
            AddToken(NE);
            AddToken(CC);
            AddToken(CS);
            AddToken(PL);
            AddToken(MI);
            AddToken(VC);
            AddToken(VS);

            // Expanding jumps
            AddToken(JCC);
            AddToken(JCS);
            AddToken(JEQ);
            AddToken(JMI);
            AddToken(JNE);
            AddToken(JPL);
            AddToken(JVC);
            AddToken(JVS);
            AddToken(JPA);
        }

        base.StartUp();
    }

    protected override bool IsSupportedPass(Pass pass)
    {
        return true;
    }

    protected override string FormatListing()
    {
        var byteCount = memory?.ByteCount;

        output.Clear();

        switch (lineType)
        {
            case '=':
                output.Append("         ");
                if (addr == null)
                    OnError("Addr is null");
                output.Append(Hex.ToHex(addr?.Resolve() ?? 0, 8));
                output.Append(addr?.IsAbsolute ?? false ? "  " : "' ");
                output.Append("        ");
                output.Append(lineType);
                output.Append(' ');
                break;

            case ' ':
                output.Append("         ");
                output.Append("        ");
                output.Append("  ");
                output.Append("        ");
                output.Append(lineType);
                output.Append(' ');
                break;

            default:
                if (IsActive && (addr != null) && ((label != null) || (lineType == ':') || (byteCount > 0)))
                {
                    long value = addr.Resolve();
                    output.Append(Hex.ToHex(value >> 16, 2));
                    output.Append(":");
                    output.Append(Hex.ToHex(value, 4));
                    output.Append(addr?.IsAbsolute == true ? "  " : "' ");

                    for (int index = 0; index < 8; ++index)
                    {
                        if (index < byteCount)
                        {
                            var code = memory?.GetByte(index) ?? 0;

                            output.Append(code >= 0 ? Hex.ToHex(code, 2) : "??");
                        }
                        else
                            output.Append("  ");
                    }
                    output.Append((byteCount > 8) ? "> " : "  ");
                    output.Append(lineType);
                    output.Append(' ');
                }
                else
                {
                    output.Append("                           ");
                    output.Append(lineType);
                    output.Append(' ');
                }
                break;
        }

        return (output.ToString());
    }

    protected override void StartPass()
    {
        base.StartPass();

        P6502.Compile(this);

        dataBank = 0;
        directPage = 0;
        bitsA = 0;
        bitsI = 0;

        sections.Add(".page0", GetModule()?.FindSection(".page0"));

        ifIndex = 0;
        loopIndex = 0;

        title = "Portable 65xx Assembler [30.00]";
    }

    protected override void EndPass()
    {
        if (ifs.Count > 0)
        {
            OnError(ERR_UNTERMINATED_IFS);
        }

        if (loops.Count > 0)
        {
            OnError(ERR_UNTERMINATED_LOOPS);
        }
    }

    protected override Token? ReadToken()
    {
        return (ScanToken());
    }

    /// <summary>
    /// A <code>StringBuffer</code> used to build up new tokens.
    /// </summary>
    private readonly StringBuilder buffer = new();

    /// <summary>
    /// Extracts the next <code>Token</code> from the source line and
    /// classifies it.
    /// </summary>
    /// <returns>The next <code>Token</code></returns>
    private Token? ScanToken()
    {
        int value = 0;

        // handle tail comments
        if (PeekChar() == ';') return EOL;

        buffer.Clear();
        var ch = NextChar();

        if (ch == '\0') return EOL;

        if (IsSpace(ch))
        {
            while (IsSpace(PeekChar()))
                NextChar();

            return WhiteSpace;
        }

        // Handle characters
        switch (ch)
        {
            case '?': return QUESTION;
            case '#': return HASH;
            case '^': return BinaryXor;
            case '-': return Minus;
            case '+': return Plus;
            case '*':
                {
                    if (PeekChar() != '=') return Times;

                    NextChar();
                    return ORG;

                }
            case '/': return Divide;
            case ';':
                {
                    // Consume comments
                    while (NextChar() != '\0')
                    {
                    }

                    return EOL;
                }

            case '%':
                {
                    if (!IsBinary(PeekChar())) return Modulo;

                    buffer.Append('%');
                    do
                    {
                        ch = NextChar();
                        buffer.Append(ch);
                        value = (value << 1) + (ch - '0');

                    } while (IsBinary(PeekChar()));

                    return new Token(Number, buffer.ToString(), value);
                }

            case '@':
                {
                    if (!IsOctal(PeekChar())) return Origin;

                    buffer.Append('@');
                    do
                    {
                        ch = NextChar();
                        buffer.Append(ch);
                        value = (value << 3) + (ch - '0');

                    } while (IsOctal(PeekChar()));

                    return new Token(Number, buffer.ToString(), value);
                }

            case '$':
                {
                    if (!IsHexadecimal(PeekChar())) return Origin;

                    buffer.Append('$');
                    do
                    {
                        ch = NextChar();
                        buffer.Append(ch);
                        value <<= 4;
                        if (char.ToLower(ch) >= 'a' && char.ToLower(ch) < 'f')
                            value += char.ToLower(ch) - 'a' + 10;
                        else
                            value += ch - '0';


                    } while (IsHexadecimal(PeekChar()));

                    return new Token(Number, buffer.ToString(), value);

                }

            case '.':
                {
                    if (IsAlphanumeric(PeekChar()))
                        break;
                    return Origin;
                }

            case '~': return Complement;
            case '=': return EQ;

            case '(': return LParen;
            case ')': return RParen;
            case '[': return LBRACKET;
            case ']': return RBRACKET;
            case ',': return Comma;
            case ':': return Colon;

            case '!':
                {
                    if (PeekChar() == '=')
                    {
                        NextChar();
                        return NE;
                    }

                    return LogicalNot;
                }

            case '&':
                {
                    if (PeekChar() == '&')
                    {
                        NextChar();
                        return LogicalAnd;
                    }

                    return BinaryAnd;
                }

            case '|':
                {
                    if (PeekChar() == '|')
                    {
                        NextChar();
                        return LogicalOr;
                    }

                    return BinaryOr;
                }

            case '<':
                {
                    switch (PeekChar())
                    {
                        case '=':
                            NextChar();
                            return Le;
                        case '<':
                            NextChar();
                            return LShift;
                    }

                    return Lt;
                }

            case '>':
                {
                    switch (PeekChar())
                    {
                        case '=':
                            NextChar();
                            return Ge;
                        case '>':
                            NextChar();
                            return RShift;
                    }

                    return Gt;
                }
        }

        // Handle numbers
        if (IsDecimal(ch))
        {
            value = ch - '0';
            while (IsDecimal(PeekChar()))
            {
                ch = NextChar();
                buffer.Append(ch);
                value = value * 10 + (ch - '0');
            }

            return new Token(Number, buffer.ToString(), value);
        }


        // Handle Symbols
        if (ch == '.' || (ch == '_') || IsAlpha(ch))
        {
            buffer.Append(ch);
            ch = PeekChar();
            while (ch == '_' || IsAlphanumeric(ch))
            {
                buffer.Append(NextChar());
                ch = PeekChar();
            }

            var symbol = buffer.ToString();

            return tokens.TryGetValue(symbol.ToUpper(), out Token? opcode) ? opcode : new Token(Symbol, symbol);
        }

        // Character Literals
        if (ch == '\'')
        {
            ch = NextChar();
            while ((ch != '\0') && (ch != '\''))
            {
                value <<= 8;
                if (ch == '\\')
                {
                    switch (PeekChar())
                    {
                        case '\t':
                            value |= '\t';
                            NextChar();
                            break;

                        case '\b':
                            value |= '\b';
                            NextChar();
                            break;

                        case '\r':
                            value |= '\r';
                            NextChar();
                            break;

                        case '\n':
                            value |= '\n';
                            NextChar();
                            break;

                        case '\\':
                            value |= '\\';
                            NextChar();
                            break;

                        case '\'':
                            value |= '\'';
                            NextChar();
                            break;

                        case '\"':
                            value |= '\"';
                            NextChar();
                            break;

                        default:
                            value |= ch;
                            break;
                    }
                }
                else
                    value |= ch;

                ch = NextChar();
            }

            if (ch != '\'') OnError(ERR_CHAR_TERM);

            return new Token(Number, "#CHAR", value);
        }

        if (ch == '\"')
        {
            while (((ch = NextChar()) != '\0') && (ch != '\"'))
            {
                if (ch == '\\')
                {
                    switch (PeekChar())
                    {
                        case 't':
                            buffer.Append('\t');
                            NextChar();
                            continue;

                        case 'b':
                            buffer.Append('\b');
                            NextChar();
                            continue;
                        case 'r':
                            buffer.Append('\r');
                            NextChar();
                            continue;
                        case 'n':
                            buffer.Append('\n');
                            NextChar();
                            continue;
                        case '\'':
                            buffer.Append('\'');
                            NextChar();
                            continue;
                        case '"':
                            buffer.Append('"');
                            NextChar();
                            continue;

                        case '\\':
                            buffer.Append("\\");
                            NextChar();
                            continue;
                    }

                    buffer.Append(ch);
                }
                else
                {
                    buffer.Append(ch);
                }
            }

            if (ch != '\"') OnError(ERR_STRING_TERM);
            return new Token(String, buffer.ToString());
        }

        buffer.Append(ch);
        return new Token(Unknown, buffer.ToString());
    }


    private static readonly string ERR_CHAR_TERM = "Unterminated character constant";
    private static readonly string ERR_STRING_TERM = "Unterminated string constant";
    private static readonly string ERR_ILLEGAL_ADDR = "Illegal addressing mode";
    private static readonly string ERR_OPCODE_NOT_SUPPORTED = "Opcode not supported by current processor type";
    private static readonly string ERR_MODE_NOT_SUPPORTED = "Addressing model not supported by current processor type";

    private static readonly string ERR_TEXT_TOO_LONG_FOR_IMMD =
        "Text literal is too long to be used in an immediate expression";

    private static readonly string ERR_NO_ACTIVE_IF = "No active IF for ELSE/ENDIF";
    private static readonly string ERR_NO_ACTIVE_REPEAT = "No active REPEAT for UNTIL";
    private static readonly string ERR_NO_ACTIVE_WHILE = "No active WHILE for ENDW";
    private static readonly string ERR_NO_ACTIVE_LOOP = "No active REPEAT or WHILE for BREAK";
    private static readonly string ERR_65816_ONLY = "Directive supported for 65816 and 65832 processors only";
    private static readonly string ERR_65832_ONLY = "Directive supported for 65832 processor only";
    private static readonly string ERR_INVALID_CONDITIONAL = "Invalid conditional flag";
    private static readonly string ERR_EXPECTED_ON_OR_OFF = "Expected ON or OFF";
    private static readonly string ERR_EXPECTED_COMMA = "Expected a comma";
    private static readonly string ERR_UNTERMINATED_IFS = "Unterminated IF statement(s) in source code";
    private static readonly string ERR_UNTERMINATED_LOOPS = "Unterminated REPEAT/WHILE statement(s) in source code";
    private static readonly string ERR_EXPECTED_X = "Expected X index";
    private static readonly string ERR_EXPECTED_Y = "Expected Y index";
    private static readonly string ERR_EXPECTED_X_OR_Y = "Expected either X or Y index";
    private static readonly string ERR_EXPECTED_X_OR_S = "Expected either X or S index";
    private static readonly string ERR_EXPECTED_S_OR_X_OR_Y = "Expected S, X or Y index";
    private static readonly string ERR_EXPECTED_CLOSING_BRACKET = "Expected closing bracket";
    private static readonly string ERR_EXPECTED_CLOSING_PARENTHESIS = "Expected closing parenthesis";
    private static readonly string ERR_MISSING_EXPRESSION = "Missing expression";

    // Represents an invalid address mode
    private const int UNKN = 0;

    // Represents the implied address mode
    private const int IMPL = 1;

    // Represents the immediate address mode
    private const int IMMD = 2;

    // Represents the accumulator addressing mode
    private const int ACCM = 3;

    // Represents the direct page addressing mode
    private const int DPAG = 4;

    // Represents the diret page indexed addressing mode
    private const int DPGX = 5;

    // Represents the direct page indexed addressing mode
    private const int DPGY = 6;

    // Represents the absolute addressing mode
    private const int ABSL = 7;

    // Represents the absolute indexed addressing mode
    private const int ABSX = 8;

    // Represents the absolute indexed addressing mode
    private const int ABSY = 9;

    // Represents the indirect addressing mode
    private const int INDI = 10;

    // Represents the indirect indexed addressing mode
    private const int INDX = 11;

    // Represents the indirect indexed addressing mode
    private const int INDY = 12;

    // Represents the stack addressing mode
    private const int STAC = 13;

    // Represents the stack indirect addressing mode
    private const int STKI = 14;

    // Represents the absolute long addressing mode
    private const int ALNG = 15;

    // Represents the absolute long indexed addressing mode
    private const int ALGX = 16;

    // Represents the absolute long indirect addressing mode
    private const int LIND = 17;

    // Represents the absolute long indirect indexed addressing mode
    private const int LINY = 18;

    /// <summary>
    /// A constant value used for TRUE
    /// </summary>
    private static readonly Value True = new(null, 1);

    /// <summary>
    /// A constant value used for FALSE
    /// </summary>
    private static readonly Value False = new(null, 0);

    //  A constant value used in relative address calculations.
    private static readonly Value TWO = new(null, 2);

    //  A constant value used in long relative address calculations.
    private static readonly Value THREE = new(null, 3);

    // A constant value used to skip over branches.
    private static readonly Value FIVE = new(null, 5);

    //  A constant value used in shifts.
    private static readonly Value EIGHT = new(null, 8);

    //  A constant value used in bank number calculations.
    private static readonly Value SIXTEEN = new(null, 16);

    //  A constant value used in Apple ][ string generation.
    private static readonly Value HI_BIT = new(null, 0x80);

    // A constant value used in bank number calculations.
    private new static readonly Value BANK = new(null, 0x00ff0000);

    // A constant value used in bank offset calculations.
    private static readonly Value OFFSET = new(null, 0x0000ffff);

    /// <summary>
    /// The current processor mask.
    /// <see cref="M6501"/>
    /// <see cref="M6502"/>
    /// <see cref="M65C02"/>
    /// <see cref="M65SC02"/>
    /// <see cref="M65816"/>
    /// </summary>
    private int processor = 0;

    // The current argument
    private Expr? arg;

    // The current data bank (65816 only).
    private int dataBank;

    // The current direct page value (65816 only).
    private int directPage;

    // A flag indicating the number of bits in the A register.
    private int bitsA;

    // A flag indicating the number of bits in the X and Y registers.
    private int bitsI;

    /// <summary>
    /// Determines the addressing mode used by the instruction.
    /// </summary>
    /// <param name="targetBank">The target bank value.</param>
    /// <returns>The address mode.</returns>
    private int ParseMode(int targetBank)
    {
        currentToken = NextRealToken();

        if (currentToken == EOL) return IMPL;

        // Handle Accumulator
        if (currentToken == A)
        {
            currentToken = NextRealToken();
            arg = null;
            return ACCM;
        }

        // Handle Immediate
        if (currentToken == HASH)
        {
            currentToken = NextRealToken();
            if (currentToken == Lt)
            {
                currentToken = NextRealToken();
                arg = ParseImmediate();
            }
            else if (currentToken == Gt)
            {
                currentToken = NextRealToken();
                arg = Expr.Shr(ParseImmediate(), EIGHT);
            }
            else if (currentToken == BinaryXor)
            {
                currentToken = NextRealToken();
                arg = Expr.Shr(ParseImmediate(), SIXTEEN);
            }
            else
                arg = ParseImmediate();

            return IMMD;
        }

        // Handle <.. <..,X <..,Y
        if (currentToken == Lt)
        {
            currentToken = NextRealToken();
            arg = ParseExpression();

            if (arg == null)
                OnError(ERR_MISSING_EXPRESSION);

            if (currentToken == Comma)
            {
                currentToken = NextRealToken();
                if (currentToken == X)
                {
                    currentToken = NextRealToken();
                    return DPGX;
                }

                if (currentToken == Y)
                {
                    currentToken = NextRealToken();
                    return DPGY;
                }

                OnError(ERR_EXPECTED_X_OR_Y);
                return UNKN;
            }

            return DPAG;
        }

        // Handle >.. and >..,X
        if (currentToken == Gt)
        {
            currentToken = NextRealToken();
            arg = ParseExpression();

            if (arg == null)
                OnError(ERR_MISSING_EXPRESSION);

            if (currentToken == Comma)
            {
                currentToken = NextRealToken();
                if (currentToken == X)
                {
                    currentToken = NextRealToken();
                    return ALGX;
                }

                OnError(ERR_EXPECTED_X);
                return UNKN;
            }

            return ALNG;
        }

        // Handle [..] and [..],Y
        if (currentToken == LBRACKET)
        {
            currentToken = NextRealToken();
            arg = ParseExpression();

            if (arg == null)
                OnError(ERR_MISSING_EXPRESSION);

            if (currentToken == RBRACKET)
            {
                currentToken = NextRealToken();
                if (currentToken == Comma)
                {
                    currentToken = NextRealToken();
                    if (currentToken == Y)
                    {
                        currentToken = NextRealToken();
                        return LINY;
                    }

                    OnError(ERR_EXPECTED_Y);
                    return UNKN;
                }

                return LIND;
            }

            OnError(ERR_EXPECTED_CLOSING_BRACKET);
            return UNKN;
        }

        // Handle (..,X) (..),Y, (..,S),Y and (..)
        if (currentToken == LParen)
        {
            currentToken = NextRealToken();
            arg = ParseExpression();

            if (arg == null)
                OnError(ERR_MISSING_EXPRESSION);

            if (currentToken == Comma)
            {
                currentToken = NextRealToken();
                if (currentToken == X)
                {
                    currentToken = NextRealToken();
                    if (currentToken == RParen)
                    {
                        currentToken = NextRealToken();
                        return INDX;
                    }

                    OnError(ERR_EXPECTED_CLOSING_PARENTHESIS);
                    return UNKN;
                }

                if (currentToken == S)
                {
                    currentToken = NextRealToken();
                    if (currentToken == RParen)
                    {
                        currentToken = NextRealToken();
                        if (currentToken == Comma)
                        {
                            currentToken = NextRealToken();
                            if (currentToken == Y)
                            {
                                currentToken = NextRealToken();
                                return STKI;
                            }

                            OnError(ERR_EXPECTED_Y);
                            return UNKN;
                        }

                        OnError(ERR_EXPECTED_COMMA);
                        return UNKN;
                    }

                    OnError(ERR_EXPECTED_CLOSING_PARENTHESIS);
                    return UNKN;
                }

                OnError(ERR_EXPECTED_X_OR_S);
                return UNKN;
            }

            if (currentToken == RParen)
            {
                currentToken = NextRealToken();
                if (currentToken == Comma)
                {
                    currentToken = NextRealToken();
                    if (currentToken == Y)
                    {
                        currentToken = NextRealToken();
                        return INDY;
                    }

                    OnError(ERR_EXPECTED_Y);
                    return UNKN;
                }

                return INDI;
            }

            return UNKN;
        }

        // Handle |.., |..,X and |..,Y or !.., !..,X and !..,Y
        if (currentToken == BinaryOr || currentToken == LogicalNot)
        {
            currentToken = NextRealToken();
            arg = ParseExpression();

            if (arg == null)
                OnError(ERR_MISSING_EXPRESSION);

            if (currentToken == Comma)
            {
                currentToken = NextRealToken();
                if (currentToken == X)
                {
                    currentToken = NextRealToken();
                    return ABSX;
                }

                if (currentToken == Y)
                {
                    currentToken = NextRealToken();
                    return ABSY;
                }

                OnError(ERR_EXPECTED_X_OR_Y);
                return UNKN;
            }

            return ABSL;
        }

        // Handle .. ..,X ..,Y and ..,S
        arg = ParseExpression();

        if (arg == null)
        {
            OnError(ERR_MISSING_EXPRESSION);
            return ABSL;
        }

        if (currentToken == Comma)
        {
            currentToken = NextRealToken();
            if (currentToken == X)
            {
                currentToken = NextRealToken();
                return arg.IsAbsolute && IsByteAddress((int)arg.Resolve()) ? DPGX : ABSX;
            }

            if (currentToken == Y)
            {
                currentToken = NextRealToken();
                return arg.IsAbsolute && IsByteAddress((int)arg.Resolve()) ? DPGY : ABSY;
            }

            if (currentToken == S)
            {
                currentToken = NextRealToken();
                return STAC;
            }

            OnError(ERR_EXPECTED_S_OR_X_OR_Y);
            return UNKN;
        }

        if (arg.IsAbsolute)
        {
            long address = arg.Resolve();

            if ((processor & (M65816 | M65832)) != 0)
            {
                if ((address & 0xff0000) == 0)
                {
                    return IsByteAddress((int)address) ? DPAG : ABSL;
                }
                else
                {
                    if (targetBank == PBANK)
                    {
                        var origin = GetOrigin();

                        if (origin?.IsAbsolute == true)
                            return ((origin.Resolve() ^ address) & 0xff0000) == 0 ? ABSL : ALNG;
                        return ALNG;
                    }

                    return ((address & 0xff0000) >> 16) == dataBank ? ABSL : ALNG;
                }
            }
            else
                return (address & 0xff00) == 0 ? DPAG : ABSL;
        }
        else if (arg.IsExternal(GetOrigin()?.GetSection()))
            return (processor & (M65816 | M65832)) != 0 ? ALNG : ABSL;
        else
            return ABSL;
    }

    /// <summary>
    /// Parses the data value for an immediate addressing mode to allow short
    /// string literals as well as numbers.
    /// </summary>
    /// <returns>An expression containing the immediate value.</returns>
    private Expr? ParseImmediate()
    {
        if (currentToken?.Kind == String)
        {
            var text = currentToken.Text;

            if (text.Length > 4)
                OnError(ERR_TEXT_TOO_LONG_FOR_IMMD);

            var value = text.Aggregate(0, (current, t) => (current << 8) | t);

            currentToken = NextRealToken();

            return (new Value(null, value));
        }

        var result = ParseExpression();

        if (result == null)
            OnError(ERR_MISSING_EXPRESSION);

        return (result);
    }

    /// <summary>
    /// Generate the code for an implied instruction
    /// </summary>
    /// <param name="opcode">The opcode byte</param>
    private void GenerateImplied(int opcode)
    {
        AddByte((byte)opcode);
    }

    /// <summary>
    /// Generate the code for an immediate instruction
    /// </summary>
    /// <param name="opcode">The opcode byte</param>
    /// <param name="expr">The immediate value</param>
    /// <param name="bits">Determines if an 8 or 16 bit value.</param>
    private void GenerateImmediate(int opcode, Expr? expr, int bits)
    {
        AddByte((byte)opcode);
        switch (bits)
        {
            default:
                OnError("Undefined memory size");
                AddByte(expr);
                break;

            case 8:
                AddByte(expr);
                break;
            case 16:
                AddWord(expr);
                break;

            case 32:
                AddLong(expr);
                break;

        }
    }

    /// <summary>
    /// Generate the code for an instruction with a direct page address.
    /// </summary>
    /// <param name="opcode">The opcode byte</param>
    /// <param name="expr">The address expression</param>
    private void GenerateDirectPage(int opcode, Expr? expr)
    {
        AddByte((byte)opcode);

        if ((directPage != 0) && (processor == M65816))
            arg = Expr.Sub(expr, new Value(null, directPage));

        AddByte(arg);
    }

    /// <summary>
    /// Generate the code for an instruction with a absolute address.
    /// </summary>
    /// <param name="opcode">The opcode byte.</param>
    /// <param name="expr">The address expression.</param>
    private void GenerateAbsolute(int opcode, Expr? expr)
    {
        AddByte((byte)opcode);
        AddWord(expr);
    }

    /// <summary>
    /// Generate the code for an instruction with in indirect address.
    /// </summary>
    /// <param name="opcode">The opcode byte.</param>
    /// <param name="expr">The address expression.</param>
    /// <param name="isLong">Determines if an 8 or 16 bit value.</param>
    private void GenerateIndirect(int opcode, Expr? expr, bool isLong)
    {
        if (isLong)
            GenerateAbsolute(opcode, expr);
        else
            GenerateDirectPage(opcode, expr);
    }

    /// <summary>
    /// Generate the code for an instruction with a relative address.
    /// </summary>
    /// <param name="opcode">The opcode byte.</param>
    /// <param name="expr">The address expression.</param>
    /// <param name="isLong">Determines if an 8 or 16 bit value.</param>
    private void GenerateRelative(int opcode, Expr? expr, bool isLong)
    {
        var origin = GetOrigin();

        if (origin != null)
        {
            AddByte((byte)opcode);
            if (isLong)
            {
                var dist = Expr.Sub(expr, Expr.Add(origin, THREE));
                if (GetPass() == Pass.FINAL)
                {
                    if (dist.IsAbsolute && ((dist.Resolve() < -32768) || (dist.Resolve() > 32767)))
                        OnError("Relative branch is out of range");
                }

                AddWord(dist);
            }
            else
            {
                var dist = Expr.Sub(expr, Expr.Add(origin, TWO));
                if (GetPass() == Pass.FINAL)
                {
                    if (dist.IsAbsolute && ((dist.Resolve() < -128) || (dist.Resolve() > 127)))
                        OnError("Relative branch is out of range");
                }

                AddByte(dist);
            }
        }
        else
            OnError("No active section");
    }

    /// <summary>
    /// Generate the code for an instruction with a long address.
    /// </summary>
    /// <param name="opcode">The opcode byte.</param>
    /// <param name="expr">The address expression.</param>
    private void GenerateLong(int opcode, Expr? expr)
    {
        AddByte((byte)opcode);
        AddAddress(expr);
    }

    /// <summary>
    /// Generates a conditional branch to the target location
    /// using relative instructions if possible.
    /// </summary>
    /// <param name="condition">The condition causing the branch.</param>
    /// <param name="target">The target address.</param>
    private void GenerateBranch(Token condition, Expr? target)
    {
        if (IsShortDistance(target))
        {
            if (condition == EQ) GenerateRelative(0xF0, target, false);
            if (condition == NE) GenerateRelative(0xD0, target, false);
            if (condition == CC) GenerateRelative(0x90, target, false);
            if (condition == CS) GenerateRelative(0xB0, target, false);
            if (condition == PL) GenerateRelative(0x10, target, false);
            if (condition == MI) GenerateRelative(0x30, target, false);
            if (condition == VC) GenerateRelative(0x50, target, false);
            if (condition == VS) GenerateRelative(0x70, target, false);
        }
        else
        {
            var skipOver = Expr.Add(GetOrigin(), FIVE);

            if (condition == EQ) GenerateRelative(0xD0, skipOver, false);
            if (condition == NE) GenerateRelative(0xF0, skipOver, false);
            if (condition == CC) GenerateRelative(0xB0, skipOver, false);
            if (condition == CS) GenerateRelative(0x90, skipOver, false);
            if (condition == PL) GenerateRelative(0x30, skipOver, false);
            if (condition == MI) GenerateRelative(0x10, skipOver, false);
            if (condition == VC) GenerateRelative(0x70, skipOver, false);
            if (condition == VS) GenerateRelative(0x50, skipOver, false);

            if ((processor & (M65816 | M65832)) != 0)
                GenerateRelative(0x82, Expr.Sub(target, TWO), true);
            else
                GenerateAbsolute(0x4C, target);
        }
    }

    /// <summary>
    /// Generates a jump to a target address using BRA if supported
    /// and within range.
    /// </summary>
    /// <param name="target">The target address.</param>
    private void GenerateJump(Expr? target)
    {
        if (HasShortBranch() && IsShortDistance(target))
            GenerateRelative(0x80, target, false);
        else
        {
            if ((processor & (M65816 | M65832)) != 0)
                GenerateRelative(0x82, target, true);
            else
                GenerateAbsolute(0x4C, target);
        }
    }

    /// <summary>
    /// Determines if a target address is within relative branch
    /// range.
    /// </summary>
    /// <param name="target">The target address.</param>
    /// <returns>return <code>true</code> if the target address is near</returns>
    private bool IsShortDistance(Expr? target)
    {
        var offset = Expr.Sub(target, Expr.Add(GetOrigin(), TWO));

        if (!offset.IsAbsolute) return (false);
        var distance = (int)offset.Resolve();

        return distance is >= -128 and <= 127;
    }

    /// <summary>
    /// Determines if the current processor supports the BRA opcode.
    /// </summary>
    /// <returns><code>true</code> if BRA is supported.</returns>
    private bool HasShortBranch()
    {
        return ((processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0);
    }

    /// <summary>
    /// Generate the series of bytes for a 24 bit address.
    /// </summary>
    /// <param name="expr">An expression representing the address.</param>
    private void AddAddress(Expr? expr)
    {
        AddWord(Expr.And(expr, OFFSET));
        AddByte(Expr.Shr(Expr.And(expr, BANK), SIXTEEN));
    }

    /// <summary>
    /// The <code>Option</code> instance use to detect <code>-traditional</code>
    /// </summary>
    private readonly Option traditionalOption = new("-traditional", "Disables structured directives");

    // A Dictionary of keyword tokens to speed up classification
    private readonly Dictionary<string, Token?> tokens = new();

    /// <summary>
    /// A <code>StringBuffer</code> used to format output.
    /// </summary>
    private readonly StringBuilder output = new();

    private int ifIndex;

    private int loopIndex;

    private readonly Stack<int> ifs = new();

    private readonly Stack<int> loops = new();

    private readonly List<Value?> elseAddr = new();

    private readonly List<Value?> endifAddr = new();

    private List<Value?> loopAddr = new();

    private List<Value?> endAddr = new();

    /// <summary>
    /// Adds a currentToken to the hash table indexed by its text in UPPER case.
    /// </summary>
    /// <param name="token">The Token to add</param>
    private void AddToken(Token token)
    {
        tokens.Add(token.Text.ToUpper(), token);
    }

    /// <summary>
    /// Determines if an address can be represented by a byte.
    /// </summary>
    /// <param name="value">The value to be tested.</param>
    /// <returns>true if the value is a byte, false otherwise.</returns>
    private bool IsByteAddress(int value)
    {
        return ((directPage <= value) && (value <= (directPage + 0xff)));
    }





}