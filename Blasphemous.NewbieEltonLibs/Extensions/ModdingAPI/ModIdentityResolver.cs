using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;

/// <summary>
/// Resolves loaded mods by concrete runtime type, calling assembly, or explicit instance.
/// </summary>
public static class ModIdentityResolver
{
    /// <summary>
    /// Attempts to resolve exactly one loaded mod by concrete runtime type or calling assembly.
    /// </summary>
    /// <param name="modType">The exact concrete mod type, or <see langword="null"/> to resolve by assembly.</param>
    /// <param name="callingAssembly">The assembly used to resolve the mod when <paramref name="modType"/> is <see langword="null"/>.</param>
    /// <param name="mod">The resolved mod, or <see langword="null"/> if resolution fails.</param>
    /// <returns><see langword="true"/> when exactly one loaded mod matches.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="callingAssembly"/> is <see langword="null"/>.</exception>
    public static bool TryResolveMod(Type? modType, Assembly callingAssembly, out BlasMod? mod)
    {
        mod = null;
        if (callingAssembly == null)
            throw new ArgumentNullException(nameof(callingAssembly));

        return TryResolveMod(modType, callingAssembly, explicitOwner: null, out mod);
    }

    /// <summary>
    /// Attempts to resolve the exact loaded mod instance supplied by the caller.
    /// </summary>
    /// <param name="explicitOwner">The loaded mod instance to resolve.</param>
    /// <param name="mod">The resolved instance, or <see langword="null"/> if it is unavailable.</param>
    /// <returns><see langword="true"/> when the exact instance is present in the loaded mod list.</returns>
    public static bool TryResolveMod(BlasMod explicitOwner, out BlasMod? mod)
    {
        if (explicitOwner == null)
        {
            mod = null;
            LogResolutionError("Cannot resolve the explicit mod identity because the mod is null.", explicitOwner);
            return false;
        }

        return TryResolveMod(explicitOwner.GetType(), typeof(ModIdentityResolver).Assembly, explicitOwner, out mod);
    }

    private static bool TryResolveMod(Type? modType, Assembly callingAssembly, BlasMod? explicitOwner, out BlasMod? mod)
    {
        mod = null;
        if (modType != null && (!typeof(BlasMod).IsAssignableFrom(modType) || modType.IsAbstract))
        {
            LogResolutionError($"Cannot resolve mod identity from invalid mod type '{modType}'.", explicitOwner);
            return false;
        }

        IEnumerable<BlasMod>? loadedMods;
        try
        {
            loadedMods = ModHelper.LoadedMods;
        }
        catch (Exception exception)
        {
            LogResolutionError($"Failed to resolve mod identity: {exception.Message}", explicitOwner);
            return false;
        }

        if (loadedMods == null)
        {
            LogResolutionError("Cannot resolve mod identity because no loaded mod list is available.", explicitOwner);
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
                bool isMatch = explicitOwner != null
                    ? ReferenceEquals(candidate, explicitOwner)
                    : modType != null
                        ? candidateType == modType
                        : candidateType.Assembly == callingAssembly;
                if (!isMatch)
                    continue;

                match = candidate;
                matchCount++;
            }
        }
        catch (Exception exception)
        {
            LogResolutionError($"Failed to resolve mod identity for '{modType}': {exception.Message}", explicitOwner);
            return false;
        }

        if (matchCount != 1 || match == null)
        {
            string identity = explicitOwner != null
                ? $"explicit mod instance '{explicitOwner.GetType()}'"
                : modType == null ? $"calling assembly '{callingAssembly.FullName}'" : $"mod type '{modType}'";
            LogResolutionError($"Cannot resolve mod identity for {identity}: expected one loaded mod, found {matchCount}.", explicitOwner);
            return false;
        }

        mod = match;
        return true;
    }

    private static void LogResolutionError(string message, BlasMod? explicitOwner)
    {
        if (explicitOwner == null)
            ModLog.Error(message);
        else
            ModLog.Error(message, explicitOwner);
    }
}