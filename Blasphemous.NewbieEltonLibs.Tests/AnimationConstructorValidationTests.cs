using BepInEx.Logging;
using Blasphemous.NewbieEltonLibs.Storage;
using System;
using System.Collections.Generic;
using UnityEngine;
using Xunit;

namespace Blasphemous.NewbieEltonLibs.Tests;

/// <summary>Verifies aggregate validation for animation description constructors.</summary>
public sealed class AnimationConstructorValidationTests
{
    /// <summary>Verifies import failures are reported in constructor order.</summary>
    [Fact]
    public void ImportConstructorAggregatesIndependentFailuresInOrder()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new AnimationImportInfo(" \t", " \r\n", 0, -1, float.NaN));

        Assert.Equal(
            "An animation name cannot be whitespace." + Environment.NewLine
            + "An animation file path cannot be whitespace." + Environment.NewLine
            + "Frame width must be positive." + Environment.NewLine
            + "Frame height must be positive." + Environment.NewLine
            + "Frame duration must be positive.",
            exception.Message);
    }

    /// <summary>Verifies animation failures include every invalid sprite index before duration.</summary>
    [Fact]
    public void AnimationConstructorAggregatesSpriteAndDurationFailuresInOrder()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new AnimationInfo(" \t", new Sprite[] { null!, null! }, float.NaN));

        Assert.Equal(
            "An animation name cannot be whitespace." + Environment.NewLine
            + "An animation sprite at index 0 cannot be null." + Environment.NewLine
            + "An animation sprite at index 1 cannot be null." + Environment.NewLine
            + "Frame duration must be positive.",
            exception.Message);
    }

    /// <summary>Verifies null animation inputs do not stop later validation.</summary>
    [Fact]
    public void AnimationConstructorReportsNullInputsAndContinues()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new AnimationInfo(null!, null!, 0));

        Assert.Equal(
            "An animation name cannot be null." + Environment.NewLine
            + "Animation sprites cannot be null." + Environment.NewLine
            + "Frame duration must be positive.",
            exception.Message);
    }

    /// <summary>Verifies empty and whitespace string reports remain mutually exclusive.</summary>
    [Fact]
    public void ConstructorsDistinguishEmptyAndWhitespaceStrings()
    {
        ArgumentException emptyAnimation = Assert.Throws<ArgumentException>(() =>
            new AnimationInfo("", new Sprite[0], 0));
        Assert.Equal(
            "An animation name cannot be empty." + Environment.NewLine
            + "An animation must contain at least one frame." + Environment.NewLine
            + "Frame duration must be positive.",
            emptyAnimation.Message);

        ArgumentException emptyImport = Assert.Throws<ArgumentException>(() =>
            new AnimationImportInfo("", "", 0, 0, 0));
        Assert.Equal(
            "An animation name cannot be empty." + Environment.NewLine
            + "An animation file path cannot be empty." + Environment.NewLine
            + "Frame width must be positive." + Environment.NewLine
            + "Frame height must be positive." + Environment.NewLine
            + "Frame duration must be positive.",
            emptyImport.Message);
    }

    /// <summary>Verifies import null inputs do not stop later validation.</summary>
    [Fact]
    public void ImportConstructorReportsNullInputsAndContinues()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new AnimationImportInfo(null!, null!, 0, 0, float.NaN));

        Assert.Equal(
            "An animation name cannot be null." + Environment.NewLine
            + "An animation file path cannot be null." + Environment.NewLine
            + "Frame width must be positive." + Environment.NewLine
            + "Frame height must be positive." + Environment.NewLine
            + "Frame duration must be positive.",
            exception.Message);
    }

    /// <summary>Verifies constructor validation remains silent in ModLog.</summary>
    [Fact]
    public void ConstructorValidationDoesNotLogFailures()
    {
        List<LogEventArgs> logs = ValidationTests.CaptureUnknownModErrors(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                new AnimationInfo(" ", new Sprite[] { null! }, -1));
            Assert.Throws<ArgumentException>(() =>
                new AnimationImportInfo(" ", " ", -1, -1, -1));
        });

        Assert.Empty(logs);
    }

    /// <summary>Verifies successful import construction preserves inputs and accepts positive infinity.</summary>
    [Fact]
    public void SuccessfulImportConstructorPreservesInputsAndAcceptsPositiveInfinity()
    {
        string name = new string(new[] { ' ', 'i', 'm', 'p', 'o', 'r', 't', ' ' });
        string filePath = new string(new[] { ' ', 'a', '.', 'p', 'n', 'g', ' ' });
        AnimationImportInfo import = new AnimationImportInfo(name, filePath, 16, 32, float.PositiveInfinity);
        Assert.Same(name, import.Name);
        Assert.Same(filePath, import.FilePath);
        Assert.Equal(16, import.Width);
        Assert.Equal(32, import.Height);
        Assert.Equal(float.PositiveInfinity, import.SecondsPerFrame);
    }
}
