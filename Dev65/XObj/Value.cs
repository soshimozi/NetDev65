namespace Dev65.XObj;

/// <summary>
/// The <see cref="Value"/> class represents an absolute value or a relative offset (such as an address).
/// </summary>
public sealed class Value : Expr
{
    private readonly Section? _section;
    private readonly long _value;

    /// <summary>
    /// Constructs a <see cref="Value"/> instance from the given <see cref="Section"/> (which may be <c>null</c>) and integer value.
    /// </summary>
    /// <param name="section">The relative section or <c>null</c>.</param>
    /// <param name="value">An integer value.</param>
    public Value(Section? section, long value)
    {
        _section = section;
        _value = value;
    }

    /// <inheritdoc />
    public override bool IsAbsolute => _section == null;
    

    /// <inheritdoc />
    public override bool IsExternal(Section? section)
    {
        return section != _section;
    }

    /// <summary>
    /// Provides access to the section.
    /// </summary>
    /// <returns>The relative section or <c>null</c>.</returns>
    public Section? GetSection()
    {
        return _section;
    }

    /// <summary>
    /// Provides access to the integer part of the value.
    /// </summary>
    /// <returns>The integer value.</returns>
    public long GetValue()
    {
        return _value;
    }

    /// <inheritdoc />
    public override long Resolve(SectionMap? sections = null, SymbolMap? symbols = null)
    {
        if (_section == null || (sections == null && symbols == null))
            return _value;

        return sections?.BaseAddressOf(_section) ?? 0 + _value;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (_section != null)
            return $"<val sect='{_section.GetName()}'>{_value}</val>";
        else
            return $"<val>{_value}</val>";
    }
}
