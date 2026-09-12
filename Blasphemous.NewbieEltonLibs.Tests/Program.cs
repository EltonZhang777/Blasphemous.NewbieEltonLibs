using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using BepInEx.Logging;
using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Helpers;
using Blasphemous.NewbieEltonLibs.Tests;
using Blasphemous.NewbieEltonLibs.Components;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;
using Blasphemous.NewbieEltonLibs.Serialization;
using Framework.FrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.Tests;

internal static class Program
{
    private static int Main()
    {
        try
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            SerializableVector3IsUsableByExternalConsumers();
            ItemCollectionIsUsableByExternalConsumers();
            EntityOrientationConversionRejectsUnsupportedValues();
            UnityEngineIgnoreConverterIsUsableByExternalConsumers();
            JsonSerializerSettingsFactoryIsUsableByExternalConsumers();
            ModLogExtensionsPreserveExternalConsumerOwnership();
            FlagApiSmokeTests.Run();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void SerializableVector3IsUsableByExternalConsumers()
    {
        SerializableVector3 value = new(1.5f, -2.25f, 3f);

        Assert(value.X == 1.5f, "X coordinate was not retained.");
        Assert(value.Y == -2.25f, "Y coordinate was not retained.");
        Assert(value.Z == 3f, "Z coordinate was not retained.");
        Assert(value.ToString() == "(1.5, -2.25, 3)", "ToString format changed.");

        Vector2 vector2 = default;
        vector2.x = 1.5f;
        vector2.y = -2.25f;
        SerializableVector3 fromVector2 = vector2;
        Assert(fromVector2.X == 1.5f && fromVector2.Y == -2.25f && fromVector2.Z == 0f, "Vector2 conversion back changed.");

        Vector3 vector3 = default;
        vector3.x = 1.5f;
        vector3.y = -2.25f;
        vector3.z = 3f;
        SerializableVector3 fromVector3 = vector3;
        Assert(fromVector3.X == 1.5f && fromVector3.Y == -2.25f && fromVector3.Z == 3f, "Vector3 conversion back changed.");

        Assert(SerializableVector3.Zero.X == 0f && SerializableVector3.Zero.Y == 0f && SerializableVector3.Zero.Z == 0f, "Zero value changed.");
        Assert(SerializableVector3.One.X == 1f && SerializableVector3.One.Y == 1f && SerializableVector3.One.Z == 1f, "One value changed.");

        JObject json = JObject.Parse(JsonConvert.SerializeObject(value));
        Assert(json.Count == 3, "JSON shape contains an unexpected property.");
        Assert((float?)json["X"] == 1.5f, "JSON X property changed.");
        Assert((float?)json["Y"] == -2.25f, "JSON Y property changed.");
        Assert((float?)json["Z"] == 3f, "JSON Z property changed.");
    }

    private static void ImplicitConversionsCompile(SerializableVector3 value)
    {
        // UnityEngine constructors require native Unity state; these assignments keep the consumer API compile-checked.
        Vector2 vector2 = value;
        Vector3 vector3 = value;
        _ = vector2;
        _ = vector3;
    }

    private static void ItemCollectionIsUsableByExternalConsumers()
    {
        DerivedItems items = new();
        IEnumerable<string> values = items.Items;

        items.DerivedItem = "updated";
        List<string> enumerated = values.ToList();

        Assert(enumerated.Contains("base"), "Inherited matching field was not collected.");
        Assert(enumerated.Contains("updated"), "Field changes were not observed by deferred enumeration.");
        Assert(enumerated.Contains("static"), "Public static matching field was not collected.");
        Assert(enumerated.Count(value => value == null) == 1, "Null matching field was not collected.");
        Assert(enumerated.Count == 4, "Fields with non-matching declared types were collected.");
    }

    private static void EntityOrientationConversionRejectsUnsupportedValues()
    {
        try
        {
            ((EntityOrientation)int.MaxValue).ToDirectionalVector();
        }
        catch (Exception)
        {
            return;
        }

        throw new InvalidOperationException("Unsupported orientation was silently converted.");
    }

    private static void DirectionConversionsCompile()
    {
        // UnityEngine directional properties require a Unity host; these assignments keep the extension compile-checked.
        Vector2 right = EntityOrientation.Right.ToDirectionalVector();
        Vector2 left = EntityOrientation.Left.ToDirectionalVector();
        _ = right;
        _ = left;
    }

    private static void UnityEngineIgnoreConverterIsUsableByExternalConsumers()
    {
        UnityEngineIgnoreConverter defaultConverter = new();
        Type[] defaultTypes =
        {
            typeof(GameObject),
            typeof(Transform),
            typeof(Texture),
            typeof(Sprite),
            typeof(UnityEngine.UI.Image),
            typeof(Material)
        };

        foreach (Type defaultType in defaultTypes)
            Assert(defaultConverter.CanConvert(defaultType), "Default converter type set changed.");

        Assert(!defaultConverter.CanConvert(typeof(Texture2D)), "Default converter began matching derived types.");

        UnityEngineIgnoreConverter defaultBooleanConverter = new(false);
        foreach (Type defaultType in defaultTypes)
            Assert(defaultBooleanConverter.CanConvert(defaultType), "Boolean default converter type set changed.");

        Assert(!defaultBooleanConverter.CanConvert(typeof(Texture2D)), "Boolean default converter began matching derived types.");

        UnityEngineIgnoreConverter allUnityObjectsConverter = new(true);
        Assert(allUnityObjectsConverter.CanConvert(typeof(UnityEngine.Object)), "All-object converter does not match UnityEngine.Object.");
        Assert(allUnityObjectsConverter.CanConvert(typeof(GameObject)), "All-object converter does not match a Unity object.");
        Assert(allUnityObjectsConverter.CanConvert(typeof(Texture2D)), "All-object converter does not match a Unity object subtype.");
        Assert(!allUnityObjectsConverter.CanConvert(typeof(string)), "All-object converter matched a non-Unity type.");

        List<Type> customTypes = new() { typeof(Texture), typeof(string) };
        UnityEngineIgnoreConverter customConverter = new(customTypes);
        customTypes.Clear();
        Assert(customConverter.CanConvert(typeof(Texture)), "Custom converter does not match its configured type.");
        Assert(customConverter.CanConvert(typeof(Texture2D)), "Custom converter does not match configured subtypes.");
        Assert(!customConverter.CanConvert(typeof(GameObject)), "Custom converter matched an unconfigured Unity type.");
        Assert(!customConverter.CanConvert(typeof(string)), "Custom converter retained a non-Unity type.");

        StringWriter output = new();
        JsonTextWriter writer = new(output);
        defaultConverter.WriteJson(writer, null!, new JsonSerializer());
        writer.Flush();
        Assert(output.ToString() == "null", "Ignored values were not written as null.");

        JsonTextReader reader = new(new StringReader("null"));
        Assert(reader.Read(), "Null JSON token was not read.");
        Assert(defaultConverter.ReadJson(reader, typeof(Texture), null, new JsonSerializer()) == null, "Ignored values were not read as null.");
    }

    private static void JsonSerializerSettingsFactoryIsUsableByExternalConsumers()
    {
        JsonSerializerSettings statsPatchSettings = JsonSerializerSettingsFactory.CreateStatsPatchSettings();
        Assert(statsPatchSettings.ReferenceLoopHandling == ReferenceLoopHandling.Ignore, "Stats patch reference handling changed.");
        Assert(statsPatchSettings.PreserveReferencesHandling == PreserveReferencesHandling.None, "Stats patch preserve-reference handling changed.");
        Assert(statsPatchSettings.TypeNameHandling == TypeNameHandling.Objects, "Stats patch type-name handling changed.");
        Assert(statsPatchSettings.Converters.OfType<StringEnumConverter>().Any(), "Stats patch enum converter is missing.");

        JsonSerializerSettings inventoryExportSettings = JsonSerializerSettingsFactory.CreateInventoryExportSettings();
        Assert(inventoryExportSettings.Converters.OfType<UnityEngineIgnoreConverter>().Any(), "Inventory export Unity converter is missing.");
        Assert(inventoryExportSettings.TypeNameHandling == TypeNameHandling.Objects, "Inventory export type-name handling changed.");

        JsonSerializerSettings penitentDataSettings = JsonSerializerSettingsFactory.CreatePenitentDataSettings();
        Assert(penitentDataSettings.Converters.OfType<StringEnumConverter>().Any(), "Penitent data enum converter is missing.");
        Assert(penitentDataSettings.TypeNameHandling == TypeNameHandling.None, "Penitent data gained unnecessary type-name handling.");
        Assert(penitentDataSettings.ReferenceLoopHandling == ReferenceLoopHandling.Error, "Penitent data gained unnecessary reference-loop handling.");

        JsonSerializerSettings comparisonSettings = JsonSerializerSettingsFactory.CreateInventoryComparisonSettings();
        Assert(comparisonSettings.Converters.OfType<StringEnumConverter>().Any(), "Comparison enum converter is missing.");
        Assert(comparisonSettings.ReferenceLoopHandling == ReferenceLoopHandling.Ignore, "Comparison reference handling changed.");
        Assert(comparisonSettings.PreserveReferencesHandling == PreserveReferencesHandling.None, "Comparison preserve-reference handling changed.");
        Assert(comparisonSettings.TypeNameHandling == TypeNameHandling.None, "Comparison settings gained polymorphic type metadata.");

        statsPatchSettings.TypeNameHandling = TypeNameHandling.None;
        Assert(JsonSerializerSettingsFactory.CreateStatsPatchSettings().TypeNameHandling == TypeNameHandling.Objects, "Factory returned shared mutable settings.");

        inventoryExportSettings.Converters.Clear();
        Assert(JsonSerializerSettingsFactory.CreateInventoryExportSettings().Converters.OfType<UnityEngineIgnoreConverter>().Any(), "Inventory settings shared a mutable converter list.");

        penitentDataSettings.TypeNameHandling = TypeNameHandling.Objects;
        Assert(JsonSerializerSettingsFactory.CreatePenitentDataSettings().TypeNameHandling == TypeNameHandling.None, "Penitent settings were shared.");

        comparisonSettings.ReferenceLoopHandling = ReferenceLoopHandling.Error;
        Assert(JsonSerializerSettingsFactory.CreateInventoryComparisonSettings().ReferenceLoopHandling == ReferenceLoopHandling.Ignore, "Comparison settings were shared.");
    }

    private static void ModLogExtensionsPreserveExternalConsumerOwnership()
    {
        SmokeMod mod = CreateRegisteredSmokeMod();
        ManualLogSource modLogger = FindLogSource(mod.Name);
        List<LogEventArgs> modEvents = new();
        EventHandler<LogEventArgs> modHandler = (_, logEvent) => modEvents.Add(logEvent);
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
                Assert(modEvents.Count == 0, "Release builds emitted debug logs.");
            }

            ICollection<BlasMod> loadedMods = (ICollection<BlasMod>)ModHelper.LoadedMods;
            loadedMods.Clear();

            ManualLogSource unknownLogger = FindLogSource("Unknown mod");
            List<LogEventArgs> unknownEvents = new();
            EventHandler<LogEventArgs> unknownHandler = (_, logEvent) => unknownEvents.Add(logEvent);
            unknownLogger.LogEvent += unknownHandler;
            try
            {
                NestedConsumerComponent.EmitUnregisteredLog();
                if (isDebugBuild)
                    AssertLog(unknownEvents, "unregistered", LogLevel.Message, "Unknown mod");
                else
                    Assert(unknownEvents.Count == 0, "Release builds emitted an unregistered debug log.");
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
        loadedModsProperty.GetSetMethod(true)!.Invoke(null, new object[] { new List<BlasMod> { mod } });

        MethodInfo registerMethod = typeof(ModLog).GetMethod("Register", BindingFlags.Static | BindingFlags.NonPublic)!;
        registerMethod.Invoke(null, new object[] { mod });
        return mod;
    }

    private static ManualLogSource FindLogSource(string sourceName)
    {
        ManualLogSource? source = BepInEx.Logging.Logger.Sources.OfType<ManualLogSource>().FirstOrDefault(item => item.SourceName == sourceName);
        return source ?? throw new InvalidOperationException("Log source was not registered: " + sourceName);
    }

    private static void AssertLog(IEnumerable<LogEventArgs> events, string message, LogLevel level, string sourceName)
    {
        string expectedMessage = "[DEBUG] " + message;
        List<LogEventArgs> capturedEvents = events.ToList();
        Assert(capturedEvents.Any(logEvent =>
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
        public object AlsoNotAnItem = new object();
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

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
