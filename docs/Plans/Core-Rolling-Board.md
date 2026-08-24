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

No active Core-owned implementation item is currently in progress. Pull from **Next** when the next Core slice starts.

## Next

No Core-owned item is currently queued. Pull from **Later** or the Content/Frontend boards when the next Core slice is selected.

## Later

### Split Core semantics tests from Content-owned scenario fixture tests

**User story:** As maintainers, we can promote engine semantics and experiment with authored content without legacy scenario fixtures making Core test intent ambiguous.

**Owners:** Core + Content.

**Plan:**

- Audit tests that assert legacy action behavior. Update them to canonical equivalents when they protect a still-current invariant, or delete them outright when they only preserve retired behavior.
- Delete tests whose only purpose is proving a specific legacy scenario still runs.
- Identify which tests in `tests/GameGameGame.Tests` are truly Core semantic contracts and which are Content-owned fixture, YAML, scenario, catalog, or authoring checks.
- Plan a true Content-owned test suite once enough canonical entities/scenarios are promoted to make fixture lifecycle and ownership explicit.

**Done when:**

- Core tests primarily assert runtime/service invariants over controlled fixtures.
- Content tests own authored scenario validity, promoted canonical fixture coverage, manifests, and authoring workflows.
- Legacy/prototype scenario coverage is either deleted or explicitly quarantined as archival/reference-only.

### Introduce an action workflow descriptor seam

**User story:** As a maintainer, I can change the workflow for an individual user-facing action without editing unrelated action switches across Core and frontend code.

**Owners:** Core + Frontend.

**Priority dependency:** Old-frontend quarantine is complete; do when action UX churn becomes the selected bottleneck.

**Plan:**

- Define a descriptor seam for action-choice presentation/workflow metadata without moving legality or execution out of Core.
- Capture per-action target source, follow-up prompt stages, submission shape, highlight semantics, and log/animation affordances.
- Migrate existing Move/Pickup/Drop/Enter/Exit/Transfer/Push workflow facts incrementally.

**Done when:**

- Adding or changing one action workflow does not require parallel ad-hoc switch edits in multiple frontend components.
- Core remains authoritative for choices and command submission.
- Frontend remains authoritative for focus, layout, and presentation.
