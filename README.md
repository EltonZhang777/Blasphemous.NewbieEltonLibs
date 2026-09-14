# Blasphemous.NewbieEltonLibs

Reusable library of APIs for developing **Blasphemous 1** mods.

This project is a prerequisite class library, not a standalone BepInEx mod. It does not own a mod entry point or lifecycle; consuming mods reference it and provide their own entry point.

## Installation and requirements

Reference the NuGet package (published to nuget.org) in your mod project:

```xml
<PackageReference Include="Blasphemous.NewbieEltonLibs" Version="0.3.0" />
```

The library targets .NET Framework 3.5. All four packages below are hard compile-time references. At runtime, the selected game profile supplies the framework and game assemblies. `Blasphemous.ModdingAPI` and `Blasphemous.GameLibs` are baseline dependencies, while `Blasphemous.CheatConsole` and `Blasphemous.Framework.Levels` are feature-gated runtime soft dependencies required when the consuming mod executes APIs that reference those packages.

| Package | Tested version | Runtime role |
| --- | --- | --- |
| `Blasphemous.ModdingAPI` | `3.0.1` | Baseline game-side dependency |
| `Blasphemous.GameLibs` | `4.0.67` | Baseline game-side dependency |
| `Blasphemous.CheatConsole` | `1.1.0` | Feature-gated soft dependency for CheatConsole APIs |
| `Blasphemous.Framework.Levels` | `0.1.4` | Feature-gated soft dependency for Level Framework APIs |

## Public API

- **CheatConsole commands and logging** — `CheatConsoleLogging` and `LogLevel` provide opt-in console input/output logging; `AutoModCommand` and `ModSubCommandAttribute` provide attribute-based mod command registration.
- **Mod-owned flags** — `ModFlagsManager` provides owner-scoped boolean flags backed by the vanilla event system.
- **Runtime resource storage** — `SpriteStorage`, `AnimationStorage`, `SpriteImportInfo`, `AnimationInfo`, and `AnimationImportInfo` organize per-mod sprite and animation resources plus import metadata.
- **Components** — `ModAnimator` displays stored animation data, and `ItemCollection<T>` collects matching public fields as items.
- **Serialization** — `SerializableVector3`, `UnityEngineIgnoreConverter`, and `JsonSerializerSettingsFactory` provide Unity-value and JSON serialization helpers.
- **ModdingAPI extensions** — `ConfigHandlerExtensions`, `FileHandlerExtensions`, `InputHandlerExtensions`, `LocalizationHandlerExtensions`, `ModCommandExtensions`, and `ModLogExtensions` cover configuration, files/JSON, input, localization, commands, and logging.
- **GameLibs extensions** — `InventoryManagerExtensions`, `I2Extensions`, `UnityExtensions`, `EnemyHealthBarExtensions`, `NewInventoryWidgetExtensions`, `EntityOrientationExtensions`, and `TraverseUtils` cover inventory, translation, Unity, enemy/boss/UI, traversal, and orientation helpers.
- **System and validation** — `ValidationUtils` and `SystemExtensions` provide general validation and .NET utility helpers.

The storage APIs are per-mod registries for caller-owned resources; they do not introduce a process-wide registry or an automatic importer. Mod-owned flags use the vanilla event system rather than a second persistence store.

## Verification and known limitations

- The accepted non-AssetBundle S1–S4 public API scope has automated, external-consumer, and tracked real-game coverage. 
- The AssetBundle success path remains explicitly deferred to [issue #60](https://github.com/EltonZhang777/Blasphemous.NewbieEltonLibs/issues/60)
- `ModAnimator` retains its known last-frame skip behavior.

## Development

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test Blasphemous.NewbieEltonLibs.Tests\Blasphemous.NewbieEltonLibs.Tests.csproj --configuration Release
.\dotnet_format.bat
```

The production DLL is copied to `publish/` after a build. `Blasphemous.NewbieEltonLibs.TestMod` is verification-only; its packaging and deployment contract is documented in [`docs/verification/testmod-build.md`](docs/verification/testmod-build.md).

## License

MIT, see [LICENSE](LICENSE).
