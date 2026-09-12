@echo off
setlocal EnableExtensions DisableDelayedExpansion

pushd "%~dp0"
if errorlevel 1 (
    echo Failed to enter the script directory.
    exit /b 1
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo The dotnet command was not found on PATH.
    set "exitCode=1"
    goto :finish
)

set "formatTarget=%~1"
if defined formatTarget goto :targetReady

set "solutionCount=0"
for %%F in (*.sln) do if exist "%%~fF" (
    set /a solutionCount+=1 >nul
    set "formatTarget=%%~fF"
)
if %solutionCount% GTR 1 (
    echo Multiple solution files found. Pass the solution or project path as the first argument.
    set "exitCode=2"
    goto :finish
)
if %solutionCount% EQU 1 goto :targetReady

set "projectCount=0"
for %%F in (*.csproj) do if exist "%%~fF" (
    set /a projectCount+=1 >nul
    set "formatTarget=%%~fF"
)
if %projectCount% GTR 1 (
    echo Multiple project files found and no solution file was found. Pass the project path as the first argument.
    set "exitCode=2"
    goto :finish
)
if not defined formatTarget (
    echo No solution or project file was found next to this script.
    set "exitCode=2"
    goto :finish
)

:targetReady
if not exist "%formatTarget%" (
    echo Format target not found: "%formatTarget%"
    set "exitCode=2"
    goto :finish
)

echo Formatting "%formatTarget%"...
dotnet restore "%formatTarget%"
if errorlevel 1 goto :failed

dotnet format whitespace "%formatTarget%" --no-restore
if errorlevel 1 goto :failed

rem info-level style fixes can make MSBuildWorkspace try to change project document properties.
dotnet format style "%formatTarget%" --no-restore --severity info
if errorlevel 1 goto :failed

set "exitCode=0"
goto :finish

:failed
set "exitCode=%errorlevel%"

:finish
popd
exit /b %exitCode%
