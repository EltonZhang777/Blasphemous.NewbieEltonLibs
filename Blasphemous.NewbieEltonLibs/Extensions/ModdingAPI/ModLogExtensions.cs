using Blasphemous.ModdingAPI;
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

    /// <summary>
    /// Logs an Info message only when the calling assembly is a debug build
    /// </summary>
    public static void InfoIfDebugBuild(object message)
    {
        if (IsAssemblyDebugBuild(Assembly.GetCallingAssembly()))
            ModLog.Info("[DEBUG] " + message);
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
        if (IsAssemblyDebugBuild(Assembly.GetCallingAssembly()))
            ModLog.Warn("[DEBUG] " + message);
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
        if (IsAssemblyDebugBuild(Assembly.GetCallingAssembly()))
            ModLog.Error("[DEBUG] " + message);
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
        if (IsAssemblyDebugBuild(Assembly.GetCallingAssembly()))
            ModLog.Fatal("[DEBUG] " + message);
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
        if (IsAssemblyDebugBuild(Assembly.GetCallingAssembly()))
            ModLog.Debug("[DEBUG] " + message);
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
        if (IsAssemblyDebugBuild(Assembly.GetCallingAssembly()))
            ModLog.Display("[DEBUG] " + message);
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
