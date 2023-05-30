using System.Text;

namespace Dev65.XObj;

/**
 * A <CODE>Library</CODE> instance contains a complete code library.
 * 
 * author Andrew Jacobs
 * version	$Id$
 */
public sealed class Library
{
    private List<Module> modules = new List<Module>();

    public Library()
    { }

    public void Clear()
    {
        modules.Clear();
    }

    public void AddModule(Module module)
    {
        modules.Add(module);
    }

    public bool UpdateModule(Module module)
    {
        for (var index = 0; index < modules.Count; ++index)
        {
            var target = modules[index];

            if (target.Name == module.Name)
            {
                modules[index] = module;
                return true;
            }
        }
        modules.Add(module);
        return false;
    }

    public bool RemoveModule(Module module)
    {
        for (var index = 0; index < modules.Count; ++index)
        {
            var target = modules[index];

            if (target.Name == module.Name)
            {
                modules.RemoveAt(index);
                return true;
            }
        }
        return false;
    }

    public Module[] GetModules()
    {
        var result = new Module[modules.Count];

        for (var index = 0; index < modules.Count; ++index)
            result[index] = modules[index];

        return result;
    }

    /**
     * Returns the XML representation of the library.
     */
    public override string ToString()
    {
        var buffer = new StringBuilder();

        buffer.Append("<library>");
        foreach (var module in modules)
            buffer.Append(module);
        buffer.Append("</library>");

        return buffer.ToString();
    }
}
