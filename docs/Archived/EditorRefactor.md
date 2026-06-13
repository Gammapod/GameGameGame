# Content Editor Plan

## High-Level Goals

- Make game content editable without requiring direct C# changes.
- Preserve the Core/Content boundary: Core defines engine primitives and runtime behavior, while Content defines templates, presentations, inventory layouts, action plans, and defaults.
- Keep YAML as the first human-readable content format while introducing editor-friendly APIs that can load, validate, inspect, and eventually save content.
- Support incremental editor tooling: start with safe data browsing and form editing, then add richer inventory and action-plan editors.
- Ensure editor-facing diagnostics catch content mistakes before runtime spawning or plan execution.

## Step 1: Expose Registry Enumeration APIs

Editors need to list available content, not only resolve known IDs. Add public read APIs to enumerate entity templates, presentations, and action-plan descriptors from the content registry.

Decisions needed:

- Decide whether enumeration returns the existing dictionaries, read-only snapshots, or dedicated DTOs.
- Decide whether joined entity-preset views should live in `GameGameGame.Content` or in a future editor/tooling assembly.

Testable outcomes:

- A caller can enumerate all entity template IDs and templates from a loaded registry.
- A caller can enumerate all presentation IDs and presentation data from a loaded registry.
- A caller can enumerate all action-plan template IDs and descriptors from a loaded registry.
- Existing registry lookup behavior remains unchanged.

## Step 2: Harden Content Validation

Validation should become useful for authoring feedback, not only runtime safety. Expand registry validation to catch common editor and YAML mistakes early.

Decisions needed:

- Decide whether validation remains string-based or moves to structured diagnostics with severity, path, code, and message.
- Decide whether unknown YAML fields should fail load, warn during validation, or remain ignored for forward compatibility.
- Decide whether validation should enforce presentation existence for every template or allow intentionally invisible/internal templates.

Testable outcomes:

- Validation reports carried entities placed outside the parent inventory bounds.
- Validation reports overlapping carried entities in the same inventory layout.
- Validation reports duplicate carried entity IDs within content where applicable.
- Validation reports carried entities on templates without usable inventory.
- Validation reports missing entity template references and missing action-plan references.
- Validation reports invalid descriptor materialization with enough context to locate the bad template, plan, step, check, or effect.

## Step 3: Add A Round-Trippable Content Document Model

The current YAML loader materializes directly into runtime-friendly registry objects. Editors also need a document model that can preserve authoring structure and save changes back to YAML.

Decisions needed:

- Decide whether the existing loader DTOs become public document types or whether separate editor document types are introduced.
- Decide how much YAML formatting and ordering should be preserved on save.
- Decide whether generated IDs are slug-based, GUID-like, or deterministic from names with collision suffixes.
- Decide whether presentations stay in a separate YAML section or are edited and saved as part of a joined entity preset view.

Testable outcomes:

- A YAML document can be loaded into an editable content document object.
- The editable document can materialize into a `PrototypeContentRegistry`.
- The editable document can be saved back to YAML.
- Loading, saving, and reloading preserves equivalent templates, presentations, action plans, carried layouts, and default variables.
- New entity templates can be created with generated stable IDs and saved to YAML.

## Step 4: Add Engine Primitive Metadata Catalogs

Action-plan editors need dropdown lists and field schemas supplied by Core, not hardcoded by editor UI. Add metadata for plan checks, effects, and value kinds.

Decisions needed:

- Decide the shape of primitive metadata: records, attributes, static catalog methods, or generated descriptors.
- Decide whether metadata lives beside descriptor materialization in Core or in a separate tooling-facing layer.
- Decide how to represent variable reads, variable writes, field requirements, literal value fields, action-plan references, and flow flags.
- Decide whether primitive display names/descriptions belong in Core metadata or a localization/presentation layer.

Testable outcomes:

- A caller can enumerate all supported `PlanCheckKind` values with display metadata and required fields.
- A caller can enumerate all supported `PlanEffectKind` values with display metadata and required fields.
- Metadata identifies variable reads and expected `PlanValueKind` types.
- Metadata identifies variable writes and produced `PlanValueKind` types.
- Metadata identifies literal fields such as coordinates, plan references, booleans, and values.
- Adding a new check or effect has a clear test that proves it appears in the catalog.

## Step 5: Validate Action-Plan Variables

Variables are currently free-form strings. Editors need type-aware validation so users can see whether a plan initializes, reads, and writes variables consistently.

Decisions needed:

- Decide whether variables are explicitly declared per action plan, inferred from default variables and primitive metadata, or both.
- Decide whether called plans share the caller context by design and how that should be represented in validation.
- Decide whether missing variables are errors, warnings, or allowed when the variable can be supplied by spawn overrides.
- Decide how to validate variables written by checks before effects in the same step or later steps.

Testable outcomes:

- Validation reports a variable read with no known initializer or prior write when the variable is required.
- Validation reports variable type mismatches, such as using an `Entity` variable where a `Direction` is required.
- Validation accepts variables written by checks and read by later effects or nested plans when the flow supports it.
- Validation accounts for template default variables and spawn/action-plan variable overrides where applicable.
- Validation reports called plans whose required context variables are not available from the caller.

## Step 6: Build Incremental Editor Interfaces

Once the APIs are in place, build editor surfaces in order of complexity: entity presets first, inventory layouts second, action plans third.

Decisions needed:

- Decide whether the first editor is an in-game/debug UI, a standalone desktop tool, a web UI, or command-line/editor-service APIs.
- Decide whether editors modify YAML files directly or operate through a save/apply workflow.
- Decide whether inventory editing uses entity templates only, entity instances only, or a hybrid of template reference plus generated instance ID.
- Decide whether action-plan rows support exactly one success and one failure effect initially, or whether the editor should anticipate multiple effects per branch.

Testable outcomes:

- Entity preset editor can create, rename, update stats, assign presentation, assign default action plan, and edit default variables.
- Inventory editor can place, move, remove, and validate carried entity instances on a 2D inventory grid.
- Inventory editor prevents or reports out-of-bounds placement, overlaps, and invalid template references.
- Action-plan editor can add, reorder, edit, and remove ranked rows.
- Action-plan editor populates check, effect, value-kind, variable, and plan-reference dropdowns from engine/content APIs.
- Edited content can be saved, reloaded, validated, spawned, and exercised by existing runtime services.
