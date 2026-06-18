# Behavior Model Consolidation Plan

Status: Planning / next architectural direction.

The current engine supports legacy low-level action plans and the first primitive-backed fallback-plan foundation. This document records the safer next direction: build a new canonical behavior system beside the legacy action-plan system rather than trying to force the old low-level `steps/checks/effects` editor model into the new fallback-chain model.

## Terminology

- **Fallback Chain**: the general notion of a cascading chain of actions that happens during a turn. This can refer to one entity's configured behavior or to the runtime evaluation chain, including future chains that cross entity boundaries.
- **Action Plan**: one entity's pre-set fallback chain.
- **Action Step**: one engine-defined primitive action. A step either has an observable effect or, if impossible/failed, allows evaluation to continue to the next fallback step.

Legacy note: existing `ActionPlanDescriptor.Steps` are low-level compatibility steps made from checks/effects. They are not the same as canonical Action Steps in the new model.

## Canonical direction

The canonical editor-facing model should be an entity behavior editor where an entity's assigned Action Plan is shown as an ordered list of Action Steps.

Semantics:

1. The first Action Step is attempted.
2. If it succeeds, the current root turn resolves.
3. If it fails or is impossible, the next Action Step in the list is attempted.
4. If the final Action Step fails or is impossible, the current root turn resolves terminally.

This makes fallback order implicit in list order rather than spread across linked top-level plan descriptors.

## Editor-facing model, first pass

Main editor areas are expected to remain broadly:

- Entity
- Inventory
- Action Plans

On the Entity tab:

- the selected entity's assigned Action Plan should be visible as a list of Action Steps;
- the editor should be able to add, remove, and rearrange Action Steps and save the result to that entity;
- selecting an Action Step to add may show a short hint/description;
- because steps imply required state, the editor should surface implied initial state such as `Facing` or `Target` and allow authors to set it;
- initial state should remain optional when the engine/editor can provide a safe default.

Action Steps themselves are engine-defined primitives. In the first canonical GUI pass, authors choose steps and order them; they do not edit arbitrary low-level checks/effects inside a step.

Stretch goals:

- apply an Action Plan template to an entity;
- save the current entity Action Plan as a reusable template;
- view/edit Action Plan templates from the Action Plans tab;
- show which entities currently use a template.

## Compatibility stance

Use Option A: keep the existing low-level action-plan model as legacy/advanced/internal compatibility while adding a new canonical behavior model beside it.

The new model should not require immediate removal of:

- low-level `ActionPlanDescriptor.Steps`;
- legacy variables;
- old check/effect materialization;
- current content loading compatibility.

The new model should eventually become the default editor-facing path.

## Reaction behavior stance

Future reaction behavior is still conceptualized, not planned.

The important future capability is that a successful interaction may trigger the target entity to run a reaction Action Plan during the root actor's turn. This does not require specifying `Bump` as the trigger yet. Avoid overfitting the near-term model around bump-specific behavior until reaction action plans and trigger semantics are better defined.

## Initial canonical Action Steps

The first useful canonical steps are expected to align with the primitive work already implemented:

- `MoveFacing`: reads persistent actor `Facing`; attempts movement; on blocked movement can set persistent actor `Target` to the blocker and fail into the next step.
- `PickupTarget`: reads persistent actor `Target`; attempts pickup; if impossible, fails into the next step.

Open design questions:

- Whether `MoveFacing` writing `Target` should be inherent to the step or a result field visible in metadata.
- Whether default state is materialized at spawn time or lazily supplied by step defaults.
- How much of an Action Plan should live directly on an entity versus as a reusable template reference.
- How templates are applied, detached, edited, and tracked across entities.

## Suggested implementation phases

## Invariant impact watchlist

The consolidation should preserve most existing invariants, but several should be re-tested or reworded as the new system replaces the linked primitive-plan bridge:

- **Actor scheduling**: entities should be schedulable from a decidable Action Plan/fallback chain, not only from legacy `IEntityActionPlan` instances.
- **Fallback/terminal turn behavior**: ordered Action Steps must preserve the invariant that explicit fallback continues evaluation while a final failed step terminates the root actor's turn.
- **Structured traces**: traces should report the Action Step attempted, whether it succeeded/failed, and why fallback continued or terminal resolution occurred.
- **Temporal recursion guard**: no immediate recursion is expected for a local ordered chain, but future template references or reaction chains must remain guarded.
- **Descriptor materialization**: new canonical behavior descriptors should materialize executable plans without relying on legacy low-level `steps/checks/effects`.
- **Primitive catalog contracts**: the Action Step catalog should become the canonical source for implied state such as `Facing` and `Target`, replacing editor-facing dependence on low-level check/effect metadata.
- **Content editor preservation**: editor operations must preserve entity-local Action Plans/behavior assignments as well as legacy action-plan data during the compatibility period.

### Phase C1: Descriptor and runtime chain model

- Add a canonical Action Plan / Fallback Chain descriptor that is an ordered list of engine-defined Action Steps.
- Interpret the list with fallback-by-order semantics.
- Keep legacy low-level descriptors loadable and executable.
- Add tests proving `MoveFacing -> PickupTarget` works from one ordered chain without linked fallback plan descriptors.

### Phase C2: Action Step catalog and metadata

- Add a machine-readable Action Step catalog as the canonical source for editor/API-selectable steps.
- Catalog entries should include at minimum: step kind, display name, short description/hint, required state, defaultable state, state writes, and whether the step is stable/canonical or advanced/legacy.
- Use the catalog for validation and editor/agent API discovery instead of requiring the editor to infer behavior from low-level checks/effects.
- Add tests proving the catalog describes every canonical Action Step and that validation/editor services consume the same metadata.

### Phase C3: Content validation and defaults

- Validate required state implied by Action Steps.
- Allow engine/editor default state where defined.
- Report actionable diagnostics for impossible chains or missing required state without defaults.

### Phase C4: Editor service and agent API support

- Add typed operations to set an entity's Action Plan list.
- Add operations to add/remove/reorder Action Steps.
- Expose Action Step metadata, including hints and implied state.

### Phase C5: GUI entity behavior editor

- Display the selected entity's Action Plan as an ordered list of Action Steps.
- Support add/remove/reorder.
- Surface implied initial state controls.
- Keep legacy low-level plan editing advanced or separate.

### Phase C6: Optional templates

- Add reusable Action Plan templates after the entity-local chain model is stable.
- Support apply/save-as-template workflows.
- Show template usage by entity.
