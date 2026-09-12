using Blasphemous.CheatConsole;
using Blasphemous.NewbieEltonLibs.CheatConsole;
using Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;
using System;
using System.Collections.Generic;
using Xunit;

namespace Blasphemous.NewbieEltonLibs.Tests;

/// <summary>Verifies the public declarative command surface.</summary>
public sealed class CheatConsoleCommandCoverageTests
{
    /// <summary>Verifies attribute values, aliases, and automatic command registration.</summary>
    [Fact]
    public void AttributeCommandsRegisterAliasesAndCustomCommands()
    {
        CommandUnderTest command = new CommandUnderTest();
        Dictionary<string, Action<string[]>> commands = command.Build();

        Assert.True(command.UppercaseAllowed);
        Assert.Contains("help", commands.Keys);
        Assert.Contains("alpha", commands.Keys);
        Assert.Contains("a", commands.Keys);
        Assert.Contains("custom", commands.Keys);
        Assert.DoesNotContain("ALPHA", commands.Keys);

        string[] parameters = { "value" };
        commands["alpha"](parameters);
        Assert.Same(parameters, command.LastParameters);
        commands["a"](parameters);
        Assert.Same(parameters, command.LastParameters);
        commands["custom"](Array.Empty<string>());
        Assert.True(command.CustomCommandCalled);
    }

    /// <summary>Verifies valid parameter counts reach the declared handler.</summary>
    [Fact]
    public void DeclaredParameterLengthsAllowMatchingCalls()
    {
        CommandUnderTest command = new CommandUnderTest();
        Dictionary<string, Action<string[]>> commands = command.Build();

        string[] parameters = { "one", "two" };
        commands["counted"](parameters);

        Assert.Same(parameters, command.LastParameters);
    }

    /// <summary>Verifies malformed command declarations fail before registration.</summary>
    [Fact]
    public void MalformedCommandDeclarationsFailFast()
    {
        Assert.Throws<InvalidOperationException>(() => new StaticHandlerCommand().Build());
        Assert.Throws<InvalidOperationException>(() => new ReturnValueCommand().Build());
        Assert.Throws<InvalidOperationException>(() => new WrongParameterCommand().Build());
        Assert.Throws<InvalidOperationException>(() => new DuplicateNameCommand().Build());
    }

    /// <summary>Verifies attribute instances expose their documented values.</summary>
    [Fact]
    public void ModSubCommandAttributeExposesValues()
    {
        ModSubCommandAttribute attribute = new ModSubCommandAttribute(
            "inspect",
            "inspect values",
            "[name]",
            0,
            2);

        Assert.Equal("inspect", attribute.Name);
        Assert.Equal("inspect values", attribute.Description);
        Assert.Equal("[name]", attribute.Usage);
        Assert.Equal(new[] { 0, 2 }, attribute.ValidLengths);

        ModSubCommandAttribute fallback = new ModSubCommandAttribute("fallback", "fallback command");
        Assert.Null(fallback.Usage);
    }

    /// <summary>Verifies the extension validation accepts a declared length.</summary>
    [Fact]
    public void ValidateParameterListAcceptsMatchingLength()
    {
        CommandUnderTest command = new CommandUnderTest();

        Assert.True(command.ValidateParameters(new[] { "one" }, 1));
    }

    private sealed class CommandUnderTest : AutoModCommand
    {
        internal string[]? LastParameters { get; set; }

        internal bool CustomCommandCalled { get; set; }

        internal bool UppercaseAllowed => AllowUppercase;

        internal Dictionary<string, Action<string[]>> Build()
        {
            return AddSubCommands();
        }

        internal bool ValidateParameters(string[] parameters, params int[] lengths)
        {
            return this.ValidateParameterList(parameters, lengths);
        }

        protected override string CommandName => "coverage";

        [ModSubCommand("alpha", "alpha command", "[value]")]
        [ModSubCommand("a", "alpha alias")]
        private void Alpha(string[] parameters)
        {
            LastParameters = parameters;
        }

        [ModSubCommand("counted", "counted command", validLengths: new[] { 2 })]
        private void Counted(string[] parameters)
        {
            LastParameters = parameters;
        }

        protected override Dictionary<string, Action<string[]>> AddCustomSubCommands()
        {
            return new Dictionary<string, Action<string[]>>
            {
                ["custom"] = _ => CustomCommandCalled = true
            };
        }
    }

    private sealed class StaticHandlerCommand : AutoModCommand
    {
        protected override string CommandName => "static";

        internal Dictionary<string, Action<string[]>> Build() => AddSubCommands();

        [ModSubCommand("invalid", "invalid")]
        private static void Invalid(string[] parameters)
        {
        }
    }

    private sealed class ReturnValueCommand : AutoModCommand
    {
        protected override string CommandName => "return";

        internal Dictionary<string, Action<string[]>> Build() => AddSubCommands();

        [ModSubCommand("invalid", "invalid")]
        private int Invalid(string[] parameters) => 0;
    }

    private sealed class WrongParameterCommand : AutoModCommand
    {
        protected override string CommandName => "parameters";

        internal Dictionary<string, Action<string[]>> Build() => AddSubCommands();

        [ModSubCommand("invalid", "invalid")]
        private void Invalid(int parameter)
        {
        }
    }

    private sealed class DuplicateNameCommand : AutoModCommand
    {
        protected override string CommandName => "duplicate";

        internal Dictionary<string, Action<string[]>> Build() => AddSubCommands();

        [ModSubCommand("same", "first")]
        private void First(string[] parameters)
        {
        }

        [ModSubCommand("same", "second")]
        private void Second(string[] parameters)
        {
        }
    }
}
