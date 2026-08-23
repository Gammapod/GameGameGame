---
id: source.current-goals
title: Current Goals
kind: source-of-truth
subkind: current-goals
status: active
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
lane: current-goals
truth_rank: 25
truth_domains: [planning-priority]
read_when:
  - deciding what project direction is currently selected
  - bridging source-of-truth facts to active roadmap and planning documents
  - checking which active plan or reference log owns follow-up planning context
related:
  - source.planning-index
  - plan.core-rolling-board
  - plan.content-rolling-board
  - plan.frontend-sadconsole-rolling-board
  - source.capability-gap-log
  - plan.sprint-retrospective
---
# Current Goals

Status: Source of truth for the mostly stable current project direction and active planning bridge.

Read when:

- deciding what project direction is currently selected;
- selecting which active roadmap, sprint/release plan, or reference log to inspect next;
- checking whether an implementation idea aligns with current goals before opening detailed planning docs.

Do not read when:

- looking for stable behavior/test traces; use `docs/Source of Truth/invariants.md`;
- checking implemented layer support; use `docs/Source of Truth/Engine-Editor-Capabilities.md`;
- checking content-authoring permission; use `docs/Source of Truth/Content-Authoring-Manual.md`;
- navigating documentation lanes only; use `docs/Source of Truth/planning-index.md` or the compiled documentation graph.

## Current strategic priority

The active direction is a stabilization-and-content-experiment follow-through after completing user-facing semantics for most current actions and the functional topology/POV foundation: keep Core/Content semantics documented, keep the new SadConsole frontend as the normal maintained frontend, then use the rolling boards for topology polish, authored content experiments, topology-aware targeting when experiments need it, action workflow maintainability, and user-facing log semantics.

## Active planning bridge

- Active Core rolling board: `docs/Plans/Core-Rolling-Board.md`.
- Active Content rolling board: `docs/Plans/Content-Rolling-Board.md`.
- Active Frontend rolling board: `docs/Plans/Frontend-SadConsole-Rolling-Board.md`.
- Active focused sprint plan: none selected; use the relevant rolling board when selecting the next Core, Content, or Frontend slice.
- Recently archived planning docs: `docs/Archived/High-Level-Roadmap.md`, `docs/Archived/SadConsole-Frontend-Roadmap.md`, `docs/Archived/Multi-Document-Content-Workspace-Compiler-Sprint-Plan.md`, `docs/Archived/Frontend-SadConsole-Workspace-Browser-Sprint-Plan.md`, and `docs/Archived/Ecology-Baseline-Metrics.md`.
- Content/scenario capability gaps: `docs/Source of Truth/Capability-Gap-Log.md`.
- Recent process observations and open retrospective questions: `docs/Archived/Sprint-Retrospective.md`.
- Historical context: `docs/Archived/`, read only when current docs link to an archived document or when investigating why a system was shaped a certain way.

## Current highest-priority backlog buckets

Trust the three active rolling boards for detailed priority order, dependencies, defer reasons, and promotion triggers. The short current ordering is:

1. Use the completed topology/POV foundation for polish and content experiments: clearer debug/presentation affordances, intentional merged/overlapping/folded-space examples, and scenario evidence for authored seams.
2. Implement topology-aware targeting distance semantics in Core/Content when a selected content experiment needs graph-distance candidate ordering: all eight adjacent cells to a target entity count as distance `0`, with further cells using Manhattan distance from that adjacency boundary.
3. Expand merged topology authorship beyond current center-aligned joins for overlapping/non-euclidean content experiments, Content-owned with Core collaboration.
4. Introduce an action workflow descriptor seam when action UX churn becomes the selected bottleneck.
5. Add a dedicated user-facing Log component with Core/Content collaboration on structured action outcomes when gameplay readability is the selected frontend/content slice.

## Maintenance rule

Keep this document short. It should name the selected direction and point to active planning documents rather than duplicating roadmap sections, completed checklists, or detailed implementation plans.
