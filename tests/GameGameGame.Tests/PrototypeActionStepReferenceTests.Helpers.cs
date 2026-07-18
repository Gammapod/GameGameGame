using GameGameGame.Core;

namespace GameGameGame.Tests;

public sealed partial class PrototypeActionStepReferenceTests
{
    private static (ActionPlanDefinition Wandering, ActionPlanDefinition HandleBlocker, IReadOnlyDictionary<ActionPlanId, ActionPlanDefinition> Registry) CreateWanderingPlanDefinitions()
    {
        var wanderingId = new ActionPlanId("wandering");
        var handleBlockerId = new ActionPlanId("handleBlocker");
        var wandering = new ActionPlanDefinition(
            wanderingId,
            [
                new ActionPlanStep(
                    "move facing",
                    [new CanMoveCheck("facing")],
                    new MoveEffect("facing"),
                    onFailure: null),
                new ActionPlanStep(
                    "handle blocker",
                    [new BlockingEntityCheck("facing", "target")],
                    new CallPlanEffect(handleBlockerId),
                    new ReverseDirectionEffect("facing", consumesTurn: false, continuePlan: true)),
                new ActionPlanStep("wait", [], new WaitEffect(), onFailure: null)
            ]);
        var handleBlocker = new ActionPlanDefinition(
            handleBlockerId,
            [
                new ActionPlanStep(
                    "pickup blocker",
                    [new CanPickupCheck("target", new GridCoord(0, 0))],
                    new PickupEffect("target", new GridCoord(0, 0)),
                    onFailure: null),
                new ActionPlanStep(
                    "reverse after bump",
                    [],
                    new ReverseDirectionEffect("facing", consumesTurn: true, continuePlan: false),
                    onFailure: null)
            ]);

        return (wandering, handleBlocker, new Dictionary<ActionPlanId, ActionPlanDefinition>
        {
            [wandering.Id] = wandering,
            [handleBlocker.Id] = handleBlocker
        });
    }

    private static bool TraceContains(TraceNode trace, string label)
    {
        return trace.Label == label || trace.Children.Any(child => TraceContains(child, label));
    }

    private static bool TraceDetailContains(TraceNode trace, string detail)
    {
        return trace.Detail?.Contains(detail, StringComparison.Ordinal) == true
            || trace.Children.Any(child => TraceDetailContains(child, detail));
    }

    private static bool TraceHasReason(TraceNode trace, FailureReason reason)
    {
        return trace.Reason == reason || trace.Children.Any(child => TraceHasReason(child, reason));
    }

    private static ActionPlanDefinition CreateBehaviorPlan(string id, ActionPlanBehaviorStepKind stepKind) =>
        new(
            new ActionPlanId(id),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(stepKind)]));

    private static EntityId AddEntityWithInventory(
        WorldState world,
        string id,
        string name,
        PlaneCoord location,
        int inventoryWidth,
        int inventoryHeight,
        int carryingCapacity)
    {
        var entityId = AddEntity(world, id, name, location, inventoryWidth, inventoryHeight, bulk: 1, aperture: carryingCapacity);
        var inventoryPlaneId = new PlaneId($"{id}Inventory");
        AddPlane(world, inventoryPlaneId, inventoryWidth, inventoryHeight);
        world.RegisterInventoryPlane(entityId, inventoryPlaneId);
        return entityId;
    }

    private static EntityId AddEntity(
        WorldState world,
        string id,
        string name,
        PlaneCoord location,
        int inventoryWidth = 0,
        int inventoryHeight = 0,
        int bulk = 1,
        int aperture = 1)
    {
        var entityId = new EntityId(id);
        var nodeId = world.GetNodeId(location);
        world.Entities.Add(entityId, new Entity(entityId, name, nodeId, inventoryWidth, inventoryHeight, bulk, aperture));
        world.Occupancy.Add(nodeId, entityId);
        return entityId;
    }

    private static void AddPlane(WorldState world, PlaneId planeId, int width, int height)
    {
        world.Planes.Add(planeId, new Plane(planeId, planeId.Value, width, height));
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                world.AddNode(planeId, new GridCoord(x, y));
            }
        }
    }

    private sealed record TestPlanCheck(
        string Label,
        bool passed,
        IReadOnlyDictionary<string, PlanValue>? Writes = null) : IPlanCheck
    {
        public PlanCheckResult Evaluate(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement) =>
            new(passed, Writes ?? new Dictionary<string, PlanValue>(), new TraceNode(Label, passed ? TraceStatus.Success : TraceStatus.Failure));
    }

    private sealed class RecordingPlanEffect(string label, List<string> executed, bool consumesTurn) : IPlanEffect
    {
        public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
        {
            executed.Add(label);

            return new PlanEffectResult(
                Succeeded: true,
                ConsumesTurn: consumesTurn,
                ContinuePlan: !consumesTurn,
                TraceNode.Success(label));
        }
    }

    private sealed class ReadDirectionEffect(string label, string variableName, Action<Direction> read) : IPlanEffect
    {
        public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
        {
            if (!context.TryGet<DirectionPlanValue>(variableName, out var value))
            {
                return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, TraceNode.Failure(label, FailureReason.None, $"missing {variableName}"));
            }

            read(value.Value);

            return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, TraceNode.Success(label, value.Value.ToString()));
        }
    }

    private sealed class SlotWritingEffect(ActionPlanSlot slot, PlanValue value) : IPlanEffect
    {
        public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
        {
            var trace = context.Set(slot, value);

            return new PlanEffectResult(true, ConsumesTurn: false, ContinuePlan: false, trace);
        }
    }

    private sealed class SlotReadingEffect(ActionPlanSlot slot, Action<Direction> read) : IPlanEffect
    {
        public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
        {
            if (!context.TryRead<DirectionPlanValue>(slot, out var value, out var trace))
            {
                return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
            }

            read(value.Value);

            return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
        }
    }
}
