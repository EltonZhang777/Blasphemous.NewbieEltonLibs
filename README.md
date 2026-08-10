# Blasphemous.NewbieEltonLibs

Reusable library of APIs for developing **Blasphemous 1** mods 
- not a mod itself, just a class library your mod references.


## Usage

Reference the NuGet package (published to nuget.org) in your mod project:

```xml
<PackageReference Include="Blasphemous.NewbieEltonLibs" Version="0.1.0" />
```

Requires .NET Framework 3.5 target and the standard Blasphemous mod stack (`Blasphemous.ModdingAPI`, `Blasphemous.CheatConsole`).

## Build

```bash
dotnet restore
dotnet build -c Release
```

The output DLL is copied to `publish/` for direct use in a mod.

## License

MIT, see [LICENSE](LICENSE).
