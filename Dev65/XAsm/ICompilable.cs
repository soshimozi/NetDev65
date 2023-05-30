namespace Dev65.XAsm;

public interface ICompilable<in TAssembler>
{
    bool Compile(TAssembler assembler);
}