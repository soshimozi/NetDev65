namespace Dev65.XAsm;

/// <summary>
/// The abstract <see cref="Opcode"/> class provides a means to associate a <see cref="Token"/> with its compile time effect.
/// </summary>
/// <author>Andrew Jacobs</author>
/// <version>$Id$</version>
public class Opcode<TAssembler> : Token, ICompilable<TAssembler> where TAssembler : IAssembler
{
    /// <summary>
    /// Constructs an <see cref="Opcode"/> instance.
    /// </summary>
    /// <param name="kind"></param>
    /// <param name="text"></param>
    /// <param name="compileAction"></param>
    /// <param name="alwaysActive">Marks an opcode that controls conditional compilation.</param>
    public Opcode(TokenKind kind, string text, Func<TAssembler, bool>? compileAction = null, bool alwaysActive = false) 
        : base(kind, text)
    {
        _compileAction = compileAction;
        this.alwaysActive = alwaysActive;
    }

    /// <summary>
    /// Determines if this <see cref="Opcode"/> is active regardless of the current conditional compilation state.
    /// </summary>
    /// <returns><c>true</c> if this <see cref="Opcode"/> should be processed.</returns>
    public bool IsAlwaysActive => alwaysActive;

    /// <summary>
    /// Performs the compilation effect of the <see cref="Opcode"/>.
    /// </summary>
    /// <returns><c>true</c> if the <see cref="Opcode"/> can have a label.</returns>
    public virtual bool Compile(TAssembler assembler)
    {
        if(_compileAction != null)
            return _compileAction(assembler);
        return false;
    }

    /// <summary>
    /// A flag indicating an <see cref="Opcode"/> which is always processed.
    /// </summary>
    private readonly bool alwaysActive;

    private readonly Func<TAssembler, bool>? _compileAction;
}
