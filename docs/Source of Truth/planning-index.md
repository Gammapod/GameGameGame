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
- Frontend UX lane: start with `docs/Source of Truth/Frontend-UX-Invariants.md` for frontend/shared-service boundaries and test traces, then `docs/Source of Truth/Frontend-UX-Standards.md` for concrete presentation and interaction guidance. Use `docs/Source of Truth/Frontend-UX-Decisions.md` for chronological rationale, `docs/Source of Truth/Frontend-Editor-Simulation-Flow.mmd` for the Editor/Simulation context diagram, `docs/Source of Truth/Entity-Panel-UX-Spec.md` for the entity-panel/breadcrumb/log model, `docs/Plans/SadConsole-UI-Specification.md` for the living SadConsole UI layout planning matrix, and `docs/Plans/Actor-POV-Inventory-Chain-Play-Layout-Plan.md` for the active actor-POV containment/inventory-chain Play layout reconstruction plan.
- Glossary lane: `docs/Source of Truth/glossary.md` records shared terminology such as spatial direction and adjacency vocabulary.
- Current-goals lane: `docs/Source of Truth/Current-Goals.md` records the mostly stable current project direction and bridges source-of-truth facts to active planning documents.
- Planning lane: `docs/Plans/High-Level-Roadmap.md`, active plans, and the gap log record detailed priorities, promotion triggers, and selected work.

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
5. `docs/Plans/High-Level-Roadmap.md`
     - Canonical source of truth for active strategic direction, prioritized backlog buckets, deferred ideas, dependencies, and promotion triggers.
6. `docs/Source of Truth/vertical-slice-map.md`
     - Cross-layer navigation map for implementation work that touches Core, Content, Editor, Agent API, GUI, tests, and docs.
     - Read selectively when a planned slice spans multiple layers.
7. `docs/Plans/Canonical-Actions-Vertical-Slice-Plan.md`
    - Active release plan focused on freezing the current Action Step catalog as legacy/prototype-compatible and promoting canonical actions through vertical slices. The canonical `Move` slice, first Pickup/Drop Action Choice interaction seam, componentized play-mode refactor, Enter/Exit policy slice, canonical Transfer sprint, and Play-mode interaction sprint are complete enough to serve as reference workflow evidence; the next canonical action target is selected from the roadmap backlog.
8. `docs/Archived/SadConsole-Dynamic-Entity-Lifecycle-Demo-Sprint-Plan.md`
   - Archived focused frontend sprint plan for making the Create/Destroy/Polymorph flagship room demoable in SadConsole through world-aware dynamic presentation and actor/initiative refresh.
9. `docs/Archived/Create-Destroy-Polymorph-Vertical-Slice-Sprint-Plan.md`
   - Archived focused sprint plan for template-backed `CreateEntity`, simple `DestroyTarget`, and `PolymorphTarget` gameplay, including Core/Content/Editor parity and the user-facing lifecycle flagship room.
10. `docs/Archived/SadConsole-Linked-Containment-Play-Mode-Sprint-Plan.md`
   - Archived focused frontend sprint plan for the successful consumer Play-mode linked containment-space proof of concept: connector-line contract, pure two-space layout, Play-mode inspection replacement, smooth connectors, F12 diagnostics, and follow-ups for connector styling, explicit inspected-space selection, and POV-driven automatic inspection policy.
11. `docs/Archived/Play-Mode-Interaction-Sprint-Plan.md`
   - Archived focused frontend sprint plan for playable consumer Play mode over abstract intent resolution, contextual prompt stacks, canonical Action Choice consumption, size-calibration coverage, and F12 interaction diagnostics.
12. `docs/Archived/New-Play-Mode-MVP-Sprint-Plan.md`
   - Archived focused frontend sprint plan for the new consumer-facing SadConsole Play mode route, Play/Debug/Edit scenario option split, and reusable layered inventory-space component that initially rendered only the controlled actor's current space.
13. `docs/Archived/Topology-Service-Phase-1-Sprint-Plan.md`
   - Archived focused sprint plan for the behavior-preserving Core topology service refactor: default grid neighbor lookup/enumeration, MovementService topology consumption, Action Choice drop/transfer topology facts, controlled exit affordance projection, and Transfer counterparty lookup.
14. `docs/Archived/Give-Take-Transfer-Vertical-Slice-Sprint-Plan.md`
   - Archived focused sprint plan for canonical peer inventory Transfer as a controller-agnostic atomic containment transfer with ActorToTarget/TargetToActor directions, policy-asymmetric validation, content test rooms, Action Choice/history/log support, and an explicitly designed frontend transfer workflow.
15. `docs/Archived/Delta-Point-of-View-Release-Plan.md`
   - Archived Delta release plan focused on arbitrary-entity point-of-view: breadcrumb-backed current place, bulk/aperture ratio, frontend/content projection, and affordance/adjective groundwork. Treat as foundation/reference unless follow-up POV work is explicitly selected.
16. `docs/Archived/Instance-Controller-Playable-Starts-Sprint-Plan.md`
   - Archived sprint plan for content-authored initial control source / nested playable starts. Treat as implementation history for placed-instance `controller` metadata, legacy player insertion fallback, nullable player coordinates, and valid playerless scenarios.
17. `docs/Archived/Initiative-Aware-PlayerChoice-Scheduler-Plan.md`
   - Archived hardening plan for initiative-aware `PlayerChoice` scheduling, headless prompt alignment, and history retargeting across active controlled actors.
18. `docs/Plans/Gamma-Editor-MVP-Plan.md`
   - On-hold Gamma release plan focused on the SadConsole Editor -> Preview -> Simulation -> Return loop. Treat as backlog/context until future roadmap selection promotes it again.
19. `docs/Plans/SadConsole-Frontend-Roadmap.md`
   - Broader frontend backlog/reference for SadConsole/debug-browser contracts. Not the active implementation plan while canonical action vertical slices are selected, except for the componentized play-mode replacement called out by that plan and focused frontend refactor sprints.
20. `docs/Plans/Beta-Capability-Gap-Log.md`
   - Reference log for scenario-discovered beta gaps, including headless-only, Console/frontend, reporting, Action Step, and engine/system gaps. Not an active implementation plan.
21. `docs/Plans/Beta-Design-Quirks-and-Gotchas.md`
   - Reference log for surprising, emergent, or currently-undocumented beta behavior that is not necessarily a bug or missing capability.
22. `docs/Plans/Sprint-Retrospective.md`
   - Recent process observations and open retrospective questions.
23. `docs/Archived/`
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
- Priorities, backlog buckets, dependencies, defer reasons, and promotion triggers belong in `High-Level-Roadmap.md`.
- Active implementation details belong in an active sprint/release plan under `docs/Plans/`; currently `Canonical-Actions-Vertical-Slice-Plan.md` is the broader active release direction, while completed focused Play-mode, Transfer, controller-starts, and initiative-aware scheduler plans live under `docs/Archived/`.
- Completed implementation plans should move to `docs/Archived/` and be summarized, not duplicated, in active planning docs.
- Retrospective/process observations belong in `Sprint-Retrospective.md` until a consolidated sprint workflow document supersedes scattered process notes.
- Avoid duplicating long explanations across documents; link to the authoritative doc instead.
