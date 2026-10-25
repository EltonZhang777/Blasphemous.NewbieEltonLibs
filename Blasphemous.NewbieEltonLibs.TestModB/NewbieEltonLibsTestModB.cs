using Blasphemous.CheatConsole;
using Blasphemous.ModdingAPI;

namespace Blasphemous.NewbieEltonLibs.TestModB;

internal sealed class NewbieEltonLibsTestModB : BlasMod
{
    internal NewbieEltonLibsTestModB()
        : base(ModInfo.ModId, ModInfo.ModName, ModInfo.ModAuthor, ModInfo.ModVersion)
    {
    }

    protected override void OnRegisterServices(ModServiceProvider provider)
    {
        provider.RegisterCommand(new UIPoolConsumerBCommand(this));
    }

    internal void Log(string message)
    {
        ModLog.Info($"[{ModInfo.ModId}] {message}", this);
    }
}