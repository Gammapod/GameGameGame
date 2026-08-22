---
id: source.planning-index
title: Planning Index
kind: source-of-truth
subkind: navigation-index
status: active
owners: [core-owner, content-editor, frontend-owner]
audience: [core-owner, content-editor, frontend-owner]
lane: navigation
truth_rank: 20
truth_domains: [navigation]
read_when:
  - starting a planning, backlog, sprint-selection, or wrap-up session
  - deciding which planning documents are current
  - onboarding into the roadmap before implementation work
related:
  - source.current-goals
  - source.content-authoring-manual
  - source.engine-editor-capabilities
  - source.invariants
---
# Planning Index

Status: Source of truth for planning-document navigation.

Read when:

- starting a planning, backlog, sprint-selection, or wrap-up session;
- deciding which planning documents are current;
- onboarding into the roadmap before implementation work.

Do not read when:

- making a narrow code/content edit whose relevant active plan is already known.

## Documentation lanes

- Core-owner/TDD lane: `docs/Source of Truth/invariants.md` records stable behavior contracts and test traces.
- Capability matrix lane: `docs/Source of Truth/Engine-Editor-Capabilities.md` records maintainer-facing capability support tiers and layer coverage.
- Content-authoring lane: `docs/Source of Truth/Content-Authoring-Manual.md` records content-editor-facing authoring capabilities, workflows, limits, and gap logging.
- Action logic lane: `docs/Source of Truth/Action-Step-Outcome-And-Affordance-Logic.md` records canonical Action Step outcome rules and actor/actee/spatial verb-affordance logic.
- Frontend game-text lane: `docs/Source of Truth/Frontend-Game-Text.md` records draft player-facing log message ID slots for current Action Steps, including success/failure and ratio-bucket variant IDs; final wording is intentionally deferred.
- Frontend UX lane: start with `docs/Source of Truth/Frontend-UX-Invariants.md` for frontend/shared-service boundaries and test traces, then `docs/Source of Truth/Frontend-UX-Standards.md` for concrete presentation and interaction guidance. Use `docs/Source of Truth/Frontend-Play-Visual-Language.md` for new Play-mode player-facing visual semantics, `docs/Source of Truth/Frontend-UX-Decisions.md` for chronological rationale, `docs/Source of Truth/Frontend-Editor-Simulation-Flow.mmd` for the Editor/Simulation context diagram, `docs/Source of Truth/Entity-Panel-UX-Spec.md` for the entity-panel/breadcrumb/log model, and `docs/Source of Truth/SadConsole-UI-Specification.md` for the living SadConsole UI layout planning matrix. Archived focused frontend plans record completed or inactive implementation context.
- Glossary lane: `docs/Source of Truth/glossary.md` records shared terminology such as spatial direction and adjacency vocabulary.
- Current-goals lane: `docs/Source of Truth/Current-Goals.md` records the mostly stable current project direction and bridges source-of-truth facts to active planning documents.
- Planning lane: `docs/Plans/Core-Rolling-Board.md`, `docs/Plans/Content-Rolling-Board.md`, `docs/Plans/Frontend-SadConsole-Rolling-Board.md`, and `docs/Source of Truth/Capability-Gap-Log.md` record current priorities, selected work, ownership, and scenario-discovered gaps.

## Required reading order for planning/content work

1. `docs/Source of Truth/Content-Authoring-Manual.md`
   - Source of truth for what content authors and content-editing agents can safely author today.
   - Use this before authoring scenarios or classifying content-facing gaps.
2. `docs/Source of Truth/Engine-Editor-Capabilities.md`
   - Source of truth for maintainer-facing engine/editor capability support tiers and layer coverage.
   - Trust this over roadmap or plan documents for implemented support status.
3. `docs/Source of Truth/invariants.md`
   - Source of truth for core-owner-facing TDD invariant/test traces.
   - Use this before planning changes to stable behavior.
4. `docs/Source of Truth/Current-Goals.md`
   - Source of truth for the current selected direction and active planning bridge.
   - Use this before opening detailed roadmap or active-plan documents.
5. `docs/Source of Truth/vertical-slice-map.md`
      - Cross-layer navigation map for implementation work that touches Core, Content, Editor, Agent API, GUI, tests, and docs.
      - Read selectively when a planned slice spans multiple layers.
6. `docs/Plans/Core-Rolling-Board.md`
     - Active rolling board for Core-owned near-term implementation, build/workflow, and shared-service slices that do not need a dedicated sprint plan.
7. `docs/Plans/Content-Rolling-Board.md`
     - Active rolling board for Content-owned authoring, validation, scenario, and experiment-support slices that do not need a dedicated sprint plan.
8. `docs/Plans/Frontend-SadConsole-Rolling-Board.md`
     - Active rolling board for the new `GameGameGame.Frontend.SadConsole` workstream and small frontend-owned slices.
9. `docs/Source of Truth/Capability-Gap-Log.md`
     - Living source-of-truth log for scenario-discovered gaps, including headless-only, frontend, reporting, Action Step, and engine/system gaps. Not an active implementation plan.
10. `docs/Source of Truth/Design-Quirks-and-Gotchas.md`
    - Living source-of-truth log for surprising, emergent, or currently-undocumented behavior that is not necessarily a bug or missing capability.
11. `docs/Source of Truth/SadConsole-UI-Specification.md`
    - Living source-of-truth UI layout/layering/resizing/mouse/render-style specification for SadConsole presentation work.
12. `docs/Archived/Content-Validation-Compiler-Migration-Sprint-Plan.md`
      - Archived focused Content/compiler migration sprint plan: one-document `ContentCompiler`, unified validation diagnostics, structured reference diagnostics, document/source attribution, symbol/reference index, and compiler routing for editor/preview/materialization consumers. Read before implementing future content workspace or compiler changes.
13. `docs/Archived/Content-Workspace-Surface-Implementation-Plan.md`
      - Archived one-document compiler-backed workspace/type-first surface plan: scenario/reference projections and read-only frontend-neutral surface context. Read before changing multi-document workspace surfaces or migrating frontend/editor consumers.
14. `docs/Archived/Graph-First-Runtime-Topology-Migration-Plan.md`
       - Archived architecture/refactor sprint plan for migrating runtime topology from coordinate-primary behavior to graph-first topology identity: topology nodes and graph edges became authoritative for movement, adjacency, pathing, visibility, and overlap/folded topology, while coordinates remain projections for authoring/layout/display/debug compatibility.
15. `docs/Archived/SadConsole-Inventory-Space-Zoom-Sprint-Plan.md`
      - Archived focused frontend sprint plan for mixed-size inventory-space rendering: Space Zoom and Relationship Tier vocabulary, SadConsole child-surface mixed scaling, shared pixel presentation geometry, connector/tooltip/layer/performance mitigations, current-location 32x32 rendering, parent-chain 16/8/4 rendering, player inventory 24x24 with 1px gaps, and SadConsole `CellDecorator` Facing overlays.
16. `docs/Archived/SadConsole-Dynamic-Entity-Lifecycle-Demo-Sprint-Plan.md`
    - Archived focused frontend sprint plan for making the Create/Destroy/Polymorph flagship room demoable in SadConsole through world-aware dynamic presentation and actor/initiative refresh.
17. `docs/Archived/Create-Destroy-Polymorph-Vertical-Slice-Sprint-Plan.md`
    - Archived focused sprint plan for template-backed `CreateEntity`, simple `DestroyTarget`, and `PolymorphTarget` gameplay, including Core/Content/Editor parity and the user-facing lifecycle flagship room.
18. `docs/Archived/SadConsole-Linked-Containment-Play-Mode-Sprint-Plan.md`
    - Archived focused frontend sprint plan for the successful consumer Play-mode linked containment-space proof of concept: connector-line contract, pure two-space layout, Play-mode inspection replacement, smooth connectors, F12 diagnostics, and follow-ups for connector styling, explicit inspected-space selection, and POV-driven automatic inspection policy.
19. `docs/Archived/Play-Mode-Interaction-Sprint-Plan.md`
    - Archived focused frontend sprint plan for playable consumer Play mode over abstract intent resolution, contextual prompt stacks, canonical Action Choice consumption, size-calibration coverage, and F12 interaction diagnostics.
20. `docs/Archived/New-Play-Mode-MVP-Sprint-Plan.md`
    - Archived focused frontend sprint plan for the new consumer-facing SadConsole Play mode route, Play/Debug/Edit scenario option split, and reusable layered inventory-space component that initially rendered only the controlled actor's current space.
21. `docs/Archived/Merged-Topology-Refactor-Sprint-Plan.md`
    - Archived behavior-preserving refactor sprint plan that prepared clean merged topology implementation seams: topology identity/fact vocabulary, directional uniqueness helper, topology identity versus layout/render coordinate naming, Content/editor topology plumbing mapper, and topology visibility projection seam stub.
22. `docs/Archived/Topology-Service-Phase-1-Sprint-Plan.md`
    - Archived focused sprint plan for the behavior-preserving Core topology service refactor: default grid neighbor lookup/enumeration, MovementService topology consumption, Action Choice drop/transfer topology facts, controlled exit affordance projection, and Transfer counterparty lookup.
23. `docs/Archived/Give-Take-Transfer-Vertical-Slice-Sprint-Plan.md`
    - Archived focused sprint plan for canonical peer inventory Transfer as a controller-agnostic atomic containment transfer with ActorToTarget/TargetToActor directions, policy-asymmetric validation, content test rooms, Action Choice/history/log support, and an explicitly designed frontend transfer workflow.
24. `docs/Archived/Delta-Point-of-View-Release-Plan.md`
    - Archived Delta release plan focused on arbitrary-entity point-of-view: breadcrumb-backed current place, bulk/aperture ratio, frontend/content projection, and affordance/adjective groundwork. Treat as foundation/reference unless follow-up POV work is explicitly selected.
25. `docs/Archived/Instance-Controller-Playable-Starts-Sprint-Plan.md`
    - Archived sprint plan for content-authored initial control source / nested playable starts. Treat as implementation history for placed-instance `controller` metadata, legacy player insertion fallback, nullable player coordinates, and valid playerless scenarios.
26. `docs/Archived/Initiative-Aware-PlayerChoice-Scheduler-Plan.md`
    - Archived hardening plan for initiative-aware `PlayerChoice` scheduling, headless prompt alignment, and history retargeting across active controlled actors.
27. `docs/Archived/`
    - Historical context only. Read archived plans when current docs link to them or when investigating why an existing system was shaped a certain way.
   - Includes completed Play-mode interaction and MVP sprint plans, completed Topology Service Phase 1, Sprint 17 scenario/tooling decoupling, Sprint 18 tech-debt cleanup, Sprint 19 Gate 4 peer-transfer, Sprint 20 scenario run/report polish, Sprint 21 Console scenario catalog, Sprint 22 Gamma containment path service plans, the completed Enter/Exit policy vertical slice sprint, Frontend Sprint 2 SadConsole balanced Simulation UX, the completed SadConsole UI pattern discovery sprint, the completed SadConsole frontend refactor/consolidation sprint, the completed Core refactor/consolidation sprint, the archived Gamma frontend demo plan, the archived frontend testing strategy proposal, archived SadConsole prototype/assessment plans, the archived SadConsole tile-scaling spike findings, the archived/paused Beta content exploration plan, and historical Agent Editor API planning.

## Current goals bridge

Use `docs/Source of Truth/Current-Goals.md` for the mostly stable current project direction and active planning bridge. This navigation index should not duplicate current roadmap prose, active sprint details, or backlog bucket summaries.

## Planning conventions

- Stable behavior contracts and test traces belong in `invariants.md`.
- Maintainer-facing capability tiers and layer support belong in `Engine-Editor-Capabilities.md`.
- Content-editor-facing authoring capabilities, workflows, limits, and gap logging belong in `Content-Authoring-Manual.md`.
- Canonical Action Step outcome and verb-affordance decision tables belong in `Action-Step-Outcome-And-Affordance-Logic.md`.
- Cross-layer implementation navigation belongs in `vertical-slice-map.md`.
- Mostly stable current project direction and active planning bridge belongs in `Current-Goals.md`.
- Priorities, backlog buckets, dependencies, defer reasons, and promotion triggers belong in the relevant active rolling board under `docs/Plans/`.
- Active implementation details belong in the relevant rolling board under `docs/Plans/` unless a deliberately selected focused sprint/release plan is created; completed or inactive focused plans live under `docs/Archived/`.
- Completed implementation plans should move to `docs/Archived/` and be summarized, not duplicated, in active planning docs.
- Retrospective/process observations belong in `Sprint-Retrospective.md` until a consolidated sprint workflow document supersedes scattered process notes.
- Avoid duplicating long explanations across documents; link to the authoritative doc instead.
