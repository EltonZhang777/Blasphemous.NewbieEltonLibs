using System;
using System.Collections.Generic;
using Blasphemous.NewbieEltonLibs.Extensions.System;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.Storage;

/// <summary>
/// Stores sprites registered by one consuming mod.
/// </summary>
public class SpriteStorage
{
    private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
    private readonly Func<Sprite, bool> _validateSprite;

    /// <summary>
    /// Creates an empty sprite registry.
    /// </summary>
    public SpriteStorage()
        : this(IsValidSprite)
    {
    }

    internal SpriteStorage(Func<Sprite, bool> validateSprite)
    {
        _validateSprite = validateSprite;
    }

    /// <summary>
    /// Gets a required sprite by its registered name.
    /// </summary>
    /// <param name="name">The registered sprite name.</param>
    /// <returns>The sprite registered under <paramref name="name" />.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the name is not registered.</exception>
    public Sprite this[string name]
    {
        get
        {
            Sprite? sprite;
            if (_sprites.TryGetValue(name, out sprite))
            {
                return sprite;
            }

            throw new KeyNotFoundException("Sprite '" + name + "' is not registered.");
        }
    }

    /// <summary>
    /// Registers a sprite under a new name.
    /// </summary>
    /// <param name="name">The name that identifies the sprite.</param>
    /// <param name="sprite">The already-created sprite to register.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> or <paramref name="sprite" /> is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="name" /> is already registered.</exception>
    public void Register(string name, Sprite sprite)
    {
        ValidateName(name, true);
        ValidateSprite(sprite, true);

        if (_sprites.ContainsKey(name))
        {
            throw new InvalidOperationException("Sprite '" + name + "' is already registered.");
        }

        _sprites.Add(name, sprite);
    }

    /// <summary>
    /// Replaces a sprite that is already registered under a name.
    /// </summary>
    /// <param name="name">The name of the sprite to replace.</param>
    /// <param name="sprite">The already-created replacement sprite.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> or <paramref name="sprite" /> is invalid.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when <paramref name="name" /> is not registered.</exception>
    public void Replace(string name, Sprite sprite)
    {
        ValidateName(name, true);
        ValidateSprite(sprite, true);

        if (!_sprites.ContainsKey(name))
        {
            throw new KeyNotFoundException("Sprite '" + name + "' is not registered.");
        }

        _sprites[name] = sprite;
    }

    /// <summary>
    /// Attempts to register a sprite under a new name without throwing for expected failures.
    /// </summary>
    /// <param name="name">The name that identifies the sprite.</param>
    /// <param name="sprite">The already-created sprite to register.</param>
    /// <returns><see langword="true" /> when the sprite was registered; otherwise, <see langword="false" />.</returns>
    public bool TryRegister(string name, Sprite sprite)
    {
        if (!ValidateName(name, false) || !ValidateSprite(sprite, false) || _sprites.ContainsKey(name))
        {
            return false;
        }

        _sprites.Add(name, sprite);
        return true;
    }

    /// <summary>
    /// Attempts to replace a sprite that is already registered under a name without throwing for expected failures.
    /// </summary>
    /// <param name="name">The name of the sprite to replace.</param>
    /// <param name="sprite">The already-created replacement sprite.</param>
    /// <returns><see langword="true" /> when the sprite was replaced; otherwise, <see langword="false" />.</returns>
    public bool TryReplace(string name, Sprite sprite)
    {
        if (!ValidateName(name, false) || !ValidateSprite(sprite, false) || !_sprites.ContainsKey(name))
        {
            return false;
        }

        _sprites[name] = sprite;
        return true;
    }

    /// <summary>
    /// Attempts to get a registered sprite without throwing when it is absent.
    /// </summary>
    /// <param name="name">The name of the sprite to find.</param>
    /// <param name="sprite">The registered sprite when found; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> when a sprite was found; otherwise, <see langword="false" />.</returns>
    public bool TryGet(string name, out Sprite? sprite)
    {
        if (!ValidateName(name, false))
        {
            sprite = null;
            return false;
        }

        return _sprites.TryGetValue(name, out sprite);
    }

    private static bool ValidateName(string name, bool throwError)
    {
        return ValidationUtils.Validate(name, value => !string.IsNullOrEmpty(value), logToModLog: false, throwError: throwError);
    }

    private bool ValidateSprite(Sprite sprite, bool throwError)
    {
        return ValidationUtils.Validate(sprite, _validateSprite, logToModLog: false, throwError: throwError);
    }

    private static bool IsValidSprite(Sprite value)
    {
        return !ReferenceEquals(value, null) && value != null;
    }
}
