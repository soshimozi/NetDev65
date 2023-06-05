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
    public static void AssemblerMain(string[] args)
    {
        new As65().Run(args);
    }

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
    // ReSharper disable once UnusedMember.Local
    private const int ANY = M6501 | M6502 | M65C02 | M65SC02 | M65816 | M65832;

    // Indicates that address mode is being parsed for bank 0
    // ReSharper disable once UnusedMember.Local
    private const int BANK0 = 0;

    // Indicates that address mode is being parsed for the current data back.
    private const int DBANK = 1;

    // Indicates that address mode is being parsed for the current program bank.
    private const int PBANK = 2;


    private readonly Opcode P6501 = new(Keyword, ".6501",
        assembler =>
        {
            assembler.Processor = M6501;

            assembler.DoSet("__6501__", True);
            assembler.DoSet("__6502__", False);
            assembler.DoSet("__65C02__", False);
            assembler.DoSet("__65SC02__", False);
            assembler.DoSet("__65816__", False);
            assembler.DoSet("__65832__", False);

            assembler.BitsA = 8;
            assembler.BitsI = 8;

            return false;
        });

    private readonly Opcode P6502 = new(Keyword, ".6502", as65 =>
    {
        as65.Processor = M6502;

        as65.DoSet("__6501__", False);
        as65.DoSet("__6502__", True);
        as65.DoSet("__65C02__", False);
        as65.DoSet("__65SC02__", False);
        as65.DoSet("__65816__", False);
        as65.DoSet("__65832__", False);

        as65.BitsA = 8;
        as65.BitsI = 8;

        return (false);
    });


    private readonly Opcode P65C02 = new(Keyword, ".65C02", as65 =>
    {
        as65.Processor = M65C02;

        as65.DoSet("__6501__", False);
        as65.DoSet("__6502__", False);
        as65.DoSet("__65C02__", True);
        as65.DoSet("__65SC02__", False);
        as65.DoSet("__65816__", False);
        as65.DoSet("__65832__", False);

        as65.BitsA = 8;
        as65.BitsI = 8;

        return (false);
    });

    private readonly Opcode P65SC02 = new(Keyword, ".65SC02", as65 =>
    {
        as65.Processor = M65SC02;

        as65.DoSet("__6501__", False);
        as65.DoSet("__6502__", False);
        as65.DoSet("__65C02__", False);
        as65.DoSet("__65SC02__", True);
        as65.DoSet("__65816__", False);
        as65.DoSet("__65832__", False);

        as65.BitsA = 8;
        as65.BitsI = 8;

        return (false);

    });

    private readonly Opcode P65816 = new(Keyword, ".65816", as65 =>
    {
        as65.Processor = M65816;
        as65.DoSet("__6501__", False);
        as65.DoSet("__6502__", False);
        as65.DoSet("__65C02__", False);
        as65.DoSet("__65SC02__", False);
        as65.DoSet("__65816__", True);
        as65.DoSet("__65832__", False);

        as65.BitsA = 8;
        as65.BitsI = 8;

        return (false);
    });


    private readonly Opcode P65832 = new(Keyword, ".65832", as65 =>
    {
        as65.Processor = M65816;
        as65.DoSet("__6501__", False);
        as65.DoSet("__6502__", False);
        as65.DoSet("__65C02__", False);
        as65.DoSet("__65SC02__", False);
        as65.DoSet("__65816__", False);
        as65.DoSet("__65832__", True);

        as65.BitsA = 8;
        as65.BitsI = 8;

        return (false);
    });

    private readonly Opcode PAGE0 = new(Keyword, ".PAGE0", as65 =>
    {
        as65.SetSection(".page0");
        return false;
    });

    private readonly Opcode DBREG = new(Keyword, ".DBREG", as65 =>
    {
        if ((as65.Processor & (M65816 | M65832)) != 0)
        {
            as65.CurrentToken = as65.NextRealToken();
            var expr = as65.ParseExpression();

            if (expr?.IsAbsolute == true)
                as65.DataBank = (int)expr.Resolve() & 0xff;
            else
                as65.OnError(ErrorMessage.ERR_CONSTANT_EXPR);
        }
        else
            as65.OnError(ERR_65816_ONLY);

        return (false);
    });

    private readonly Opcode DPAGE = new(Keyword, ".DPAGE", as65 =>
    {
        if ((as65.Processor & (M65816 | M65832)) != 0)
        {
            as65.CurrentToken = as65.NextRealToken();
            var expr = as65.ParseExpression();

            if (expr?.IsAbsolute == true)
                as65.DirectPage = (int)expr.Resolve() & 0xffff;
            else
                as65.OnError(ErrorMessage.ERR_CONSTANT_EXPR);
        }
        else
            as65.OnError(ERR_65816_ONLY);

        return (false);
    });

    private readonly Opcode LONGA = new(Keyword, ".LONGA", assembler =>
    {
        if ((assembler.Processor & (M65816 | M65832)) != 0)
        {
            assembler.CurrentToken = assembler.NextRealToken();
            if (assembler.CurrentToken == As65.QUESTION)
            {
                assembler.BitsA = -1;
                assembler.CurrentToken = assembler.NextRealToken();
            }
            else if ((assembler.CurrentToken == As65.ON) || (assembler.CurrentToken == As65.Off))
            {
                assembler.BitsA = (assembler.CurrentToken == As65.ON) ? 16 : 8;
                assembler.CurrentToken = assembler.NextRealToken();
            }
            else
                assembler.OnError(ERR_EXPECTED_ON_OR_OFF);
        }
        else
            assembler.OnError(ERR_65816_ONLY);

        return (false);

    });

    private readonly Opcode LONGI = new(Keyword, ".LONGI", assembler =>
    {
        if ((assembler.Processor & (M65816 | M65832)) != 0)
        {
            assembler.CurrentToken = assembler.NextRealToken();
            if (assembler.CurrentToken == As65.QUESTION)
            {
                assembler.BitsI = -1;
                assembler.CurrentToken = assembler.NextRealToken();
            }
            else if ((assembler.CurrentToken == As65.ON) || (assembler.CurrentToken == As65.Off))
            {
                assembler.BitsI = (assembler.CurrentToken == As65.ON) ? 16 : 8;
                assembler.CurrentToken = assembler.NextRealToken();
            }
            else
                assembler.OnError(ERR_EXPECTED_ON_OR_OFF);
        }
        else
            assembler.OnError(ERR_65816_ONLY);

        return (false);

    });

    private readonly Opcode WIDEA = new(Keyword, ".WIDEA", assembler =>
    {
        if (assembler.Processor == M65832)
        {
            assembler.CurrentToken = assembler.NextRealToken();
            if (assembler.CurrentToken == As65.QUESTION)
            {
                assembler.BitsA = -1;
                assembler.CurrentToken = assembler.NextRealToken();
            }

            if ((assembler.CurrentToken == As65.ON) || (assembler.CurrentToken == As65.Off))
            {
                assembler.BitsA = (assembler.CurrentToken == As65.ON) ? 32 : 8;
                assembler.CurrentToken = assembler.NextRealToken();
            }
            else
                assembler.OnError(ERR_EXPECTED_ON_OR_OFF);
        }
        else
            assembler.OnError(ERR_65832_ONLY);

        return (false);
    });

    private readonly Opcode WIDEI = new(Keyword, ".WIDEI", assembler =>
    {
        if (assembler.Processor == M65832)
        {
            assembler.CurrentToken = assembler.NextRealToken();
            if (assembler.CurrentToken == As65.QUESTION)
            {
                assembler.BitsA = -1;
                assembler.CurrentToken = assembler.NextRealToken();
            }
            else if (assembler.CurrentToken == As65.ON || assembler.CurrentToken == As65.Off)
            {
                assembler.BitsI = (assembler.CurrentToken == As65.ON) ? 32 : 8;
                assembler.CurrentToken = assembler.NextRealToken();
            }
            else
            {
                assembler.OnError(ERR_EXPECTED_ON_OR_OFF);
            }
        }
        else
        {
            assembler.OnError(ERR_65832_ONLY);
        }

        return false;
    });
    private readonly Opcode ADDR = new(Keyword, ".ADDR", as65 =>
    {
        do
        {
            as65.CurrentToken = as65.NextRealToken();
            var expr = as65.ParseExpression();

            if (expr != null)
                as65.AddAddress(expr);
            else
                as65.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        } while (as65.CurrentToken == Comma);

        if (as65.CurrentToken != EOL) as65.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        return (true);
    });

    /// <summary>
    /// A <see cref="Token"/> representing the '?' character.
    /// </summary>
    private static readonly Token QUESTION = new(Keyword, "?");

    /// <summary>
    /// A <see cref="Token"/> representing the '#' character.
    /// </summary>
    private static readonly Token HASH = new(Keyword, "#");

    /// <summary>
    /// A <see cref="Token"/> representing the A register.
    /// </summary>
    private static readonly Token A = new(Keyword, "A");

    /// <summary>
    /// A <see cref="Token"/> representing the S register.
    /// </summary>
    private static readonly Token S = new(Keyword, "S");

    /// <summary>
    /// A <see cref="Token"/> representing the X register.
    /// </summary>
    private static readonly Token X = new(Keyword, "X");

    /// <summary>
    /// A <see cref="Token"/> representing the Y register.
    /// </summary>
    private static readonly Token Y = new(Keyword, "Y");

    /// <summary>
    /// A <see cref="Token"/> representing the ON keyword.
    /// </summary>
    private static readonly Token ON = new(Keyword, "ON");

    /// <summary>
    /// A <see cref="Token"/> representing the OFF keyword.
    /// </summary>
    private static readonly Token Off = new(Keyword, "OFF");

    /// <summary>
    /// A <see cref="Token"/> representing the '[' character.
    /// </summary>
    private static readonly Token LBRACKET = new(Keyword, "[");

    /// <summary>
    /// A <see cref="Token"/> representing the ']' character.
    /// </summary>
    private static readonly Token RBRACKET = new(Keyword, "]");

    /// <summary>
    /// A <see cref="Token"/> representing the EQ keyword.
    /// </summary>
    private static readonly Token EQ = new(Keyword, "EQ");

    /// <summary>
    /// A <see cref="Token"/> representing the NE keyword.
    /// </summary>
    private static readonly Token NE = new(Keyword, "NE");

    /// <summary>
    /// A <see cref="Token"/> representing the CC keyword.
    /// </summary>
    private static readonly Token CC = new(Keyword, "CC");

    /// <summary>
    /// A <see cref="Token"/> representing the CS keyword.
    /// </summary>
    private static readonly Token CS = new(Keyword, "CS");

    /// <summary>
    /// A <see cref="Token"/> representing the PL keyword.
    /// </summary>
    private static readonly Token PL = new(Keyword, "PL");

    /// <summary>
    /// A <see cref="Token"/> representing the MI keyword.
    /// </summary>
    private static readonly Token MI = new(Keyword, "MI");

    /// <summary>
    /// A <see cref="Token"/> representing the VC keyword.
    /// </summary>
    private static readonly Token VC = new(Keyword, "VC");

    /// <summary>
    /// A <see cref="Token"/> representing the VS keyword.
    /// </summary>
    private static readonly Token VS = new(Keyword, "VS");

    private sealed class Jump : Opcode
    {
        private readonly Token? _flag;

        public Jump(string mnemonic, Token? flag) : base(Keyword, mnemonic)
        {
            _flag = flag;
        }

        public override bool Compile(IAssembler assembler)
        {
            assembler.CurrentToken = assembler.NextRealToken();

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
    private sealed class BitOperation : Opcode
    {
        private readonly int _opcode;

        public BitOperation(TokenKind kind, string text, int opcode) : base(kind, text)
        {
            _opcode = opcode;
        }

        public override bool Compile(IAssembler assembler)
        {
            if ((assembler.Processor & (M6501 | M65C02)) != 0)
            {
                assembler.CurrentToken = assembler.NextRealToken();
                var addr = assembler.ParseExpression();
                if (assembler.Origin != null)
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
    private sealed class BitBranch : Opcode
    {
        private readonly int _opcode;

        public BitBranch(TokenKind kind, string text, int opcode) : base(kind, text)
        {
            _opcode = opcode;
        }

        public override bool Compile(IAssembler assembler)
        {
            if ((assembler.Processor & (M6501 | M65C02)) != 0)
            {
                assembler.CurrentToken = assembler.NextRealToken();

                var addr = assembler.ParseExpression();

                if (assembler.CurrentToken == Comma)
                {
                    assembler.CurrentToken = assembler.NextRealToken();
                }
                else
                {
                    assembler.OnError("Expected comma");
                }

                var jump = assembler.ParseExpression();
                var origin = assembler.Origin;

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
    private readonly Opcode ADC = new(Keyword, "ADC", as65 =>
    {
        switch (as65.ParseMode(DBANK))
        {
            case IMMD:
                as65.GenerateImmediate(0x69, as65.Arg, as65.BitsA);
                break;
            case DPAG:
                as65.GenerateDirectPage(0x65, as65.Arg);
                break;
            case ABSL:
                as65.GenerateAbsolute(0x6d, as65.Arg);
                break;
            case DPGX:
                as65.GenerateDirectPage(0x75, as65.Arg);
                break;
            case ABSX:
                as65.GenerateAbsolute(0x7d, as65.Arg);
                break;
            case DPGY:
            case ABSY:
                as65.GenerateAbsolute(0x79, as65.Arg);
                break;
            case INDX:
                as65.GenerateDirectPage(0x61, as65.Arg);
                break;
            case INDY:
                as65.GenerateDirectPage(0x71, as65.Arg);
                break;
            case INDI:
                if ((as65.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    as65.GenerateDirectPage(0x72, as65.Arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALNG:
                if ((as65.Processor & (M65816 | M65832)) != 0)
                    as65.GenerateLong(0x6f, as65.Arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALGX:
                if ((as65.Processor & (M65816 | M65832)) != 0)
                    as65.GenerateLong(0x7f, as65.Arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LIND:
                if ((as65.Processor & (M65816 | M65832)) != 0)
                    as65.GenerateDirectPage(0x67, as65.Arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LINY:
                if ((as65.Processor & (M65816 | M65832)) != 0)
                    as65.GenerateDirectPage(0x77, as65.Arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STAC:
                if ((as65.Processor & (M65816 | M65832)) != 0)
                    as65.GenerateImmediate(0x63, as65.Arg, 8);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STKI:
                if ((as65.Processor & (M65816 | M65832)) != 0)
                    as65.GenerateImmediate(0x73, as65.Arg, 8);
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
    private readonly Opcode AND = new(Keyword, "AND", as65 =>
    {
        switch (as65.ParseMode(DBANK))
        {
            case IMMD:
                as65.GenerateImmediate(0x29, as65.Arg, as65.BitsA);
                break;
            case DPAG:
                as65.GenerateDirectPage(0x25, as65.Arg);
                break;
            case ABSL:
                as65.GenerateAbsolute(0x2d, as65.Arg);
                break;
            case DPGX:
                as65.GenerateDirectPage(0x35, as65.Arg);
                break;
            case ABSX:
                as65.GenerateAbsolute(0x3d, as65.Arg);
                break;
            case DPGY:
            case ABSY:
                as65.GenerateAbsolute(0x39, as65.Arg);
                break;
            case INDX:
                as65.GenerateDirectPage(0x21, as65.Arg);
                break;
            case INDY:
                as65.GenerateDirectPage(0x31, as65.Arg);
                break;
            case INDI:
                if ((as65.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    as65.GenerateDirectPage(0x32, as65.Arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALNG:
                if ((as65.Processor & (M65816 | M65832)) != 0)
                    as65.GenerateLong(0x2f, as65.Arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALGX:
                if ((as65.Processor & (M65816 | M65832)) != 0)
                    as65.GenerateLong(0x3f, as65.Arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LIND:
                if ((as65.Processor & (M65816 | M65832)) != 0)
                    as65.GenerateDirectPage(0x27, as65.Arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LINY:
                if ((as65.Processor & (M65816 | M65832)) != 0)
                    as65.GenerateDirectPage(0x37, as65.Arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STAC:
                if ((as65.Processor & (M65816 | M65832)) != 0)
                    as65.GenerateImmediate(0x23, as65.Arg, 8);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STKI:
                if ((as65.Processor & (M65816 | M65832)) != 0)
                    as65.GenerateImmediate(0x33, as65.Arg, 8);
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
    private readonly Opcode ASL = new(Keyword, "ASL", as65 =>
    {
        switch (as65.ParseMode(DBANK))
        {
            case IMPL:
            case ACCM:
                as65.GenerateImplied(0x0a);
                break;
            case DPAG:
                as65.GenerateDirectPage(0x06, as65.Arg);
                break;
            case ABSL:
                as65.GenerateAbsolute(0x0e, as65.Arg);
                break;
            case DPGX:
                as65.GenerateDirectPage(0x16, as65.Arg);
                break;
            case ABSX:
                as65.GenerateAbsolute(0x1e, as65.Arg);
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
    private readonly Opcode BBR0 = new BitBranch(Keyword, "BBR0", 0x0f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBR1 instruction.
    /// </summary>
    private readonly Opcode BBR1 = new BitBranch(Keyword, "BBR1", 0x1f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBR2 instruction.
    /// </summary>
    private readonly Opcode BBR2 = new BitBranch(Keyword, "BBR2", 0x2f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBR3 instruction.
    /// </summary>
    private readonly Opcode BBR3 = new BitBranch(Keyword, "BBR3", 0x3f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBR4 instruction.
    /// </summary>
    private readonly Opcode BBR4 = new BitBranch(Keyword, "BBR4", 0x4f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBR5 instruction.
    /// </summary>
    private readonly Opcode BBR5 = new BitBranch(Keyword, "BBR5", 0x5f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBR6 instruction.
    /// </summary>
    private readonly Opcode BBR6 = new BitBranch(Keyword, "BBR6", 0x6f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBR7 instruction.
    /// </summary>
    private readonly Opcode BBR7 = new BitBranch(Keyword, "BBR7", 0x7f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBS0 instruction.
    /// </summary>
    private readonly Opcode BBS0 = new BitBranch(Keyword, "BBS0", 0x8f);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBS1 instruction.
    /// </summary>
    private readonly Opcode BBS1 = new BitBranch(Keyword, "BBS1", 0x9F);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBS2 instruction.
    /// </summary>
    private readonly Opcode BBS2 = new BitBranch(Keyword, "BBS2", 0xAF);

    /// <summary>
    /// An<code> Opcode</code> that handles the BBS4 instruction. 
    /// </summary>
    private readonly Opcode BBS3 = new BitBranch(Keyword, "BBS3", 0xBF);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBS4 instruction.
    /// </summary>
    private readonly Opcode BBS4 = new BitBranch(Keyword, "BBS4", 0xCF);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBS5 instruction.
    /// </summary>
    private readonly Opcode BBS5 = new BitBranch(Keyword, "BBS5", 0xDF);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBS6 instruction.
    /// </summary>
    private readonly Opcode BBS6 = new BitBranch(Keyword, "BBS6", 0xEF);

    /// <summary>
    /// An <code>Opcode</code> that handles the BBS7 instruction.
    /// </summary>
    private readonly Opcode BBS7 = new BitBranch(Keyword, "BBS7", 0xFF);


    /// <summary>
    /// An <code>Opcode</code> that handles the BCC instruction.
    /// </summary>
    private readonly Opcode BCC = new(Keyword, "BCC", as65 =>
    {
        switch (as65.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG:
                as65.GenerateRelative(0x90, as65.Arg, false);
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
    private readonly Opcode BCS = new(Keyword, "BCS", as65 =>
    {
        switch (as65.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG:
                as65.GenerateRelative(0xb0, as65.Arg, false);
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
    private readonly Opcode BEQ = new(Keyword, "BEQ", as65 =>
    {
        switch (as65.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG:
                as65.GenerateRelative(0xf0, as65.Arg, false);
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
    private readonly Opcode BIT = new(Keyword, "BIT", as65 =>
    {
        switch (as65.ParseMode(DBANK))
        {
            case DPAG:
                as65.GenerateDirectPage(0x24, as65.Arg);
                break;
            case ABSL:
                as65.GenerateAbsolute(0x2c, as65.Arg);
                break;
            case IMMD:
                if ((as65.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    as65.GenerateImmediate(0x89, as65.Arg, as65.BitsA);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case DPGX:
                if ((as65.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    as65.GenerateDirectPage(0x34, as65.Arg);
                else
                    as65.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ABSX:
                if ((as65.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    as65.GenerateAbsolute(0x3c, as65.Arg);
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
    private readonly Opcode BMI = new(Keyword, "BMI", as65 =>
    {
        switch (as65.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG:
                as65.GenerateRelative(0x30, as65.Arg, false);
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
    private readonly Opcode BNE = new(Keyword, "BNE", as65 =>
    {
        switch (as65.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG:
                as65.GenerateRelative(0xd0, as65.Arg, false);
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
    private readonly Opcode BPL = new(Keyword, "BPL", as65 =>
    {
        switch (as65.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG:
                as65.GenerateRelative(0x10, as65.Arg, false);
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
    private readonly Opcode BRA = new(Keyword, "BRA", as65 =>
    {
        if ((as65.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
        {
            switch (as65.ParseMode(PBANK))
            {
                case DPAG:
                case ABSL:
                case ALNG:
                    as65.GenerateRelative(0x80, as65.Arg, false);
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
    private readonly Opcode BRK = new(Keyword, "BRK", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0x00, asm.Arg, 8); break;
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
    private readonly Opcode BRL = new(Keyword, "BRL", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(PBANK))
            {
                case DPAG:
                case ABSL:
                case ALNG:
                    asm.GenerateRelative(0x82, asm.Arg, true);
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
    private readonly Opcode BVC = new(Keyword, "BVC", asm =>
    {
        switch (asm.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG:
                asm.GenerateRelative(0x50, asm.Arg, false);
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
    private readonly Opcode BVS = new(Keyword, "BVS", asm =>
    {
        switch (asm.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG:
                asm.GenerateRelative(0x70, asm.Arg, false);
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
    private readonly Opcode CLC = new(Keyword, "CLC", asm =>
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
    private readonly Opcode CLD = new(Keyword, "CLD", asm =>
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
    private readonly Opcode CLI = new(Keyword, "CLI", asm =>
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
    private readonly Opcode CLV = new(Keyword, "CLV", asm =>
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
    private readonly Opcode CMP = new(Keyword, "CMP", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0xc9, asm.Arg, asm.BitsA); break;
            case DPAG: asm.GenerateDirectPage(0xc5, asm.Arg); break;
            case ABSL: asm.GenerateAbsolute(0xcd, asm.Arg); break;
            case DPGX: asm.GenerateDirectPage(0xd5, asm.Arg); break;
            case ABSX: asm.GenerateAbsolute(0xdd, asm.Arg); break;
            case DPGY:
            case ABSY: asm.GenerateAbsolute(0xd9, asm.Arg); break;
            case INDX: asm.GenerateDirectPage(0xc1, asm.Arg); break;
            case INDY: asm.GenerateDirectPage(0xd1, asm.Arg); break;
            case INDI:
                if ((asm.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0xd2, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALNG:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0xcf, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALGX:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0xdf, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LIND:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0xc7, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LINY:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0xd7, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STAC:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateImmediate(0xc3, asm.Arg, 8);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STKI:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateImmediate(0xd3, asm.Arg, 8);
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
    private readonly Opcode COP = new(Keyword, "COP", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMMD: asm.GenerateImmediate(0x02, asm.Arg, 8); break;
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
    private readonly Opcode CPX = new(Keyword, "CPX", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0xe0, asm.Arg, asm.BitsI); break;
            case DPAG: asm.GenerateDirectPage(0xe4, asm.Arg); break;
            case ABSL: asm.GenerateAbsolute(0xec, asm.Arg); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });

    /// <summary>
    /// An <code>Opcode</code> that handles the CPY instruction.
    /// </summary>				
    private readonly Opcode CPY = new(Keyword, "CPY", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0xc0, asm.Arg, asm.BitsI); break;
            case DPAG: asm.GenerateDirectPage(0xc4, asm.Arg); break;
            case ABSL: asm.GenerateAbsolute(0xcc, asm.Arg); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });

    /// <summary>
    /// An <code>Opcode</code> that handles the DEC instruction.
    /// </summary>				
    private readonly Opcode DEC = new(Keyword, "DEC", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case DPAG: asm.GenerateDirectPage(0xC6, asm.Arg); break;
            case ABSL: asm.GenerateAbsolute(0xCE, asm.Arg); break;
            case DPGX: asm.GenerateDirectPage(0xD6, asm.Arg); break;
            case ABSX: asm.GenerateAbsolute(0xDE, asm.Arg); break;
            case ACCM:
                if ((asm.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
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
    private readonly Opcode DEX = new(Keyword, "DEX", asm =>
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
    private readonly Opcode DEY = new(Keyword, "DEY", asm =>
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
    private readonly Opcode EOR = new(Keyword, "EOR", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0x49, asm.Arg, asm.BitsA); break;
            case DPAG: asm.GenerateDirectPage(0x45, asm.Arg); break;
            case ABSL: asm.GenerateAbsolute(0x4d, asm.Arg); break;
            case DPGX: asm.GenerateDirectPage(0x55, asm.Arg); break;
            case ABSX: asm.GenerateAbsolute(0x5d, asm.Arg); break;
            case DPGY:
            case ABSY: asm.GenerateAbsolute(0x59, asm.Arg); break;
            case INDX: asm.GenerateDirectPage(0x41, asm.Arg); break;
            case INDY: asm.GenerateDirectPage(0x51, asm.Arg); break;
            case INDI:
                if ((asm.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0x52, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALNG:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0x4f, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALGX:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0x5f, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LIND:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0x47, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LINY:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0x57, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STAC:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateImmediate(0x43, asm.Arg, 8);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STKI:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateImmediate(0x53, asm.Arg, 8);
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
    private readonly Opcode INC = new(Keyword, "INC", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case DPAG: asm.GenerateDirectPage(0xe6, asm.Arg); break;
            case ABSL: asm.GenerateAbsolute(0xee, asm.Arg); break;
            case DPGX: asm.GenerateDirectPage(0xf6, asm.Arg); break;
            case ABSX: asm.GenerateAbsolute(0xfe, asm.Arg); break;
            case ACCM:
                if ((asm.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
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
    private readonly Opcode INX = new(Keyword, "INX", asm =>
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
    private readonly Opcode INY = new(Keyword, "INY", asm =>
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
    private readonly Opcode JML = new(Keyword, "JML", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(PBANK))
            {
                case DPAG:
                case ABSL:
                case ALNG: asm.GenerateLong(0x5c, asm.Arg); break;
                case LIND: asm.GenerateIndirect(0xdc, asm.Arg, true); break;
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
    private readonly Opcode JMP = new(Keyword, "JMP", asm =>
    {
        switch (asm.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL: asm.GenerateAbsolute(0x4c, asm.Arg); break;
            case INDI: asm.GenerateIndirect(0x6c, asm.Arg, true); break;
            case INDX:
                if ((asm.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    asm.GenerateAbsolute(0x7c, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;
            case ALNG:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0x5c, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;
            case LIND:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateIndirect(0xdc, asm.Arg, true);
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
    private readonly Opcode JSL = new(Keyword, "JSL", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(PBANK))
            {
                case DPAG:
                case ABSL:
                case ALNG: asm.GenerateLong(0x22, asm.Arg); break;
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
    private readonly Opcode JSR = new(Keyword, "JSR", asm =>
    {
        switch (asm.ParseMode(PBANK))
        {
            case DPAG:
            case ABSL:
            case ALNG: asm.GenerateAbsolute(0x20, asm.Arg); break;

            case INDX:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateAbsolute(0xfc, asm.Arg);
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
    private readonly Opcode LDA = new(Keyword, "LDA", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0xa9, asm.Arg, asm.BitsA); break;
            case DPAG: asm.GenerateDirectPage(0xa5, asm.Arg); break;
            case ABSL: asm.GenerateAbsolute(0xad, asm.Arg); break;
            case DPGX: asm.GenerateDirectPage(0xb5, asm.Arg); break;
            case ABSX: asm.GenerateAbsolute(0xbd, asm.Arg); break;
            case DPGY:
            case ABSY: asm.GenerateAbsolute(0xb9, asm.Arg); break;
            case INDX: asm.GenerateDirectPage(0xa1, asm.Arg); break;
            case INDY: asm.GenerateDirectPage(0xb1, asm.Arg); break;
            case INDI:
                if ((asm.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0xb2, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALNG:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0xaf, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALGX:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0xbf, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LIND:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0xa7, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LINY:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0xb7, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STAC:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateImmediate(0xa3, asm.Arg, 8);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STKI:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateImmediate(0xb3, asm.Arg, 8);
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
    private readonly Opcode LDX = new(Keyword, "LDX", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0xa2, asm.Arg, asm.BitsI); break;
            case DPAG: asm.GenerateDirectPage(0xa6, asm.Arg); break;
            case ABSL: asm.GenerateAbsolute(0xae, asm.Arg); break;
            case DPGY: asm.GenerateDirectPage(0xb6, asm.Arg); break;
            case ABSY: asm.GenerateAbsolute(0xbe, asm.Arg); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the LDY instruction.
    /// </summary>				
    private readonly Opcode LDY = new(Keyword, "LDY", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0xa0, asm.Arg, asm.BitsI); break;
            case DPAG: asm.GenerateDirectPage(0xa4, asm.Arg); break;
            case ABSL: asm.GenerateAbsolute(0xac, asm.Arg); break;
            case DPGX: asm.GenerateDirectPage(0xb4, asm.Arg); break;
            case ABSX: asm.GenerateAbsolute(0xbc, asm.Arg); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the LSR instruction.
    /// </summary>				
    private readonly Opcode LSR = new(Keyword, "LSR", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL:
            case ACCM: asm.GenerateImplied(0x4a); break;
            case DPAG: asm.GenerateDirectPage(0x46, asm.Arg); break;
            case ABSL: asm.GenerateAbsolute(0x4e, asm.Arg); break;
            case DPGX: asm.GenerateDirectPage(0x56, asm.Arg); break;
            case ABSX: asm.GenerateAbsolute(0x5e, asm.Arg); break;
            default:
                asm.OnError(ERR_ILLEGAL_ADDR);
                break;
        }
        return (true);
    });


    /// <summary>
    /// An <code>Opcode</code> that handles the MVN instruction.
    /// </summary>				
    private readonly Opcode MVN = new(Keyword, "MVN", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
        {
            asm.CurrentToken = asm.NextRealToken();
            var dst = asm.ParseExpression();

            if (asm.CurrentToken == Comma)
            {
                asm.CurrentToken = asm.NextRealToken();
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
    private readonly Opcode MVP = new(Keyword, "MVP", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
        {
            asm.CurrentToken = asm.NextRealToken();
            var dst = asm.ParseExpression();

            if (asm.CurrentToken == Comma)
            {
                asm.CurrentToken = asm.NextRealToken();
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
    private readonly Opcode NOP = new(Keyword, "NOP", asm =>
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
    private readonly Opcode ORA = new(Keyword, "ORA", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD: asm.GenerateImmediate(0x09, asm.Arg, asm.BitsA); break;
            case DPAG: asm.GenerateDirectPage(0x05, asm.Arg); break;
            case ABSL: asm.GenerateAbsolute(0x0f, asm.Arg); break;
            case DPGX: asm.GenerateDirectPage(0x15, asm.Arg); break;
            case ABSX: asm.GenerateAbsolute(0x1f, asm.Arg); break;
            case DPGY:
            case ABSY: asm.GenerateAbsolute(0x19, asm.Arg); break;
            case INDX: asm.GenerateDirectPage(0x01, asm.Arg); break;
            case INDY: asm.GenerateDirectPage(0x11, asm.Arg); break;
            case INDI:
                if ((asm.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0x12, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALNG:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0x0f, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case ALGX:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateLong(0x1f, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LIND:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0x07, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case LINY:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateDirectPage(0x17, asm.Arg);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STAC:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateImmediate(0x03, asm.Arg, 8);
                else
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                break;

            case STKI:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                    asm.GenerateImmediate(0x13, asm.Arg, 8);
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
    private readonly Opcode PEA = new(Keyword, "PEA", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case DPAG:
                case ABSL:
                case IMMD: asm.GenerateImmediate(0xf4, asm.Arg, 16); break;
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
    private readonly Opcode PEI = new(Keyword, "PEI", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case INDI: asm.GenerateImmediate(0xd4, asm.Arg, 8); break;
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
    private readonly Opcode PER = new(Keyword, "PER", asm =>
    {
        // put your code here
        if ((asm.Processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(PBANK))
            {
                case DPAG:
                case ABSL:
                    asm.GenerateRelative(0x62, asm.Arg, true); break;
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
    private readonly Opcode PHA = new(Keyword, "PHA", asm =>
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
    private readonly Opcode PHB = new(Keyword, "PHB", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
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
    private readonly Opcode PHD = new(Keyword, "PHD", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
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
    private readonly Opcode PHK = new(Keyword, "PHK", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
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
    private readonly Opcode PHP = new(Keyword, "PHP", asm =>
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
    private readonly Opcode PHX = new(Keyword, "PHX", asm =>
    {
        if ((asm.Processor & (M6502 | M65SC02 | M65816 | M65832)) != 0)
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
    private readonly Opcode PHY = new(Keyword, "PHY", asm =>
    {
        if ((asm.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
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
    private readonly Opcode PLA = new(Keyword, "PLA", asm =>
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
    private readonly Opcode PLB = new(Keyword, "PLB", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
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
    private readonly Opcode PLD = new(Keyword, "PLD", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
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
    private readonly Opcode PLP = new(Keyword, "PLP", asm =>
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
    private readonly Opcode PLX = new(Keyword, "PLX", asm =>
    {
        if ((asm.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
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
    private readonly Opcode PLY = new(Keyword, "PLY", asm =>
    {
        if ((asm.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
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
    private readonly Opcode REP = new(Keyword, "REP", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMPL:
                    asm.GenerateImmediate(0xc2, asm.Arg, 8);
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
    private readonly Opcode RMB0 = new BitOperation(Keyword, "RMB0", 0x07);

    /// <summary>
    /// An <CODE>Opcode</CODE> that handles the RMB1 instruction.
    /// </summary>
    private readonly Opcode RMB1 = new BitOperation(Keyword, "RMB1", 0x17);

    /// <summary>
    /// An <CODE>Opcode</CODE> that handles the RMB2 instruction.
    /// </summary>
    private readonly Opcode RMB2 = new BitOperation(Keyword, "RMB2", 0x27);

    /// <summary>
    /// An <CODE>Opcode</CODE> that handles the RMB3 instruction.
    /// </summary>
    private readonly Opcode RMB3 = new BitOperation(Keyword, "RMB3", 0x37);

    /// <summary>
    /// An <CODE>Opcode</CODE> that handles the RMB4 instruction.
    /// </summary>
    private readonly Opcode RMB4 = new BitOperation(Keyword, "RMB4", 0x47);

    /// <summary>
    /// An <CODE>Opcode</CODE> that handles the RMB5 instruction.
    /// </summary>
    private readonly Opcode RMB5 = new BitOperation(Keyword, "RMB5", 0x57);

    /// <summary>
    /// An <CODE>Opcode</CODE> that handles the RMB6 instruction.
    /// </summary>
    private readonly Opcode RMB6 = new BitOperation(Keyword, "RMB6", 0x67);

    /// <summary>
    /// An <CODE>Opcode</CODE> that handles the RMB7 instruction.
    /// </summary>
    private readonly Opcode RMB7 = new BitOperation(Keyword, "RMB7", 0x77);

    /// <summary>
    /// The <code>Opcode</code> to handle the ROL instruction.
    /// </summary>
    private readonly Opcode ROL = new(Keyword, "ROL", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL:
            case ACCM:
                asm.GenerateImplied(0x2a);
                break;
            case DPAG:
                asm.GenerateDirectPage(0x26, asm.Arg);
                break;
            case ABSL:
                asm.GenerateAbsolute(0x2e, asm.Arg);
                break;
            case DPGX:
                asm.GenerateDirectPage(0x36, asm.Arg);
                break;
            case ABSX:
                asm.GenerateAbsolute(0x3e, asm.Arg);
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
    private readonly Opcode ROR = new(Keyword, "ROR", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMPL:
            case ACCM:
                asm.GenerateImplied(0x6A);
                break;
            case DPAG:
                asm.GenerateDirectPage(0x66, asm.Arg);
                break;
            case ABSL:
                asm.GenerateAbsolute(0x6e, asm.Arg);
                break;
            case DPGX:
                asm.GenerateDirectPage(0x76, asm.Arg);
                break;
            case ABSX:
                asm.GenerateAbsolute(0x7e, asm.Arg);
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
    private readonly Opcode RTI = new(Keyword, "RTI", asm =>
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
    private readonly Opcode RTL = new(Keyword, "RTL", asm => {
        if ((asm.Processor & (M65816 | M65832)) != 0)
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
    private readonly Opcode RTS = new(Keyword, "RTS", asm =>
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
    private readonly Opcode SBC = new(Keyword, "SBC", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case IMMD:
                asm.GenerateImmediate(0xe9, asm.Arg, asm.BitsA);
                break;
            case DPAG:
                asm.GenerateDirectPage(0xe5, asm.Arg);
                break;
            case ABSL:
                asm.GenerateAbsolute(0xed, asm.Arg);
                break;
            case DPGX:
                asm.GenerateDirectPage(0xf5, asm.Arg);
                break;
            case ABSX:
                asm.GenerateAbsolute(0xfd, asm.Arg);
                break;
            case DPGY:
            case ABSY:
                asm.GenerateAbsolute(0xf8, asm.Arg);
                break;
            case INDX:
                asm.GenerateDirectPage(0xe1, asm.Arg);
                break;
            case INDY:
                asm.GenerateDirectPage(0xf1, asm.Arg);
                break;
            case INDI:
                if ((asm.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                {
                    asm.GenerateDirectPage(0xf2, asm.Arg);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }

                break;
            case ALNG:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateLong(0xef, asm.Arg);
                }
                else
                {

                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }

                break;

            case ALGX:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateLong(0xff, asm.Arg);
                }
                else
                {

                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }

                break;

            case LIND:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateDirectPage(0xe7, asm.Arg);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }

                return true;
            case LINY:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateDirectPage(0xf7, asm.Arg);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }

                return true;
            case STAC:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateImmediate(0xe3, asm.Arg, 8);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }

                return true;

            case STKI:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateImmediate(0xf3, asm.Arg, 8);
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
    private readonly Opcode SEC = new(Keyword, "SEC", asm =>
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
    private readonly Opcode SED = new(Keyword, "SED", asm =>
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
    private readonly Opcode SEI = new(Keyword, "SEI", asm =>
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
    private readonly Opcode SEP = new(Keyword, "SEP", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMMD: asm.GenerateImmediate(0xe2, asm.Arg, 8);
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

    private readonly Opcode SMB0 = new BitOperation(Keyword, "SMB0", 0x87);
    private readonly Opcode SMB1 = new BitOperation(Keyword, "SMB1", 0X97);
    private readonly Opcode SMB2 = new BitOperation(Keyword, "SMB2", 0xA7);
    private readonly Opcode SMB3 = new BitOperation(Keyword, "SMB3", 0xB7);
    private readonly Opcode SMB4 = new BitOperation(Keyword, "SMB4", 0xC7);
    private readonly Opcode SMB5 = new BitOperation(Keyword, "SMB5", 0xD7);
    private readonly Opcode SMB6 = new BitOperation(Keyword, "SMB6", 0xE7);
    private readonly Opcode SMB7 = new BitOperation(Keyword, "SMB7", 0xF7);

    /// <summary>
    /// The <code>Opcode</code> to handle the STA instruction.
    /// </summary>
    private readonly Opcode STA = new(Keyword, "STA", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case DPAG: asm.GenerateDirectPage(0x85, asm.Arg);
                break;

            case ABSL:
                asm.GenerateAbsolute(0x8d, asm.Arg);
                break;

            case DPGX:
                asm.GenerateDirectPage(0x95, asm.Arg);
                break;

            case ABSX:
                asm.GenerateAbsolute(0x9d, asm.Arg);
                break;

            case DPGY:
            case ABSY:
                asm.GenerateAbsolute(0x99, asm.Arg);
                break;
            case INDX:
                asm.GenerateDirectPage(0x81, asm.Arg);
                break;

            case INDI:
                if ((asm.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
                {
                    asm.GenerateDirectPage(0x92, asm.Arg);
                }
                else
                {

                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }

                break;

            case ALNG:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateLong(0x8f, asm.Arg);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }
                break;

            case ALGX:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateLong(0x9f, asm.Arg);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }
                break;

            case LIND:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateDirectPage(0x87, asm.Arg);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }
                break;

            case LINY:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateDirectPage(0x97, asm.Arg);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }
                break;

            case STAC:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateImmediate(0x83, asm.Arg, 8);
                }
                else
                {
                    asm.OnError(ERR_MODE_NOT_SUPPORTED);
                }
                break;

            case STKI:
                if ((asm.Processor & (M65816 | M65832)) != 0)
                {
                    asm.GenerateImmediate(0x93, asm.Arg, 8);
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
    private readonly Opcode STP = new(Keyword, "STP", asm =>
    {
        if ((asm.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
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
    private readonly Opcode STX = new(Keyword, "STX", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case DPAG: asm.GenerateDirectPage(0x86, asm.Arg);
                break;

            case ABSL: asm.GenerateAbsolute(0x8e, asm.Arg);
                break;

            case DPGY:
                asm.GenerateDirectPage(0x96, asm.Arg);
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
    private readonly Opcode STY = new(Keyword, "STY", asm =>
    {
        switch (asm.ParseMode(DBANK))
        {
            case DPAG:
                asm.GenerateDirectPage(0x84, asm.Arg);
                break;

            case ABSL:
                asm.GenerateAbsolute(0x8c, asm.Arg);
                break;

            case DPGX:
                asm.GenerateDirectPage(0x94, asm.Arg);
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
    private readonly Opcode STZ = new(Keyword, "STZ", asm =>
    {
        if ((asm.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case DPAG:
                    asm.GenerateDirectPage(0x64, asm.Arg);
                    break;
                case ABSL:
                    asm.GenerateAbsolute(0x9c, asm.Arg);
                    break;
                case DPGX:
                    asm.GenerateDirectPage(0x74, asm.Arg);
                    break;
                case ABSX:
                    asm.GenerateAbsolute(0X9e, asm.Arg);
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
    private readonly Opcode TAX = new(Keyword, "TAX", asm =>
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
    private readonly Opcode TAY = new(Keyword, "TAY", asm =>
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
    private readonly Opcode TCD = new(Keyword, "TCD", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
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
    private readonly Opcode TCS = new(Keyword, "TCS", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
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
    private readonly Opcode TDC = new(Keyword, "TDC", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
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
    private readonly Opcode TRB = new(Keyword, "TRB", asm =>
    {
        if ((asm.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case DPAG: asm.GenerateDirectPage(0x14, asm.Arg); break;
                case ABSL: asm.GenerateAbsolute(0x1C, asm.Arg); break;
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
    private readonly Opcode TSB = new(Keyword, "TSB", asm =>
    {
        if ((asm.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case DPAG: asm.GenerateDirectPage(0x04, asm.Arg); break;
                case ABSL: asm.GenerateAbsolute(0x0C, asm.Arg); break;
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
    private readonly Opcode TSC = new(Keyword, "TSC", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
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
    private readonly Opcode TSX = new(Keyword, "TSX", asm =>
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
    private readonly Opcode TXA = new(Keyword, "TXA", asm =>
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
    private readonly Opcode TXS = new(Keyword, "TXS", asm =>
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
    private readonly Opcode TXY = new(Keyword, "TXY", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
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
    private readonly Opcode TYA = new(Keyword, "TYA", asm =>
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
    private readonly Opcode TYX = new(Keyword, "TYX", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
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
    private readonly Opcode WAI = new(Keyword, "WAI", asm =>
    {
        if ((asm.Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0)
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
    private readonly Opcode WDM = new(Keyword, "WDM", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
        {
            switch (asm.ParseMode(DBANK))
            {
                case IMMD: asm.GenerateImmediate(0x42, asm.Arg, 8); break;
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
    private readonly Opcode XBA = new(Keyword, "XBA", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
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
    private readonly Opcode XCE = new(Keyword, "XCE", asm =>
    {
        if ((asm.Processor & (M65816 | M65832)) != 0)
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
    private new readonly Opcode IF = new(Keyword, "IF", asm =>
    {
        var index = asm.IfIndex++;

        asm.Ifs.Push(index);

        if (asm.Pass == Pass.FIRST)
        {
            asm.ElseAddr.Add(null);
            asm.EndIfAddr.Add(null);
        }

        asm.CurrentToken = asm.NextRealToken();

        var target = asm.ElseAddr.ElementAt(index) ?? (asm.EndIfAddr.ElementAt(index) ?? asm.Origin);

        if (asm.CurrentToken == EQ) asm.GenerateBranch(NE, target);
        else if (asm.CurrentToken == NE) asm.GenerateBranch(EQ, target);
        else if (asm.CurrentToken == CC) asm.GenerateBranch(CS, target);
        else if (asm.CurrentToken == CS) asm.GenerateBranch(CC, target);
        else if (asm.CurrentToken == PL) asm.GenerateBranch(MI, target);
        else if (asm.CurrentToken == MI) asm.GenerateBranch(PL, target);
        else if (asm.CurrentToken == VC) asm.GenerateBranch(VS, target);
        else if (asm.CurrentToken == VS) asm.GenerateBranch(VC, target);
        else
            asm.OnError(ERR_INVALID_CONDITIONAL);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the ELSE structured assembly
    /// command.
    /// </summary>
    private new readonly Opcode ELSE = new(Keyword, "ELSE", asm =>
    {
        if (asm.Ifs.Count > 0)
        {
            var index = asm.Ifs.Peek();

            var target = asm.EndIfAddr[index] ?? asm.Origin;

            asm.GenerateJump(target);
            asm.ElseAddr[index] = asm.Section?.GetOrigin();
        }
        else
            asm.OnError(ERR_NO_ACTIVE_IF);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the ENDIF structured assembly
    /// command.
    /// </summary>
    private new readonly Opcode ENDIF = new(Keyword, "ENDIF", asm =>
    {
        if (asm.Ifs.Count > 0)
        {
            var index = asm.Ifs.Pop();

            asm.EndIfAddr[index] = asm.Section?.GetOrigin();
        }
        else
            asm.OnError(ERR_NO_ACTIVE_IF);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the REPEAT structured assembly
    /// command.
    /// </summary>
    private new readonly Opcode REPEAT = new(Keyword, "REPEAT", asm =>
    {
        var index = asm.LoopIndex++;

        asm.Loops.Push(index);

        if (asm.Pass == Pass.FIRST)
        {
            asm.LoopAddr.Add(asm.Section?.GetOrigin());
            asm.EndAddr.Add(null);
        }
        else
            asm.LoopAddr[index] = asm.Section?.GetOrigin();

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the UNTIL structured assembly
    /// command.
    /// </summary>
    private readonly Opcode UNTIL = new(Keyword, "UNTIL", asm =>
    {
        if (asm.Loops.Count > 0)
        {
            var index = asm.Loops.Pop();

            asm.CurrentToken = asm.NextRealToken();

            var target = asm.LoopAddr[index] ?? asm.Origin;

            if (asm.CurrentToken == EQ) asm.GenerateBranch(NE, target);
            else if (asm.CurrentToken == NE) asm.GenerateBranch(EQ, target);
            else if (asm.CurrentToken == CC) asm.GenerateBranch(CS, target);
            else if (asm.CurrentToken == CS) asm.GenerateBranch(CC, target);
            else if (asm.CurrentToken == PL) asm.GenerateBranch(MI, target);
            else if (asm.CurrentToken == MI) asm.GenerateBranch(PL, target);
            else if (asm.CurrentToken == VC) asm.GenerateBranch(VS, target);
            else if (asm.CurrentToken == VS) asm.GenerateBranch(VC, target);
            else
                asm.OnError(ERR_INVALID_CONDITIONAL);

            asm.EndAddr[index] = asm.Section?.GetOrigin();
        }
        else
            asm.OnError(ERR_NO_ACTIVE_REPEAT);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the FOREVER structured assembly
    /// command.
    /// </summary>
    private readonly Opcode FOREVER = new(Keyword, "FOREVER", asm =>
    {
        if (asm.Loops.Count > 0)
        {
            var index = asm.Loops.Pop();

            var target = asm.LoopAddr[index] ?? asm.Origin;

            asm.GenerateJump(target);

            asm.EndAddr[index] = asm.Section?.GetOrigin();
        }
        else
            asm.OnError(ERR_NO_ACTIVE_REPEAT);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the WHILE structured assembly
    /// command.
    /// </summary>
    private readonly Opcode WHILE = new(Keyword, "WHILE", asm =>
    {
        var index = asm.LoopIndex++;

        asm.Loops.Push(index);

        if (asm.Pass == Pass.FIRST)
        {
            asm.LoopAddr.Add(asm.Section?.GetOrigin());
            asm.EndAddr.Add(null);
        }
        else
            asm.LoopAddr[index] = asm.Section?.GetOrigin();

        asm.CurrentToken = asm.NextRealToken();

        var target = asm.EndAddr[index] ?? asm.Origin;

        if (asm.CurrentToken == EQ) asm.GenerateBranch(NE, target);
        else if (asm.CurrentToken == NE) asm.GenerateBranch(EQ, target);
        else if (asm.CurrentToken == CC) asm.GenerateBranch(CS, target);
        else if (asm.CurrentToken == CS) asm.GenerateBranch(CC, target);
        else if (asm.CurrentToken == PL) asm.GenerateBranch(MI, target);
        else if (asm.CurrentToken == MI) asm.GenerateBranch(PL, target);
        else if (asm.CurrentToken == VC) asm.GenerateBranch(VS, target);
        else if (asm.CurrentToken == VS) asm.GenerateBranch(VC, target);
        else asm.OnError(ERR_INVALID_CONDITIONAL);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the ENDW structured assembly
    /// command.
    /// </summary>
    private readonly Opcode ENDW = new(Keyword, "ENDW", asm =>
    {
        if (asm.Loops.Count > 0)
        {
            var index = asm.Loops.Pop();

            var target = asm.LoopAddr[index] ?? asm.Section?.GetOrigin();

            asm.GenerateJump(target);

            asm.EndAddr[index] = asm.Section?.GetOrigin();
        }
        else
            asm.OnError(ERR_NO_ACTIVE_WHILE);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the CONT structured assembly
    /// command.
    /// </summary>
    private readonly Opcode CONT = new(Keyword, "CONT", asm =>
    {
        if (asm.Loops.Count > 0)
        {
            var index = asm.Loops.Peek();

            asm.CurrentToken = asm.NextRealToken();

            var target = asm.LoopAddr[index] ?? asm.Origin;

            if (asm.CurrentToken == EOL) asm.GenerateJump(target);
            else if (asm.CurrentToken == EQ) asm.GenerateBranch(asm.CurrentToken, target);
            else if (asm.CurrentToken == NE) asm.GenerateBranch(asm.CurrentToken, target);
            else if (asm.CurrentToken == CC) asm.GenerateBranch(asm.CurrentToken, target);
            else if (asm.CurrentToken == CS) asm.GenerateBranch(asm.CurrentToken, target);
            else if (asm.CurrentToken == PL) asm.GenerateBranch(asm.CurrentToken, target);
            else if (asm.CurrentToken == MI) asm.GenerateBranch(asm.CurrentToken, target);
            else if (asm.CurrentToken == VC) asm.GenerateBranch(asm.CurrentToken, target);
            else if (asm.CurrentToken == VS) asm.GenerateBranch(asm.CurrentToken, target);
            else
                asm.OnError(ERR_INVALID_CONDITIONAL);
        }
        else
            asm.OnError(ERR_NO_ACTIVE_LOOP);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the BREAK structured assembly
    /// command.
    /// </summary>
    private readonly Opcode BREAK = new(Keyword, "BREAK", asm =>
    {
        if (asm.Loops.Count > 0)
        {
            var index = asm.Loops.Peek();

            asm.CurrentToken = asm.NextRealToken();

            var target = asm.EndAddr[index] ?? asm.Origin;

            if (asm.CurrentToken == EOL) asm.GenerateJump(target);
            else if (asm.CurrentToken == EQ) asm.GenerateBranch(asm.CurrentToken, target);
            else if (asm.CurrentToken == NE) asm.GenerateBranch(asm.CurrentToken, target);
            else if (asm.CurrentToken == CC) asm.GenerateBranch(asm.CurrentToken, target);
            else if (asm.CurrentToken == CS) asm.GenerateBranch(asm.CurrentToken, target);
            else if (asm.CurrentToken == PL) asm.GenerateBranch(asm.CurrentToken, target);
            else if (asm.CurrentToken == MI) asm.GenerateBranch(asm.CurrentToken, target);
            else if (asm.CurrentToken == VC) asm.GenerateBranch(asm.CurrentToken, target);
            else if (asm.CurrentToken == VS) asm.GenerateBranch(asm.CurrentToken, target);
            else
                asm.OnError(ERR_INVALID_CONDITIONAL);
        }
        else
            asm.OnError(ERR_NO_ACTIVE_LOOP);

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the A2STR instruction.
    /// </summary>
    private readonly Opcode A2STR = new(Keyword, "A2STR", asm =>
    {
        do {
            asm.CurrentToken = asm.NextRealToken();
            if (asm.CurrentToken?.Kind == String)
            {
                var value = asm.CurrentToken.Text;

                foreach (var b in value)
                    asm.AddByte((byte)(b | 0x80));

                asm.CurrentToken = asm.NextRealToken();
            }
            else
            {
                var expr = asm.ParseExpression();

                if (expr != null)
                    asm.AddByte(Expr.Or(expr, HI_BIT));
                else
                    asm.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
            }
        } while (asm.CurrentToken == Comma);

        if (asm.CurrentToken != EOL) asm.OnError(ErrorMessage.ERR_INVALID_EXPRESSION);
        return (true);

    });

    /// <summary>
    /// The <code>Opcode</code> to handle the HSTR instruction.
    /// </summary>
    private readonly Opcode HSTR = new(Keyword, "HSTR", asm =>
    {
        asm.CurrentToken = asm.NextRealToken();

        if (asm.CurrentToken?.Kind == String)
        {
            var value = asm.CurrentToken.Text;

            for (var index = 0; index < value.Length;)
            {
                var ch = value[index++];

                asm.AddByte((byte)(ch | ((index < value.Length) ? 0x00 : 0x80)));
            }

            asm.CurrentToken = asm.NextRealToken();
        }
        else
            asm.OnError(".HSTR must have a string argument");

        return (true);
    });

    /// <summary>
    /// The <code>Opcode</code> to handle the PSTR instruction.
    /// </summary>
    private readonly Opcode PSTR = new(Keyword, "PSTR", asm =>
    {
        asm.CurrentToken = asm.NextRealToken();

        if (asm.CurrentToken?.Kind == String)
        {
            var value = asm.CurrentToken.Text;

            if (value.Length > 255)
            {
                asm.OnError("String is too long for a Pascal string");
                return (true);
            }

            asm.AddByte((byte)(value.Length));
            foreach (var b in value)
                asm.AddByte((byte)b);

            asm.CurrentToken = asm.NextRealToken();
        }
        else
            asm.OnError(".PSTR must have a string argument");

        return (true);
    });

    private readonly Opcode JCC;
    private readonly Opcode JCS;
    private readonly Opcode JEQ;
    private readonly Opcode JMI;
    private readonly Opcode JNE;
    private readonly Opcode JPL;
    private readonly Opcode JVC;
    private readonly Opcode JVS;
    private readonly Opcode JPA;

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
        AddToken(Assembler.ELSE);
        AddToken(End);
        AddToken(Assembler.ENDIF);
        AddToken(ENDM);
        AddToken(ENDR);
        AddToken(Equ);
        AddToken(EXITM);
        AddToken(EXTERN);
        AddToken(GLOBAL);
        AddToken(Assembler.IF);
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
        AddToken(Assembler.REPEAT);
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
        AddToken(Assembler.BANK);

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
        var byteCount = Memory?.ByteCount;

        output.Clear();

        switch (LineType)
        {
            case '=':
                output.Append("         ");
                if (Addr == null)
                    OnError("Addr is null");
                output.Append(Hex.ToHex(Addr?.Resolve() ?? 0, 8));
                output.Append(Addr?.IsAbsolute ?? false ? "  " : "' ");
                output.Append("        ");
                output.Append(LineType);
                output.Append(' ');
                break;

            case ' ':
                output.Append("         ");
                output.Append("        ");
                output.Append("  ");
                output.Append("        ");
                output.Append(LineType);
                output.Append(' ');
                break;

            default:
                if (IsActive && (Addr != null) && ((Label != null) || (LineType == ':') || (byteCount > 0)))
                {
                    var value = Addr.Resolve();
                    output.Append(Hex.ToHex(value >> 16, 2));
                    output.Append(":");
                    output.Append(Hex.ToHex(value, 4));
                    output.Append(Addr?.IsAbsolute == true ? "  " : "' ");

                    for (var index = 0; index < 8; ++index)
                    {
                        if (index < byteCount)
                        {
                            var code = Memory?.GetByte(index) ?? 0;

                            output.Append(code >= 0 ? Hex.ToHex(code, 2) : "??");
                        }
                        else
                            output.Append("  ");
                    }
                    output.Append((byteCount > 8) ? "> " : "  ");
                    output.Append(LineType);
                    output.Append(' ');
                }
                else
                {
                    output.Append("                           ");
                    output.Append(LineType);
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

        DataBank = 0;
        DirectPage = 0;
        BitsA = 0;
        BitsI = 0;

        Sections.Add(".page0", Module?.FindSection(".page0"));

        IfIndex = 0;
        LoopIndex = 0;

        Title = "Portable 65xx Assembler [30.00]";
    }

    protected override void EndPass()
    {
        if (Ifs.Count > 0)
        {
            OnError(ERR_UNTERMINATED_IFS);
        }

        if (Loops.Count > 0)
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
        var value = 0;

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
                    if (!IsOctal(PeekChar())) return OriginToken;

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
                    if (!IsHexadecimal(PeekChar())) return OriginToken;

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
                    return OriginToken;
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

            if (tokenDictionary.ContainsKey(symbol.ToUpper()))
            {
                return tokenDictionary[symbol.ToUpper()];
            }
            else
            {
                return new Token(Symbol, symbol);
            }

            //return tokenDictionary.TryGetValue(symbol.ToUpper(), out var opcode) ? opcode : new Token(Symbol, symbol);
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

    // Represents the direct page indexed addressing mode
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
    /// Determines the addressing mode used by the instruction.
    /// </summary>
    /// <param name="targetBank">The target bank value.</param>
    /// <returns>The address mode.</returns>
    public override int ParseMode(int targetBank)
    {
        CurrentToken = NextRealToken();

        if (CurrentToken == EOL) return IMPL;

        // Handle Accumulator
        if (CurrentToken == A)
        {
            CurrentToken = NextRealToken();
            Arg = null;
            return ACCM;
        }

        // Handle Immediate
        if (CurrentToken == HASH)
        {
            CurrentToken = NextRealToken();
            if (CurrentToken == Lt)
            {
                CurrentToken = NextRealToken();
                Arg = ParseImmediate();
            }
            else if (CurrentToken == Gt)
            {
                CurrentToken = NextRealToken();
                Arg = Expr.Shr(ParseImmediate(), EIGHT);
            }
            else if (CurrentToken == BinaryXor)
            {
                CurrentToken = NextRealToken();
                Arg = Expr.Shr(ParseImmediate(), SIXTEEN);
            }
            else
                Arg = ParseImmediate();

            return IMMD;
        }

        // Handle <.. <..,X <..,Y
        if (CurrentToken == Lt)
        {
            CurrentToken = NextRealToken();
            Arg = ParseExpression();

            if (Arg == null)
                OnError(ERR_MISSING_EXPRESSION);

            if (CurrentToken != Comma) return DPAG;
            CurrentToken = NextRealToken();
            if (CurrentToken == X)
            {
                CurrentToken = NextRealToken();
                return DPGX;
            }

            if (CurrentToken == Y)
            {
                CurrentToken = NextRealToken();
                return DPGY;
            }

            OnError(ERR_EXPECTED_X_OR_Y);
            return UNKN;

        }

        // Handle >.. and >..,X
        if (CurrentToken == Gt)
        {
            CurrentToken = NextRealToken();
            Arg = ParseExpression();

            if (Arg == null)
                OnError(ERR_MISSING_EXPRESSION);

            if (CurrentToken == Comma)
            {
                CurrentToken = NextRealToken();
                if (CurrentToken == X)
                {
                    CurrentToken = NextRealToken();
                    return ALGX;
                }

                OnError(ERR_EXPECTED_X);
                return UNKN;
            }

            return ALNG;
        }

        // Handle [..] and [..],Y
        if (CurrentToken == LBRACKET)
        {
            CurrentToken = NextRealToken();
            Arg = ParseExpression();

            if (Arg == null)
                OnError(ERR_MISSING_EXPRESSION);

            if (CurrentToken == RBRACKET)
            {
                CurrentToken = NextRealToken();
                if (CurrentToken == Comma)
                {
                    CurrentToken = NextRealToken();
                    if (CurrentToken == Y)
                    {
                        CurrentToken = NextRealToken();
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
        if (CurrentToken == LParen)
        {
            CurrentToken = NextRealToken();
            Arg = ParseExpression();

            if (Arg == null)
                OnError(ERR_MISSING_EXPRESSION);

            if (CurrentToken == Comma)
            {
                CurrentToken = NextRealToken();
                if (CurrentToken == X)
                {
                    CurrentToken = NextRealToken();
                    if (CurrentToken == RParen)
                    {
                        CurrentToken = NextRealToken();
                        return INDX;
                    }

                    OnError(ERR_EXPECTED_CLOSING_PARENTHESIS);
                    return UNKN;
                }

                if (CurrentToken == S)
                {
                    CurrentToken = NextRealToken();
                    if (CurrentToken == RParen)
                    {
                        CurrentToken = NextRealToken();
                        if (CurrentToken == Comma)
                        {
                            CurrentToken = NextRealToken();
                            if (CurrentToken == Y)
                            {
                                CurrentToken = NextRealToken();
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

            if (CurrentToken != RParen) return UNKN;
            CurrentToken = NextRealToken();
            if (CurrentToken != Comma) return INDI;
            CurrentToken = NextRealToken();
            if (CurrentToken == Y)
            {
                CurrentToken = NextRealToken();
                return INDY;
            }

            OnError(ERR_EXPECTED_Y);
            return UNKN;

        }

        // Handle |.., |..,X and |..,Y or !.., !..,X and !..,Y
        if (CurrentToken == BinaryOr || CurrentToken == LogicalNot)
        {
            CurrentToken = NextRealToken();
            Arg = ParseExpression();

            if (Arg == null)
                OnError(ERR_MISSING_EXPRESSION);

            if (CurrentToken != Comma) return ABSL;
            CurrentToken = NextRealToken();
            if (CurrentToken == X)
            {
                CurrentToken = NextRealToken();
                return ABSX;
            }

            if (CurrentToken == Y)
            {
                CurrentToken = NextRealToken();
                return ABSY;
            }

            OnError(ERR_EXPECTED_X_OR_Y);
            return UNKN;

        }

        // Handle .. ..,X ..,Y and ..,S
        Arg = ParseExpression();

        if (Arg == null)
        {
            OnError(ERR_MISSING_EXPRESSION);
            return ABSL;
        }

        if (CurrentToken == Comma)
        {
            CurrentToken = NextRealToken();
            if (CurrentToken == X)
            {
                CurrentToken = NextRealToken();
                return Arg.IsAbsolute && IsByteAddress((int)Arg.Resolve()) ? DPGX : ABSX;
            }

            if (CurrentToken == Y)
            {
                CurrentToken = NextRealToken();
                return Arg.IsAbsolute && IsByteAddress((int)Arg.Resolve()) ? DPGY : ABSY;
            }

            if (CurrentToken == S)
            {
                CurrentToken = NextRealToken();
                return STAC;
            }

            OnError(ERR_EXPECTED_S_OR_X_OR_Y);
            return UNKN;
        }

        if (Arg.IsAbsolute)
        {
            var address = Arg.Resolve();

            if ((Processor & (M65816 | M65832)) == 0) return (address & 0xff00) == 0 ? DPAG : ABSL;
            if ((address & 0xff0000) == 0)
            {
                return IsByteAddress((int)address) ? DPAG : ABSL;
            }

            if (targetBank != PBANK) return ((address & 0xff0000) >> 16) == DataBank ? ABSL : ALNG;
            var origin = Origin;

            if (origin?.IsAbsolute == true)
                return ((origin.Resolve() ^ address) & 0xff0000) == 0 ? ABSL : ALNG;
            return ALNG;

        }

        if (Arg.IsExternal(Origin?.GetSection()))
            return (Processor & (M65816 | M65832)) != 0 ? ALNG : ABSL;
        return ABSL;
    }

    /// <summary>
    /// Parses the data value for an immediate addressing mode to allow short
    /// string literals as well as numbers.
    /// </summary>
    /// <returns>An expression containing the immediate value.</returns>
    public Expr? ParseImmediate()
    {
        if (CurrentToken?.Kind == String)
        {
            var text = CurrentToken.Text;

            if (text.Length > 4)
                OnError(ERR_TEXT_TOO_LONG_FOR_IMMD);

            var value = text.Aggregate(0, (current, t) => (current << 8) | t);

            CurrentToken = NextRealToken();

            return (new Value(null, value));
        }

        var result = ParseExpression();

        if (result == null)
            OnError(ERR_MISSING_EXPRESSION);

        return (result);
    }

    public override int DataBank { get; set; }

    /// <summary>
    /// Generate the code for an implied instruction
    /// </summary>
    /// <param name="opcode">The opcode byte</param>
    public override void GenerateImplied(int opcode)
    {
        AddByte((byte)opcode);
    }

    public override Expr? Arg { get; set; }

    /// <summary>
    /// Generate the code for an immediate instruction
    /// </summary>
    /// <param name="opcode">The opcode byte</param>
    /// <param name="expr">The immediate value</param>
    /// <param name="bits">Determines if an 8 or 16 bit value.</param>
    public override void GenerateImmediate(int opcode, Expr? expr, int bits)
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
    public override void GenerateDirectPage(int opcode, Expr? expr)
    {
        AddByte((byte)opcode);

        if ((DirectPage != 0) && (Processor == M65816))
            Arg = Expr.Sub(expr, new Value(null, DirectPage));

        AddByte(Arg);
    }

    /// <summary>
    /// Generate the code for an instruction with a absolute address.
    /// </summary>
    /// <param name="opcode">The opcode byte.</param>
    /// <param name="expr">The address expression.</param>
    public override void GenerateAbsolute(int opcode, Expr? expr)
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
    public override void GenerateIndirect(int opcode, Expr? expr, bool isLong)
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
    public override void GenerateRelative(int opcode, Expr? expr, bool isLong)
    {
        var origin = Origin;

        if (origin != null)
        {
            AddByte((byte)opcode);
            if (isLong)
            {
                var dist = Expr.Sub(expr, Expr.Add(origin, THREE));
                if (Pass == Pass.FINAL)
                {
                    if (dist.IsAbsolute && ((dist.Resolve() < -32768) || (dist.Resolve() > 32767)))
                        OnError("Relative branch is out of range");
                }

                AddWord(dist);
            }
            else
            {
                var dist = Expr.Sub(expr, Expr.Add(origin, TWO));
                if (Pass == Pass.FINAL)
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
    public override void GenerateLong(int opcode, Expr? expr)
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
    public override void GenerateBranch(Token condition, Expr? target)
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
            var skipOver = Expr.Add(Origin, FIVE);

            if (condition == EQ) GenerateRelative(0xD0, skipOver, false);
            if (condition == NE) GenerateRelative(0xF0, skipOver, false);
            if (condition == CC) GenerateRelative(0xB0, skipOver, false);
            if (condition == CS) GenerateRelative(0x90, skipOver, false);
            if (condition == PL) GenerateRelative(0x30, skipOver, false);
            if (condition == MI) GenerateRelative(0x10, skipOver, false);
            if (condition == VC) GenerateRelative(0x70, skipOver, false);
            if (condition == VS) GenerateRelative(0x50, skipOver, false);

            if ((Processor & (M65816 | M65832)) != 0)
                GenerateRelative(0x82, Expr.Sub(target, TWO), true);
            else
                GenerateAbsolute(0x4C, target);
        }
    }

    public override List<Value?> EndAddr { get; } = new();

    /// <summary>
    /// Generates a jump to a target address using BRA if supported
    /// and within range.
    /// </summary>
    /// <param name="target">The target address.</param>
    public override void GenerateJump(Expr? target)
    {
        if (HasShortBranch() && IsShortDistance(target))
            GenerateRelative(0x80, target, false);
        else
        {
            if ((Processor & (M65816 | M65832)) != 0)
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
    public bool IsShortDistance(Expr? target)
    {
        var offset = Expr.Sub(target, Expr.Add(Origin, TWO));

        if (!offset.IsAbsolute) return (false);
        var distance = (int)offset.Resolve();

        return distance is >= -128 and <= 127;
    }

    /// <summary>
    /// Determines if the current processor supports the BRA opcode.
    /// </summary>
    /// <returns><code>true</code> if BRA is supported.</returns>
    public bool HasShortBranch()
    {
        return ((Processor & (M65C02 | M65SC02 | M65816 | M65832)) != 0);
    }

    //public override Source Source { get; set; }
    //public override Token CurrentToken { get; set; }

    public override int DirectPage { get; set; }

    /// <summary>
    /// Generate the series of bytes for a 24 bit address.
    /// </summary>
    /// <param name="expr">An expression representing the address.</param>
    public override void AddAddress(Expr? expr)
    {
        AddWord(Expr.And(expr, OFFSET));
        AddByte(Expr.Shr(Expr.And(expr, BANK), SIXTEEN));
    }

    public override int IfIndex { get; set; }
    public override int LoopIndex { get; set; }
    public override Stack<int> Ifs { get; } = new();
    public override Stack<int> Loops { get; } = new();
    public override List<Value?> ElseAddr { get; } = new();
    public override List<Value?> EndIfAddr { get; } = new();
    public override List<Value?> LoopAddr { get; } = new();

    /// <summary>
    /// The <code>Option</code> instance use to detect <code>-traditional</code>
    /// </summary>
    private readonly Option traditionalOption = new("-traditional", "Disables structured directives");

    // A Dictionary of keyword tokens to speed up classification
    private readonly Dictionary<string, Token?> tokenDictionary = new();

    /// <summary>
    /// A <code>StringBuffer</code> used to format output.
    /// </summary>
    private readonly StringBuilder output = new();

    //private int ifIndex;

    //private int loopIndex;

    //private readonly Stack<int> ifs = new();

    //private readonly Stack<int> loops = new();

    //private readonly List<Value?> elseAddr = new();

    //private readonly List<Value?> endifAddr = new();

    //private readonly List<Value?> loopAddr = new();

    //private readonly List<Value?> endAddr = new();

    //public override int Processor { get; set; }

    /// <summary>
    /// Adds a CurrentToken to the hash table indexed by its text in UPPER case.
    /// </summary>
    /// <param name="token">The Token to add</param>
    public void AddToken(Token token)
    {
        var key = token.Text.ToUpper();
        tokenDictionary.SafeAdd(key, token);
    }

    public override int BitsA { get; set; }
    public override int BitsI { get; set; }

    /// <summary>
    /// Determines if an address can be represented by a byte.
    /// </summary>
    /// <param name="value">The value to be tested.</param>
    /// <returns>true if the value is a byte, false otherwise.</returns>
    private bool IsByteAddress(int value)
    {
        return ((DirectPage <= value) && (value <= (DirectPage + 0xff)));
    }

}