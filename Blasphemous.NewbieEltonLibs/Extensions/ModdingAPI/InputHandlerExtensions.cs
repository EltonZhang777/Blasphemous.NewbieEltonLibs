using Blasphemous.ModdingAPI.Input;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using System.Collections.Generic;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;

/// <summary>
/// Extension methods for InputHandler
/// </summary>
public static class InputHandlerExtensions
{
    /// <summary>
    /// Tries to get the KeyCode for a named keybinding
    /// </summary>
    public static bool TryGetKeybinding(this InputHandler inputHandler, string keybindName, out KeyCode keyCode)
    {
        keyCode = KeyCode.None;
        Dictionary<string, KeyCode> keybindings = GetAllKeybindings(inputHandler);
        return keybindings != null && keybindings.TryGetValue(keybindName, out keyCode);
    }

    /// <summary>
    /// Gets all keybindings as a dictionary of name to KeyCode
    /// </summary>
    public static Dictionary<string, KeyCode> GetAllKeybindings(this InputHandler inputHandler)
    {
        return TraverseUtils.GetValue<Dictionary<string, KeyCode>>(inputHandler, "_keybindings");
    }

    /// <summary>
    /// Checks the direction of the axis that is pressed (i.e. switched to) on this frame
    /// </summary>
    public static int GetAxisDown(this InputHandler inputHandler, AxisCode axis, bool useRawInput)
    {
        int currentAxis = FloatToIntAxis(inputHandler.GetAxis(axis, useRawInput));
        int previousAxis = FloatToIntAxis(inputHandler.GetAxisPrevious(axis, useRawInput));
        return currentAxis == previousAxis
            ? 0
            : currentAxis;

        int FloatToIntAxis(float axis)
        {
            if (axis > Mathf.Epsilon)
                return 1;
            if (axis < -Mathf.Epsilon)
                return -1;
            return 0;
        }
    }
}
