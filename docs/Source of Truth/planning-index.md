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
4. `docs/Plans/Beta-Content-Exploration-Plan.md`
   - Active plan for ordering beta vignettes, capability gates, primitive showcase work, actor-zoo exploration, capability-gap logging, and beta content organization.
5. `docs/Plans/Beta-Capability-Gap-Log.md`
   - Active log for scenario-discovered beta gaps, including headless-only, Console/frontend, reporting, Action Step, and engine/system gaps.
6. `docs/Plans/Sprint-17-Scenario-Tooling-Decoupling.md`
   - Recently completed implementation slice before Gate 4 `Give`/`Take`; keep as wrap-up reference until archived.
   - Canonical scenario materialization, extracted headless scenario run/record services, and removal of non-UI dependencies on `GameGameGame.Editor` / Avalonia.
7. `docs/Plans/Sprint-Retrospective.md`
   - Recent process observations and open retrospective questions.
8. `docs/Archived/`
   - Historical context only. Read archived plans when current docs link to them or when investigating why an existing system was shaped a certain way.

## Current strategic priority

Alpha MVP is complete: the game can launch and be played in an authored scenario, and a player entity can be inserted into scenarios. Beta targets gameplay demo vignettes: several small authored scenarios that can be played, run headlessly, and used to discover which interactions are interesting before committing to a unified frontend/player-interaction model.

## Current highest-priority backlog buckets

1. Foundational movement and peer interaction primitives, with Gate 4 `Give`/`Take` as the next planned mechanics gate after the Sprint 17 decoupling cleanup.
2. Beta gameplay vignette design using alpha scenario feedback, as described in `High-Level-Roadmap.md`, `docs/Plans/Beta-Content-Exploration-Plan.md`, and `docs/Plans/Beta-Capability-Gap-Log.md`.
3. Scenario/testing/tooling feedback loop, especially persisted scenario runs, report summaries, recorded artifact review, and Sprint 17 polish follow-ups.
4. Scenario/testing/tooling decoupling polish from `docs/Plans/Sprint-17-Scenario-Tooling-Decoupling.md`, including root-only terminology, linked Editor test file organization, `EditorViewModelTests` service extraction opportunities, and eventual `MinimalScenarioRunner` cleanup.
5. Scenario/content packaging beyond alpha.
6. Inventory, containment, and transfer mechanics.
7. Spawning, creation, and template materialization.
8. Runtime architecture and simulation scale.
9. Behavior authoring reuse and organization.
10. Future integrated game/editor frontend.
11. Reactions and cross-entity behavior.
12. Future player control and action choice model.
13. Long-horizon diegetic/meta systems.

See `docs/Plans/High-Level-Roadmap.md` for relative priority order, dependencies, defer reasons, and promotion triggers within each bucket.

## Planning conventions

- Capabilities and supported authoring tiers belong in `Engine-Editor-Capabilities.md`.
- Cross-layer implementation navigation belongs in `vertical-slice-map.md`.
- Priorities, backlog buckets, dependencies, defer reasons, and promotion triggers belong in `High-Level-Roadmap.md`.
- Active implementation details belong in an active sprint plan under `docs/Plans/`.
- Completed implementation plans should move to `docs/Archived/` and be summarized, not duplicated, in active planning docs.
- Retrospective/process observations belong in `Sprint-Retrospective.md` until a consolidated sprint workflow document supersedes scattered process notes.
- Avoid duplicating long explanations across documents; link to the authoritative doc instead.
