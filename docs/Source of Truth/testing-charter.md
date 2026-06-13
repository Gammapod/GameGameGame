# Testing Charter

Tests follow the same architectural split as `src`.

## Core Tests

Core tests cover engine behavior that content is allowed to reference.

They are responsible for action primitives, movement, inventory interactions, weight/capacity rules, action plan interpretation, traces, and turn resolution. Core tests should use controlled test fixtures and should not depend on prototype content values.

## Content Tests

Content tests cover the integration pipeline for content.

They are responsible for YAML loading, editable document roundtrips, editor services, registry validation, and broad validation of built-in content. Content tests may assert values from inline test fixtures or explicit edits made by the test, but should not pin valid prototype content choices such as exact balance values, glyphs, positions, or action plan behavior.

## Console Tests

Console tests cover frontend behavior.

They are responsible for input handling, rendering-facing behavior, and user workflows. Console tests are currently deprioritized, but future tests should stay focused on console behavior rather than re-testing Core or Content internals.
