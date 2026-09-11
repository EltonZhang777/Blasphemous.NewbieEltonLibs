# Blasphemous NewbieEltonLibs Context

Reusable APIs for Blasphemous 1 mods, including console observation, safe mod-owned flags, and shared runtime resources.

## Console observation

**Console input**:
Text submitted to the game's console for command processing. _Avoid_: individual keystrokes.

**Console output**:
A line written to the game's visible console. _Avoid_: only mod-command output.

**Caller Debug build**:
Whether the assembly that invoked a logging configuration method was built with JIT optimization disabled. _Avoid_: the library assembly's build configuration.

## Runtime resources

**Storage**:
A mod-owned collection of named runtime resources. It does not own resource creation or destruction.
_Avoid_: global registry, importer

**Sprite**:
A Unity 2D visual resource identified by a name chosen by the consuming mod.

**Animation**:
A named sequence of sprites with a frame duration.

**ImportInfo**:
Data that describes how a consuming mod loads an external resource before registering it in storage.
_Avoid_: resource, storage entry

**ModAnimator**:
A Unity component that displays an Animation through a SpriteRenderer and advances its frames over time.

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
