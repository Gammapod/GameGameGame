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

The active direction is a stabilization-and-topology follow-through after completing user-facing semantics for most current actions: document the current Core/Content semantics, make the new SadConsole frontend the normal maintained frontend, then advance topology/POV, topology-aware targeting, merged/overlapping topology authorship, action workflow maintainability, and user-facing log semantics through the rolling boards.

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

1. Update Core/Content-owned content-facing semantics documentation based on existing implemented behavior before selecting new implementation work.
2. Quarantine the old SadConsole frontend from default build/test/package workflows and make `GameGameGame.Frontend.SadConsole` the documented real frontend.
3. Make the new frontend topology/POV based so play rendering can show reachable spaces across layer boundaries rather than one room plane.
4. Implement topology-aware targeting distance semantics in Core/Content: all eight adjacent cells to a target entity count as distance `0`, with further cells using Manhattan distance from that adjacency boundary.
5. Expand merged topology authorship for overlapping/non-euclidean content experiments, Content-owned with Core collaboration.
6. Later: introduce an action workflow descriptor seam after old-frontend quarantine, and add a dedicated user-facing Log component with Core/Content collaboration on structured log semantics.

## Maintenance rule

Keep this document short. It should name the selected direction and point to active planning documents rather than duplicating roadmap sections, completed checklists, or detailed implementation plans.
