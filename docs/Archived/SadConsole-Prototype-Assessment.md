# SadConsole Prototype Assessment

Status: Completed / archived assessment checkpoint after the SadConsole entity-panel/action prototype spike. See `docs/Plans/SadConsole-Spike-Findings.md` for the consolidated findings to merge forward.

Read when:

- deciding whether to continue the SadConsole prototype;
- deciding whether SadConsole should become a serious final-engine candidate;
- planning the next frontend UX experiment after entity panels, breadcrumb panels, and first-pass action controls;
- comparing keyboard-first panel controls, mouse controls, and future engine requirements.

Related source of truth:

- `docs/Source of Truth/Frontend-UX-Invariants.md` records broad frontend UX invariants.
- `docs/Source of Truth/Entity-Panel-UX-Spec.md` records the entity-panel UX model.
- `docs/Archived/SadConsole-Entity-Panel-Prototype.md` records the implementation spike and turn-by-turn findings.
- `docs/Plans/SadConsole-Spike-Findings.md` records the end-of-spike architecture findings and recommended service splits.

## Current assessment

The SadConsole prototype should stop as an active spike and remain as reference/evidence. It should not be declared the final frontend engine.

The prototype has proven that SadConsole can host a useful debug-first frontend over existing simulation contracts. It has not yet proven that SadConsole is the best long-term engine for mouse-friendly panels, animation, rich editor UX, or polished RPG status-window presentation.

Immediate priority update: the spike is ended before more SadConsole-specific feature work. The next frontend planning step should focus on shared session/action/target/log service contracts before choosing whether to continue SadConsole, compare another engine, or keep Console as the supported demo surface.

## What the prototype has proven

- SadConsole can run in this repository as a standalone frontend prototype.
- Existing scenario materialization can feed the prototype without new simulation semantics.
- Direct scenario launch is sufficient for testing authored scenarios.
- `EntityInspectionPanel` maps naturally to SadConsole entity panels.
- A player's current playspace can be represented as an entity panel rather than as a special separate map surface.
- Breadcrumb paths can become a left-to-right panel chain.
- Panels can be expanded/collapsed enough to test longer containment chains.
- Focused-panel keyboard cursors allow inspecting entities in any rendered inventory panel.
- Existing Core actions can drive movement, pickup, drop, enter, and exit from the prototype.
- `LastTurnReport` can feed a universal log after player actions.

## What remains unproven

- Whether panel-chain layout remains readable with many panels, large grids, and long entity names.
- Whether a keyboard-only interaction model can be made comfortable without tracking too many independent cursors/focus states.
- Whether SadConsole mouse hit-testing and hover/click interactions are pleasant enough for this UX.
- Whether local per-panel logs can be displayed compactly without overwhelming entity panels.
- Whether a manifest/scenario menu can reuse the existing Console catalog concepts cleanly in SadConsole.
- Whether the current SadConsole/MonoGame DesktopGL host can produce an itch.io-playable browser/HTML5 build.
- Whether facing/target indicators can be shown clearly in-grid without clutter.
- Whether animation and RPG-style panel polish are comfortable in SadConsole.
- Whether future player-friendly editor tools are practical in SadConsole or would be better in Godot/another richer 2D UI stack.

## Current UX friction

The most important user-facing friction is not lack of capability; it is focus and cursor management.

The prototype currently separates:

- player movement through `WASD`;
- inspection cursor movement through arrow keys;
- panel focus through `Tab`;
- action selection through hotkeys such as `P`, `O`, `E`, and `X`.

This makes every action technically possible, but it can be disorienting because the player must remember which panel has focus while also moving the player in a different panel. Mouse controls could help, but keyboard-first play should remain viable if possible.

## Keyboard-first UX alternatives to test before relying on mouse

### Option 1: Player-centric default focus with inspect mode

Default mode:

- movement keys move the player;
- current playspace/player panel is implicitly focused;
- actions target obvious player-adjacent or player-carried contexts where possible.

Inspect mode:

- a key such as `I` enters panel/cursor navigation;
- `Tab` switches panels only in inspect mode;
- `Esc` exits inspect mode and returns focus to the player.

Pros:

- reduces the need to track focus during normal play;
- resembles Console's mode separation;
- preserves keyboard-only inspection.

Cons:

- mode switching must be visibly obvious;
- actions that use inspection selection need clear handoff from inspect mode to action mode.

### Option 2: Focus follows inspected panel, player movement is explicit command mode

Default mode:

- arrow keys move the focused panel cursor;
- Enter inspects;
- `M` or `WASD` with a modifier moves the player.

Pros:

- panel focus is always meaningful;
- inspecting nested panels feels primary.

Cons:

- normal movement becomes less immediate;
- likely worse for play than debug/authoring.

### Option 3: Action-first prompts

Pressing an action key starts a short prompt:

- `P`: choose pickup target from current playspace panel;
- `O`: choose carried item, then choose drop destination;
- `E`: choose enter target;
- `X`: choose exit direction.

During a prompt, focus is constrained to valid panels/cells and the UI explains what is being selected.

Pros:

- reduces accidental use of the wrong focused panel;
- scales to multi-step actions;
- works well with keyboard and later mouse.

Cons:

- requires explicit prompt/action state;
- adds implementation complexity before editor work.

### Option 4: Numbered visible panels and quick focus keys

Each visible panel has a number. Pressing `1`-`4` focuses a panel directly; `Tab` remains available.

Pros:

- less disorienting than repeated `Tab` cycling;
- easy to render and explain;
- still keyboard-only.

Cons:

- only works cleanly for the currently visible panel set;
- does not solve multi-step action targeting by itself.

### Option 5: Split play focus from inspect focus

Maintain two visible focus indicators:

- player/action focus: where player commands will act;
- inspect focus: which panel/cell will be inspected on Enter.

Pros:

- makes the current prototype's mental model explicit;
- allows action and inspection focus to diverge intentionally.

Cons:

- two focus indicators may be visually noisy;
- likely too complex for player-facing default UX.

## Mouse controls assessment

Mouse controls remain valuable, especially for:

- clicking any visible entity to inspect it;
- selecting action targets;
- expanding/collapsing panels;
- future editor UX.

However, mouse should be treated as a convenience layer over a coherent keyboard model, not as the only way to make panel focus understandable.

Before adding mouse controls, the prototype should centralize panel layout and grid-cell screen rectangles so hit-testing can reuse the same layout data as rendering.

## Technical debt assessment

The prototype is still useful, but it is past the point where feature work should be added without small structure improvements.

Current manageable debt:

- prototype-specific session state lives in one frontend project;
- direct scenario launch is intentionally minimal;
- rendering is simple SadConsole cell drawing;
- view models now isolate some formatting from drawing.

Debt that will compound soon:

- focus, selection, prompt/action-target state are not explicit enough;
- panel layout geometry is not centralized for mouse hit-testing;
- panel expand/collapse and cursor state are prototype-only and not yet designed as durable UX state;
- local logs need a real grouping/projection model before rendering;
- action controls are functional but not comfortable.

## Recommendation

Do not continue the SadConsole prototype by default before a frontend architecture plan addresses the shared-service splits recorded in `docs/Plans/SadConsole-Spike-Findings.md`.

Superseded recommendation: this section recorded the pre-close recommendation for a **shareable feedback build pass**. The current recommendation is to plan shared frontend-facing service contracts before more SadConsole feature work.

Suggested scope:

1. Add panel-specific log filters derived from the shared universal turn trace/local order reports, with an escape hatch to view the full chronological log.
2. Add a SadConsole scenario manifest/menu using the existing catalog/manifest concepts, while preserving direct launch for developer testing.
3. Investigate the HTML5/browser export path for SadConsole + MonoGame in this repository before committing to itch.io browser delivery.
4. If browser export is feasible, add a repeatable packaging command/pipeline for an itch.io browser upload.
5. If browser export is not feasible, document the blocker, produce the best downloadable fallback, and treat web delivery as a frontend-engine decision pressure.
6. Keep keyboard focus/action polish limited to what the feedback build needs; do not let it block scenario selection, local logs, or packaging.

Timebox rule: after these three explorations are implemented or documented as blocked, do not continue adding SadConsole features until the frontend-role decision is made.

Do not yet:

- declare SadConsole final;
- start a Godot comparison;
- implement full mouse interaction;
- implement integrated editor tools;
- add frontend-only simulation/reporting behavior to make the logs look better.

## Decision checkpoint after next slice

After the shareable feedback build pass, reassess:

- Can external testers select scenarios and understand the entity-panel UI without command-line setup?
- Do panel-specific logs improve comprehension compared with the universal log?
- Is an itch.io browser build feasible with SadConsole/MonoGame, or does web delivery require a different stack?
- Can keyboard-only play and inspection feel coherent enough for feedback?
- Is SadConsole layout/focus state still manageable?
- Are mouse controls now a straightforward convenience layer?
- Are we confident enough to continue SadConsole toward a real frontend, or should we prototype the same UX in Godot before committing?

Decision outcomes:

- **Replace Console as debug/prototype frontend** if SadConsole can cover both tester-facing and developer-debug workflows cleanly.
- **Keep SadConsole alongside Console as main shareable frontend** if SadConsole is best for feedback but Console remains valuable as the smallest robust debug path.
- **Archive as postmortem/R&D** if SadConsole's UX lessons are useful but delivery constraints, especially browser packaging, point toward another frontend.

## Keyboard-first focus/action UX pass findings

- Added explicit input modes to the prototype: `Play`, `Inspect`, `PickupTarget`, `DropItem`, `DropDestination`, `EnterTarget`, and `ExitDirection`.
- Play mode is now player-centric: WASD moves the player, while panel focus/cursor controls are inactive unless the player enters inspect/action selection.
- `I` enters Inspect mode, where Tab/arrows/Enter/Space operate on panels and cells.
- `P`, `O`, `E`, and `X` enter action prompt modes instead of immediately acting on whichever panel happened to be focused.
- `Esc` cancels non-play modes and returns to Play; in Play it exits the prototype.
- This should reduce the main focus confusion by making panel cursor movement temporary and purposeful rather than always active.
- Remaining roughness: prompts are still first-pass and only lightly constrain valid panels/cells. Future work should add valid-target highlighting/skipping and clearer prompt text near the relevant panels.
