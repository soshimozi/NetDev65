using Dev65.XObj;

namespace Dev65.XAsm;

// marker interface
public interface IAssembler
{
    Pass? Pass { get; set; }
    Token? CurrentToken { get; set; }
    Token? NextRealToken();
    int Processor { get; set; }

    void AddToken(Token token);
    void DoSet(string label, Expr? value);
    int BitsA { get; set; }
    int BitsI { get; set; }

    void SetSection(string section);
    Expr? ParseExpression();
    Expr? ParseImmediate();
    int DataBank { get; set; }
    void OnError(string message);
    void OnWarning(string message);
    int DirectPage { get; set; }

    void AddAddress(Expr? expr);

    int IfIndex { get; set; }
    int LoopIndex { get; set; }

    Stack<int> Ifs { get; }
    Stack<int> Loops { get; }
    List<Value?> ElseAddr { get; }
    List<Value?> EndIfAddr { get; }
    List<Value?> LoopAddr { get; }
    List<Value?> EndAddr { get; }
    void GenerateJump(Expr? target);
    void GenerateDirectPage(int opcode, Expr? expr);
    void GenerateBranch(Token condition, Expr? target);
    void GenerateAbsolute(int opcode, Expr? expr);
    void GenerateIndirect(int opcode, Expr? expr, bool isLong);
    void GenerateRelative(int opcode, Expr? expr, bool isLong);
    void GenerateLong(int opcode, Expr? expr);
    bool IsShortDistance(Expr? target);
    bool HasShortBranch();

    Value? Origin { get; set; }
    void AddByte(Expr? expr);
    void AddByte(byte value);
    int ParseMode(int bank);
    void GenerateImmediate(int opcode, Expr? expr, int bits);
    void GenerateImplied(int opcode);
    Expr? Arg { get; }
    FileStream? FindFile(string filename, bool search);
    Stack<ISource> Sources { get; }

    Expr? Addr { get; set; }
    Token? Label { get; set; }
    HashSet<string> Variable { get; }
    Dictionary<string, Expr?> Symbols { get; }

    HashSet<string> NotLocal { get; }


    // The set of symbol which have been imported.
    HashSet<string> Externals { get; }
    void AddWord(Expr? expr);
    void AddWord(long value);
    void AddLong(Expr? expr);
    void AddLong(long value);
    bool IsActive { get; }
    Stack<bool> Status { get; }
    string MacroName { get; set; }
    TextSource? SavedLines { get; set; }
    Dictionary<string, TextSource> Macros { get; }
    Dictionary<string, Section?> Sections { get; }
    bool ThrowPage { get; set; }

    // The module being generated.
    Module? Module { get; }

    // The current sections.
    Section? Section { get; set; }

    HashSet<string> Globals { get; }
    string SectionName { get; set; }
    bool Listing { get; set; }
    string Title { get; set; }

    Stack<Token> Tokens { get; }
}