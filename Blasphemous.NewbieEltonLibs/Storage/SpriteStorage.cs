using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.Storage;

/// <summary>
/// Stores sprites registered by one consuming mod.
/// </summary>
public class SpriteStorage
{
    private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

    /// <summary>
    /// Creates an empty sprite registry.
    /// </summary>
    public SpriteStorage()
    {
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
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="name" /> is already registered.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name" /> or <paramref name="sprite" /> is null.</exception>
    public void Register(string name, Sprite sprite)
    {
        ValidateName(name);
        ValidateSprite(sprite);

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
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when <paramref name="name" /> is not registered.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name" /> or <paramref name="sprite" /> is null.</exception>
    public void Replace(string name, Sprite sprite)
    {
        ValidateName(name);
        ValidateSprite(sprite);

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
        if (!IsValidName(name) || sprite == null || _sprites.ContainsKey(name))
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
        if (!IsValidName(name) || sprite == null || !_sprites.ContainsKey(name))
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
        if (!IsValidName(name))
        {
            sprite = null;
            return false;
        }

        return _sprites.TryGetValue(name, out sprite);
    }

    private static bool IsValidName(string name)
    {
        return !string.IsNullOrEmpty(name);
    }

    private static void ValidateName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("A sprite name cannot be empty.", nameof(name));
        }
    }

    private static void ValidateSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            throw new ArgumentNullException(nameof(sprite));
        }
    }
}
