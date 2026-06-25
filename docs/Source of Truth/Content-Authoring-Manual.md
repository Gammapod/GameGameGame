# Content Authoring Manual

Status: Source of truth for content-editor-facing authoring capabilities and workflows.

Read when:

- authoring or reviewing game content;
- deciding what can be expressed with current content tools;
- writing beta scenarios, primitive showcases, or capability-gap notes.

Do not read when:

- changing Core behavior or tests; use `invariants.md` first;
- checking implementation-layer support tiers; use `Engine-Editor-Capabilities.md`.

This document should explain what content authors and content-editing agents can safely do today without exposing Core/runtime implementation details beyond necessity.

## Current Authoring Surface

Content authors can currently author:

- entity templates and presentations;
- inventory dimensions, weight, carrying capacity, and carried entity layout;
- default action-plan assignment;
- initial actor `Facing`;
- canonical ordered behavior chains;
- persisted scenarios with scenario root, player template, player entity ID, and player start placement.

The current editor and agent/headless tooling can:

- create, open, save, reload, validate, and preview content documents;
- create, edit, duplicate, delete, and reorder entity templates and action plans;
- add, remove, and reorder canonical behavior-chain Action Steps;
- materialize persisted scenarios;
- launch persisted scenarios in Console by content file and scenario ID;
- run scenario reports headlessly;
- record persisted scenarios to PNG frames and GIF artifacts.

## Authoring Model

Use normal content definitions. Scenarios should reference entity templates, presentations, and action plans rather than inventing scenario-only scripting.

Use canonical ordered behavior chains for new normal behavior. Legacy low-level action-plan authoring exists for compatibility with older content, but new content should prefer canonical Action Steps.

Avoid arbitrary variable-name authoring in new content. Use supported actor state such as `Facing` and `Target` through supported authoring workflows.

## Current Action Steps

The currently authorable canonical Action Steps are:

- `MoveFacing`
- `Backstep`
- `PickupTarget`
- `DropFacing`
- `PushFacing`
- `DestroyTarget`
- `CreateFacing`
- `TurnLeft`
- `TurnRight`
- `ReverseFacing`
- `AcquireNearestTarget`
- `SeekTarget`
- `FleeTarget`
- `MaintainChebyshevDistanceTwo`
- `StrafeClockwise`
- `StrafeAnticlockwise`

Author-facing behavior notes:

- `Facing`-based steps use the actor's current facing direction.
- `Target`-based steps use the actor's current target.
- `AcquireNearestTarget` currently selects a same-plane non-self target and has no authorable filters.
- `CreateFacing` currently creates a placeholder entity, not an authored template.
- `Give`, `Take`, template-backed spawning, reactions, and richer target filters are not currently authorable.

## Scenario Workflows

Author beta scenarios as small, focused vignettes that are playable or inspectable with current tools.

Preferred workflow:

1. Define the content goal in scenario terms.
2. Use currently supported templates, presentations, scenarios, player insertion, initial `Facing`, inventory values, and canonical behavior chains.
3. Validate the content document.
4. Run the scenario headlessly and review setup, turns, observations, diagnostics, final state, and capability gaps.
5. Record the scenario when visual review is useful.
6. Log unsupported desired behavior as a capability gap instead of forcing implementation-specific workarounds.

## Capability Gaps

Use a capability-gap entry when desired content cannot be expressed cleanly with current tools.

Record:

- scenario or vignette;
- desired behavior;
- current workaround, if any;
- missing capability;
- scenario count or scenario value unlocked;
- requested priority;
- classification.

Classify gaps as:

- content-only authoring friction;
- reporting/tooling request;
- new Action Step or primitive using existing engine state;
- new engine capability or system;
- content/package organization issue.

Promote a request when repeated scenario pressure, one flagship blocked vignette, hard-to-interpret reports, or repeated authoring friction shows that new support is worth planning.

## Current Limitations

- New authoring should not depend on legacy low-level variables or `SetVariable`.
- `AcquireNearestTarget` has no template, category, player, item, or prop filters yet.
- Scenario definitions cannot currently author per-entity initial `Target` overrides.
- Scenario reports do not yet provide rich inventory/containment summaries for every content-review need.
- There is no single persisted-scenario preview-plus-simulation report yet.
- `CreateFacing` is useful for placeholder creation but not for authored spawning.
