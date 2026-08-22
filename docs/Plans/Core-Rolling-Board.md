---
id: plan.core-rolling-board
title: Core Rolling Board
kind: rolling-board
status: active
owners: [core-owner]
audience: [core-owner, frontend-owner, content-editor]
lane: core
related:
  - source.invariants
  - source.engine-editor-capabilities
  - source.content-authoring-manual
  - plan.frontend-sadconsole-rolling-board
  - plan.content-rolling-board
---

# Core Rolling Board

Status: Active rolling board for Core-owned engine/editor-parity and build/workflow slices.

Purpose: Track small, continuously updated Core work without creating a dedicated sprint document for every slice. Move items from **Next** or **Later** into **Now** as capacity opens. Update acceptance notes as items complete.

## Board policy

- **Now**: the current implementation focus. Keep this short enough that active work is obvious.
- **Next**: likely upcoming work with enough clarity to start soon.
- **Later**: known follow-ups, dependency-bound work, or larger decisions.
- Prefer behavior contracts, testable outcomes, and vertical-slice boundaries over task dumps.
- For stable behavior changes, follow the TDD workflow in `docs/Source of Truth/testing-charter.md` and trace affected invariants from `docs/Source of Truth/invariants.md` before production changes.
- Preserve layer ownership: Core owns runtime semantics and shared service contracts; Content owns authoring guidance and content fixtures; frontend owns presentation, selection, focus, layout, and animation.

## Now

### Update content-facing semantics documentation before new implementation

**User story:** As a content author or maintainer, I can understand the current implemented action/content semantics without reading engine source before we start the next implementation slice.

**Owners:** Core + Content.

**Plan:**

- Update `docs/Source of Truth/Content-Authoring-Manual.md` for currently implemented semantics, including newer actions and known limits.
- Reconcile `docs/Source of Truth/Engine-Editor-Capabilities.md` and action-step source-of-truth docs with actual Core/Content support where they lag.
- Clearly classify promoted canonical, prototype-compatible, and legacy/retired action semantics.
- Prefer examples that point to existing content/scenario fixtures rather than inventing new runtime behavior.

**Done when:**

- Content authors can tell which existing semantics are safe to use today.
- Known limits for Create/Destroy/Polymorph, TargetPathMove, Push, costs, merged topology, and ecology-supporting semantics are explicitly documented or linked to gap logs.
- No new production code is required for this item.

### Quarantine old frontend from default build/package path

**User story:** As a developer, I want default build, test, run, and feedback-package workflows to treat `GameGameGame.Frontend.SadConsole` as the real frontend and keep the old `GameGameGame.SadConsole` only as explicit reference material.

**Priority dependency:** Start after the content-facing semantics documentation update above.

**Plan:**

- Remove old frontend and old frontend tests from default solution/CI/package dependencies, or move them behind an explicit opt-in reference-only workflow.
- Publish/package `src/GameGameGame.Frontend.SadConsole` for feedback builds.
- Update README/workflow docs so normal commands target the new frontend.
- Keep old source available for reference unless a separate archival/removal decision is made.

**Done when:**

- Default `dotnet build`/CI/feedback package no longer depends on the old frontend.
- Legacy frontend status is explicit and cannot silently gate normal work.
- The new frontend is the documented run/package target.

## Next

### Add topology-aware targeting distance semantics

**User story:** As a behavior author, I can target entities using distance semantics that survive merged, folded, and non-euclidean topology experiments.

**Distance rule:** Cells adjacent to an entity in all eight directions count as distance `0` from that entity. Further cells use Manhattan distance from that adjacency boundary.

**Plan:**

- Specify the Core distance helper before changing targeting behavior.
- Decide how the rule composes with graph/materialized topology links and same-cell/source-cell projections.
- Update targeting candidate ordering/preview/reporting through shared services rather than frontend guesses.
- Add tests for ordinary grids, diagonal adjacency, merged/source-cell links, and edge cases around occupied/current cells.

**Done when:**

- Targeting uses the documented distance rule through a shared Core/Content seam.
- Existing targeting invariants remain traced or explicitly superseded.
- Content/editor previews can explain why candidates were ordered or selected.

## Later

### Introduce an action workflow descriptor seam

**User story:** As a maintainer, I can change the workflow for an individual user-facing action without editing unrelated action switches across Core and frontend code.

**Owners:** Core + Frontend.

**Priority dependency:** Do after the old frontend is quarantined from default workflows.

**Plan:**

- Define a descriptor seam for action-choice presentation/workflow metadata without moving legality or execution out of Core.
- Capture per-action target source, follow-up prompt stages, submission shape, highlight semantics, and log/animation affordances.
- Migrate existing Move/Pickup/Drop/Enter/Exit/Transfer/Push workflow facts incrementally.

**Done when:**

- Adding or changing one action workflow does not require parallel ad-hoc switch edits in multiple frontend components.
- Core remains authoritative for choices and command submission.
- Frontend remains authoritative for focus, layout, and presentation.
