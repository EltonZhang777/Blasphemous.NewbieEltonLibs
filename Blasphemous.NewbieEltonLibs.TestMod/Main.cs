using BepInEx;
using System;

namespace Blasphemous.NewbieEltonLibs.TestMod;

[BepInPlugin(ModInfo.ModId, ModInfo.ModName, ModInfo.ModVersion)]
[BepInDependency("Blasphemous.ModdingAPI", "3.0.1")]
internal sealed class Main : BaseUnityPlugin
{
    private NewbieEltonLibsTestMod? _mod;

    private void Start()
    {
        try
        {
            _mod = new NewbieEltonLibsTestMod();
            Logger.LogInfo($"[{ModInfo.ModId}] STARTUP_READY");
        }
        catch (Exception exception)
        {
            Logger.LogError($"[{ModInfo.ModId}] STARTUP_FAILED: {exception}");
        }
    }
}
