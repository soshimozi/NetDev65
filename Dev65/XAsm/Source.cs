﻿using System;
namespace Dev65.XAsm;

using System;

/// <summary>
/// The <see cref="ISource"/> interface defines a common way to get the next line of source code from a file, a macro, or a repeat section.
/// </summary>
/// <author>Andrew Jacobs</author>
/// <version>$Id$</version>
public interface ISource : IDisposable
{
    /// <summary>
    /// Fetches the next line of source text from the underlying storage and bundles it with its origin details.
    /// </summary>
    /// <returns>The next line of source text or <c>null</c> if the end of this <see cref="ISource"/> has been reached.</returns>
    Line? NextLine();
}
