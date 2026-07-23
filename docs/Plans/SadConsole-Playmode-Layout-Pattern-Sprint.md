---
id: plan.sadconsole-playmode-layout-pattern-sprint
title: SadConsole Play-Mode Layout Pattern Sprint
kind: sprint-plan
status: completed
truth_rank: 50
truth_domains: [planning-priority, frontend-presentation]
owners: [frontend-owner]
audience: [frontend-owner]
read_when:
  - implementing low-risk SadConsole play-mode layout pattern experiments
  - preparing the componentized play-mode rewrite
related:
  - plan.sadconsole-ui-specification
  - plan.sadconsole-frontend-roadmap
  - source.frontend-ux-invariants
  - source.frontend-ux-standards
  - source.frontend-ux-decisions
---

# SadConsole Play-Mode Layout Pattern Sprint

Status: Completed focused sprint. This is a learning/architecture-prep slice for the existing mock play-mode surface, not the full componentized play-mode rewrite.

## Goal

Use `GameplayMockScreen` / `GameplayMockConsole` to prove the first reusable layout-pattern seams from `docs/Plans/SadConsole-UI-Specification.md` with minimal behavior churn.

The sprint should create experience with named regions, layered regions, overlay placement, debug visibility, and pure layout tests before replacing the mock play-mode architecture.

## Non-goals

- Do not rewrite play mode.
- Do not change Core action legality, Action Choice semantics, turn advancement, logs, materialization, or editor/content mutation rules.
- Do not implement full user-customizable layouts.
- Do not implement fully responsive window resizing unless the stretch spike proves it is trivial.
- Do not replace all rendering with child `ScreenObject`/`ScreenSurface` architecture yet.
- Do not add real mouse action submission in this sprint.

## Current baseline

- `GameplayMockScreen.BuildFrame(width, height)` now consumes `GameplayMockLayout.Resolve(width, height)` for HUD/current-place/inspection/action-selector/diagnostics bounds.
- `GameplayMockFrame` exposes compatibility bounds plus named layered `GameplayMockRegion` data.
- `GameplayMockConsole` still draws current-place and inspection panels directly, uses a child `Console` HUD layer, and lets `SadConsoleComponentRenderer` draw overlay-ish components.
- Existing tests in `GameplayMockScreenTests` and `GameplayMockLayoutTests` assert the current high-level layout shape, split rounding/min-size behavior, debug rows, hit-testing, and manual recalculation seam.

## Completed result

- Added `GameplayMockLayout.Resolve(width, height)` as a pure tested mock play-mode layout resolver.
- Added named layered regions: `0`, `0.1`, `0.2`, `0.3`, `0.2.1`, and `0.diagnostics`.
- Routed action selector and diagnostics overlay placement through layout-owned bounds while preserving current visible behavior.
- Added small mock-specific ratio split helpers for HUD/content and current-place/inspection layout, including tests for floor rounding and minimum sizes.
- Added hidden-by-default `F12` layout debug overlay with region IDs, bounds, layers, manual recalculation status, and live mouse-hover hit-test output.
- Added pure topmost-region hit testing with local coordinates; no click/action submission behavior was added.
- Added `F11` logical-console layout re-resolve/redraw seam. It reports fixed SadConsole logical cell dimensions; true OS-window-to-cell resizing remains future spike work.

## Must Have scope

### 1. Extract current bounds into a pure mock layout resolver

Create a small frontend-owned helper, for example `GameplayMockLayout.Resolve(width, height)`, that returns the current layout bounds without changing the visible default layout.

Expected regions in the first resolver:

- `0`: play-mode screen/root viewport;
- `0.1`: HUD/status;
- `0.2`: current-place panel;
- `0.3`: inspection/player-inventory panel;
- `0.2.1`: action selector overlay region;
- `0.diagnostics`: diagnostics overlay/panel region when needed.

Acceptance notes:

- Preserve current minimum viewport behavior unless a bug is discovered.
- Keep the resolver pure and testable without launching SadConsole.
- Add focused tests for the default 120x42 viewport and at least one smaller/larger viewport.

### 2. Add named layered regions to the mock frame

Introduce a lightweight resolved-region model, for example:

```csharp
internal sealed record GameplayMockRegion(string Id, string Title, SadConsoleRect Bounds, int Layer);
```

or a similarly named type.

Acceptance notes:

- `GameplayMockFrame` should carry the resolved regions in addition to compatibility bounds while callers are migrated.
- Existing `HudBounds`, `CurrentPlaceBounds`, and `InspectionBounds` may remain as facade properties/fields for narrow diffs.
- Layers should be explicit even if rendering still mostly happens in current order.

### 3. Route the action selector through an explicit overlay region

Move action selector bounds to the layout resolver instead of deriving them ad hoc from `currentPlaceBounds` at component construction time.

Acceptance notes:

- The action selector remains functionally identical.
- The selector region should have a higher layer than current-place and HUD/inspection content.
- Tests should assert that `0.2.1` exists only/especially when the selector is active, or that it is resolved consistently and only rendered when active. Choose whichever is simpler for current code.

### 4. Add a layout debug panel/toggle

Add a mock play-mode debug surface that displays the resolved region IDs, bounds, and layers.

Example rows:

```text
0.1 HUD        L0 T0 W24 H42 Z2
0.2 Place      L25 T0 W94 H28 Z1
0.3 Inspect    L25 T28 W94 H14 Z1
0.2.1 Actions  L30 T3 W40 H8 Z10
```

Acceptance notes:

- The debug display is frontend-only presentation state.
- It should be hidden by default and toggled through a clearly documented debug key or startup/test seam.
- It must not obscure normal play unless intentionally toggled.
- Add tests for the debug rows at the screen-model/component level where practical.

### 5. Prototype relative splits for the current layout only

The first resolver may be implementation-specific, but it should express at least the current HUD/content/inspection split in a way that can evolve toward UI-M01/UI-M02.

Acceptance notes:

- Prefer small internal concepts such as percent/ratio split, gap, min width/height, and layer over a broad reusable layout language.
- Record any rounding/min-size decision in a short comment or test name.
- Avoid building a general declarative layout DSL in this sprint.

## Stretch goals

### S1. Mouse hover/click hit-test diagnostics only

Add a non-mutating diagnostic path that identifies the current mouse-hovered region and, where applicable, region-local cell coordinate.

Acceptance notes:

- No action submission, no selection mutation, and no gameplay changes.
- This should exercise the resolved-region tree's hit-test order, especially topmost layer wins.
- If SadConsole mouse plumbing takes longer than expected, stop at a pure hit-test helper plus tests.

### S2. Manual “recalculate layout” command

Add a manual command path that rebuilds/resolves layout from the current console `Width`/`Height` and redraws.

Acceptance notes:

- This does not need true dynamic window resizing.
- If current root console dimensions are fixed, the command can still be useful as a proof of redraw/re-resolve plumbing and debug message.

## Suggested implementation order

1. Add `GameplayMockLayout` and focused tests that reproduce current bounds.
2. Add `GameplayMockRegion` / resolved-region collection and migrate `BuildFrame` to consume the resolver.
3. Keep compatibility bounds on `GameplayMockFrame` to reduce renderer churn.
4. Move action selector and diagnostics panel placement to named regions.
5. Add debug-region rows and a toggle/state seam.
6. Add small relative split concepts only as needed to describe the current layout.
7. Attempt stretch hit-test helper before SadConsole live mouse plumbing.
8. Attempt manual recalculate command if the redraw plumbing is still simple.

## Test plan

- Add or update `GameplayMockScreenTests` for:
  - default 120x42 resolved regions;
  - smaller/larger viewport behavior;
  - stable layer ordering;
  - action selector overlay region placement;
  - debug layout rows.
- Add a dedicated `GameplayMockLayoutTests` suite if the resolver becomes non-trivial.
- Run `dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj` before declaring the sprint complete.
- Run broader tests only if shared contracts are touched, which this sprint should avoid.

## Documentation updates when complete

- Update `docs/Plans/SadConsole-UI-Specification.md` current project baseline with the implemented resolver/region model.
- If a pattern is accepted as reusable, update `docs/Source of Truth/Frontend-UX-Decisions.md` or the component gallery notes with the accepted project pattern.
- If stretch mouse diagnostics land, update UI-N05 planning status.

## Sprint findings log

- **2026-07-23 checkpoint 1 friction:** The existing action selector bounds are not purely viewport-derived because height depends on the current number of action steps.
- **Mitigation used:** `GameplayMockLayout.Resolve(width, height)` exposes a stable named `0.2.1` overlay region for layout/layer inspection, and `GameplayMockLayout.ResolveActionSelectorBounds(layout, itemCount)` preserves the exact current component height when the selector is rendered.
- **2026-07-23 checkpoint 2 friction:** The debug region overlay can collide with existing POV/setup diagnostics because both are intentionally frontend-only overlay panels.
- **Mitigation used:** Keep the layout debug panel hidden by default, toggle it explicitly with `F12`, and render it after setup diagnostics so the requested debug surface wins only when intentionally enabled.
- **2026-07-23 checkpoint 3 friction:** The current layout has an asymmetric right-edge gutter and minimum-size behavior that does not map cleanly to a general split abstraction.
- **Mitigation used:** Keep the first split primitives narrow and mock-specific (`SplitHorizontal` / `SplitVertical`) with explicit gap, min-size, and rounding tests instead of introducing a broad reusable layout DSL.
- **2026-07-23 checkpoint 4 friction:** The project had no existing SadConsole mouse plumbing, so live hover diagnostics risked becoming a larger input feature.
- **Mitigation used:** Limit mouse support to `UseMouse` plus `ProcessMouse` cell capture while layout debug is visible; the pure hit-test helper remains non-mutating and no click/action submission behavior was added.
- **2026-07-23 checkpoint 5 friction:** The current root console dimensions are fixed at startup, and normal redraws already resolve layout from `Width`/`Height`, so a manual recalculation command has little runtime effect today.
- **Mitigation used:** Wire `F11` as an explicit re-resolve/redraw seam that reports the current console dimensions and records the latest manual recalculation in the debug panel, without pretending to support dynamic window resizing yet.
- **2026-07-23 checkpoint 5 feedback:** The first `F11` message was easy to misread as measuring the OS window size; user testing confirmed it always reported `120x42` after window resize.
- **Mitigation used:** Update the command and debug-row wording to say “logical console” and explicitly note that window pixel resizing does not change cell dimensions yet.

## Exit criteria

- Current mock play-mode behavior is preserved.
- Layout bounds are resolved through a pure helper rather than inline in `BuildFrame`.
- The frame exposes named/layered regions aligned with the UI specification vocabulary.
- The action selector uses an explicit overlay region.
- A hidden-by-default layout debug display can show region IDs, bounds, and layers.
- Tests cover the resolver and debug rows without launching the real SadConsole window.
