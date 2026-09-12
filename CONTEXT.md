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

**Local flag name**:
The name a mod supplies to the safe flag API; it is not a vanilla flag ID and cannot select another mod's flag.
_Avoid_: raw flag ID

**Vanilla flag ID**:
The canonical identifier used by `Core.Events` for a flag, including the owning mod's namespace for a mod-owned flag; formatting-equivalent names identify the same vanilla flag.
_Avoid_: local flag name

**Mod identity**:
The loaded `BlasMod` identified by its concrete runtime type or by its calling assembly when no type is supplied.
_Avoid_: display name, caller name

**Uninitialized flag**:
A registered mod-owned flag that has no corresponding value in the current vanilla flag state.
_Avoid_: false flag

## Verification vocabulary

**Public API surface**:
The library-owned public types and members intended for consuming mods, including extension members; inherited upstream members and internal or private implementation details are excluded.
_Avoid_: every public member reachable through a dependency

**External consumer**:
An assembly outside the library project that references this package, such as a consuming mod or the smoke-test assembly. An external-consumer acceptance check uses the library's public surface rather than `InternalsVisibleTo` seams.
_Avoid_: test-only access

**Verification slice**:
A test-planning grouping for this repository's acceptance work. S1 through S4 describe coverage scope, not namespaces, assemblies, or runtime components.
_Avoid_: production module
