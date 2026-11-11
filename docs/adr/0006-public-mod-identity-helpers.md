---
status: accepted
---

# Keep mod identity helpers public

`ModIdentityResolver` and `ReferenceComparer<T>` are intentional public APIs so consuming mods can reuse the library's mod identity resolution and reference-identity comparison. Their visibility is part of the reusable library contract, and making either type non-public later would be a breaking API change.
