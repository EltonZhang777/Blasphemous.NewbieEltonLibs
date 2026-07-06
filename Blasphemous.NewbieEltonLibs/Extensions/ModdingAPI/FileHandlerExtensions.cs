using Blasphemous.ModdingAPI.Files;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;

/// <summary>
/// Extension methods for FileHandler
/// </summary>
public static class FileHandlerExtensions
{
    /// <summary>
    /// Gets the data directory path for this mod
    /// </summary>
    public static string GetDataPath(this FileHandler fileHandler)
    {
        return Traverse.Create(fileHandler).Field("dataPath").GetValue<string>();
    }

    /// <summary>
    /// Gets the config directory path for this mod
    /// </summary>
    public static string GetConfigPath(this FileHandler fileHandler)
    {
        return Traverse.Create(fileHandler).Field("configPath").GetValue<string>();
    }

    /// <summary>
    /// Gets all file names in the data directory
    /// </summary>
    public static string[] GetAllDataFileNames(this FileHandler fileHandler)
    {
        return Directory.GetFiles(fileHandler.GetDataPath()).Select(x => Path.GetFileName(x)).ToArray();
    }

    /// <summary>
    /// Loads a JSON data file by name, deserializing to the specified type
    /// </summary>
    public static T LoadDataAsJson<T>(this FileHandler fileHandler, string fileName)
    {
        if (!fileHandler.LoadDataAsJson(fileName, out T result))
        {
            throw new ArgumentException($"Failed to load {fileName} to JSON of type {typeof(T)}!");
        }
        return result;
    }

    /// <summary>
    /// Loads a JSON data file by name with custom serializer settings, deserializing to the specified type
    /// </summary>
    public static T LoadDataAsJson<T>(this FileHandler fileHandler, string fileName, JsonSerializerSettings settings)
    {
        if (!fileHandler.ReadFileContents(Path.Combine(fileHandler.GetDataPath(), fileName), out string output))
        {
            throw new ArgumentException($"Failed to load {fileName} to JSON of type {typeof(T)}!");
        }
        return JsonConvert.DeserializeObject<T>(output, settings);
    }

    /// <summary>
    /// Tries to load a JSON file from the content folder
    /// </summary>
    public static bool LoadContentAsJson<T>(this FileHandler fileHandler, string fileName, out T output)
    {
        if (ReadFileContents(fileHandler, Path.Combine(fileHandler.ContentFolder, fileName), out string output2))
        {
            output = JsonConvert.DeserializeObject<T>(output2);
            return true;
        }

        output = default;
        return false;
    }

    /// <summary>
    /// Writes a JSON object to a file in the content folder
    /// </summary>
    public static void WriteJsonToContent(this FileHandler fileHandler, string fileName, object obj)
    {
        File.WriteAllText(
            Path.Combine(fileHandler.ContentFolder, fileName),
            JsonConvert.SerializeObject(obj, Formatting.Indented));
    }

    /// <summary>
    /// Writes a JSON object to a file in the content folder with custom formatting and serializer settings
    /// </summary>
    public static void WriteJsonToContent(this FileHandler fileHandler, string fileName, object obj, JsonSerializerSettings settings = null, Formatting formatting = Formatting.Indented)
    {
        if (settings != null)
        {
            File.WriteAllText(
                Path.Combine(fileHandler.ContentFolder, fileName),
                JsonConvert.SerializeObject(obj, formatting, settings));
        }
        else
        {
            File.WriteAllText(
                Path.Combine(fileHandler.ContentFolder, fileName),
                JsonConvert.SerializeObject(obj, formatting));
        }
    }

    /// <summary>
    /// Loads a file as an AssetBundle from the data directory
    /// </summary>
    /// <param name="fileHandler">The file handler instance</param>
    /// <param name="fileName">The asset bundle file name</param>
    /// <param name="assetBundle">The loaded asset bundle, or null</param>
    /// <returns>True if the asset bundle was loaded successfully</returns>
    public static bool LoadDataAsAssetBundle(this FileHandler fileHandler, string fileName, out AssetBundle assetBundle)
    {
        assetBundle = AssetBundle.LoadFromFile(Path.Combine(GetDataPath(fileHandler), fileName));
        return assetBundle != null;
    }

    private static bool ReadFileContents(this FileHandler fileHandler, string path, out string output)
    {
        if (File.Exists(path))
        {
            output = File.ReadAllText(path);
            return true;
        }

        output = null;
        return false;
    }
}
