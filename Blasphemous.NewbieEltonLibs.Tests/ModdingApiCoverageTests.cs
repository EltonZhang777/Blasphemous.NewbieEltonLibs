using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Config;
using Blasphemous.ModdingAPI.Files;
using Blasphemous.ModdingAPI.Input;
using Blasphemous.ModdingAPI.Localization;
using Blasphemous.CheatConsole;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;
using Gameplay.UI.Widgets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using Xunit;

namespace Blasphemous.NewbieEltonLibs.Tests;

/// <summary>Verifies deterministic ModdingAPI extensions and consumer signatures.</summary>
public sealed class ModdingApiCoverageTests
{
    /// <summary>Verifies file path, JSON, and content helpers use the configured paths.</summary>
    [Fact]
    public void FileHandlerExtensionsRoundTripJson()
    {
        string root = Path.Combine(Path.GetTempPath(), "newbie-elton-" + Guid.NewGuid().ToString("N"));
        string dataPath = Path.Combine(root, "data") + Path.DirectorySeparatorChar;
        string contentPath = Path.Combine(root, "content") + Path.DirectorySeparatorChar;
        string configPath = Path.Combine(root, "config.json");
        Directory.CreateDirectory(dataPath);
        Directory.CreateDirectory(contentPath);

        try
        {
            FileHandler fileHandler = CreateFileHandler(dataPath, contentPath, configPath);
            File.WriteAllText(Path.Combine(dataPath, "data.json"), "{\"Value\":7}");
            File.WriteAllText(Path.Combine(dataPath, "notes.txt"), "notes");

            Assert.Equal(dataPath, fileHandler.GetDataPath());
            Assert.Equal(configPath, fileHandler.GetConfigPath());
            Assert.Contains("data.json", fileHandler.GetAllDataFileNames());
            Assert.Equal(7, fileHandler.LoadDataAsJson<Payload>("data.json").Value);
            Assert.Equal(7, fileHandler.LoadDataAsJson<Payload>("data.json", new JsonSerializerSettings()).Value);

            File.WriteAllText(Path.Combine(contentPath, "input.json"), "{\"Value\":8}");
            Assert.True(fileHandler.LoadContentAsJson("input.json", out Payload? loaded));
            Assert.Equal(8, loaded!.Value);

            fileHandler.WriteJsonToContent("default-output.json", new Payload { Value = 10 });
            Assert.Equal(10, JsonConvert.DeserializeObject<Payload>(
                File.ReadAllText(Path.Combine(contentPath, "default-output.json")))!.Value);

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            fileHandler.WriteJsonToContent("output.json", new Payload { Value = 9 }, settings, Formatting.None);
            Payload output = JsonConvert.DeserializeObject<Payload>(
                File.ReadAllText(Path.Combine(contentPath, "output.json")))!;
            Assert.Equal(9, output.Value);
            Assert.DoesNotContain("Name", File.ReadAllText(Path.Combine(contentPath, "output.json")));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    /// <summary>Verifies config extensions persist and reload a consumer model.</summary>
    [Fact]
    public void ConfigHandlerExtensionsRoundTripJson()
    {
        string root = Path.Combine(Path.GetTempPath(), "newbie-elton-config-" + Guid.NewGuid().ToString("N"));
        string configPath = Path.Combine(root, "config.json");
        Directory.CreateDirectory(root);

        try
        {
            FileHandler fileHandler = CreateFileHandler(string.Empty, string.Empty, configPath);
            TestMod mod = (TestMod)RuntimeHelpers.GetUninitializedObject(typeof(TestMod));
            TraverseUtils.SetValue(ref mod, "_fileHandler", fileHandler);
            ConfigHandler configHandler = (ConfigHandler)RuntimeHelpers.GetUninitializedObject(typeof(ConfigHandler));
            TraverseUtils.SetValue(ref configHandler, "_mod", mod);

            Payload defaults = configHandler.Load<Payload>();
            Assert.Equal(0, defaults.Value);
            Assert.True(File.Exists(configPath));

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            configHandler.Save(new Payload { Value = 11 }, Formatting.None, settings);

            Assert.True(File.Exists(configPath));
            Assert.DoesNotContain("Name", File.ReadAllText(configPath));
            Assert.Equal(11, configHandler.Load<Payload>().Value);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    /// <summary>Verifies keybinding lookup exposes the configured external map.</summary>
    [Fact]
    public void InputHandlerExtensionsReadKeybindings()
    {
        InputHandler inputHandler = (InputHandler)RuntimeHelpers.GetUninitializedObject(typeof(InputHandler));
        Dictionary<string, KeyCode> keybindings = new Dictionary<string, KeyCode>
        {
            ["jump"] = KeyCode.Space
        };
        TraverseUtils.SetValue(ref inputHandler, "_keybindings", keybindings);

        Assert.Same(keybindings, inputHandler.GetAllKeybindings());
        Assert.True(inputHandler.TryGetKeybinding("jump", out KeyCode keyCode));
        Assert.Equal(KeyCode.Space, keyCode);
        Assert.False(inputHandler.TryGetKeybinding("missing", out _));
    }

    /// <summary>Verifies command extension access and valid parameter counts.</summary>
    [Fact]
    public void ModCommandExtensionsUseTheAssociatedConsole()
    {
        TestCommand command = (TestCommand)RuntimeHelpers.GetUninitializedObject(typeof(TestCommand));
        ConsoleWidget console = (ConsoleWidget)RuntimeHelpers.GetUninitializedObject(typeof(ConsoleWidget));
        TraverseUtils.SetValue(ref command, "console", console);

        Assert.Same(console, command.GetConsoleWidget());
        Assert.True(command.ValidateParameterList(new[] { "value" }, 1));
    }

    /// <summary>Verifies live framework extensions are represented by public signatures.</summary>
    [Fact]
    public void ModdingApiExtensionsExposeExpectedMethods()
    {
        Assert.NotNull(typeof(ConfigHandlerExtensions).GetMethod(nameof(ConfigHandlerExtensions.Load)));
        Assert.NotNull(typeof(ConfigHandlerExtensions).GetMethod(nameof(ConfigHandlerExtensions.Save)));
        Assert.NotNull(typeof(FileHandlerExtensions).GetMethod(nameof(FileHandlerExtensions.GetDataPath)));
        Assert.NotNull(typeof(FileHandlerExtensions).GetMethod(nameof(FileHandlerExtensions.GetConfigPath)));
        Assert.NotNull(typeof(FileHandlerExtensions).GetMethod(nameof(FileHandlerExtensions.GetAllDataFileNames)));
        Assert.Contains(typeof(FileHandlerExtensions).GetMethods(), method => method.Name == nameof(FileHandlerExtensions.LoadDataAsJson));
        Assert.NotNull(typeof(FileHandlerExtensions).GetMethod(nameof(FileHandlerExtensions.LoadContentAsJson)));
        Assert.Contains(typeof(FileHandlerExtensions).GetMethods(), method => method.Name == nameof(FileHandlerExtensions.WriteJsonToContent));
        Assert.NotNull(typeof(FileHandlerExtensions).GetMethod(nameof(FileHandlerExtensions.LoadDataAsAssetBundle)));
        Assert.NotNull(typeof(InputHandlerExtensions).GetMethod(nameof(InputHandlerExtensions.GetAxisDown)));
        Assert.NotNull(typeof(InputHandlerExtensions).GetMethod(nameof(InputHandlerExtensions.GetAllKeybindings)));
        Assert.NotNull(typeof(LocalizationHandlerExtensions).GetMethod(nameof(LocalizationHandlerExtensions.Localize)));
        Assert.NotNull(typeof(ModCommandExtensions).GetMethod(nameof(ModCommandExtensions.GetConsoleWidget)));
        Assert.NotNull(typeof(ModCommandExtensions).GetMethod(nameof(ModCommandExtensions.ValidateParameterList)));
        Assert.Contains(typeof(ModLogExtensions).GetMethods(), method => method.Name == nameof(ModLogExtensions.InfoIfDebugBuild));
        Assert.Contains(typeof(ModLogExtensions).GetMethods(), method => method.Name == nameof(ModLogExtensions.DisplayIfDebugBuild));
    }

    private static FileHandler CreateFileHandler(string dataPath, string contentPath, string configPath)
    {
        FileHandler fileHandler = (FileHandler)RuntimeHelpers.GetUninitializedObject(typeof(FileHandler));
        TraverseUtils.SetValue(ref fileHandler, "dataPath", dataPath);
        TraverseUtils.SetValue(ref fileHandler, "contentPath", contentPath);
        TraverseUtils.SetValue(ref fileHandler, "configPath", configPath);
        return fileHandler;
    }

    private sealed class Payload
    {
        /// <summary>Gets or sets the payload value used by the JSON round trip.</summary>
        public int Value { get; set; }

        /// <summary>Gets or sets an optional payload name used by serializer-setting checks.</summary>
        public string? Name { get; set; }
    }

    private sealed class TestMod : BlasMod
    {
        private TestMod() : base("coverage", "coverage", "coverage", "1.0")
        {
        }
    }

    private sealed class TestCommand : ModCommand
    {
        protected override string CommandName => "coverage";

        protected override bool AllowUppercase => true;

        protected override Dictionary<string, Action<string[]>> AddSubCommands()
        {
            return new Dictionary<string, Action<string[]>>();
        }
    }
}
