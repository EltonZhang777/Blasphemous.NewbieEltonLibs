# Issue #49 real-game evidence packet

Status: `WAITING_FOR_MANUAL_REAL_GAME_RUN`

This packet records the TestMod implementation and the automated baseline. It
does not claim Unity/BepInEx behavior until a tracked game session and manual
results are attached.

## Session identity

| Field | Value |
| --- | --- |
| Repository | `EltonZhang777/Blasphemous.NewbieEltonLibs` |
| Branch | `codex/issue-49-testmod-real-game-verification` |
| Worktree | `C:\Users\28090\Documents\GitHub\Blasphemous.NewbieEltonLibs\.wt\issue-49-testmod-real-game-verification` |
| TestMod | `NewbieEltonLibsTestMod` / `0.2.1` |
| Game version/commit | `PENDING: capture from the launched profile` |
| Plugin list | `PENDING: capture from BepInEx log` |
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

The package contains only these TestMod-owned files:

```text
publish/NewbieEltonLibsTestMod/plugins/NewbieEltonLibsTestMod.dll
publish/NewbieEltonLibsTestMod/plugins/Blasphemous.NewbieEltonLibs.dll
publish/NewbieEltonLibsTestMod/localization/Newbie Elton Libraries Test Mod.txt
```

## Scenario records

| Slice | Implementation commit | In-game command(s) | Expected evidence | Actual result | Status |
| --- | --- | --- | --- | --- | --- |
| S1 flags | `6ed42a4` | `newbie-test s1` / `flags` | owner-only access, absent vs stored false, vanilla mutation, both NG+ policies, lifecycle markers | Pending tracked session; no result inferred | Pending manual |
| S2 console | `5d1a565` | `s2-gating`, `s2-input`, `s2-output`, `s2-write`, `s2-programmatic` | one input/output record per boundary line, whitespace suppression, immediate independent toggles, Debug gate, `Unknown mod` fallback | Pending tracked session; no result inferred | Pending manual |
| S3 resources | `22b9cb2` | `newbie-test s3`, `s3-stop` | storage isolation and Try/throw contracts, first frame, duration, null stop, frame order showing known last-frame skip | Pending tracked session; no result inferred | Pending manual |
| S4 Unity/GameLibs | `54bd0a7` | `s4-unity`, `s4-live`, `s4-inventory` | real Unity helpers and live enemy/Boss/UI/I2/inventory objects | Pending tracked session; scene-dependent checks may be `BLOCKED` | Pending manual |
| S4 ModdingAPI/commands | `1b077ac` | `s4-api`, `help` and invalid-parameter commands | file/bundle, input/axis, localization, console, command declaration evidence | Pending tracked session; bundle success needs a real data file | Pending manual |

For each row, append the tracked session id, game version, plugin list, exact
steps, expected/actual text, a bounded log excerpt, and a screenshot or
recording link. Do not replace `Pending manual` with `Passed` based only on a
successful build.

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
