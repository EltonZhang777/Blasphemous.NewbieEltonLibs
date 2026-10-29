using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Blasphemous.NewbieEltonLibs.Extensions.System;

/// <summary>
/// Compares reference types by object identity.
/// </summary>
/// <typeparam name="T">The reference type to compare.</typeparam>
public sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
{
    /// <summary>
    /// Compares two values by object identity.
    /// </summary>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    /// <returns><see langword="true"/> when both references identify the same object.</returns>
    public bool Equals(T? x, T? y)
    {
        return ReferenceEquals(x, y);
    }

    /// <summary>
    /// Gets a hash code based on object identity.
    /// </summary>
    /// <param name="obj">The value to hash.</param>
    /// <returns>The runtime identity hash code for <paramref name="obj"/>.</returns>
    public int GetHashCode(T obj)
    {
        return RuntimeHelpers.GetHashCode(obj);
    }
}