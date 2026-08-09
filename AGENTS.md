# AGENTS.md

Technical overview of `Blasphemous.NewbieEltonLibs` for AI agents and contributors. Read this before modifying the codebase.

## What this is

A **class library** (not a mod) that provides reusable APIs for developing Blasphemous 1 mods. Mods reference it as a NuGet package and call its extension methods / base classes. There is no `BlasMod` entry point here — mod structure belongs to the consuming mod.

- Target: `net35` (game is Unity engine on .NET 3.5), `LangVersion=latest`, `Nullable=enable`
- Packages: `Blasphemous.GameLibs`, `Blasphemous.ModdingAPI`, `Blasphemous.CheatConsole` (from `nuget.bepinex.dev` + nuget.org, see `nuget.config`)
- Build copies the produced DLL into `publish/` (csproj `Development` target, `AfterTargets=Build`)
- CI (`.github/workflows/build.yml`): builds Release, drafts a GitHub release with `publish/<name>.dll`, pushes nupkg to nuget.org
- Solutions: both `Blasphemous.NewbieEltonLibs.sln` (VS 2022 format) and `.slnx` exist — keep them in sync

## Repository layout

```
Blasphemous.NewbieEltonLibs/
├── Extensions/
│   ├── GameLibs/       # extensions over game types (Unity/Blasphemous internals)
│   ├── ModdingAPI/     # extensions over ModdingAPI framework types
│   └── System/         # extensions over plain .NET types
├── CheatConsole/       # console-command abstraction layer (AutoModCommand)
├── GlobalUsings.cs     # global type aliases (BlasCollectibleItem, I2LocManager, UObject)
└── Blasphemous.NewbieEltonLibs.csproj
```

## Architecture

All public API is **static extension classes** except the `CheatConsole` layer, which provides a base class. Everything is XML-doc'd. Common pattern: access game private members via `TraverseUtils` (Harmony Traverse wrapper) instead of reflection.

### Extensions/GameLibs — game-type extensions

| File | Provides |
|------|----------|
| `TraverseUtils.cs` | Core utility: `GetValue`/`SetValue` on fields/properties regardless of accessibility, `SetValueIfValidated`, `SetValueIfNotNull`, `Validate` (logs or throws). Foundation for most other extensions |
| `EnemyHealthBarExtensions.cs` | `EnemyHealthBar.GetOwner()`, `BossHealth.GetTarget()`, `UIController.GetBossHealth()` |
| `I2Extensions.cs` | Low-level I2 Localize operations: `DoGetSecondaryTranslatedObj<T>`, `DoDeserializeTranslation`, `DoGetObject<T>`, `DoGetTranslatedObject<T>` |
| `InventoryManagerExtensions.cs` | Inventory queries: `GetAllInventoryObjects`, `GetAllOwnedInventoryObjects`, `GetAll/GetOwned...OfType<T>` (generic or `ItemType`), `GetInventoryItemFromId`/`TryGetInventoryItemFromId`, `GetItemTypeFromId`/`TryGetItemTypeFromId`. **ID prefix rule: RE=Relic, RB=Bead, QI=Quest, PR=Prayer, CO=Collectible, HE=Sword** |
| `NewInventoryWidgetExtensions.cs` | `NewInventory_LayoutGrid.SetLastSlotSelected` (clamped), `NewInventoryWidget.Get_currentLayout` |
| `UnityExtensions.cs` | `GetOrElseAddComponent<T>`, `ChangeAlphaTo`, `GetHierarchy`, `StartCoroutineSafe`, `TryStartCoroutine` |

### Extensions/ModdingAPI — framework extensions

| File | Provides |
|------|----------|
| `ConfigHandlerExtensions.cs` | `Load<T>`/`Save<T>` with optional `JsonSerializerSettings`; auto-creates default config file on first load |
| `FileHandlerExtensions.cs` | Paths (`GetDataPath`/`GetConfigPath`), `GetAllDataFileNames`, `LoadDataAsJson` (with/without settings), `LoadContentAsJson`, `WriteJsonToContent`, `LoadDataAsAssetBundle` |
| `InputHandlerExtensions.cs` | `TryGetKeybinding`, `GetAllKeybindings`, `GetAxisDown` (edge-detected axis via Traverse on `_keybindings`) |
| `LocalizationHandlerExtensions.cs` | `Localize(key, languageName)` — looks up key in target language, falls back to default language, else logs and returns `"LOC_ERROR"` |
| `ModCommandExtensions.cs` | `GetConsoleWidget` (Traverse into private `console`), `ValidateParameterList` (multi-length param validation with unified error wording) |
| `ModLogExtensions.cs` | `Info/Warn/Error/Fatal/Debug/DisplayIfDebugBuild(message[, mod])` — logs only when the calling (or mod's) assembly is a Debug build (detected via `DebuggableAttribute`) |

### Extensions/System — .NET extensions

`SystemExtensions.cs`: `List<T>.Move(oldIndex, newIndex)`, `Enum.GetNextEnumValue<T>(stepLength)` (wraps around), `string.ReplaceWords(Dictionary<string,string>)` (regex multi-word replace).

### CheatConsole — command abstraction layer

Designed so mods can add game-console commands without writing `AddSubCommands()` boilerplate. Depends on the `Blasphemous.CheatConsole` package (upstream `ModCommand` — **do not modify upstream; it is only cloned for reference**).

- `ModSubCommandAttribute` — `[AttributeUsage(AttributeTargets.Method, AllowMultiple=true)]`, ctor `(name, description, usage = null, params validLengths)`. Multiple attributes on one method = aliases.
- `AutoModCommand : ModCommand` — subclasses only implement `CommandName`; everything else is automatic:
  - Scans `GetType().GetMethods(Instance|Public|NonPublic)` for methods marked `[ModSubCommand]`, binds them via `Delegate.CreateDelegate` (net35-safe)
  - **Fail fast** (`InvalidOperationException`): duplicate sub-command names, non-`void(string[])` signature, static methods
  - Auto-registers `help` (listed first, others alphabetical; `usage` falls back to the name); subclasses may override `help` via attribute or `AddCustomSubCommands()`
  - `validLengths` set → automatic parameter-count validation reusing `ModCommandExtensions.ValidateParameterList` wording; absent → no validation
  - `AllowUppercase` defaults to `true` (names matched case-sensitively), override to change
  - `protected virtual AddCustomSubCommands()` extension point for hand-written entries (do not appear in auto help; key `help` overrides the generated one)

## Development conventions

- **net35 compatibility is mandatory.** Avoid APIs newer than .NET 3.5 at runtime (e.g. `Array.Empty<T>`, generic `GetCustomAttribute<T>`, `Span<T>`). C# language features are fine (`LangVersion=latest`) — the compiler lowers them.
- **Never use reflection directly** for private game members — wrap it in `TraverseUtils` or an extension class, so call sites stay clean and null-failures log via `ModLog`.
- Private members are accessed through `Harmony Traverse` (`TraverseUtils.GetValue/SetValue`), not `FieldInfo`.
- Every public member needs an XML doc comment (`<summary>`).
- Extension class naming: `{TargetType}Extensions.cs` under the matching `Extensions/` sub-namespace.
- Global aliases live in `GlobalUsings.cs` (currently `BlasCollectibleItem`, `I2LocManager`, `UObject`). Add aliases there, not in files.
- Build locally: `dotnet build Blasphemous.NewbieEltonLibs.sln -c Debug`. Output DLL is copied to `publish/` automatically.
- Prefer `TryXxx` variants over throwing in library code (see `TryGetInventoryItemFromId`, `TryGetKeybinding`).

## Common tasks

- **Add an extension method**: create/append to `Extensions/<area>/<Type>Extensions.cs`, use `TraverseUtils` for private state, XML-doc it.
- **Add a console command for a mod**: subclass `AutoModCommand`, implement `CommandName`, mark handler methods `[ModSubCommand("name", "description", "[usage]", n)]`; register via `ModServiceProvider.RegisterCommand(...)` in the consuming mod.
- **Add a NuGet dependency**: edit the csproj `PackageReference`; restore pulls from nuget.org + BepInEx feeds.
