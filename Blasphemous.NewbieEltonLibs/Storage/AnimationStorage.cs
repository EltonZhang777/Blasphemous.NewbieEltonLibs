using System;
using System.Collections.Generic;
using Blasphemous.NewbieEltonLibs.Extensions.System;

namespace Blasphemous.NewbieEltonLibs.Storage;

/// <summary>
/// Stores animations registered by one consuming mod.
/// </summary>
public class AnimationStorage
{
    private readonly Dictionary<string, AnimationInfo> _animations = new Dictionary<string, AnimationInfo>();

    /// <summary>
    /// Creates an empty animation registry.
    /// </summary>
    public AnimationStorage()
    {
    }

    /// <summary>
    /// Gets a required animation by its registered name.
    /// </summary>
    /// <param name="name">The registered animation name.</param>
    /// <returns>The animation registered under <paramref name="name" />.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the name is not registered.</exception>
    public AnimationInfo this[string name]
    {
        get
        {
            AnimationInfo? animation;
            if (_animations.TryGetValue(name, out animation))
            {
                return animation;
            }

            throw new KeyNotFoundException("Animation '" + name + "' is not registered.");
        }
    }

    /// <summary>
    /// Registers an animation using its validated name.
    /// </summary>
    /// <param name="animation">The animation to register.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="animation" /> is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the animation name is already registered.</exception>
    public void Register(AnimationInfo animation)
    {
        ValidateAnimation(animation, true);

        if (_animations.ContainsKey(animation.Name))
        {
            throw new InvalidOperationException("Animation '" + animation.Name + "' is already registered.");
        }

        _animations.Add(animation.Name, animation);
    }

    /// <summary>
    /// Replaces an animation that is already registered under its name.
    /// </summary>
    /// <param name="animation">The replacement animation.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="animation" /> is invalid.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the animation name is not registered.</exception>
    public void Replace(AnimationInfo animation)
    {
        ValidateAnimation(animation, true);

        if (!_animations.ContainsKey(animation.Name))
        {
            throw new KeyNotFoundException("Animation '" + animation.Name + "' is not registered.");
        }

        _animations[animation.Name] = animation;
    }

    /// <summary>
    /// Attempts to register an animation without throwing for expected failures.
    /// </summary>
    /// <param name="animation">The animation to register.</param>
    /// <returns><see langword="true" /> when the animation was registered; otherwise, <see langword="false" />.</returns>
    public bool TryRegister(AnimationInfo? animation)
    {
        if (!ValidateAnimation(animation, false) || _animations.ContainsKey(animation!.Name))
        {
            return false;
        }

        _animations.Add(animation.Name, animation);
        return true;
    }

    /// <summary>
    /// Attempts to replace an animation without throwing for expected failures.
    /// </summary>
    /// <param name="animation">The replacement animation.</param>
    /// <returns><see langword="true" /> when the animation was replaced; otherwise, <see langword="false" />.</returns>
    public bool TryReplace(AnimationInfo? animation)
    {
        if (!ValidateAnimation(animation, false) || !_animations.ContainsKey(animation!.Name))
        {
            return false;
        }

        _animations[animation.Name] = animation;
        return true;
    }

    /// <summary>
    /// Attempts to get a registered animation without throwing when it is absent.
    /// </summary>
    /// <param name="name">The name of the animation to find.</param>
    /// <param name="animation">The registered animation when found; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> when an animation was found; otherwise, <see langword="false" />.</returns>
    public bool TryGet(string name, out AnimationInfo? animation)
    {
        if (!ValidationUtils.Validate(name, value => !string.IsNullOrEmpty(value), logToModLog: false, throwError: false))
        {
            animation = null;
            return false;
        }

        return _animations.TryGetValue(name, out animation);
    }

    private static bool ValidateAnimation(AnimationInfo? animation, bool throwError)
    {
        return ValidationUtils.Validate(animation, value => value != null, logToModLog: false, throwError: throwError);
    }
}
