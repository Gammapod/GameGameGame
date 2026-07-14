# Delta Point-of-View Release Plan

Status: Foundation/reference plan. Delta POV foundation has been implemented enough to support the new active canonical-actions vertical-slice direction; promote follow-up POV work only when canonical actions, Action Choice, or frontend presentation need it.

Read when:

- selecting Delta release work;
- deciding where observer-relative room/place, breadcrumb, bulk/aperture, or affordance semantics belong;
- planning frontend presentation that depends on the controlled entity's point of view;
- preparing future runtime control-source / Action Choice work where the player can command arbitrary entities.

Related source of truth:

- `docs/Source of Truth/invariants.md` records stable Core behavior contracts and test traces.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` records implemented Core/Editor/frontend-facing support tiers.
- `docs/Source of Truth/Content-Authoring-Manual.md` records what authors can safely create today.
- `docs/Source of Truth/Frontend-UX-Invariants.md` records frontend/shared-service boundaries and test traces.
- `docs/Source of Truth/Frontend-UX-Standards.md` records presentation and interaction standards.
- `docs/Plans/High-Level-Roadmap.md` records broader backlog ordering and deferred ideas.

## Delta release target

Delta should establish a finalized first-slice **point-of-view** model for any arbitrary entity, then prove that frontend/content presentation can consume that model without making the player entity special.

Target statement:

- Given any observer entity, the engine/shared services can describe that entity's structural and semantic context relative to the current world state.
- The observer's point of view is built on containment ancestry/breadcrumbs, a flexible current-place rule, and observer bulk versus place aperture data.
- Frontends consume point-of-view projections to present the world from the perspective of the entity currently being commanded or inspected.
- The first implementation avoids hardcoded player-only semantics and prepares for future runtime control-source / Action Choice work where player controls can be assigned to arbitrary entities.
- Rich affordance/adjective and reciprocal-awareness language is designed after the current-place and ratio foundation is stable.

## Core model principles

1. **Observer-relative by default**
   - Point of view is always queried for an observer entity ID.
   - Missing, deleted, or non-materialized observers should produce structured diagnostics rather than frontend guesses.

2. **Breadcrumb-backed, not parallel ancestry**
   - Point-of-view queries should build on the existing containment/breadcrumb/path service where possible.
   - The breadcrumb service answers “where is this entity structurally?”
   - The point-of-view service answers “what does this structure mean from this observer's perspective?”

3. **Flexible current-place resolution**
   - Do not permanently define “room/place” as “nearest ancestor without an action plan.”
   - Prefer a rule over containment/spatial ancestry with options such as max depth or stop-at quality.
   - A future content-authored or derived place quality can refine what counts as a room, container, body, object, or generic place.

4. **Ratios before categories**
   - Core/shared services should expose observer bulk, place aperture, and a named numeric ratio before committing to labels such as tight, narrow, spacious, small, or large.
   - Preferred first numeric field: `BulkToApertureRatio = observerBulk / placeAperture`.
   - Height/width and area interpretation should be deferred or exposed as raw facts until gameplay/frontend needs clarify useful thresholds.

5. **Affordance adjectives come from action capability metadata**
   - A future action step that has success criteria should be able to declare what its using entity sees as an adjective on matching entities, such as portable, pushable, stabbable, enterable, or breakable.
   - Reciprocal adjectives should be explicit metadata, not automatic for every action. Example: if a goblin sees the player as stabbable, the player may see the goblin as hostile when the action metadata declares that reciprocal relation.
   - The model should distinguish “what I can do to them” from “what they can do to me.”

6. **Projection first, action dependency later**
   - The first Delta slices should be read-only projection/query work.
   - Future action steps may consume point-of-view concepts, such as “affect all entities in my current room,” only after the point-of-view invariants stabilize.

## Must Have requirements

### Core point-of-view foundation

1. Add a Core/shared point-of-view query service or equivalent API for an arbitrary observer entity.
2. Reuse existing containment/breadcrumb/path behavior rather than duplicating ancestry traversal.
3. Return observer identity, containment path/breadcrumbs, selected current-place candidate, and the rule/basis used to select it.
4. Return observer bulk, place aperture, and `BulkToApertureRatio` when the data is available.
5. Return structured diagnostics for missing observer, missing path/place, or missing bulk/aperture facts.
6. Add stable Core tests and invariant traces for the selected first-slice behavior.

### Flexible place selection

1. Support at least the initial current-place rule needed for frontend MVP.
2. Keep the rule explicit in the result so later changes can be understood and tested.
3. Allow bounded breadcrumb queries such as up to `n` ancestors, and/or prepare an option for stopping at a derived or authored quality.

### Frontend/content MVP

1. Expose a content/frontend-facing projection over the Core point-of-view facts.
2. Let the frontend present the observer's current place and parent breadcrumbs without hardcoding player-only assumptions.
3. Display or otherwise surface bulk/aperture ratio facts in a simple form before polished size language is finalized.
4. Preserve frontend responsibility for layout, wording, focus, and visual treatment while keeping semantic facts in shared services.

### Documentation and capability tracking

1. Update `invariants.md` when stable Core point-of-view behavior lands.
2. Update `Engine-Editor-Capabilities.md` when point-of-view support tier or layer coverage changes.
3. Update `Content-Authoring-Manual.md` only when content authors can author or rely on new point-of-view/adjective/place-quality capabilities.

## Could Have requirements

1. Derived place qualities, such as room-like, container-like, body-like, object-like, or unknown.
2. Content-authored place-quality metadata if derived rules prove insufficient.
3. Raw width, height, area, occupancy, or navigable-space facts in the projection.
4. First small affordance/adjective metadata slice for one or two action steps.
5. Reciprocal adjective proof, such as stabbable -> hostile, if a suitable existing or new action-step capability is selected.
6. Point-of-view-aware action prompt labels in frontend MVP.
7. Debug view explaining why a current place or adjective was selected.

## Explicit non-goals for Delta MVP

- Implementing full runtime control-source / Action Choice control semantics.
- Making point-of-view action execution authoritative before projection invariants are stable.
- Finalizing all size labels or thresholds in Core.
- Finalizing height/width/area semantics.
- Adding broad new gameplay mechanics solely to demonstrate point of view.
- Making the frontend decide engine facts such as current place, legality, or affordance matches.

## Multi-phase implementation plan

### Phase 0: Alignment and invariant planning

Goal: turn the Delta target into testable Core outcomes before production code changes.

Scope:

1. Review current breadcrumb/containment path services and tests.
2. Trace affected existing invariants or record that no existing invariant covers point-of-view semantics yet.
3. Define the first current-place rule and the first DTO/result shape.
4. Add intentionally failing tests for observer path, current place, ratio, and diagnostics.

Exit criteria:

- The first Core point-of-view slice has explicit testable outcomes and invariant trace expectations.

Invariant/test trace for Phase 1:

- Affected existing invariants: Entity And Space / cycle-safe traversal; Inventory, Bulk, And Aperture / entity bulk and aperture facts. No previous invariant directly covers point-of-view semantics.
- Existing tests to preserve: `EntityContainmentPathServiceBuildsUpwardPathForNestedEntity`, `EntityContainmentPathServiceLimitsUpwardPathByMaxDepth`, `EntityContainmentPathServiceReportsMissingEntity`, `EntityContainmentPathServiceDetectsContainmentCycle`, and bulk/aperture transition tests listed in `invariants.md`.
- New intentionally failing tests: `PointOfViewUsesContainmentBreadcrumbsAndSelectsNearestContainerAsCurrentPlace`, `PointOfViewReportsObserverBulkPlaceApertureAndRatio`, `PointOfViewReportsMissingObserverDiagnostic`, `PointOfViewReportsNoCurrentPlaceWhenObserverHasNoContainingInventoryOwner`, and `PointOfViewPreservesBreadcrumbTruncationFromQueryOptions`.

### Phase 1: Core point-of-view foundation

Goal: implement the observer-relative structural and ratio model.

Scope:

1. Add the Core/shared query service.
2. Integrate with existing breadcrumb/path behavior.
3. Return current place, path, selection basis, bulk/aperture facts, ratio, and diagnostics.
4. Keep the service read-only.

Exit criteria:

- Core tests pass for arbitrary observer entities, nested containment, current-place selection, ratio calculation, and missing-data diagnostics.

Current status:

- Initial Core read-only `PointOfViewService` foundation is implemented and traced by `PointOfViewServiceTests`. Content/frontend projection, place qualities, and affordance/adjective language remain follow-up phases.

### Phase 2: Flexible place and breadcrumb options

Goal: avoid hardcoding one brittle room rule.

Scope:

1. Add bounded breadcrumb options such as max ancestor depth if needed by the frontend MVP.
2. Prepare or implement stop-at-quality selection if a derived quality exists in the first slice.
3. Ensure the result reports which rule selected the current place.

Exit criteria:

- Callers can request useful parent breadcrumbs and understand why a current place was selected.

### Phase 3: Frontend/content MVP projection

Goal: prove the point-of-view model can drive presentation.

Scope:

1. Add content/frontend-facing projection DTOs or adapters over the Core facts.
2. Surface current place, breadcrumbs, and bulk/aperture ratio in the frontend.
3. Keep presentation simple and avoid final size/adjective language unless supported by shared facts.

Exit criteria:

- The frontend can present a non-player-special observer perspective from shared point-of-view facts.

Invariant/test trace for initial Content projection seam:

- Affected existing frontend boundary invariant: frontends do not invent simulation semantics; entity panel projections provide frontend-neutral facts while layout/wording remains frontend-owned.
- Existing tests to preserve: `EntityPanelProjectionCombinesIdentityPathStateGridAndContents`, `EntityPanelProjectionIncludesStructuredLocalLogSnippetsWhenAnchored`, and Core `PointOfViewServiceTests`.
- New intentionally failing tests: `EntityPanelProjectionIncludesPointOfViewFactsForProjectedEntity` and `EntityPanelProjectionCarriesPointOfViewDiagnosticsWithoutFrontendGuessing`.

Current status:

- Initial Content projection seam is implemented through `EntityPanelProjectionService`. Entity panel projections now include point-of-view current-place facts, bulk/aperture ratio, and structured diagnostics. SadConsole presentation/wording is not yet updated.

### Phase 4: Affordance/adjective design slice

Goal: design the next semantic layer without blocking the MVP foundation.

Scope:

1. Define action-step metadata for observer-facing adjectives.
2. Define explicit reciprocal adjective metadata.
3. Pick one or two existing/future action capabilities as proofs, only if the foundation is stable.
4. Decide whether adjective matching is capability-based, current-legality-based, or both for the selected proof.

Exit criteria:

- A small tested adjective/reciprocal model exists or a concrete follow-up plan is recorded.

Current status:

- Initial one-way target-adjective projection is implemented. `PointOfViewService` can derive adjectives from target-capability Action Steps in the observer's supplied action-plan descriptor and checks those capabilities against other entities in the current place through existing non-mutating affordance queries. Current proof adjectives include portable, enterable, pushable, breakable, receivable, and takeable as frontend-facing labels over Action Step capabilities. Reciprocal awareness remains follow-up work.
- Reciprocal target-adjective projection is implemented for the same capability/adjective vocabulary. `PointOfViewService` can derive reciprocal adjectives from other current-place entities' supplied action-plan descriptors and checks those capabilities against the observer. The `--play-mock` frontend consumes the projection and displays separate `adjectives ...` and `reciprocal ...` groups without recomputing semantics.

### Phase 5: Polish and release decision

Goal: decide whether Delta is ready to become the foundation for arbitrary-entity player control.

Scope:

1. Refine frontend wording and debug display.
2. Update capability/source-of-truth docs.
3. Decide whether the next release should promote runtime control-source / Action Choice or continue point-of-view affordance work.

Exit criteria:

- Point-of-view is stable enough to inform frontend presentation and future arbitrary controlled-entity work.
