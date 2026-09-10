using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.Serialization;

/// <summary>
/// Serializable representation of a <see cref="Vector3"/>.
/// </summary>
public readonly record struct SerializableVector3
{
    /// <summary>
    /// Gets the X coordinate.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Gets the Y coordinate.
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// Gets the Z coordinate.
    /// </summary>
    public float Z { get; }

    /// <summary>
    /// Creates a serializable vector with the specified coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    public SerializableVector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Formats this vector as a parenthesized coordinate tuple.
    /// </summary>
    /// <returns>The formatted coordinate tuple.</returns>
    public override string ToString() => $"({X}, {Y}, {Z})";

    /// <summary>
    /// Gets the zero vector.
    /// </summary>
    public static SerializableVector3 Zero => new(0, 0, 0);

    /// <summary>
    /// Gets the one vector.
    /// </summary>
    public static SerializableVector3 One => new(1, 1, 1);

    /// <summary>
    /// Converts a serializable vector to a <see cref="Vector3"/>.
    /// </summary>
    /// <param name="value">The vector to convert.</param>
    public static implicit operator Vector3(SerializableVector3 value) => new(value.X, value.Y, value.Z);

    /// <summary>
    /// Converts a <see cref="Vector3"/> to a serializable vector.
    /// </summary>
    /// <param name="value">The vector to convert.</param>
    public static implicit operator SerializableVector3(Vector3 value) => new(value.x, value.y, value.z);

    /// <summary>
    /// Converts a serializable vector to a <see cref="Vector2"/>.
    /// </summary>
    /// <param name="value">The vector to convert.</param>
    public static implicit operator Vector2(SerializableVector3 value) => new(value.X, value.Y);

    /// <summary>
    /// Converts a <see cref="Vector2"/> to a serializable vector with a zero Z coordinate.
    /// </summary>
    /// <param name="value">The vector to convert.</param>
    public static implicit operator SerializableVector3(Vector2 value) => new(value.x, value.y, 0);
}
