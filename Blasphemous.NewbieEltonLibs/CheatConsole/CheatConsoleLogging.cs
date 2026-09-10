using Blasphemous.ModdingAPI;
using Gameplay.UI.Widgets;
using HarmonyLib;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Blasphemous.NewbieEltonLibs.CheatConsole;

/// <summary>
/// Log levels supported by <see cref="ModLog"/>.
/// </summary>
public enum LogLevel
{
    /// <summary>Logs at info level.</summary>
    Info,

    /// <summary>Logs at warning level.</summary>
    Warn,

    /// <summary>Logs at error level.</summary>
    Error,

    /// <summary>Logs at fatal level.</summary>
    Fatal,

    /// <summary>Logs at debug level.</summary>
    Debug,

    /// <summary>Logs at display level.</summary>
    Display
}

/// <summary>
/// Controls optional logging of submitted cheat-console commands and visible output.
/// </summary>
public static class CheatConsoleLogging
{
    private const string HarmonyId = "Blasphemous.NewbieEltonLibs.CheatConsoleLogging";
    private const string InputPrefix = "[CheatConsole Input] ";
    private const string OutputPrefix = "[CheatConsole Output] ";

    private static bool inputActive;
    private static LogLevel inputLogLevel = LogLevel.Info;
    private static bool inputDebugBuildOnly = true;
    private static bool inputCallerIsDebugBuild;

    private static bool outputActive;
    private static LogLevel outputLogLevel = LogLevel.Info;
    private static bool outputDebugBuildOnly = true;
    private static bool outputCallerIsDebugBuild;

    private static bool patchApplied;

    /// <summary>
    /// Enables or disables logging of submitted cheat-console commands.
    /// </summary>
    /// <param name="active">Whether command logging is active.</param>
    /// <param name="logLevel">The BepInEx log level to use.</param>
    /// <param name="debugBuildOnly">Whether to log only when the calling assembly is a Debug build.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogCheatConsoleInput(bool active, LogLevel logLevel = LogLevel.Info, bool debugBuildOnly = true)
    {
        ValidateLogLevel(logLevel);
        inputActive = active;
        inputLogLevel = logLevel;
        inputDebugBuildOnly = debugBuildOnly;
        inputCallerIsDebugBuild = IsAssemblyDebugBuild(Assembly.GetCallingAssembly());
        EnsurePatched();
    }

    /// <summary>
    /// Enables or disables logging of lines written to the cheat console.
    /// </summary>
    /// <param name="active">Whether console output logging is active.</param>
    /// <param name="logLevel">The BepInEx log level to use.</param>
    /// <param name="debugBuildOnly">Whether to log only when the calling assembly is a Debug build.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogCheatConsoleOutput(bool active, LogLevel logLevel = LogLevel.Info, bool debugBuildOnly = true)
    {
        ValidateLogLevel(logLevel);
        outputActive = active;
        outputLogLevel = logLevel;
        outputDebugBuildOnly = debugBuildOnly;
        outputCallerIsDebugBuild = IsAssemblyDebugBuild(Assembly.GetCallingAssembly());
        EnsurePatched();
    }

    private static void EnsurePatched()
    {
        if (patchApplied)
            return;

        try
        {
            new Harmony(HarmonyId).PatchAll(typeof(CheatConsoleLogging).Assembly);
            patchApplied = true;
        }
        catch (Exception exception)
        {
            ModLog.Error($"Failed to install cheat-console logging patches: {exception}");
        }
    }

    private static void ValidateLogLevel(LogLevel logLevel)
    {
        if (!Enum.IsDefined(typeof(LogLevel), logLevel))
            throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, "Unsupported cheat-console log level.");
    }

    private static bool IsAssemblyDebugBuild(Assembly assembly)
    {
        DebuggableAttribute? attribute = (DebuggableAttribute?)Attribute.GetCustomAttribute(assembly, typeof(DebuggableAttribute));
        return attribute != null && attribute.IsJITOptimizerDisabled;
    }

    private static void Log(LogLevel logLevel, object message)
    {
        switch (logLevel)
        {
            case LogLevel.Info:
                ModLog.Info(message);
                break;
            case LogLevel.Warn:
                ModLog.Warn(message);
                break;
            case LogLevel.Error:
                ModLog.Error(message);
                break;
            case LogLevel.Fatal:
                ModLog.Fatal(message);
                break;
            case LogLevel.Debug:
                ModLog.Debug(message);
                break;
            case LogLevel.Display:
                ModLog.Display(message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, "Unsupported cheat-console log level.");
        }
    }

    private static bool ShouldLog(bool active, bool debugBuildOnly, bool callerIsDebugBuild)
    {
        return active && (!debugBuildOnly || callerIsDebugBuild);
    }

    [HarmonyPatch(typeof(ConsoleWidget), nameof(ConsoleWidget.ProcessCommand))]
    private static class ProcessCommandPatch
    {
        [HarmonyPrefix]
        private static void Prefix(string command)
        {
            if (ShouldLog(inputActive, inputDebugBuildOnly, inputCallerIsDebugBuild))
                Log(inputLogLevel, InputPrefix + command);
        }
    }

    [HarmonyPatch(typeof(ConsoleWidget), nameof(ConsoleWidget.Write))]
    private static class WritePatch
    {
        [HarmonyPostfix]
        private static void Postfix(string text)
        {
            if (ShouldLog(outputActive, outputDebugBuildOnly, outputCallerIsDebugBuild))
                Log(outputLogLevel, OutputPrefix + text);
        }
    }
}
