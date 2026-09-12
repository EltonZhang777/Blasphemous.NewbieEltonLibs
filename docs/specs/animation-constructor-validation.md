# Animation constructor validation

## Problem Statement

As a Blasphemous 1 mod author, I want invalid animation descriptions to tell me
all of their input problems at once, so that I can correct a configuration
without repeatedly fixing one constructor argument per run.

`AnimationInfo` and `AnimationImportInfo` currently stop at the first invalid
argument. They also do not reject whitespace-only strings or null elements in an
animation's sprite array. Their constructors need one deterministic validation
contract while preserving the existing animation and import APIs.

## Solution

Make both constructors validate every independent input before assigning any
public property. Begin each constructor with an empty error-message value and
use `StringBuilder` for accumulation. Each failed validation appends one
domain-specific report; validation continues with the remaining inputs.

Use S1's `ValidationUtils.Validate<T>` with logging and throwing disabled for
each predicate. Do not add a public or separate `TryValidate` API. When any
reports exist, convert the accumulated text to one message and throw exactly
one `ArgumentException` after all checks finish. Assign constructor arguments to
the read-only properties only when no report exists.

Reports use the existing English messages wherever possible, are separated by
`Environment.NewLine`, have no trailing newline, and appear in deterministic
constructor order:

- `AnimationInfo`: `name`, the `sprites` array, each invalid sprite by index,
  then `secondsPerFrame`.
- `AnimationImportInfo`: `name`, `filePath`, `width`, `height`, then
  `secondsPerFrame`.

The constructor stores successful strings and arrays unchanged. It does not trim
strings or clone arrays.

## User Stories

1. As a mod author, I want a null animation name to be reported clearly, so
   that I know which input is missing.
2. As a mod author, I want an empty animation name to be rejected, so that an
   animation can be addressed by a meaningful identifier.
3. As a mod author, I want a whitespace-only animation name to be rejected, so
   that visually blank identifiers cannot enter storage.
4. As a mod author, I want a null animation sprite array to be reported, so
   that a missing frame collection is distinguishable from an empty one.
5. As a mod author, I want an empty sprite array to be rejected, so that an
   animation always has a frame to display.
6. As a mod author, I want every null sprite element to be reported with its
   zero-based index, so that I can repair the exact frame entry.
7. As a mod author, I want a non-positive frame duration to be rejected, so
   that animation playback cannot use an invalid interval.
8. As a mod author, I want `NaN` frame durations to be rejected, so that an
   invalid floating-point value cannot bypass the positive-duration rule.
9. As a mod author, I want positive infinity to retain the existing accepted
   behavior, so that this change does not silently broaden the duration
   contract.
10. As a mod author, I want a null import name to be reported clearly, so that
    invalid import metadata is easy to diagnose.
11. As a mod author, I want an empty or whitespace-only import name to be
    rejected, so that imported animations have usable identifiers.
12. As a mod author, I want a null file path to be reported clearly, so that I
    can distinguish a missing path from an empty path.
13. As a mod author, I want an empty or whitespace-only file path to be
    rejected, so that importing does not proceed with a blank source.
14. As a mod author, I want non-positive frame width and height to be rejected,
    so that spritesheet slicing receives meaningful dimensions.
15. As a mod author, I want multiple invalid fields reported in one exception,
    so that one correction cycle can address the whole input.
16. As a mod author, I want reports in stable constructor order, so that error
    output is predictable and actionable.
17. As a mod author, I want one final `ArgumentException` for aggregate
    constructor failures, so that callers have one consistent failure type.
18. As a mod author, I want successful strings and arrays preserved exactly, so
    that validation does not change resource identifiers or frame ownership.
19. As a library maintainer, I want constructor validation to reuse S1's shared
    validation API, so that generic predicate behavior is not duplicated.
20. As a library maintainer, I want constructor validation failures to stay
    silent in `ModLog`, so that expected aggregation does not create unrelated
    game-log noise.
21. As a library maintainer, I want the runtime behavior to remain compatible
    with .NET 3.5, so that the library remains usable in Blasphemous 1.
22. As an existing consumer, I want storage, animator, and other callers to
    remain unchanged, so that this constructor contract does not expand into an
    unrelated migration.

## Implementation Decisions

- Change only the validation behavior of the two public animation-description
  constructors.
- Use the S1 `ValidationUtils.Validate<T>` contract with named options that
  disable logging and throwing for individual predicates.
- Do not expose a `TryValidate` method. The S1 boolean result is sufficient for
  silent predicate validation.
- Keep null, empty, and whitespace checks for one string mutually exclusive,
  while continuing validation across different constructor parameters.
- Use the Unity `== null` semantics when checking each sprite element.
- Reject width and height values less than or equal to zero.
- Keep the duration predicate equivalent to `value > 0`: reject negative values,
  zero, and `NaN`; accept positive infinity.
- Preserve the current messages for existing empty, dimension, and duration
  failures. Add explicit messages for null, whitespace, and indexed null-sprite
  failures:
  - `An animation name cannot be null.`
  - `An animation name cannot be whitespace.`
  - `Animation sprites cannot be null.`
  - `An animation sprite at index {index} cannot be null.`
  - `An animation file path cannot be null.`
  - `An animation file path cannot be whitespace.`
- Use a .NET 3.5-compatible whitespace predicate.
- Do not trim successful strings or clone successful sprite arrays.
- Keep the shared validation API, its generic error message, and its public
  compatibility surface unchanged.

## Testing Decisions

Tests should observe only the public constructor behavior, not private helper
names, predicate call structure, or the choice of `StringBuilder`.

Use the existing test project's style and test the highest available seam: the
two public constructors. Cover valid construction, each null/empty/whitespace
case, empty sprite arrays, indexed null sprite elements, non-positive numeric
values, `NaN`, positive infinity, and combinations of failures.

Verify the exact `ArgumentException` type, message text, message order,
newline separator, absence of a trailing newline, and preservation of valid
input references and values. This spec does not add or modify test
implementation.

## Out of Scope

- Adding a public or separate `TryValidate` method.
- Changing S1's `ValidationUtils.Validate<T>` behavior or generic error text.
- Changing animation or sprite storage registration semantics.
- Changing `ModAnimator` or any other caller.
- Logging constructor validation failures through `ModLog`.
- Cloning sprite arrays, trimming strings, or rejecting positive infinity.
- Rewording unrelated console, command, or upstream validation errors.
- Changing `CONTEXT.md` or adding a new ADR; the existing glossary and
  ADR-0003 are sufficient.
- Implementing production code or tests as part of this spec publication.

## Further Notes

S1 is recorded in [GitHub Issue #24](https://github.com/EltonZhang777/Blasphemous.NewbieEltonLibs/issues/24)
and ADR-0003. This S2 specification records only the deferred constructor
aggregation contract. No new project-specific domain term was introduced, so
the glossary remains unchanged.
