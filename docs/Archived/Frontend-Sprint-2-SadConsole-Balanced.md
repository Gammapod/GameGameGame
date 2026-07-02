# Frontend Sprint 2: SadConsole Balanced Simulation UX

Status: Completed / archived implementation plan.

Read when:

- implementing the second frontend sprint for `src/GameGameGame.SadConsole`;
- selecting work around SadConsole Simulation-mode layout, panel chains, and prompt UX;
- deciding what is in scope before starting Editor-mode preview work.

Related source of truth:

- `docs/Plans/SadConsole-Frontend-Roadmap.md` is the staged frontend roadmap.
- `docs/Source of Truth/Frontend-UX-Invariants.md` defines frontend/shared-service boundaries.
- `docs/Source of Truth/Frontend-UX-Standards.md` defines entity-neutral presentation, glyph, log, and prompt standards.
- `docs/Source of Truth/Entity-Panel-UX-Spec.md` defines the entity-panel, breadcrumb, and log model.

## Sprint goal

Make the current SadConsole Simulation-mode debug browser more structurally durable and more useful for scenario inspection without adding frontend-owned simulation semantics.

The sprint intentionally focuses on Simulation-mode foundations before broad Editor-mode UI. It should leave the codebase better prepared for later mouse hit-testing, Editor -> Preview -> Simulation flow, and source-jump work.

## Selected scope

### Slice 1: View-model and layout extraction

Goal: separate frontend presentation models and reusable layout geometry from direct shell rendering.

Scope:

1. Introduce small SadConsole-owned view/layout types for the current session screen.
2. Keep shared facts sourced from `EntityPanelProjectionService`, `ControlledActorAffordanceService`, and `ActionLogProjection`.
3. Preserve current behavior and key bindings while reducing the amount of projection/layout assembly inside `SadConsoleShell`.
4. Avoid building durable gameplay/session semantics in the frontend view model.

Exit criteria:

- SadConsole still launches scenarios, plays, inspects, pickup/drop, enter/exit, and renders current two-panel layout.
- Panel rectangle information is centralized enough to seed the panel-chain and later mouse-hit-test slices.
- Targeted SadConsole build succeeds, preferably using a temporary output path when interactive binaries may be locked.

### Slice 2: Breadcrumb panel-chain renderer

Goal: move from fixed two-panel rendering toward the canonical breadcrumb-as-panel-chain model.

Scope:

1. Build visible panels from the inspected entity's containment breadcrumb where practical.
2. Render ancestor/current panels left-to-right using the extracted layout/view-model layer.
3. Add compact/collapsed cards for long chains or narrow layouts.
4. Keep breadcrumb navigation read-only for this sprint.

Exit criteria:

- Inspecting a nested entity can show its containment context as panel cards/panels.
- Current container/control context remains visibly distinguishable.
- Long chains degrade through collapsed cards rather than unreadable full panels.

### Slice 3: Valid-choice cycling prompts

Goal: make keyboard action prompts favor valid affordance choices instead of requiring arbitrary cursor movement over mostly invalid cells.

Scope:

1. For pickup source/destination, drop source/destination, enter target, and exit direction prompts, allow cycling through valid candidates.
2. Keep final command execution through `ControlledActorCommandService`.
3. Keep invalid/blocked affordance highlighting visible for debug context where available.
4. Show prompt text that explains the selected valid choice and representative blocked reasons.

Exit criteria:

- The default keyboard flow can choose valid targets with fewer cursor movements.
- Affordance hints remain non-authoritative; command execution remains shared/Core-owned.
- Invalid selections remain explainable without frontend-owned legality rules.

## Stretch scope

If the three selected slices complete cleanly, consider a small local-activity presentation pass:

- merge visible contents rows and local controlled-command snippets into a more activity-like section;
- label limitations honestly while autonomous turn outcome projection remains shared-service follow-up work.

## Explicit non-goals

- No broad Editor-mode mutation UI.
- No live hot-editing while Simulation continues.
- No runtime debug mutation primitives.
- No frontend-only action legality, materialization, provenance, or autonomous log semantics.
- No mouse interaction before layout geometry exists.
- No claim that controlled-command logs are complete autonomous simulation history.

## Verification

Targeted build command:

```powershell
dotnet build "src\GameGameGame.SadConsole\GameGameGame.SadConsole.csproj" -p:OutputPath="C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-sadconsole-build"
```

Manual smoke path when an interactive window is available:

1. Launch SadConsole with the default catalog/manifest.
2. Select a scenario and press Enter.
3. Move, inspect, pickup/drop where applicable, enter/exit where applicable.
4. Confirm glyphs remain identity glyphs and state appears through adjacent/text/decorator-style presentation.
5. Confirm logs remain honestly labeled as controlled-command logs until broader shared projection exists.

## Open questions to record during sprint

- Does the extracted layout model have enough geometry for later mouse hit-testing without over-designing now?
- How many panels can remain readable before horizontal scrolling or stronger collapse policy is needed?
- Does valid-choice cycling reduce prompt confusion enough to defer mouse controls?
- Does SadConsole reveal any missing shared projection data that should be coordinated with core-owner?

## Progress log

- Slice 1 started: added small SadConsole-owned session/panel/log view models plus centralized two-panel/global-log rectangles. `SadConsoleShell` now builds a session render view before drawing, while still sourcing facts from shared projection, affordance, and log services. Behavior is intended to remain unchanged for the first extraction step.
- Slice 2 started: session view construction now builds visible panels from the inspected entity's containment breadcrumb path. The first pass renders up to four panels left-to-right; chains longer than four keep the root as a collapsed card and show the last three ancestor/inspection panels. Breadcrumb navigation remains read-only.
- Slice 3 started: action prompt modes now preselect first valid choices where available and support `Tab` cycling through valid pickup sources/destinations, drop sources/destinations, enter targets, and exit directions. Arrow-key cursor movement remains available for debug/arbitrary-cell selection, and command execution remains routed through `ControlledActorCommandService`.
- Prompt polish: Inspect mode now highlights visible occupied cells in the current container as inspectable navigation targets and supports `Tab` cycling through those visible entities. This remains frontend navigation over runtime/projection facts, not Core action-target legality.
- Deferred prompt polish: no-valid-target prompt suppression remains backlog/future work for more granular UX testing; do not add it to this sprint unless explicitly reselected.
- Stretch slice completed: panel rendering now presents a single `Local activity` section instead of separate `Contents` and `Local log` sections. Contents rows are shown in projected order with indented previous-action/controlled-command snippets, and remaining local controlled-command snippets are shown underneath when space allows. This remains an honest presentation pass over current projection data; broader autonomous turn outcome projection is still shared-service follow-up work.

## Feedback triage

User feedback after the first valid-choice cycling MVP identified several follow-ups. Triage:

- **Valid inspection target highlighting/cycling:** related to this sprint's prompt-cycling work and Stage 7 focus/selection UX. Small enough to consider as an in-sprint polish item if we choose another usability slice before local-activity consolidation. It remains frontend-owned because inspection is presentation/navigation state over already-visible projected entities.
- **Arbitrary future action-step target discovery, including targets on other inventory planes:** already belongs to the broader Stage 9 Action Choice / `PlayerInputStep` direction and likely needs Core-owned target/choice contracts beyond current direct-control affordances. Do not implement in this sprint as a SadConsole-only rule system.
- **Blinking gold cursor/highlight:** already promoted by `Frontend-UX-Standards.md` as a desired cursor/focus follow-up. Defer unless we explicitly select a small animation/style polish slice; it is frontend-owned but not necessary for the current structural sprint.
- **Do not enter action selection modes when no valid targets exist:** related to this sprint's valid-choice prompt work. Small enough to consider as in-sprint polish for current direct-control prompts, using existing affordance data and without changing command legality. Keep final command execution authoritative.
