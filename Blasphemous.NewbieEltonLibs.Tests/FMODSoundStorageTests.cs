using System;
using System.Collections.Generic;
using Blasphemous.NewbieEltonLibs.Storage;
using FMOD;
using Xunit;

namespace Blasphemous.NewbieEltonLibs.Tests;

/// <summary>Verifies named FMOD Sound storage through its public contract.</summary>
public sealed class FMODSoundStorageTests
{
    /// <summary>Verifies strict and Try registration, lookup, and replacement behavior.</summary>
    [Fact]
    public void FMODSoundStorageSupportsStrictAndTryOperations()
    {
        FMODSoundStorage storage = new();
        Sound first = CreateSound(75);
        Sound replacement = CreateSound(76);

        storage.Register("ambience", first);

        Assert.Equal(first.handle, storage["ambience"].handle);
        Assert.False(storage.TryRegister("ambience", replacement));
        Assert.Throws<InvalidOperationException>(() => storage.Register("ambience", replacement));
        Assert.Equal(first.handle, storage["ambience"].handle);
        Assert.False(storage.TryReplace("missing", replacement));
        Assert.Throws<KeyNotFoundException>(() => storage.Replace("missing", replacement));
        Assert.True(storage.TryReplace("ambience", replacement));
        Assert.Equal(replacement.handle, storage["ambience"].handle);
        storage.Replace("ambience", first);
        Assert.Equal(first.handle, storage["ambience"].handle);
        Assert.True(storage.TryGet("ambience", out Sound found));
        Assert.Equal(first.handle, found.handle);
        Assert.False(storage.TryGet("missing", out _));
        Assert.Throws<KeyNotFoundException>(() => _ = storage["missing"]);
        Assert.Equal(new IntPtr(75), first.handle);
    }

    /// <summary>Verifies separate consuming mods retain independent FMOD Sound names.</summary>
    [Fact]
    public void FMODSoundStorageInstancesAreIndependent()
    {
        FMODSoundStorage first = new();
        FMODSoundStorage second = new();

        first.Register("shared-name", CreateSound(77));

        Assert.True(first.TryGet("shared-name", out _));
        Assert.False(second.TryGet("shared-name", out _));
    }

    /// <summary>Verifies invalid names and sounds without handles follow strict/Try validation.</summary>
    [Fact]
    public void FMODSoundStorageRejectsInvalidNamesAndUninitializedSounds()
    {
        FMODSoundStorage storage = new();
        Sound sound = CreateSound(78);

        Assert.Throws<ArgumentException>(() => storage.Register(string.Empty, sound));
        Assert.Throws<ArgumentException>(() => storage.Register("invalid", default));
        Assert.False(storage.TryRegister(string.Empty, sound));
        Assert.False(storage.TryRegister("invalid", default));
        Assert.False(storage.TryReplace("invalid", sound));
        Assert.False(storage.TryReplace("invalid", default));
        Assert.False(storage.TryGet(string.Empty, out _));

        storage.Register("valid", sound);
        Assert.Throws<ArgumentException>(() => storage.Replace("valid", default));
    }

    private static Sound CreateSound(int handle)
    {
        return new Sound { handle = new IntPtr(handle) };
    }
}
