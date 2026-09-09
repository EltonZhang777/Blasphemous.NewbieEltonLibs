# AGENTS.md

## Project boundary

This is a reusable Blasphemous 1 class library, not a mod. Consuming mods own the mod entry point.

The runtime ceiling is .NET 3.5; preserve it when adding code.

## Cross-cutting rules

- Access private game members through `TraverseUtils` (Harmony Traverse), not direct reflection.
- Document every public member with an XML `<summary>`.
- Prefer `TryXxx` when failure is expected.
- Follow existing `ModLog` null/error behavior.

## Conditional references

- Before domain or architecture changes, read `docs/agents/domain.md` and relevant ADRs under `docs/adr/`.
- For issue operations, read `docs/agents/issue-tracker.md`.
- For triage, read `docs/agents/triage-labels.md`.
