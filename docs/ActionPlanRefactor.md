# Action Plan Refactor

This document describes an incremental path from the current C# action-plan classes toward explicit, data-composable action plans with variables, ranked checks/effects, nested plan calls, and structured turn traces.

The refactor should preserve the current playable slice while changing one layer at a time. Primitive engine actions such as move, pickup, drop, and wait should remain stable until the plan interpreter can compose them.

## Goals

- Keep UI/frontends free of simulation logic.
- Keep primitive world effects in the engine.
- Make action plans explicit content/data compositions rather than entity-specific C# behavior classes.
- Support arbitrary plan variables such as `facing`, `target`, `range`, or later entity references.
- Support ranked steps made of checks, success effects, failure effects, and control-flow policy.
- Support nested plan calls with recursion/depth guards.
- Preserve structured traces for testing and debugging.

## Non-Goals For The First Pass

- Do not introduce a full scripting language.
- Do not serialize plans to external files yet.
- Do not rewrite primitive actions unless needed by the plan interpreter.
- Do not introduce complex variable scoping until shared per-entity plan state proves insufficient.

## Step 1: Preserve Primitive Actions As Engine Operations

Keep the existing primitive action types as the initial execution targets for plans:

- `MoveAction`
- `PickupAction`
- `DropAction`
- `WaitAction`

These actions continue to own concrete world mutations and their direct validation until checks are separately factored.

Testable outcomes:

- Existing movement tests still pass.
- Existing pickup/drop tests still pass.
- Existing trace tests for primitive action failures still pass.
- No console code directly mutates world occupancy for player actions.

## Step 2: Add Generic Plan State

Introduce a plan runtime state object that stores named variables independently of a specific plan class.

Suggested shape:

```csharp
public sealed class ActionPlanContext
{
    public Dictionary<string, PlanValue> Variables { get; } = [];
}
```

Use strict value types if practical:

```csharp
public abstract record PlanValue;
public sealed record DirectionValue(Direction Value) : PlanValue;
public sealed record EntityValue(EntityId Value) : PlanValue;
public sealed record CoordValue(GridCoord Value) : PlanValue;
public sealed record IntValue(int Value) : PlanValue;
```

Initial recommendation:

- Use one shared mutable context per entity plan assignment.
- Persistent variables such as `facing` live in that context.
- Temporary variables such as `target` may be overwritten during execution.
- Nested plans share the same context.

Testable outcomes:

- A plan variable can be initialized to `Direction.West`.
- A plan execution can read `facing` from context.
- A plan execution can update `facing` in context.
- `facing` persists across turns for the same actor.
- A variable update appears in the plan trace.

## Step 3: Introduce Explicit Plan Definition Types

Add data-shaped types for plans and ranked steps.

Suggested shape:

```csharp
public readonly record struct ActionPlanId(string Value);

public sealed record ActionPlanDefinition(
    ActionPlanId Id,
    IReadOnlyList<ActionPlanStep> Steps);

public sealed record ActionPlanStep(
    string Label,
    IReadOnlyList<IPlanCheck> Checks,
    IPlanEffect? OnSuccess,
    IPlanEffect? OnFailure);
```

Each step should evaluate checks in order. The step succeeds only if all checks pass.

Testable outcomes:

- A plan can contain multiple ranked steps.
- Steps are evaluated in order.
- Later steps are not evaluated after a consuming successful step.
- A failed step can continue to the next ranked step.
- A trace node is emitted for the plan and for each evaluated step.

## Step 4: Add Plan Checks

Separate plan-level checks from primitive action execution.

Suggested interface:

```csharp
public interface IPlanCheck
{
    PlanCheckResult Evaluate(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement);
}

public sealed record PlanCheckResult(
    bool Passed,
    IReadOnlyDictionary<string, PlanValue> VariableWrites,
    TraceNode Trace);
```

Initial checks should cover the current slime behavior:

- `CanMoveCheck`, reading direction from a variable or literal.
- `BlockingEntityCheck`, reading direction and writing a target entity variable.
- `CanPickupCheck`, reading target and destination.

Testable outcomes:

- `CanMoveCheck` passes when the destination is in bounds and empty.
- `CanMoveCheck` fails with a trace when movement is blocked or out of bounds.
- `BlockingEntityCheck` writes `target` when an entity blocks the requested direction.
- `BlockingEntityCheck` fails and does not write `target` when no blocker exists.
- `CanPickupCheck` reports the same important failure reasons as `PickupAction` for the covered cases.
- Check variable writes appear in the step trace.

## Step 5: Add Plan Effects And Control Flow

Introduce explicit success/failure effects and control-flow results.

Suggested shape:

```csharp
public enum PlanFlow
{
    Continue,
    Stop,
    ConsumeTurn
}

public interface IPlanEffect
{
    PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement);
}

public sealed record PlanEffectResult(
    bool Succeeded,
    bool ConsumesTurn,
    bool ContinuePlan,
    TraceNode Trace);
```

Initial effects should wrap existing primitives and variable writes:

- `MoveEffect`
- `PickupEffect`
- `WaitEffect`
- `SetVariableEffect`
- `CompositeEffect`, if needed to combine variable writes and an action in one branch.

Testable outcomes:

- `MoveEffect` executes the same world mutation as `MoveAction`.
- `PickupEffect` executes the same world mutation as `PickupAction`.
- `SetVariableEffect` can update `facing` without consuming a turn.
- A step can define different behavior for success and failure.
- A failed check can continue without consuming a turn.
- A successful primitive action can consume the turn.
- Effect traces are nested under the step trace.

## Step 6: Implement The Plan Interpreter

Add an interpreter responsible for resolving an `ActionPlanDefinition` against an actor and context.

The interpreter should:

- Create a trace node for the plan.
- Create a trace node for each evaluated step.
- Evaluate step checks in order.
- Apply check variable writes only when appropriate.
- Run the step's success effect when all checks pass.
- Run the step's failure effect when any check fails and a failure effect exists.
- Respect effect control flow.
- Return a result compatible with turn resolution.

Suggested result:

```csharp
public sealed record PlanExecutionResult(
    bool Succeeded,
    bool ConsumesTurn,
    bool ContinuePlan,
    TraceNode Trace);
```

Testable outcomes:

- A plan with a successful first step stops or consumes according to its effect.
- A plan with a failed first step and no consuming failure effect tries the next step.
- A plan with all failed non-consuming steps returns failure with a clear trace.
- Variable writes from checks are available to later effects in the same step.
- Variable writes persist into later steps when committed.
- The trace contains plan, step, check, and effect nodes in execution order.

## Step 7: Support Nested Plan Calls

Add a plan effect that invokes another plan definition.

Suggested effect:

```csharp
public sealed record CallPlanEffect(ActionPlanId PlanId) : IPlanEffect;
```

The interpreter should receive a registry of plan definitions and maintain an execution stack.

Guardrails:

- Use a maximum call depth, such as `16`.
- Include every called plan in the trace.
- Record the call stack in the failure trace when the depth guard is hit.
- Keep nested plans sharing the same actor and context for the first pass.

Testable outcomes:

- A plan can call another plan on failure.
- The nested plan can read variables written by the caller.
- The nested plan can update variables in the shared context.
- A consuming action in the nested plan consumes the actor's turn.
- The trace shows the caller plan, call effect, and nested plan.
- Recursive or too-deep calls fail deterministically with a trace.

## Step 8: Recreate Wandering Slime As Plan Data

Replace the bespoke `WanderingSlimeActionPlan` behavior with explicit plan definitions.

Example conceptual plan:

```text
Plan Wandering
  Variable facing = West

  Step Move facing
    Check CanMove(actor, var:facing)
    Success: Move(var:facing), ConsumeTurn
    Failure: Continue

  Step Handle blocker
    Check BlockingEntity(actor, var:facing) -> target
    Success: CallPlan(HandleBlocker)
    Failure: Set facing = Reverse(var:facing), Continue

  Step Wait
    Success: Wait, ConsumeTurn

Plan HandleBlocker
  Step Pickup blocker
    Check CanPickup(actor, var:target, inventorySlot(0,0))
    Success: Pickup(var:target, inventorySlot(0,0)), ConsumeTurn
    Failure: Continue

  Step Bump and reverse
    Success: Set facing = Reverse(var:facing), ConsumeTurn
```

Testable outcomes:

- Slime still moves west on the first turn in the first-slice world.
- Slime still picks up a carryable blocker in front of it.
- Slime still fails to pick up an overweight blocker and reverses facing.
- Giant Slime can use the same plan definition with its own context.
- A Rock can be assigned the same plan definition if given a context and scheduled.
- Existing slime behavior tests pass after being updated to inspect context rather than concrete plan properties.

## Step 9: Move Plan Composition To Content

Once the interpreter is stable, Core should own only:

- Plan model types.
- Plan interpreter.
- Built-in checks.
- Built-in effects.
- Primitive actions.

Content should own:

- Concrete plan definitions such as `Wandering` and `HandleBlocker`.
- Initial variable values for an entity's assigned plan context.
- Which entity instances or templates receive which plans.

Testable outcomes:

- Core does not reference `Slime`, `GiantSlime`, or any prototype entity IDs.
- Content can create the first-slice slime plan definitions.
- Content can assign the same plan definition to multiple entities with independent contexts.
- Tests can construct a minimal Core-only plan without using `PrototypeContent`.

## Step 10: Prepare For Entity Templates And Spawn Overrides

After plans are explicit, introduce entity templates separately from entity instances.

Templates should eventually include:

- Mechanical entity properties.
- Presentation metadata or a reference to presentation metadata.
- Initial carried entities and layout.
- Default action plan assignments.
- Default plan variable initialization.

Spawn overrides should allow combinations such as:

- Spawn a Rock using normal Rock properties.
- Override the Rock's plan assignment to use the Slime wandering plan.
- Override initial variables such as `facing`.

Testable outcomes:

- An entity instance can be created from a template.
- Spawn overrides can change properties without changing the template.
- Spawn overrides can assign a different plan than the template default.
- Initial carried entities are placed into the spawned entity's inventory plane.
- Tests can spawn a Rock with the wandering plan and verify that it moves when scheduled.

## Step 11: Add Data-Shaped Plan Descriptors

Introduce a descriptor layer for built-in checks and effects so content can define plans through simple values instead of directly constructing runtime `IPlanCheck` and `IPlanEffect` objects.

Current first pass:

- `ActionPlanDescriptor` materializes to `ActionPlanDefinition`.
- `ActionPlanStepDescriptor` materializes to `ActionPlanStep`.
- `PlanCheckDescriptor` stores check kind and simple inputs such as variable names and literal coordinates.
- `PlanEffectDescriptor` stores effect kind and simple inputs such as variable names, plan IDs, literal coordinates, and flow flags.
- `PlanValueDescriptor` stores default and override variable values as data and materializes them into runtime `PlanValue` objects when an interpreted plan is created.
- `PrototypeContentRegistry.Validate()` checks content references before runtime use.
- `YamlContentLoader` can load templates, presentations, action-plan descriptors, carried-entity layouts, and default variables from YAML strings or files.

Testable outcomes:

- Descriptor inputs are inspectable without runtime check/effect instances.
- A descriptor materializes to executable built-ins.
- Prototype content defines wandering and blocker plans through descriptors.
- The content registry stores action-plan descriptors and materializes them at the runtime boundary.
- Entity templates and spawn overrides store plan variable descriptors instead of runtime `PlanValue` objects.
- The content registry can validate presentations, template plan references, carried template references, descriptor materialization, and nested plan calls.
- Registry-driven spawns track each entity's template ID, so presentation lookup no longer depends on prototype entity IDs or entity names.
- `PrototypeContent` no longer exposes prototype action-plan factory helpers; tests and callers use registry-created plans instead.
- YAML is the chosen human-editable content format for the first external content pipeline.
- The built-in prototype registry is loaded from the embedded `PrototypeContent.yaml` asset.
- Registry spawns support descriptor-ID action-plan overrides through `ActionPlanOverrideId`.
- Template-level and carried-entity runtime `Func<IEntityActionPlan>` hooks have been removed.
- `CreateFirstSliceWorld()` has been removed; callers use `CreateFirstSlice()` to keep world, registry, and action plans together.
- Static `PrototypeContent.SpawnEntity(...)` is internal implementation detail; public spawning goes through `PrototypeContentRegistry`.
- The spawn-option runtime `Func<IEntityActionPlan>` hook has been removed.
- Content does not directly construct built-in runtime checks/effects.

Remaining follow-up:

- Continue hardening YAML validation and diagnostics as external content grows.

## Trace Requirements

Every plan execution trace should be useful for debugging and stable enough for tests.

Minimum trace hierarchy:

```text
Turn N
  Resolve plan for Actor
    Plan PlanId
      Step Step Label
        Check CheckName
        Effect EffectName
        Call Plan OtherPlanId
          Plan OtherPlanId
            Step ...
```

Trace nodes should include:

- Plan IDs.
- Step labels.
- Check names and pass/fail state.
- Failure reasons where available.
- Variable reads/writes when relevant.
- Primitive action traces nested under effects.
- Nested plan calls.
- Depth-guard failures.

Testable outcomes:

- Tests can assert that a failed check was recorded before a fallback step.
- Tests can assert that a nested plan was called.
- Tests can assert that variable changes occurred.
- Tests can assert that a primitive action trace is nested below the effect that invoked it.

## Recommended Order Of Implementation

1. Add context and plan value types.
2. Add plan definition and step types.
3. Add the interpreter with one or two trivial checks/effects.
4. Add enough checks/effects to express wandering movement.
5. Add nested calls and depth guard.
6. Recreate the current slime plan through definitions.
7. Move plan definitions into Content.
8. Split tests into primitive action, interpreter, content behavior, and later template/spawn tests.

This order keeps each change testable and avoids replacing the entire behavior system at once.
