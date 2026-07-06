using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Blasphemous.NewbieEltonLibs.Extensions.System;

/// <summary>
/// Extension methods for common .NET types
/// </summary>
public static class SystemExtensions
{
    /// <summary>
    /// Moves an item from one index to another within a list
    /// </summary>
    /// <param name="list">The list to operate on</param>
    /// <param name="oldIndex">The index of the item to move</param>
    /// <param name="newIndex">The target index to move the item to</param>
    public static void Move<T>(this List<T> list, int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= list.Count || newIndex < 0 || newIndex >= list.Count)
            throw new ArgumentOutOfRangeException();

        if (oldIndex == newIndex)
            return;

        T? item = list[oldIndex];
        list.RemoveAt(oldIndex);
        //if (newIndex > oldIndex) 
        //    newIndex--; 
        list.Insert(newIndex, item);
    }

    /// <summary>
    /// Gets the next enum value by stepping forward (or backward) by the specified step length
    /// </summary>
    /// <param name="currentValue">The current enum value</param>
    /// <param name="stepLength">Number of steps to advance (negative values go backward)</param>
    /// <returns>The enum value at the new position, wrapping around if out of bounds</returns>
    public static T GetNextEnumValue<T>(this T currentValue, int stepLength = 1) where T : Enum
    {
        T[] values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
        int currentIndex = Array.IndexOf(values, currentValue);

        // Calculate the new index, handling out-of-bounds cases
        int newIndex = (currentIndex + stepLength) % values.Length;
        if (newIndex < 0)
        {
            newIndex += values.Length;
        }

        return values[newIndex];
    }

    /// <summary>
    /// Replaces multiple words in a string using a dictionary mapping
    /// </summary>
    /// <param name="str">The input string to process</param>
    /// <param name="targetsToReplacements">Dictionary mapping words to find to their replacement strings</param>
    /// <returns>The string with all specified words replaced</returns>
    public static string ReplaceWords(this string str, Dictionary<string, string> targetsToReplacements)
    {
        string pattern = string.Join("|", targetsToReplacements.Keys.ToArray());
        str = Regex.Replace(str, pattern, match =>
        {
            return targetsToReplacements[match.Value];
        });
        return str;
    }
}
