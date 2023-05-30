using Dev65.XAsm;
using Dev65.XObj;
using System.Diagnostics;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Dev65.XApp;

/// <summary>
/// Abstract base class for application execution logic.
/// </summary>
public abstract class Application
{
    private static Application? _application;
    private string?[]? arguments;
    private bool finished;
    private readonly Option helpOption = new("-help", "Displays help information");

    /// <summary>
    /// Gets the current application instance.
    /// </summary>
    public static Application? CurrentApplication => _application;

    protected Application()
    {
        _application = this;
    }

    /// <summary>
    /// Begins execution of the application with the given command line arguments.
    /// </summary>
    public void Run(string?[] args)
    {
        this.arguments = Option.ProcessOptions(args);

        StartUp();
        while (!finished)
            Execute();
        CleanUp();
    }

    /// <summary>
    /// Indicates whether the execution of the application has finished.
    /// </summary>
    public bool IsFinished
    {
        get => finished;
        set => finished = value;
    }



public override string ToString() => $"{GetType().Name}[{ToDebug()}]";

    protected abstract void Execute();

    protected virtual void StartUp()
    {
        if (!helpOption.IsPresent) return;

        Console.Error.WriteLine($"Usage:\n    dotnet {GetType().Name}{Option.ListOptions()}{DescribeArguments()}");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Options:");
        Option.DescribeOptions();
        Environment.Exit(1);
    }

    protected virtual void CleanUp()
    {
    }

    protected virtual string DescribeArguments()
    {
        return "";
    }

    protected string?[]? GetArguments()
    {
        return arguments;
    }

    protected virtual string? GetSystemPreferencesRoot()
    {
        return null;
    }

    protected virtual string? GetUserPreferencesRoot()
    {
        return GetSystemPreferencesRoot();
    }

    protected virtual string ToDebug()
    {
        var buffer = new StringBuilder();

        buffer.Append("arguments=");
        if (arguments != null)
        {
            buffer.Append('[');
            for (var index = 0; index != arguments.Length; ++index)
            {
                if (index != 0) buffer.Append(',');

                if (arguments[index] != null)
                    buffer.Append($"\"{arguments[index]}\"");
                else
                    buffer.Append("null");
            }
            buffer.Append(']');
        }
        else
            buffer.Append("null");

        buffer.Append(",finished=" + finished);

        return buffer.ToString();
    }
}
