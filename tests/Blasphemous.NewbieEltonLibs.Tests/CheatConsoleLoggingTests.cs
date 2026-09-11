using Gameplay.UI.Widgets;
using Blasphemous.NewbieEltonLibs.CheatConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Xunit;

namespace Blasphemous.NewbieEltonLibs.Tests;

/// <summary>Verifies the cheat-console logging contract.</summary>
public sealed class CheatConsoleLoggingTests
{
    /// <summary>Verifies the public log-level names and configuration defaults.</summary>
    [Fact]
    public void PublicApiExposesExpectedLevelsAndDefaults()
    {
        Assert.Equal(
            new[] { "Info", "Warn", "Error", "Fatal", "Debug", "Display" },
            Enum.GetNames(typeof(LogLevel)));

        AssertConfigurationMethod(nameof(CheatConsoleLogging.LogCheatConsoleInput));
        AssertConfigurationMethod(nameof(CheatConsoleLogging.LogCheatConsoleOutput));
    }

    /// <summary>Verifies the initial state of a log channel.</summary>
    [Fact]
    public void ChannelDefaultsToInfoAndDebugBuildOnly()
    {
        ConsoleLogChannel channel = new ConsoleLogChannel();

        Assert.False(channel.Active);
        Assert.Equal(LogLevel.Info, channel.LogLevel);
        Assert.True(channel.DebugBuildOnly);
        Assert.False(channel.CallerIsDebugBuild);
        Assert.False(channel.ShouldLog());
    }

    /// <summary>Verifies disabled configuration retains the latest settings.</summary>
    [Fact]
    public void DisabledConfigurationStillStoresTheLatestSettings()
    {
        ConsoleLogChannel channel = new ConsoleLogChannel();

        channel.Configure(true, LogLevel.Warn, false, false);
        channel.Configure(false, LogLevel.Fatal, true, true);

        Assert.False(channel.ShouldLog());
        Assert.False(channel.Active);
        Assert.Equal(LogLevel.Fatal, channel.LogLevel);
        Assert.True(channel.DebugBuildOnly);
        Assert.True(channel.CallerIsDebugBuild);
    }

    /// <summary>Verifies input and output channel state remains independent.</summary>
    [Fact]
    public void InputAndOutputChannelsRemainIndependent()
    {
        ConsoleLogChannel input = new ConsoleLogChannel();
        ConsoleLogChannel output = new ConsoleLogChannel();

        input.Configure(true, LogLevel.Info, false, false);
        output.Configure(true, LogLevel.Error, true, false);

        Assert.True(input.ShouldLog());
        Assert.Equal(LogLevel.Info, input.LogLevel);
        Assert.False(output.ShouldLog());
        Assert.Equal(LogLevel.Error, output.LogLevel);
    }

    /// <summary>Verifies enabled logging forwards its level and prefix.</summary>
    [Theory]
    [InlineData(LogLevel.Info)]
    [InlineData(LogLevel.Warn)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Fatal)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Display)]
    public void EnabledChannelUsesConfiguredLevelAndStablePrefix(LogLevel level)
    {
        ConsoleLogChannel channel = new ConsoleLogChannel();
        channel.Configure(true, level, false, false);
        List<KeyValuePair<LogLevel, object>> entries = new List<KeyValuePair<LogLevel, object>>();

        CheatConsoleLogging.LogConfigured(
            channel,
            "[CheatConsole Input] ",
            "help",
            (loggedLevel, message) => entries.Add(new KeyValuePair<LogLevel, object>(loggedLevel, message)));

        KeyValuePair<LogLevel, object> entry = Assert.Single(entries);
        Assert.Equal(level, entry.Key);
        Assert.Equal("[CheatConsole Input] help", entry.Value);
    }

    /// <summary>Verifies debug-only logging checks the caller build configuration.</summary>
    [Fact]
    public void DebugBuildOnlyChannelRequiresDebugCaller()
    {
        ConsoleLogChannel channel = new ConsoleLogChannel();

        channel.Configure(true, LogLevel.Info, true, false);
        Assert.False(channel.ShouldLog());

        channel.Configure(true, LogLevel.Info, true, true);
        Assert.True(channel.ShouldLog());

        channel.Configure(true, LogLevel.Info, false, false);
        Assert.True(channel.ShouldLog());
    }

    /// <summary>Verifies disabled logging does not invoke its sink.</summary>
    [Fact]
    public void DisabledChannelDoesNotInvokeLogSink()
    {
        ConsoleLogChannel channel = new ConsoleLogChannel();
        channel.Configure(false, LogLevel.Info, false, false);
        bool invoked = false;

        CheatConsoleLogging.LogConfigured(
            channel,
            "[CheatConsole Output] ",
            "line",
            (_, __) => invoked = true);

        Assert.False(invoked);
    }

    /// <summary>Verifies output logging uses the output prefix.</summary>
    [Fact]
    public void OutputChannelUsesOutputPrefix()
    {
        ConsoleLogChannel channel = new ConsoleLogChannel();
        channel.Configure(true, LogLevel.Warn, false, false);
        List<KeyValuePair<LogLevel, object>> entries = new List<KeyValuePair<LogLevel, object>>();

        CheatConsoleLogging.LogConfigured(
            channel,
            "[CheatConsole Output] ",
            "line",
            (loggedLevel, message) => entries.Add(new KeyValuePair<LogLevel, object>(loggedLevel, message)));

        KeyValuePair<LogLevel, object> entry = Assert.Single(entries);
        Assert.Equal(LogLevel.Warn, entry.Key);
        Assert.Equal("[CheatConsole Output] line", entry.Value);
    }

    /// <summary>Verifies the patches target the public console seams.</summary>
    [Fact]
    public void PatchesObserveSubmitAndWriteSeams()
    {
        AssertPatchTarget(typeof(ConsoleWidget_Submit_CheatConsoleInput_Patch), "Submit");
        AssertPatchTarget(typeof(ConsoleWidget_Write_CheatConsoleOutput_Patch), "Write");

        MethodInfo postfix = typeof(ConsoleWidget_Write_CheatConsoleOutput_Patch).GetMethod(
            "Postfix",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.Equal("message", postfix.GetParameters()[0].Name);
    }

    /// <summary>Verifies invalid log levels are rejected.</summary>
    [Fact]
    public void InvalidLogLevelIsRejectedBeforeConfiguration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CheatConsoleLogging.LogCheatConsoleInput(false, (LogLevel)123));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CheatConsoleLogging.LogCheatConsoleOutput(false, (LogLevel)123));
    }

    private static void AssertConfigurationMethod(string methodName)
    {
        MethodInfo method = typeof(CheatConsoleLogging).GetMethod(methodName)!;
        ParameterInfo[] parameters = method.GetParameters();

        Assert.Equal(typeof(void), method.ReturnType);
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(bool), parameters[0].ParameterType);
        Assert.Equal(typeof(LogLevel), parameters[1].ParameterType);
        Assert.Equal(typeof(bool), parameters[2].ParameterType);
        Assert.True(parameters[1].HasDefaultValue);
        Assert.Equal(LogLevel.Info, parameters[1].DefaultValue);
        Assert.True(parameters[2].HasDefaultValue);
        Assert.Equal(true, parameters[2].DefaultValue);
    }

    private static void AssertPatchTarget(Type patchType, string methodName)
    {
        CustomAttributeData patch = Assert.Single(
            CustomAttributeData.GetCustomAttributes(patchType)
                .Where(attribute => attribute.AttributeType == typeof(HarmonyPatch)));

        Assert.Equal(typeof(ConsoleWidget), patch.ConstructorArguments[0].Value);
        Assert.Equal(methodName, patch.ConstructorArguments[1].Value);
    }
}
