using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using Framework.Managers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace Blasphemous.NewbieEltonLibs.Tests;

/// <summary>Verifies the public safe mod-owned flag contract.</summary>
public sealed class FlagApiCoverageTests
{
    /// <summary>Verifies vanilla flag formatting preserves the vanilla rules.</summary>
    [Theory]
    [InlineData("mod id:flag name", "MOD_ID:FLAG_NAME")]
    [InlineData("mod-id/flag.name", "MOD-ID/FLAG.NAME")]
    [InlineData("", "")]
    public void FormatToFlagIdUsesVanillaNormalization(string value, string expected)
    {
        Assert.Equal(expected, ModFlagsManager.FormatToFlagId(value));
    }

    /// <summary>Verifies null formatter input fails at the public boundary.</summary>
    [Fact]
    public void FormatToFlagIdRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ModFlagsManager.FormatToFlagId(null!));
    }

    /// <summary>Verifies registry names are canonicalized through the public formatter.</summary>
    [Fact]
    public void RegistryUsesCanonicalFormatterForRegistration()
    {
        ModFlagRegistry registry = new();

        Assert.True(registry.TryRegister(
            typeof(OwnerMod),
            "Example Mod",
            "flag name",
            false,
            out ModFlagInfo? registration));

        Assert.NotNull(registration);
        Assert.Equal("EXAMPLE_MOD:FLAG_NAME", registration!.VanillaId);
    }

    /// <summary>Verifies vanilla reads normalize the complete ID at the adapter boundary.</summary>
    [Fact]
    public void VanillaAdapterReadsCanonicalIds()
    {
        EventManager events = (EventManager)RuntimeHelpers.GetUninitializedObject(typeof(EventManager));
        Dictionary<string, FlagObject> flags = [];
        TraverseUtils.SetValue(ref events, "flags", flags);
        FlagObject storedFalse = (FlagObject)RuntimeHelpers.GetUninitializedObject(typeof(FlagObject));
        storedFalse.value = false;
        flags["EXAMPLE_MOD:FLAG_NAME"] = storedFalse;

        Assert.True(ModFlagAdapter.TryGet(events, "Example Mod:flag name", out bool value));
        Assert.False(value);
        Assert.False(ModFlagAdapter.TryGet(events, "missing", out _));
    }

    private sealed class OwnerMod
    {
    }
}