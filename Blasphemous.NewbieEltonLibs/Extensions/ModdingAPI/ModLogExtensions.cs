using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Helpers;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;

/// <summary>
/// Static helper methods for conditionally logging messages only in debug builds
/// </summary>
public static class ModLogExtensions
{
    private static bool IsAssemblyDebugBuild(Assembly assembly)
    {
        DebuggableAttribute attr = (DebuggableAttribute)Attribute.GetCustomAttribute(assembly, typeof(DebuggableAttribute));
        return attr != null && attr.IsJITOptimizerDisabled;
    }

    private static bool TryGetCallingMod(Assembly callingAssembly, out BlasMod mod)
    {
        if (ModHelper.LoadedMods == null)
        {
            mod = null!;
            return false;
        }

        return ModHelper.TryGetMod(loadedMod => loadedMod.GetType().Assembly == callingAssembly, out mod);
    }

    private static void LogIfDebugBuild(
        object message,
        Assembly callingAssembly,
        Action<object> log,
        Action<object, BlasMod> logWithMod)
    {
        if (!IsAssemblyDebugBuild(callingAssembly))
            return;

        string debugMessage = "[DEBUG] " + message;
        if (TryGetCallingMod(callingAssembly, out BlasMod mod))
            logWithMod(debugMessage, mod);
        else
            log(debugMessage);
    }

    /// <summary>
    /// Logs an Info message only when the calling assembly is a debug build
    /// </summary>
    public static void InfoIfDebugBuild(object message)
    {
        Assembly callingAssembly = Assembly.GetCallingAssembly();
        LogIfDebugBuild(message, callingAssembly, ModLog.Info, ModLog.Info);
    }

    /// <summary>
    /// Logs an Info message only when the specified mod's assembly is a debug build
    /// </summary>
    public static void InfoIfDebugBuild(object message, BlasMod mod)
    {
        if (IsAssemblyDebugBuild(mod.GetType().Assembly))
            ModLog.Info("[DEBUG] " + message, mod);
    }

    /// <summary>
    /// Logs a Warn message only when the calling assembly is a debug build
    /// </summary>
    public static void WarnIfDebugBuild(object message)
    {
        Assembly callingAssembly = Assembly.GetCallingAssembly();
        LogIfDebugBuild(message, callingAssembly, ModLog.Warn, ModLog.Warn);
    }

    /// <summary>
    /// Logs a Warn message only when the specified mod's assembly is a debug build
    /// </summary>
    public static void WarnIfDebugBuild(object message, BlasMod mod)
    {
        if (IsAssemblyDebugBuild(mod.GetType().Assembly))
            ModLog.Warn("[DEBUG] " + message, mod);
    }

    /// <summary>
    /// Logs an Error message only when the calling assembly is a debug build
    /// </summary>
    public static void ErrorIfDebugBuild(object message)
    {
        Assembly callingAssembly = Assembly.GetCallingAssembly();
        LogIfDebugBuild(message, callingAssembly, ModLog.Error, ModLog.Error);
    }

    /// <summary>
    /// Logs an Error message only when the specified mod's assembly is a debug build
    /// </summary>
    public static void ErrorIfDebugBuild(object message, BlasMod mod)
    {
        if (IsAssemblyDebugBuild(mod.GetType().Assembly))
            ModLog.Error("[DEBUG] " + message, mod);
    }

    /// <summary>
    /// Logs a Fatal message only when the calling assembly is a debug build
    /// </summary>
    public static void FatalIfDebugBuild(object message)
    {
        Assembly callingAssembly = Assembly.GetCallingAssembly();
        LogIfDebugBuild(message, callingAssembly, ModLog.Fatal, ModLog.Fatal);
    }

    /// <summary>
    /// Logs a Fatal message only when the specified mod's assembly is a debug build
    /// </summary>
    public static void FatalIfDebugBuild(object message, BlasMod mod)
    {
        if (IsAssemblyDebugBuild(mod.GetType().Assembly))
            ModLog.Fatal("[DEBUG] " + message, mod);
    }

    /// <summary>
    /// Logs a Debug message only when the calling assembly is a debug build
    /// </summary>
    public static void DebugIfDebugBuild(object message)
    {
        Assembly callingAssembly = Assembly.GetCallingAssembly();
        LogIfDebugBuild(message, callingAssembly, ModLog.Debug, ModLog.Debug);
    }

    /// <summary>
    /// Logs a Debug message only when the specified mod's assembly is a debug build
    /// </summary>
    public static void DebugIfDebugBuild(object message, BlasMod mod)
    {
        if (IsAssemblyDebugBuild(mod.GetType().Assembly))
            ModLog.Debug("[DEBUG] " + message, mod);
    }

    /// <summary>
    /// Logs a Display message only when the calling assembly is a debug build
    /// </summary>
    public static void DisplayIfDebugBuild(object message)
    {
        Assembly callingAssembly = Assembly.GetCallingAssembly();
        LogIfDebugBuild(message, callingAssembly, ModLog.Display, ModLog.Display);
    }

    /// <summary>
    /// Logs a Display message only when the specified mod's assembly is a debug build
    /// </summary>
    public static void DisplayIfDebugBuild(object message, BlasMod mod)
    {
        if (IsAssemblyDebugBuild(mod.GetType().Assembly))
            ModLog.Display("[DEBUG] " + message, mod);
    }
}