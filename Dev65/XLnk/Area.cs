using Dev65.XObj;

namespace Dev65.XLnk;

using System;
using System.Collections.Generic;

/// <summary>
/// The <see cref="Area"/> class keeps a record of all the memory areas where
/// a type of section can be placed. As objects are assigned to absolute
/// locations the memory area list is updated to keep track of the remaining
/// space.
/// </summary>
public sealed class Area
{
    private List<Region> regions = new List<Region>();

    /// <summary>
    /// Constructs an <see cref="Area"/> given a string containing a list of memory address pairs in hexadecimal (e.g., 'FF00-FDFF,FF00-FFFF').
    /// </summary>
    /// <param name="location">The memory address pairs.</param>
    public Area(string location)
    {
        var pairs = location.Split(',');

        foreach (var pair in pairs)
        {
            var addrs = pair.Split('-');

            if (addrs.Length != 2)
            {
                Console.Error.WriteLine("Invalid address pair (" + pair + ")");
                Environment.Exit(1);
            }

            var region = new Region(addrs[0], addrs[1]);

            var handled = false;
            for (var position = 0; position < regions.Count; position++)
            {
                var other = regions[position];

                if (region.GetStart() < other.GetStart())
                {
                    regions.Insert(position, region);
                    handled = true;
                    break;
                }
            }

            if (!handled)
                regions.Add(region);
        }
    }

    /// <summary>
    /// Provides access to the list of current free regions for this <see cref="Area"/>.
    /// </summary>
    /// <returns>The list of free regions for this <see cref="Area"/>.</returns>
    public List<Region> GetRegions()
    {
        return regions;
    }

    /// <summary>
    /// Determines the lowest free memory address for this <see cref="Area"/>.
    /// </summary>
    /// <returns>The lowest free memory address.</returns>
    public long GetLoAddr()
    {
        return regions[0].GetStart();
    }

    /// <summary>
    /// Determines the highest free memory address for this <see cref="Area"/>.
    /// </summary>
    /// <returns>The highest free memory address.</returns>
    public long GetHiAddr()
    {
        return regions[^1].GetEnd();
    }

    /// <summary>
    /// Attempts to fit the given <see cref="Section"/> into the first suitable <see cref="Region"/> controlled by this <see cref="Area"/>.
    /// </summary>
    /// <param name="section">The <see cref="Section"/> to be fitted.</param>
    /// <returns>The address where the <see cref="Section"/> was placed.</returns>
    public long FitSection(Section section)
    {
        long addr = -1;
        var size = section.GetSize();

        if (section.IsAbsolute())
        {
            addr = section.GetStart();

            // Find the region that contains the section
            for (var index = 0; index < regions.Count; index++)
            {
                var region = regions[index];

                if (region.GetStart() <= addr && (addr + size - 1) <= region.GetEnd())
                {
                    if (region.GetStart() == addr)
                    {
                        region.Reserve(size);
                    }
                    else
                    {
                        region = region.Split(addr);
                        regions.Insert(index + 1, region);
                        region.Reserve(size);
                    }

                    break;
                }
            }
        }
        else
        {
            for (var index = 0; index < regions.Count; index++)
            {
                var region = regions[index];

                // Find first region large enough to hold section
                if (region.GetSize() >= size)
                {
                    addr = region.GetStart();
                    region.Reserve(size);
                    break;
                }
            }
        }

        return addr;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return "{" + string.Join(", ", regions) + "}";
    }
}
