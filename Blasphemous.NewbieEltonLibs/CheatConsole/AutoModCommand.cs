using Blasphemous.CheatConsole;
using Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Blasphemous.NewbieEltonLibs.CheatConsole;

/// <summary>
/// A <see cref="ModCommand"/> that auto-registers sub-commands from methods marked with
/// <see cref="ModSubCommandAttribute"/> and auto-generates the <c>help</c> sub-command.
/// Sub-classes only need to provide <see cref="ModCommand.CommandName"/>; everything else
/// (registration, help output, optional parameter validation) is handled here.
/// </summary>
public abstract class AutoModCommand : ModCommand
{
    private List<SubCommandInfo> subCommandInfos = null!;

    /// <summary>
    /// Whether to keep uppercase letters in sub-command names. Defaults to <c>true</c>
    /// (names are matched case-sensitively); override to change.
    /// </summary>
    protected override bool AllowUppercase => true;

    /// <summary>
    /// Builds the sub-command registry: scans the declaring type for methods marked with
    /// <see cref="ModSubCommandAttribute"/>, auto-registers <c>help</c> unless overridden,
    /// then merges hand-written sub-commands from <see cref="AddCustomSubCommands"/>.
    /// </summary>
    protected override Dictionary<string, Action<string[]>> AddSubCommands()
    {
        Dictionary<string, Action<string[]>> commands = [];
        List<SubCommandInfo> infos = [];

        // 1. scan attribute-declared sub-commands
        foreach (MethodInfo method in GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            object[] attributes = method.GetCustomAttributes(typeof(ModSubCommandAttribute), false);
            if (attributes.Length == 0)
                continue;

            ValidateMethod(method);
            Action<string[]> action = (Action<string[]>)Delegate.CreateDelegate(typeof(Action<string[]>), this, method);

            foreach (ModSubCommandAttribute attribute in attributes.Cast<ModSubCommandAttribute>())
            {
                string name = attribute.Name;
                if (string.IsNullOrEmpty(name))
                    throw new InvalidOperationException($"Sub-command name cannot be empty on method '{method.Name}' of '{GetType().FullName}'");

                if (commands.ContainsKey(name))
                    throw new InvalidOperationException($"Duplicate sub-command '{name}' on '{GetType().FullName}'");

                commands.Add(name, WrapValidation(action, attribute.ValidLengths));
                infos.Add(new SubCommandInfo(name, attribute.Usage, attribute.Description));
            }
        }

        // 2. auto-register help if not overridden by a sub-class
        if (!commands.ContainsKey("help"))
        {
            commands.Add("help", WrapValidation(SubCommand_Help, [0]));
            infos.Add(new SubCommandInfo("help", null, "show available sub-commands"));
        }

        // 3. merge hand-written sub-commands (may override help)
        Dictionary<string, Action<string[]>> custom = AddCustomSubCommands();
        if (custom != null)
        {
            foreach (KeyValuePair<string, Action<string[]>> pair in custom)
            {
                if (commands.ContainsKey(pair.Key) && pair.Key != "help")
                    throw new InvalidOperationException($"Duplicate sub-command '{pair.Key}' on '{GetType().FullName}'");
                commands[pair.Key] = pair.Value;
            }
        }

        // help first, then alphabetical by name
        infos.Sort((a, b) =>
        {
            bool aHelp = a.Name == "help", bHelp = b.Name == "help";
            if (aHelp != bHelp)
                return aHelp ? -1 : 1;
            return string.CompareOrdinal(a.Name, b.Name);
        });

        subCommandInfos = infos;
        return commands;
    }

    /// <summary>
    /// Optional extension point for hand-written sub-commands that are not declared via
    /// <see cref="ModSubCommandAttribute"/>. Entries returned here are merged into the
    /// registry after attribute-declared ones; the key <c>help</c> may be used to override
    /// the auto-generated help. Hand-written entries do not appear in the auto-generated
    /// help list (they have no description metadata).
    /// </summary>
    protected virtual Dictionary<string, Action<string[]>> AddCustomSubCommands()
    {
        return [];
    }

    /// <summary>
    /// Auto-generated <c>help</c> sub-command: lists every attribute-declared sub-command,
    /// help first, then alphabetically.
    /// </summary>
    private void SubCommand_Help(string[] parameters)
    {
        Write($"Available {CommandName} commands:");
        foreach (SubCommandInfo info in subCommandInfos)
        {
            string line = (info.Usage == null || info.Usage == info.Name)
                ? $"{info.Name}"
                : $"{info.Name} {info.Usage}";
            Write($"{CommandName} {line} : {info.Description}");
        }
    }

    private void ValidateMethod(MethodInfo method)
    {
        if (method.IsStatic)
            throw new InvalidOperationException($"Sub-command method '{method.Name}' on '{GetType().FullName}' must be an instance method");

        if (method.ReturnType != typeof(void))
            throw new InvalidOperationException($"Sub-command method '{method.Name}' on '{GetType().FullName}' must return void");

        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length != 1
            || parameters[0].ParameterType != typeof(string[])
            || parameters[0].IsOut
            || parameters[0].ParameterType.IsByRef)
        {
            throw new InvalidOperationException($"Sub-command method '{method.Name}' on '{GetType().FullName}' must take a single string[] parameter");
        }
    }

    /// <summary>
    /// Wraps a sub-command handler with automatic parameter-count validation when
    /// <paramref name="validLengths"/> is non-empty.
    /// </summary>
    private Action<string[]> WrapValidation(Action<string[]> action, int[] validLengths)
    {
        if (validLengths == null || validLengths.Length == 0)
            return action;

        return parameters =>
        {
            if (!this.ValidateParameterList(parameters, validLengths))
                return;
            action(parameters);
        };
    }

    private sealed class SubCommandInfo(string name, string? usage, string description)
    {
        public string Name = name;
        public string? Usage = usage;
        public string Description = description;
    }
}