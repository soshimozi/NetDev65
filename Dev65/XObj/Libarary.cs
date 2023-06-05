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
    private readonly List<Module?> _modules = new();

    public Library()
    { }

    public void Clear()
    {
        _modules.Clear();
    }

    public void AddModule(Module? module)
    {
        _modules.Add(module);
    }

    public bool UpdateModule(Module module)
    {
        for (var index = 0; index < _modules.Count; ++index)
        {
            var target = _modules[index];

            if (target?.Name == module.Name)
            {
                _modules[index] = module;
                return true;
            }
        }
        _modules.Add(module);
        return false;
    }

    public bool RemoveModule(Module module)
    {
        for (var index = 0; index < _modules.Count; ++index)
        {
            var target = _modules[index];

            if (target?.Name == module.Name)
            {
                _modules.RemoveAt(index);
                return true;
            }
        }
        return false;
    }

    public Module?[] GetModules()
    {
        var result = new Module?[_modules.Count];

        for (var index = 0; index < _modules.Count; ++index)
            result[index] = _modules[index];

        return result;
    }

    /**
     * Returns the XML representation of the library.
     */
    public override string ToString()
    {
        var buffer = new StringBuilder();

        buffer.Append("<library>");
        foreach (var module in _modules)
            buffer.Append(module);
        buffer.Append("</library>");

        return buffer.ToString();
    }
}
