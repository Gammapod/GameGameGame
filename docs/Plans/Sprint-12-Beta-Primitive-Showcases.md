# Sprint 12: Beta Primitive Showcases

Status: Active sprint plan.

Read when:

- authoring or testing the first beta primitive showcase scenarios;
- deciding which current-tool vignette to build next;
- recording capability gaps, reporting gaps, or content-organization friction discovered during primitive showcases.

Related plans:

- `docs/Plans/Beta-Content-Exploration-Plan.md`
- `docs/Plans/Beta-Capability-Gap-Log.md`
- `docs/Plans/High-Level-Roadmap.md`
- `docs/Source of Truth/Engine-Editor-Capabilities.md`

## Sprint goal

Create and validate the first beta gameplay vignettes using currently authorable capabilities, one primitive showcase at a time. Each showcase should be defined as content, tested headlessly until the content editor is satisfied, then handed to the user for manual Console testing and discussion before moving to the next showcase.

## Working loop

For each primitive showcase:

1. Define the intended vignette and expected observations.
2. Author the scenario using current content/editor capabilities.
3. Validate the content document.
4. Run the scenario headlessly and inspect behavior/report output.
5. Iterate on content until the scenario is clear enough to hand off.
6. Provide the user with the scenario ID, content file path, expected behavior, manual test notes, and any discovered capability/reporting gaps.
7. Discuss results before authoring the next showcase.

## Showcase order

1. `PushFacing` primitive showcase.
2. `DestroyTarget` primitive showcase.
3. `CreateFacing` primitive showcase.
4. `DropFacing` primitive showcase, if not folded into the pickup/drop/weight vignette.
5. Pickup/drop/weight puzzle.
6. First curated local-interaction actor zoo.

## First showcase: `PushFacing`

Target outcome:

- demonstrate a successful push into an empty cell;
- demonstrate a failed push when the pushed entity is blocked;
- make the resulting movement/blocker behavior readable in headless reports and playable in Console;
- record any missing report summaries or content-authoring friction.

Initial constraints:

- use only currently supported canonical behavior-chain Action Steps;
- prefer a small scenario-root inventory room;
- keep content isolated from alpha/prototype fixtures unless explicitly reusing stable templates;
- do not request Direction Transform work until current-tool showcases have been exercised.

## Expected sprint outputs

- One or more beta content documents or organized beta content files.
- At least the `PushFacing` showcase content and manual-test handoff.
- Capability-gap notes for any blocked desired behavior.
- Reporting/tooling requests if scenario behavior is hard to interpret.
- A recommendation on whether to continue current-tool primitive showcases or promote the Direction Transform gate.

## Current sprint notes

- `PushFacing` and `DestroyTarget` showcases are Console-playable and headlessly validated.
- `CreateFacing` is headlessly validated, but successful creation currently exposes GAP-001: runtime placeholder entities lack content-template/presentation assignment and can crash Console rendering/inspection after creation. Until this is fixed, successful `CreateFacing` should be treated as headless-only for manual review.
