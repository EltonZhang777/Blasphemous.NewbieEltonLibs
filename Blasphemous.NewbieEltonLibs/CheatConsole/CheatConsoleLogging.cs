using Blasphemous.ModdingAPI;
using Blasphemous.NewbieEltonLibs.HarmonyPatches;
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
    private const string InputPrefix = "[CheatConsole Input] ";
    private const string OutputPrefix = "[CheatConsole Output] ";

    private static readonly ConsoleLogChannel InputChannel = new();
    private static readonly ConsoleLogChannel OutputChannel = new();

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
        InputChannel.Configure(active, logLevel, debugBuildOnly, IsAssemblyDebugBuild(Assembly.GetCallingAssembly()));
        HarmonyPatchInstaller.TryEnsurePatched();
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
        OutputChannel.Configure(active, logLevel, debugBuildOnly, IsAssemblyDebugBuild(Assembly.GetCallingAssembly()));
        HarmonyPatchInstaller.TryEnsurePatched();
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

    internal static void LogConfigured(
        ConsoleLogChannel channel,
        string prefix,
        string message,
        Action<LogLevel, object> log)
    {
        if (channel.ShouldLog())
            log(channel.LogLevel, prefix + message);
    }

    internal static bool IsInputLoggingActive()
    {
        return InputChannel.ShouldLog();
    }

    internal static void LogInput(string command)
    {
        LogConfigured(InputChannel, InputPrefix, command, Log);
    }

    internal static void LogOutput(string text)
    {
        LogConfigured(OutputChannel, OutputPrefix, text, Log);
    }
}