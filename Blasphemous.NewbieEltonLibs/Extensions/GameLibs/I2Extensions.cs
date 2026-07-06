using I2.Loc;

namespace Blasphemous.NewbieEltonLibs.Extensions.GameLibs;

/// <summary>
/// Extension methods for I2 Localize providing low-level translation operations
/// </summary>
public static class I2Extensions
{
    /// <summary>
    /// Gets the secondary translated object from a Localize instance, updating the main and secondary translation references
    /// </summary>
    public static T DoGetSecondaryTranslatedObj<T>(
        this Localize localize,
        ref string mainTranslation,
        ref string secondaryTranslation) where T : UObject
    {
        string text;
        string text2;
        localize.DoDeserializeTranslation(mainTranslation, out text, out text2);
        T t = (T)(object)null;
        if (!string.IsNullOrEmpty(text2))
        {
            t = localize.DoGetObject<T>(text2);
            if (t != null)
            {
                mainTranslation = text;
                secondaryTranslation = text2;
            }
        }
        if (t == null)
        {
            t = localize.DoGetObject<T>(secondaryTranslation);
        }
        return t;
    }

    /// <summary>
    /// Deserializes a translation string into its value and secondary parts
    /// </summary>
    public static void DoDeserializeTranslation(
        this Localize localize,
        string translation,
        out string value,
        out string secondary)
    {
        if (!string.IsNullOrEmpty(translation) && translation.Length > 1 && translation[0] == '[')
        {
            int num = translation.IndexOf(']');
            if (num > 0)
            {
                secondary = translation.Substring(1, num - 1);
                value = translation.Substring(num + 1);
                return;
            }
        }
        value = translation;
        secondary = string.Empty;
    }

    /// <summary>
    /// Gets the translated object of the specified type by the given translation string
    /// </summary>
    public static T DoGetObject<T>(
        this Localize localize,
        string Translation) where T : UObject
    {
        if (string.IsNullOrEmpty(Translation))
        {
            return (T)(object)null;
        }
        T translatedObject = localize.DoGetTranslatedObject<T>(Translation);
        if (translatedObject == null)
        {
            translatedObject = localize.DoGetTranslatedObject<T>(Translation);
        }
        return translatedObject;
    }

    /// <summary>
    /// Finds the translated object of the specified type by the given translation string
    /// </summary>
    public static T DoGetTranslatedObject<T>(
        this Localize localize,
        string Translation) where T : UObject
    {
        return localize.FindTranslatedObject<T>(Translation);
    }
}
