using Blasphemous.ModdingAPI.Files;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
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
        Assert.False(string.IsNullOrEmpty(failure));
        Assert.False(storage.TryGet("unsupported", out _));
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

    /// <summary>Verifies an in-flight import reserves its name until the terminal callback.</summary>
    [Fact]
    public void AudioClipImportReservesNameUntilItCompletes()
    {
        AudioClipStorage storage = CreateStorage();
        FileHandler fileHandler = CreateFileHandler(AppContext.BaseDirectory);
        int firstFailureCount = 0;
        int secondFailureCount = 0;
        string? firstFailureName = null;
        string? secondFailureName = null;

        IEnumerator firstImport = storage.ImportAsync(fileHandler, "pending", "../outside.mp3",
            (_, _) => { }, (name, _) =>
            {
                firstFailureCount++;
                firstFailureName = name;
            });
        Assert.False(storage.TryRegister("pending", CreateAudioClip()));

        IEnumerator secondImport = storage.ImportAsync(fileHandler, "pending", "../outside.mp3",
            (_, _) => { }, (name, _) =>
            {
                secondFailureCount++;
                secondFailureName = name;
            });

        Assert.False(firstImport.MoveNext());
        Assert.False(secondImport.MoveNext());
        Assert.Equal(1, firstFailureCount);
        Assert.Equal(1, secondFailureCount);
        Assert.Equal("pending", firstFailureName);
        Assert.Equal("pending", secondFailureName);
        Assert.True(storage.TryRegister("pending", CreateAudioClip()));
    }

    private static AudioClipStorage CreateStorage()
    {
        return new AudioClipStorage(clip => clip is not null);
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
}
