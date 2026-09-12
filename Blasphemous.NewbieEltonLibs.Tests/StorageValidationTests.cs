using Blasphemous.NewbieEltonLibs.Storage;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Xunit;

namespace Blasphemous.NewbieEltonLibs.Tests;

/// <summary>Verifies shared parameter validation in resource storage.</summary>
public sealed class StorageValidationTests
{
    /// <summary>Verifies strict storage operations reject invalid arguments without logging.</summary>
    [Fact]
    public void StrictOperationsRejectInvalidArgumentsWithoutLogging()
    {
        AnimationStorage animations = new AnimationStorage();
        List<BepInEx.Logging.LogEventArgs> animationLogs = ValidationTests.CaptureUnknownModErrors(() =>
        {
            Assert.IsType<ArgumentException>(Assert.Throws<ArgumentException>(() => animations.Register(null!)));
            Assert.IsType<ArgumentException>(Assert.Throws<ArgumentException>(() => animations.Replace(null!)));
        });

        SpriteStorage sprites = new SpriteStorage();
        List<BepInEx.Logging.LogEventArgs> spriteLogs = ValidationTests.CaptureUnknownModErrors(() =>
        {
            Assert.IsType<ArgumentException>(Assert.Throws<ArgumentException>(() => sprites.Register("", null!)));
            Assert.IsType<ArgumentException>(Assert.Throws<ArgumentException>(() => sprites.Register("hero", null!)));
            Assert.IsType<ArgumentException>(Assert.Throws<ArgumentException>(() => sprites.Replace("", null!)));
            Assert.IsType<ArgumentException>(Assert.Throws<ArgumentException>(() => sprites.Replace("hero", null!)));
        });

        Assert.Empty(animationLogs);
        Assert.Empty(spriteLogs);
    }

    /// <summary>Verifies Try storage operations reject invalid arguments silently.</summary>
    [Fact]
    public void TryOperationsRejectInvalidArgumentsWithoutLogging()
    {
        AnimationStorage animations = new AnimationStorage();
        SpriteStorage sprites = new SpriteStorage();

        List<BepInEx.Logging.LogEventArgs> logs = ValidationTests.CaptureUnknownModErrors(() =>
        {
            Assert.False(animations.TryRegister(null));
            Assert.False(animations.TryReplace(null));
            Assert.False(animations.TryGet("", out _));
            Assert.False(sprites.TryRegister("", null!));
            Assert.False(sprites.TryRegister("hero", null!));
            Assert.False(sprites.TryReplace("", null!));
            Assert.False(sprites.TryReplace("hero", null!));
            Assert.False(sprites.TryGet("", out _));
        });

        Assert.Empty(logs);
    }

    /// <summary>Verifies the default storage path reaches Unity's overloaded null check.</summary>
    [Fact]
    public void DefaultSpriteValidationUsesUnityNullOperator()
    {
        SpriteStorage storage = new SpriteStorage();
        Sprite sprite = CreateSprite();

        Assert.Throws<NullReferenceException>(() => storage.Register("hero", sprite));
    }

    /// <summary>Verifies animation registration and replacement state errors remain distinct.</summary>
    [Fact]
    public void AnimationRegistrationPreservesStateErrors()
    {
        AnimationStorage storage = new AnimationStorage();
        AnimationInfo first = CreateAnimation("idle");
        AnimationInfo replacement = CreateAnimation("idle");

        storage.Register(first);

        Assert.Throws<InvalidOperationException>(() => storage.Register(replacement));
        Assert.Throws<KeyNotFoundException>(() => storage.Replace(CreateAnimation("missing")));
        Assert.False(storage.TryRegister(replacement));
        Assert.False(storage.TryReplace(CreateAnimation("missing")));
        Assert.True(storage.TryReplace(replacement));
        Assert.Same(replacement, storage["idle"]);
    }

    /// <summary>Verifies sprite registration and replacement state errors remain distinct.</summary>
    [Fact]
    public void SpriteRegistrationPreservesStateErrors()
    {
        SpriteStorage storage = new SpriteStorage(value => !ReferenceEquals(value, null));
        Sprite first = CreateSprite();
        Sprite replacement = CreateSprite();

        storage.Register("hero", first);

        Assert.Throws<InvalidOperationException>(() => storage.Register("hero", replacement));
        Assert.Throws<KeyNotFoundException>(() => storage.Replace("missing", replacement));
        Assert.False(storage.TryRegister("hero", replacement));
        Assert.False(storage.TryReplace("missing", replacement));
        Assert.True(storage.TryReplace("hero", replacement));
        Assert.Same(replacement, storage["hero"]);
    }

    private static AnimationInfo CreateAnimation(string name)
    {
        return new AnimationInfo(name, new Sprite[] { null! }, 0.1f);
    }

    private static Sprite CreateSprite()
    {
        return (Sprite)RuntimeHelpers.GetUninitializedObject(typeof(Sprite));
    }
}
