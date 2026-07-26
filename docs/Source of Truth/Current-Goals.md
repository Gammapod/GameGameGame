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
  - plan.high-level-roadmap
  - plan.canonical-actions-vertical-slice
  - plan.sadconsole-frontend-roadmap
  - plan.beta-capability-gap-log
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

The active release direction is canonical action vertical slices: freeze the current broad Action Step catalog as legacy/prototype-compatible, then promote actions one at a time with engine rules, POV/affordance facts, frontend log IDs, content test rooms, editor support, componentized play-mode consumption, and Core-owned runtime control-source / Action Choice support for arbitrary controlled entities.

## Active planning bridge

- Active roadmap: `docs/Plans/High-Level-Roadmap.md`.
- Active release plan: `docs/Plans/Canonical-Actions-Vertical-Slice-Plan.md`.
- Recently archived focused sprint plan: `docs/Archived/Topology-Service-Phase-1-Sprint-Plan.md`.
- Broader frontend backlog/reference: `docs/Plans/SadConsole-Frontend-Roadmap.md`.
- Content/scenario capability gaps: `docs/Plans/Beta-Capability-Gap-Log.md`.
- Recent process observations and open retrospective questions: `docs/Plans/Sprint-Retrospective.md`.
- Historical context: `docs/Archived/`, read only when current docs link to an archived document or when investigating why a system was shaped a certain way.

## Current highest-priority backlog buckets

Trust `docs/Plans/High-Level-Roadmap.md` for detailed priority order, dependencies, defer reasons, and promotion triggers. The short current ordering is:

1. Select the next canonical action vertical slice from the roadmap backlog using the completed Move, Pickup/Drop seam, Enter/Exit, and Transfer evidence.
2. Canonical runtime control-source / Action Choice model follow-through.
3. Componentized Gamma play-mode follow-through over canonical action/Action Choice/POV/log services.
4. Delta point-of-view follow-through where needed by canonical actions.
5. Gamma SadConsole Editor MVP and broader SadConsole/debug-browser contract follow-through.
6. Scenario/testing/tooling feedback loop and scenario/content packaging.
7. Deferred mechanics/content systems: movement/peer primitives, inventory/containment/transfer follow-ups, spawning/templates, runtime scale, behavior reuse, reactions, and long-horizon diegetic/meta systems.

## Maintenance rule

Keep this document short. It should name the selected direction and point to active planning documents rather than duplicating roadmap sections, completed checklists, or detailed implementation plans.
