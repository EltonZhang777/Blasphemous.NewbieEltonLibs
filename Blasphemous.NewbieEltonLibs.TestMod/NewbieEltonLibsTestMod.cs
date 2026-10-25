using Blasphemous.CheatConsole;
using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Input;
using Blasphemous.NewbieEltonLibs.Audio;
using Blasphemous.NewbieEltonLibs.Components;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;
using Blasphemous.NewbieEltonLibs.Storage;
using FMOD;
using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.TestMod;

internal sealed class NewbieEltonLibsTestMod : BlasMod
{
    internal const string TestKeybinding = "s4_test_action";

    private readonly MonoBehaviour _coroutineHost;
    private ModAnimator? _trackedAnimator;
    private SpriteRenderer? _trackedRenderer;
    private Sprite[]? _trackedFrames;
    private Sprite? _lastTrackedSprite;
    private readonly object _workerStateLock = new();
    private readonly Queue<string> _workerLogs = new();
    private readonly Queue<PendingSoundPlayback> _pendingSoundPlaybacks = new();
    private readonly List<FMOD.Sound> _ownedImportedSounds = new();
    private readonly FMODSoundStorage _fmodSoundStorage = new();
    private bool _gameExited = true;

    private sealed class PendingSoundPlayback
    {
        internal readonly FMODSoundImportResult Result;
        internal readonly FMOD.System LowlevelSystem;
        internal readonly int MainThreadId;
        internal readonly int CallbackThreadId;

        internal PendingSoundPlayback(FMOD.System lowlevelSystem, FMODSoundImportResult result, int mainThreadId, int callbackThreadId)
        {
            LowlevelSystem = lowlevelSystem;
            Result = result;
            MainThreadId = mainThreadId;
            CallbackThreadId = callbackThreadId;
        }
    }

    internal NewbieEltonLibsTestMod(MonoBehaviour coroutineHost)
        : base(ModInfo.ModId, ModInfo.ModName, ModInfo.ModAuthor, ModInfo.ModVersion)
    {
        _coroutineHost = coroutineHost;
    }

    protected override void OnInitialize()
    {
        InputHandler.RegisterDefaultKeybindings(new Dictionary<string, KeyCode>
        {
            [TestKeybinding] = KeyCode.F8
        });
        LocalizationHandler.RegisterDefaultLanguage("en");
        RegisterVerificationFlags();
        ModLog.Info($"[{ModInfo.ModId}] MOD_INITIALIZED", this);
    }

    protected override void OnNewGame()
    {
        SetGameActive();
        LogFlagLifecycle("NEW_GAME");
    }

    protected override void OnLoadGame()
    {
        SetGameActive();
        LogFlagLifecycle("LOAD_GAME");
    }

    protected override void OnExitGame()
    {
        LogFlagLifecycle("EXIT_GAME");
        ReleaseImportedSounds();
    }

    protected override void OnRegisterServices(ModServiceProvider provider)
    {
        provider.RegisterCommand(new TestModCommand(this));
    }

    protected override void OnUpdate()
    {
        FlushWorkerLogs();
        ProcessPendingSoundPlaybacks();

        int axisEdge = InputHandler.GetAxisDown(AxisCode.MoveHorizontal, true);
        if (axisEdge != 0)
        {
            Log($"S4|input-axis|edge={axisEdge}");
        }

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

    internal void StartAudioSmokeCheck()
    {
        AudioClipStorage audioClips = new();
        _coroutineHost.StartCoroutine(VerifyAudioImport(audioClips, "mp3", "smoke-mp3", "audio/smoke.mp3", true));
        _coroutineHost.StartCoroutine(VerifyAudioImport(audioClips, "wav", "smoke-wav", "audio/smoke.wav", true));
        _coroutineHost.StartCoroutine(VerifyAudioImport(audioClips, "traversal", "unsafe-path", "../outside.mp3", false,
            "The audio path cannot contain a parent directory segment."));
        _coroutineHost.StartCoroutine(VerifyAudioImport(audioClips, "unsupported", "unsupported-format", "audio/clip.ogg", false,
            "Only MP3 and PCM WAV files are supported."));
    }

    internal void StartFMODSoundStorageSmokeCheck()
    {
        FMODSoundStorage first = new();
        FMODSoundStorage second = new();
        Sound sound = new() { handle = new IntPtr(75) };
        Sound duplicate = new() { handle = new IntPtr(76) };

        bool registered = first.TryRegister("smoke", sound);
        bool duplicateRejected = !first.TryRegister("smoke", duplicate);
        bool lookup = first.TryGet("smoke", out Sound stored) && stored.handle == sound.handle;
        bool isolated = !second.TryGet("smoke", out _);
        bool passed = registered && duplicateRejected && lookup && isolated;

        Log($"S6|sound-storage|{(passed ? "PASS" : "FAIL")}|registered={registered}|lookup={lookup}|duplicateRejected={duplicateRejected}|isolated={isolated}");
    }

    internal void StartFMODSoundPlaybackCheck(int mainThreadId)
    {
        try
        {
            FMOD.System lowlevelSystem = RuntimeManager.LowlevelSystem;
            FMODSoundImporter.ImportAsync(lowlevelSystem, FileHandler, "audio/test_meme.mp3",
                result => QueueSoundPlayback(lowlevelSystem, result, mainThreadId));
            Log("S8|request|START|file=audio/test_meme.mp3");
        }
        catch (Exception exception)
        {
            Log($"S8|playback|FAIL|request could not be queued: {exception}");
        }
    }

    private IEnumerator VerifyAudioImport(AudioClipStorage audioClips, string label, string name, string path,
        bool expectSuccess, string? expectedFailure = null)
    {
        int successCount = 0;
        int failureCount = 0;
        string? callbackName = null;
        AudioClip? importedClip = null;
        string? failure = null;

        yield return audioClips.ImportAsync(FileHandler, name, path,
            (completedName, clip) =>
            {
                successCount++;
                callbackName = completedName;
                importedClip = clip;
            },
            (completedName, message) =>
            {
                failureCount++;
                callbackName = completedName;
                failure = message;
            });
        yield return null;

        try
        {
            bool stored = audioClips.TryGet(name, out AudioClip? storedClip);
            bool passed = expectSuccess
                ? successCount == 1 && failureCount == 0 && callbackName == name && stored && storedClip == importedClip
                : successCount == 0 && failureCount == 1 && callbackName == name && !stored && failure == expectedFailure;

            Log($"S5|{label}|{(passed ? "PASS" : "FAIL")}|success={successCount}|failure={failureCount}|name={callbackName ?? "none"}|stored={stored}|reason={failure ?? "none"}");
        }
        finally
        {
            if (importedClip is not null)
            {
                UnityEngine.Object.Destroy(importedClip);
            }
        }
    }

    internal void QueueWorkerLog(string message)
    {
        lock (_workerStateLock)
        {
            _workerLogs.Enqueue(message);
        }
    }

    private void QueueSoundPlayback(FMOD.System lowlevelSystem, FMODSoundImportResult result, int mainThreadId)
    {
        int callbackThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        lock (_workerStateLock)
        {
            if (!_gameExited)
            {
                if (result.Succeeded)
                {
                    _ownedImportedSounds.Add(result.Sound);
                }

                _pendingSoundPlaybacks.Enqueue(new PendingSoundPlayback(lowlevelSystem, result, mainThreadId, callbackThreadId));
                return;
            }
        }

        if (result.Succeeded)
        {
            ReleaseLateImportedSound(result.Sound);
        }
        QueueWorkerLog($"S8|playback|FAIL|game exited before the import callback could be processed;error={result.ErrorMessage}");
    }

    private void ReleaseLateImportedSound(FMOD.Sound sound)
    {
        try
        {
            if (sound.hasHandle())
            {
                FMOD.RESULT result = sound.release();
                if (result != FMOD.RESULT.OK)
                {
                    QueueWorkerLog($"S8|late-release|FAIL|result={result}");
                }
            }
        }
        catch (Exception exception)
        {
            QueueWorkerLog($"S8|late-release|FAIL|exception={exception}");
        }
    }

    private void ProcessPendingSoundPlaybacks()
    {
        while (true)
        {
            PendingSoundPlayback pending;
            lock (_workerStateLock)
            {
                if (_pendingSoundPlaybacks.Count == 0)
                {
                    return;
                }

                pending = _pendingSoundPlaybacks.Dequeue();
            }

            if (!pending.Result.Succeeded)
            {
                Log($"S8|playback|FAIL|import failed;callbackThread={pending.CallbackThreadId};mainThread={pending.MainThreadId};error={pending.Result.ErrorMessage}");
                continue;
            }

            bool callbackOffMainThread = pending.CallbackThreadId != pending.MainThreadId;
            bool runningOnMainThread = System.Threading.Thread.CurrentThread.ManagedThreadId == pending.MainThreadId;
            bool registered = _fmodSoundStorage.TryRegister("test-meme", pending.Result.Sound)
                || _fmodSoundStorage.TryReplace("test-meme", pending.Result.Sound);
            bool retrieved = _fmodSoundStorage.TryGet("test-meme", out Sound sound)
                && sound.handle == pending.Result.Sound.handle;

            if (!callbackOffMainThread || !runningOnMainThread || !registered || !retrieved)
            {
                Log($"S8|playback|FAIL|callbackOffMain={callbackOffMainThread};handoffMain={runningOnMainThread};registered={registered};retrieved={retrieved}");
                continue;
            }

            try
            {
                FMOD.RESULT result = FMODSoundPlayer.PlayOnce(pending.LowlevelSystem, sound);
                Log($"S8|playback|{(result == FMOD.RESULT.OK ? "PASS" : "FAIL")}|callbackOffMain={callbackOffMainThread};handoffMain={runningOnMainThread};registered={registered};retrieved={retrieved};result={result};oneShot=true");
            }
            catch (Exception exception)
            {
                Log($"S8|playback|FAIL|callbackOffMain={callbackOffMainThread};handoffMain={runningOnMainThread};registered={registered};retrieved={retrieved};exception={exception}");
            }
        }
    }

    internal void RetainImportedSound(FMOD.Sound sound)
    {
        bool releaseImmediately;
        lock (_workerStateLock)
        {
            releaseImmediately = _gameExited;
            if (!releaseImmediately)
            {
                _ownedImportedSounds.Add(sound);
            }
        }

        if (releaseImmediately && sound.hasHandle())
        {
            FMOD.RESULT result = sound.release();
            if (result != FMOD.RESULT.OK)
            {
                QueueWorkerLog($"S7|late-release|FAIL|result={result}");
            }
        }
    }

    private void SetGameActive()
    {
        lock (_workerStateLock)
        {
            _gameExited = false;
        }
    }

    private void FlushWorkerLogs()
    {
        while (true)
        {
            string message;
            lock (_workerStateLock)
            {
                if (_workerLogs.Count == 0)
                {
                    return;
                }

                message = _workerLogs.Dequeue();
            }

            Log(message);
        }
    }

    private void ReleaseImportedSounds()
    {
        FMOD.Sound[] sounds;
        lock (_workerStateLock)
        {
            _gameExited = true;
            _pendingSoundPlaybacks.Clear();
            sounds = _ownedImportedSounds.ToArray();
            _ownedImportedSounds.Clear();
        }

        foreach (FMOD.Sound sound in sounds)
        {
            if (sound.hasHandle())
            {
                FMOD.RESULT result = sound.release();
                if (result != FMOD.RESULT.OK)
                {
                    Log($"S7|release|FAIL|result={result}");
                }
            }
        }
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
        currentSprite = _trackedRenderer?.sprite;
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
