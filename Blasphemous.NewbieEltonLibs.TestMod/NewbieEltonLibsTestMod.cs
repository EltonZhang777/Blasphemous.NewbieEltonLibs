using Blasphemous.CheatConsole;
using Blasphemous.ModdingAPI;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;

namespace Blasphemous.NewbieEltonLibs.TestMod;

internal sealed class NewbieEltonLibsTestMod : BlasMod
{
    internal NewbieEltonLibsTestMod()
        : base(ModInfo.ModId, ModInfo.ModName, ModInfo.ModAuthor, ModInfo.ModVersion)
    {
    }

    protected override void OnInitialize()
    {
        RegisterVerificationFlags();
        ModLog.Info($"[{ModInfo.ModId}] MOD_INITIALIZED", this);
    }

    protected override void OnNewGame()
    {
        LogFlagLifecycle("NEW_GAME");
    }

    protected override void OnLoadGame()
    {
        LogFlagLifecycle("LOAD_GAME");
    }

    protected override void OnExitGame()
    {
        LogFlagLifecycle("EXIT_GAME");
    }

    protected override void OnRegisterServices(ModServiceProvider provider)
    {
        provider.RegisterCommand(new TestModCommand(this));
    }

    internal void Log(string message)
    {
        ModLog.Info($"[{ModInfo.ModId}] {message}", this);
    }

    private void RegisterVerificationFlags()
    {
        ModFlagsManager.RegisterFlag(TestModCommand.AbsentFlag);
        ModFlagsManager.RegisterFlag(TestModCommand.StoredFalseFlag);
        ModFlagsManager.RegisterFlag(TestModCommand.DirectVanillaFlag);
        ModFlagsManager.RegisterFlag(TestModCommand.PreservedFlag, true);
        ModFlagsManager.RegisterFlag(TestModCommand.TransientFlag, false);
    }

    private void LogFlagLifecycle(string lifecycle)
    {
        RegisterVerificationFlags();
        bool preservedReadable = ModFlagsManager.TryGetFlag(TestModCommand.PreservedFlag, out bool preserved);
        bool transientReadable = ModFlagsManager.TryGetFlag(TestModCommand.TransientFlag, out bool transient);
        Log($"S1|{lifecycle}|preserved={(preservedReadable ? preserved.ToString() : "ABSENT")}|transient={(transientReadable ? transient.ToString() : "ABSENT")}");
    }
}
