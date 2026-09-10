using Framework.FrameworkCore;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.Extensions.GameLibs;

/// <summary>
/// Provides conversions from game entity orientations to directional vectors.
/// </summary>
public static class EntityOrientationExtensions
{
    /// <summary>
    /// Converts an entity orientation to its normalized horizontal direction.
    /// </summary>
    /// <param name="entityOrientation">The orientation to convert.</param>
    /// <returns><see cref="Vector2.right"/> for right and <see cref="Vector2.left"/> for left.</returns>
    public static Vector2 ToDirectionalVector(this EntityOrientation entityOrientation)
    {
#pragma warning disable CS8524 // Undefined enum values must retain the source fail-fast behavior.
        return entityOrientation switch
        {
            EntityOrientation.Right => Vector2.right,
            EntityOrientation.Left => Vector2.left
        };
#pragma warning restore CS8524
    }
}
