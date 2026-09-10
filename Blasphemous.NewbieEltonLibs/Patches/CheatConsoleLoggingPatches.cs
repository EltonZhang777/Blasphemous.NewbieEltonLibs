using Gameplay.UI.Widgets;
using HarmonyLib;
using Sirenix.Utilities;

namespace Blasphemous.NewbieEltonLibs.CheatConsole;

[HarmonyPatch(typeof(ConsoleWidget), "Submit")]
internal static class ConsoleWidget_Submit_CheatConsoleInput_Patch
{
    [HarmonyPrefix]
    private static void Prefix(ConsoleWidget __instance)
    {
        if (!CheatConsoleLogging.IsInputLoggingActive()
            || __instance == null
            || __instance.input == null)
            return;

        string command = __instance.input.text;
        if (command.IsNullOrWhitespace())
            return;

        CheatConsoleLogging.LogInput(command);
    }
}

[HarmonyPatch(typeof(ConsoleWidget), "Write", new[] { typeof(string) })]
internal static class ConsoleWidget_Write_CheatConsoleOutput_Patch
{
    [HarmonyPostfix]
    private static void Postfix(string message)
    {
        CheatConsoleLogging.LogOutput(message);
    }
}
