---
id: source.testing-charter
title: Testing Charter
purpose: TDD workflow and testing expectations for semantic Core Content Editor and frontend behavior changes.
summary: Testing policy for planned semantic changes, invariant traces, test placement, content tests, and frontend boundary coverage.
kind: source-of-truth
subkind: testing-charter
status: active
owners: [core-owner, frontend-owner]
audience: [core-owner, frontend-owner, content-editor]
lane: testing
truth_rank: 15
truth_domains: [testing-policy, test-trace]
read_when:
  - planning semantic Core Content or Editor code changes
  - planning stable frontend behavior tests
related:
  - source.invariants
  - source.frontend-ux-invariants
---
# Testing Charter

Tests follow the same architectural split as `src`.

## TDD Workflow For Planned Semantic Changes

Every planned Core/Content/Editor semantic code change must have at least one testable outcome before implementation begins. If a semantic change cannot be described in testable terms, it is not ready for implementation.

For this semantic workflow, write intentionally failing tests for the planned behavior before changing production code. These tests should be the first executable expression of the desired behavior and should fail for the expected reason before implementation starts.

If the plan changes existing behavior, the plan must include an invariant/test trace before implementation:

- affected invariant or invariants from `docs/Source of Truth/invariants.md`, or `None` if no invariant is affected;
- existing tests associated with those invariants;
- which existing tests should be revised to become failing tests for the new behavior;
- any new tests needed in addition to revised existing tests.

If this trace is not listed in the plan, the change is not ready for implementation.

At implementation time, the implementing agent should first make the plan ready for implementation by confirming testable outcomes and invariant/test trace. Then it should review the traced existing tests, revise them where appropriate, and add new tests only where needed. Production code changes should follow after the intentionally failing tests are in place.

The expected loop is:

1. Confirm the planned behavior has testable outcomes.
2. Trace affected invariants and existing tests, or explicitly record `None`.
3. Write or revise tests so they intentionally fail for the planned behavior.
4. Implement the smallest coordinated Core/Content/Editor semantic change that makes the tests pass.
5. Run the targeted tests and relevant broader suites.
6. Update capability, invariant, and planning docs when behavior or support status changes.

## Frontend Testing Workflow

Frontend work follows the same architectural split, but does not require strict red/green TDD for every exploratory UI change.

Stable pure frontend logic should receive automated tests once it is extracted or shaped into a testable seam. This includes layout slots, panel-chain visibility and titles, prompt candidate filtering/cycling, local activity row composition, menu/session summaries, log labels, and future hit-test geometry.

Exploratory UX work may be implemented and manually evaluated before tests pin it. This includes visual density, wording, colors, animation, cursor feel, mouse comfort, and other presentation choices that are still expected to churn.

Frontend tests must not duplicate Core legality, materialization, turn advancement, inventory, containment, action-resolution, or durable content semantics. If a frontend behavior depends on those facts, test or extend the owning Core/Content/Editor service instead and keep frontend tests focused on how existing shared facts are presented or selected.

Frontend plans should trace affected constraints in `docs/Source of Truth/Frontend-UX-Invariants.md` when changing stable presentation, layout, prompt, log, or frontend-boundary behavior. If the affected behavior is still exploratory, the plan should say which parts are manual-review-only and which parts should receive automated tests once the view-model seam stabilizes.

## Lightweight Frontend Test Standard

Frontend tests should stay light while components and interaction patterns are still being prototyped.

Use suites as the normal frontend trace unit. Invariant docs may trace to component or helper suites such as `SadConsolePanelChainViewTests`, `PromptChoiceCyclerTests`, or `LocalActivityViewBuilderTests` instead of listing every individual case. Individual test names are useful when a single behavior is especially important or when the suite name would be ambiguous.

Use test-count thresholds as review triggers, not hard quotas. As a guideline, a stable frontend invariant should usually trace to one or two focused suites or a small number of representative tests. If an invariant starts accumulating many frontend tests, review whether the tests are too detailed, whether they belong in a component suite, whether the invariant should be split, or whether the behavior actually belongs in Core/Content/Editor.

Avoid testing every visual element or wording choice. Prefer representative tests that protect the component contract: ordering policy, collapse policy, empty-state honesty, role labels, candidate filtering, wrapping behavior, clipping/layout bounds, and whether shared facts are consumed without redefining them.

Do not add snapshot or cell-perfect tests by default. Add lightweight text snapshots only after the view-builder output is stable enough that snapshots catch real regressions instead of expected UX iteration. Do not launch the real SadConsole window in automated tests unless a later frontend automation plan explicitly promotes that approach.

For a new frontend component, use this sequence:

1. Create stub or placeholder tests only when they clarify the intended seam, component boundary, or still-open questions.
2. Explore the component through implementation, manual testing, and refinement without pinning every intermediate shape.
3. Once the component shape stabilizes, decide which existing or new frontend invariants define it, then add focused tests that trace to those invariants.
4. When editing an existing stable component, trace the existing component suite and affected frontend invariants before changing behavior.

Frontend stub tests must not become permanent noise. Delete, replace, or complete them when the component is either stabilized, deferred, or abandoned.

Current frontend test projects:

- `tests/GameGameGame.Frontend.SadConsole.Tests` for the new clean SadConsole frontend surface. This project should establish pure settings, display/drawable-bounds, scenario-browser, component-gallery, input-routing, and Play-surface view-model tests before renderer/input adapters are pinned.
- `tests/GameGameGame.SadConsole.Tests` for the legacy/reference SadConsole project only when it is explicitly built or mined outside default solution workflows.

Archived strategy reference:

- `docs/Archived/Frontend-Testing-Strategy-Proposal.md`

## Core Tests

Core tests cover engine behavior that content is allowed to reference.

They are responsible for action primitives, movement, inventory interactions, Bulk/Aperture rules, action plan interpretation, traces, and turn resolution. Core tests should use controlled test fixtures and should not depend on prototype content values.

## Content Tests

Content tests cover the integration pipeline for content.

They are responsible for YAML loading, editable document roundtrips, editor services, registry validation, and broad validation of content infrastructure. Existing authored content is transient prototype/test data unless explicitly promoted as stable shipping content; do not add or keep tests for specific transient content scenarios. Content tests may assert values from inline test fixtures or explicit edits made by the test, but should not pin prototype content choices such as exact balance values, glyphs, positions, scenario choreography, final coordinates, or action plan behavior.

## SadConsole Tests

SadConsole tests cover frontend-owned presentation and interaction-model logic without launching the real SadConsole window or asserting cell-perfect rendering.

They are responsible for pure layout geometry, panel-chain selection, collapsed-card policy, prompt candidate filtering and cycling, local activity presentation rows, and future stable view-builder or hit-test behavior. Legacy/reference-only seams may still use `InternalsVisibleTo("GameGameGame.SadConsole.Tests")` while mined explicitly, but normal maintained frontend seams should use the new frontend test assembly rather than making test seams public API prematurely.

For `GameGameGame.Frontend.SadConsole.Tests`, prefer the same standard with the new assembly name. The first test trace should cover settings defaults, display/drawable-bounds resolution, workspace scenario browser view models, selection/request mapping, diagnostic display data, component-gallery screen models, and Play-surface layout over shared session/projection DTOs. These tests must not duplicate workspace composition, materialization, player insertion, action legality, turn advancement, command outcomes, diagnostics classification, or YAML mutation semantics.

Manual SadConsole smoke testing remains required for frontend sprints until richer UI automation is promoted. Representative manual checks should include launching the default catalog or manifest, selecting representative scenarios, verifying panel chains and collapsed cards, exercising inspect/action cycling, checking local activity readability, confirming glyph identity is preserved, and confirming limited logs are honestly labeled.
