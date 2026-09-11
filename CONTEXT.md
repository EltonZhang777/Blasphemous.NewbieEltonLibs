# Blasphemous NewbieEltonLibs Context

Reusable APIs for Blasphemous 1 mods, including console observation and safe mod-owned flags.

## Console observation

**Console input**:
Text submitted to the game's console for command processing. _Avoid_: individual keystrokes.

**Console output**:
A line written to the game's visible console. _Avoid_: only mod-command output.

**Caller Debug build**:
Whether the assembly that invoked a logging configuration method was built with JIT optimization disabled. _Avoid_: the library assembly's build configuration.

## Flag ownership

This context defines the vocabulary and ownership rules for mod-owned flags in the library.

## Language

**Mod-owned flag**:
A boolean flag registered by one mod and readable or writable only through that mod's safe flag API.
_Avoid_: shared flag, global flag

**Vanilla flag**:
A flag managed directly by Blasphemous' `Core.Events` system, including flags used by original-game events.
_Avoid_: mod flag

**Registration**:
The declaration that grants a mod permission to use a local flag name; registration does not create or initialize the vanilla flag.
_Avoid_: initialization, creation

**Mod identity**:
The loaded `BlasMod` identified by its concrete runtime type or by its calling assembly when no type is supplied.
_Avoid_: display name, caller name

**Uninitialized flag**:
A registered mod-owned flag that has no corresponding value in the current vanilla flag state.
_Avoid_: false flag
