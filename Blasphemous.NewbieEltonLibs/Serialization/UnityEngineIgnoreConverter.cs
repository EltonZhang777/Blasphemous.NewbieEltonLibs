using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.Serialization;

/// <summary>
/// Serializes configured Unity object types as JSON null values.
/// </summary>
public class UnityEngineIgnoreConverter : JsonConverter
{
    private static readonly Type[] DefaultIgnoredTypes =
    {
        typeof(GameObject),
        typeof(Transform),
        typeof(Texture),
        typeof(Sprite),
        typeof(UnityEngine.UI.Image),
        typeof(Material)
    };

    private readonly bool _ignoreAllUnityObjects;
    private readonly bool _includeDerivedTypes;
    private readonly Type[] _ignoredTypes;

    /// <summary>
    /// Creates a converter that ignores the six Unity object types used by the original exporter.
    /// </summary>
    public UnityEngineIgnoreConverter()
    {
        _ignoredTypes = (Type[])DefaultIgnoredTypes.Clone();
    }

    /// <summary>
    /// Creates a converter that ignores either the default types or every Unity object type.
    /// </summary>
    /// <param name="ignoreAllUnityObjects"><see langword="true"/> to ignore <see cref="UnityEngine.Object"/> and all derived types.</param>
    public UnityEngineIgnoreConverter(bool ignoreAllUnityObjects)
    {
        _ignoreAllUnityObjects = ignoreAllUnityObjects;
        _ignoredTypes = ignoreAllUnityObjects ? new Type[0] : (Type[])DefaultIgnoredTypes.Clone();
    }

    /// <summary>
    /// Creates a converter for the supplied Unity object types and their derived types.
    /// </summary>
    /// <param name="ignoredTypes">The candidate types to ignore; non-Unity types are skipped.</param>
    public UnityEngineIgnoreConverter(IEnumerable<Type> ignoredTypes)
    {
        if (ignoredTypes == null)
            throw new ArgumentNullException(nameof(ignoredTypes));

        _includeDerivedTypes = true;
        _ignoredTypes = ignoredTypes
            .Where(type => type != null && typeof(UnityEngine.Object).IsAssignableFrom(type))
            .ToArray();
    }

    /// <summary>
    /// Determines whether the converter handles the specified type.
    /// </summary>
    /// <param name="objectType">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is configured to be ignored; otherwise, <see langword="false"/>.</returns>
    public override bool CanConvert(Type objectType)
    {
        if (objectType == null)
            return false;

        if (_ignoreAllUnityObjects)
            return typeof(UnityEngine.Object).IsAssignableFrom(objectType);

        return _includeDerivedTypes
            ? _ignoredTypes.Any(type => type.IsAssignableFrom(objectType))
            : _ignoredTypes.Any(type => type == objectType);
    }

    /// <summary>
    /// Writes an ignored value as a JSON null token.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The value being serialized.</param>
    /// <param name="serializer">The active JSON serializer.</param>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        writer.WriteNull();
    }

    /// <summary>
    /// Reads an ignored value as <see langword="null"/>.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="objectType">The target type.</param>
    /// <param name="existingValue">The existing target value, if any.</param>
    /// <param name="serializer">The active JSON serializer.</param>
    /// <returns><see langword="null"/>.</returns>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        return null;
    }
}
