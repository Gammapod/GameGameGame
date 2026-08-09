---
id: plan.target-path-movement-vertical-slice-sprint
title: Target Path Movement Vertical Slice Sprint Plan
kind: plan
status: archived
truth_rank: 40
truth_domains: [planning-priority, implementation-navigation, test-trace]
owners: [core-owner, content-editor]
audience: [core-owner, content-editor, frontend-owner]
read_when:
  - implementing target-aware pathfinding movement Action Steps
  - changing SeekTarget FleeTarget MaintainChebyshevDistanceTwo StrafeClockwise or StrafeAnticlockwise semantics
  - authoring or validating target-path movement YAML
related:
  - source.invariants
  - source.testing-charter
  - source.engine-editor-capabilities
  - source.content-authoring-manual
  - source.action-step-outcome-and-affordance-logic
  - source.vertical-slice-map
---

# Target Path Movement Vertical Slice Sprint Plan

Status: Active focused sprint plan for promoting target-aware movement to a canonical pathfinding-backed Action Step. This records the agreed design direction before implementation; production code changes must follow the TDD workflow in `docs/Source of Truth/testing-charter.md`.

## Goal

Keep `Move` as facing-relative/tank-control movement, and introduce a canonical target-path movement Action Step for authored action plans that can seek, flee, maintain distance, and orbit a target using real pathfinding.

The slice should also produce user-facing scenarios that demonstrate:

1. distinct path-movement failure logs;
2. seeking/fleeing entities successfully navigating a maze;
3. two entities orbiting the player in different directions and at different distances.

## Agreed design decisions

- `Move` remains directional/facing-relative and is not overloaded with target-path modes.
- New canonical authoring should use one parameterized target-path movement step, tentatively `TargetPathMove`.
- Existing one-off target movement steps (`SeekTarget`, `FleeTarget`, `MaintainChebyshevDistanceTwo`, `StrafeClockwise`, `StrafeAnticlockwise`) remain loadable/runtime-compatible but should be hidden from new canonical authoring once the new step is available.
- Content-editor signoff is required on the final YAML shape and author-facing wording before implementation begins.
- Diagonal movement is allowed and must use the same Core topology/movement legality rules as canonical movement.
- Distance is measured to the nearest legal target-adjacent space, not to the target's occupied space. Seek path selection uses shortest legal movement steps; distance-band behavior (`FleeAdjacency`, `MaintainDistance`, and `Orbit`) uses the same legal movement graph with cardinal moves costed as `1` and diagonal moves costed as `1.5` rounded down to the authored integer band, producing octagonal rather than square orbit rings.
- `desiredDistance` is the canonical distance field name.
- Orbiting off-ring first corrects inward/outward toward `desiredDistance`; once on-ring, orbiting attempts the deterministic next ring step.
- If the deterministic next orbit step is blocked/unavailable, the step falls through rather than searching ahead around the ring.

## Proposed authoring model

Minimal YAML shape:

```yaml
behavior:
  - kind: TargetPathMove
    targetLabel: enemy
    pathMode: SeekAdjacency
```

Parameterized examples:

```yaml
# Move one step along the shortest legal path toward target adjacency.
- kind: TargetPathMove
  targetLabel: enemy
  pathMode: SeekAdjacency

# Move one step that increases path distance from target adjacency.
- kind: TargetPathMove
  targetLabel: enemy
  pathMode: FleeAdjacency

# Seek or flee to reach a distance of 3 from target adjacency.
- kind: TargetPathMove
  targetLabel: enemy
  pathMode: MaintainDistance
  desiredDistance: 3

# Follow the distance-6 ring clockwise around the target.
- kind: TargetPathMove
  targetLabel: player
  pathMode: Orbit
  desiredDistance: 6
  orbitDirection: Clockwise
```

Proposed enums:

- `pathMode`: `SeekAdjacency`, `FleeAdjacency`, `MaintainDistance`, `Orbit`
- `orbitDirection`: `Clockwise`, `Anticlockwise`

Compatibility mapping for old step kinds:

| Existing step | New canonical equivalent |
|---|---|
| `SeekTarget` | `TargetPathMove` with `pathMode: SeekAdjacency` |
| `FleeTarget` | `TargetPathMove` with `pathMode: FleeAdjacency` |
| `MaintainChebyshevDistanceTwo` | `TargetPathMove` with `pathMode: MaintainDistance`, `desiredDistance: 2` |
| `StrafeClockwise` | `TargetPathMove` with `pathMode: Orbit`, `orbitDirection: Clockwise`; default/compatibility distance to be resolved during implementation review |
| `StrafeAnticlockwise` | `TargetPathMove` with `pathMode: Orbit`, `orbitDirection: Anticlockwise`; default/compatibility distance to be resolved during implementation review |

## Runtime semantics

For all `TargetPathMove` modes:

1. Read the authored target reference from the executing actor's canonical action state.
2. Require actor and target to exist on the same plane.
3. Build a target-adjacency set from legal nodes adjacent to the target's occupied node, excluding the target's own occupied node.
4. Use Core topology and movement legality for graph traversal and one-step relocation, including eight-way/diagonal movement, diagonal corner blocking, directed topology policy, bounds, and occupancy.
5. Execute at most one adjacent move per Action Step attempt.
6. On successful movement, consume the turn, preserve `Target`, and set `Facing` to the actual absolute direction moved.
7. On fallthrough/failure, preserve `Target` and preserve `Facing` unless another step changes it.
8. Emit structured trace/outcome details sufficient for scenario reports and future frontend logs.

Mode details:

- `SeekAdjacency`: move one step along the shortest legal path to the nearest reachable target-adjacent node. If already at distance `0`, fall through so later interaction steps can run.
- `FleeAdjacency`: move one legal step that increases distance-band value from the target-adjacency set. If no legal increasing step exists, fall through.
- `MaintainDistance`: if current distance is greater than `desiredDistance`, seek inward; if less, flee outward; if equal, fall through.
- `Orbit`: if current distance is not `desiredDistance`, correct inward/outward as maintain-distance movement would; if current distance equals `desiredDistance`, attempt the deterministic next ring step in `orbitDirection`; if that specific next ring step is illegal/unavailable, fall through.

## Scope

### In scope

1. Core pathfinding/distance-field support for target-adjacent movement over existing topology/movement legality.
2. Canonical `TargetPathMove` descriptor/runtime handler.
3. Compatibility runtime/load handling for old target movement step kinds.
4. YAML/descriptor load, save, round-trip, and validation support for `pathMode`, `desiredDistance`, and `orbitDirection`.
5. Action Step catalog, preview, editor-service, agent/headless API, and SadConsole editor parity for new canonical authoring.
6. Hiding old one-off target movement steps from new canonical authoring while preserving compatibility.
7. Structured failure/fallthrough reasons for target missing, different plane, no reachable adjacency, no increasing flee move, and blocked orbit next step.
8. Three user-facing scenario fixtures: failure logs, maze seek/flee, and dual orbiters around the player.
9. Source-of-truth updates after implementation.

### Out of scope / follow-up

- General multi-turn route reservation or collision prediction between multiple actors.
- Searching ahead around an orbit ring when the deterministic next ring step is blocked.
- Variable or expression-based desired distances.
- Weighted terrain, movement costs greater than one, or stochastic path selection.
- Polished custom SadConsole controls beyond consuming shared editor-service facts.
- Player Action Choice typed prompts for `TargetPathMove` beyond existing authored-step fallback unless required by tests.

## Validation rules

- `TargetPathMove` requires exactly one target reference (`targetLabel`, `targetSlot`, or explicitly supported `targetSelf` if implementation review accepts self-targeting).
- `pathMode` is required.
- `desiredDistance` must be an integer `>= 0`.
- `desiredDistance` is required for `MaintainDistance` and `Orbit`.
- `orbitDirection` is required for `Orbit`.
- `orbitDirection` is rejected for non-`Orbit` modes.
- Prefer rejecting `desiredDistance` on `SeekAdjacency`/`FleeAdjacency` in the first slice unless content-editor signoff chooses permissive ignoring.
- Old one-off target movement steps should validate as compatibility steps but be omitted from new-step catalog choices.

## Six-step implementation plan with TDD trace

Implementation must not start until the current step has intentionally failing tests or explicitly records why no new test is needed. Each step below includes the invariant/test trace required by `docs/Source of Truth/testing-charter.md`.

### Step 1: Core descriptor, catalog shape, and compatibility preservation

Goal: introduce the new canonical step shape without changing existing behavior yet.

Planned work:

- Add Core descriptor fields/enums for `TargetPathMove`: `pathMode`, `desiredDistance`, and `orbitDirection`.
- Add Action Step catalog metadata and field contracts for the new step.
- Keep existing target movement step kinds executable as compatibility behavior during this step.

Affected invariants:

- `Canonical behavior descriptors and legacy action-plan descriptors preserve structured built-in inputs and materialize executable plans.`
- `The Action Step/primitive catalog describes every exposed primitive, value kind, implied state contract, and field contract.`
- `Canonical Action Steps must preserve their documented state contracts for Facing, Target, movement, target selection, inventory transfer, fallthrough, and deterministic tie-breaks.`

Existing tests to review/preserve:

- `ActionPlanDescriptorKeepsBuiltInInputsAsData`
- `ActionPlanDescriptorMaterializesExecutableBuiltIns`
- `BuiltInPlanPartsExposeStructuredInputs`
- `PlanPrimitiveCatalogExposesAllCheckEffectAndValueKinds`
- `ContentEditorListsCanonicalActionStepMetadata`
- Existing old-step tests: `SeekTargetAdjacentFallsThroughAndPreservesTarget`, `FleeTargetMovesAwayFromTargetAndPreservesTarget`, `MaintainChebyshevDistanceTwoBacksAwayWhenTooCloseAndPreservesTarget`, `StrafeClockwiseMovesPerpendicularToSeekPrimaryAndPreservesTarget`, `StrafeAnticlockwiseMovesOppositePerpendicularAndPreservesTarget`

New intentionally failing tests before production changes:

- `ActionStepCatalogExposesTargetPathMoveFieldContracts`
- `ActionPlanDescriptorPreservesTargetPathMoveInputsAsData`
- `ExistingTargetMovementStepsRemainRuntimeCompatible`

Exit criteria:

- New descriptor/catalog shape exists and old steps still pass existing compatibility tests.

### Step 2: Pathfinding distance field and Seek/Flee semantics

Goal: implement true pathfinding for `SeekAdjacency` and `FleeAdjacency`.

Planned work:

- Add a Core path/distance helper over existing topology/movement legality.
- Define target-adjacency sets and graph distance to adjacency.
- Implement `SeekAdjacency` shortest-path first-step selection.
- Implement `FleeAdjacency` legal adjacent step selection that increases graph distance.
- Preserve `Target`, update `Facing` only on successful movement, and emit structured trace details.

Affected invariants:

- `Plain adjacency means eight-way cardinal or intercardinal adjacency unless a contract explicitly says cardinal-only...`
- `Authored topology policy may add directed inventory-boundary adjacency... authoritative for movement...`
- `Canonical behavior chains continue after a failed/non-consuming step and stop after the first successful turn-consuming step.`
- `Canonical Action Steps must preserve their documented state contracts for Facing, Target, movement, target selection, inventory transfer, fallthrough, and deterministic tie-breaks.`

Existing tests to review/revise:

- `SeekTargetBlockedByIncidentalEntityPreservesGoalTarget`
- `FleeTargetMovesAwayFromTargetAndPreservesTarget`
- `FleeTargetFallsThroughWhenNoValidIncreasingMoveExists`
- `CanonicalMoveDiagonalAllowsOneBlockedCorner`
- `CanonicalMoveDiagonalRejectsTwoBlockedCorners`
- Directed topology movement tests listed under the directed topology policy invariant where applicable.

New intentionally failing tests before production changes:

- `TargetPathMoveSeekAdjacencyRoutesAroundMazeWithDiagonals`
- `TargetPathMoveSeekFallsThroughWhenAlreadyAdjacent`
- `TargetPathMoveFleeChoosesIncreasingPathDistanceFromAdjacency`
- `TargetPathMoveFleeFallsThroughWhenNoIncreasingMoveExists`
- `TargetPathMoveUsesDiagonalMovementAndCornerBlockingRules`
- `TargetPathMoveReportsDifferentPlaneAndUnreachableAdjacencyFailures`

Exit criteria:

- Seek/flee behavior is pathfinding-backed, deterministic, diagonal-capable, and traceable.

### Step 3: MaintainDistance and Orbit semantics

Goal: implement configurable desired distance and deterministic ring orbiting.

Planned work:

- Implement `MaintainDistance` using seek/flee correction relative to `desiredDistance`.
- Implement `Orbit` off-ring correction and on-ring deterministic next-step selection.
- Ensure blocked next orbit step falls through without searching ahead around the ring.
- Decide and document how compatibility `StrafeClockwise`/`StrafeAnticlockwise` map to a desired distance when no distance was authored historically.

Affected invariants:

- `Canonical behavior chains continue after a failed/non-consuming step and stop after the first successful turn-consuming step.`
- `Canonical behavior-chain traces must report attempted steps, state reads/writes, fallback continuation/stopping, and terminal turn outcome.`
- `Canonical Action Steps must preserve their documented state contracts for Facing, Target, movement, target selection, inventory transfer, fallthrough, and deterministic tie-breaks.`

Existing tests to review/revise:

- `MaintainChebyshevDistanceTwoBacksAwayWhenTooCloseAndPreservesTarget`
- `MaintainChebyshevDistanceTwoFallsThroughAtExactDistance`
- `StrafeClockwiseMovesPerpendicularToSeekPrimaryAndPreservesTarget`
- `StrafeAnticlockwiseMovesOppositePerpendicularAndPreservesTarget`
- `StrafeClockwiseUsesSeekTargetPrimaryTieBreakOnDiagonal`

New intentionally failing tests before production changes:

- `TargetPathMoveMaintainDistanceSeeksWhenTooFar`
- `TargetPathMoveMaintainDistanceFleesWhenTooClose`
- `TargetPathMoveMaintainDistanceFallsThroughAtDesiredDistance`
- `TargetPathMoveOrbitClockwiseFollowsDeterministicRing`
- `TargetPathMoveOrbitAnticlockwiseFollowsDeterministicRing`
- `TargetPathMoveOrbitFollowsOctagonalDistanceBandsAroundCorners`
- `TargetPathMoveOrbitCorrectsToDesiredDistanceBeforeOrbiting`
- `TargetPathMoveOrbitFallsThroughWhenNextRingStepIsBlocked`

Exit criteria:

- Maintain/orbit are configurable, deterministic, and match the agreed blocked/off-ring policies.

### Step 4: Content/YAML validation, editor service, agent API, and frontend editor parity

Goal: make `TargetPathMove` authorable and inspectable through canonical Content/editor workflows.

Planned work:

- Add YAML load/save/round-trip support for `pathMode`, `desiredDistance`, and `orbitDirection`.
- Harden validation for required/forbidden combinations.
- Add editor-service and agent API mutation support for path mode, desired distance, and orbit direction.
- Update previews and catalog summaries.
- Hide old one-off target movement steps from new canonical authoring choices while preserving load/display compatibility.
- Add SadConsole editor support sufficient to view/edit the new canonical fields through shared services.

Affected invariants:

- `YAML content loads from strings and files into registries that can be validated.`
- `Editable content documents round-trip through materialization and saved YAML.`
- `Content editor operations preserve declared IDs, presentations, carried layouts, Action Plans/behavior assignments, legacy action plans, and validation results.`
- `Frontend editor snapshots and service-backed template/action-plan mutations expose... stable engine-defined action-step choices... through shared content/editor services rather than Avalonia view models or ad-hoc YAML mutation.`

Existing tests to review/revise:

- `YamlContentLoaderLoadsCanonicalBehaviorChain`
- `EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml`
- `ContentEditorServiceAddsReordersAndRemovesActionPlanSteps`
- `ContentEditorDefaultsRequiredMoveAndTransferOptionsWhenAuthoringBehaviorSteps`
- `ActionPlanStepMutationsDefaultRequiredMoveAndTransferOptions`
- `ContentEditorListsCanonicalActionStepMetadata`
- `AgentContentEditorApiAuthorsCanonicalEnterExitBehavior`
- `ContentToolDispatcherCreatesBehaviorPlanAndPreviewWithValidationSummary`

New intentionally failing tests before production changes:

- `YamlContentLoaderLoadsTargetPathMoveBehavior`
- `EditableContentDocumentRoundTripsTargetPathMoveFields`
- `PrototypeRegistryValidationReportsInvalidTargetPathMoveFields`
- `ContentEditorServiceAuthorsTargetPathMoveFields`
- `AgentContentEditorApiAuthorsTargetPathMoveBehavior`
- `ActionPlanPreviewSummarizesTargetPathMoveFields`
- `CanonicalActionStepCatalogHidesLegacyTargetMovementStepsFromNewAuthoring`

Exit criteria:

- Authors and agents can create valid `TargetPathMove` plans without hand-writing unsupported fields; invalid combinations receive actionable diagnostics.

### Step 5: User-facing scenario content and logs

Goal: provide demonstration scenarios that exercise success and failure paths through shared scenario tooling.

Planned work:

- Add a failure-log scenario that intentionally triggers distinct `TargetPathMove` failure/fallthrough reasons.
- Add a maze scenario where seeking and fleeing actors navigate successfully around obstacles using true pathfinding.
- Add an orbit scenario with two orbiters around the player, in opposite directions and at different desired distances.
- Ensure scenario reports/log projections expose structured facts without parsing formatted trace text.

Affected invariants:

- `Built-in content must load and validate, but tests should not pin valid design choices such as balance values, glyphs, positions, or action plan behavior.`
- `Scenario runs use shared Content/Core services and schedule contained actors deterministically for scenario-root inventory spaces.`
- `Scenario reports treat expected in-simulation inability to act as runtime observation, not as an engine/runtime failure.`
- `Persisted scenario player narrative log reports expose a compact player narrative projection from shared scenario history/action-step outcome projection... and must not derive rows by parsing formatted trace lines.`

Existing tests to review/revise:

- `PrototypeRegistryValidationPassesForBuiltInContent`
- `ScenarioRunServiceShowsBehaviorStepsAndTreatsNoActionAsObservation`
- `ScenarioRunServiceReportsMultiTurnMoveFacingScenario`
- `AgentContentEditorApiRunsPersistedScenarioById`
- `AgentContentEditorApiRunsPersistedScenarioPlayerNarrativeLogById`

New intentionally failing tests before production changes:

- `TargetPathMovementFailureScenarioEmitsDistinctStructuredLogs`
- `TargetPathMovementMazeScenarioDemonstratesSeekAndFleePathfinding`
- `TargetPathMovementOrbitScenarioDemonstratesOppositeDirectionOrbiters`

Exit criteria:

- The three scenarios validate, run through shared scenario tooling, and demonstrate the intended user-facing behavior without pinning unnecessary aesthetic/balance details.

### Step 6: Documentation and source-of-truth updates

Goal: promote the implemented behavior from sprint plan to maintained source-of-truth documentation.

Planned work:

- Update `docs/Source of Truth/Engine-Editor-Capabilities.md` with the new stable/advanced support status and hidden-legacy-step authoring policy.
- Update `docs/Source of Truth/invariants.md` with final behavior contracts and test traces.
- Update `docs/Source of Truth/Content-Authoring-Manual.md` with author-facing YAML examples and distance/orbit wording.
- Update `docs/Source of Truth/Action-Step-Outcome-And-Affordance-Logic.md` and `Frontend-Game-Text.md` if final structured logs add or rename outcome/failure IDs.
- Move or summarize this plan when the sprint completes.

Affected invariants:

- Documentation step only; trace updates are required because runtime/content behavior changed.

Existing tests to review/preserve:

- All targeted tests added or revised in Steps 1-5.

New intentionally failing tests before production changes:

- None for documentation-only edits, unless doc examples are covered by existing content fixture tests.

Exit criteria:

- Capability, invariant/test trace, and content-authoring docs match implemented behavior.

## Content-editor signoff checklist

Before implementation begins, content-editor should explicitly accept or revise:

- the step name `TargetPathMove`;
- field names `pathMode`, `desiredDistance`, and `orbitDirection`;
- enum values `SeekAdjacency`, `FleeAdjacency`, `MaintainDistance`, `Orbit`, `Clockwise`, and `Anticlockwise`;
- whether `targetSelf` is supported or rejected;
- whether `desiredDistance` on seek/flee is rejected or ignored;
- author-facing wording for distance-to-adjacency;
- compatibility/default-distance policy for old `StrafeClockwise` and `StrafeAnticlockwise` steps.

## Sprint friction log

### Step 1: hidden legacy target movement versus existing service callers

Friction: Marking `SeekTarget`, `FleeTarget`, `MaintainChebyshevDistanceTwo`, `StrafeClockwise`, and `StrafeAnticlockwise` as non-stable catalog entries hides them from new-step picker lists, but existing editor/frontend tests and compatibility callers still use service-level insertion/replacement APIs with those kinds.

Mitigation: For the Step 1 catalog slice, hide the old steps from listed stable choices by tier while preserving service-level compatibility insertion/replacement. Step 4 should revisit this split and decide whether shared editor APIs should reject old target movement kinds outright after migration/presets exist, or keep accepting them as explicit compatibility authoring while omitting them from normal picker choices.

### Step 2: compact trace projection omitted new primitive details

Friction: The new `Primitive TargetPathMove` trace initially executed successfully but compact behavior-chain summaries omitted movement/failure detail because `ActionStepAttemptProjection` only projected known primitive labels.

Mitigation: Add `Primitive TargetPathMove` to the projection allow-list in Step 2. Future new Action Steps should include compact attempt projection updates alongside runtime handlers so scenario reports and frontend/debug logs expose structured result details immediately.

### Step 5: authored off-plane target setup is not yet straightforward through editor services

Friction: The planned failure-log vignette wanted a target in a different plane/container. Current scenario authoring can create carried nested targets, but a stable service-level way to pre-bind a behavior step's labeled target to that nested runtime entity was not available in the Step 5 slice, and template-level `ActionStateDefaults.Target` did not produce the intended persisted-scenario target binding for the nested entity.

Mitigation: The Step 5 test scenario keeps distinct target-read failures via missing target label and missing default target slot, and still covers unreachable adjacency, no increasing flee step, blocked orbit step, and already-adjacent seek fallthrough. A future scenario-authoring polish task should add or document a service-backed way to pre-bind labeled targets to nested placed entities if authored off-plane target-path failure demos remain desired.
