using Blasphemous.CheatConsole;
using Blasphemous.ModdingAPI;
using Blasphemous.NewbieEltonLibs.Components;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using System;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.TestMod;

internal sealed class NewbieEltonLibsTestMod : BlasMod
{
    private ModAnimator? _trackedAnimator;
    private SpriteRenderer? _trackedRenderer;
    private Sprite[]? _trackedFrames;
    private Sprite? _lastTrackedSprite;

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

    protected override void OnUpdate()
    {
        if (_trackedRenderer == null || _trackedFrames == null)
        {
            return;
        }

        Sprite? currentSprite = _trackedRenderer.sprite;
        if (currentSprite == _lastTrackedSprite)
        {
            return;
        }

        _lastTrackedSprite = currentSprite;
        int frameIndex = Array.IndexOf(_trackedFrames, currentSprite);
        Log($"S3|frame|index={frameIndex}|name={(currentSprite == null ? "NULL" : currentSprite.name)}|time={Time.time:0.000}");
    }

    internal void Log(string message)
    {
        ModLog.Info($"[{ModInfo.ModId}] {message}", this);
    }

    internal void TrackAnimator(ModAnimator animator, SpriteRenderer renderer, Sprite[] frames)
    {
        _trackedAnimator = animator;
        _trackedRenderer = renderer;
        _trackedFrames = frames;
        _lastTrackedSprite = null;
    }

    internal bool StopTrackedAnimator(out Sprite? currentSprite)
    {
        currentSprite = _trackedRenderer == null ? null : _trackedRenderer.sprite;
        if (_trackedAnimator == null)
        {
            return false;
        }

        _trackedAnimator.Animation = null;
        return true;
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
