using Blasphemous.NewbieEltonLibs.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using UnityEngine;
using Xunit;

namespace Blasphemous.NewbieEltonLibs.Tests;

/// <summary>Verifies public serialization and settings contracts.</summary>
public sealed class SerializationCoverageTests
{
    /// <summary>Verifies vector values, constants, conversions, and JSON shape.</summary>
    [Fact]
    public void SerializableVector3ExposesStablePublicShape()
    {
        SerializableVector3 value = new SerializableVector3(1.5f, -2.25f, 3f);
        Vector3 vector3 = default;
        vector3.x = 1.5f;
        vector3.y = -2.25f;
        vector3.z = 3f;
        Vector2 vector2 = default;
        vector2.x = 1.5f;
        vector2.y = -2.25f;
        SerializableVector3 fromVector3 = vector3;
        SerializableVector3 fromVector2 = vector2;

        Assert.Equal("(1.5, -2.25, 3)", value.ToString());
        Assert.Equal(new SerializableVector3(0, 0, 0), SerializableVector3.Zero);
        Assert.Equal(new SerializableVector3(1, 1, 1), SerializableVector3.One);
        Assert.Equal(1.5f, fromVector3.X);
        Assert.Equal(-2.25f, fromVector2.Y);
        Assert.Equal(value, fromVector3);
        Assert.Equal(new SerializableVector3(1.5f, -2.25f, 0), fromVector2);

        JObject json = JObject.Parse(JsonConvert.SerializeObject(value));
        Assert.Equal(1.5f, (float)json["X"]!);
        Assert.Equal(-2.25f, (float)json["Y"]!);
        Assert.Equal(3f, (float)json["Z"]!);
    }

    /// <summary>Verifies ignored Unity types and null converter behavior.</summary>
    [Fact]
    public void UnityEngineIgnoreConverterHonorsConfiguredTypes()
    {
        UnityEngineIgnoreConverter defaults = new UnityEngineIgnoreConverter();
        Assert.True(defaults.CanConvert(typeof(GameObject)));
        Assert.True(defaults.CanConvert(typeof(Texture)));
        Assert.False(defaults.CanConvert(typeof(Texture2D)));
        Assert.False(defaults.CanConvert(null!));

        UnityEngineIgnoreConverter allObjects = new UnityEngineIgnoreConverter(true);
        Assert.True(allObjects.CanConvert(typeof(UnityEngine.Object)));
        Assert.True(allObjects.CanConvert(typeof(Texture2D)));
        Assert.False(allObjects.CanConvert(typeof(string)));

        UnityEngineIgnoreConverter custom = new UnityEngineIgnoreConverter(new[] { typeof(Texture), typeof(string) });
        Assert.True(custom.CanConvert(typeof(Texture2D)));
        Assert.False(custom.CanConvert(typeof(GameObject)));
        Assert.False(custom.CanConvert(typeof(string)));

        StringWriter output = new StringWriter();
        JsonTextWriter writer = new JsonTextWriter(output);
        defaults.WriteJson(writer, null, new JsonSerializer());
        writer.Flush();
        Assert.Equal("null", output.ToString());

        JsonTextReader reader = new JsonTextReader(new StringReader("null"));
        Assert.True(reader.Read());
        Assert.Null(defaults.ReadJson(reader, typeof(Texture), null, new JsonSerializer()));
    }

    /// <summary>Verifies settings factories return the documented independent presets.</summary>
    [Fact]
    public void SettingsFactoriesReturnIndependentPresets()
    {
        JsonSerializerSettings stats = JsonSerializerSettingsFactory.CreateStatsPatchSettings();
        JsonSerializerSettings inventory = JsonSerializerSettingsFactory.CreateInventoryExportSettings();
        JsonSerializerSettings penitent = JsonSerializerSettingsFactory.CreatePenitentDataSettings();
        JsonSerializerSettings comparison = JsonSerializerSettingsFactory.CreateInventoryComparisonSettings();

        Assert.Equal(TypeNameHandling.Objects, stats.TypeNameHandling);
        Assert.Equal(ReferenceLoopHandling.Ignore, stats.ReferenceLoopHandling);
        Assert.Contains(stats.Converters, converter => converter is StringEnumConverter);
        Assert.Contains(inventory.Converters, converter => converter is UnityEngineIgnoreConverter);
        Assert.Equal(TypeNameHandling.None, penitent.TypeNameHandling);
        Assert.Equal(ReferenceLoopHandling.Error, penitent.ReferenceLoopHandling);
        Assert.Equal(TypeNameHandling.None, comparison.TypeNameHandling);
        Assert.Equal(ReferenceLoopHandling.Ignore, comparison.ReferenceLoopHandling);

        stats.TypeNameHandling = TypeNameHandling.None;
        inventory.Converters.Clear();

        Assert.Equal(TypeNameHandling.Objects, JsonSerializerSettingsFactory.CreateStatsPatchSettings().TypeNameHandling);
        Assert.Contains(JsonSerializerSettingsFactory.CreateInventoryExportSettings().Converters, converter => converter is UnityEngineIgnoreConverter);
    }

    private static void ImplicitConversionsCompile(SerializableVector3 value)
    {
        Vector3 vector3 = value;
        Vector2 vector2 = value;
        _ = vector3;
        _ = vector2;
    }
}
