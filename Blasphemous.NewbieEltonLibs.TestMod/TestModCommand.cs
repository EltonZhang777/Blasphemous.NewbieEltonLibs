using Blasphemous.CheatConsole;
using Blasphemous.ModdingAPI;
using Blasphemous.NewbieEltonLibs.CheatConsole;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using Framework.Managers;
using Gameplay.UI.Widgets;
using System;

namespace Blasphemous.NewbieEltonLibs.TestMod;

internal sealed class TestModCommand : AutoModCommand
{
    internal const string AbsentFlag = "absent_registered";
    internal const string StoredFalseFlag = "stored_false";
    internal const string DirectVanillaFlag = "direct_vanilla";
    internal const string PreservedFlag = "preserved";
    internal const string TransientFlag = "transient";

    private readonly NewbieEltonLibsTestMod _mod;

    internal TestModCommand(NewbieEltonLibsTestMod mod)
    {
        _mod = mod;
    }

    protected override string CommandName => "newbie-test";

    [ModSubCommand("s1", "verify mod-owned flags and lifecycle markers", validLengths: new[] { 0 })]
    [ModSubCommand("flags", "alias for s1", validLengths: new[] { 0 })]
    private void RunFlags(string[] parameters)
    {
        try
        {
            bool absentRegistered = ModFlagsManager.RegisterFlag(AbsentFlag);
            bool falseRegistered = ModFlagsManager.RegisterFlag(StoredFalseFlag);
            bool preservedRegistered = ModFlagsManager.RegisterFlag(PreservedFlag, true);
            bool transientRegistered = ModFlagsManager.RegisterFlag(TransientFlag, false);
            Report("registration", absentRegistered && falseRegistered && preservedRegistered && transientRegistered,
                $"absent={absentRegistered},false={falseRegistered},preserved={preservedRegistered},transient={transientRegistered}");

            bool absentRead = ModFlagsManager.TryGetFlag(AbsentFlag, out bool absentValue);
            Report("absent-registered", !absentRead,
                $"TryGet={absentRead},value={absentValue}; expected an absent vanilla entry");

            bool wroteFalse = ModFlagsManager.TrySetFlag(StoredFalseFlag, false);
            bool readFalse = ModFlagsManager.TryGetFlag(StoredFalseFlag, out bool falseValue);
            Report("stored-false", wroteFalse && readFalse && !falseValue,
                $"write={wroteFalse},read={readFalse},value={falseValue}");

            bool wroteTrue = ModFlagsManager.TrySetFlag(StoredFalseFlag, true);
            bool readTrue = ModFlagsManager.TryGetFlag(StoredFalseFlag, out bool trueValue);
            Report("stored-true", wroteTrue && readTrue && trueValue,
                $"write={wroteTrue},read={readTrue},value={trueValue}");

            bool directMutation = ModFlagsManager.TrySetFlag("direct_vanilla", true);
            if (directMutation)
            {
                string vanillaId = ModFlagsManager.FormatToFlagId(ModInfo.ModId + ":direct_vanilla");
                Core.Events.SetFlag(vanillaId, false, false);
                bool directRead = ModFlagsManager.TryGetFlag("direct_vanilla", out bool directValue);
                Report("vanilla-mutation", directRead && !directValue,
                    $"vanilla={vanillaId},read={directRead},value={directValue}");
            }
            else
            {
                Report("vanilla-mutation", false, "could not seed the registered flag");
            }

            bool wrongOwnerRead = ModFlagsManager.TryGetFlag(StoredFalseFlag, typeof(ForeignOwnerProbe), out _);
            Report("wrong-owner", !wrongOwnerRead, $"TryGet={wrongOwnerRead}; expected false");

            bool preservedWrite = ModFlagsManager.TrySetFlag(PreservedFlag, true);
            bool transientWrite = ModFlagsManager.TrySetFlag(TransientFlag, true);
            Report("lifecycle-seed", preservedWrite && transientWrite,
                $"preserved={preservedWrite},transient={transientWrite}; save/reload/NG+ and inspect S1 lifecycle lines");

            Write("S1 seeded. Save and reload this slot, then perform the normal New Game Plus transition. Check output_log.txt for S1|LOAD_GAME, S1|EXIT_GAME, and S1|NEW_GAME values.");
        }
        catch (Exception exception)
        {
            Report("exception", false, exception.ToString());
        }
    }

    [ModSubCommand("s2-input", "enable or disable console input logging", "<on|off>", validLengths: new[] { 1 })]
    private void ConfigureInputLogging(string[] parameters)
    {
        if (!TryParseToggle(parameters[0], out bool active))
        {
            ReportS2("input-config", false, "expected on or off");
            return;
        }

        CheatConsoleLogging.LogCheatConsoleInput(active);
        ReportS2("input-config", true, $"active={active},debugBuildOnly=true");
    }

    [ModSubCommand("s2-output", "enable or disable console output logging", "<on|off>", validLengths: new[] { 1 })]
    private void ConfigureOutputLogging(string[] parameters)
    {
        if (!TryParseToggle(parameters[0], out bool active))
        {
            ReportS2("output-config", false, "expected on or off");
            return;
        }

        CheatConsoleLogging.LogCheatConsoleOutput(active);
        ReportS2("output-config", true, $"active={active},debugBuildOnly=true");
    }

    [ModSubCommand("s2-level", "set input or output log level", "<input|output> <level>", validLengths: new[] { 2 })]
    private void ConfigureLoggingLevel(string[] parameters)
    {
        if (!Enum.IsDefined(typeof(LogLevel), parameters[1]))
        {
            ReportS2("level-config", false, $"unsupported level '{parameters[1]}'");
            return;
        }

        LogLevel level = (LogLevel)Enum.Parse(typeof(LogLevel), parameters[1], true);
        if (string.Equals(parameters[0], "input", StringComparison.OrdinalIgnoreCase))
        {
            CheatConsoleLogging.LogCheatConsoleInput(true, level);
        }
        else if (string.Equals(parameters[0], "output", StringComparison.OrdinalIgnoreCase))
        {
            CheatConsoleLogging.LogCheatConsoleOutput(true, level);
        }
        else
        {
            ReportS2("level-config", false, "expected input or output");
            return;
        }

        ReportS2("level-config", true, $"channel={parameters[0]},level={level},active=true");
    }

    [ModSubCommand("s2-write", "write one visible console line", "<message>")]
    private void WriteOutput(string[] parameters)
    {
        Write(string.Join(" ", parameters));
    }

    [ModSubCommand("s2-programmatic", "process a command without Submit", validLengths: new[] { 0 })]
    private void ProcessProgrammatically(string[] parameters)
    {
        ConsoleWidget console = ConsoleWidget.Instance;
        if (console == null)
        {
            ReportS2("programmatic", false, "ConsoleWidget.Instance is unavailable");
            return;
        }

        console.ProcessCommand("newbie-test s2-write PROGRAMMATIC_OUTPUT");
        ReportS2("programmatic", true, "ProcessCommand invoked; inspect input records for no nested player-input entry");
    }

    [ModSubCommand("s2-gating", "enable both channels with Debug caller gating", validLengths: new[] { 0 })]
    private void EnableDebugGatedLogging(string[] parameters)
    {
        CheatConsoleLogging.LogCheatConsoleInput(true, LogLevel.Info, true);
        CheatConsoleLogging.LogCheatConsoleOutput(true, LogLevel.Info, true);
        ReportS2("debug-gating", true, "both channels configured with debugBuildOnly=true; this TestMod package is Debug");
    }

    private bool TryParseToggle(string value, out bool active)
    {
        if (string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
        {
            active = true;
            return true;
        }

        if (string.Equals(value, "off", StringComparison.OrdinalIgnoreCase))
        {
            active = false;
            return true;
        }

        active = false;
        return false;
    }

    private void ReportS2(string check, bool passed, string detail)
    {
        string line = $"S2|{check}|{(passed ? "PASS" : "FAIL")}|{detail}";
        _mod.Log(line);
        Write(line);
    }

    private void Report(string check, bool passed, string detail)
    {
        string line = $"S1|{check}|{(passed ? "PASS" : "FAIL")}|{detail}";
        _mod.Log(line);
        Write(line);
    }

    private sealed class ForeignOwnerProbe : BlasMod
    {
        internal ForeignOwnerProbe()
            : base("NewbieEltonLibsForeignOwnerProbe", "Foreign owner probe", "NewbieElton", "0.0.0")
        {
        }
    }
}
