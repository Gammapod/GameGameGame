# Archived: Variable Abstraction Plan

> Archived after the canonical slot cleanup direction was accepted. Future action-plan work should use
> `Movement-Primitives-Parity-Plan.md` and `Engine-Editor-Capabilities.md` as the active planning documents.

## Goal

Action-plan variables should become engine-defined primitives rather than arbitrary editor-authored names. The editor should present action plans as simple primitive choices plus any necessary literal parameters. Primitives themselves should declare which canonical slots they read and write.

The intended model is:

- Internally, the engine may maintain a small fixed actor action-state context.
- Externally, the editor only exposes slots that are implied by selected primitives and assigned action plans.
- All canonical slots are persistent across turns.
- Arbitrary variable creation, variable-name wiring, and variable suggestion workflows should be removed from editor-facing action-plan authoring.
- After this variable abstraction is complete, action-plan editing parity can be finished on top of the simplified model.

## Canonical Slot Direction

Initial canonical slots should be minimal:

- `Facing`: `Direction`, persistent actor state used by directional primitives.
- `Target`: `EntityId`, persistent actor state written by targeting primitives and read by follow-up target primitives.

Additional slots should only be added when an engine primitive requires them. They should be engine-defined, typed, catalogued, and validated.

## Step 1: Document current variable usage and intended replacement

Inventory all current variable-name usage across Core, Content, and Editor.

Current arbitrary variable surfaces include:

- `ActionPlanContext.Variables` keyed by string.
- `PlanVariableRef<TValue>`.
- `PlanCheckDescriptor.DirectionVariable`.
- `PlanCheckDescriptor.TargetVariable`.
- `PlanEffectDescriptor.DirectionVariable`.
- `PlanEffectDescriptor.TargetVariable`.
- `PlanEffectDescriptor.VariableName`.
- `EntityTemplate.DefaultPlanVariables`.
- YAML `defaultPlanVariables`.
- editor default-variable forms and variable suggestions.

Define the target replacement for each current variable surface before removing it.

Expected direction:

- Direction reads use canonical `Facing`.
- Blocking/targeting writes canonical `Target`.
- Pickup reads canonical `Target`.
- Reverse-direction updates canonical `Facing`.
- `SetVariable` is removed from editor-authored action plans or replaced by narrower canonical-slot primitives if still needed.

### Testable outcomes

- A checked-in mapping exists from every current variable-bearing descriptor field to either a canonical slot or an explicit removal/deprecation decision.
- The mapping identifies which existing YAML fields are legacy compatibility fields.
- The mapping identifies all tests that currently depend on arbitrary variable names.

## Step 2: Add engine-defined canonical action slots

Introduce a Core representation for canonical action-plan slots.

Possible implementation shapes:

- `enum ActionPlanSlot { Facing, Target }`
- typed slot descriptors in a catalog
- an actor action-state/context object that stores values by slot rather than arbitrary string

The context may still use a dictionary internally, but public/Core primitive APIs should prefer typed canonical slots over arbitrary variable names.

All slot values are persistent across turns. A slot value written during one plan execution remains available to later turns and to other plans for the same actor state.

### Testable outcomes

- Core can store and retrieve `Facing` as a `Direction` slot.
- Core can store and retrieve `Target` as an `EntityId` slot.
- Slot reads fail with structured trace information when the slot is unset or has the wrong kind.
- Slot writes produce structured trace information.
- Slot values persist after one interpreted plan execution and are available to a later execution using the same actor state/context.

## Step 3: Convert Core built-in primitives to canonical slots

Update built-in checks and effects so their editor-facing behavior no longer requires arbitrary variable names.

Target primitive contracts:

- `CanMove` reads `Facing`.
- `Move` reads `Facing`.
- `BlockingEntity` reads `Facing` and writes `Target`.
- `CanPickup` reads `Target` and uses its inventory coordinate parameter if that remains a literal primitive field.
- `Pickup` reads `Target` and uses its inventory coordinate parameter if that remains a literal primitive field.
- `ReverseDirection` reads and writes `Facing`.
- `Wait` reads/writes no slots.
- `CallPlan` shares the same persistent actor state/context with the called plan.
- `SetVariable` is either deprecated from editor-authored plans or replaced with constrained canonical-slot effects.

This step may keep legacy constructors temporarily, but canonical constructors and descriptor materialization should become the preferred path.

### Testable outcomes

- A plan can move an actor using `Facing` without specifying a variable name in the check/effect descriptor.
- `BlockingEntity` writes `Target` without specifying a target variable name.
- A later `Pickup` primitive can read the `Target` written by `BlockingEntity` without variable-name wiring.
- `ReverseDirection` updates persistent `Facing` and the updated value is visible on a later turn.
- `CallPlan` sees and can update the same canonical slots as the caller.
- Existing arbitrary-variable runtime tests are either migrated to canonical-slot tests or explicitly retained as legacy-compatibility tests.

## Step 4: Update action-plan descriptors and primitive catalog

Change descriptors and catalog metadata so canonical slot usage is described by primitive definitions instead of editable field names.

Descriptor direction:

- Remove or deprecate `DirectionVariable`, `TargetVariable`, and `VariableName` from editor-facing descriptors.
- Keep literal fields that are still genuinely authored, such as inventory coordinates or called plan IDs.
- Make required primitive fields catalog-driven.
- Add catalog metadata for canonical slot reads and writes.

The catalog should become the source of truth for:

- available check kinds
- available effect kinds
- display names
- literal authored fields
- canonical slot reads
- canonical slot writes
- default descriptor construction

### Testable outcomes

- `PlanPrimitiveCatalog` reports `Facing` reads for `CanMove`, `Move`, and `ReverseDirection`.
- `PlanPrimitiveCatalog` reports `Target` write for `BlockingEntity`.
- `PlanPrimitiveCatalog` reports `Target` reads for `CanPickup` and `Pickup`.
- Default descriptors can be created from primitive kinds without arbitrary variable names.
- Descriptor materialization for current primitives does not require arbitrary variable names.
- Catalog completeness tests fail if a new primitive lacks slot/read/write metadata.

## Step 5: Add legacy YAML compatibility and canonical save behavior

Existing content may contain variable-name fields and `defaultPlanVariables`. During transition, the loader should be able to read existing content and map known legacy variables to canonical slots where safe.

Suggested compatibility behavior:

- Legacy `directionVariable: facing` maps to canonical `Facing`.
- Legacy `targetVariable: target` maps to canonical `Target`.
- Legacy default variable `facing` maps to default canonical `Facing`.
- Legacy default variable `target` may map to default canonical `Target` only if a persistent default target is intentionally supported.
- Unknown arbitrary variable names should produce validation diagnostics in strict/canonical mode.
- Saving through the editor should emit canonical YAML, not legacy arbitrary variable fields.

### Testable outcomes

- Current prototype content loads successfully through the compatibility path.
- Current prototype content can be saved/reloaded in canonical form.
- Legacy `facing` defaults become canonical facing defaults.
- Legacy primitive variable-name fields do not appear in newly saved canonical YAML.
- Unknown legacy variable names produce clear diagnostics in strict/canonical validation mode.

## Step 6: Replace template default variables with canonical actor state defaults

Replace editor-facing `DefaultPlanVariables` with a constrained actor-state default representation.

Possible shape:

```csharp
public sealed record ActorActionStateDefaults(
    Direction? Facing = null,
    EntityId? Target = null);
```

Only expose defaults that are meaningful for persistent canonical slots. In the editor, this should be presented as simple fields implied by assigned action plans, such as initial facing direction.

### Testable outcomes

- Entity templates can declare an initial `Facing` value without using arbitrary variable names.
- Spawning an entity with an assigned action plan initializes persistent actor state from canonical defaults.
- If a plan uses `Facing` and no default is provided, validation either supplies a documented default or reports a structured diagnostic.
- Default variable editor tests are replaced by canonical actor-state default tests.
- Save/reload preserves canonical actor-state defaults.

## Step 7: Update content validation for canonical slots

Validation should infer required canonical slots from assigned action plans and primitive catalog metadata.

Validation responsibilities:

- identify which persistent slots are read by an assigned plan and its called plans
- identify which slots are written before later reads in plan order
- determine whether missing initial slot values are acceptable or diagnostic-worthy
- report diagnostics using slot names and expected kinds, not arbitrary variable names
- reject arbitrary variable fields in strict/canonical mode

### Testable outcomes

- A template assigned a plan that reads `Facing` reports a missing-facing diagnostic if no default or documented fallback exists.
- A plan where `BlockingEntity` writes `Target` before `Pickup` reads it validates without requiring an entity-template default target.
- A plan that reads `Target` before any primitive writes it reports a missing-target diagnostic unless a default target is explicitly allowed.
- Called-plan slot requirements are included in validation of the assigning template.
- Diagnostics include template ID, action plan ID/template ID, step index, slot, and expected kind when relevant.

## Step 8: Remove arbitrary variable editing from editor services

Remove or deprecate service operations that allow arbitrary variable creation and arbitrary variable-name editing.

Service direction:

- Remove/list-deprecate `ListDefaultPlanVariables`, `SetDefaultPlanVariable`, and `RemoveDefaultPlanVariable` as editor-facing APIs.
- Add canonical actor-state default APIs, such as `GetActorActionStateDefaults` and `SetInitialFacing`.
- Add primitive field-editing operations that only expose catalog-declared literal fields.
- Ensure action-plan check/effect creation no longer asks for variable names.

### Testable outcomes

- Editor service can create/update action-plan checks without variable-name inputs.
- Editor service can create/update action-plan effects without variable-name inputs.
- Editor service exposes initial facing as a constrained field when relevant.
- Editor service no longer exposes arbitrary default variable creation in canonical mode.
- Existing editor-service tests using arbitrary variables are migrated or marked as legacy compatibility tests.

## Step 9: Simplify the GUI action-plan workflow

Remove GUI controls that expose arbitrary variable names and variable creation.

Simplified GUI direction:

- Users choose check/effect primitive kinds.
- Users edit only literal primitive fields, such as inventory coordinate or called plan reference.
- The editor shows implied canonical slot reads/writes as read-only explanatory metadata if useful.
- The editor exposes initial actor-state defaults, such as initial facing, as simple template fields when required or useful.
- Variable suggestion lists and missing-variable buttons are removed or replaced with canonical-slot diagnostics/actions.

### Testable outcomes

- A user can create the current wandering behavior without typing `facing` or `target`.
- A user can assign initial facing through a constrained direction selector.
- No GUI field requires an arbitrary variable name for current primitives.
- Validation messages refer to canonical slots rather than arbitrary variable names.
- View-model tests cover creating/editing action plans through the simplified workflow.

## Step 10: Remove or contain legacy arbitrary-variable support

Once canonical authoring is stable, decide how much legacy arbitrary-variable support remains.

Recommended direction:

- Runtime internals may keep generic storage if useful.
- Public descriptors, editor services, GUI, and future agent API should use canonical slots only.
- Legacy YAML loading may remain for migration, but canonical save should not emit legacy fields.
- Strict validation should reject arbitrary variable fields.

### Testable outcomes

- New content authored through the editor contains no arbitrary variable-name fields.
- Strict validation rejects arbitrary variable-name fields.
- Legacy content can still be opened and resaved into canonical form if compatibility is retained.
- No editor UI or service method used by the GUI requires arbitrary variable names.

## Step 11: Reassess action-plan parity after variable abstraction

After variables are abstracted, reassess action-plan parity before adding the agent API.

Remaining parity questions likely include:

- whether every intended Core action primitive is available as a descriptor/catalog primitive
- whether `DropAction` should become an action-plan primitive or remain direct-player/console behavior
- whether effect turn behavior is static, catalogued, or editor-configurable
- whether action-plan field editing is complete at the service layer
- whether validation covers all authored action-plan fields and references

### Testable outcomes

- A current parity matrix exists for action-plan runtime primitives versus descriptor/catalog/editor support.
- Any intentional non-parity, such as direct-only `DropAction`, is documented.
- Each catalogued primitive has Core runtime tests, descriptor materialization tests, content roundtrip tests, editor-service tests, and GUI/view-model tests where applicable.
- The editor can author all currently supported action-plan primitives without direct YAML editing.

## Step 12: Prepare for the agent-usable editor API

Once variable abstraction and action-plan editing parity are complete, define the agent API over the simplified canonical model.

The agent API should not expose arbitrary variables. It should expose:

- action-plan primitive catalog
- canonical slot metadata as read-only primitive behavior
- action-plan CRUD
- step/check/effect editing with only literal fields
- canonical actor-state defaults
- validation diagnostics using canonical slots
- canonical YAML preview/diff/save

### Testable outcomes

- API design documentation contains no arbitrary variable creation operations.
- API catalog output lists primitive literal fields and canonical slot reads/writes.
- API validation output reports canonical slot diagnostics.
- A minimal API workflow can create the current wandering behavior without variable-name inputs.
