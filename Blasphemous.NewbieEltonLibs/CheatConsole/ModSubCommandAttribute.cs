using System;

namespace Blasphemous.NewbieEltonLibs.CheatConsole;

/// <summary>
/// Marks a method as a sub-command of an <see cref="AutoModCommand"/>.
/// Apply one attribute per sub-command name; multiple attributes on the same
/// method register aliases for the same handler.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ModSubCommandAttribute : Attribute
{
    /// <summary>
    /// The name of the sub-command as typed in the console.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Short description of the sub-command, shown in the auto-generated help.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Usage placeholder shown in the auto-generated help (e.g. <c>[patchName]</c>).
    /// Null falls back to <see cref="Name"/>.
    /// </summary>
    public string? Usage { get; }

    /// <summary>
    /// Allowed parameter counts for automatic validation. Empty (default) disables validation.
    /// </summary>
    public int[] ValidLengths { get; }

    /// <summary>
    /// Declares a sub-command for an <see cref="AutoModCommand"/>.
    /// </summary>
    /// <param name="name">Sub-command name as typed in the console</param>
    /// <param name="description">Short description shown in the auto-generated help</param>
    /// <param name="usage">Usage placeholder shown in help; defaults to <paramref name="name"/></param>
    /// <param name="validLengths">Allowed parameter counts; omit to skip automatic validation</param>
    public ModSubCommandAttribute(string name, string description, string? usage = null, params int[]? validLengths)
    {
        Name = name;
        Description = description;
        Usage = usage;
        ValidLengths = validLengths ?? [];
    }
}
