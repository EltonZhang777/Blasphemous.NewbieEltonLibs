# Centralized Harmony patch installation

## Problem Statement

Harmony patch definitions are currently stored in a feature-agnostic `Patches` area, while console logging owns installation and invokes Harmony once per patch type. This makes the installation boundary easy to miss when new patches are added and leaves patch organization inconsistent with the library-wide scope of the installation identity.

The library must centralize patch discovery and installation without changing console observation. Console input must still be observed at the player submission seam, console output at the visible-write seam, and the existing opt-in logging behavior must remain unchanged.

## Solution

Place every Harmony patch definition in the `HarmonyPatches` area and namespace. Add one internal static `HarmonyPatchInstaller` in that area as the only installation boundary. It creates the library-level Harmony owner and performs one assembly scan, allowing future patch definitions to be discovered without a manually maintained type list.

Installation remains lazy. The existing public console logging configuration methods ensure installation after validating their arguments and configuring their respective channels. Future patch-dependent public features use the same internal installer before their first patch-dependent behavior. No global eager bootstrap is introduced because the project is a class library and has no mod entry point.

## User Stories

1. As a mod author, I want console logging to install its required Harmony patches automatically when I configure it, so that I do not need a separate patch setup call.
2. As a mod author, I want input logging and output logging to remain independently configurable, so that centralizing patch installation does not couple their observable behavior.
3. As a mod author, I want console input to continue to mean a complete command submitted by the player, so that individual keystrokes are not logged as commands.
4. As a mod author, I want console output to continue to mean each line written to the visible game console, so that built-in, mod, help, and error output remains observable.
5. As a mod author, I want the existing `Submit` and `Write(string)` observation seams to remain unchanged, so that command and output logging keep their current timing and scope.
6. As a mod author, I want the existing prefixes, configured log levels, and Debug-build filtering to remain unchanged, so that existing log consumers do not need to adapt.
7. As a mod author, I want an invalid log level to be rejected before patch installation is attempted, so that invalid configuration keeps its current failure behavior.
8. As a mod author, I want configuring a channel with logging disabled to retain the current installation behavior, so that disabling a channel does not create a new initialization exception.
9. As a mod author, I want repeated calls to either console logging configuration method to be safe, so that separate initialization paths do not install duplicate patches.
10. As a mod author, I want concurrent configuration calls to share one installation attempt, so that race conditions cannot install the same library patches more than once.
11. As a mod author, I want a successful installation to become a no-op for later calls, so that normal configuration changes do not repeatedly scan or patch the assembly.
12. As a mod author, I want a failed installation to be reported through the existing error logging behavior, so that patch failures are diagnosable without changing the public configuration API.
13. As a mod author, I want a later configuration call to be able to retry a failed installation, so that a transient or ordering-related failure does not permanently disable console observation.
14. As a library maintainer, I want all Harmony patch definitions in one named area, so that a new patch has an obvious home and reviewers can detect misplaced patch code.
15. As a library maintainer, I want one installation boundary for all library patches, so that new features cannot silently introduce independent `PatchAll` calls.
16. As a library maintainer, I want future patch definitions to be found by scanning the library assembly, so that adding a patch does not require editing a registration list.
17. As a library maintainer, I want future patch-dependent features to ensure installation before their first use, so that central discovery does not depend on console logging being configured first.
18. As a library maintainer, I want the Harmony owner identity to represent the library, so that all centrally managed patches have one stable ownership scope.
19. As a consuming mod author, I want the library to remain free of a new public initialization API, so that adopting the refactor does not add startup boilerplate to consuming mods.
20. As a consuming mod author, I want the library to avoid global eager patching merely because its assembly was loaded, so that unrelated library APIs do not gain new startup side effects.
21. As a library maintainer, I want the refactor to remain compatible with the game's .NET 3.5 runtime, so that patch installation works in the supported game environment.
22. As a library maintainer, I want the refactor to preserve existing failure and retry semantics, so that centralization changes organization rather than the console logging contract.

## Implementation Decisions

- Move the existing console logging patch definitions into the `HarmonyPatches` area and use the matching `Blasphemous.NewbieEltonLibs.HarmonyPatches` namespace. Keep patch types internal and preserve their current targets and prefix/postfix behavior.
- Add an internal static `HarmonyPatchInstaller` in the same area. It owns the synchronization state, the applied-state flag, the library-level Harmony owner ID `Blasphemous.NewbieEltonLibs`, and the internal `TryEnsurePatched` operation.
- Remove the installation helper from `CheatConsoleLogging`. Its public configuration methods call the centralized installer after log-level validation and channel configuration.
- Treat installation as lazy, triggered by the first call to a patch-dependent public feature. The two existing console logging configuration methods remain the first triggers; a future patch-dependent feature must call the same internal installer before using its patched behavior.
- Do not rely on a static class being executed merely because the assembly is loaded. Do not add a module initializer, a new public bootstrap API, or a consuming-mod entry-point requirement.
- Use one assembly-level Harmony scan from the centralized installer. The installer is the only place allowed to call `PatchAll`; patch authors must not maintain a second explicit type list or feature-local installation path.
- Mark installation applied only after the complete scan succeeds. Preserve the existing double-check and lock behavior so repeated and concurrent calls are idempotent.
- On failure, preserve the existing error-level logging and exception details, leave installation marked incomplete, and allow a later call to retry. Do not add rollback, failure latching, or per-patch recovery in this change.
- Preserve the public console logging API, channel independence, prefixes, log-level routing, caller Debug-build filtering, `Submit` input filtering, and `Write(string)` output capture.
- Keep the implementation compatible with the repository's .NET 3.5 runtime ceiling and existing Harmony/ModdingAPI dependencies.

## Testing Decisions

- Tests should verify externally observable behavior and stable patch targets, not the private class layout, lock implementation, or the number of internal calls made to Harmony.
- Reuse the existing console logging channel and public configuration tests to protect defaults, independent input/output state, log-level routing, Debug-build filtering, invalid log-level rejection, and stable prefixes.
- Reuse the existing patch-target seam test to verify that input remains attached to `ConsoleWidget.Submit` and output remains attached to `ConsoleWidget.Write(string)`.
- Do not add a test-only seam for intercepting `Harmony.PatchAll` or exposing the internal installer merely to count calls. Idempotency and the single installation boundary are verified by source review and the centralized implementation; observable console behavior remains covered at the existing higher seam.
- Update only test references required by the namespace move if compilation requires it. Do not change the behavioral assertions or add business-patch tests.
- The existing test style—focused xUnit tests over the public logging contract plus reflection-based verification of stable patch metadata—is the prior art for this change.

## Out of Scope

- Adding or changing any business Harmony patch.
- Changing the `Submit` or `Write(string)` observation seams, command filtering, output capture, prefixes, log levels, Debug-build filtering, or channel semantics.
- Adding a public patch initialization API or requiring consuming mods to call one.
- Installing patches eagerly at assembly load or through a module initializer.
- Maintaining explicit per-patch registration lists alongside the assembly scan.
- Runtime validation that enforces the folder or namespace convention.
- Patch rollback, failure circuit breakers, per-patch isolation, or a new recovery state machine.
- Changing unrelated public APIs, persistence, localization, inventory, flag, storage, or serialization behavior.
- Changing `CONTEXT.md`; the existing console observation vocabulary remains sufficient.

## Further Notes

The existing console observation ADR remains authoritative for the `Submit` and `Write(string)` seams. The centralized installation trade-off is recorded separately in the Harmony patch installation ADR. The implementation should preserve the current failure message and retry behavior even though the installer owner ID is now library-wide; any future need for feature-specific failure reporting is a separate decision.
