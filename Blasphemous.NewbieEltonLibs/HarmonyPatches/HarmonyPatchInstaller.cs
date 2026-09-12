using Blasphemous.ModdingAPI;
using HarmonyLib;
using System;

namespace Blasphemous.NewbieEltonLibs.HarmonyPatches;

internal static class HarmonyPatchInstaller
{
    private const string HarmonyId = "Blasphemous.NewbieEltonLibs";

    private static readonly object SyncRoot = new object();

    private static bool patchApplied;

    internal static bool TryEnsurePatched()
    {
        if (patchApplied)
            return true;

        lock (SyncRoot)
        {
            if (patchApplied)
                return true;

            try
            {
                Harmony harmony = new Harmony(HarmonyId);
                harmony.PatchAll(typeof(HarmonyPatchInstaller).Assembly);
                patchApplied = true;
                return true;
            }
            catch (Exception exception)
            {
                ModLog.Error($"Failed to install cheat-console logging patches: {exception}");
                return false;
            }
        }
    }
}
