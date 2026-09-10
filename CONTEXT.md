# Blasphemous NewbieEltonLibs Context

Reusable APIs for Blasphemous 1 mods, including shared runtime resource types and components.

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
