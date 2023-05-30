using System.Text;

namespace Dev65.XApp;

/// <summary>
/// The <c>Option</c> class provides a basic command line processing capability. 
/// Instances of <c>Option</c> define the keywords to look for and the presence of associated parameters.
/// </summary>
public sealed class Option
{
    private static readonly List<Option> Options = new();
    private readonly string? name;
    private readonly string description;
    private readonly string? parameter;
    private bool present;
    private string? value;

    public Option(string name, string description, string? parameter = null)
    {
        this.name = name;
        this.description = description;
        this.parameter = parameter;
        Options.Add(this);
    }

    public bool IsPresent => present;

    public string? Value => value;

    public override string ToString()
    {
        return GetType().Name + "[" + ToDebug() + "]";
    }

    public static string?[] ProcessOptions(string?[] arguments)
    {
        int index;

        for (index = 0; index < arguments.Length; ++index)
        {
            var matched = false;

            foreach (var option in Options)
            {
                matched = arguments[index]?.Equals(option.name) ?? false;
                if (!matched) continue;
                option.present = true;
                if (option.parameter != null)
                    option.value = arguments[++index];
                break;
            }

            if (!matched) break;
        }

        var remainder = new string?[arguments.Length - index];
        for (var count = 0; index < arguments.Length;)
            remainder[count++] = arguments[index++];

        return remainder;
    }

    public static string ListOptions()
    {
        var buffer = new StringBuilder();

        foreach (var option in Options)
        {
            if (buffer.Length == 0) buffer.Append(' ');

            buffer.Append('[');
            buffer.Append(option.name);
            if (option.parameter != null)
            {
                buffer.Append(' ');
                buffer.Append(option.parameter);
            }
            buffer.Append(']');
        }

        return buffer.ToString();
    }

    public static void DescribeOptions()
    {
        var spaces = new string(' ', 44);

        foreach (var option in Options)
        {
            if (option.parameter != null)
                Console.Error.WriteLine("    "
                    + (option.name + " " + option.parameter + spaces).Substring(0, 16)
                    + " " + option.description);
            else
                Console.Error.WriteLine("    "
                    + (option.name + spaces).Substring(0, 16)
                    + " " + option.description);
        }
    }

    private string ToDebug()
    {
        var buffer = new StringBuilder();

        buffer.Append($"name={name ?? "null"}");
        buffer.Append($",description={description}");
        buffer.Append($",parameter={parameter ?? "null"}");
        buffer.Append($",present={present}");

        return buffer.ToString();
    }
}
