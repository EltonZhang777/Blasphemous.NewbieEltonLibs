using Blasphemous.NewbieEltonLibs.Extensions.System;
using System;
using System.Text;
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
    /// <param name="name">The non-empty, non-whitespace animation name.</param>
    /// <param name="sprites">The non-empty sequence of non-null animation frames.</param>
    /// <param name="secondsPerFrame">The positive duration of each frame in seconds.</param>
    /// <exception cref="ArgumentException">Thrown when any argument is invalid.</exception>
    public AnimationInfo(string name, Sprite[] sprites, float secondsPerFrame)
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

        if (!ValidationUtils.Validate(sprites, value => value != null, logToModLog: false, throwError: false))
        {
            AppendError(errorMessage, "Animation sprites cannot be null.");
        }
        else if (!ValidationUtils.Validate(sprites, value => value.Length > 0, logToModLog: false, throwError: false))
        {
            AppendError(errorMessage, "An animation must contain at least one frame.");
        }
        else
        {
            for (int index = 0; index < sprites.Length; index++)
            {
                if (!ValidationUtils.Validate(sprites[index], value => value is not null && value != null, logToModLog: false, throwError: false))
                {
                    AppendError(errorMessage, "An animation sprite at index " + index + " cannot be null.");
                }
            }
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
        Sprites = sprites;
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
    /// Gets the sprites displayed by this animation in order.
    /// </summary>
    public Sprite[] Sprites { get; }

    /// <summary>
    /// Gets the duration of each animation frame in seconds.
    /// </summary>
    public float SecondsPerFrame { get; }
}