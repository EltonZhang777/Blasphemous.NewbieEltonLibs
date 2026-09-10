using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Helpers;
using Framework.Managers;
using System;
using System.Collections.Generic;

namespace Blasphemous.NewbieEltonLibs.Extensions.GameLibs;

/// <summary>
/// Provides ownership-scoped access to boolean flags stored by the vanilla event system.
/// </summary>
public static class ModOwnedFlags
{
    private static readonly ModOwnedFlagRegistry _registry = new();

    /// <summary>
    /// Registers a local flag name for the uniquely loaded mod represented by <paramref name="modType"/>.
    /// </summary>
    /// <param name="localName">The mod-local flag name.</param>
    /// <param name="modType">The exact concrete <see cref="BlasMod"/> type that owns the flag.</param>
    /// <param name="preserveInNewGamePlus">Whether the vanilla flag should be preserved in New Game Plus.</param>
    /// <returns><see langword="true"/> when the registration is new or identical to an existing registration.</returns>
    public static bool RegisterFlag(string localName, Type? modType, bool preserveInNewGamePlus = false)
    {
        if (!TryResolveMod(modType, out BlasMod? mod) || mod == null)
            return false;

        if (_registry.TryRegister(mod.GetType(), mod.Id, localName, preserveInNewGamePlus, out _))
            return true;

        ModLog.Error($"Failed to register mod-owned flag '{localName}' because the name is already registered with different ownership or configuration.", mod);
        return false;
    }

    /// <summary>
    /// Attempts to read a registered mod-owned flag from the current vanilla flag state.
    /// </summary>
    /// <param name="localName">The mod-local flag name.</param>
    /// <param name="modType">The exact concrete <see cref="BlasMod"/> type that owns the flag.</param>
    /// <param name="value">Receives the stored flag value when the operation succeeds.</param>
    /// <returns><see langword="true"/> when the flag is registered and exists in the current vanilla state.</returns>
    public static bool TryGetFlag(string localName, Type? modType, out bool value)
    {
        value = false;
        if (!TryResolveMod(modType, out BlasMod? mod) || mod == null)
            return false;

        if (!_registry.TryGet(mod.GetType(), mod.Id, localName, out ModOwnedFlagRegistration? registration) || registration == null)
        {
            ModLog.Error($"Cannot read unregistered mod-owned flag '{localName}'.", mod);
            return false;
        }

        if (!TryGetEvents(mod, "read", out EventManager? events) || events == null)
        {
            return false;
        }

        try
        {
            return ModOwnedFlagAdapter.TryGet(events, registration.VanillaId, out value);
        }
        catch (Exception exception)
        {
            ModLog.Error($"Failed to read mod-owned flag '{localName}': {exception.Message}", mod);
            return false;
        }
    }

    /// <summary>
    /// Attempts to write a registered mod-owned flag through the vanilla event system.
    /// </summary>
    /// <param name="localName">The mod-local flag name.</param>
    /// <param name="modType">The exact concrete <see cref="BlasMod"/> type that owns the flag.</param>
    /// <param name="value">The value to store.</param>
    /// <returns><see langword="true"/> when the flag is registered and the vanilla write is invoked.</returns>
    public static bool TrySetFlag(string localName, Type? modType, bool value)
    {
        if (!TryResolveMod(modType, out BlasMod? mod) || mod == null)
            return false;

        if (!_registry.TryGet(mod.GetType(), mod.Id, localName, out ModOwnedFlagRegistration? registration) || registration == null)
        {
            ModLog.Error($"Cannot write unregistered mod-owned flag '{localName}'.", mod);
            return false;
        }

        if (!TryGetEvents(mod, "write", out EventManager? events) || events == null)
        {
            return false;
        }

        try
        {
            return ModOwnedFlagAdapter.TrySet(events, registration.VanillaId, value, registration.PreserveInNewGamePlus);
        }
        catch (Exception exception)
        {
            ModLog.Error($"Failed to write mod-owned flag '{localName}': {exception.Message}", mod);
            return false;
        }
    }

    private static bool TryGetEvents(BlasMod mod, string operation, out EventManager? events)
    {
        events = null;
        try
        {
            events = Core.Events;
        }
        catch (Exception exception)
        {
            ModLog.Error($"Cannot {operation} mod-owned flag because the vanilla event manager could not be resolved: {exception.Message}", mod);
            return false;
        }

        if (events != null)
            return true;

        ModLog.Error($"Cannot {operation} mod-owned flag because the vanilla event manager is unavailable.", mod);
        return false;
    }

    private static bool TryResolveMod(Type? modType, out BlasMod? mod)
    {
        mod = null;
        if (modType == null || !typeof(BlasMod).IsAssignableFrom(modType) || modType.IsAbstract)
        {
            ModLog.Error($"Cannot resolve mod-owned flag identity from invalid mod type '{modType}'.");
            return false;
        }

        IEnumerable<BlasMod>? loadedMods = ModHelper.LoadedMods;
        if (loadedMods == null)
        {
            ModLog.Error($"Cannot resolve mod-owned flag identity for '{modType}' because no loaded mod list is available.");
            return false;
        }

        BlasMod? match = null;
        int matchCount = 0;
        try
        {
            foreach (BlasMod candidate in loadedMods)
            {
                if (candidate == null || candidate.GetType() != modType)
                    continue;

                match = candidate;
                matchCount++;
            }
        }
        catch (Exception exception)
        {
            ModLog.Error($"Failed to resolve mod-owned flag identity for '{modType}': {exception.Message}");
            return false;
        }

        if (matchCount != 1 || match == null)
        {
            ModLog.Error($"Cannot resolve mod-owned flag identity for '{modType}': expected one loaded mod, found {matchCount}.");
            return false;
        }

        mod = match;
        return true;
    }
}
