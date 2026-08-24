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

## Next

### Ecology and topology content experiments

**User story:** As a content author, I can use the completed topology/POV and Octagonal-distance foundation to author ecology-focused experiments, inspect their behavior, and identify which engine/content gaps should be promoted next.

**Owners:** Content, with Core collaboration required.

**Foundation status:** Functional graph topology, topology/POV rendering, Octagonal-distance visibility/targeting, source-cell-link seams, center-aligned merged-layer joins, and diagonal seam movement are complete. Current merged-layer `spaces[].origin` values remain projection/layout metadata only; cross-contributor runtime movement, POV, pathing, and adjacency require explicit joins/source-cell links.

**Plan:**

- Build or refine ecology examples that exercise births, deaths, consumption, carrying-capacity pressure, local pursuit/avoidance, and Octagonal-distance targeting.
- Use intentional seams, negative/overlapping layout projection, room/hall loops, and player-readable POV/context presentation when they help ecology scenarios rather than as abstract topology demos.
- Record which polish items matter most in practice: debug labels, seam visualization, non-center joins, asymmetric/one-way joins, intentional overlap/fold vocabulary, cooldowns, target sets, awareness counts, or density predicates.
- Add validation/reporting improvements when experiments expose confusing authoring failure modes.
- Extend authoring beyond the current cardinal center-aligned joins only after the desired authored semantics are demonstrated by content examples.

**Done when:**

- At least one promoted ecology experiment demonstrates the completed topology/Octagonal-distance foundation without relying on accidental layout-coordinate adjacency.
- Follow-up topology/ecology vocabulary is prioritized from content evidence rather than abstract capability wishlists.
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
