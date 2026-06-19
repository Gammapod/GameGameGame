# Sprint 11: Alpha Scenario Materialization

Status: Completed / archived during Sprint 11 wrap-up.

Read when:

- reviewing the first alpha scenario package/materialization implementation slice;
- deciding how scenario definitions, player insertion, materialization, and agent/editor validation fit together;
- selecting follow-up beta roadmap work after the alpha MVP path.

Related:

- `docs/Source of Truth/Engine-Editor-Capabilities.md`
- `docs/Plans/High-Level-Roadmap.md`
- `docs/Source of Truth/vertical-slice-map.md`
- `docs/Archived/Sprint-10-Scenario-Feedback-Loop.md`

## Goal

Pull in one week of completable alpha-roadmap work: define the smallest explicit alpha scenario definition and promote scenario setup/player insertion into a reusable materialization path that editor/agent APIs can validate and exercise. This sprint should move toward arbitrary authored scenario launch without touching Console code or checked-in game content yet.

## One-week sprint plan

Sprint priority: implement the smallest vertical slice of the alpha scenario package/materialization path. Scenario report polish is included only where it directly proves materialization/player insertion behavior. Broad new mechanics, movement primitives, scheduler work, and frontend work remain deferred.

### Selected scope

1. **Minimal alpha scenario definition model**
   - Add a typed scenario definition/request shape that references normal content templates instead of relying on hardcoded prototype IDs or magic names.
   - Required first-slice fields: scenario identity/name, scenario-root entity template ID, player entity template ID, runtime player entity ID, and player start coordinate in the scenario-root inventory/play plane.
   - Keep the model compatible with Sprint 10's scenario-root inventory space and avoid inventing a separate gameplay language.
   - Use inline/test-authored documents or in-memory editor sessions only; do not add checked-in content fixtures this sprint.

2. **Reusable scenario materialization service**
   - Extract/promote spawn/setup logic out of `AgentContentEditorApi.RunScenario` into a reusable editor/Core-adjacent service that can materialize a scenario definition into runtime objects.
   - Materialization result should include `WorldState`, runtime action-plan map, presentation/registry lookup as needed by callers, scenario root entity ID, player entity ID, active scenario plane/container, actor order/scheduling inputs, and structured diagnostics.
   - Keep the existing `RunScenario` path working while moving it onto the shared materialization path where practical.

3. **Player insertion contract**
   - Define and test how a selected player template becomes a selected runtime player entity ID and is inserted into the scenario-root inventory/play plane.
   - Validate missing scenario root, missing/invalid player template, invalid start coordinates, and occupied start cells with actionable diagnostics.
   - Preserve the direct-player-input assumption for alpha; do not require player-controlled Action Steps, AI/default-plan behavior, scheduler/speed changes, or future action-choice discovery.

4. **Editor/agent validation and preview surface**
   - Add editor/agent operations or request variants to inspect/validate/materialize/run the alpha scenario definition.
   - Return categorized diagnostics that distinguish authoring/validation errors, unsupported capability gaps, expected runtime observations, and runtime engine failures.
   - Add only lightweight text-report additions needed to show scenario identity, player insertion, active plane, and relevant diagnostics.

### Defer this sprint

- Console arbitrary scenario launch or changes under `src/GameGameGame.Console`.
- Checked-in prototype/content fixture changes under `src/GameGameGame.Content`.
- New Action Steps or mechanics such as `ReverseFacing`, `TurnLeft`, `TurnRight`, `Backstep`, `SeekTarget`, `Give`, `Take`, or template-selecting `CreateFacing(templateId)`.
- Scheduler/speed/multiple actions per turn, reactions, saved runlogs, golden runlog tests, or integrated frontend work.
- Broad Avalonia GUI parity for scenario authoring.

## Testable outcomes

- A typed alpha scenario definition can be created in tests against an editor/content session and validated without hardcoded prototype IDs.
- Materialization inserts the scenario root and player into the scenario-root inventory/play plane, returns the selected runtime player entity ID and active scenario plane, and exposes action plans/presentations needed by callers.
- Missing root template, missing player template, invalid player start, and occupied player start produce structured authoring/validation diagnostics and prevent misleading simulation.
- Existing scenario-root inventory runs still pass through the agent API after the setup logic is shared.
- A scenario report or validation result includes enough concise text/structured data for an agent to confirm which scenario was materialized, where the player was inserted, and why materialization failed when it fails.

## TDD readiness / invariant trace

Affected invariants:

- Entity locations are represented by occupancy of nodes in planes.
- At most one entity may occupy a node at a time.
- Entity action state such as `Facing` and `Target` is typed and persists on the actor entity across plan executions.
- Content editor operations preserve declared IDs, presentations, carried layouts, Action Plans/behavior assignments, legacy action plans, and validation results.

Existing coverage to trace before implementation:

- Occupancy/location: `EntityLocationsAreRepresentedByNodeOccupancy`, `MovementCannotPlaceEntityOnOccupiedNode`, `PrototypeRegistryValidationReportsOverlappingCarriedEntities`.
- Typed action state/defaults: `CanonicalFacingPersistsOnActorActionStateAcrossPlanExecutions`, `CanonicalTargetPersistsOnActorActionStateWhenBlockingEntityIsFound`, `SpawnedActionPlanUsesCanonicalInitialFacingDefault`.
- Editor/API scenario and validation coverage: `AgentContentEditorApiRunsScenarioRootInventoryActorsInInitiativeOrder`, existing `AgentContentEditorApiTests`, `ContentEditorServiceValidatesCurrentDocumentAfterEdits`, `ContentEditorServiceValidationReportsCurrentDocumentErrors`.
- Scenario report baseline: `ScenarioRunReportTests` remain historical/test-helper coverage until replaced or clearly superseded.

First intentionally failing tests:

- Add materialization tests for a valid alpha scenario definition that inserts a player at the requested start and returns root/player/plane IDs.
- Add validation tests for missing scenario root, missing player template, invalid start coordinate, and occupied start coordinate.
- Revise or add an `AgentContentEditorApi` test so `RunScenario` or a new scenario-definition command uses the shared materialization result and includes player insertion in structured/report output.

## Suggested week split

- **Day 1:** Confirm scenario definition and materialization-result shapes; write failing tests for valid materialization and validation failures.
- **Days 2-3:** Extract/promote the shared materialization service and preserve existing `RunScenario` behavior.
- **Day 4:** Implement player insertion diagnostics and agent/editor validation/report surface.
- **Day 5:** Run targeted and relevant broader tests, update `Engine-Editor-Capabilities.md` for actual support status, update roadmap/handoff notes, and identify the next alpha slice.

## Handoff notes for future sessions

Update this section during or at the end of Sprint 11.

Record:

- central implementation files and tests;
- verification commands used;
- scenario definition fields that proved stable or changed;
- materialization/player insertion gaps encountered;
- whether Console scenario launch is unblocked for a later sprint;
- any roadmap changes or explicitly deferred follow-ups.

Current handoff state:

- Initial TDD slice added `AgentContentEditorApi.MaterializeScenario(AgentAlphaScenarioDefinition)` and a shared `AlphaScenarioMaterializer` in the Editor project.
- The first alpha scenario definition shape is scenario ID/name, scenario-root template ID, player template ID, deterministic runtime player entity ID, and player start coordinate.
- Materialization currently creates a scenario host plane, spawns the root as `scenarioRoot`, uses the root inventory as the active `scenarioRoot` play plane, inserts the player at the requested start, and returns setup lines plus structured validation/runtime/capability-gap channels.
- Validation coverage exists for missing root template, missing player template, invalid player start, and occupied player start. Existing `RunScenario` now uses the shared root-only materialization path while preserving Sprint 10 scenario-root inventory behavior.
- Follow-up slice added persisted scenario definitions to editable content documents under `scenarios`, editor-service/agent `UpsertScenario`, agent materialization by scenario ID, and canonical validation for persisted missing root/player references, unusable roots, invalid starts, occupied starts, and player-ID conflicts.
- Console restriction was lifted. Follow-up slice added `ConsoleScenarioLauncher`, a content-facing `ScenarioMaterializer`, and Console startup support for either the built-in prototype or `content-file scenario-id` launch. Console play, pickup, drop, inspect, and rendering flows now use the selected player entity ID rather than `PrototypeContent.PlayerId`.
- Final alpha smoke slice added embedded `AlphaScenarioContent.yaml`, `AlphaScenarioContent.LoadDocument()`, fixture validation/materialization coverage, and Console launch/player-move smoke coverage. The current alpha target is now represented end-to-end in tests: persisted scenario definition -> materialization -> player insertion -> Console-launchable session -> player action.
