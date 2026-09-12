using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Blasphemous.NewbieEltonLibs.Serialization;

/// <summary>
/// Creates independent JSON settings for the supported export scenarios.
/// </summary>
public static class JsonSerializerSettingsFactory
{
    /// <summary>
    /// Creates settings for polymorphic Stats Patch data.
    /// </summary>
    /// <returns>Fresh settings with enum conversion, ignored reference loops, and object type metadata.</returns>
    public static JsonSerializerSettings CreateStatsPatchSettings()
    {
        JsonSerializerSettings settings = new();
        settings.Converters.Add(new StringEnumConverter());
        settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        settings.PreserveReferencesHandling = PreserveReferencesHandling.None;
        settings.TypeNameHandling = TypeNameHandling.Objects;
        return settings;
    }

    /// <summary>
    /// Creates settings for inventory export data.
    /// </summary>
    /// <returns>Fresh Stats Patch settings with the default Unity object ignore converter.</returns>
    public static JsonSerializerSettings CreateInventoryExportSettings()
    {
        JsonSerializerSettings settings = CreateStatsPatchSettings();
        settings.Converters.Insert(0, new UnityEngineIgnoreConverter());
        return settings;
    }

    /// <summary>
    /// Creates settings for Penitent data.
    /// </summary>
    /// <returns>Fresh settings with string enum conversion and no polymorphic type metadata.</returns>
    public static JsonSerializerSettings CreatePenitentDataSettings()
    {
        JsonSerializerSettings settings = new();
        settings.Converters.Add(new StringEnumConverter());
        return settings;
    }

    /// <summary>
    /// Creates the narrow settings used when comparing inventory effect data in debug builds.
    /// </summary>
    /// <returns>Fresh settings with enum conversion and reference-loop handling only.</returns>
    public static JsonSerializerSettings CreateInventoryComparisonSettings()
    {
        JsonSerializerSettings settings = new();
        settings.Converters.Add(new StringEnumConverter());
        settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        settings.PreserveReferencesHandling = PreserveReferencesHandling.None;
        return settings;
    }
}