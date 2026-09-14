using System;

namespace Blasphemous.NewbieEltonLibs.CheatConsole;

/// <summary>
/// Marks a method as a sub-command of an <see cref="AutoModCommand"/>.
/// Apply one attribute per sub-command name; multiple attributes on the same
/// method register aliases for the same handler.
/// </summary>
/// <remarks>
/// Declares a sub-command for an <see cref="AutoModCommand"/>.
/// </remarks>
/// <param name="name">Sub-command name as typed in the console</param>
/// <param name="description">Short description shown in the auto-generated help</param>
/// <param name="usage">Usage placeholder shown in help; defaults to <paramref name="name"/></param>
/// <param name="validLengths">Allowed parameter counts; omit to skip automatic validation</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ModSubCommandAttribute(string name, string description, string? usage = null, params int[]? validLengths) : Attribute
{
    /// <summary>
    /// The name of the sub-command as typed in the console.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Short description of the sub-command, shown in the auto-generated help.
    /// </summary>
    public string Description { get; } = description;

    /// <summary>
    /// Usage placeholder shown in the auto-generated help (e.g. <c>[patchName]</c>).
    /// Null falls back to <see cref="Name"/>.
    /// </summary>
    public string? Usage { get; } = usage;

    /// <summary>
    /// Allowed parameter counts for automatic validation. Empty (default) disables validation.
    /// </summary>
    public int[] ValidLengths { get; } = validLengths ?? [];
}