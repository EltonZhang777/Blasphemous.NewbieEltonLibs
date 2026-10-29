# TestMod build and deployment contract

`Blasphemous.NewbieEltonLibs.TestMod` (A) is an external net35 consumer. Its
Debug build consumes the production library built in Release. Its
`PackageTestMod` target clears and repopulates the shared package root:

```text
publish/NewbieEltonLibsTestMod/plugins/NewbieEltonLibsTestMod.dll
publish/NewbieEltonLibsTestMod/plugins/Blasphemous.NewbieEltonLibs.dll
publish/NewbieEltonLibsTestMod/localization/Newbie Elton Libraries Test Mod.txt
publish/NewbieEltonLibsTestMod/data/Newbie Elton Libraries Test Mod/audio/smoke.wav
publish/NewbieEltonLibsTestMod/data/Newbie Elton Libraries Test Mod/audio/test_meme.mp3
```

`Blasphemous.NewbieEltonLibs.TestModB` (B) references A and uses the same
`publish/NewbieEltonLibsTestMod` package root. B's `PackageTestModB` target adds
`plugins/NewbieEltonLibsTestModB.dll` without clearing that root. Building B
builds A first through the project reference: A clears and repackages its files,
then B's target adds B. Therefore, build B last. Any later A build, including an
A `run --dry-run` without `--artifact`, clears B from the package; rebuild B
after any such A build.

GameLibs 4.0.67, Framework.UI 0.2.0, ModdingAPI 3.0.1, and CheatConsole 1.1.0
are compile-time references for both projects; Framework.Levels 0.1.4 is also
referenced by A. A's package target copies its plugin, the library assembly,
localization, and audio fixtures; B's target adds only its own plugin. Neither
target packages upstream framework or game assemblies, which the selected
profile supplies.
The WAV and MP3 files are fixtures for the FMOD import and playback checks:
`smoke.wav` exercises PCM WAV import, and `test_meme.mp3` exercises MP3 import
and one-shot playback.

Run `newbie-test s6-sound-storage` to verify FMOD Sound registration, lookup,
duplicate rejection, and per-instance isolation through the consuming mod's
public API. The `S6|sound-storage|PASS` log records each result. This storage-only
check uses placeholder handles and does not call FMOD or release a sound.

Run `newbie-test s7-sound-import` to verify MP3 and PCM WAV success, missing
file and path-escape failures, callback cardinality, and worker-thread callbacks.
Run `newbie-test s8-sound-playback` to verify main-thread handoff, storage
registration/retrieval, and one-shot playback; confirm that the test audio is
audible. S8 exercises low-level FMOD Sound playback, not FMOD Studio events.

From the repository root, use the configured `blasphemous-modding-test` CLI:

```powershell
$SkillRoot = 'C:\Users\28090\.codex\skills\blasphemous-modding-helper'
$Python3 = 'C:\Users\28090\anaconda3\python.exe'
$Project = (Join-Path (Get-Location) 'Blasphemous.NewbieEltonLibs.TestMod\Blasphemous.NewbieEltonLibs.TestMod.csproj')
$ProjectB = (Join-Path (Get-Location) 'Blasphemous.NewbieEltonLibs.TestModB\Blasphemous.NewbieEltonLibs.TestModB.csproj')
$PackageRoot = (Join-Path (Get-Location) 'publish\NewbieEltonLibsTestMod')
$Profile = 'C:\path\to\disposable\Blasphemous-profile'

& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') run `
  --project $Project --profile $Profile --configuration Debug --dry-run

& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') run `
  --project $Project --profile $Profile --configuration Debug --startup-timeout 60

# Replace SESSION_ID with the id printed by run.
& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') logs SESSION_ID --current
& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') stop SESSION_ID
& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') clean SESSION_ID
```

For a combined A+B package, finish any A-only profile session using its `logs`,
`stop`, and `clean` commands above, then run this sequence from the repository
root:

```powershell
# The project reference builds A first; A packages its files, then B adds its DLL.
dotnet build $ProjectB --configuration Debug

# Select the shared package explicitly. With --artifact, run does not rebuild A.
& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') run `
  --project $ProjectB --profile $Profile --artifact $PackageRoot --dry-run

# Deploys the package and launches the selected profile's game.
& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') run `
  --project $ProjectB --profile $Profile --artifact $PackageRoot --startup-timeout 60
```

The B `--artifact` argument is required here because B writes to A's package
root; it also prevents the deployment command from rebuilding and clearing that
root. The dry run validates and prints the package and destination plan without
deployment or launch. The final `run` deploys and launches; use the printed
session ID with the `logs`, `stop`, and `clean` commands above.

After the game reaches the main menu, run these Cheat Console commands in
order:

```text
newbie-ui-pool-test run
newbie-ui-pool-b-test run
newbie-ui-pool-test run
```

The first A run checks that wrapping an active caller object preserves its
state, that pooled clones stay inactive until their first reuse, and checks
canvas accessors, owner overload reuse, transforms, callbacks, prefab/canvas
isolation, and capacity. Its first-use check is
`S5|inactive-before-first-reuse|PASS`; it reports `BLOCKED` if the pool was
already initialized in this game session. A schedules destruction of its
pooled object, so `S5|owner-isolation` may be `BLOCKED` while it waits for B.
B must report `PASS` for `inactive-before-first-reuse`,
`consumer-b-explicit-owner`, `consumer-b-reuse`,
`destroyed-instance-replaced`, and `owner-isolation`. The final A run must
report `PASS` for `destroyed-instance-replaced` and `owner-isolation`.

The `logs SESSION_ID --current` output is startup evidence only. Manual
verification requires running the three commands above in game, recording the
scene/save, the reported `S5` results, and any visible errors, then exiting the
game normally. After the player confirms the scenario is complete, preserve and
inspect the session snapshot before stopping or cleaning it:

```powershell
& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') snapshot SESSION_ID
& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') logs SESSION_ID --snapshot
& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') stop SESSION_ID
& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') clean SESSION_ID
```

Check the snapshot for `TypeLoadException` involving TestMod A, TestMod B, or
the library, and for failed `S5` checks. Record errors from unrelated profile
mods separately. Snapshot and startup evidence do not replace the player's
manual report.

## Recorded real-game results

The completed A→B→A run on 2026-10-25 used session
`9f98bb01b0e24826b379473153272d70`. Both canvas accessors, wrapper activation,
missing-pool behavior, first-use inactivity, inferred and explicit owner reuse,
transform updates, callbacks, prefab and canvas isolation, capacity, destroyed
instance replacement, and final owner isolation passed. A's first owner-isolation
check was `BLOCKED` while it waited for B; the final A first-use check was
`BLOCKED` because that pool had already been initialized. B and the final A run
reported `PASS` for owner isolation and destroyed-instance replacement. No
`S5|FAIL` entries were recorded.

The session snapshot (`bepinex.log` and `unity.log`) ended with `All mods
disposed!`. An earlier launch attempt crashed during
`FMODAudioManager.Initialize` before any S5 checks; the completed rerun did not
reproduce that crash. The snapshot also contains a `TypeLoadException` from the
unrelated `ConsoleControlApiProbe` profile mod.

The A-only dry run prints the selected project, build, target name, package
files, profile, and destination paths before any profile mutation. In either
flow, `run` records the process tree and startup evidence; `stop` and `clean`
operate only on the selected session and preserve changed or unrelated files.
