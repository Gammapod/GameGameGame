# Behavior System Next Steps

Status: Completed / archived during sprint wrap-up. GUI clarity, first safe legacy hiding, behavior trace formatting, canonical plan preview, and the first utility Action Step batch were completed. Remaining follow-up priorities are tracked in `docs/Plans/High-Level-Roadmap.md` and the next sprint plan.

The first canonical behavior-chain slice is complete and archived in [Behavior Model Consolidation First Slice](../Archived/Behavior-Model-Consolidation-First-Slice.md). The current canonical behavior model supports ordered `MoveFacing` and `PickupTarget` Action Steps across Core, YAML, validation/default handling, editor services, agent API, and minimal GUI authoring.

This document records the next priorities before adding many new primitives.

## Upcoming priorities

### 1. Legacy behavior cleanup plan

Goal: reduce the old low-level behavior authoring surface while preserving runtime/content compatibility until canonical replacements exist.

Near-term cleanup should focus on authoring and UI clarity, not deleting runtime compatibility:

- keep canonical behavior-chain authoring the visually preferred GUI/API route;
- clearly label low-level `steps/checks/effects` as legacy/advanced compatibility;
- hide low-level `steps/checks/effects` from normal GUI authoring unless the selected plan is already a legacy low-level plan;
- create new GUI action plans as empty/passive plans instead of seeding legacy wait steps;
- keep loading/executing existing low-level content;
- keep transitional primitive-backed linked plans valid but no longer recommended;
- identify which legacy checks/effects are still required by checked-in content and tests.

Do not remove Core/runtime support for legacy low-level plans until canonical Action Steps can replace the important behavior patterns.

Canonical primitives likely needed before fully retiring legacy low-level authoring:

- `Wait`, to express an explicit consumed no-op turn;
- `ReverseFacing` or equivalent, to replace legacy `ReverseDirection` behavior in wandering/bounce patterns;
- `BumpTarget` or equivalent target interaction fallback;
- eventual target/relocation primitives only when concrete content needs them.

### Completed: behavior trace formatter

Goal completed: add a compact formatter for canonical behavior-chain traces before the Action Step catalog grows.

The formatter summarizes:

- each attempted Action Step;
- whether it succeeded or failed;
- why fallback continued;
- state reads/writes such as `Facing` and `Target`;
- consumed-turn/terminal outcome.

This supports tests, debugging, future GUI diagnostics, and content-editing agents without requiring them to inspect raw trace trees.

### Completed: canonical plan preview command

Goal completed: add an editor service / agent API command that summarizes the selected entity or action plan in canonical terms before content is saved or manually inspected in YAML.

The first version reports:

- action-plan shape: canonical behavior chain, transitional primitive plan, legacy low-level steps, or empty/passive;
- ordered canonical Action Steps, display names, hints, required/defaultable state, and state writes;
- materialized/defaulted state such as `Facing = West`;
- validation and canonical-authoring diagnostics relevant to the plan/entity;
- YAML preview/diff references where available.

This directly supports legacy cleanup and agent/editor authoring confidence. It does not introduce new engine semantics.

### Completed: first utility canonical Action Step batch

Goal completed: add new primitives deliberately after cleanup, trace formatting, and preview support.

First utility batch implemented:

- `DropFacing`, to put the first carried object on the floor in the actor's `Facing` direction;
- `PushFacing`, to push a blocking entity in `Facing` and consume the turn on success;
- `DestroyTarget`, to recursively remove `Target` and its contained inventory entities;
- `CreateFacing`, to create a placeholder rock-like entity in `Facing` as a prototype for future spawning/projectile/clone actions.

Deferred brainstorm Action Steps remain conceptualized, not planned here:

- `TeleportTo`, likely requiring a future `TargetLocation`/destination state slot;
- `Give`, moving carried inventory into `Target` inventory;
- `Take`, moving from adjacent/target inventory into actor inventory;
- `Wait`, `ReverseFacing`, `BumpTarget`, and `SeekTarget` / move toward target;
- player/screen messages;
- cooldowns or other runtime states.

Each new Action Step should include Core behavior, descriptor/YAML support if needed, validation/default metadata, editor service/API support, GUI affordances, tests, and capability manual updates.

## Conceptualized, not planned here

The following remain useful ideas but are not upcoming implementation work:

- behavior/action-plan templates and template usage UI;
- scheduler/speed/multiple-actions-per-turn;
- reaction slots and triggered interactions;
- diegetic action-plan entities;
- broader gameplay primitives without a concrete content/design need.
