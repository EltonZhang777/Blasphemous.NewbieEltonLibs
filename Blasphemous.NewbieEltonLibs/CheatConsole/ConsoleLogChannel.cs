namespace Blasphemous.NewbieEltonLibs.CheatConsole;

internal sealed class ConsoleLogChannel
{
    internal bool Active { get; private set; }

    internal LogLevel LogLevel { get; private set; } = LogLevel.Info;

    internal bool DebugBuildOnly { get; private set; } = true;

    internal bool CallerIsDebugBuild { get; private set; }

    internal void Configure(bool active, LogLevel logLevel, bool debugBuildOnly, bool callerIsDebugBuild)
    {
        Active = active;
        LogLevel = logLevel;
        DebugBuildOnly = debugBuildOnly;
        CallerIsDebugBuild = callerIsDebugBuild;
    }

    internal bool ShouldLog()
    {
        return Active && (!DebugBuildOnly || CallerIsDebugBuild);
    }
}