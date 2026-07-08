# Gamma Frontend Demo Plan

Status: Supporting Gamma tester/demo context. Superseded for Gamma release selection by `docs/Plans/Gamma-Editor-MVP-Plan.md`, which makes the SadConsole Editor -> Preview -> Simulation -> Return loop the active Gamma checkpoint. Use this document for historical tester/demo goals and Console polish scope; do not treat Console breadcrumb work as the default next sprint unless it is explicitly re-selected.

Read when:

- reviewing tester/demo goals that should inform the Gamma Editor MVP;
- selecting short-term work whose purpose is external playtest feedback rather than adding new mechanics;
- deciding whether a UI request belongs in short-term Console polish or a future real frontend;
- curating current scenarios for tester-facing play.

Do not read when:

- selecting the immediate Gamma implementation sequence; use `docs/Plans/Gamma-Editor-MVP-Plan.md`.

Related source of truth:

- `docs/Source of Truth/Content-Authoring-Manual.md` records currently authorable scenario/content capabilities.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` records implemented cross-layer support.
- `docs/Source of Truth/invariants.md` records TDD traces when stable behavior changes.
- `docs/Plans/High-Level-Roadmap.md` owns backlog buckets and deferred features.
- `docs/Archived/Beta-Content-Exploration-Plan.md` records the paused/completed Beta exploration sequence.

## Goal

Prepare the existing authored scenarios for sharing with test players so feedback can guide future mechanics and frontend investment.

Gamma should not add broad new gameplay mechanics by default. It should make current scenarios easier to choose, understand, play, inspect, and give feedback on. Mechanics gates such as template spawning and reactions remain backlog items until player feedback or a specific scenario need re-promotes them.

## Frontend direction

The long-term UI concept is a breadcrumb/inspection-chain interface over entity containment paths. The current Console can prototype the read-only and simple display portions, but richer interactive breadcrumb panels should wait for a real frontend engine.

## Promoted Gamma stages

| Stage | Gamma scope | Related backlog / existing work | Promotion reason |
|---|---|---|---|
| 1. Read-only inspection path service | Completed in Sprint 22: Core structural, cycle-safe containment paths for entities, including upward ancestry, root-relative paths, max-depth truncation, and shared-root two-branch paths. Presentation/content enrichment is a follow-up concern. | Related to Bucket 1 compact world/state summaries and cycle-safe inventory/containment summaries; related to Bucket 6 runtime location/container ownership indexing as a future scale optimization, but should start as a simple read-only query. | Needed before any breadcrumb display can be reliable; reuses existing containment relationships without creating new gameplay semantics. |
| 2. Console breadcrumb display | Show current player path and currently inspected entity path in Console. Non-interactive text is enough for Gamma. | Related to Bucket 8 frontend requirement discovery and Sprint 21 Console scenario menu; also supports Bucket 1 scenario/debug readability. | Gives testers a clearer sense of where they are and what they are inspecting while keeping Console changes modest. |
| 3. Scenario/tester curation | Current manifest descriptions are sufficient for initial demoability; only revisit curation if testers are confused by scenario ordering, deprecated/crashy/headless-only entries, or naming. | Related to Bucket 5 scenario/content packaging, richer scenario metadata, and Sprint 21 manifest-only descriptions. Does not require scenario-definition metadata yet. | Curation is useful polish but no longer blocks the next Gamma breadcrumb/display work. |
| 4. Improved inspection panel content | Improve current Console inspection panels with clearer current-space/entity summaries and useful debug information such as path, inventory, local turn order, previous action, and scenario instructions where available. | Related to Bucket 1 compact state summaries, local turn-order/previous-action reporting, and future report/debug surfaces. | Helps testers and maintainers understand scenario behavior without needing headless reports or code inspection. |

## Deferred until a real frontend engine

These are not Gamma Console goals unless a later decision explicitly reopens them.

| Stage | Deferred backlog placement | Related features |
|---|---|---|
| 5. Interactive breadcrumb navigation | Bucket 8: Future integrated game/editor frontend. Keep as a future frontend requirement: select breadcrumb ancestors/entities, change inspection focus, and navigate nested spaces through the breadcrumb model. | Related to Bucket 10 future player control/action choice model if breadcrumb selection becomes part of action choice; related to editor/debug frontend requirements. |
| 6. Collapsible multi-entity inspection chain UI | Bucket 8: Future integrated game/editor frontend. Console may prototype text summaries, but collapsible/expandable panels, multi-entity chain layout, focus management, and scrolling should wait for a real frontend stack. | Related to Bucket 1 test inspector/runlog stepper and long-term integrated editor/debugger surfaces. |

## Gamma dependency order

1. Add read-only Core inspection path service with tests around structural containment path queries.
2. Display player/inspected breadcrumbs in Console using the shared Core path service.
3. Improve inspection panel content using existing debug/report data.
4. Revisit scenario/tester curation only if tester confusion or crash-prone menu entries make it necessary.

Scenario curation is currently non-blocking: existing authored descriptions are acceptable for initial tester sharing, and feedback can be gathered through separate channels rather than additional in-UI packaging.

## TDD / validation notes

- Prioritize tests around shared inspection-path/query services and catalog/manifest behavior, not brittle keyboard UI flows.
- Existing invariants related to cycle-safe containment traversal and Console scenario launch remain relevant.
- Console rendering can remain lightly tested; smoke/targeted tests are enough unless new behavior is moved into shared services.

## Out of scope for Gamma

- New major mechanics gates by default: template spawning, reaction system, combat systems, scheduler/speed, or player-input Action Step.
- Content package/import semantics.
- Full frontend replacement or technology selection unless Console clearly cannot support tester feedback.
- Collapsible multi-panel breadcrumb UI.
