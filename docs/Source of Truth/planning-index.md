# Planning Index

Status: Source of truth for planning-document navigation.

Read when:

- starting a planning, backlog, sprint-selection, or wrap-up session;
- deciding which planning documents are current;
- onboarding into the roadmap before implementation work.

Do not read when:

- making a narrow code/content edit whose relevant active plan is already known.

## Required reading order for planning/content work

1. `docs/Source of Truth/Engine-Editor-Capabilities.md`
   - Canonical source of truth for implemented and authorable engine/editor capabilities.
   - Trust this over roadmap or plan documents if there is a conflict.
2. `docs/Plans/High-Level-Roadmap.md`
   - Canonical source of truth for active strategic direction, prioritized backlog buckets, deferred ideas, dependencies, and promotion triggers.
3. `docs/Source of Truth/vertical-slice-map.md`
   - Cross-layer navigation map for implementation work that touches Core, Content, Editor, Agent API, GUI, tests, and docs.
   - Read selectively when a planned slice spans multiple layers.
4. Active or likely-next sprint plan under `docs/Plans/`
   - Current expected implementation slice and testable outcomes.
   - As of this writing: `docs/Plans/Next-Sprint-Scenario-Testing-Plan.md`.
5. `docs/Plans/Sprint-Retrospective.md`
   - Recent process observations and open retrospective questions.
6. `docs/Archived/`
   - Historical context only. Read archived plans when current docs link to them or when investigating why an existing system was shaped a certain way.

## Current strategic priority

Prioritize the scenario feedback loop before broad new mechanics. The next sprint should improve generated scenario authoring, simulation feedback, compact traces/state summaries, and explicit capability-gap reporting so future movement, inventory, spawning, and reaction decisions are evidence-driven.

## Current highest-priority backlog buckets

1. Scenario/testing/tooling feedback loop.
2. Direction, movement, and canonical action semantics.
3. Inventory, containment, and transfer mechanics.
4. Spawning, creation, and template materialization.
5. Scenario/content packaging.
6. Runtime architecture and simulation scale.
7. Behavior authoring reuse and organization.
8. Future integrated game/editor frontend.
9. Reactions and cross-entity behavior.
10. Long-horizon diegetic/meta systems.

See `docs/Plans/High-Level-Roadmap.md` for relative priority order, dependencies, defer reasons, and promotion triggers within each bucket.

## Planning conventions

- Capabilities and supported authoring tiers belong in `Engine-Editor-Capabilities.md`.
- Cross-layer implementation navigation belongs in `vertical-slice-map.md`.
- Priorities, backlog buckets, dependencies, defer reasons, and promotion triggers belong in `High-Level-Roadmap.md`.
- Active implementation details belong in an active sprint plan under `docs/Plans/`.
- Completed implementation plans should move to `docs/Archived/` and be summarized, not duplicated, in active planning docs.
- Retrospective/process observations belong in `Sprint-Retrospective.md` until a consolidated sprint workflow document supersedes scattered process notes.
- Avoid duplicating long explanations across documents; link to the authoritative doc instead.
