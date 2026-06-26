# Sprint 21: Console Scenario Catalog

Status: Archived completed plan. First phase implemented; future package/import follow-up deferred.

Related source of truth:

- `docs/Source of Truth/invariants.md` records stable behavior contracts and test traces.
- `docs/Source of Truth/testing-charter.md` records TDD expectations and notes that Console UI tests are currently deprioritized.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` records implemented support status after this plan is implemented.
- `docs/Source of Truth/Content-Authoring-Manual.md` records author-facing supported workflows after this plan is implemented.
- `docs/Plans/High-Level-Roadmap.md` owns backlog priority and longer-horizon package ideas.

## Goal

Make Console usable as a lightweight scenario browser for prototype playtesting while preserving the UI-agnostic Content/Core launch contract that a future frontend can consume.

The first selected approach is the hybrid discovery + cached manifest model:

1. discover scenarios from a designated folder tree of existing YAML content documents and automatically write a `Manifest.yaml` cache to the scanned folder;
2. read a generated scenario manifest so normal Console startup can load a cached list instead of scanning every file;
3. let Console show a scenario list, launch a selected scenario, and return to the list when the player exits the scenario.

Do not add scenario-definition metadata fields or content package files in this slice. The generated manifest may carry an optional `description` field per entry for menu display; scanner-created entries leave it blank, and rediscovery preserves manually-authored descriptions for unchanged content path + scenario ID pairs.

Default paths for the first implementation:

- Default discovery folder: `src\GameGameGame.Content\Beta`
- Default manifest/cache path: `src\GameGameGame.Content\Beta\Manifest.yaml`

## Scope

### In scope

- A shared scenario-catalog model that is independent of Console rendering/input and can be reused by a future frontend.
- A single-file catalog path that lists the `scenarios:` entries in one `EditableContentDocument`.
- A folder-discovery path that scans a designated folder for YAML files, skips the generated `Manifest.yaml`, writes a fresh manifest/cache to the scanned folder, and records each discovered scenario with at least:
  - content file path;
  - scenario ID;
  - scenario display name from the existing scenario `name` field when available;
  - optional manifest-only description text when already present in the manifest.
- A generated manifest/cache file containing discovered scenario entries.
- A command/workflow to populate or refresh the manifest from folder discovery; folder discovery itself refreshes `<folder>\Manifest.yaml`, and the command may also write an explicit `--output` manifest path.
- A Console menu that can load a manifest or discover a folder, show scenarios, launch one, and return to the list after scenario exit.
- Preserve existing direct launch by content file path + scenario ID.
- Keep scenario launch using existing materialization outputs: world state, registry, action plans, player entity ID, and active plane.

Suggested CLI shape:

- `GameGameGame.Console <content-file> <scenario-id>`: existing direct launch.
- `GameGameGame.Console --content <file>`: list scenarios from one file.
- `GameGameGame.Console --discover <folder>`: discover scenarios from a folder, refresh `<folder>\Manifest.yaml`, and show the menu.
- `GameGameGame.Console scan-scenarios <folder> --output <manifest>`: generate or refresh a manifest.
- `GameGameGame.Console --manifest <manifest>`: load scenarios from a cached manifest.
- `GameGameGame.Console`: use the default manifest when present; otherwise discover the default folder and write the default manifest.

### Out of scope

- New scenario metadata fields beyond existing scenario ID/name/root/player/start data.
- Content package root files or import/merge semantics.
- Splitting content by type into separate entity/action-plan/scenario files.
- Extensive keyboard-driven Console UI tests.
- Reorganizing or modifying existing checked-in content YAML files.

## TDD and test strategy

Console is a prototype frontend, so exhaustive UI testing is not the priority. The priority is the contract between Console and shared Content/Core services.

Testable outcomes before production code changes:

1. A catalog builder lists scenarios from a single editable document with stable path/scenario ID/name data.
2. Folder discovery finds scenarios from multiple YAML files without requiring Console input handling.
3. A manifest round-trips discovered scenario entries and can be used instead of rescanning.
4. Launching a catalog entry creates a fresh `ConsoleGameSession` through existing scenario materialization.
5. Launching the same entry twice does not reuse mutated runtime world state.

Invariant/test trace:

- Affected invariant: `Console scenario launch consumes scenario materialization outputs rather than hardcoded prototype player or plane IDs.`
- Existing tests: `ConsoleScenarioLauncherBuildsPlayableSessionFromPersistedScenario`, `AlphaScenarioFixtureCanLaunchInConsoleAndAcceptPlayerMove`.
- Likely revised/new tests:
  - keep existing launch tests as the core launch-contract trace;
  - add catalog/manifest tests around the new shared catalog service;
  - optionally add a small launcher-by-catalog-entry test if launch selection is factored outside interactive Console input.

## Implementation notes

- Prefer placing catalog/discovery/manifest logic in a non-UI layer that Console and future frontend code can call. Console should not own content discovery semantics directly.
- Discovery should tolerate invalid or partial YAML by surfacing diagnostics per file rather than crashing the whole menu when possible.
- Scenario ID collisions across different files should remain distinguishable by content path + scenario ID. A later metadata/package slice can decide whether global scenario IDs are required.
- The manifest is a cache/index of existing loose content files, not a package definition and not the source of content truth.
- Optional manifest `description` values are human-facing menu annotations only. They do not change scenario materialization, validation, or content document semantics.
- Existing direct invocation should remain available for scripts and agent workflows.

## Deferred follow-up

If content duplication or extensive reuse becomes the bottleneck, revisit a content package model with explicit package files, import/merge semantics, and possibly separate files for scenario definitions, reusable entity templates, presentations, and action plans. That work is intentionally deferred because the current pain point is scenario discovery/selection, not reuse architecture.
