# Standalone GUI Editor Plan

## Goals

- Build a standalone desktop GUI that can load, edit, validate, and save content files.
- Keep editor services UI-agnostic so the same backend can support a future in-game editor.
- Start with entity presets and inventory layouts, then expand into action-plan editing.
- Keep YAML visible and trustworthy during early iterations, but avoid requiring users to hand-edit YAML for common workflows.
- Preserve the runtime boundary: edited content must materialize through the same registry and validation path used by the game.

## Current Service Foundation

Already available:

- `EditableContentDocument` loads, saves, and materializes YAML-shaped content.
- `ContentEditorService` lists and edits joined entity presets.
- `ContentEditorService` places and moves carried entities.
- `ContentEditorService` lists and edits action-plan steps.
- `ContentEditorService` edits template default plan variables.
- `ContentEditorService.Validate()` returns current registry validation results.
- `PlanPrimitiveCatalog` exposes engine-defined check/effect/value metadata for editor dropdowns.

Known limitations:

- Diagnostics are string-only and not mapped to document paths or UI fields.
- File workflow is not represented as an editor session with dirty state, save path, reload, or error handling.
- Inventory editing has place/move operations but no remove/replace/list-specific helpers.
- Action-plan editing edits whole step descriptors, not field-level primitive inputs.
- There is no preview/simulation service for validating edited content against runtime behavior.
- There is no GUI project yet.

## Step 1: Editor Session And File Workflow

Introduce an editor-session service around `EditableContentDocument` and `ContentEditorService` for GUI ownership of file state.

Expected complexity: Medium.

Needed service capabilities:

- Open content from a file path.
- Create a new empty content document.
- Save to the current path.
- Save as a new path.
- Track dirty state after mutations.
- Expose current file path and document status.
- Surface load/save errors without crashing the UI.

Testable outcomes:

- Opening a YAML file creates an editor session with the expected document and file path.
- Saving writes YAML that can be reloaded into an equivalent registry.
- Mutating through the editor service marks the session dirty.
- Saving clears dirty state.
- Save-as changes the session file path.
- Invalid file load returns a useful failure result instead of throwing into the GUI layer.

## Step 2: Structured Editor Diagnostics

Move beyond string-only validation by adding diagnostics that can be mapped to UI panels, fields, rows, and grid cells.

Expected complexity: High.

Needed service capabilities:

- Diagnostic severity, such as error, warning, info.
- Diagnostic code, such as `MissingPresentation`, `InventoryOverlap`, `MissingVariable`.
- Diagnostic message for display.
- Content path or target, such as entity template ID, action-plan ID, step index, carried entity ID, variable name.
- Compatibility adapter from existing string validation, if needed during transition.

Testable outcomes:

- Inventory bounds errors identify the parent template ID and carried entity ID.
- Inventory overlap errors identify both carried entity IDs and the coordinate.
- Missing presentation errors identify the entity template ID.
- Missing action-plan reference errors identify the source template and referenced plan ID.
- Missing variable and type mismatch diagnostics identify template, action plan, step, variable name, expected type, and actual type when known.
- GUI callers can filter diagnostics by selected entity preset, selected inventory layout, or selected action plan.

## Step 3: Complete Entity Preset Editing Service

Round out preset operations so the GUI can implement normal create/edit/delete workflows without directly mutating document DTOs.

Expected complexity: Medium.

Needed service capabilities:

- Create entity preset with generated ID and default presentation.
- Rename/update mechanical fields.
- Update presentation fields.
- Delete entity preset with validation or reference checks.
- Duplicate entity preset with generated ID.
- List references to a template before deletion.
- Assign or clear default action plan.

Testable outcomes:

- Creating a preset adds both template and presentation entries.
- Generated IDs are stable and collision-safe.
- Updating stats and presentation persists through save/reload.
- Deleting a referenced template reports references or is blocked.
- Duplicating a template copies mechanical fields, presentation, default variables, and carried layout with safe IDs where needed.
- Assigning a default action plan updates YAML and validation catches missing variables.

## Step 4: Complete Inventory Layout Editing Service

Make inventory editing grid-friendly and safe enough for a desktop GUI.

Expected complexity: Medium.

Needed service capabilities:

- List carried entities for a parent template as editor models.
- Place carried entity with generated instance ID.
- Move carried entity.
- Remove carried entity.
- Replace carried entity template reference.
- Auto-find first open inventory cell.
- Validate proposed placement before applying, or apply and return diagnostics.

Testable outcomes:

- Listing carried entities returns IDs, template references, coordinates, and presentation data when available.
- Placing with generated instance ID produces a collision-safe ID.
- Moving an entity updates only that carried entity.
- Removing an entity persists through save/reload.
- Replacing a carried template reference updates validation and presentation lookup.
- First-open-cell helper returns expected coordinates or no result when full.
- Proposed invalid placement reports out-of-bounds or overlap diagnostics without requiring runtime spawn.

## Step 5: Action-Plan Field Editing Service

Provide field-level action-plan operations so a GUI can bind primitive forms directly to service calls.

Expected complexity: High.

Needed service capabilities:

- Create, rename, duplicate, and delete action plans.
- Add check to a step from `PlanCheckKind` using default field values.
- Update check field values by field name.
- Remove and reorder checks within a step.
- Set success or failure effect from `PlanEffectKind` using default field values.
- Update effect field values by field name.
- Clear success or failure effect.
- Expose field schemas from `PlanPrimitiveCatalog` together with current descriptor values.

Testable outcomes:

- Creating a plan adds a valid empty action-plan descriptor.
- Adding a `CanMove` check creates a descriptor with editable `directionVariable` field.
- Updating a check variable field persists through save/reload.
- Setting a `Move` effect creates an editable `directionVariable` field.
- Setting a `CallPlan` effect exposes a plan-reference field populated from known plans.
- Reordering checks preserves descriptor content.
- Invalid primitive field combinations are reported by validation.

## Step 6: Variable Editing And Inference Helpers

Action-plan UI needs strong support for variables, because this is where editor users are most likely to make mistakes.

Expected complexity: High.

Needed service capabilities:

- List default variables for an entity template.
- Add, update, remove default variables.
- Infer variable reads and writes for an action plan using `PlanPrimitiveCatalog`.
- Suggest variables by expected `PlanValueKind` for primitive fields.
- Suggest missing default variables for a template-assigned plan.
- Show variables produced by checks and effects in plan order.

Testable outcomes:

- Variable list returns name, kind, and literal value.
- Adding/updating/removing variables persists through save/reload.
- Variable suggestions for a `Direction` field include known direction variables and exclude entity variables.
- Missing-variable suggestions identify `facing` for a plan using `CanMove("facing")`.
- Check-written variables, such as `BlockingEntity.target`, appear as available for later effects in the same step or later rows.
- Type mismatch diagnostics can be resolved through variable editor operations.

## Step 7: YAML Preview And Document Diff Support

Early GUI users should be able to trust the generated YAML and understand what changed.

Expected complexity: Low to Medium.

Needed service capabilities:

- Get current YAML preview without saving.
- Compare current YAML to last loaded/saved YAML.
- Reset/reload current document from disk after confirmation.
- Optional: normalize YAML formatting for stable diffs.

Testable outcomes:

- YAML preview reflects current in-memory edits.
- Dirty document diff reports changed YAML after a mutation.
- Saving updates the baseline used for diffs.
- Reload discards unsaved edits and restores file content.

## Step 8: Runtime Preview And Smoke-Test Service

A useful standalone GUI should eventually prove that edited content can be used by the game, not just validated structurally.

Expected complexity: Medium to High.

Needed service capabilities:

- Materialize current document into a registry.
- Spawn selected entity template into a small preview world.
- Inspect spawned entity using existing inspection services.
- Optionally advance turns for entities with action plans.
- Return traces or preview errors to the GUI.

Testable outcomes:

- A selected entity template can be spawned into a preview world.
- Preview inspection returns name, presentation, stats, and inventory grid.
- A template with carried entities shows those entities in preview inventory.
- A template with an action plan can execute one preview turn and return trace output.
- Runtime preview failures are reported as editor diagnostics or preview errors.

## Step 9: Standalone Desktop GUI Skeleton

Create the GUI app once the service layer can support a meaningful first vertical slice.

Expected complexity: Medium.

Likely GUI choices:

- Avalonia: good cross-platform desktop option, pure .NET, suitable for this project.
- WPF: fastest if Windows-only is acceptable.
- WinUI: modern Windows UI, more platform-specific.

Recommended first choice: Avalonia, unless Windows-only speed is more important than portability.

Testable outcomes:

- GUI project builds in the solution.
- GUI can open a YAML content file.
- GUI shows entity preset list and selected preset form.
- GUI can edit entity name, stats, glyph, and color.
- GUI can show current validation errors.
- GUI can save YAML and reload it through existing services.

## Step 10: First End-To-End GUI Editing Experiment

Build the smallest usable desktop editor workflow.

Expected complexity: Medium to High.

Workflow:

- Open `PrototypeContent.yaml` or another content file.
- Select an entity preset.
- Edit mechanical fields and presentation.
- Edit inventory layout on a 2D grid.
- Validate current document.
- Preview YAML.
- Save.
- Reload and verify content remains valid.

Testable outcomes:

- A user can complete the workflow without touching YAML directly.
- Saved YAML materializes into a valid registry.
- Entity and inventory edits survive app restart/reload.
- Validation errors are visible before save.
- The GUI does not need to reference runtime-only action plan or entity behavior types directly.

## Recommended Build Order

1. Editor session and file workflow.
2. Complete entity preset editing service.
3. Complete inventory layout editing service.
4. YAML preview and diff support.
5. Standalone GUI skeleton with entity preset editing.
6. Structured diagnostics.
7. Action-plan field editing service.
8. Variable editing and inference helpers.
9. Runtime preview service.
10. Full action-plan GUI.

This order gets to a usable standalone GUI quickly while avoiding premature complexity in the action-plan editor.
