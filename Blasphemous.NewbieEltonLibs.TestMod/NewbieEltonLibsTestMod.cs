using Blasphemous.ModdingAPI;

namespace Blasphemous.NewbieEltonLibs.TestMod;

internal sealed class NewbieEltonLibsTestMod : BlasMod
{
    internal NewbieEltonLibsTestMod()
        : base(ModInfo.ModId, ModInfo.ModName, ModInfo.ModAuthor, ModInfo.ModVersion)
    {
    }

    protected override void OnInitialize()
    {
        ModLog.Info($"[{ModInfo.ModId}] MOD_INITIALIZED", this);
    }
}
