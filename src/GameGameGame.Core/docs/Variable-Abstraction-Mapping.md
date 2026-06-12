# Variable Abstraction Mapping

This document completes Step 1 of `Variable-Abstraction-Plan.md`: it maps every current arbitrary variable surface to the intended canonical-slot replacement or deprecation path.

## Target model summary

Action-plan authors should not create or wire arbitrary variables. Engine primitives will read and write typed canonical slots.

Initial canonical slots:

| Slot | Kind | Lifetime | Purpose |
|---|---|---|---|
| `Facing` | `Direction` | Persistent across turns | Directional intent/state for movement and directional primitives. |
| `Target` | `EntityId` | Persistent across turns | Current selected/observed entity target for target-based primitives. |

Externally, slots are bestowed by primitive usage: the editor should only show or require state implied by selected primitives and assigned action plans. Internally, the engine may use a fixed actor action-state context containing the canonical slots.

## Current arbitrary variable surfaces

| Current surface | Location | Current purpose | Target replacement | Legacy compatibility decision |
|---|---|---|---|---|
| `ActionPlanContext.Variables` | `src/GameGameGame.Core/ActionPlanContext.cs` | Stores arbitrary string-keyed `PlanValue` values. | Replace public primitive-facing usage with typed canonical slot access. Internal storage may remain dictionary-like if keys are canonical slots rather than arbitrary strings. | Temporary compatibility can keep string access while descriptors/loaders migrate. |
| `PlanVariableRef<TValue>` | `src/GameGameGame.Core/PlanValueSources.cs` | Lets primitives read arbitrary named variables. | Replace primitive constructors/properties with canonical slot reads, such as `Facing` and `Target`. | Legacy constructors may remain temporarily for compatibility tests. |
| `PlanCheckDescriptor.DirectionVariable` | `src/GameGameGame.Core/ActionPlanDescriptors.cs` | Editor/YAML chooses which direction variable a check reads. | Remove from canonical authoring. `CanMove` and `BlockingEntity` read `Facing`. | `directionVariable: facing` maps to `Facing`; other names produce strict/canonical diagnostics. |
| `PlanCheckDescriptor.TargetVariable` | `src/GameGameGame.Core/ActionPlanDescriptors.cs` | Editor/YAML chooses which entity variable a check reads or writes. | Remove from canonical authoring. `BlockingEntity` writes `Target`; `CanPickup` reads `Target`. | `targetVariable: target` maps to `Target`; other names produce strict/canonical diagnostics. |
| `PlanEffectDescriptor.DirectionVariable` | `src/GameGameGame.Core/ActionPlanDescriptors.cs` | Editor/YAML chooses which direction variable an effect reads/writes. | Remove from canonical authoring. `Move` reads `Facing`; `ReverseDirection` reads/writes `Facing`. | `directionVariable: facing` maps to `Facing`; other names produce strict/canonical diagnostics. |
| `PlanEffectDescriptor.TargetVariable` | `src/GameGameGame.Core/ActionPlanDescriptors.cs` | Editor/YAML chooses which entity variable an effect reads. | Remove from canonical authoring. `Pickup` reads `Target`. | `targetVariable: target` maps to `Target`; other names produce strict/canonical diagnostics. |
| `PlanEffectDescriptor.VariableName` | `src/GameGameGame.Core/ActionPlanDescriptors.cs` | `SetVariable` writes an arbitrary variable. | Remove `SetVariable` from editor-authored canonical plans, or replace later with constrained canonical-slot effects if needed. | Existing `SetVariable(variableName: facing, value: Direction)` may migrate to a future `SetFacing`/`SetCanonicalSlot` decision. Unknown variables are not canonical. |
| `PlanEffectKind.SetVariable` | `src/GameGameGame.Core/ActionPlanDescriptors.cs`, `src/GameGameGame.Core/PlanBuiltIns.cs` | General arbitrary variable assignment. | Deprecate from editor-facing primitive list during canonicalization unless a specific constrained replacement is chosen. | Runtime may keep temporarily for legacy plans/tests. Canonical save should not emit arbitrary `SetVariable`. |
| `PlanPrimitiveCatalog` variable fields | `src/GameGameGame.Core/PlanPrimitiveCatalog.cs` | Describes editable variable read/write fields. | Replace editable variable fields with read-only canonical slot read/write metadata. Keep literal fields such as coordinates and plan references. | Catalog may report legacy fields only in compatibility metadata if needed. |
| `EntityTemplate.DefaultPlanVariables` | `src/GameGameGame.Content/EntityTemplates.cs` | Template-authored arbitrary variable defaults. | Replace with constrained actor action-state defaults, initially `Facing` and possibly `Target` only if persistent default target is desired. | `defaultPlanVariables.facing` maps to initial `Facing`. `defaultPlanVariables.target` maps only if default `Target` remains supported. |
| YAML `defaultPlanVariables` | `src/GameGameGame.Content/PrototypeContent.yaml` and loader DTOs | YAML representation of arbitrary default variables. | Replace with canonical actor-state defaults in saved YAML. | Current built-in `facing` entries migrate to canonical initial facing. |
| YAML `directionVariable`, `targetVariable`, `variableName` | content YAML and loader/editor DTOs | YAML representation of arbitrary primitive variable wiring. | Remove from canonical YAML for primitives whose slot usage is engine-defined. | Known canonical names map during load; strict/canonical validation rejects unknown names. |
| Editor default-variable list and form | `src/GameGameGame.Editor/MainWindow.cs`, `MainEditorViewModel.cs` | Allows users to list/create/update/delete arbitrary default variables. | Replace with constrained actor-state default controls, such as initial facing direction when required or useful. | Existing tests and UI paths migrate to canonical state defaults. |
| Editor variable suggestions | `src/GameGameGame.Editor/MainWindow.cs`, `MainEditorViewModel.cs` | Suggests arbitrary direction/entity variable names and repair actions. | Remove. Replace with canonical-slot diagnostics/actions if needed, such as “set initial facing.” | Existing missing-variable repair tests migrate to missing-slot default tests. |
| Editor check/effect variable input fields | `src/GameGameGame.Editor/MainWindow.cs`, `MainEditorViewModel.cs` | Lets users type variable names for checks/effects. | Remove for canonical primitives. GUI should show only literal primitive fields and optional read-only slot metadata. | Existing GUI tests expecting `facing`/`target` inputs migrate to no-variable-input workflows. |
| Content validation `MissingPlanVariable` / `PlanVariableTypeMismatch` | `src/GameGameGame.Content/PrototypeContentRegistry.cs`, `ContentValidationResult.cs` | Validates arbitrary variable reads/writes by name. | Replace or supplement with canonical slot diagnostics, e.g. missing `Facing` default or `Target` read before write/default. | Existing diagnostic codes may remain temporarily but messages/metadata should shift from variable names to slot names. |

## Primitive replacement mapping

| Current primitive | Current variable fields | Canonical slot behavior | Remaining authored fields |
|---|---|---|---|
| `CanMove` check | `directionVariable` | Reads `Facing`. | None. |
| `BlockingEntity` check | `directionVariable`, `targetVariable` | Reads `Facing`; writes `Target`. | None. |
| `CanPickup` check | `targetVariable`, `inventoryCoord` | Reads `Target`. | `inventoryCoord`, unless later replaced by another canonical/literal policy. |
| `Move` effect | `directionVariable` | Reads `Facing`. | None. |
| `Pickup` effect | `targetVariable`, `inventoryCoord` | Reads `Target`. | `inventoryCoord`, unless later replaced by another canonical/literal policy. |
| `ReverseDirection` effect | `directionVariable`, turn flags | Reads and writes `Facing`. | Turn-control policy is separate from variable abstraction and should be resolved during action-plan parity cleanup. |
| `Wait` effect | None | Reads/writes no slots. | None. |
| `SetVariable` effect | `variableName`, `value`, turn flags | Not part of canonical editor-authored plans by default. | Replacement undecided; likely remove from editor or replace with constrained canonical-slot effects. |
| `CallPlan` effect | `planId` | Called plan shares the same persistent actor action state. | `planId`. |

## Legacy YAML fields

Known legacy fields that should be supported during migration:

- `defaultPlanVariables`
- `directionVariable`
- `targetVariable`
- `variableName` for existing `SetVariable` content/tests if retained temporarily

Known safe mappings:

- `defaultPlanVariables.facing` -> canonical initial `Facing`
- `directionVariable: facing` -> canonical `Facing`
- `targetVariable: target` -> canonical `Target`

Potentially unsafe mappings:

- Any non-`facing` direction variable name.
- Any non-`target` target variable name.
- Any `SetVariable` that writes a non-canonical variable.
- Any default variable whose name is not a canonical slot alias.

Strict/canonical mode should diagnose unsafe mappings rather than silently preserving arbitrary variable behavior.

## Affected implementation areas

Core:

- `ActionPlanContext.cs`
- `PlanValueSources.cs`
- `ActionPlanDescriptors.cs`
- `PlanBuiltIns.cs`
- `PlanPrimitiveCatalog.cs`
- `InterpretedEntityActionPlan.cs`
- `ActionPlanInterpreter.cs`

Content/editor backend:

- `EntityTemplates.cs`
- `YamlContentLoader.cs`
- `EditableContentDocument.cs`
- `PrototypeContentRegistry.cs`
- `ContentValidationResult.cs`
- `ContentEditorService.cs`
- `ContentEditorSession.cs` indirectly through save/reload roundtrips

GUI editor:

- `MainEditorViewModel.cs`
- `MainWindow.cs`

Built-in content reference:

- `src/GameGameGame.Content/PrototypeContent.yaml` currently uses `defaultPlanVariables.facing`, `directionVariable: facing`, and `targetVariable: target`. It should be treated as read-only reference unless content migration is explicitly approved.

## Affected tests

The migration will touch tests that assert arbitrary variable descriptors, YAML, diagnostics, editor fields, or variable suggestions.

Core tests likely affected:

- `ActionPlanContextStoresTypedVariables`
- `PlanVariableRefReadsTypedVariableFromContext`
- `PlanInterpreterCommitsCheckVariableWritesBeforeEffect`
- `BuiltInCanMoveCheckFailureFallsThroughToSetVariableEffect`
- `ActionPlanContextVariableUpdatesAreTraced`
- `CallPlanEffectRunsNestedPlanWithSharedContextAndTrace`
- descriptor/materialization tests that assert `directionVariable`, `targetVariable`, or `SetVariable`

Content/editor-service tests likely affected:

- tests for `ContentEditorServiceEditsTemplateDefaultPlanVariables`
- tests for validation of missing variables and type mismatches
- tests for action-plan step checks/effects containing `DirectionVariable`, `TargetVariable`, or `VariableName`
- YAML roundtrip tests that assert `defaultPlanVariables`, `directionVariable`, `targetVariable`, or `variableName`

Editor view-model tests likely affected:

- tests that select or edit check direction/target variable inputs
- tests that select or edit effect direction/target variable inputs
- tests for `SetVariable` effect UI
- tests for variable visibility properties
- tests for variable suggestion lists
- tests for missing-variable repair buttons
- tests for default variable list/create/update/remove workflows
- tests asserting YAML preview contains `directionVariable`, `targetVariable`, `variableName`, or `defaultPlanVariables`

## Completion criteria for Step 1

Step 1 is complete when:

- every current arbitrary variable surface has a target replacement or deprecation path in this document
- legacy YAML fields are identified
- currently affected test categories are identified
- later code-changing steps can use this mapping as the migration checklist
