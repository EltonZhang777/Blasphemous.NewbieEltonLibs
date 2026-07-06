using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Config;
using Blasphemous.ModdingAPI.Files;
using HarmonyLib;
using Newtonsoft.Json;
using System.IO;

namespace Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;

/// <summary>
/// Extension methods for ConfigHandler
/// </summary>
public static class ConfigHandlerExtensions
{
    private static BlasMod GetMod(this ConfigHandler configHandler) => Traverse.Create(configHandler).Field("_mod").GetValue<BlasMod>();

    /// <summary>
    /// Load config while specifying <see cref="JsonSerializerSettings"/>
    /// </summary>
    public static T Load<T>(this ConfigHandler configHandler, JsonSerializerSettings settings = null) where T : new()
    {
        FileHandler fileHandler = configHandler.GetMod().FileHandler;
        string configPath = fileHandler.GetConfigPath();
        string text = File.Exists(configPath) ? File.ReadAllText(configPath) : string.Empty;
        if (text == string.Empty)
        {
            T val = new();
            configHandler.Save(val);
            return val;
        }

        return JsonConvert.DeserializeObject<T>(text, settings);
    }

    /// <summary>
    /// Save config while specifying <see cref="JsonSerializerSettings"/>
    /// </summary>
    public static void Save<T>(this ConfigHandler configHandler, T config, Formatting formatting = Formatting.Indented, JsonSerializerSettings settings = null)
    {
        FileHandler fileHandler = configHandler.GetMod().FileHandler;
        string configPath = fileHandler.GetConfigPath();
        string directory = Path.GetDirectoryName(configPath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string outputContents = JsonConvert.SerializeObject(config, formatting, settings);
        File.WriteAllText(configPath, outputContents);
    }
}
