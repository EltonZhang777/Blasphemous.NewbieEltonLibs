# Centralize Harmony patch installation

All Harmony patch definitions live under `Blasphemous.NewbieEltonLibs.HarmonyPatches`, and the internal `HarmonyPatchInstaller` is the sole installation boundary. It uses the library-level Harmony ID `Blasphemous.NewbieEltonLibs` and one assembly scan, so future patches do not require a manually maintained type list.

Installation remains lazy because this repository is a class library with no mod entry point: each patch-dependent public feature ensures installation before its first use. Successful installation is idempotent; failures retain the existing error logging and retry behavior. This organization does not change the console observation seams or their observable logging contract.
