# Blasphemous console logging context

This context defines the vocabulary for observing the game's debug console without changing its behavior.

## Console observation

**Console input**:
Text submitted to the game's console for command processing. _Avoid_: individual keystrokes.

**Console output**:
A line written to the game's visible console. _Avoid_: only mod-command output.

**Caller Debug build**:
Whether the assembly that invoked a logging configuration method was built with JIT optimization disabled. _Avoid_: the library assembly's build configuration.
