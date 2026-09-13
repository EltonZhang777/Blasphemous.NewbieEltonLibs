using BepInEx.Logging;
using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Helpers;
using Blasphemous.NewbieEltonLibs.Components;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;
using Blasphemous.NewbieEltonLibs.Serialization;
using Framework.FrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Xunit;

namespace Blasphemous.NewbieEltonLibs.Tests;

/// <summary>Verifies external-consumer smoke scenarios through xUnit discovery.</summary>
public sealed class ExternalConsumerSmokeTests
{
    /// <summary>Verifies external consumers can use serializable vectors and their JSON representation.</summary>
    [Fact]
    public void SerializableVector3IsUsableByExternalConsumers()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        try
        {
            SerializableVector3 value = new(1.5f, -2.25f, 3f);

            Assert.Equal(1.5f, value.X);
            Assert.Equal(-2.25f, value.Y);
            Assert.Equal(3f, value.Z);
            Assert.Equal("(1.5, -2.25, 3)", value.ToString());

            Vector2 vector2 = default;
            vector2.x = 1.5f;
            vector2.y = -2.25f;
            SerializableVector3 fromVector2 = vector2;
            Assert.Equal(new SerializableVector3(1.5f, -2.25f, 0f), fromVector2);

            Vector3 vector3 = default;
            vector3.x = 1.5f;
            vector3.y = -2.25f;
            vector3.z = 3f;
            SerializableVector3 fromVector3 = vector3;
            Assert.Equal(value, fromVector3);

            Assert.Equal(SerializableVector3.Zero, new SerializableVector3(0f, 0f, 0f));
            Assert.Equal(SerializableVector3.One, new SerializableVector3(1f, 1f, 1f));

            JObject json = JObject.Parse(JsonConvert.SerializeObject(value));
            Assert.Equal(3, json.Count);
            Assert.Equal(1.5f, (float)json["X"]!);
            Assert.Equal(-2.25f, (float)json["Y"]!);
            Assert.Equal(3f, (float)json["Z"]!);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    /// <summary>Verifies external consumers can enumerate matching item fields.</summary>
    [Fact]
    public void ItemCollectionIsUsableByExternalConsumers()
    {
        DerivedItems items = new();
        IEnumerable<string> values = items.Items;

        items.DerivedItem = "updated";
        List<string> enumerated = [.. values];

        Assert.Contains("base", enumerated);
        Assert.Contains("updated", enumerated);
        Assert.Contains("static", enumerated);
        Assert.Equal(1, enumerated.Count(value => value == null));
        Assert.Equal(4, enumerated.Count);
    }

    /// <summary>Verifies unsupported entity orientations fail through the public extension.</summary>
    [Fact]
    public void EntityOrientationConversionRejectsUnsupportedValues()
    {
        Assert.ThrowsAny<Exception>(() => ((EntityOrientation)int.MaxValue).ToDirectionalVector());
    }

    /// <summary>Verifies external consumers can configure and use the Unity-object JSON converter.</summary>
    [Fact]
    public void UnityEngineIgnoreConverterIsUsableByExternalConsumers()
    {
        UnityEngineIgnoreConverter defaultConverter = new();
        Type[] defaultTypes =
        [
            typeof(GameObject),
            typeof(Transform),
            typeof(Texture),
            typeof(Sprite),
            typeof(UnityEngine.UI.Image),
            typeof(Material)
        ];

        foreach (Type defaultType in defaultTypes)
            Assert.True(defaultConverter.CanConvert(defaultType));

        Assert.False(defaultConverter.CanConvert(typeof(Texture2D)));

        UnityEngineIgnoreConverter defaultBooleanConverter = new(false);
        foreach (Type defaultType in defaultTypes)
            Assert.True(defaultBooleanConverter.CanConvert(defaultType));

        Assert.False(defaultBooleanConverter.CanConvert(typeof(Texture2D)));

        UnityEngineIgnoreConverter allUnityObjectsConverter = new(true);
        Assert.True(allUnityObjectsConverter.CanConvert(typeof(UnityEngine.Object)));
        Assert.True(allUnityObjectsConverter.CanConvert(typeof(GameObject)));
        Assert.True(allUnityObjectsConverter.CanConvert(typeof(Texture2D)));
        Assert.False(allUnityObjectsConverter.CanConvert(typeof(string)));

        List<Type> customTypes = [typeof(Texture), typeof(string)];
        UnityEngineIgnoreConverter customConverter = new(customTypes);
        customTypes.Clear();
        Assert.True(customConverter.CanConvert(typeof(Texture)));
        Assert.True(customConverter.CanConvert(typeof(Texture2D)));
        Assert.False(customConverter.CanConvert(typeof(GameObject)));
        Assert.False(customConverter.CanConvert(typeof(string)));

        StringWriter output = new();
        JsonTextWriter writer = new(output);
        defaultConverter.WriteJson(writer, null!, new JsonSerializer());
        writer.Flush();
        Assert.Equal("null", output.ToString());

        JsonTextReader reader = new(new StringReader("null"));
        Assert.True(reader.Read());
        Assert.Null(defaultConverter.ReadJson(reader, typeof(Texture), null, new JsonSerializer()));
    }

    /// <summary>Verifies external consumers receive independent serializer setting presets.</summary>
    [Fact]
    public void JsonSerializerSettingsFactoryIsUsableByExternalConsumers()
    {
        JsonSerializerSettings statsPatchSettings = JsonSerializerSettingsFactory.CreateStatsPatchSettings();
        Assert.Equal(ReferenceLoopHandling.Ignore, statsPatchSettings.ReferenceLoopHandling);
        Assert.Equal(PreserveReferencesHandling.None, statsPatchSettings.PreserveReferencesHandling);
        Assert.Equal(TypeNameHandling.Objects, statsPatchSettings.TypeNameHandling);
        Assert.Contains(statsPatchSettings.Converters, converter => converter is StringEnumConverter);

        JsonSerializerSettings inventoryExportSettings = JsonSerializerSettingsFactory.CreateInventoryExportSettings();
        Assert.Contains(inventoryExportSettings.Converters, converter => converter is UnityEngineIgnoreConverter);
        Assert.Equal(TypeNameHandling.Objects, inventoryExportSettings.TypeNameHandling);

        JsonSerializerSettings penitentDataSettings = JsonSerializerSettingsFactory.CreatePenitentDataSettings();
        Assert.Contains(penitentDataSettings.Converters, converter => converter is StringEnumConverter);
        Assert.Equal(TypeNameHandling.None, penitentDataSettings.TypeNameHandling);
        Assert.Equal(ReferenceLoopHandling.Error, penitentDataSettings.ReferenceLoopHandling);

        JsonSerializerSettings comparisonSettings = JsonSerializerSettingsFactory.CreateInventoryComparisonSettings();
        Assert.Contains(comparisonSettings.Converters, converter => converter is StringEnumConverter);
        Assert.Equal(ReferenceLoopHandling.Ignore, comparisonSettings.ReferenceLoopHandling);
        Assert.Equal(PreserveReferencesHandling.None, comparisonSettings.PreserveReferencesHandling);
        Assert.Equal(TypeNameHandling.None, comparisonSettings.TypeNameHandling);

        statsPatchSettings.TypeNameHandling = TypeNameHandling.None;
        Assert.Equal(TypeNameHandling.Objects, JsonSerializerSettingsFactory.CreateStatsPatchSettings().TypeNameHandling);

        inventoryExportSettings.Converters.Clear();
        Assert.Contains(JsonSerializerSettingsFactory.CreateInventoryExportSettings().Converters, converter => converter is UnityEngineIgnoreConverter);

        penitentDataSettings.TypeNameHandling = TypeNameHandling.Objects;
        Assert.Equal(TypeNameHandling.None, JsonSerializerSettingsFactory.CreatePenitentDataSettings().TypeNameHandling);

        comparisonSettings.ReferenceLoopHandling = ReferenceLoopHandling.Error;
        Assert.Equal(ReferenceLoopHandling.Ignore, JsonSerializerSettingsFactory.CreateInventoryComparisonSettings().ReferenceLoopHandling);
    }

    /// <summary>Verifies ModLog extensions attribute external-consumer logs to the owning mod.</summary>
    [Fact]
    public void ModLogExtensionsPreserveExternalConsumerOwnership()
    {
        SmokeMod mod = CreateRegisteredSmokeMod();
        ManualLogSource modLogger = FindLogSource(mod.Name);
        List<LogEventArgs> modEvents = [];
        void modHandler(object? _, LogEventArgs logEvent) => modEvents.Add(logEvent);
        modLogger.LogEvent += modHandler;

        try
        {
            NestedConsumerComponent.EmitParameterlessLogs();
            NestedConsumerComponent.EmitExplicitLogs(mod);

            bool isDebugBuild = ((DebuggableAttribute?)Attribute.GetCustomAttribute(
                Assembly.GetExecutingAssembly(), typeof(DebuggableAttribute)))?.IsJITOptimizerDisabled == true;
            if (isDebugBuild)
            {
                AssertLog(modEvents, "parameterless info", LogLevel.Message, mod.Name);
                AssertLog(modEvents, "parameterless warn", LogLevel.Warning, mod.Name);
                AssertLog(modEvents, "parameterless error", LogLevel.Error, mod.Name);
                AssertLog(modEvents, "parameterless fatal", LogLevel.Fatal, mod.Name);
                AssertLog(modEvents, "parameterless debug", LogLevel.Info, mod.Name);
                AssertLog(modEvents, "parameterless display", LogLevel.Message, mod.Name);
                AssertLog(modEvents, "explicit info", LogLevel.Message, mod.Name);
                AssertLog(modEvents, "explicit warn", LogLevel.Warning, mod.Name);
                AssertLog(modEvents, "explicit error", LogLevel.Error, mod.Name);
                AssertLog(modEvents, "explicit fatal", LogLevel.Fatal, mod.Name);
                AssertLog(modEvents, "explicit debug", LogLevel.Info, mod.Name);
                AssertLog(modEvents, "explicit display", LogLevel.Message, mod.Name);
            }
            else
            {
                Assert.Empty(modEvents);
            }

            ICollection<BlasMod> loadedMods = (ICollection<BlasMod>)ModHelper.LoadedMods;
            loadedMods.Clear();

            ManualLogSource unknownLogger = FindLogSource("Unknown mod");
            List<LogEventArgs> unknownEvents = [];
            void unknownHandler(object? _, LogEventArgs logEvent) => unknownEvents.Add(logEvent);
            unknownLogger.LogEvent += unknownHandler;
            try
            {
                NestedConsumerComponent.EmitUnregisteredLog();
                if (isDebugBuild)
                    AssertLog(unknownEvents, "unregistered", LogLevel.Message, "Unknown mod");
                else
                    Assert.Empty(unknownEvents);
            }
            finally
            {
                unknownLogger.LogEvent -= unknownHandler;
            }
        }
        finally
        {
            modLogger.LogEvent -= modHandler;
        }
    }

    private static SmokeMod CreateRegisteredSmokeMod()
    {
        SmokeMod mod = (SmokeMod)RuntimeHelpers.GetUninitializedObject(typeof(SmokeMod));

        // The real constructor invokes Harmony's native detour setup, which cannot run in this net8 harness.
        typeof(BlasMod).GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(mod, "Newbie Elton smoke");

        PropertyInfo loadedModsProperty = typeof(ModHelper).GetProperty(
            "LoadedMods", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        loadedModsProperty.GetSetMethod(true)!.Invoke(null, [new List<BlasMod> { mod }]);

        MethodInfo registerMethod = typeof(ModLog).GetMethod("Register", BindingFlags.Static | BindingFlags.NonPublic)!;
        registerMethod.Invoke(null, [mod]);
        return mod;
    }

    private static ManualLogSource FindLogSource(string sourceName)
    {
        ManualLogSource? source = BepInEx.Logging.Logger.Sources.OfType<ManualLogSource>().FirstOrDefault(item => item.SourceName == sourceName);
        Assert.NotNull(source);
        return source!;
    }

    private static void AssertLog(IEnumerable<LogEventArgs> events, string message, LogLevel level, string sourceName)
    {
        string expectedMessage = "[DEBUG] " + message;
        List<LogEventArgs> capturedEvents = [.. events];
        Assert.True(capturedEvents.Any(logEvent =>
            logEvent.Level == level &&
            logEvent.Source.SourceName == sourceName &&
            Equals(logEvent.Data, expectedMessage)),
            "Expected log was not attributed to " + sourceName + ": " + expectedMessage +
            ". Captured: " + string.Join(" | ", capturedEvents.Select(logEvent => logEvent.ToString())));
    }

    private class BaseItems : ItemCollection<string>
    {
        public string BaseItem = "base";
    }

    private sealed class DerivedItems : BaseItems
    {
        public static string StaticItem = "static";
        public string DerivedItem = "derived";
        public int NotAnItem = 123;
        public object AlsoNotAnItem = new();
        public string NullItem = null!;
    }

    private sealed class SmokeMod : BlasMod
    {
        public SmokeMod() : base("newbie-elton-smoke", "Newbie Elton smoke", "Smoke test", "1.0.0")
        {
        }
    }

    private static class NestedConsumerComponent
    {
        public static void EmitParameterlessLogs()
        {
            ModLogExtensions.InfoIfDebugBuild("parameterless info");
            ModLogExtensions.WarnIfDebugBuild("parameterless warn");
            ModLogExtensions.ErrorIfDebugBuild("parameterless error");
            ModLogExtensions.FatalIfDebugBuild("parameterless fatal");
            ModLogExtensions.DebugIfDebugBuild("parameterless debug");
            ModLogExtensions.DisplayIfDebugBuild("parameterless display");
        }

        public static void EmitExplicitLogs(BlasMod mod)
        {
            ModLogExtensions.InfoIfDebugBuild("explicit info", mod);
            ModLogExtensions.WarnIfDebugBuild("explicit warn", mod);
            ModLogExtensions.ErrorIfDebugBuild("explicit error", mod);
            ModLogExtensions.FatalIfDebugBuild("explicit fatal", mod);
            ModLogExtensions.DebugIfDebugBuild("explicit debug", mod);
            ModLogExtensions.DisplayIfDebugBuild("explicit display", mod);
        }

        public static void EmitUnregisteredLog()
        {
            ModLogExtensions.InfoIfDebugBuild("unregistered");
        }
    }

}