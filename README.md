# Blasphemous.NewbieEltonLibs

Reusable library of APIs for developing **Blasphemous 1** mods 
- not a mod itself, just a class library your mod references.


## Usage

Reference the NuGet package (published to nuget.org) in your mod project:

```xml
<PackageReference Include="Blasphemous.NewbieEltonLibs" Version="0.1.0" />
```

Requires .NET Framework 3.5 target and the standard Blasphemous mod stack (`Blasphemous.ModdingAPI`, `Blasphemous.CheatConsole`).

## Cheat console logging

Console input and visible output logging are opt-in and configured independently:

```cs
using Blasphemous.NewbieEltonLibs.CheatConsole;

CheatConsoleLogging.LogCheatConsoleInput(true);
CheatConsoleLogging.LogCheatConsoleOutput(true, LogLevel.Warn, debugBuildOnly: false);
```

Both methods default to `LogLevel.Info` and only log from Debug builds unless
`debugBuildOnly` is disabled. Input logging observes complete commands submitted
by the player; output logging observes every line written to the visible console.

## Build

```bash
dotnet restore
dotnet build -c Release
```

The output DLL is copied to `publish/` for direct use in a mod.

## License

MIT, see [LICENSE](LICENSE).
