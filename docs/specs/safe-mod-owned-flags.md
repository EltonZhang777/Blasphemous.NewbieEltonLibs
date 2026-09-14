# Safe mod-owned flags

## Problem Statement

Mods currently have no shared safe API for persistent boolean flags. Direct use of the game's global flag system allows accidental ID collisions and makes it possible for one mod to read or write another mod's flags. The game's `GetFlag` method also returns `false` for both an absent flag and a stored `false` value.

## Solution

Add a registration-based API for mod-owned boolean flags. A mod registers a local name during initialization, and the library maps it to a vanilla flag ID prefixed with the owning mod ID. The full ID is formatted through the public `ModFlagsManager.FormatToFlagId` method, which mirrors the vanilla event system's ID formatting. The vanilla event system remains the source of truth, preserving slot-save behavior and compatibility with original-game events.

The API uses standard `Try` semantics: an operation succeeds only when the caller is authorized and the underlying vanilla flag exists. An uninitialized registered flag therefore cannot be mistaken for a stored `false` value. Registration itself does not create or initialize a vanilla flag.

## Traceability

The numbered stories below are referenced as `SF-<number>` in the public API coverage specification. In particular, `SF-4` means the absent-versus-stored-`false` contract, `SF-10` means explicit concrete mod-type identity, and `SF-12` means fail-closed behavior for ambiguous or missing identity.

## User Stories

1. As a mod author, I want to register a flag before using it, so that every mod-owned flag has an explicit ownership declaration.
2. As a mod author, I want my flag IDs to include my mod ID, so that local names cannot collide with another mod's names.
3. As a mod author, I want to use local flag names in the safe API, so that I cannot accidentally bypass the ownership prefix by supplying a raw vanilla ID.
4. As a mod author, I want to read my registered flag with `Try` semantics, so that an absent flag is distinct from a stored `false` value.
5. As a mod author, I want to set only my own registered flags, so that a typo or another mod's name cannot mutate unrelated state.
6. As a mod author, I want registration to be safe to repeat with the same configuration, so that initialization paths remain idempotent.
7. As a mod author, I want conflicting duplicate registration to fail, so that the NG+ preservation policy cannot be silently changed.
8. As a mod author, I want to choose `preserveInNewGamePlus` when registering, so that the flag follows the intended vanilla NG+ behavior.
9. As a mod author, I want the default NG+ preservation policy to match the vanilla default, so that ordinary flags need no extra configuration.
10. As a mod author, I want to identify my mod explicitly with its concrete type, so that shared assemblies can still resolve the correct mod.
11. As a mod author, I want the API to infer my mod from the calling assembly when no type is supplied, so that ordinary calls stay concise.
12. As a mod author, I want ambiguous or missing identity resolution to fail closed, so that a call cannot be attributed to the wrong mod.
13. As a mod author, I want vanilla event code that sets my prefixed flag to be visible through the safe API, so that the safe API reflects the actual game state.
14. As a mod author, I want to continue using the game's existing `Core.Events` API for low-level vanilla flags, so that no redundant pass-through wrapper is introduced.
15. As a mod author, I want mod-owned flags to use the game's slot persistence, so that they follow normal save-slot behavior without a second storage system.
16. As a mod author, I want global ModdingAPI persistence to remain available for future work, so that this feature does not imply a fragile cross-mod persistence protocol now.

## Implementation Decisions

- Provide a small static safe flag API with registration, `TryGet`, and `TrySet` operations.
- Registration accepts a local flag name, an optional `preserveInNewGamePlus` value defaulting to `false`, and an optional concrete mod `Type`.
- Registration returns failure for conflicting duplicates and is idempotent for an identical registration.
- Registration and lookup use the canonical vanilla ID: formatting-equivalent owner prefixes or local names identify the same flag. A same-owner, same-policy repeat is idempotent; a same-ID registration with a different owner type or NG+ policy fails.
- `ModFlagsManager.FormatToFlagId(string)` is a public pure formatter for complete vanilla flag IDs. It throws `ArgumentNullException` for `null`, leaves an empty string empty, replaces only ASCII spaces with underscores, applies the vanilla `ToUpper()` behavior, and leaves other separators unchanged. It does not trim, validate, add an ownership prefix, or grant access.
- Safe operations accept only local names and derive the vanilla ID as `<modId>:<localName>` before passing the complete ID through `FormatToFlagId`.
- `TryCreateVanillaId` rejects null, empty, and all-whitespace mod IDs or local names before composing the complete ID and routing it through `FormatToFlagId`.
- A concrete supplied `Type` takes precedence over calling-assembly inference and must exactly match one loaded `BlasMod` runtime type.
- Without a supplied type, the calling assembly must resolve to exactly one loaded mod. Missing or ambiguous resolution fails closed.
- Registration stores permission and ownership metadata only; it does not call `Core.Events.SetFlag`.
- `TryGet` succeeds only when the flag is registered and exists in the current vanilla flag state. A stored `false` is returned as a successful operation with `value == false`; an absent flag returns failure.
- `TrySet` succeeds only for a registered flag owned by the resolved mod and delegates storage to `Core.Events.SetFlag`, applying the registration's NG+ preservation policy.
- `ModFlagAdapter` routes both dictionary reads and vanilla writes through `FormatToFlagId`; `ModFlagsManager` does not duplicate the conversion. `ModFlagInfo.ModId` retains the raw mod ID, while mod-owned flag ownership compares the owner prefix after `FormatToFlagId`; unrelated mod identity resolution and validation remain unchanged.
- Unregistered access logs an error and returns failure. Registered-but-uninitialized reads return failure without treating the state as an error.
- The existing public `Core.Events.GetFlag`/`SetFlag` APIs remain the low-level vanilla flag surface; this library does not add redundant forwarding methods.
- ModdingAPI slot/global persistence is not extended to inject fields into arbitrary consuming-mod save classes. Such support would require an explicit cooperative protocol in each consuming mod and is out of scope.

## Testing Decisions

- Tests should verify observable behavior: formatter vectors and null behavior, registration ownership, duplicate handling, ID prefixing, identity resolution, `TryGet` distinction between absent and stored `false`, `TrySet` authorization, and NG+ preservation forwarding.
- The highest useful seam is the pure registration/identity/name-resolution logic, separated from the thin `Core.Events` adapter.
- The game adapter should be covered only by focused integration-style checks where the referenced game types are available; tests should not duplicate the game's own persistence implementation.
- Extend the existing smoke-test project and `FlagApiSmokeTests` style; do not add a test framework or a second test harness.

## Out of Scope

- Global ModdingAPI persistence for mod-owned flags.
- Automatic injection of flag fields into another mod's custom persistence data.
- A bridge that synchronizes ModdingAPI global data with `Core.Events`.
- A new wrapper around already-public raw vanilla flag methods.
- Non-boolean flags, flag deletion, or flag migration tooling.
- Permissions for one mod to read another mod's mod-owned flags.

## Further Notes

The vanilla flag system stores flags per save slot and already exposes `preserveInNewGamePlus`. Its private flag dictionary is needed only to distinguish an absent flag from an existing `false`; that access must follow the repository's existing `TraverseUtils` convention.
