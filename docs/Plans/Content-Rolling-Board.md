---
id: plan.content-rolling-board
title: Content Rolling Board
kind: rolling-board
status: active
owners: [content-editor]
audience: [content-editor, core-owner, frontend-owner]
lane: content
related:
  - source.content-authoring-manual
  - source.engine-editor-capabilities
  - source.capability-gap-log
  - plan.core-rolling-board
  - plan.frontend-sadconsole-rolling-board
---

# Content Rolling Board

Status: Active rolling board for content authoring, validation, scenario, and experiment-support slices.

Purpose: Track small, continuously updated Content work without creating a dedicated sprint document for every scenario/content slice. Move items from **Next** or **Later** into **Now** as capacity opens. Update acceptance notes as items complete.

## Board policy

- **Now**: the current implementation or documentation focus. Keep this short enough that active work is obvious.
- **Next**: likely upcoming work with enough clarity to start soon.
- **Later**: known follow-ups, dependency-bound work, or larger decisions.
- Prefer author-facing workflows, validation outcomes, and scenario evidence over task dumps.
- Content docs must distinguish implemented support, prototype/experimental support, and capability gaps.
- Preserve ownership boundaries: Content owns authoring guidance, YAML/editor/tooling, validation, materialization, and scenario fixtures; Core owns runtime semantics; frontend owns presentation.

## Now

No active Content-owned implementation item is currently in progress. Pull from **Next** when the next content-authoring slice starts.

## Recently completed

### Update content-facing semantics documentation before new implementation

**User story:** As a content author, I can confidently use currently implemented action/content semantics without reverse-engineering Core or Beta scenario files.

**Owners:** Content + Core.

**Completed:** 2026-08-23.

**Plan:**

- Update `docs/Source of Truth/Content-Authoring-Manual.md` for currently implemented action semantics and authoring workflows.
- Include status/limits for Create/Destroy/Polymorph, TargetPathMove, Push, costs, merged topology, and ecology-supporting patterns that already exist.
- Link or add gap-log entries when current semantics are deliberately incomplete.
- Prefer examples from existing Beta content where possible.

**Completion notes:**

- Content authors can tell which semantics are promoted, prototype-compatible, legacy, or unsupported.
- Existing content experiments are documented as supported examples or explicitly marked experimental.
- No new production code is required for this item.

## Next

### Topology polish and merged-space content experiments

**User story:** As a content author, I can use the completed functional topology foundation to author clear, inspectable merged-space experiments and identify which polish or vocabulary gaps should be promoted next.

**Owners:** Content, with Core collaboration required.

**Foundation status:** Functional graph topology, topology/POV rendering, source-cell-link seams, center-aligned merged-layer joins, and diagonal seam movement are complete. Current merged-layer `spaces[].origin` values remain projection/layout metadata only; cross-contributor runtime movement, POV, pathing, and adjacency require explicit joins/source-cell links.

**Plan:**

- Build or refine content examples that exercise intentional seams, negative/overlapping layout projection, room/hall loops, and player-readable POV/context presentation.
- Record which polish items matter most in practice: debug labels, seam visualization, non-center joins, asymmetric/one-way joins, intentional overlap/fold vocabulary, or topology-aware targeting distance.
- Add validation/reporting improvements when experiments expose confusing authoring failure modes.
- Extend authoring beyond the current cardinal center-aligned joins only after the desired authored semantics are demonstrated by content examples.

**Done when:**

- At least one promoted experiment demonstrates the completed topology foundation without relying on accidental layout-coordinate adjacency.
- Follow-up topology vocabulary is prioritized from content evidence rather than abstract capability wishlists.
- Core graph identity, Content YAML/materialization, and frontend/debug presentation remain aligned.

## Later

### Establish a Content-owned test suite for promoted fixtures and scenario catalogs

**User story:** As a content maintainer, I can evolve experimental Beta content while keeping promoted canonical scenarios and manifests under explicit Content-owned tests.

**Plan:**

- Move or split Content-owned checks out of the broad Core test project when they primarily validate YAML loading, authoring services, manifests, scenario fixtures, or playable catalog entries.
- Update tests that still depend on legacy actions to canonical examples when they protect current authoring semantics; delete tests that only prove retired legacy scenarios run.
- Define promotion criteria for canonical entities/scenarios before pinning them in tests, so experimental content can continue changing without false Core regressions.
- Keep feedback manifests free of legacy/retired-action scenarios unless intentionally marked as non-playable archival references outside the curated feedback path.

**Done when:**

- Promoted canonical content has clear fixture ownership and targeted test coverage.
- Experimental and legacy content do not silently gate Core semantic changes.
- Content test failures point to authoring/catalog/fixture ownership rather than ambiguous engine regressions.

### Prepare richer ecology experiment authoring and observability

**User story:** As a content experimenter, I can observe ecological dynamics through structured reports rather than final population counts only.

**Plan:**

- Add scenario ideas for births, deaths, consumption, cooldowns, density gates, and carrying-capacity loops.
- Prefer structured event/time-series reporting once Core/Content seams are selected.
- Coordinate with topology-aware targeting when experiments depend on local population pressure or awareness counts.

**Done when:**

- Ecology scenarios can explain why populations changed, not only what final counts were.
- Missing engine/editor capabilities are logged as capability gaps instead of hidden in scenario prose.
