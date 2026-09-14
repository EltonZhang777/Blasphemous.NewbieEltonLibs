# Public API verification coverage

## Purpose

This specification defines the acceptance coverage for the library-owned public API surface. It records the smallest useful checks and their runner boundaries; it does not add test implementation or production code.

The runtime contract remains net35. The test project may target net8 as an external consumer, but a passing net8 test does not replace a net35 build or a real game-bound check.

## Numbering and authority

- `SF-<number>` refers to a user story in [safe-mod-owned-flags.md](safe-mod-owned-flags.md). `SF-4`, `SF-10`, and `SF-12` are the stories called out by the grill.
- `ISSUE-<number>` refers to a GitHub issue, not a story number. `ISSUE-10` covers `ModLogExtensions` identity, `ISSUE-12` covers console logging, and [ISSUE-19](https://github.com/EltonZhang777/Blasphemous.NewbieEltonLibs/issues/19) is the authority for runtime resource storage.
- `S1` through `S4` are verification slices. They are planning labels, not namespaces, assemblies, or implementation layers.

| Slice | Authority | Scope |
| --- | --- | --- |
| S1 | Safe mod-owned flags; `SF-4`, `SF-10`, `SF-12` | Registration, identity, ownership, vanilla adapter, and slot-visible state |
| S2 | `ISSUE-10`, `ISSUE-12` | `ModLogExtensions` identity and cheat-console input/output logging |
| S3 | `ISSUE-19` | Sprite/animation storage, import DTOs, and `ModAnimator` |
| S4 | General public API inventory, including `ISSUE-4` migration coverage | Every remaining library-owned public type and member |

## Runner boundary

| Runner | Acceptance role | Explicit limit |
| --- | --- | --- |
| xUnit | Deterministic public behavior, validation, formatting, collection semantics, and settings | Internal seams may support setup, but do not count as public acceptance by themselves |
| `dotnet run` smoke | External-consumer compilation and runtime smoke, including `ModLog` ownership and public adapters that can run without a live game | It is not a Unity or BepInEx simulation |
| Real Unity/BepInEx manual check | `ConsoleWidget` boundary behavior, `ModAnimator`, and save-slot lifecycle | Do not add a broad framework simulator or duplicate vanilla persistence |

Patch metadata, internal channels, internal registries, and direct private-state setup are auxiliary evidence only. They can explain a failure, but cannot close a public behavior gap.

## S1 — Safe mod-owned flags

Minimum acceptance checks:

- **S1.1 — Formatter**: verify the public `ModFlagsManager.FormatToFlagId` vectors, null behavior, empty input, ASCII-space replacement, casing, and preservation of other separators.
- **S1.2 — Registration and names**: verify local-name-only calls, `<modId>:<localName>` prefixing, invalid identity/name rejection, identical duplicate idempotence, and conflicting duplicate failure.
- **S1.3 — Identity**: verify explicit concrete type precedence, unique calling-assembly inference, and `SF-12` fail-closed behavior for no match or ambiguity.
- **S1.4 — Read semantics**: verify `SF-4`: a registered absent flag fails, while a registered stored `false` succeeds with `value == false`.
- **S1.5 — Ownership and writes**: verify an owner can use `TrySetFlag` and `TryGetFlag`, another mod cannot, raw full IDs cannot bypass the local-name boundary, and the registered NG+ policy is forwarded.
- **S1.6 — Vanilla state and lifecycle**: verify a vanilla event mutation is visible through the safe API and that slot load/reset does not leave stale registration ownership. This check must use the real adapter/game lifecycle; it must not reimplement vanilla persistence.

S1.1–S1.5 belong in xUnit or the external-consumer smoke path according to constructibility. S1.6 is a focused real-game check when the referenced game types are available.

## S2 — Console logging and `ModLog` identity

Minimum acceptance checks:

- **S2.1 — Public configuration**: both input and output APIs default to active `Info` with debug-build-only enabled; all six `LogLevel` values are accepted; invalid enum values fail before partial configuration.
- **S2.2 — Channel state**: input and output are independent, disabling takes effect immediately, and a later configuration call stores the latest settings even while disabled.
- **S2.3 — Caller identity**: an external consumer exercises parameterless and explicit `ModLogExtensions` overloads, including every log level required by `ISSUE-10`; a registered mod is attributed to its own identity and an unregistered caller follows the documented `Unknown mod` fallback.
- **S2.4 — Console boundary**: a real `ConsoleWidget.Submit()` logs one complete nonblank player command with the input prefix; `Write(string)` logs every visible line with the output prefix, including built-in/mod/help/error/validation output; the channels remain independent.
- **S2.5 — Non-input and preservation behavior**: programmatic command processing does not create an input event, release/debug caller gating behaves as documented, and enabling observation does not change command processing or visible output.

S2.1–S2.3 are deterministic or external-consumer checks. S2.4–S2.5 require a controlled sink at the real boundary or a focused real Unity/BepInEx manual check; patch attributes alone do not satisfy them.

## S3 — Runtime resource storage and animation

Minimum acceptance checks:

- **S3.1 — Per-mod storage**: separate `SpriteStorage` and `AnimationStorage` instances do not share names or state; `Register`, `Replace`, `TryRegister`, `TryReplace`, `TryGet`, duplicate handling, and missing replacement follow their public contracts.
- **S3.2 — Resource contracts**: `AnimationInfo` rejects invalid names, frames, elements, and durations; `AnimationImportInfo` rejects invalid import metadata; `SpriteImportInfo` exposes its documented defaults and accepts caller-owned import data.
- **S3.3 — Animator behavior**: in real Unity, assigning a non-null `Animation` applies the first frame and advances `SpriteRenderer` frames at the configured duration; assigning `null` stops updates without inventing resource destruction.
- **S3.4 — Ownership boundaries**: storage accepts resources from any source, does not own `FileHandler` importing or JSON persistence, does not destroy Unity resources, and is not a static global registry. These are architecture checks, not reasons to add a test framework.

The known `ModAnimator` last-frame skip is tracked separately by `ISSUE-19`; S3 must not add a corrective test or silently change that behavior.

## S4 — Other public API surface

“Other” means every public type, constructor, property, field, enum value, extension method, overload, and documented behavior owned by this library that is not S1–S3. It includes the following complete groups:

| Area | Public surface | Minimum acceptance |
| --- | --- | --- |
| CheatConsole command abstraction | `AutoModCommand`; `ModSubCommandAttribute`; attribute properties and constructor; automatic help ordering, aliases, valid-length validation, uppercase policy, custom additions, and fail-fast invalid declarations | External-consumer or xUnit checks using the upstream command type; do not modify upstream code |
| Components | `ItemCollection<T>.Items` | Declared public fields, inherited/static fields, deferred enumeration, null matching, and exclusion of wrong types |
| Serialization | `SerializableVector3`; `UnityEngineIgnoreConverter`; `JsonSerializerSettingsFactory` | Conversions, constants, string/JSON shape, null handling, exact ignored-type policies, and fresh independent settings |
| System extensions | `List<T>.Move`; `Enum.GetNextEnumValue<T>`; `string.ReplaceWords` | Bounds, forward/backward and wraparound movement, negative enum steps, and documented replacement behavior |
| GameLibs extensions | `EnemyHealthBarExtensions`; `EntityOrientationExtensions`; `I2Extensions`; `InventoryManagerExtensions`; `NewInventoryWidgetExtensions`; `TraverseUtils`; `UnityExtensions` | Public signatures compile for an external consumer; deterministic conversion/validation/bounds behavior is tested; private game-state, inventory, localization, UI, hierarchy, and coroutine behavior gets focused real-game verification when required. No broad Unity simulation |
| ModdingAPI extensions | `ConfigHandlerExtensions`; `FileHandlerExtensions`; `InputHandlerExtensions`; `LocalizationHandlerExtensions`; `ModCommandExtensions` | Public signatures and non-game contracts are externally exercised; live file, keybinding, localization, console, and asset-bundle behavior is checked only against the real framework surface |

S4 does not require one test per overload when overloads share one implementation contract, but every public overload must be represented by either a direct call or an explicit signature/compile check. Inherited upstream members, internal/private helpers, and raw upstream APIs remain outside this library's acceptance count.

## Existing evidence baseline

- `FlagApiSmokeTests` currently exercises useful internal registry/adapter seams and some public failure paths, but does not close the full S1 public success, NG+ forwarding, or real slot-lifecycle checks.
- `CheatConsoleLoggingTests` currently covers public configuration, internal channel behavior, log prefixes/levels, and patch metadata; it does not drive real `ConsoleWidget.Submit()`/`Write()` behavior or prove behavior preservation at the game boundary.
- `Program` already provides external-consumer smoke for `SerializableVector3`, `ItemCollection<T>`, unsupported orientation values, `UnityEngineIgnoreConverter`, settings factories, and representative `ModLogExtensions` identity. It does not constitute full S3/S4 coverage.

The next implementation pass should close these gaps slice by slice, starting with the minimum checks above. This document intentionally does not change the test project or production code.
