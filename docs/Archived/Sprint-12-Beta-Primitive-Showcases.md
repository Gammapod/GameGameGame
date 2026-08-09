# Sprint 12: Beta Primitive Showcases

Status: Completed / archived.

Original active plan location:

- `docs/Plans/Sprint-12-Beta-Primitive-Showcases.md`

Related current plans:

- `docs/Plans/Beta-Content-Exploration-Plan.md`
- `docs/Source of Truth/Capability-Gap-Log.md`
- `docs/Plans/High-Level-Roadmap.md`
- `docs/Source of Truth/Engine-Editor-Capabilities.md`

## Sprint goal

Create and validate the first beta gameplay vignettes using currently authorable capabilities, one primitive showcase at a time. Each showcase should be defined as content, tested headlessly until the content editor is satisfied, then handed to the user for manual Console testing and discussion before moving to the next showcase.

## Completed scope

Sprint 12 established the beta current-tool showcase workspace and authored the first beta content package under:

- `src/GameGameGame.Content/Beta/CurrentTools/`

Completed scenarios:

1. **`beta-push-showcase`**
   - File: `src/GameGameGame.Content/Beta/CurrentTools/PushFacingShowcase.yaml`
   - Demonstrates successful `PushFacing` and blocked push failure.
   - Console-playable and headlessly validated.

2. **`beta-destroy-showcase`**
   - File: `src/GameGameGame.Content/Beta/CurrentTools/DestroyTargetShowcase.yaml`
   - Demonstrates `MoveFacing -> DestroyTarget` against a blocker and safe failed self/no-target destruction behavior.
   - Console-playable and headlessly validated.

3. **`beta-create-showcase`**
   - File: `src/GameGameGame.Content/Beta/CurrentTools/CreateFacingShowcase.yaml`
   - Demonstrates successful `CreateFacing` and blocked creation in headless reports.
   - Headlessly validated, but not Console-playable when successful creation occurs because of GAP-001.

4. **`beta-drop-showcase`**
   - File: `src/GameGameGame.Content/Beta/CurrentTools/DropFacingShowcase.yaml`
   - Demonstrates successful `DropFacing`, empty inventory failure, and blocked destination failure.
   - Console-playable and headlessly validated.

5. **`beta-pickup-drop-weight`**
   - File: `src/GameGameGame.Content/Beta/CurrentTools/PickupDropWeightShowcase.yaml`
   - Demonstrates light pickup success, heavy pickup failure, and manual player pickup/drop/weight experimentation.
   - Console-playable and headlessly validated.

6. **`beta-behavior-chain-composition`**
   - File: `src/GameGameGame.Content/Beta/CurrentTools/BehaviorChainCompositionShowcase.yaml`
   - Demonstrates composed fallback chains: `MoveFacing -> PushFacing`, `MoveFacing -> DestroyTarget`, and `MoveFacing -> PickupTarget`.
   - Console-playable and headlessly validated.

Test coverage was consolidated in:

- `tests/GameGameGame.Tests/BetaContentFixtureTests.cs`

Final observed verification during the sprint:

- targeted beta fixture tests passed;
- full test suite passed with 282 tests;
- existing `Tmds.DBus.Protocol` package vulnerability warning remained unrelated to sprint content.

## Capability gaps discovered

### GAP-001: `CreateFacing` placeholder entities lack content-template/presentation assignment

Discovered while manually testing `beta-create-showcase` in Console.

Summary:

- `CreateFacing` creates a runtime `placeholderRock` entity.
- The entity is not spawned through content registry template materialization.
- Console rendering/inspection asks `PrototypeContentRegistry` for the created entity's presentation.
- No entity-template assignment exists, so Console can crash after successful creation.

Tracking entry:

- `docs/Source of Truth/Capability-Gap-Log.md`

Implication:

- `CreateFacing` remains useful for headless proof of creation semantics.
- Console-playable creation/spawning vignettes need either placeholder presentation binding or template-backed creation such as `CreateFacing(templateId)` / `SpawnTemplateFacing`.

## Deferred from Sprint 12

The curated actor zoo was intentionally deferred. The current-tool primitive showcases and composition/weight scenarios proved enough of the pre-gate surface to proceed toward the first beta gate without adding another scenario family immediately.

Deferred actor-zoo direction:

- curated one-room actor demonstrations remain useful later;
- automated actor isolation previews remain a reporting/tooling backlog idea;
- actor-zoo work should be revisited after the Direction Transform gate, when turners, patrollers, bouncers, and backsteppers can make the zoo more informative.

## Recommendation after Sprint 12

Proceed toward the first beta gate: the Direction Transform batch.

Requested/expected gate capabilities:

- `ReverseFacing`
- `TurnLeft`
- `TurnRight`
- `Backstep`

Rationale:

- Current-tool primitive showcases have now exercised push, destroy, create, drop, pickup/weight, and behavior-chain composition.
- The next large content limitation is not another current primitive, but the lack of relative facing transforms for patrols, bouncers, sentries, and richer local movement behaviors.
