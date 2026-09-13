# TestMod build and deployment contract

`Blasphemous.NewbieEltonLibs.TestMod` is an external net35 consumer. Its
Debug build consumes the production library built in Release and packages only
the project-owned plugin assemblies plus its localization resource:

```text
publish/NewbieEltonLibsTestMod/plugins/NewbieEltonLibsTestMod.dll
publish/NewbieEltonLibsTestMod/plugins/Blasphemous.NewbieEltonLibs.dll
publish/NewbieEltonLibsTestMod/localization/Newbie Elton Libraries Test Mod.txt
```

GameLibs 4.0.67, ModdingAPI 3.0.1, CheatConsole 1.1.0, and Framework.Levels
0.1.4 are compile-time references only. The package does not copy upstream
framework or game assemblies; the selected profile supplies those dependencies.

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
