using BepInEx;
using System;

namespace Blasphemous.NewbieEltonLibs.TestModB;

[BepInPlugin(ModInfo.ModId, ModInfo.ModName, ModInfo.ModVersion)]
[BepInDependency("Blasphemous.ModdingAPI", "3.0.1")]
[BepInDependency("NewbieEltonLibsTestMod")]
internal sealed class Main : BaseUnityPlugin
{
    private NewbieEltonLibsTestModB? _mod;

    private void Start()
    {
        try
        {
            _mod = new NewbieEltonLibsTestModB();
            Logger.LogInfo($"[{ModInfo.ModId}] STARTUP_READY");
        }
        catch (Exception exception)
        {
            Logger.LogError($"[{ModInfo.ModId}] STARTUP_FAILED: {exception}");
        }
    }
}