using System.Diagnostics;
using Dev65.XApp;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using Dev65.XObj;
using System.Xml.Linq;

namespace Dev65.XLnk;

public class LinkerErrorEventArgs : EventArgs
{
    public string Message { get; }

    public Exception? Exception { get; }

    public LinkerErrorEventArgs(string message, Exception? ex)
    {
        Message = message;
        Exception = ex;
    }
}

public class LinkerWarningEventArgs : EventArgs
{
    public string Message { get; }

    public LinkerWarningEventArgs(string message)
    {
        Message = message;
    }
}

public abstract class Linker : Application
{
    private readonly int _byteSize;
    private readonly long _byteMask;

    public event EventHandler<LinkerErrorEventArgs> LinkerError;
    public event EventHandler<LinkerWarningEventArgs> LinkerWarning; 

    // the .CODE, .DATA, and .BSS areas
    private Dictionary<string, Area> areas = new();

    private Target? target;

    private Option code = new("-code", "Code region(s)", "<regions>");

    private Option data = new("-data", "Data region(s)", "<regions>");

    private Option bss = new("-bss", "BSS region(s)", "<regions>");

    private Option hex = new("-hex", "Generate HEX output");

    private Option ihx = new("-ihx", "Generate Intel HEX output");

    private Option bin = new("-bin", "Generate binary output");

    private Option wdc = new("-wdc", "Generate WDC binary output");

    private Option dmp = new("-dmp", "Generate Dump HEX output");

    private Option cdo = new("-c", "Generate C data output");

    private Option output = new("-output", "Output file", "<file>");

    /**
	 * The set of modules to be linked.
	 */
    private List<Module?> modules = new();

    private List<Library?> libraries = new();

    protected Linker(int byteSize)
    {
        _byteSize = byteSize;
        _byteMask = (1L << byteSize) - 1;
    }

    protected override void StartUp()
    {
        base.StartUp();

        CreateAreas();

        var count = 0;
        if (hex.IsPresent) ++count;
        if (ihx.IsPresent) ++count;
        if (bin.IsPresent) ++count;
        if (wdc.IsPresent) ++count;
        if (dmp.IsPresent) ++count;
        if (cdo.IsPresent) ++count;

        switch (count)
        {
            case 0:
                OnError("No output format selected (-bin, -hex, -ihx, -dmp, -c, or -wdc");
                IsFinished = true;
                return;
            case > 1:
                OnError("Only one output format can be selected at a time.");
                IsFinished = true;
                return;
        }

        var hi = 0x00000000L;
        var lo = 0xffffffffL;

        if (areas.TryGetValue(".code", out var area))
        {
            if (area.GetHiAddr() > hi) hi = area.GetHiAddr();
            if (area.GetLoAddr() < lo) lo = area.GetLoAddr();
        }

        if (areas.TryGetValue(".data", out area))
        {
            if (area.GetHiAddr() > hi) hi = area.GetHiAddr();
            if (area.GetLoAddr() < lo) lo = area.GetLoAddr();
        }

        if (wdc.IsPresent)
        {
            target = new WDCTarget(lo, hi, _byteSize);
        }
        else if (hex.IsPresent)
        {
            target = new HexTarget(lo, hi, _byteSize);
        }
        else if (dmp.IsPresent)
        {
            target = new DumpTarget(lo, hi, _byteSize);
        }
        else if (bin.IsPresent)
        {
            target = new BinTarget(lo, hi, _byteSize);
        }

        if (GetArguments()?.Length != 0) return;

        OnError("Error: No object or library files specified");
        IsFinished = true;
    }

    protected override void Execute()
    {
        var arguments = GetArguments();

        if (arguments == null) return;

        // Stage I - load all the modules and libraries
        foreach (var arg in arguments)
        {
            if (arg?.EndsWith(".obj") == true)
            {
                var obj = Parser.Parse(arg);
                if (obj != null && obj is Module module)
                {
                    if (!modules.Contains(module))
                    {
                        modules.Add(module);
                    }
                    else
                    {
                        OnWarning($"Module '{arg}' specified more than once.");
                    }
                }
                else
                {
                    OnError($"Invalid object file '{arg}'");
                    IsFinished = true;
                }
            } else if (arg?.EndsWith(".lib") == true)
            {
                var obj = Parser.Parse(arg) as Library;
                if (obj == null)
                {
                    OnError($"Invalid library file '{arg}'");
                    IsFinished = true;
                }
                else
                {
                    if (!libraries.Contains(obj))
                    {
                        libraries.Add(obj);
                    }
                    else
                    {
                        OnWarning($"Library '{arg}' specified more than once");
                    }
                }

            }
            else
            {
                OnError($"Unrecognized file type for '{arg}'");
                IsFinished = true;
            }
        }

        if (IsFinished) return;

        // Stage II - process all the modules that must be linked
        foreach (var module in modules)
        {
            ProcessModule(module);
        }

        // Stage III - process libraries for any required modules

        // Stage IV - Sort sections by type and size

        // Stage V - Fit sections into available memory

        // Stage VI - Calculate all the global symbol addresses
        
        // Stage VII - Copy code to target fixing cross references
    }

    private void ProcessModule(Module? module)
    {
        throw new NotImplementedException();
    }

    protected override void CleanUp()
    {
        base.CleanUp();
    }

    protected override string DescribeArguments()
    {
        return " <object/library file> ...";
    }

    protected virtual void OnError(string message, Exception? ex = null)
    {
        OnLinkerError(new LinkerErrorEventArgs(message, ex));
    }

    protected virtual void OnWarning(string message)
    {
        OnLinkerWarning(new LinkerWarningEventArgs(message));
    }

    protected virtual void CreateAreas()
    {
        // Created real areas
        if (code.Value != null)
        {
            if (code.Value.Contains("-"))
                AddArea(".code", code.Value);
        }
        if (data.Value != null)
        {
            if (data.Value.Contains("-"))
                AddArea(".data", data.Value);
        }
        if (bss.Value != null)
        {
            if (bss.Value.Contains("-"))
                AddArea(".bss", bss.Value);
        }

        // If no data area defined alias it to the code.
        if (!areas.ContainsKey(".data"))
            areas.Add(".data", areas[".code"]);
    }

    private void AddArea(string name, string location)
    {
        var area = new Area(location);
        areas.SafeAdd(name, area);
    }

    private void OnLinkerError(LinkerErrorEventArgs e)
    {
        LinkerError?.Invoke(this, e);
    }

    private void OnLinkerWarning(LinkerWarningEventArgs e)
    {
        LinkerWarning?.Invoke(this, e); 
    }
}