# Runtime Behavior Override Findings

Status: Active spike findings.

Read when:

- evaluating whether runtime behavior overrides should be promoted beyond spike status;
- deciding the next gameplay-facing model for changing entity behavior during play;
- separating the proven gameplay value from incidental implementation details in the first spike.

Related plan:

- `docs/Plans/Runtime-Behavior-Override-Spike.md`

Reference implementation branch:

- `action-plan-diegenesis`

The branch contains a working throwaway/reference spike, not a recommended production design. Use it to inspect the minimum cross-layer touchpoints needed to prove the concept, then rebuild a cleaner promoted version from the findings when selected.

## Proven gameplay value

The spike proves the core gameplay value:

> An entity can be given a different action plan during gameplay.

Manual proof:

- A player picked up a slime.
- The player applied the slime's action plan to a previously inert object through the GiveOverwrite flow.
- The previously inert object began acting according to the slime-provided behavior.

This validates the broad design direction that behavior can be changed at runtime as game state, without mutating authored content definitions.

## Architectural touchpoints from the reference spike

The reference implementation was small enough to prove the concept, but broad enough to identify the layers a promoted version must deliberately own.

Key touchpoints:

- **Runtime world state:** `WorldState` gained runtime behavior-provider assignment state, plus clone/restore support so history snapshots can preserve the override relation.
- **Turn scheduling:** `TurnService` needed to distinguish an actor's own authored plan from its effective runtime behavior source, and to skip provider entities while they are assigned as providers.
- **Action resolution context:** provider plans resolved with the target actor as the actor/body/action-state owner. This preserved actor-owned `Facing`, `Target`, location, inventory, bulk/aperture, and turn consumption.
- **Local turn-order reporting:** `LocalTurnOrderReport` needed effective-actor awareness so an overridden inert object appears actor-capable, while the provider entity does not appear as separately scheduled.
- **Frontend-neutral entity panels:** `EntityPanelProjectionService` needed to expose the active behavior source in `ActionPlanSummary`, so inspection could show when an entity is using a runtime provider instead of only its own plan.
- **Controlled command bridge:** `ControlledActorCommandService` gained spike/debug commands `GiveOverwrite` and `TakeOverwrite`, proving that runtime behavior mutation should flow through shared Core command services rather than frontend-local mutation.
- **Structured log projection:** `ActionOutcomeProjection` needed command-kind/sentence/anchor handling for overwrite commands so logs and local panels could remain structured rather than parsed strings.
- **SadConsole prompt UX:** SadConsole could wire the feature through debug prompt modes (`G` GiveOverwrite, `T` TakeOverwrite) over shared Core commands while keeping simulation legality out of frontend code.
- **Docs/tests:** `invariants.md`, `Engine-Editor-Capabilities.md`, Core tests, projection tests, and SadConsole build/tests all needed updates, confirming this is a cross-layer capability rather than a narrow UI feature.

Promoted work should probably replace the ad hoc checks with a shared Core-owned effective behavior query/service, for example:

```text
EffectiveBehaviorService
  IsScheduledActor(entity)
  GetEffectivePlan(actor)
  GetBehaviorSources(actor)
  ComposeEffectivePlan(actor)
  DescribeBehaviorForProjection(actor)
```

This service would prevent `TurnService`, local reports, projections, and future frontends from each rediscovering behavior-source rules independently.

## Incidental implementation details not required by the feature

The first implementation proves the concept, but several details should not be treated as required for the eventual gameplay feature.

### The action plan does not have to come from an existing entity instance

The spike uses an existing carried entity as the behavior provider because it was a small vertical slice over existing entities, inventory, inspection, and action plans.

Future models may provide action-plan changes from other runtime sources, such as:

- abstract behavior tokens;
- equipped behavior modules;
- status effects;
- scenario/debug actions;
- authored plan references materialized into runtime state;
- generated behavior bundles.

The key feature is runtime behavior replacement/composition, not the provider being a normal entity instance.

### The action plan does not have to replace the old plan completely

The spike uses a complete override:

```text
effective plan = provider plan
```

A preferred future model may be compositional, especially prepend/fallback composition:

```text
effective plan = override/prepended plan -> original/default plan fallback
```

This would let the added behavior try first while preserving the actor's original behavior when the added behavior cannot act. That shape likely better matches authored fallback-chain semantics and avoids making temporary behavior changes feel like total identity replacement.

Open design question:

- Should prepended behavior use normal canonical fallthrough semantics, or should it be represented as two composed behavior chains with a clear boundary in traces/logs?

### The action-plan change does not have to be limited to player action

The spike exposes GiveOverwrite/TakeOverwrite through player-controlled debug UX because that was the fastest way to prove the gameplay loop.

The underlying feature should not be assumed to be player-only. Future non-player or system-driven behavior changes may include:

- autonomous actors applying behavior changes to other entities;
- traps/status effects that install temporary behavior;
- environmental machines that apply behavior modules;
- scenario/debug commands;
- reaction systems, once promoted.

The general capability is runtime behavior mutation. Player GiveOverwrite/TakeOverwrite is only one initiating interaction.

## Current spike simplification to revisit

The provider entity currently remains physically located in inventory while also being assigned as the target actor's behavior provider. This is acceptable for the spike because the behavior-provider relation represents an abstract overwrite slot, but a promoted model should decide whether behavior sources are:

- physically equipped into a slot;
- referenced while remaining in inventory;
- copied/instanced as runtime behavior state;
- consumed/transformed by the assignment;
- stackable/composable with other behavior sources.

## Recommended next design direction

For the next iteration, prefer terminology and implementation that distinguish the proven feature from the first debug UX:

- **Runtime behavior modifier** rather than only `BehaviorProvider`.
- **Prepend behavior** or **behavior overlay** rather than only overwrite.
- **Effective plan composition** rather than only plan replacement.

Suggested next experiment:

> Change the effective behavior model from full replacement to prepend composition, so an applied behavior tries first and the actor's default plan remains fallback when the applied behavior fails or does not consume the turn.
