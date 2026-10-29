# Public API coverage status

This record maps the public API coverage work to its verification boundary. S1–S4
and S6–S8 are verification slices, not production modules.

## Automated coverage

| Slice | Scope | Evidence |
| --- | --- | --- |
| S1 / #36 | Flag formatting, canonical registry IDs, stored-false reads, and adapter normalization | `FlagApiCoverageTests`, `FlagApiSmokeTests` |
| S2 / #37 | Console configuration, channels, levels, caller gating, prefixes, and patch seams | `CheatConsoleLoggingTests`, `Program` ModLog smoke |
| S3 / #38 | Animation/import DTOs, per-instance animation storage, and sprite-storage public surface | `StorageCoverageTests`; Unity-backed behavior remains manual |
| S4 / #39 | Auto commands, aliases, validation, malformed declarations, and `ItemCollection` | `CheatConsoleCommandCoverageTests`, `Program` `ItemCollection` smoke |
| S4 / #40 | Serializable vectors, Unity converter, JSON presets, and System extensions | `SerializationCoverageTests`, `SystemExtensionsCoverageTests` |
| S4 / #41–#42 | Deterministic orientation failure, translation parsing, inventory prefixes, traversal validation, and game-bound signatures | `GameLibsCoverageTests` |
| S4 / #43–#44 | File/config JSON round trips, keybinding lookup, command validation, and ModdingAPI signatures | `ModdingApiCoverageTests` |
| S6 / #75 | Per-mod FMOD Sound storage, strict/Try registration, explicit replacement, and non-owning lifetime | `FMODSoundStorageTests`; TestMod `s6-sound-storage` consumer smoke |
| S7 / #76 | Asynchronous MP3/PCM WAV import as FMOD Sounds, data-path confinement, callback cardinality, and worker-thread callbacks | `FMODSoundImporterTests`; TestMod `s7-sound-import` game smoke |
| S8 / #77 | One-shot playback of a registered FMOD Sound after the callback-to-main-thread handoff | TestMod `s8-sound-playback` game smoke |

The automated suite is an external-consumer net8 test project referencing the
net35 library. Internal registries and private state are used only to arrange
supporting evidence; acceptance assertions remain at public boundaries where
the dependency can run without Unity.

## Required manual follow-ups

These checks cannot run in the desktop .NET harness and remain actionable rather
than being reported as passed:

1. In a real Unity/BepInEx session, enable both console channels from a debug
   consuming assembly. Submit one nonblank player command and whitespace input;
   verify one complete input record with the input prefix. Exercise built-in,
   mod, help, error, and validation output through `Write(string)` and verify
   every visible line gets one output record. Exercise programmatic command
   processing and confirm it creates no player-input record and does not alter
   command behavior. Invoke an `AutoModCommand` help command and verify help is
   listed first, followed by attribute commands in ordinal name order.
2. In Unity, assign a real `AnimationInfo` to `ModAnimator`, verify the first
   frame is applied immediately, frame timing advances, and assigning `null`
   stops updates without destroying caller-owned resources. Record the known
   last-frame skip as the existing separate follow-up; do not correct it here.
   Register, replace, and retrieve real `Sprite` instances in separate
   `SpriteStorage` objects, including duplicate and missing-name failures.
3. In the game event system, register a flag through `ModFlagsManager`, verify
   owner-only `TryGetFlag`/`TrySetFlag` behavior and NG+ preservation forwarding,
   confirm vanilla mutation is visible, distinguish an absent flag from stored
   `false`, and run save/load/New Game Plus checks for the registered policy.
   Confirm no second persistence store is introduced.
4. Against live game objects, verify enemy/boss/UI accessors, I2 translation
   object operations, inventory ownership/lookups, and new-inventory selection.
   Verify component reuse/creation, alpha clamping, hierarchy, coroutine
   helpers, and supported direction conversions with real Unity components.
5. Against the real ModdingAPI, verify asset-bundle success/failure, axis edge
   transitions, localization target/default/error behavior, and console-widget
   access/parameter validation wording. Keep the consuming smoke assembly as the
   signature/compile check for every public overload.
6. In a real Unity/BepInEx session, run `newbie-test s6-sound-storage`. Verify
   `S6|sound-storage|PASS` reports registration, lookup, duplicate rejection,
   and per-instance isolation. The check uses placeholder handles and does not
   create or release an FMOD Sound; building TestMod proves compilation and
   packaging only. Record real game logs before treating this check as passed.
7. Manually play an existing FMOD Studio event and confirm its playback remains
   unchanged. The S8 smoke exercises low-level FMOD Sound playback, not Studio events.

## Recorded real-game evidence

- **S7 / #76 (2026-10-26):** In MENU, MP3 and PCM WAV imports succeeded once
  each on worker threads; missing-file and escaping-path requests failed once
  each. [Issue verification record](https://github.com/EltonZhang777/Blasphemous.NewbieEltonLibs/issues/76#issuecomment-6401073142).
- **S8 / #77 (2026-10-26):** One-shot playback passed in MENU, BOSS_RUSH,
  NEW_GAME, and NEW_GAME_PLUS; the user confirmed audible playback.
  [Issue verification record](https://github.com/EltonZhang777/Blasphemous.NewbieEltonLibs/issues/77#issuecomment-6401070098).

Future real-game results should be appended here by the manual verifier; no
broad Unity simulator, upstream modification, or duplicated vanilla persistence
is part of this coverage change. The reproducible issue packet is
[`issue-49-evidence.md`](issue-49-evidence.md).
