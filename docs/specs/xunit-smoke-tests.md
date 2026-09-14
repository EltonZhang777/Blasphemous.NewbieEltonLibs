# xUnit smoke test migration

## Problem Statement

The test project contains useful manual smoke-test flows in addition to its existing xUnit tests. Those flows require a hand-run `Program.Main` entry point, so the test runner cannot discover, report, or isolate them as normal tests.

## Solution

Convert every manual smoke scenario in the test project into independently discoverable xUnit tests. Preserve the existing public-behavior coverage while replacing exit-code checks, console-only success signals, and manual exception handling with xUnit assertions.

## User Stories

1. As a library maintainer, I want every existing smoke scenario discovered by xUnit, so that one standard test command executes the complete automated test suite.
2. As a library maintainer, I want each smoke scenario reported independently, so that a failure identifies the affected behavior.
3. As an external library consumer, I want the existing smoke coverage retained, so that migration does not reduce confidence in public API behavior.
4. As a build maintainer, I want the test project independent of a hand-run `Main` entry point, so that CI and local runners use the same execution path.
5. As a test author, I want tests to run without launching the game, so that deterministic library behavior remains fast and repeatable.

## Implementation Decisions

- Keep the change within the test project; production library behavior is out of scope.
- Use xUnit `Fact` or `Theory` cases at the existing public-behavior test seam.
- Convert manual success/failure signaling into xUnit assertions and retain meaningful scenario boundaries.
- Remove the requirement for a startup object or manual test-program execution once all covered flows are migrated.
- Keep the existing test runtime boundary: ordinary .NET execution, without starting Blasphemous 1 or simulating Unity/BepInEx.

## Testing Decisions

- Tests assert externally observable library behavior, not private implementation details.
- Existing xUnit coverage is the prior art and should retain its current organization and conventions.
- Completion requires the test runner to discover and execute the migrated scenarios alongside the existing suite.
- The two migration slices must each be independently verifiable with the standard test command.

## Out of Scope

- Changes to production library code.
- TestMod creation or real-game verification.
- Broad Unity/BepInEx simulation.
- New coverage unrelated to the manual smoke scenarios.

## Further Notes

The real-game boundary remains a separate verification task. This specification only covers deterministic tests that can run in the ordinary .NET test environment.
