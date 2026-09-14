---
status: accepted
---

# Use per-mod runtime resource registries

The reusable storage API will use per-mod instances. Consuming mods create, register, and replace their own Sprite and Animation resources; storage does not read `sprites.json` or `animations.json`, does not depend on `FileHandler` for automatic importing, and does not destroy Unity resources. This keeps resource creation and lifecycle ownership with the mod that created the resource and avoids a process-wide name collision domain.

To preserve the requested `SpriteImportInfo` API, the library will use the `Blasphemous.Framework.Levels` vector type rather than introduce a duplicate vector type.

The migration deliberately leaves an existing `ModAnimator` bug for later evaluation: its frame-advance condition skips the last frame of animations containing more than one frame. The migration must not silently change that behavior; a separate follow-up should decide the intended loop semantics and add focused verification.

## Considered options

- Automatic importing in the storage constructor: rejected because it couples reusable storage to one mod's JSON layout and file-loading policy.
- A static global registry: rejected because it shares state and names across consuming mods and makes ownership and testing less clear.
