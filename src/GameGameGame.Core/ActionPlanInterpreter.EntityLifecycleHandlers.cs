namespace GameGameGame.Core;

public sealed partial class ActionPlanInterpreter
{
    private PlanEffectResult ApplyCreateEntity(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        ActionPlanBehaviorStepDescriptor step)
    {
        var trace = new TraceNode("Primitive CreateEntity", TraceStatus.Info);
        if (string.IsNullOrWhiteSpace(step.TemplateId))
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = "CreateEntity requires templateId";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!world.RuntimeEntityTemplates.TryGetValue(step.TemplateId, out var template))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"template {step.TemplateId} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!TrySelectCreateDestination(world, actorId, context, step, trace, out var destination))
        {
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var createdId = GenerateRuntimeEntityId(world, template.TemplateId);
        CreateRuntimeEntity(world, createdId, template, destination);
        trace.Status = TraceStatus.Success;
        trace.Detail = $"created {createdId} from {template.TemplateId} at {destination}";
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
    }

    private PlanEffectResult ApplyPolymorphTarget(
        WorldState world,
        ActionPlanContext context,
        ActionPlanBehaviorStepDescriptor step)
    {
        var trace = new TraceNode("Primitive PolymorphTarget", TraceStatus.Info);
        if (string.IsNullOrWhiteSpace(step.TemplateId))
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = "PolymorphTarget requires templateId";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!world.RuntimeEntityTemplates.TryGetValue(step.TemplateId, out var template))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"template {step.TemplateId} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!context.TryRead<EntityPlanValue>(ActionPlanSlot.Target, out var target, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(readTrace);
        if (!world.Entities.TryGetValue(target.Value, out var entity))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"target {target.Value} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        world.Entities[target.Value] = entity with
        {
            Name = template.Name,
            Bulk = template.Bulk,
            Aperture = template.Aperture,
            EnterPolicy = template.EnterPolicy,
            ExitPolicy = template.ExitPolicy,
            TopologyPolicy = template.TopologyPolicy,
            TemplateId = template.TemplateId
        };
        ApplyTemplateActionDefaults(world, target.Value, template, preserveExistingFacing: true);
        trace.Status = TraceStatus.Success;
        trace.Detail = $"polymorphed {target.Value} into {template.TemplateId}";
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
    }

    private bool TrySelectCreateDestination(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        ActionPlanBehaviorStepDescriptor step,
        TraceNode trace,
        out PlaneCoord destination)
    {
        var placement = step.CreatePlacement ?? CreateEntityPlacement.AdjacentOpen;
        if (placement == CreateEntityPlacement.AdjacentOpen)
        {
            foreach (var direction in DirectionMath.AllDirections)
            {
                if (_movement.TryGetMoveDestination(world, actorId, direction, out destination) && _movement.CanPlace(world, destination))
                {
                    trace.Add(TraceNode.Success("Selected create destination", destination.ToString()));
                    return true;
                }
            }

            destination = default;
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidPlacement;
            trace.Detail = "no open adjacent destination for CreateEntity";
            return false;
        }

        var mode = step.DirectionMode ?? ActionPlanMoveDirectionMode.Forward;
        if (!TryResolveMoveDirection(mode, context, out var resolvedDirection, out var readTrace, out var failureDetail))
        {
            if (readTrace is not null)
            {
                trace.Add(readTrace);
            }

            destination = default;
            trace.Status = TraceStatus.Failure;
            trace.Detail = failureDetail;
            return false;
        }

        if (readTrace is not null)
        {
            trace.Add(readTrace);
        }

        if (!_movement.TryGetMoveDestination(world, actorId, resolvedDirection, out destination) || !_movement.CanPlace(world, destination))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidPlacement;
            trace.Detail = $"cannot create entity at {destination}";
            return false;
        }

        trace.Add(TraceNode.Success("Selected create destination", destination.ToString()));
        return true;
    }

    private static void CreateRuntimeEntity(
        WorldState world,
        EntityId entityId,
        RuntimeEntityTemplate template,
        PlaneCoord destination)
    {
        var nodeId = world.GetNodeId(destination);
        world.Entities.Add(entityId, new Entity(
            entityId,
            template.Name,
            nodeId,
            template.InventoryWidth,
            template.InventoryHeight,
            template.Bulk,
            template.Aperture,
            template.EnterPolicy,
            template.ExitPolicy,
            template.TopologyPolicy,
            template.TemplateId));
        world.Occupancy.Add(nodeId, entityId);
        ApplyTemplateActionDefaults(world, entityId, template, preserveExistingFacing: false);

        if (template.InventoryWidth > 0 && template.InventoryHeight > 0)
        {
            var planeId = new PlaneId(entityId.Value);
            world.Planes.Add(planeId, new Plane(planeId, $"{template.Name} Inventory", template.InventoryWidth, template.InventoryHeight));
            for (var y = 0; y < template.InventoryHeight; y++)
            {
                for (var x = 0; x < template.InventoryWidth; x++)
                {
                    world.AddNode(planeId, new GridCoord(x, y));
                }
            }

            world.RegisterInventoryPlane(entityId, planeId);
        }
    }

    private static void ApplyTemplateActionDefaults(
        WorldState world,
        EntityId entityId,
        RuntimeEntityTemplate template,
        bool preserveExistingFacing)
    {
        if (template.DefaultActionPlanId is { } planId)
        {
            world.SetDefaultActionPlanId(entityId, planId);
        }
        else
        {
            world.ClearDefaultActionPlanId(entityId);
        }

        if (!preserveExistingFacing && template.InitialFacing is { } facing)
        {
            world.SetActionFacing(entityId, facing);
        }
    }

    private static EntityId GenerateRuntimeEntityId(WorldState world, string templateId)
    {
        var normalized = string.IsNullOrWhiteSpace(templateId) ? "entity" : templateId.Trim().ToLowerInvariant();
        var index = 1;
        while (true)
        {
            var candidate = new EntityId($"{normalized}-{index}");
            if (!world.Entities.ContainsKey(candidate))
            {
                return candidate;
            }

            index++;
        }
    }
}
