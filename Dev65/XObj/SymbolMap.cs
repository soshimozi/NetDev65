namespace Dev65.XObj;
using System.Collections.Generic;

/// <summary>
/// A <see cref="SymbolMap"/> holds the details of where symbols have been placed in memory.
/// </summary>
public sealed class SymbolMap
{
    private Dictionary<string, long> map = new Dictionary<string, long>();

    /// <summary>
    /// Adds an entry to the lookup table for the given symbol and address.
    /// </summary>
    /// <param name="name">The symbol name.</param>
    /// <param name="value">Its memory address.</param>
    public void AddAddress(string name, long value)
    {
        map[name] = value;
    }

    /// <summary>
    /// Looks up the address allocated to the given symbol.
    /// </summary>
    /// <param name="name">The target symbol name.</param>
    /// <returns>The associated memory address.</returns>
    public long AddressOf(string name)
    {
        return map[name];
    }

    /// <summary>
    /// Returns a list containing all the symbol names in the map.
    /// </summary>
    /// <returns>A list of symbol names.</returns>
    public List<string> GetSymbols()
    {
        return new List<string>(map.Keys);
    }
}
