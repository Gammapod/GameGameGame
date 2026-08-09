---
id: plan.play-mode-interaction-sprint
title: Play Mode Interaction Sprint Plan
kind: plan
subkind: sprint-plan
status: archived
owners: [frontend-owner]
audience: [frontend-owner, core-owner]
lane: frontend-ux
truth_rank: 55
truth_domains: [planning-priority, frontend-presentation]
read_when:
  - implementing playable input and action prompts in the consumer-facing SadConsole Play mode
  - planning frontend intent resolution over canonical action choices
  - testing new Play mode with the canonical debug size-calibration room
related:
  - source.frontend-ux-invariants
  - source.frontend-ux-standards
  - source.frontend-ux-decisions
  - source.action-step-outcome-and-affordance-logic
  - source.sadconsole-ui-specification
  - plan.canonical-actions-vertical-slice
  - plan.new-play-mode-mvp-sprint
---

# Play Mode Interaction Sprint Plan

Status: Completed focused sprint plan. New consumer-facing SadConsole Play mode is now playable through abstract input intent resolution, contextual prompts, and canonical Action Choice consumption for the current canonical options in the size-calibration testbed.

## Sprint goal

Make the new consumer-facing SadConsole Play mode playable through abstract intent resolution, using the canonical debug size-calibration scenario as the primary manual/test fixture.

By the end of the sprint, the player should be able to perform the current canonical player-facing options from new Play mode without relying on the old Debug-mode menu workflow:

- `Move`;
- `TransformAdjacentToInventory` / Pickup;
- `TransformInventoryToAdjacent` / Drop;
- `EnterTarget`;
- `ExitFacing`;
- `Transfer`.

The UX is intentionally provisional. The durable goal is the architecture:

> Inputs produce abstract intent seeds. Shared services expand those seeds into action candidates or nested choice requests. Play mode auto-submits when the remaining choice is unambiguous; otherwise it shows a contextual prompt stack that asks only for unresolved information.

## Primary constraint

The player should never be asked for information the engine/shared services can infer.

Play mode should ask only for unresolved ambiguity, such as:

- which target;
- which carried item/source;
- which counterparty;
- which direction;
- which destination.

If shared facts leave exactly one valid inferred choice, Play mode should submit that choice instead of opening a confirmation prompt.

## Non-goals

- Do not define the final roguelike UX.
- Do not make all interactions menu-first.
- Do not add SadConsole-owned action legality, failure policy, turn-consumption policy, containment rules, or inventory semantics.
- Do not duplicate Core Action Choice or affordance rules.
- Do not implement mouse interaction unless it falls out cheaply from the same candidate model.
- Do not polish animation, facing, target, or decorator visuals beyond what is needed to understand the current prompt/selection.

## Architecture direction

### 1. Intent seed layer

Frontend inputs create abstract intent seeds rather than directly opening action-specific menus. Example seed vocabulary:

```text
MoveDirection(direction)
DefaultAction
ContextDirection(direction)
ContextCell(coord)
ContextEntity(entityId)
RequestedVerb(Pickup / Drop / Enter / Exit / Transfer)
Cancel
Select
```

This keeps multiple UX styles possible later: immediate movement, bump-to-context, default-action hotkeys, explicit action selectors, and future mouse click target-first interaction.

### 2. Candidate resolution layer

A Play-mode adapter asks shared Core-aware services for possible action candidates or nested Action Choice requests from the current actor and intent seed.

Conceptual candidate data includes:

```text
ActionCandidate
- action kind / authored step
- actor
- target/source/destination anchors, when known
- remaining unresolved choice kind, if any
- validity
- blocked reason / explanation
- display label
- default rank / priority
```

If existing Action Choice services already expose the required facts, SadConsole should adapt those. If not, this is a Core-owner coordination seam, not a reason to duplicate legality in the frontend.

### 3. Frontend policy layer

SadConsole owns only presentation/input policy:

```text
0 valid candidates -> show a short explanation
1 complete valid candidate -> submit immediately
multiple valid candidates -> show a contextual popup
1 incomplete candidate -> push the next prompt layer
multiple incomplete candidates -> ask only the ambiguous layer
```

### 4. Generic prompt stack

Prompts should be generic rather than hardcoded as `PickupMenu`, `DropMenu`, and so on.

```text
PromptLayer
- title
- choices
- focused index
- select behavior: refine or submit
- cancel behavior: pop
```

`Esc` / Cancel always unwinds the prompt stack without mutating simulation state.

## Milestone 1: Immediate movement

Goal: directional movement works in new Play mode without menus.

Work:

- Map movement keys to `MoveDirection` seeds.
- Submit movement through existing shared movement/action services.
- Refresh the current-space view after success/failure.
- Preserve direct movement as a low-friction player path.

Acceptance criteria:

- In `canonical-debug-size-calibration-room`, the player can move around the current space from new Play mode.
- Failed moves do not advance or mutate through frontend-owned semantics.
- Movement does not open a menu.

## Milestone 2: Play-mode intent resolver façade

Goal: create the durable seam before adding many workflows.

Work:

- Add a Play-mode interaction controller/service in SadConsole.
- Receive input intent seeds.
- Ask shared services for action candidates or nested choice requests.
- Apply frontend policy: auto-submit, prompt, or explain.
- Add focused tests around this policy using fake/shared candidate data.

Acceptance criteria:

- Tests prove that one complete valid candidate auto-submits.
- Tests prove that multiple valid candidates open a prompt.
- Tests prove that no valid candidates show an explanation.
- Tests prove that Cancel unwinds prompts.
- Tests assert frontend policy behavior without encoding action legality.

## Milestone 3: Default action key

Goal: `Enter` or another primary key performs the obvious action when there is one.

Initial default policy:

1. If exactly one complete obvious action is available, submit it.
2. If one action kind is obvious but needs one ambiguous choice, ask only for that choice.
3. If multiple plausible actions exist, open a context popup.
4. If none exist, show a short "Nothing obvious to do" style explanation.

Likely early mappings:

- adjacent portable entity -> Pickup;
- carried entity plus adjacent open destination -> Drop when requested through the Drop/default policy;
- adjacent enterable container/room -> Enter;
- currently contained actor with valid exit -> Exit;
- adjacent counterparty with inventory relationship -> Transfer.

Acceptance criteria:

- The default action can pick up a single unambiguous adjacent portable entity in the size-calibration scenario.
- The default action opens a chooser instead of guessing when multiple candidates exist.
- The default action does not ask for a destination when shared services infer the only valid destination.

## Milestone 4: Context/bump action path

Goal: bumping or focusing a valid target can open or perform actions against that target.

Work:

- Treat blocked movement or explicit context direction as `ContextDirection(direction)`.
- Resolve the target/counterparty/entity from shared/current-space facts.
- Ask shared action services what can be done with that target.
- Auto-submit if one complete valid action exists; otherwise show a context popup.

Acceptance criteria:

- Context/bump interaction on adjacent calibration fixtures exposes relevant actions such as pickup, enter, and transfer when shared services report them valid.
- Blocked or unavailable actions are explainable if shown.
- Bump/context policy remains frontend presentation policy over shared facts, not engine truth.

## Milestone 5: Canonical option coverage

Goal: every current canonical player-facing option is reachable in new Play mode.

Minimum coverage target:

| Action | Minimum Play-mode path |
|---|---|
| `Move` | Direction key, immediate. |
| Pickup / `TransformAdjacentToInventory` | Default action or context target. |
| Drop / `TransformInventoryToAdjacent` | Drop hotkey or action popup from carried item. |
| `EnterTarget` | Context target / default when adjacent enterable target is unambiguous. |
| `ExitFacing` | Default/action key while contained; ask direction only if ambiguous. |
| `Transfer` | Context target/counterparty; ask item/source only if ambiguous. |

Acceptance criteria:

- In `canonical-debug-size-calibration-room`, a tester can manually exercise all six rows without entering Debug mode.
- The UX may be rough, but all prompts use Select/Cancel stack behavior.
- Prompt layers ask only for unresolved choices.

## Milestone 6: Feedback and debug visibility

Goal: the player/tester can understand what happened and why a prompt appeared.

Work:

- [x] Show a compact result/status line for the last action.
- [x] Show prompt title and current focused choice while a prompt is active.
- [x] In `F12` debug mode, show useful interaction diagnostics:
  - current intent seed;
  - candidate count;
  - selected candidate;
  - prompt stack depth;
  - last submission result or failure reason.

Implementation note:

- `ConsumerPlayModeScreen.DebugRows()` now includes interaction rows for last input, candidate summary, decision, prompt stack/focus, accepted shortcuts, and last submission path/result. These are debug-only (`F12`) rows and do not change normal player-facing presentation.

Acceptance criteria:

- Manual testing can diagnose why a candidate was auto-submitted, prompted, or rejected.
- Normal mode remains player-facing and avoids debug-text-heavy presentation.

## Size-calibration testbed

Use `canonical-debug-size-calibration-room` as the main fixture.

The fixture is useful because the player action plan already includes:

```yaml
Move
TransformAdjacentToInventory
TransformInventoryToAdjacent
EnterTarget
ExitFacing
Transfer
```

and the room contains bulk, aperture, nested-room, nested-bag, narrowing-crevice, and narrowing-box fixtures.

Recommended manual test script:

1. Start `canonical-debug-size-calibration-room` through `Play`.
2. Move around with directional keys.
3. Pick up a valid nearby small/bulk-compatible object.
4. Attempt pickup of an invalid/aperture-blocked object and verify explanation.
5. Drop a carried object.
6. Enter a valid nested room/bag/container.
7. Attempt entering an invalid/narrow target and verify explanation.
8. Exit back out.
9. Transfer an item between the actor and an adjacent inventory-bearing target.
10. Confirm all of the above can be done without entering Debug mode.

## Risks and mitigation

## Completion summary

- Immediate movement is routed through Play-mode intent resolution and shared runtime services.
- Default/context actions resolve shared Action Choice candidates for Pickup, Drop, Enter, Exit, and Transfer.
- Ambiguous or incomplete actions open nested Select/Cancel prompts that ask only for unresolved choices.
- Active prompts consume movement input before player movement; destination prompts can use candidate coordinates for focus movement and shortcut directions where present.
- Transfer remains an explicit transfer-panel workflow and does not auto-submit directly.
- `F12` debug diagnostics now summarize interaction input, decision, prompt stack, focus, candidate counts/sample, shortcuts, and submission path/result.
- Verification at wrap-up: `dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj` passed `287/287`.

## Sprint friction log

- **Milestone 3 test fixture path:** A focused SadConsole test initially tried to load `src/GameGameGame.Content/Beta/Debug/CanonicalDebugRooms.yaml` relative to the test output directory, which failed because test execution runs from `tests/GameGameGame.SadConsole.Tests/bin/...`. **Mitigation:** Use the content file copied by the SadConsole project into `AppContext.BaseDirectory/Content/Beta/Debug/CanonicalDebugRooms.yaml`, preserving the same size-calibration scenario while keeping the test output-directory independent.
- **Milestone 3 size-calibration starting position:** The size-calibration scenario's starting tile does not necessarily have an obvious adjacent default action, so asserting an immediate default action from turn 0 was brittle. **Mitigation:** Move the player one tile north in the test before invoking the default action, positioning the actor adjacent to the maintained calibration fixtures while still exercising the real Play-mode movement and shared Action Choice path.
- **Milestone 3 transfer-panel test compile issue:** The focused custom-component prompt test initially missed the SadConsole test-visible namespace imports for `SadConsoleRect`/theme helpers. **Mitigation:** add explicit test imports and rerun targeted and full SadConsole suites.
- **Milestone 5 size-calibration navigation path:** The first pickup/drop/transfer reachability test path tried to step west from `(2,3)` into the aperture chest at `(1,3)`. **Mitigation:** route one more tile north before stepping west so the test positions the actor at `(1,2)`, adjacent to `debugWeightBulk0`, without relying on blocked movement.
- **Milestone 5 prompt completion behavior:** Nested prompt submissions originally popped only the innermost prompt, leaving the parent prompt active after a successful destination/item submit. This made completed Transfer still look prompt-active. **Mitigation:** treat a successful complete-candidate selection as submitting the prompt stack and clear all prompt layers; Cancel remains the one-layer unwind path.
- **Milestone 5 content plane naming assumption:** Pickup/drop coverage initially assumed the size-calibration root inventory plane was named `world`; the materialized scenario uses `scenarioRoot`. **Mitigation:** capture the item's original plane before pickup and assert return to that plane after Drop instead of hardcoding the plane id.
- **Pickup destination prompt movement capture:** The first pickup destination panel rendered a selected cell, but movement keys still fell through to actor movement unless the prompt choice declared a directional shortcut. **Mitigation:** active prompts now consume all movement input first; shortcut directions submit when present, otherwise movement input changes prompt focus using candidate cell coordinates.
- **Pickup destination highlight visibility:** The inventory-space decorator renderer only drew decorator glyphs when cells were at least three tiles wide, so one-tile prompt grids could carry selected/focused state without a visible highlight. **Mitigation:** inventory-space rendering now applies a highlight background for selected/focused cells, preserving the existing glyph and approximating the desired semi-transparent overlay until true layered/alpha rendering is available.

## Milestone 3 accepted UX notes

- **Prompt placement:** The current action prompt is centered and may cover the entire visible inventory-space component. This is acceptable for now as a placement issue. A likely future invariant/backlog candidate is that the actor and immediate surroundings should not be covered by ordinary action prompts.
- **Exit direction UX:** The current Exit default-action path can present one menu option per direction. This is acceptable for now. A better long-term flow is for Exit to show possible destination options within the inventory-space component and let directional input complete the Exit.
- **Transfer panel exception:** Transfer intentionally does not auto-submit even when there is only one candidate. After choosing a counterparty, a transfer panel should show both entities' inventory spaces so the player has enough information to decide whether to transfer. Give/Take direction is inferred from the selected item ownership. Future UX should support selecting any item on either side with cursor movement or mouse and, for Take, selecting the destination empty space in the same panel. For now, no Transfer should occur except through the transfer panel.
- **Pickup/Drop inventory-panel direction:** Pickup destination choice should show the controlled actor's inventory and select an empty destination cell. Drop should first show the controlled actor's inventory and select the carried item, rather than presenting one top-level option per item. Drop destination selection may still use the existing destination prompt, but directional movement input should submit matching adjacent destination choices when present.

### Shared candidate shape may be insufficient

Risk: current Action Choice or affordance facts may not let Play mode ask "what can I do with this target/seed?" without duplicating Core logic.

Mitigation: call `core-owner` whenever engine semantics are unclear or need to change. If `core-owner` determines that the change is small, they should make the change as part of this sprint. If the change is larger, they should record an action item describing the complexity of the necessary changes so the user can follow up in a separate session.

### Transfer may be the hardest UX

Risk: Transfer may require counterparty, item, direction, and destination choices, and can easily regress to a menu-heavy flow.

Mitigation: infer every part that shared services can determine uniquely. Prompt only for the remaining ambiguity. Escalate unclear ownership or missing Action Choice facts to `core-owner` under the sprint mitigation rule above.

### Bump semantics could accidentally become engine truth

Risk: directional bump behavior may become hardcoded as action legality or simulation semantics.

Mitigation: keep bump/context behavior as frontend player-control policy over shared facts. Movement/action execution remains authoritative in Core/shared services. Escalate missing semantic facts to `core-owner` under the sprint mitigation rule above.

## Definition of done

The sprint is done when:

- New Play mode supports immediate movement.
- New Play mode has an abstract intent/candidate/prompt-stack controller.
- Current canonical player-facing options are reachable in the size-calibration scenario.
- Unambiguous actions auto-submit.
- Ambiguous actions prompt only for unresolved information.
- Cancel never mutates simulation state.
- Frontend tests cover policy behavior without encoding action legality.
- Missing or unclear shared capabilities were either handled by `core-owner` as small sprint changes or recorded by `core-owner` as follow-up action items with complexity notes.
