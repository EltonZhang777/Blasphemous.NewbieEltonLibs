using Blasphemous.NewbieEltonLibs.Extensions.System;
using FMOD;
using System;
using System.Collections.Generic;

namespace Blasphemous.NewbieEltonLibs.Storage;

/// <summary>Stores named <see cref="Sound" /> values for one consuming mod without owning their lifetime.</summary>
public class FMODSoundStorage
{
    private readonly Dictionary<string, Sound> _sounds = [];

    /// <summary>Creates an empty FMOD Sound storage.</summary>
    public FMODSoundStorage()
    {
    }

    /// <summary>Gets a required FMOD Sound registered under a name.</summary>
    /// <param name="name">The registered FMOD Sound name.</param>
    /// <returns>The FMOD Sound registered under <paramref name="name" />.</returns>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">Thrown when the name is not registered.</exception>
    public Sound this[string name]
    {
        get
        {
            if (_sounds.TryGetValue(name, out Sound sound))
            {
                return sound;
            }

            throw new KeyNotFoundException("FMOD Sound '" + name + "' is not registered.");
        }
    }

    /// <summary>Registers an existing FMOD Sound under a new name.</summary>
    /// <param name="name">The name that identifies the FMOD Sound.</param>
    /// <param name="sound">The FMOD Sound to register.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="name" /> or <paramref name="sound" /> is invalid.</exception>
    /// <exception cref="System.InvalidOperationException">Thrown when <paramref name="name" /> is already registered.</exception>
    public void Register(string name, Sound sound)
    {
        ValidateName(name, true);
        ValidateSound(sound, true);

        if (_sounds.ContainsKey(name))
        {
            throw new System.InvalidOperationException("FMOD Sound '" + name + "' is already registered.");
        }

        _sounds.Add(name, sound);
    }

    /// <summary>Replaces an FMOD Sound that is already registered under a name.</summary>
    /// <param name="name">The name of the FMOD Sound to replace.</param>
    /// <param name="sound">The replacement FMOD Sound.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="name" /> or <paramref name="sound" /> is invalid.</exception>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">Thrown when <paramref name="name" /> is not registered.</exception>
    public void Replace(string name, Sound sound)
    {
        ValidateName(name, true);
        ValidateSound(sound, true);

        if (!_sounds.ContainsKey(name))
        {
            throw new KeyNotFoundException("FMOD Sound '" + name + "' is not registered.");
        }

        _sounds[name] = sound;
    }

    /// <summary>Attempts to register an FMOD Sound without throwing for expected failures.</summary>
    /// <param name="name">The name that identifies the FMOD Sound.</param>
    /// <param name="sound">The FMOD Sound to register.</param>
    /// <returns><see langword="true" /> when registered; otherwise, <see langword="false" /> when the name or sound is invalid, or the name is already registered.</returns>
    public bool TryRegister(string name, Sound sound)
    {
        if (!ValidateName(name, false) || !ValidateSound(sound, false) || _sounds.ContainsKey(name))
        {
            return false;
        }

        _sounds.Add(name, sound);
        return true;
    }

    /// <summary>Attempts to replace a registered FMOD Sound without throwing for expected failures.</summary>
    /// <param name="name">The name of the FMOD Sound to replace.</param>
    /// <param name="sound">The replacement FMOD Sound.</param>
    /// <returns><see langword="true" /> when replaced; otherwise, <see langword="false" /> when the name or sound is invalid, or the name is not registered.</returns>
    public bool TryReplace(string name, Sound sound)
    {
        if (!ValidateName(name, false) || !ValidateSound(sound, false) || !_sounds.ContainsKey(name))
        {
            return false;
        }

        _sounds[name] = sound;
        return true;
    }

    /// <summary>Attempts to get a registered FMOD Sound without throwing when it is absent.</summary>
    /// <param name="name">The name of the FMOD Sound to find.</param>
    /// <param name="sound">The registered FMOD Sound when found; otherwise, the default value.</param>
    /// <returns><see langword="true" /> when found; otherwise, <see langword="false" />.</returns>
    public bool TryGet(string name, out Sound sound)
    {
        if (!ValidateName(name, false))
        {
            sound = default;
            return false;
        }

        return _sounds.TryGetValue(name, out sound);
    }

    private static bool ValidateName(string name, bool throwError)
    {
        return ValidationUtils.Validate(name, value => !string.IsNullOrEmpty(value), logToModLog: false, throwError: throwError);
    }

    private static bool ValidateSound(Sound sound, bool throwError)
    {
        return ValidationUtils.Validate(sound, value => value.handle != IntPtr.Zero, logToModLog: false, throwError: throwError);
    }
}
