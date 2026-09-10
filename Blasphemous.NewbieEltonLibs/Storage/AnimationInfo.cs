using System;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.Storage;

/// <summary>
/// Describes a named animation and its frame timing.
/// </summary>
public class AnimationInfo
{
    /// <summary>
    /// Creates a validated animation description.
    /// </summary>
    /// <param name="name">The non-empty animation name.</param>
    /// <param name="sprites">The non-empty sequence of animation frames.</param>
    /// <param name="secondsPerFrame">The positive duration of each frame in seconds.</param>
    /// <exception cref="ArgumentException">Thrown when the name is empty or the frame sequence is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the name or frame sequence is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="secondsPerFrame" /> is not positive.</exception>
    public AnimationInfo(string name, Sprite[] sprites, float secondsPerFrame)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("An animation name cannot be empty.", nameof(name));
        }

        if (sprites == null)
        {
            throw new ArgumentNullException(nameof(sprites));
        }

        if (sprites.Length == 0)
        {
            throw new ArgumentException("An animation must contain at least one frame.", nameof(sprites));
        }

        if (secondsPerFrame <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(secondsPerFrame), "Frame duration must be positive.");
        }

        Name = name;
        Sprites = sprites;
        SecondsPerFrame = secondsPerFrame;
    }

    /// <summary>
    /// Gets the unique animation name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the sprites displayed by this animation in order.
    /// </summary>
    public Sprite[] Sprites { get; }

    /// <summary>
    /// Gets the duration of each animation frame in seconds.
    /// </summary>
    public float SecondsPerFrame { get; }
}
