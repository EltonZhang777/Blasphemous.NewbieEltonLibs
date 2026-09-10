using System;

namespace Blasphemous.NewbieEltonLibs.Storage;

/// <summary>
/// Describes the spritesheet data needed to import an animation.
/// </summary>
public class AnimationImportInfo
{
    /// <summary>
    /// Creates validated animation import information.
    /// </summary>
    /// <param name="name">The non-empty animation name.</param>
    /// <param name="filePath">The non-empty path to the animation spritesheet.</param>
    /// <param name="width">The positive width of each spritesheet frame.</param>
    /// <param name="height">The positive height of each spritesheet frame.</param>
    /// <param name="secondsPerFrame">The positive duration of each frame in seconds.</param>
    /// <exception cref="ArgumentException">Thrown when a string argument is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when a string argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument is not positive.</exception>
    public AnimationImportInfo(string name, string filePath, int width, int height, float secondsPerFrame)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("An animation name cannot be empty.", nameof(name));
        }

        if (filePath == null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (filePath.Length == 0)
        {
            throw new ArgumentException("An animation file path cannot be empty.", nameof(filePath));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Frame width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Frame height must be positive.");
        }

        if (!(secondsPerFrame > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(secondsPerFrame), "Frame duration must be positive.");
        }

        Name = name;
        FilePath = filePath;
        Width = width;
        Height = height;
        SecondsPerFrame = secondsPerFrame;
    }

    /// <summary>
    /// Gets the unique animation name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the path to the animation spritesheet.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the width of each spritesheet frame.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of each spritesheet frame.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the duration of each animation frame in seconds.
    /// </summary>
    public float SecondsPerFrame { get; }
}
