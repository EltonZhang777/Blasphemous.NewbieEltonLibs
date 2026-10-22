---
status: accepted
---

# Keep UI object pools isolated by consuming mod

The reusable library will expose `PixelPerfect` and `HighRes` accessors backed by the matching canvases provided by `Blasphemous.Framework.UI`. Consuming mods do not need to initialize or register those canvases.

Each consuming mod owns a separate pool for each prefab. The inferred `ReuseObject` overload resolves the mod from the calling assembly; an overload accepting the mod explicitly supports calls made through a shared helper assembly. Both overloads resolve to the same mod identity so they use the same pool. Pool containers are scoped to that mod and prefab rather than found globally by prefab name.

The returned object wrapper exposes its `GameObject`, preserving the consuming mod's expected access pattern. Pool reuse continues to invoke `PoolObject.OnObjectReuse` on pooled objects.

## Considered options

- Requiring each consuming mod to supply or initialize the UI canvases was rejected because it adds setup to every consumer.
- Sharing one pool per prefab across all consuming mods was rejected because one mod could consume another mod's pooled instances or capacity.
- Finding pool containers globally by prefab name was rejected because different mods may use the same prefab name.
