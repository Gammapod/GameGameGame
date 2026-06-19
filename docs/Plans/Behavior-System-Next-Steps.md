# Behavior System Next Steps

Status: Upcoming / planning basis for future sprints.

The first canonical behavior-chain slice is complete and archived in [Behavior Model Consolidation First Slice](../Archived/Behavior-Model-Consolidation-First-Slice.md). The current canonical behavior model supports ordered `MoveFacing` and `PickupTarget` Action Steps across Core, YAML, validation/default handling, editor services, agent API, and minimal GUI authoring.

This document records the next priorities before adding many new primitives.

## Upcoming priorities

### 1. Legacy behavior cleanup plan

Goal: reduce the old low-level behavior authoring surface while preserving runtime/content compatibility until canonical replacements exist.

Near-term cleanup should focus on authoring and UI clarity, not deleting runtime compatibility:

- make canonical behavior-chain authoring the visually preferred GUI/API route;
- clearly label low-level `steps/checks/effects` as legacy/advanced compatibility;
- keep loading/executing existing low-level content;
- keep transitional primitive-backed linked plans valid but no longer recommended;
- identify which legacy checks/effects are still required by checked-in content and tests.

Do not remove Core/runtime support for legacy low-level plans until canonical Action Steps can replace the important behavior patterns.

Canonical primitives likely needed before fully retiring legacy low-level authoring:

- `Wait`, to express an explicit consumed no-op turn;
- `ReverseFacing` or equivalent, to replace legacy `ReverseDirection` behavior in wandering/bounce patterns;
- `BumpTarget` or equivalent target interaction fallback;
- eventual target/relocation primitives only when concrete content needs them.

### 2. Behavior trace formatter

Goal: add a compact formatter for canonical behavior-chain traces before the Action Step catalog grows.

The formatter should summarize:

- each attempted Action Step;
- whether it succeeded or failed;
- why fallback continued;
- state reads/writes such as `Facing` and `Target`;
- consumed-turn/terminal outcome.

This should support tests, debugging, GUI diagnostics, and content-editing agents without requiring them to inspect raw trace trees.

### 3. New canonical Action Steps

Goal: add new primitives deliberately after cleanup and trace formatting.

Candidate ideas remain conceptual until planned in detail:

- `Wait`;
- `ReverseFacing`;
- `BumpTarget`;
- `SeekTarget` / move toward target;
- entity creation/destruction;
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
