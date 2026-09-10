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
            VerifyVanillaAdapterContract();
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

    private static void VerifyVanillaAdapterContract()
    {
        EventManager events = (EventManager)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(EventManager));
        TraverseUtils.SetValue(ref events, "flags", new Dictionary<string, FlagObject>());
        Dictionary<string, FlagObject> flags = TraverseUtils.GetValue<Dictionary<string, FlagObject>>(events, "flags")!;

        Assert(!ModOwnedFlagAdapter.TryGet(events, "EXAMPLE.MOD:MISSING", out _));
        FlagObject storedFalse = (FlagObject)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(FlagObject));
        storedFalse.value = false;
        flags["EXAMPLE.MOD:STORED_FALSE"] = storedFalse;
        Assert(ModOwnedFlagAdapter.TryGet(events, "EXAMPLE.MOD:STORED_FALSE", out bool storedValue));
        Assert(!storedValue);

        FlagObject preserved = (FlagObject)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(FlagObject));
        preserved.value = true;
        preserved.preserveInNewGamePlus = true;
        flags["EXAMPLE.MOD:PRESERVED"] = preserved;
        Assert(ModOwnedFlagAdapter.TryGet(events, "EXAMPLE.MOD:PRESERVED", out bool preservedValue));
        Assert(preservedValue && flags["EXAMPLE.MOD:PRESERVED"].preserveInNewGamePlus);
    }

    private static TMod CreateUninitializedMod<TMod>(string id) where TMod : BlasMod
    {
        TMod mod = (TMod)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(TMod));
        FieldInfo idField = typeof(BlasMod).GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        idField.SetValue(mod, id);
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
