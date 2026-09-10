using Blasphemous.ModdingAPI;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using Framework.Managers;
using System;
using System.Collections.Generic;
using System.Reflection;

internal static class Program
{
    private static int Main()
    {
        try
        {
            VerifyRegistrationContract();
            VerifyPublicOwnershipContract();
            VerifyCallingAssemblyContract();
            VerifyVanillaAdapterContract();
            VerifyLifecycleContract();
            Console.WriteLine("Flag API smoke test passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void VerifyRegistrationContract()
    {
        ModOwnedFlagRegistry registry = new();
        Type ownerType = typeof(OwnerMod);
        Type otherType = typeof(OtherMod);

        Assert(ModOwnedFlagRegistry.TryCreateVanillaId("Example.Mod", "flag name", out string vanillaId));
        Assert(vanillaId == "EXAMPLE.MOD:FLAG_NAME");
        Assert(!ModOwnedFlagRegistry.TryCreateVanillaId(" ", "flag", out _));
        Assert(!ModOwnedFlagRegistry.TryCreateVanillaId("mod", " ", out _));

        Assert(registry.TryRegister(ownerType, "Example.Mod", "flag name", false, out ModOwnedFlagRegistration? registration));
        Assert(registration != null && registration.VanillaId == "EXAMPLE.MOD:FLAG_NAME");
        Assert(registration != null && !registration.PreserveInNewGamePlus);
        Assert(registry.TryRegister(ownerType, "Example.Mod", "FLAG_NAME", false, out _));
        Assert(!registry.TryRegister(ownerType, "Example.Mod", "flag name", true, out _));
        Assert(registry.TryGet(ownerType, "Example.Mod", "flag name", out _));
        Assert(!registry.TryGet(otherType, "Other.Mod", "flag name", out _));
    }

    private static void VerifyPublicOwnershipContract()
    {
        OwnerMod owner = CreateUninitializedMod<OwnerMod>("Example.Mod");
        OtherMod other = CreateUninitializedMod<OtherMod>("Other.Mod");
        SetLoadedMods(owner, other);

        Assert(ModOwnedFlags.RegisterFlag("public", typeof(OwnerMod), true));
        Assert(ModOwnedFlags.RegisterFlag("public", typeof(OwnerMod), true));
        Assert(!ModOwnedFlags.RegisterFlag("public", typeof(OwnerMod), false));
        Assert(!ModOwnedFlags.TrySetFlag("public", typeof(OtherMod), false));
        Assert(!ModOwnedFlags.TryGetFlag("public", typeof(OtherMod), out _));
        Assert(!ModOwnedFlags.TrySetFlag("EXAMPLE.MOD:PUBLIC", typeof(OwnerMod), false));
    }

    private static void VerifyCallingAssemblyContract()
    {
        OwnerMod owner = CreateUninitializedMod<OwnerMod>("Fallback.Mod");
        OtherMod other = CreateUninitializedMod<OtherMod>("Other.Mod");

        SetLoadedMods(owner, other);
        Assert(ModOwnedFlags.RegisterFlag("explicit-precedence", typeof(OwnerMod)));

        SetLoadedMods(owner);
        Assert(ModOwnedFlags.RegisterFlag("fallback-single"));
        Assert(ModOwnedFlags.RegisterFlag("fallback-single", typeof(OwnerMod)));

        SetLoadedMods(owner, other);
        Assert(!ModOwnedFlags.RegisterFlag("fallback-ambiguous"));

        Type externalModType = typeof(BlasMod).Assembly.GetType("Blasphemous.ModdingAPI.ModdingAPI")!;
        BlasMod externalMod = (BlasMod)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(externalModType);
        SetLoadedMods(externalMod);
        Assert(!ModOwnedFlags.RegisterFlag("fallback-no-match"));
    }

    private static void VerifyVanillaAdapterContract()
    {
        EventManager events = CreateEvents();
        Dictionary<string, FlagObject> flags = TraverseUtils.GetValue<Dictionary<string, FlagObject>>(events, "flags")!;

        Assert(!ModOwnedFlagAdapter.TryGet(events, "EXAMPLE.MOD:MISSING", out _));
        FlagObject storedFalse = CreateFlag(false, false);
        flags["EXAMPLE.MOD:STORED_FALSE"] = storedFalse;
        Assert(ModOwnedFlagAdapter.TryGet(events, "EXAMPLE.MOD:STORED_FALSE", out bool storedValue));
        Assert(!storedValue);

        FlagObject preserved = CreateFlag(true, true);
        flags["EXAMPLE.MOD:PRESERVED"] = preserved;
        Assert(ModOwnedFlagAdapter.TryGet(events, "EXAMPLE.MOD:PRESERVED", out bool preservedValue));
        Assert(preservedValue && flags["EXAMPLE.MOD:PRESERVED"].preserveInNewGamePlus);
    }

    private static void VerifyLifecycleContract()
    {
        ModOwnedFlagRegistry registry = new();
        Assert(registry.TryRegister(typeof(OwnerMod), "Lifecycle.Mod", "slot flag", true, out ModOwnedFlagRegistration? registration));
        Assert(registration != null && registration.PreserveInNewGamePlus);

        EventManager events = CreateEvents();
        Dictionary<string, FlagObject> flags = TraverseUtils.GetValue<Dictionary<string, FlagObject>>(events, "flags")!;
        Assert(!ModOwnedFlagAdapter.TryGet(events, registration!.VanillaId, out _));

        flags[registration.VanillaId] = CreateFlag(false, true);
        Assert(ModOwnedFlagAdapter.TryGet(events, registration.VanillaId, out bool storedFalse));
        Assert(!storedFalse);

        Dictionary<string, FlagObject> restoredFlags = new();
        restoredFlags[registration.VanillaId] = CreateFlag(true, true);
        TraverseUtils.SetValue(ref events, "flags", restoredFlags);
        Assert(registry.TryGet(typeof(OwnerMod), "Lifecycle.Mod", "slot flag", out _));
        Assert(ModOwnedFlagAdapter.TryGet(events, registration.VanillaId, out bool restoredValue));
        Assert(restoredValue);

        restoredFlags[registration.VanillaId].value = false;
        Assert(ModOwnedFlagAdapter.TryGet(events, registration.VanillaId, out bool resetValue));
        Assert(!resetValue);
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
        setter.Invoke(null, new object[] { mods });
    }

    private static void Assert(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("Flag API smoke test assertion failed.");
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
