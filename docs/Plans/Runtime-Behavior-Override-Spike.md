# Runtime Behavior Override Spike

Status: Active spike plan.

Read when:

- exploring runtime-only action-plan override behavior;
- deciding whether entity-as-behavior-provider should be promoted from spike to supported capability;
- assessing the targeting-override stretch goal for runtime template-identity targeting.

Related source of truth:

- `docs/Source of Truth/invariants.md` records stable Core behavior contracts and test traces.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` records implemented support tiers if this spike is promoted.
- `docs/Source of Truth/Content-Authoring-Manual.md` records content-facing authoring support; this spike should not add normal content-authoring support unless explicitly promoted later.
- `docs/Plans/High-Level-Roadmap.md` records promotion/defer decisions.
- `docs/Plans/Runtime-Behavior-Override-Findings.md` records conclusions from the spike and separates proven gameplay value from incidental first-implementation details.

## Spike decision

This spike has pivoted away from granular diegetic Action Step entities and granular target-template objects.

The spike will explore a narrower entity-provider model:

1. **Action-plan override:** one runtime entity may be assigned/equipped as another entity's behavior provider. The actor executes the provider entity's normal authored/default action plan using the actor's own body, location, inventory, action state, and target-selection context.
2. **Targeting override stretch goal:** one runtime entity may be assigned/equipped as another entity's target-template provider. The provider's source template identity temporarily overrides the actor's normal target template selection for matching target rules.

The `Use` action idea is deferred to backlog because it requires separate decisions about actor/source attribution, target legality, chosen Action Step semantics, and turn consumption.

## Non-goals

- Do not make Action Steps into movable item entities in this spike.
- Do not make target templates into separate `Idea` entities in this spike.
- Do not mutate YAML, content templates, authored action plans, or editor documents.
- Do not add normal content-authoring/editor support for overrides yet.
- Do not design or implement the deferred `Use carried entity` action during this spike.
- Do not support arbitrary provider stacking/chaining unless a trivial implementation naturally falls out of the first slice.

## Working model

Definitions:

- **Actor:** the entity whose turn is being resolved. The actor supplies body/location/inventory/action-state facts and consumes the turn.
- **Behavior provider:** an optional runtime entity whose default authored action plan supplies the ordered behavior chain for the actor's turn.
- **Target-template provider:** an optional runtime entity whose source template identity supplies the target template filter for the actor's target selection.

Initial policy:

- If an actor has a behavior provider, the actor resolves the provider's default action plan instead of the actor's own default action plan.
- The actor's own target rules, target slots, `Facing`, inventory, bulk/aperture, and location remain actor-owned unless a later stretch goal explicitly overrides targeting.
- A behavior provider is not independently scheduled while it is assigned/equipped as a provider.
- Removing the behavior provider restores the actor's own default action plan on later turns.
- Overrides are runtime simulation state and must be restorable by history/rollback if the spike reaches shared history integration.

## TDD plan

Planned Core semantic changes must start with intentionally failing tests before production code changes.

Affected invariants from `docs/Source of Truth/invariants.md`:

- `An entity is an actor only if it has a decidable Action Plan/fallback chain or decision trigger.`
- `Canonical behavior chains execute ordered Action Steps in one Action Plan without requiring linked fallback plans.`
- `Shared actor plan resolution must preserve canonical trace wrapping, consuming-step stop behavior, terminal non-consuming stop behavior, and all-continue terminal failure behavior across turn service, headless run, and recording consumers.`
- `Entity action state such as Facing and Target is typed and persists on the actor entity across plan executions.`
- `Simulation history snapshots preserve restorable world state...` if override state is included in the spike implementation.

Existing tests to review before implementation:

- `TurnServiceOnlySchedulesEntitiesWithActionPlans`
- `InterpretedEntityActionPlanCanBeScheduledByTurnService`
- `BehaviorChainRunsMoveFacingThenPickupTargetWithoutLinkedFallbackPlan`
- `BehaviorChainStopsAfterFirstSuccessfulActionStep`
- `ResolvePlanReportsConsumingSuccessWithCanonicalTraceShape`
- `ResolvePlanContinuesAfterFallthroughAndStopsAtTerminalFailure`
- `WorldStateClonePreservesMutableSimulationStateWithoutSharingCollections`
- `RollbackRestoresFrameSnapshotAndVisibleTraceContext`

Candidate new failing tests:

1. `ActorUsesBehaviorProviderDefaultPlanWhenOverrideIsAssigned`
   - Given an actor with its own plan and a provider entity with a different plan, resolving the actor turn attempts the provider plan steps with the actor as the actor.
2. `BehaviorProviderIsNotScheduledIndependentlyWhileAssigned`
   - Given a provider entity that would otherwise be schedulable, turn ordering excludes it while it is assigned as another entity's behavior provider.
3. `RemovingBehaviorProviderRestoresActorDefaultPlan`
   - Given an actor override is removed, later actor turns resolve the actor's own authored plan again.
4. `BehaviorOverridePreservesActorTargetingAndActionState`
   - Given actor-owned targeting/facing state and provider-owned behavior, the provider plan reads/writes the actor's state, not the provider's state.
5. Stretch: `TargetTemplateProviderOverridesTargetingRuleTemplateIdentity`
   - Given an actor with a runtime target-template provider, target refresh matches the provider entity's source template identity instead of the actor's authored target template for the selected rule/slot.
6. If history is touched: `RollbackRestoresRuntimeBehaviorOverrideAssignment`.

## Implementation checkpoints

### Checkpoint 1: Runtime behavior override semantics

Goal: prove actor turn resolution can select a behavior provider's action plan while preserving actor-owned execution context.

Stop/record prohibitively complex if this requires a broad rewrite of action-plan descriptors, canonical behavior-chain resolution, or turn scheduling.

### Checkpoint 2: In-game visibility

Goal: entity inspection/panel projection can show:

- actor's own default plan summary;
- active behavior source: own plan or provider entity;
- provider plan summary when an override is active.

Frontend display can remain minimal/textual during the spike.

Status: Complete for the frontend-neutral projection/SadConsole text path. `EntityPanelProjection.ActionPlanSummary` now reports own plan plus active behavior-provider source, and local contents treat an override actor as actor-capable while treating an assigned provider as inert for turn-order display.

### Checkpoint 3: In-game assignment/removal

Goal: a runtime command or existing interaction path can assign/remove a behavior provider in-game.

Preferred shape: present the operation through an existing verb vocabulary such as equip/take/give if already available without distorting Core semantics. If existing verbs are too inventory-specific, use a clearly marked spike/debug command and record the remaining design gap.

Status: Complete for a first debug UX. SadConsole exposes `G` GiveOverwrite and `T` TakeOverwrite prompt flows. GiveOverwrite selects a carried provider and an adjacent target actor, then assigns the provider as that actor's behavior provider. TakeOverwrite selects an adjacent overridden actor and clears its provider, returning/confirming the provider in the player inventory destination. Current spike simplification: the provider's ordinary entity location remains in inventory while the runtime behavior-provider relation represents the abstract overwrite slot.

### Stretch checkpoint: Target-template override

Goal: a runtime target-template provider can override target matching by using the provider entity's source template identity.

Stop/record follow-up if runtime entity-to-template provenance is not available enough for deterministic targeting.

## Completion criteria

The spike is complete if either:

1. Working in-game action-plan override editing exists: action-plan override state is visible in-game, can be changed at runtime, changes actor behavior on later turns, and remains simulation-only; or
2. The spike records that action-plan override editing is prohibitively complex, with the blocking subsystem and next architectural prerequisite identified.

Targeting override is a stretch goal and is not required for spike completion.
