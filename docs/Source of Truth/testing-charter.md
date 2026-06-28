# Testing Charter

Tests follow the same architectural split as `src`.

## TDD Workflow For Planned Code Changes

Every planned code change must have at least one testable outcome before implementation begins. If a change cannot be described in testable terms, it is not ready for implementation.

Before changing production code, write intentionally failing tests for the planned behavior. These tests should be the first executable expression of the desired behavior and should fail for the expected reason before implementation starts.

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
4. Implement the smallest coordinated Core/Content/Editor change that makes the tests pass.
5. Run the targeted tests and relevant broader suites.
6. Update capability, invariant, and planning docs when behavior or support status changes.

## Core Tests

Core tests cover engine behavior that content is allowed to reference.

They are responsible for action primitives, movement, inventory interactions, Bulk/Aperture rules, action plan interpretation, traces, and turn resolution. Core tests should use controlled test fixtures and should not depend on prototype content values.

## Content Tests

Content tests cover the integration pipeline for content.

They are responsible for YAML loading, editable document roundtrips, editor services, registry validation, and broad validation of built-in content. Content tests may assert values from inline test fixtures or explicit edits made by the test, but should not pin valid prototype content choices such as exact balance values, glyphs, positions, or action plan behavior.

## Console Tests

Console tests cover frontend behavior.

They are responsible for input handling, rendering-facing behavior, and user workflows. Console tests are currently deprioritized, but future tests should stay focused on console behavior rather than re-testing Core or Content internals.
