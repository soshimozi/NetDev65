using Dev65.XApp;

namespace Dev65.XLnk;

using Dev65.XObj;
using System;

/// <summary>
/// The <see cref="Region"/> class contains the lower and upper address of a memory block from which areas are consumed as code and data modules are processed.
/// </summary>
public sealed class Region
{
    private long start;
    private long end;

    /// <summary>
    /// Constructs a <see cref="Region"/> to represent the indicated memory area range.
    /// </summary>
    /// <param name="start">The start address of the memory block.</param>
    /// <param name="end">The end address of the memory block.</param>
    public Region(string start, string end)
    {
        this.start = ParseAddress(start);
        this.end = ParseAddress(end);
    }

    /// <summary>
    /// Returns the start address of the <see cref="Region"/>.
    /// </summary>
    /// <returns>The start address of the <see cref="Region"/>.</returns>
    public long GetStart()
    {
        return start;
    }

    /// <summary>
    /// Returns the end address of the <see cref="Region"/>.
    /// </summary>
    /// <returns>The end address of the <see cref="Region"/>.</returns>
    public long GetEnd()
    {
        return end;
    }

    /// <summary>
    /// Returns the remaining size of the <see cref="Region"/>.
    /// </summary>
    /// <returns>The remaining size of the <see cref="Region"/>.</returns>
    public int GetSize()
    {
        return start <= end ? (int)(end - start + 1) : 0;
    }

    /// <summary>
    /// Reduces the region by reserving a number of bytes at its start.
    /// </summary>
    /// <param name="size">The amount to reserve.</param>
    public void Reserve(int size)
    {
        start += size;
    }

    /// <summary>
    /// Splits a region at a given address returning the tail instance.
    /// </summary>
    /// <param name="address">Where to split.</param>
    /// <returns>The tail region.</returns>
    public Region Split(long address)
    {
        var tail = new Region(address, end);
        end = address - 1;
        return tail;
    }

    /// <summary>
    /// Constructs a <see cref="Region"/> for the indicated address range.
    /// </summary>
    /// <param name="start">The start of the region.</param>
    /// <param name="end">The end of the region.</param>
    private Region(long start, long end)
    {
        this.start = start;
        this.end = end;
    }

    /// <summary>
    /// Parses an address expressed in hex.
    /// </summary>
    /// <param name="address">The address string.</param>
    /// <returns>The parsed address.</returns>
    private static long ParseAddress(string address)
    {
        try
        {
            return long.Parse(address.ToUpper(), System.Globalization.NumberStyles.HexNumber);
        }
        catch (Exception)
        {
            Console.Error.WriteLine($"Error: Invalid hex address ({address})");

            if(Application.CurrentApplication != null)
                Application.CurrentApplication.IsFinished = true;
        }
        return 0;
    }

    /// <summary>
    /// Returns a string representation of the <see cref="Region"/>.
    /// </summary>
    /// <returns>A string representing the <see cref="Region"/>.</returns>
    public override string ToString()
    {
        return $"${Hex.ToHex(start, 8)}-${Hex.ToHex(end, 8)}";
    }
}
