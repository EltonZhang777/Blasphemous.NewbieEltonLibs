using System;
using System.Collections.Generic;

namespace Blasphemous.NewbieEltonLibs.Extensions.GameLibs;

internal sealed class ModFlagRegistry
{
    private readonly Dictionary<string, ModFlagInfo> _registrations = [];

    internal bool TryRegister(Type modType, string modId, string localName, bool preserveInNewGamePlus, out ModFlagInfo? registration)
    {
        registration = null;
        if (!TryCreateVanillaId(modId, localName, out string vanillaId))
            return false;

        if (_registrations.TryGetValue(vanillaId, out ModFlagInfo existing))
        {
            if (IsSameOwner(existing, modType, modId) &&
                existing.PreserveInNewGamePlus == preserveInNewGamePlus)
            {
                registration = existing;
                return true;
            }

            return false;
        }

        registration = new ModFlagInfo(modType, modId, vanillaId, preserveInNewGamePlus);
        _registrations.Add(vanillaId, registration);
        return true;
    }

    internal bool TryGet(Type modType, string modId, string localName, out ModFlagInfo? registration)
    {
        registration = null;
        if (!TryCreateVanillaId(modId, localName, out string vanillaId))
            return false;

        if (!_registrations.TryGetValue(vanillaId, out ModFlagInfo candidate))
            return false;

        if (!IsSameOwner(candidate, modType, modId))
            return false;

        registration = candidate;
        return true;
    }

    private static bool IsSameOwner(ModFlagInfo registration, Type modType, string modId)
    {
        return registration.ModType == modType &&
            ModFlagsManager.FormatToFlagId(registration.ModId) == ModFlagsManager.FormatToFlagId(modId);
    }

    internal static bool TryCreateVanillaId(string? modId, string? localName, out string vanillaId)
    {
        vanillaId = null!;
        if (IsNullOrWhiteSpace(modId) || IsNullOrWhiteSpace(localName))
            return false;

        vanillaId = ModFlagsManager.FormatToFlagId(modId + ":" + localName);
        return true;
    }

    private static bool IsNullOrWhiteSpace(string? value)
    {
        if (value == null)
            return true;

        return value.Length == 0 || value.Trim().Length == 0;
    }
}

internal sealed class ModFlagInfo
{
    internal Type ModType { get; }
    internal string ModId { get; }
    internal string VanillaId { get; }
    internal bool PreserveInNewGamePlus { get; }

    internal ModFlagInfo(Type modType, string modId, string vanillaId, bool preserveInNewGamePlus)
    {
        ModType = modType;
        ModId = modId;
        VanillaId = vanillaId;
        PreserveInNewGamePlus = preserveInNewGamePlus;
    }
}