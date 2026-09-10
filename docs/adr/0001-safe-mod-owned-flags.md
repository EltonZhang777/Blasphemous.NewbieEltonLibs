# Safe mod-owned flags use the vanilla event system

Status: accepted

Registered mod-owned flags use `Core.Events` as their storage so they remain compatible with the game's slot saves and original event system. The library keeps a separate in-memory registration and ownership map, prefixes each vanilla ID with `<modId>:<localName>`, and exposes `RegisterFlag`, `TryGetFlag`, and `TrySetFlag`; registration grants access but does not create a vanilla flag.

`TryGetFlag` returns success only when the flag is registered and currently exists in the vanilla flag state, so a stored `false` remains distinguishable from an uninitialized flag without using `bool?`. `preserveInNewGamePlus` is chosen at registration and defaults to the vanilla behavior (`false`). A caller may identify its mod with an exact concrete `Type`; when omitted, the calling assembly must resolve to exactly one loaded mod. Unregistered access fails closed, while direct use of the already-public `Core.Events` API remains a separate low-level vanilla operation.

The alternative of injecting flag fields into each mod's ModdingAPI persistence record is out of scope: persistence serializes each mod's self-declared custom data under that mod's ID, and the persistence save/load entry points are internal. Supporting it would require a cooperative data protocol in every consuming mod rather than a reusable library API.
