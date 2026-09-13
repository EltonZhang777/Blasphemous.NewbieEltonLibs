# Issue #49 real-game evidence packet

Status: `PARTIALLY_VERIFIED_PENDING_EVIDENCE_LINKS`

This packet records the TestMod implementation and the automated baseline. The
manual Unity/BepInEx claims are limited to the tracked-session results and
bounded evidence recorded below.

## Session identity

| Field | Value |
| --- | --- |
| Repository | `EltonZhang777/Blasphemous.NewbieEltonLibs` |
| Branch | `codex/issue-49-testmod-real-game-verification` |
| Worktree | `C:\Users\28090\Documents\GitHub\Blasphemous.NewbieEltonLibs\.wt\issue-49-testmod-real-game-verification` |
| TestMod | `NewbieEltonLibsTestMod` / `0.2.1` |
| Game version/commit | `Blasphemous.exe` `2017.4.40.7214086`; Unity `2017.4.40.7214086` |
| Plugin list | Modding API 3.0.1; Cheat Console 1.1.0; Custom Settings 0.1.0; UI Framework 0.2.0; Damage Numbers Reborn 2.0.1; Debug Mod 1.4.0; Level Framework 0.2.0; Editor Mode 0.1.0; Stats Framework 0.1.0; Level Framework Extended 0.1.0; Localization Patcher 1.3.0; Mod List 0.1.0; Newbie Elton Libraries Test Mod 0.2.1 |
| Profile | `C:\Users\28090\Desktop\Games\Games\versions_of_blasphemous_1\渎神 mod测试  开发中mod` |
| Unity log | `C:\Users\28090\AppData\LocalLow\TheGameKitchen\Blasphemous\output_log.txt` |

## Automated baseline

| Check | Result |
| --- | --- |
| TestMod Debug net35 build | Passed, 0 warnings/errors |
| TestMod package dry-run | Passed; 3 files planned, no profile mutation/process launch |
| Release solution build | Passed, 0 errors; existing net8 compatibility/obsolete warnings remain |
| xUnit suite | Passed, 83/83 |
| External-consumer smoke | Passed: `Flag API smoke test passed.` |
| Production-library behavior | No production-library source changed by #49 slices |
| Parent issue #35 | Not modified or closed |

The final automated gate was rerun against commit `4e592c7` on 2026-09-14:
Release build succeeded with 0 errors (the existing 11 compatibility/
obsolete warnings remain), xUnit passed `83/83`, and the external-consumer
smoke printed `Flag API smoke test passed.`.

## Manual verification update

Tracked session `8bebb70a34b94b5d9307b6c83e4201d6` deployed the patched TestMod,
loaded `NewbieEltonLibsTestMod`, reached `STARTUP_READY`, and was stopped and
cleaned safely. Cleanup restored the three previously deployed files and
removed no new files.

Tracked follow-up session `bf30f5e7d4424f32881092e72418b8b4` used the same
worktree at commit `4e592c7`, loaded the TestMod, and reached `ready`. It was
stopped after manual verification and cleaned safely, restoring the same three
files. The follow-up recorded the following additional evidence:

- `s2-input on` and `s2-output on` both reported `PASS`.
- A whitespace-only submission produced no `[CheatConsole Input]` record;
  `newbie-test s2-write after-whitespace` then produced the expected visible
  output. The console losing focus after the empty submission is inherited
  from the game's `ConsoleWidget.Submit()` early return and is not a library
  defect.
- `newbie-test s4-unity` reported `S4|unity-helpers|PASS`.
- `newbie-test s4-inventory` reported filter, prefix, lookup, and selection
  `PASS`; layout was `BLOCKED` before the inventory was open and `PASS` with
  `Layout_Normal` in the two inventory-open checks.

| Slice | Actual result from captured logs/evidence | Current status |
| --- | --- | --- |
| S1 flags | Ownership, absent/stored-false, stored values, vanilla mutation, and stale-owner probes passed. NG+ recorded `S1|NEW_GAME|preserved=True|transient=False`; subsequent load/exit records preserved the expected values. | Functional pass; #52 can close; packet link consolidation remains in #57 |
| S2 console | Sessions `8bebb70...` and `bf30f5...` cover nonblank input/output, configuration, validation, whitespace suppression, and visible output. `NEWBIE-TEST HELP` is expected to be unknown under the accepted case-sensitive policy. | Functional pass; programmatic/toggle screenshot evidence remains for #53 |
| S3 resources | The user-provided screenshot confirms the first animation continued while the second execution was stopped. Earlier captured logs covered storage, timing, validation, and caller-owned resource behavior. | Functional pass; #54 can close; artifact linkage remains in #57 |
| S4 Unity/GameLibs | Live `ElderBrother` and `PietyMonster` owner/target checks passed; no-live-object cases were correctly `BLOCKED`; UI Boss and I2 checks passed. The follow-up also passed the Unity helper and inventory checks in and around the open inventory UI. | Functional pass; representative GUI artifact remains for #55/#57 |
| S4 ModdingAPI/commands | No `api-exception`; file paths, missing JSON, input, localization target/error, Chinese/Spanish fallback, console widget, declarations, and command contract passed. The uppercase policy conforms to the accepted case-sensitive sub-command rule. Bundle success was unavailable and is deferred as non-blocking. | Functional pass; representative GUI artifact remains for #56/#57 |

### Localization test-text interpretation

The TestMod localization file defines `testmod.hello` in English and Spanish,
and `testmod.default_only` in English only. `newbie-test s4-api` reports these
values in the visible Cheat Console and matching BepInEx output; it does not
render them in the HUD, NPC dialogue, inventory, or settings UI.

In `S4|localization-target`, `english=` is intentionally always English,
while `current=` is the current-language result. `defaultOnly=` is intentionally
the English fallback for a key with no translation. Therefore English is
expected for `current=` in Chinese, because no Chinese `testmod.hello` entry
exists; Spanish must show `Hola desde NewbieEltonLibs TestMod` in `current=`.
`testmod.missing` is expected to resolve to `LOC_ERROR`.

### Bundle decision

The configured profile has no valid TestMod asset bundle, and bundle success is
not important to the current goal. The missing-bundle path remains covered by
`S4|asset-bundle-missing|PASS`. The `asset-bundle-provided` success path is
therefore explicitly **non-blocking and deferred**; do not hold #49 closure on
it. If a valid bundle is added later, run that success path as a separate
follow-up.

### Follow-up results and remaining evidence

1. **Uppercase policy** is resolved without a code change. The lower-case
   command is the supported form; the upper-case sub-command is expected to be
   unknown because matching is case-sensitive.
2. **Whitespace suppression** is verified. The blank submission is ignored,
   produces no input record, and the console works after it is reopened. The
   focus loss is an upstream UI behavior, not a TestMod failure.
3. **S4 Unity/GameLibs follow-up** is verified. Unity helpers pass, and the
   inventory-open checks pass with `Layout_Normal`.
4. Remaining work is evidence-only unless a stricter artifact requirement is
   desired:
   - run `newbie-test s2-programmatic` and capture the console/log boundary for
     the missing independent programmatic-command proof;
   - attach or link representative S2, S4 Unity/GameLibs, and S4 API
     screenshots/recordings for #53, #55, #56, and #57;
   - retain the existing bundle-success deferral as non-blocking.

The package contains only these TestMod-owned files:

```text
publish/NewbieEltonLibsTestMod/plugins/NewbieEltonLibsTestMod.dll
publish/NewbieEltonLibsTestMod/plugins/Blasphemous.NewbieEltonLibs.dll
publish/NewbieEltonLibsTestMod/localization/Newbie Elton Libraries Test Mod.txt
```

## Scenario records

| Slice | Implementation commit | In-game command(s) | Expected evidence | Actual result | Status |
| --- | --- | --- | --- | --- | --- |
| S1 flags | `6ed42a4` | `newbie-test s1` / `flags` | owner-only access, absent vs stored false, vanilla mutation, both NG+ policies, lifecycle markers | Session `8bebb70...`; all probes passed and NG+/load/exit markers recorded | Pass; #52 ready to close |
| S2 console | `5d1a565` | `s2-gating`, `s2-input`, `s2-output`, `s2-write`, `s2-programmatic` | one input/output record per boundary line, whitespace suppression, immediate independent toggles, Debug gate, `Unknown mod` fallback | Sessions `8bebb70...`/`bf30f5...`; whitespace and visible write pass; programmatic proof not rerun in follow-up | Functional pass; #53 remains open for programmatic/artifact evidence |
| S3 resources | `22b9cb2` | `newbie-test s3`, `s3-stop` | storage isolation and Try/throw contracts, first frame, duration, null stop, frame order showing known last-frame skip | Earlier session logs plus user `Screenshot (46).png`; first animation continued while second stopped | Pass; #54 ready to close |
| S4 Unity/GameLibs | `54bd0a7` | `s4-unity`, `s4-live`, `s4-inventory` | real Unity helpers and live enemy/Boss/UI/I2/inventory objects | Sessions `8bebb70...`/`bf30f5...`; live bosses, Unity helpers, and inventory checks passed; scene-dependent checks classified | Functional pass; #55 remains open for artifact evidence |
| S4 ModdingAPI/commands | `1b077ac` | `s4-api`, `help` and invalid-parameter commands | file/bundle, input/axis, localization, console, command declaration evidence | Session `8bebb70...`; all non-bundle probes passed; bundle success explicitly deferred | Functional pass; #56 remains open for artifact evidence |

For each row, retain the tracked session id, game version, plugin list, exact
steps, expected/actual text, and bounded log excerpts. The remaining issue
blocker is the absence of accessible screenshot/recording links for S2 and S4,
plus the unrerun `s2-programmatic` boundary proof; no product `FAIL` is open.

## Manual run contract

Use a disposable copy of the configured profile. The normal run deploys the
three package files and launches the tracked `Blasphemous.exe` process.

```powershell
$SkillRoot = 'C:\Users\28090\.codex\skills\blasphemous-modding-helper'
$Python3 = 'C:\Users\28090\anaconda3\python.exe'
$Project = 'C:\Users\28090\Documents\GitHub\Blasphemous.NewbieEltonLibs\.wt\issue-49-testmod-real-game-verification\Blasphemous.NewbieEltonLibs.TestMod\Blasphemous.NewbieEltonLibs.TestMod.csproj'
$Profile = 'C:\Users\28090\Desktop\Games\Games\versions_of_blasphemous_1\渎神 mod测试  开发中mod'

& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') run `
  --project $Project --profile $Profile --configuration Debug --startup-timeout 60

# Replace SESSION_ID with the id printed by run.
& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') logs SESSION_ID
```

After startup, use the in-game console and record the visible console plus
the matching BepInEx/Unity log lines:

1. Run `newbie-test help`; verify `help` is first and the remaining entries
   are ordinal by command name. Run `NEWBIE-TEST HELP` to record the configured
   uppercase policy. Run `newbie-test s1` and capture all S1 lines.
2. Run `newbie-test s2-gating`, `newbie-test s2-input on`, and
   `newbie-test s2-output on`. Submit a normal command, submit whitespace-only
   input, run `help`, `newbie-test s2-write visible_line`, an unknown command,
   and `newbie-test s2-level input`. Check input/output prefixes and the
   `Unknown mod` fallback. Toggle each channel off and confirm the other still
   changes immediately. Run `newbie-test s2-programmatic` and confirm there is
   no nested player-input record.
3. Run `newbie-test s3`, wait at least 0.3 seconds, and capture the displayed
   frames and `S3|frame` order. Run `newbie-test s3-stop`; verify the current
   sprite remains and the caller-owned resources are not destroyed. Record the
   last-frame skip separately as existing behavior.
4. In gameplay run `newbie-test s4-unity`. Run `newbie-test s4-live` near a
   live enemy/Boss and run it again with the inventory UI available. Run
   `newbie-test s4-inventory` while the inventory is open.
5. Run `newbie-test s4-api`, switch away from English and run it again for
   default fallback evidence. To test asset-bundle success, place a valid
   bundle in this mod's data directory and run `newbie-test s4-api <filename>`;
   the missing-bundle failure is exercised automatically.
6. Save the disposable slot, reload it, and perform the normal New Game Plus
   transition after `newbie-test s1`. Record `S1|LOAD_GAME`, `S1|EXIT_GAME`,
   and `S1|NEW_GAME` values for both policies.

When the manual result is complete, stop and clean only the tracked session:

```powershell
& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') stop SESSION_ID
& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') clean SESSION_ID
```

## Classification rules

* `PRODUCT_BEHAVIOR_DEFECT`: a public TestMod check reports `FAIL` with the
  required game/framework objects and dependencies present.
* `PROFILE_DEPENDENCY_TEST_BLOCKER`: startup, dependency loading, missing live
  scene objects, unavailable disposable save transition, or missing optional
  bundle prevents the scenario from reaching its public boundary.
* `KNOWN_EXISTING_BEHAVIOR`: the S3 last-frame skip; record it without changing
  `ModAnimator` in this ticket.

No manual scenario is currently marked as a product defect. The final packet
must add bounded log excerpts and representative screenshots/recordings before
#52–#57 are closed. The parent #35 remains open and untouched.
