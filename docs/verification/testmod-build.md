# TestMod build and deployment contract

`Blasphemous.NewbieEltonLibs.TestMod` is an external net35 consumer. Its
Debug build consumes the production library built in Release and packages its
plugin assemblies and its localization resource:

```text
publish/NewbieEltonLibsTestMod/plugins/NewbieEltonLibsTestMod.dll
publish/NewbieEltonLibsTestMod/plugins/Blasphemous.NewbieEltonLibs.dll
publish/NewbieEltonLibsTestMod/localization/Newbie Elton Libraries Test Mod.txt
publish/NewbieEltonLibsTestMod/data/Newbie Elton Libraries Test Mod/audio/smoke.wav
publish/NewbieEltonLibsTestMod/data/Newbie Elton Libraries Test Mod/audio/test_meme.mp3
```

GameLibs 4.0.67, ModdingAPI 3.0.1, CheatConsole 1.1.0, and Framework.Levels
0.1.4 are compile-time references only. The package does not copy upstream
framework or game assemblies; the selected profile supplies those dependencies.
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
$Profile = 'C:\path\to\disposable\Blasphemous-profile'

& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') run `
  --project $Project --profile $Profile --configuration Debug --dry-run

& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') run `
  --project $Project --profile $Profile --configuration Debug --startup-timeout 60

# Replace SESSION_ID with the id printed by run.
& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') logs SESSION_ID
& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') stop SESSION_ID
& $Python3 (Join-Path $SkillRoot 'scripts\blasphemous_modding_test.py') clean SESSION_ID
```

The dry run prints the selected project, build, target name, package files,
profile, and destination paths before any profile mutation. `run` records the
process tree and current startup evidence; `stop` and `clean` operate only on
that session and preserve changed or unrelated files.
