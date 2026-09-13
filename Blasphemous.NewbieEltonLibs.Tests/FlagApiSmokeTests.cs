using Blasphemous.ModdingAPI;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using Framework.Managers;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Blasphemous.NewbieEltonLibs.Tests;

/// <summary>Verifies the Flag API smoke scenarios through xUnit discovery.</summary>
public sealed class FlagApiSmokeTests
{
    /// <summary>Verifies the public formatter contract and null handling.</summary>
    [Fact]
    public void VerifyPublicFormatterContract()
    {
        Assert.Equal("EXAMPLE.MOD:FLAG_NAME", ModFlagsManager.FormatToFlagId("Example.Mod:flag name"));
        Assert.Equal("MOD-ID:FLAG-NAME/OTHER", ModFlagsManager.FormatToFlagId("mod-id:flag-name/other"));
        Assert.Equal(string.Empty, ModFlagsManager.FormatToFlagId(string.Empty));
        Assert.Throws<ArgumentNullException>(() => ModFlagsManager.FormatToFlagId(null!));
    }

    /// <summary>Verifies registry validation, ownership, and canonical registration IDs.</summary>
    [Fact]
    public void VerifyRegistrationContract()
    {
        ModFlagRegistry registry = new();
        Type ownerType = typeof(OwnerMod);
        Type otherType = typeof(OtherMod);

        Assert.True(ModFlagRegistry.TryCreateVanillaId("Example.Mod", "flag name", out string vanillaId));
        Assert.Equal("EXAMPLE.MOD:FLAG_NAME", vanillaId);
        Assert.True(ModFlagRegistry.TryCreateVanillaId("Example.Mod", "flag-name/other", out vanillaId));
        Assert.Equal("EXAMPLE.MOD:FLAG-NAME/OTHER", vanillaId);
        Assert.False(ModFlagRegistry.TryCreateVanillaId(null, "flag", out _));
        Assert.False(ModFlagRegistry.TryCreateVanillaId("mod", null, out _));
        Assert.False(ModFlagRegistry.TryCreateVanillaId(string.Empty, "flag", out _));
        Assert.False(ModFlagRegistry.TryCreateVanillaId("mod", string.Empty, out _));
        Assert.False(ModFlagRegistry.TryCreateVanillaId(" ", "flag", out _));
        Assert.False(ModFlagRegistry.TryCreateVanillaId("mod", " ", out _));

        Assert.True(registry.TryRegister(ownerType, "Example.Mod", "flag name", false, out ModFlagInfo? registration));
        Assert.NotNull(registration);
        Assert.Equal("EXAMPLE.MOD:FLAG_NAME", registration!.VanillaId);
        Assert.Equal("Example.Mod", registration.ModId);
        Assert.False(registration.PreserveInNewGamePlus);
        Assert.True(registry.TryRegister(ownerType, "example.mod", "FLAG_NAME", false, out _));
        Assert.False(registry.TryRegister(ownerType, "example.mod", "flag name", true, out _));
        Assert.False(registry.TryRegister(otherType, "example.mod", "FLAG_NAME", false, out _));
        Assert.True(registry.TryGet(ownerType, "example.mod", "flag name", out _));
        Assert.False(registry.TryGet(otherType, "Other.Mod", "flag name", out _));
    }

    /// <summary>Verifies public flag registration and ownership enforcement.</summary>
    [Fact]
    public void VerifyPublicOwnershipContract()
    {
        OwnerMod owner = CreateUninitializedMod<OwnerMod>("Example.Mod");
        OtherMod other = CreateUninitializedMod<OtherMod>("Other.Mod");
        SetLoadedMods(owner, other);

        Assert.True(ModFlagsManager.RegisterFlag("public", typeof(OwnerMod), true));
        Assert.True(ModFlagsManager.RegisterFlag("public", typeof(OwnerMod), true));
        Assert.False(ModFlagsManager.RegisterFlag("public", typeof(OwnerMod), false));
        Assert.False(ModFlagsManager.TrySetFlag("public", typeof(OtherMod), false));
        Assert.False(ModFlagsManager.TryGetFlag("public", typeof(OtherMod), out _));
        Assert.False(ModFlagsManager.TrySetFlag("EXAMPLE.MOD:PUBLIC", typeof(OwnerMod), false));
    }

    /// <summary>Verifies calling-assembly fallback and ambiguity handling.</summary>
    [Fact]
    public void VerifyCallingAssemblyContract()
    {
        OwnerMod owner = CreateUninitializedMod<OwnerMod>("Fallback.Mod");
        OtherMod other = CreateUninitializedMod<OtherMod>("Other.Mod");

        SetLoadedMods(owner, other);
        Assert.True(ModFlagsManager.RegisterFlag("explicit-precedence", typeof(OwnerMod)));

        SetLoadedMods(owner);
        Assert.True(ModFlagsManager.RegisterFlag("fallback-single"));
        Assert.True(ModFlagsManager.RegisterFlag("fallback-single", typeof(OwnerMod)));

        SetLoadedMods(owner, other);
        Assert.False(ModFlagsManager.RegisterFlag("fallback-ambiguous"));

        Type externalModType = typeof(BlasMod).Assembly.GetType("Blasphemous.ModdingAPI.ModdingAPI")!;
        BlasMod externalMod = (BlasMod)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(externalModType);
        SetLoadedMods(externalMod);
        Assert.False(ModFlagsManager.RegisterFlag("fallback-no-match"));
    }

    /// <summary>Verifies vanilla adapter reads preserve stored false and NG+ state.</summary>
    [Fact]
    public void VerifyVanillaAdapterContract()
    {
        EventManager events = CreateEvents();
        Dictionary<string, FlagObject> flags = TraverseUtils.GetValue<Dictionary<string, FlagObject>>(events, "flags")!;

        Assert.False(ModFlagAdapter.TryGet(events, "example.mod:missing", out _));
        FlagObject storedFalse = CreateFlag(false, false);
        flags["EXAMPLE.MOD:STORED_FALSE"] = storedFalse;
        Assert.True(ModFlagAdapter.TryGet(events, "Example.Mod:stored false", out bool storedValue));
        Assert.False(storedValue);

        FlagObject preserved = CreateFlag(true, true);
        flags["EXAMPLE.MOD:PRESERVED"] = preserved;
        Assert.True(ModFlagAdapter.TryGet(events, "Example.Mod:preserved", out bool preservedValue));
        Assert.True(preservedValue);
        Assert.True(flags["EXAMPLE.MOD:PRESERVED"].preserveInNewGamePlus);
    }

    /// <summary>Verifies registered flags survive vanilla state replacement and reset.</summary>
    [Fact]
    public void VerifyLifecycleContract()
    {
        ModFlagRegistry registry = new();
        Assert.True(registry.TryRegister(typeof(OwnerMod), "Lifecycle.Mod", "slot flag", true, out ModFlagInfo? registration));
        Assert.NotNull(registration);
        Assert.True(registration!.PreserveInNewGamePlus);

        EventManager events = CreateEvents();
        Dictionary<string, FlagObject> flags = TraverseUtils.GetValue<Dictionary<string, FlagObject>>(events, "flags")!;
        Assert.False(ModFlagAdapter.TryGet(events, registration.VanillaId, out _));

        flags[registration.VanillaId] = CreateFlag(false, true);
        Assert.True(ModFlagAdapter.TryGet(events, registration.VanillaId, out bool storedFalse));
        Assert.False(storedFalse);

        Dictionary<string, FlagObject> restoredFlags = new()
        {
            [registration.VanillaId] = CreateFlag(true, true)
        };
        TraverseUtils.SetValue(ref events, "flags", restoredFlags);
        Assert.True(registry.TryGet(typeof(OwnerMod), "Lifecycle.Mod", "slot flag", out _));
        Assert.True(ModFlagAdapter.TryGet(events, registration.VanillaId, out bool restoredValue));
        Assert.True(restoredValue);

        restoredFlags[registration.VanillaId].value = false;
        Assert.True(ModFlagAdapter.TryGet(events, registration.VanillaId, out bool resetValue));
        Assert.False(resetValue);
    }

    private static EventManager CreateEvents()
    {
        EventManager events = (EventManager)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(EventManager));
        TraverseUtils.SetValue(ref events, "flags", new Dictionary<string, FlagObject>());
        return events;
    }

    private static FlagObject CreateFlag(bool value, bool preserveInNewGamePlus)
    {
        FlagObject flag = (FlagObject)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(FlagObject));
        flag.value = value;
        flag.preserveInNewGamePlus = preserveInNewGamePlus;
        return flag;
    }

    private static TMod CreateUninitializedMod<TMod>(string id) where TMod : BlasMod
    {
        TMod mod = (TMod)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(TMod));
        TraverseUtils.SetValue(ref mod, "<Id>k__BackingField", id);
        return mod;
    }

    private static void SetLoadedMods(params BlasMod[] mods)
    {
        PropertyInfo property = typeof(Blasphemous.ModdingAPI.Helpers.ModHelper).GetProperty("LoadedMods", BindingFlags.Static | BindingFlags.Public)!;
        MethodInfo setter = property.GetSetMethod(true)!;
        setter.Invoke(null, [mods]);
    }

    private sealed class OwnerMod : BlasMod
    {
        private OwnerMod() : base("unused", "unused", "unused", "unused") { }
    }

    private sealed class OtherMod : BlasMod
    {
        private OtherMod() : base("unused", "unused", "unused", "unused") { }
    }
}
