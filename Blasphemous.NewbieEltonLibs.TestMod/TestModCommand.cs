using Blasphemous.CheatConsole;
using Blasphemous.ModdingAPI;
using Blasphemous.NewbieEltonLibs.CheatConsole;
using Blasphemous.NewbieEltonLibs.Components;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using Blasphemous.NewbieEltonLibs.Storage;
using Framework.Managers;
using Gameplay.UI.Widgets;
using System;
using UnityEngine;

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

    [ModSubCommand("s3", "verify runtime resource storage and ModAnimator", validLengths: new[] { 0 })]
    private void RunResources(string[] parameters)
    {
        try
        {
            Sprite first = CreateSprite("S3_FIRST", Color.red);
            Sprite replacement = CreateSprite("S3_REPLACEMENT", Color.green);
            Sprite last = CreateSprite("S3_LAST", Color.blue);

            SpriteStorage sprites = new();
            SpriteStorage separateSprites = new();
            sprites.Register("cycle", first);
            bool duplicateTryRegister = !sprites.TryRegister("cycle", replacement);
            bool replace = sprites.TryReplace("cycle", replacement);
            bool missingReplace = !sprites.TryReplace("missing", replacement);
            bool get = sprites.TryGet("cycle", out Sprite? storedSprite) && storedSprite == replacement;
            bool missingGet = !sprites.TryGet("missing", out _);
            bool isolated = !separateSprites.TryGet("cycle", out _);
            bool duplicateRegister = Throws<InvalidOperationException>(() => sprites.Register("cycle", first));
            bool missingReplaceThrow = Throws<System.Collections.Generic.KeyNotFoundException>(() => sprites.Replace("missing", first));
            ReportS3("sprite-storage", duplicateTryRegister && replace && missingReplace && get && missingGet && isolated && duplicateRegister && missingReplaceThrow,
                $"duplicateTryRegister={duplicateTryRegister},replace={replace},missingReplace={missingReplace},get={get},missingGet={missingGet},isolated={isolated},duplicateRegister={duplicateRegister},missingReplaceThrow={missingReplaceThrow}");

            AnimationInfo animation = new("S3_CYCLE", new[] { first, replacement, last }, 0.1f);
            AnimationInfo replacementAnimation = new("S3_CYCLE", new[] { last, first }, 0.1f);
            AnimationStorage animations = new();
            AnimationStorage separateAnimations = new();
            animations.Register(animation);
            bool duplicateAnimation = !animations.TryRegister(animation);
            bool replaceAnimation = animations.TryReplace(replacementAnimation);
            bool restoreAnimation = animations.TryReplace(animation);
            bool getAnimation = animations.TryGet("S3_CYCLE", out AnimationInfo? storedAnimation) && storedAnimation == animation;
            bool missingAnimation = !animations.TryGet("missing", out _);
            bool isolatedAnimation = !separateAnimations.TryGet("S3_CYCLE", out _);
            bool invalidAnimation = Throws<ArgumentException>(() => new AnimationInfo("", new[] { first }, 0));
            bool invalidImport = Throws<ArgumentException>(() => new AnimationImportInfo("S3_INVALID", " ", 0, 0, 0));
            ReportS3("animation-storage", duplicateAnimation && replaceAnimation && restoreAnimation && getAnimation && missingAnimation && isolatedAnimation && invalidAnimation && invalidImport,
                $"duplicate={duplicateAnimation},replace={replaceAnimation},restore={restoreAnimation},get={getAnimation},missing={missingAnimation},isolated={isolatedAnimation},invalidAnimation={invalidAnimation},invalidImport={invalidImport}");

            GameObject animatorObject = new("NewbieEltonLibsTestMod.S3.Animator");
            ModAnimator animator = animatorObject.GetOrElseAddComponent<ModAnimator>();
            SpriteRenderer renderer = animatorObject.GetComponent<SpriteRenderer>();
            animator.Animation = animation;
            _mod.TrackAnimator(animator, renderer, animation.Sprites);
            renderer.sortingOrder = 32767;
            renderer.enabled = true;
            if (Camera.main != null)
            {
                animatorObject.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 2f;
            }

            bool firstFrame = renderer.sprite == first;
            ReportS3("animator-first-frame", firstFrame, $"sprite={(renderer.sprite == null ? "NULL" : renderer.sprite.name)}");
            ReportS3("animator-duration", true, "assigned 3 frames at 0.1s; inspect S3|frame sequence for timing and existing last-frame skip");
            Write("S3 running. Observe S3|frame order, then run newbie-test s3-stop; the current sprite should remain and no resource should be destroyed.");
        }
        catch (Exception exception)
        {
            ReportS3("exception", false, exception.ToString());
        }
    }

    [ModSubCommand("s3-stop", "set the tracked ModAnimator animation to null", validLengths: new[] { 0 })]
    private void StopResources(string[] parameters)
    {
        bool stopped = _mod.StopTrackedAnimator(out Sprite? currentSprite);
        ReportS3("animator-null", stopped, $"stopped={stopped},current={(currentSprite == null ? "NULL" : currentSprite.name)}; resources remain caller-owned");
    }

    private static Sprite CreateSprite(string name, Color color)
    {
        Texture2D texture = new(1, 1);
        texture.name = name + "_Texture";
        texture.SetPixel(0, 0, color);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        sprite.name = name;
        return sprite;
    }

    private static bool Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return true;
        }

        return false;
    }

    private void ReportS3(string check, bool passed, string detail)
    {
        string line = $"S3|{check}|{(passed ? "PASS" : "FAIL")}|{detail}";
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
