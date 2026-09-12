using Blasphemous.Framework.Levels;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using Blasphemous.NewbieEltonLibs.Storage;
using System;
using System.Collections.Generic;
using UnityEngine;
using Xunit;

namespace Blasphemous.NewbieEltonLibs.Tests;

/// <summary>Verifies public runtime resource and animation contracts.</summary>
public sealed class StorageCoverageTests
{
    /// <summary>Verifies valid animation data retains its public inputs.</summary>
    [Fact]
    public void AnimationInfoRetainsFramesAndTiming()
    {
        Sprite[] sprites = { null!, null! };

        AnimationInfo animation = new AnimationInfo("idle", sprites, 0.25f);

        Assert.Equal("idle", animation.Name);
        Assert.Same(sprites, animation.Sprites);
        Assert.Equal(0.25f, animation.SecondsPerFrame);
    }

    /// <summary>Verifies animation descriptions reject invalid public inputs.</summary>
    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    public void AnimationInfoRejectsInvalidDuration(float secondsPerFrame)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AnimationInfo("idle", new Sprite[] { null! }, secondsPerFrame));
    }

    /// <summary>Verifies animation descriptions reject missing names and frames.</summary>
    [Fact]
    public void AnimationInfoRejectsMissingNameAndFrames()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AnimationInfo(null!, Array.Empty<Sprite>(), 1f));
        Assert.Throws<ArgumentException>(() =>
            new AnimationInfo(string.Empty, Array.Empty<Sprite>(), 1f));
        Assert.Throws<ArgumentNullException>(() =>
            new AnimationInfo("idle", null!, 1f));
        Assert.Throws<ArgumentException>(() =>
            new AnimationInfo("idle", Array.Empty<Sprite>(), 1f));
    }

    /// <summary>Verifies animation import data exposes its validated values.</summary>
    [Fact]
    public void AnimationImportInfoRetainsValues()
    {
        AnimationImportInfo import = new AnimationImportInfo("idle", "sprites/idle.png", 16, 24, 0.2f);

        Assert.Equal("idle", import.Name);
        Assert.Equal("sprites/idle.png", import.FilePath);
        Assert.Equal(16, import.Width);
        Assert.Equal(24, import.Height);
        Assert.Equal(0.2f, import.SecondsPerFrame);
    }

    /// <summary>Verifies animation import data rejects invalid metadata.</summary>
    [Fact]
    public void AnimationImportInfoRejectsInvalidMetadata()
    {
        Assert.Throws<ArgumentNullException>(() => new AnimationImportInfo(null!, "path", 1, 1, 1f));
        Assert.Throws<ArgumentException>(() => new AnimationImportInfo(string.Empty, "path", 1, 1, 1f));
        Assert.Throws<ArgumentNullException>(() => new AnimationImportInfo("name", null!, 1, 1, 1f));
        Assert.Throws<ArgumentException>(() => new AnimationImportInfo("name", string.Empty, 1, 1, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationImportInfo("name", "path", 0, 1, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationImportInfo("name", "path", 1, 0, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationImportInfo("name", "path", 1, 1, 0f));
    }

    /// <summary>Verifies sprite import defaults remain stable and caller-owned.</summary>
    [Fact]
    public void SpriteImportInfoUsesDocumentedDefaults()
    {
        SpriteImportInfo import = new SpriteImportInfo();

        Assert.Equal(string.Empty, import.Name);
        Assert.Equal(32, import.PixelsPerUnit);
        Assert.Equal(new Vector(0.5f, 0.5f, 0.5f), import.Pivot);

        import.Name = "portrait";
        import.PixelsPerUnit = 64;
        import.Pivot = new Vector(0.25f, 0.75f, 0.5f);

        Assert.Equal("portrait", import.Name);
        Assert.Equal(64, import.PixelsPerUnit);
        Assert.Equal(new Vector(0.25f, 0.75f, 0.5f), import.Pivot);
    }

    /// <summary>Verifies animation storage registration and replacement semantics.</summary>
    [Fact]
    public void AnimationStorageSupportsRegisterReplaceAndTryOperations()
    {
        AnimationStorage storage = new AnimationStorage();
        AnimationInfo first = CreateAnimation("idle");
        AnimationInfo replacement = CreateAnimation("idle");
        AnimationInfo other = CreateAnimation("walk");

        Assert.True(storage.TryRegister(first));
        Assert.False(storage.TryRegister(first));
        Assert.Same(first, storage["idle"]);
        Assert.True(storage.TryGet("idle", out AnimationInfo? found));
        Assert.Same(first, found);
        Assert.False(storage.TryGet("missing", out _));
        Assert.False(storage.TryReplace(other));
        Assert.True(storage.TryReplace(replacement));
        Assert.Same(replacement, storage["idle"]);
        Assert.Throws<InvalidOperationException>(() => storage.Register(replacement));
        Assert.Throws<KeyNotFoundException>(() => storage.Replace(other));
        Assert.Throws<KeyNotFoundException>(() => _ = storage["missing"]);
        Assert.Throws<ArgumentNullException>(() => storage.Register(null!));
        Assert.Throws<ArgumentNullException>(() => storage.Replace(null!));
        Assert.False(storage.TryRegister(null));
        Assert.False(storage.TryReplace(null));
    }

    /// <summary>Verifies separate animation stores do not share state.</summary>
    [Fact]
    public void AnimationStorageInstancesAreIndependent()
    {
        AnimationStorage first = new AnimationStorage();
        AnimationStorage second = new AnimationStorage();

        first.Register(CreateAnimation("shared-name"));

        Assert.True(first.TryGet("shared-name", out _));
        Assert.False(second.TryGet("shared-name", out _));
    }

    /// <summary>Verifies sprite storage exposes its public registration contract.</summary>
    [Fact]
    public void SpriteStorageExposesRegistrationOperations()
    {
        SpriteStorage storage = new SpriteStorage();
        Assert.NotNull(typeof(SpriteStorage).GetMethod(nameof(SpriteStorage.Register)));
        Assert.NotNull(typeof(SpriteStorage).GetMethod(nameof(SpriteStorage.Replace)));
        Assert.NotNull(typeof(SpriteStorage).GetMethod(nameof(SpriteStorage.TryRegister)));
        Assert.NotNull(typeof(SpriteStorage).GetMethod(nameof(SpriteStorage.TryReplace)));
        Assert.NotNull(typeof(SpriteStorage).GetMethod(nameof(SpriteStorage.TryGet)));
        Assert.False(storage.TryGet("missing", out _));
        Assert.Throws<KeyNotFoundException>(() => _ = storage["missing"]);
        Assert.False(storage.TryRegister(string.Empty, null!));
        Assert.False(storage.TryReplace(string.Empty, null!));
        Assert.Throws<ArgumentNullException>(() => storage.Register(null!, null!));
    }

    /// <summary>Verifies separate sprite stores do not share state.</summary>
    [Fact]
    public void SpriteStorageInstancesStartIndependent()
    {
        SpriteStorage first = new SpriteStorage();
        SpriteStorage second = new SpriteStorage();

        Assert.False(first.TryGet("shared-name", out _));
        Assert.False(second.TryGet("shared-name", out _));
    }

    private static AnimationInfo CreateAnimation(string name)
    {
        return new AnimationInfo(name, new Sprite[] { null! }, 0.1f);
    }
}
