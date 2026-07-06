using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Localization;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using System.Collections.Generic;

namespace Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;

/// <summary>
/// Extension methods for LocalizationHandler
/// </summary>
public static class LocalizationHandlerExtensions
{
    /// <summary>
    /// Localizes a key to the specified language, falling back to the default language if not found
    /// </summary>
    /// <param name="localizationHandler">The localization handler instance</param>
    /// <param name="key">The localization key to translate</param>
    /// <param name="languageName">The target language name (e.g. English, Spanish)</param>
    /// <returns>The localized string, or "LOC_ERROR" if not found in any language</returns>
    public static string Localize(
        this LocalizationHandler localizationHandler,
        string key,
        string languageName)
    {
        Dictionary<string, Dictionary<string, string>> _textByLanguage = TraverseUtils.GetValue<Dictionary<string, Dictionary<string, string>>>(localizationHandler, "_textByLanguage", TraverseUtils.TraverseAccessType.Field);
        string _defaultLanguage = TraverseUtils.GetValue<string>(localizationHandler, "_defaultLanguage", TraverseUtils.TraverseAccessType.Field);
        BlasMod _mod = TraverseUtils.GetValue<BlasMod>(localizationHandler, "_mod", TraverseUtils.TraverseAccessType.Field);

        // get langauge code by language name
        int languageIndex = I2LocManager.Sources[0].GetLanguageIndex(languageName);
        string languageCode = I2LocManager.Sources[0].GetLanguagesCode()[languageIndex];

        // The language exists and contains the specified key
        if (_textByLanguage.ContainsKey(languageCode) && _textByLanguage[languageCode].ContainsKey(key))
        {
            return _textByLanguage[languageCode][key];
        }

        // The language doesn't exist - use default language
        if (_textByLanguage.ContainsKey(_defaultLanguage) && _textByLanguage[_defaultLanguage].ContainsKey(key))
        {
            return _textByLanguage[_defaultLanguage][key];
        }

        ModLog.Error($"Failed to localize '{key}' to any language.", _mod);
        return "LOC_ERROR";
    }
}
