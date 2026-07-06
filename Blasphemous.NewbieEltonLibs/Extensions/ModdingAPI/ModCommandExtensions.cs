using Blasphemous.CheatConsole;
using Gameplay.UI.Widgets;
using HarmonyLib;
using System.Linq;
using System.Text;

namespace Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;

/// <summary>
/// Extension methods for ModCommand
/// </summary>
public static class ModCommandExtensions
{
    /// <summary>
    /// Gets the ConsoleWidget instance associated with this ModCommand
    /// </summary>
    public static ConsoleWidget GetConsoleWidget(this ModCommand command)
    {
        return Traverse.Create(command).Field("console").GetValue<ConsoleWidget>();
    }

    /// <summary>
    /// Validates that the parameter count matches one of the valid lengths, writing an error message if not
    /// </summary>
    /// <param name="modCommand">The mod command instance</param>
    /// <param name="parameters">The array of parameters to validate</param>
    /// <param name="validParameterLengths">One or more valid parameter lengths</param>
    /// <returns>True if the parameter count is valid, false otherwise</returns>
    public static bool ValidateParameterList(this ModCommand modCommand, string[] parameters, params int[] validParameterLengths)
    {
        if (!validParameterLengths.Contains(parameters.Length))
        {
            StringBuilder sb = new();
            sb.Append($"This command takes ");
            for (int i = 0; i < validParameterLengths.Length; i++)
            {
                sb.Append($"{validParameterLengths[i]} ");
                if (i != validParameterLengths.Length - 1)
                    sb.Append("or ");
            }
            sb.Append($"parameters.  You passed {parameters.Length}");
            modCommand.GetConsoleWidget().Write(sb.ToString());

            return false;
        }

        return true;
    }
}
