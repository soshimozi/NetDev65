﻿namespace Dev65.XObj;

/// <summary>
/// A <see cref="SectionMap"/> holds the details of where <see cref="Section"/> instances have been placed in memory.
/// </summary>
public sealed class SectionMap
{
    private readonly Dictionary<Section, long> _map = new();

    /// <summary>
    /// Determines the base address of the given <see cref="Section"/>.
    /// </summary>
    /// <param name="section">The target <see cref="Section"/>.</param>
    /// <returns>The base address of the section.</returns>
    public long BaseAddressOf(Section section)
    {
        return _map[section];
    }

    public void SetBaseAddress(Section section, long addr)
    {
        _map[section] = addr;
    }

    /// <summary>
    /// Returns a list of all the code sections.
    /// </summary>
    /// <returns>A list of all the code sections.</returns>
    public List<Section> GetSections()
    {
        return _map.Keys.ToList();
    }
}