---
id: plan.documentation-compiler-spike
title: Documentation Compiler Spike Plan
kind: plan
status: active
truth_rank: 45
truth_domains: [implementation-navigation, process]
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
read_when:
  - implementing the documentation compiler spike
  - reviewing documentation schema and linting MVP scope
related:
  - source.planning-index
  - source.testing-charter
  - source.invariants
---
# Documentation Compiler Spike Plan

Status: Active spike plan.

## Purpose

Prove that GameGameGame project documentation can be described by a small deterministic schema, linted as project tooling, and used to derive role-specific read paths for agent workflows.

The spike should improve discoverability and fact-finding for the current target audiences:

- `core-owner`
- `frontend-owner`
- `content-editor`

This is a tooling spike, not a broad documentation rewrite. The MVP should establish enough structure to validate the approach before any large consolidation effort.

## Source-of-truth context

Relevant current documentation rules come from:

- `docs/Source of Truth/planning-index.md` for documentation lanes and planning conventions.
- `docs/Source of Truth/invariants.md` for stable behavior/test-trace ownership.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` for maintainer-facing capability tier/layer ownership.
- `docs/Source of Truth/Content-Authoring-Manual.md` for content-facing authorability and gap workflow ownership.
- `docs/Source of Truth/Frontend-UX-Invariants.md`, `docs/Source of Truth/Frontend-UX-Standards.md`, and `docs/Source of Truth/Frontend-UX-Decisions.md` for frontend boundary, presentation, and decision-log ownership.
- `docs/Source of Truth/testing-charter.md` for TDD expectations on project tooling changes.

## MVP target statement

The MVP proves viability when a documentation compiler can:

1. Parse Markdown frontmatter from a selected documentation set.
2. Build a documentation graph from declared document IDs and relationship fields.
3. Lint deterministic documentation invariants with useful diagnostics.
4. Generate ordered read paths for `core-owner`, `content-editor`, and `frontend-owner`.
5. Run through tests as normal project tooling.

## Proposed tool location

Preferred initial location:

```text
src/GameGameGame.Documentation/
tests/GameGameGame.Documentation.Tests/
```

If the implementation discovers that a script-style tool is much cheaper for the spike, it may use a temporary tooling location, but the plan should record that as a deliberate spike decision before implementation.

Candidate commands:

```text
dotnet run --project src/GameGameGame.Documentation -- lint
dotnet run --project src/GameGameGame.Documentation -- graph
dotnet run --project src/GameGameGame.Documentation -- read-path --role core-owner
dotnet run --project src/GameGameGame.Documentation -- read-path --role content-editor
dotnet run --project src/GameGameGame.Documentation -- read-path --role frontend-owner
```

## Minimal metadata schema

MVP frontmatter example:

```yaml
---
id: source.content-authoring-manual
title: Content Authoring Manual
kind: source-of-truth
subkind: authoring-manual
status: active
owners: [content-editor]
audience: [content-editor, core-owner]
lane: content-authoring
read_when:
  - starting content authoring
  - deciding current authorability
do_not_read_when:
  - changing Core behavior or tests
related:
  - source.engine-editor-capabilities
  - source.planning-index
supersedes: []
superseded_by:
code_refs: []
test_refs: []
---
```

### Required for every compiled document

- `id`
- `title`
- `kind`
- `status`
- `owners`
- `audience`

### Required for `kind: source-of-truth`

- `lane`
- `read_when`

### Optional in MVP

- `subkind`
- `do_not_read_when`
- `related`
- `supersedes`
- `superseded_by`
- `truth_rank`
- `truth_domains`
- `code_refs`
- `test_refs`
- `manual_review`
- `decision_refs`
- `invariant_refs`
- `boundary_refs`
- `component_gallery_refs`

Optional fields should parse without forcing complete adoption in the first spike.

## Initial allowed values

### Roles

- `core-owner`
- `content-editor`
- `frontend-owner`

The schema should not overfit permanently to only these roles, but unknown roles should be linted in the MVP unless explicitly allowed by configuration or code.

### Kinds

- `source-of-truth`
- `plan`
- `roadmap`
- `backlog-reference`
- `gap-log`
- `retrospective`
- `archived`
- `generated`

### Source-of-truth lanes

- `navigation`
- `invariant-trace`
- `testing`
- `capability-matrix`
- `content-authoring`
- `action-logic`
- `frontend-game-text`
- `frontend-ux-invariants`
- `frontend-ux-standards`
- `frontend-ux-decisions`
- `ux-spec`
- `vertical-slice`
- `current-goals`
- `glossary`
- `design-notes`

## MVP lint invariants

The first linter should focus on deterministic structural checks.

1. Every compiled document has required metadata.
2. Document IDs are unique.
3. `owners` and `audience` use known role IDs.
4. `kind` uses a known kind.
5. `source-of-truth` documents have `lane` and non-empty `read_when`.
6. `source-of-truth` lanes use known lane IDs.
7. `related`, `supersedes`, and `superseded_by` references resolve to known document IDs when present.
8. Markdown links inside compiled docs resolve to existing files where practical for repository-relative links.
9. `code_refs` resolve to existing repository paths when present.
10. `test_refs` parse as declared references; path-style test refs should resolve when present. Exact test-name lookup may be deferred.
11. Documents under `docs/Source of Truth/` should be `kind: source-of-truth` unless explicitly exempted.
12. Documents under `docs/Archived/` should be `kind: archived` or have `status: archived` unless explicitly exempted.
13. Active implementation plans should not live under `docs/Archived/`.
14. Archived documents should not be emitted as current authority in role read paths unless explicitly included as historical context.
15. Generated read paths must contain only known document IDs.

## Source-of-truth lane ownership rules to encode or prepare for

The MVP should encode the simple form where possible and leave richer semantic linting as follow-up.

- Stable behavior contracts and test traces belong in `invariant-trace` docs.
- Maintainer-facing capability tiers and layer coverage belong in `capability-matrix` docs.
- Content-editor-facing authoring capabilities, workflows, limits, and gap workflow guidance belong in `content-authoring` docs.
- Canonical Action Step outcome and affordance logic belong in `action-logic` docs.
- Frontend/shared-service boundary rules and frontend test traces belong in `frontend-ux-invariants` docs.
- Frontend presentation standards belong in `frontend-ux-standards` docs.
- Frontend chronological rationale belongs in `frontend-ux-decisions` docs.
- Active implementation details belong in active plans under `docs/Plans/`.
- Completed implementation plans belong under `docs/Archived/` and should be summarized, not duplicated, in active planning docs.

## MVP documentation set to annotate

Do not attempt full repository annotation in the spike. Start with enough documents to prove graph/read-path value.

### Core/source docs

- `docs/Source of Truth/planning-index.md`
- `docs/Source of Truth/invariants.md`
- `docs/Source of Truth/testing-charter.md`
- `docs/Source of Truth/Engine-Editor-Capabilities.md`
- `docs/Source of Truth/Content-Authoring-Manual.md`
- `docs/Source of Truth/Action-Step-Outcome-And-Affordance-Logic.md`
- `docs/Source of Truth/vertical-slice-map.md`

### Frontend docs

- `docs/Source of Truth/Frontend-UX-Invariants.md`
- `docs/Source of Truth/Frontend-UX-Standards.md`
- `docs/Source of Truth/Frontend-UX-Decisions.md`
- `docs/Source of Truth/Entity-Panel-UX-Spec.md`
- `docs/Source of Truth/Frontend-Game-Text.md`

### Planning/reference samples

- `docs/Plans/High-Level-Roadmap.md`
- `docs/Plans/Canonical-Actions-Vertical-Slice-Plan.md`
- `docs/Plans/Beta-Capability-Gap-Log.md`
- `docs/Plans/Sprint-Retrospective.md`

## MVP role read paths

Role profiles may be hardcoded for the spike. Move them to configuration only after the approach is proven.

### `core-owner`

Required starting lanes:

1. `navigation`
2. `invariant-trace`
3. `testing`
4. `capability-matrix`
5. `vertical-slice`

Conditional lanes:

- `content-authoring` when authoring parity or editor/content workflow changes.
- `action-logic` when Action Step outcome/affordance behavior changes.
- frontend lanes when shared frontend contracts or play surfaces are in scope.

### `content-editor`

Required starting lanes:

1. `navigation`
2. `content-authoring`

Conditional lanes:

- `capability-matrix` when checking implementation support tiers.
- `action-logic` when summarizing Action Step outcome behavior.
- `gap-log` / planning references when desired content is unsupported.

The generated path should avoid making `invariant-trace` the normal starting point for content authoring.

### `frontend-owner`

Required starting lanes:

1. `navigation`
2. `frontend-ux-invariants`
3. `frontend-ux-standards`
4. `frontend-ux-decisions`

Conditional lanes:

- `ux-spec` for entity panel, breadcrumb, inspection, and log work.
- `frontend-game-text` for player-facing log wording/message ID work.
- `capability-matrix` when shared engine/editor/frontend support boundaries are involved.
- `content-authoring` when Editor mode authoring workflows are involved.

The generated path should warn against frontend-only simulation, action legality, materialization, or editor mutation semantics.

## Truth rank and domain model

Truth rank is an ordering aid for deterministic conflict handling. Lower numbers are treated as more authoritative when two compiled documents appear to differ within the same fact domain. This does not automatically reconcile prose contradictions; it tells agents which source to trust first and which lower-ranked source should be marked for follow-up.

Initial rank bands:

- `10`: stable behavior/test-trace truth derived from current code and tests.
- `15`: testing workflow policy.
- `20`: high-level navigation, capability support, and frontend boundary truth.
- `25`: specialized runtime/action, UX projection truth, and current-goals planning bridge.
- `30`: content authorability and frontend presentation guidance.
- `35`: implementation navigation.
- `40`: decision rationale/history for active standards.
- `45`: active implementation plans.
- `50`: active roadmap priority.
- `55`: reference backlog/gap logs.
- `70`: retrospective/process notes.

Initial fact domains:

- `runtime-behavior`
- `stable-contract`
- `test-trace`
- `capability-support`
- `parity-tier`
- `authorability`
- `content-workflow`
- `gap-workflow`
- `action-logic`
- `frontend-boundary`
- `frontend-presentation`
- `frontend-rationale`
- `planning-priority`
- `implementation-navigation`
- `testing-policy`
- `process`
- `navigation`

Conflict rule for agents:

1. Identify the fact domain involved in the apparent contradiction.
2. Prefer the compiled document with the lowest `truth_rank` in that domain.
3. Do not synthesize a compromise automatically.
4. Record the lower-ranked contradiction as follow-up documentation cleanup.

The implementation should treat this as metadata and linting support first. Semantic contradiction detection remains deferred.

## Traversal profiles

Traversal profiles model common discovery tasks as deterministic paths through lanes. They are separate from owner read paths: owner read paths answer “what should this role usually start with?”, while traversal profiles answer “what path supports this task?”

Initial profiles:

- `core-stable-behavior-change`: navigation -> invariant-trace -> testing -> capability-matrix -> vertical-slice.
- `capability-support-change`: navigation -> capability-matrix -> invariant-trace -> content-authoring -> vertical-slice.
- `capability-support-change`: navigation -> current-goals -> capability-matrix -> invariant-trace -> content-authoring -> vertical-slice.
- `content-authoring`: navigation -> content-authoring.
- `content-gap-review`: navigation -> content-authoring.
- `frontend-ux-change`: navigation -> frontend-ux-invariants -> frontend-ux-standards -> frontend-ux-decisions.
- `canonical-action-slice`: navigation -> current-goals -> invariant-trace -> testing -> capability-matrix -> content-authoring -> action-logic -> frontend-game-text -> vertical-slice.
- `sprint-wrapup`: navigation -> current-goals.

Traversal output should support plain text, Mermaid `.mmd`, and simple coverage metrics that show which compiled docs appear on common paths.

## Wishlist items intentionally deferred beyond MVP

These are valuable, but not required to prove viability:

- Rich duplicate-prose detection.
- Full documentation consolidation.
- Complete archive cleanup.
- Code-side `DOC:` anchors.
- Full Action Step generated support graph.
- Content scenario coverage graph.
- Frontend component-gallery pattern index.
- Manual-review checklist runner.
- External SadConsole documentation research trace.
- Semantic linting that classifies arbitrary paragraphs by lane.
- Automatic validation that `test_refs` match individual test method names.

## Testable outcomes before implementation

This tooling work should follow TDD. Before production implementation, add intentionally failing tests for at least these outcomes:

1. Markdown frontmatter parser reads scalar, list, empty-list, and empty-value fields from a fixture document.
2. Documentation compiler loads multiple fixture documents into a graph keyed by document ID.
3. Linter reports missing required metadata.
4. Linter reports duplicate document IDs.
5. Linter reports unresolved `related` references.
6. Linter reports a `source-of-truth` document missing `lane` or `read_when`.
7. Linter reports unknown owner/audience roles.
8. Linter accepts a small valid source-of-truth fixture set.
9. Read-path generation for `content-editor` starts with navigation and content-authoring lanes and excludes invariant-trace by default.
10. Read-path generation for `frontend-owner` includes frontend invariants, standards, and decisions in order.
11. Read-path generation for `core-owner` includes invariants, testing, capability matrix, and vertical-slice navigation.
12. Link/path validation reports a missing repository-relative Markdown or code reference in a fixture.

Invariant/test trace for this spike:

- Affected existing behavior invariant: None. This is new project tooling for documentation governance.
- Existing tests associated with affected invariants: None.
- New tests needed: documentation compiler parser, graph, lint, and read-path tests listed above.

## Done criteria

The spike is complete when:

1. The MVP metadata schema is implemented or deliberately adjusted with rationale.
2. The selected MVP documentation set has frontmatter metadata.
3. `lint` passes on the selected MVP documentation set.
4. Fixture tests prove lint failures for missing metadata, duplicate IDs, unresolved references, and source-of-truth lane/read-condition omissions.
5. The tool can print a simple graph of declared doc relationships.
6. The tool can print role-specific read paths for `core-owner`, `content-editor`, and `frontend-owner`.
7. The final handoff records which deferred wishlist items should be promoted next, if any.

## Risks and guardrails

- Do not make planning docs too heavy during the spike; strict rules apply first to source-of-truth docs.
- Do not let `code_refs` imply content authorability or frontend ownership of shared semantics.
- Do not turn generated indexes into competing sources of truth; generated output should point back to authoritative docs.
- Do not require every archived document to comply with current source-of-truth semantics.
- Do not remove or rewrite existing content YAML as part of this spike.
- Do not use metadata examples that revive removed Console or Avalonia workflows.
- Keep human-readable `Read when` / `Do not read when` prose where useful; frontmatter should make these rules machine-readable, not replace readable guidance.

## Spike friction log

- **No repository solution file was present.** The spike can still proceed by adding a standalone project/test-project pair and running `dotnet test` against the documentation test project directly. Mitigation: keep command examples project-file based unless a future workspace solution is introduced.
- **Frontmatter parsing is intentionally minimal for MVP.** Scalar fields, inline lists, block lists, empty lists, and empty scalar values are covered. Nested structures such as future `manual_review` objects or rich component-gallery metadata should be treated as deferred schema work. Mitigation: keep MVP optional fields flat or ignored until a richer parser/schema decision is made.
- **Markdown link linting can become noisy if applied to every historical document.** The current loader compiles only documents with frontmatter IDs, so archive/reference docs are not forced into current lint semantics until deliberately annotated. Mitigation: continue staged annotation and keep archive rules weaker than source-of-truth rules.
- **Traversal profiles are hardcoded and intentionally incomplete.** The first coverage metric showed that planning sample docs and `Entity-Panel-UX-Spec.md` have zero traversal coverage even though they are linked in the graph. Mitigation: treat traversal coverage as evidence for profile tuning; move profiles to configuration and add conditional task dimensions after the MVP proves useful.
- **Splitting current project direction out of `planning-index.md` required a new source-of-truth lane.** The documentation graph now has `source.current-goals` as a planning-priority bridge while `planning-index.md` stays focused on documentation lanes and governance. Mitigation: keep `Current-Goals.md` short and mostly stable, and tune traversal profiles rather than reintroducing roadmap prose into the navigation index.
