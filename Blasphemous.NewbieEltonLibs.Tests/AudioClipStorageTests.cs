using Blasphemous.ModdingAPI.Files;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Blasphemous.NewbieEltonLibs.Storage;
using UnityEngine;
using Xunit;

namespace Blasphemous.NewbieEltonLibs.Tests;

/// <summary>Verifies named AudioClip storage through its public contract.</summary>
public sealed class AudioClipStorageTests
{
    /// <summary>Verifies registration, replacement, and strict/Try miss behavior.</summary>
    [Fact]
    public void AudioClipStorageSupportsStrictAndTryOperations()
    {
        AudioClipStorage storage = CreateStorage();
        AudioClip first = CreateAudioClip();
        AudioClip replacement = CreateAudioClip();

        storage.Register("menu", first);

        Assert.Same(first, storage["menu"]);
        Assert.False(storage.TryRegister("menu", replacement));
        Assert.Throws<InvalidOperationException>(() => storage.Register("menu", replacement));
        Assert.False(storage.TryReplace("missing", replacement));
        Assert.Throws<KeyNotFoundException>(() => storage.Replace("missing", replacement));
        Assert.True(storage.TryReplace("menu", replacement));
        Assert.Same(replacement, storage["menu"]);
        Assert.True(storage.TryGet("menu", out AudioClip? found));
        Assert.Same(replacement, found);
        Assert.False(storage.TryGet("missing", out _));
        Assert.Throws<KeyNotFoundException>(() => _ = storage["missing"]);
    }

    /// <summary>Verifies separate consuming mods retain independent AudioClip names.</summary>
    [Fact]
    public void AudioClipStorageInstancesAreIndependent()
    {
        AudioClipStorage first = CreateStorage();
        AudioClipStorage second = CreateStorage();

        first.Register("shared-name", CreateAudioClip());

        Assert.True(first.TryGet("shared-name", out _));
        Assert.False(second.TryGet("shared-name", out _));
    }

    /// <summary>Verifies an MP3 fixture decodes into interleaved PCM samples.</summary>
    [Fact]
    public void AudioClipImportDecodesMp3Fixture()
    {
        string mp3Path = Path.Combine(AppContext.BaseDirectory, "AudioFixtures", "audio", "smoke.mp3");

        float[] samples = AudioClipStorage.DecodeMp3(mp3Path, out int channels, out int sampleRate);

        Assert.NotEmpty(samples);
        Assert.InRange(channels, 1, 2);
        Assert.True(sampleRate > 0);
        Assert.Equal(0, samples.Length % channels);
        Assert.True(Array.Exists(samples, sample => sample != 0f));
    }

    /// <summary>Verifies invalid names and null clips follow strict/Try validation.</summary>
    [Fact]
    public void AudioClipStorageRejectsInvalidNamesAndNullClips()
    {
        AudioClipStorage storage = new();

        Assert.Throws<ArgumentException>(() => storage.Register(string.Empty, CreateAudioClip()));
        Assert.Throws<ArgumentException>(() => storage.Register("menu", null!));
        Assert.False(storage.TryRegister(string.Empty, CreateAudioClip()));
        Assert.False(storage.TryRegister("menu", null!));
        Assert.False(storage.TryReplace("menu", null!));
        Assert.False(storage.TryGet(string.Empty, out _));
    }

    /// <summary>Verifies traversal and unsupported formats fail once without storing an AudioClip.</summary>
    [Fact]
    public void AudioClipImportRejectsTraversalAndUnsupportedFormatsExactlyOnce()
    {
        AudioClipStorage storage = CreateStorage();
        FileHandler fileHandler = CreateFileHandler(AppContext.BaseDirectory);
        int successCount = 0;
        int failureCount = 0;
        string? callbackName = null;
        string? failure = null;

        IEnumerator traversal = storage.ImportAsync(fileHandler, "traversal", "../outside.mp3",
            (_, _) => successCount++, (name, message) =>
            {
                failureCount++;
                callbackName = name;
                failure = message;
            });

        Assert.False(traversal.MoveNext());
        Assert.False(traversal.MoveNext());
        Assert.Equal(0, successCount);
        Assert.Equal(1, failureCount);
        Assert.Equal("traversal", callbackName);
        Assert.False(string.IsNullOrEmpty(failure));
        Assert.False(storage.TryGet("traversal", out _));

        successCount = 0;
        failureCount = 0;
        callbackName = null;
        failure = null;
        IEnumerator unsupported = storage.ImportAsync(fileHandler, "unsupported", "audio/clip.ogg",
            (_, _) => successCount++, (name, message) =>
            {
                failureCount++;
                callbackName = name;
                failure = message;
            });

        Assert.False(unsupported.MoveNext());
        Assert.False(unsupported.MoveNext());
        Assert.Equal(0, successCount);
        Assert.Equal(1, failureCount);
        Assert.Equal("unsupported", callbackName);
        Assert.Equal("Only MP3 and PCM WAV files are supported.", failure);
        Assert.False(storage.TryGet("unsupported", out _));
    }

    /// <summary>Verifies Data roots reached through a symbolic link or junction are rejected.</summary>
    [Fact]
    public void AudioClipImportRejectsReparsePointsAtDataRootAndItsAncestors()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), "NewbieAudioReparse-" + Guid.NewGuid().ToString("N"));
        string dataTargetPath = Path.Combine(tempPath, "target", "Data");
        string audioTargetPath = Path.Combine(dataTargetPath, "audio");
        Directory.CreateDirectory(audioTargetPath);
        File.WriteAllBytes(Path.Combine(audioTargetPath, "clip.mp3"), new byte[0]);

        string parentLinkPath = Path.Combine(tempPath, "linked-parent");
        CreateDirectoryLink(parentLinkPath, Path.GetDirectoryName(dataTargetPath)!);
        AssertReparseDataPathRejected(Path.Combine(parentLinkPath, "Data"), "ancestor-link");

        string rootLinkPath = Path.Combine(tempPath, "linked-data");
        CreateDirectoryLink(rootLinkPath, dataTargetPath);
        AssertReparseDataPathRejected(rootLinkPath, "root-link");
    }

    /// <summary>Verifies non-PCM WAV data fails once and leaves storage unchanged.</summary>
    [Fact]
    public void AudioClipImportRejectsNonPcmWaveExactlyOnce()
    {
        AudioClipStorage storage = CreateStorage();
        string dataPath = Path.Combine(AppContext.BaseDirectory, "AudioFixtures");
        FileHandler fileHandler = CreateFileHandler(dataPath);
        int successCount = 0;
        int failureCount = 0;
        string? callbackName = null;
        string? failure = null;

        IEnumerator import = storage.ImportAsync(fileHandler, "float-wave", "audio/non-pcm.wav",
            (_, _) => successCount++, (name, message) =>
            {
                failureCount++;
                callbackName = name;
                failure = message;
            });

        Assert.False(import.MoveNext());
        Assert.False(import.MoveNext());
        Assert.Equal(0, successCount);
        Assert.Equal(1, failureCount);
        Assert.Equal("float-wave", callbackName);
        Assert.False(string.IsNullOrEmpty(failure));
        Assert.False(storage.TryGet("float-wave", out _));
    }

    /// <summary>Verifies a duplicate import name fails without replacing its existing clip.</summary>
    [Fact]
    public void AudioClipImportRejectsDuplicateNameWithoutReplacingStoredClip()
    {
        AudioClipStorage storage = CreateStorage();
        AudioClip existing = CreateAudioClip();
        storage.Register("existing", existing);
        FileHandler fileHandler = CreateFileHandler(AppContext.BaseDirectory);
        int successCount = 0;
        int failureCount = 0;
        string? callbackName = null;
        string? failure = null;

        IEnumerator import = storage.ImportAsync(fileHandler, "existing", "audio/clip.mp3",
            (_, _) => successCount++, (name, message) =>
            {
                failureCount++;
                callbackName = name;
                failure = message;
            });

        Assert.False(import.MoveNext());
        Assert.False(import.MoveNext());
        Assert.Equal(0, successCount);
        Assert.Equal(1, failureCount);
        Assert.Equal("existing", callbackName);
        Assert.False(string.IsNullOrEmpty(failure));
        Assert.True(storage.TryGet("existing", out AudioClip? stored));
        Assert.Same(existing, stored);
    }

    /// <summary>Verifies an unstarted, disposed import does not leave a name reservation.</summary>
    [Fact]
    public void AudioClipImportDoesNotReserveNameBeforeItStarts()
    {
        AudioClipStorage storage = CreateStorage();
        FileHandler fileHandler = CreateFileHandler(AppContext.BaseDirectory);
        int callbackCount = 0;
        IEnumerator import = storage.ImportAsync(fileHandler, "pending", "../outside.mp3",
            (_, _) => callbackCount++, (_, _) => callbackCount++);

        ((IDisposable)import).Dispose();

        Assert.Equal(0, callbackCount);
        Assert.True(storage.TryRegister("pending", CreateAudioClip()));
    }

    /// <summary>Verifies disposing a started import releases its name and delivers one failure callback.</summary>
    [Fact]
    public void AudioClipImportCancellationAfterStartReleasesReservationOnce()
    {
        string dataPath = CreatePcmDataPath();
        int requestDisposeCount = 0;
        AudioClipStorage storage = new AudioClipStorage(clip => clip is not null,
            _ => (WWW)RuntimeHelpers.GetUninitializedObject(typeof(WWW)), _ => requestDisposeCount++);
        FileHandler fileHandler = CreateFileHandler(dataPath);
        int successCount = 0;
        int failureCount = 0;
        string? callbackName = null;
        string? failureMessage = null;
        IEnumerator import = storage.ImportAsync(fileHandler, "cancelled", "audio/clip.wav",
            (_, _) => successCount++, (name, message) =>
            {
                failureCount++;
                callbackName = name;
                failureMessage = message;
            });

        Assert.True(import.MoveNext(), failureMessage);
        Assert.False(storage.TryRegister("cancelled", CreateAudioClip()));
        ((IDisposable)import).Dispose();
        ((IDisposable)import).Dispose();

        Assert.Equal(0, successCount);
        Assert.Equal(1, failureCount);
        Assert.Equal(1, requestDisposeCount);
        Assert.Equal("cancelled", callbackName);
        Assert.True(storage.TryRegister("cancelled", CreateAudioClip()));
    }

    /// <summary>Verifies a missing request fails once and releases its reserved name.</summary>
    [Fact]
    public void AudioClipImportRejectsNullRequestAndReleasesReservation()
    {
        string dataPath = CreatePcmDataPath();
        int successCount = 0;
        int failureCount = 0;
        string? callbackName = null;
        AudioClipStorage storage = new AudioClipStorage(clip => clip is not null,
            _ => null!, _ => { });
        FileHandler fileHandler = CreateFileHandler(dataPath);
        IEnumerator import = storage.ImportAsync(fileHandler, "missing-request", "audio/clip.wav",
            (_, _) => successCount++, (name, _) =>
            {
                failureCount++;
                callbackName = name;
            });

        Assert.False(import.MoveNext());
        Assert.False(import.MoveNext());

        Assert.Equal(0, successCount);
        Assert.Equal(1, failureCount);
        Assert.Equal("missing-request", callbackName);
        Assert.False(storage.TryGet("missing-request", out _));
        Assert.True(storage.TryRegister("missing-request", CreateAudioClip()));
    }

    private static string CreatePcmDataPath()
    {
        string dataPath = Path.Combine(Path.GetTempPath(), "NewbieAudioFixture-" + Guid.NewGuid().ToString("N"));
        string audioPath = Path.Combine(dataPath, "audio");
        Directory.CreateDirectory(audioPath);
        WritePcmWave(Path.Combine(audioPath, "clip.wav"));
        return dataPath;
    }

    private static AudioClipStorage CreateStorage()
    {
        return new AudioClipStorage(clip => clip is not null);
    }

    private static void AssertReparseDataPathRejected(string dataPath, string name)
    {
        AudioClipStorage storage = CreateStorage();
        FileHandler fileHandler = CreateFileHandler(dataPath);
        int successCount = 0;
        int failureCount = 0;
        string? callbackName = null;
        string? failure = null;
        IEnumerator import = storage.ImportAsync(fileHandler, name, "audio/clip.mp3",
            (_, _) => successCount++, (callback, message) =>
            {
                failureCount++;
                callbackName = callback;
                failure = message;
            });

        Assert.False(import.MoveNext());
        Assert.False(import.MoveNext());
        Assert.Equal(0, successCount);
        Assert.Equal(1, failureCount);
        Assert.Equal(name, callbackName);
        Assert.Contains("symbolic link or junction", failure ?? string.Empty);
        Assert.False(storage.TryGet(name, out _));
    }

    private static void CreateDirectoryLink(string linkPath, string targetPath)
    {
        if (OperatingSystem.IsWindows())
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "cmd.exe",
                Arguments = "/c mklink /J \"" + linkPath + "\" \"" + targetPath + "\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using Process process = Process.Start(startInfo)!;
            process.WaitForExit();
            Assert.Equal(0, process.ExitCode);
            return;
        }

        Directory.CreateSymbolicLink(linkPath, targetPath);
    }

    private static AudioClip CreateAudioClip()
    {
        return (AudioClip)RuntimeHelpers.GetUninitializedObject(typeof(AudioClip));
    }

    private static FileHandler CreateFileHandler(string dataPath)
    {
        FileHandler fileHandler = (FileHandler)RuntimeHelpers.GetUninitializedObject(typeof(FileHandler));
        TraverseUtils.SetValue(ref fileHandler, "dataPath", dataPath);
        return fileHandler;
    }

    private static void WritePcmWave(string path)
    {
        using FileStream stream = File.Create(path);
        using BinaryWriter writer = new(stream);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(38U);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16U);
        writer.Write((ushort)1);
        writer.Write((ushort)1);
        writer.Write(8000U);
        writer.Write(16000U);
        writer.Write((ushort)2);
        writer.Write((ushort)16);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(2U);
        writer.Write((short)0);
    }
}
