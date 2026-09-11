using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Helpers;
using Framework.Managers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Blasphemous.NewbieEltonLibs.Extensions.GameLibs;

/// <summary>
/// Provides ownership-scoped access to boolean flags stored by the vanilla event system.
/// </summary>
public static class ModFlagsManager
{
    private static readonly ModFlagRegistry _registry = new();

    /// <summary>
    /// Registers a local flag name for the uniquely loaded mod represented by <paramref name="modType"/>.
    /// </summary>
    /// <param name="localName">The mod-local flag name.</param>
    /// <param name="modType">The exact concrete <see cref="BlasMod"/> type that owns the flag.</param>
    /// <param name="preserveInNewGamePlus">Whether the vanilla flag should be preserved in New Game Plus.</param>
    /// <returns><see langword="true"/> when the registration is new or identical to an existing registration.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool RegisterFlag(string localName, Type? modType, bool preserveInNewGamePlus = false)
    {
        return RegisterFlag(localName, modType, Assembly.GetCallingAssembly(), preserveInNewGamePlus);
    }

    /// <summary>
    /// Registers a local flag name for the uniquely loaded mod in the calling assembly.
    /// </summary>
    /// <param name="localName">The mod-local flag name.</param>
    /// <param name="preserveInNewGamePlus">Whether the vanilla flag should be preserved in New Game Plus.</param>
    /// <returns><see langword="true"/> when the registration is new or identical to an existing registration.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool RegisterFlag(string localName, bool preserveInNewGamePlus = false)
    {
        return RegisterFlag(localName, null, Assembly.GetCallingAssembly(), preserveInNewGamePlus);
    }

    private static bool RegisterFlag(string localName, Type? modType, Assembly callingAssembly, bool preserveInNewGamePlus)
    {
        if (!TryResolveMod(modType, callingAssembly, out BlasMod? mod) || mod == null)
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
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool TryGetFlag(string localName, Type? modType, out bool value)
    {
        return TryGetFlag(localName, modType, Assembly.GetCallingAssembly(), out value);
    }

    /// <summary>
    /// Attempts to read a registered mod-owned flag for the uniquely loaded mod in the calling assembly.
    /// </summary>
    /// <param name="localName">The mod-local flag name.</param>
    /// <param name="value">Receives the stored flag value when the operation succeeds.</param>
    /// <returns><see langword="true"/> when the flag is registered and exists in the current vanilla state.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool TryGetFlag(string localName, out bool value)
    {
        return TryGetFlag(localName, null, Assembly.GetCallingAssembly(), out value);
    }

    private static bool TryGetFlag(string localName, Type? modType, Assembly callingAssembly, out bool value)
    {
        value = false;
        if (!TryResolveMod(modType, callingAssembly, out BlasMod? mod) || mod == null)
            return false;

        if (!_registry.TryGet(mod.GetType(), mod.Id, localName, out ModFlagInfo? registration) || registration == null)
        {
            ModLog.Error($"Cannot read unregistered mod-owned flag '{localName}'.", mod);
            return false;
        }

        if (!TryGetVanillaEventManager(mod, "read", out EventManager? events) || events == null)
        {
            return false;
        }

        try
        {
            return ModFlagAdapter.TryGet(events, registration.VanillaId, out value);
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
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool TrySetFlag(string localName, Type? modType, bool value)
    {
        return TrySetFlag(localName, modType, Assembly.GetCallingAssembly(), value);
    }

    /// <summary>
    /// Attempts to write a registered mod-owned flag for the uniquely loaded mod in the calling assembly.
    /// </summary>
    /// <param name="localName">The mod-local flag name.</param>
    /// <param name="value">The value to store.</param>
    /// <returns><see langword="true"/> when the flag is registered and the vanilla write is invoked.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool TrySetFlag(string localName, bool value)
    {
        return TrySetFlag(localName, null, Assembly.GetCallingAssembly(), value);
    }

    private static bool TrySetFlag(string localName, Type? modType, Assembly callingAssembly, bool value)
    {
        if (!TryResolveMod(modType, callingAssembly, out BlasMod? mod) || mod == null)
            return false;

        if (!_registry.TryGet(mod.GetType(), mod.Id, localName, out ModFlagInfo? registration) || registration == null)
        {
            ModLog.Error($"Cannot write unregistered mod-owned flag '{localName}'.", mod);
            return false;
        }

        if (!TryGetVanillaEventManager(mod, "write", out EventManager? events) || events == null)
        {
            return false;
        }

        try
        {
            return ModFlagAdapter.TrySet(events, registration.VanillaId, value, registration.PreserveInNewGamePlus);
        }
        catch (Exception exception)
        {
            ModLog.Error($"Failed to write mod-owned flag '{localName}': {exception.Message}", mod);
            return false;
        }
    }

    private static bool TryGetVanillaEventManager(BlasMod mod, string operation, out EventManager? events)
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

    private static bool TryResolveMod(Type? modType, Assembly callingAssembly, out BlasMod? mod)
    {
        mod = null;
        if (modType != null && (!typeof(BlasMod).IsAssignableFrom(modType) || modType.IsAbstract))
        {
            ModLog.Error($"Cannot resolve mod-owned flag identity from invalid mod type '{modType}'.");
            return false;
        }

        IEnumerable<BlasMod>? loadedMods;
        try
        {
            loadedMods = ModHelper.LoadedMods;
        }
        catch (Exception exception)
        {
            ModLog.Error($"Failed to resolve mod-owned flag identity: {exception.Message}");
            return false;
        }

        if (loadedMods == null)
        {
            ModLog.Error("Cannot resolve mod-owned flag identity because no loaded mod list is available.");
            return false;
        }

        BlasMod? match = null;
        int matchCount = 0;
        try
        {
            foreach (BlasMod candidate in loadedMods)
            {
                if (candidate == null)
                    continue;

                Type candidateType = candidate.GetType();
                if (modType != null ? candidateType != modType : candidateType.Assembly != callingAssembly)
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
            string identity = modType == null ? $"calling assembly '{callingAssembly.FullName}'" : $"mod type '{modType}'";
            ModLog.Error($"Cannot resolve mod-owned flag identity for {identity}: expected one loaded mod, found {matchCount}.");
            return false;
        }

        mod = match;
        return true;
    }
}
