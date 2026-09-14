using Blasphemous.NewbieEltonLibs.Extensions.System;
using System;
using System.Text;

namespace Blasphemous.NewbieEltonLibs.Storage;

/// <summary>
/// Describes the spritesheet data needed to import an animation.
/// </summary>
public class AnimationImportInfo
{
    /// <summary>
    /// Creates validated animation import information.
    /// </summary>
    /// <param name="name">The non-empty, non-whitespace animation name.</param>
    /// <param name="filePath">The non-empty, non-whitespace path to the animation spritesheet.</param>
    /// <param name="width">The positive width of each spritesheet frame.</param>
    /// <param name="height">The positive height of each spritesheet frame.</param>
    /// <param name="secondsPerFrame">The positive duration of each frame in seconds.</param>
    /// <exception cref="ArgumentException">Thrown when any argument is invalid.</exception>
    public AnimationImportInfo(string name, string filePath, int width, int height, float secondsPerFrame)
    {
        StringBuilder errorMessage = new();

        if (!ValidationUtils.Validate(name, value => value != null, logToModLog: false, throwError: false))
        {
            AppendError(errorMessage, "An animation name cannot be null.");
        }
        else if (!ValidationUtils.Validate(name, value => value.Length > 0, logToModLog: false, throwError: false))
        {
            AppendError(errorMessage, "An animation name cannot be empty.");
        }
        else if (!ValidationUtils.Validate(name, value => value.Trim().Length > 0, logToModLog: false, throwError: false))
        {
            AppendError(errorMessage, "An animation name cannot be whitespace.");
        }

        if (!ValidationUtils.Validate(filePath, value => value != null, logToModLog: false, throwError: false))
        {
            AppendError(errorMessage, "An animation file path cannot be null.");
        }
        else if (!ValidationUtils.Validate(filePath, value => value.Length > 0, logToModLog: false, throwError: false))
        {
            AppendError(errorMessage, "An animation file path cannot be empty.");
        }
        else if (!ValidationUtils.Validate(filePath, value => value.Trim().Length > 0, logToModLog: false, throwError: false))
        {
            AppendError(errorMessage, "An animation file path cannot be whitespace.");
        }

        if (!ValidationUtils.Validate(width, value => value > 0, logToModLog: false, throwError: false))
        {
            AppendError(errorMessage, "Frame width must be positive.");
        }

        if (!ValidationUtils.Validate(height, value => value > 0, logToModLog: false, throwError: false))
        {
            AppendError(errorMessage, "Frame height must be positive.");
        }

        if (!ValidationUtils.Validate(secondsPerFrame, value => value > 0, logToModLog: false, throwError: false))
        {
            AppendError(errorMessage, "Frame duration must be positive.");
        }

        if (errorMessage.Length > 0)
        {
            throw new ArgumentException(errorMessage.ToString());
        }

        Name = name;
        FilePath = filePath;
        Width = width;
        Height = height;
        SecondsPerFrame = secondsPerFrame;
    }

    private static void AppendError(StringBuilder errorMessage, string message)
    {
        if (errorMessage.Length > 0)
        {
            errorMessage.Append(Environment.NewLine);
        }

        errorMessage.Append(message);
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