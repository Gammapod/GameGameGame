using GameGameGame.Core;

namespace GameGameGame.Content;

public static class PrototypeContent
{
    public static readonly EntityId GameId = new("game");
    public static readonly EntityId PlayerId = new("player");
    public static readonly EntityId SlimeId = new("slime");
    public static readonly EntityId GiantSlimeId = new("giantSlime");
    public static readonly EntityId RockId = new("rock");

    public static readonly PlaneId GamePlaneId = new("gamePlane");
    public static readonly PlaneId GameInventoryPlaneId = new("world");
    public static readonly PlaneId PlayerInventoryPlaneId = new("player");
    public static readonly PlaneId SlimeInventoryPlaneId = new("slime");
    public static readonly PlaneId GiantSlimeInventoryPlaneId = new("giantSlime");

    public static readonly EntityTemplateId GameTemplateId = new("game");
    public static readonly EntityTemplateId PlayerTemplateId = new("player");
    public static readonly EntityTemplateId SlimeTemplateId = new("slime");
    public static readonly EntityTemplateId GiantSlimeTemplateId = new("giantSlime");
    public static readonly EntityTemplateId RockTemplateId = new("rock");

    public static readonly ActionPlanTemplateId WanderingActionPlanTemplateId = new("wandering");

    public static readonly ActionPlanTemplateId HandleBlockerActionPlanTemplateId = new("handleBlocker");

    public static FirstSliceBuildResult CreateFirstSlice()
    {
        var registry = CreateRegistry();
        var world = new WorldState();
        var actionPlans = new Dictionary<EntityId, IEntityActionPlan>();

        AddRectangularPlane(world, new Plane(GamePlaneId, "Game Plane", 1, 1));
        CollectActionPlan(actionPlans, registry.SpawnEntity(
            world,
            GameTemplateId,
            new EntitySpawnOptions(
                GameId,
                new PlaneCoord(GamePlaneId, new GridCoord(0, 0)),
                InventoryPlaneId: GameInventoryPlaneId,
                InventoryPlaneName: "World")));
        CollectActionPlan(actionPlans, registry.SpawnEntity(
            world,
            PlayerTemplateId,
            new EntitySpawnOptions(
                PlayerId,
                new PlaneCoord(GameInventoryPlaneId, new GridCoord(1, 2)),
                InventoryPlaneId: PlayerInventoryPlaneId,
                InventoryPlaneName: "Player Inventory")));
        CollectActionPlan(actionPlans, registry.SpawnEntity(
            world,
            SlimeTemplateId,
            new EntitySpawnOptions(
                SlimeId,
                new PlaneCoord(GameInventoryPlaneId, new GridCoord(1, 1)),
                InventoryPlaneId: SlimeInventoryPlaneId,
                InventoryPlaneName: "Slime Inventory")));
        CollectActionPlan(actionPlans, registry.SpawnEntity(
            world,
            RockTemplateId,
            new EntitySpawnOptions(RockId, new PlaneCoord(GameInventoryPlaneId, new GridCoord(2, 1)))));
        CollectActionPlan(actionPlans, registry.SpawnEntity(
            world,
            GiantSlimeTemplateId,
            new EntitySpawnOptions(
                GiantSlimeId,
                new PlaneCoord(GameInventoryPlaneId, new GridCoord(3, 3)),
                InventoryPlaneId: GiantSlimeInventoryPlaneId,
                InventoryPlaneName: "Giant Slime Inventory")));

        return new FirstSliceBuildResult(world, actionPlans, registry);
    }

    private static void CollectActionPlan(Dictionary<EntityId, IEntityActionPlan> actionPlans, EntitySpawnResult result)
    {
        foreach (var (entityId, actionPlan) in result.ActionPlans)
        {
            actionPlans[entityId] = actionPlan;
        }
    }

    public static PrototypeContentRegistry CreateRegistry() =>
        YamlContentLoader.LoadRegistryResource(typeof(PrototypeContent).Assembly, "GameGameGame.Content.PrototypeContent.yaml");

    public static EntityTemplate CreateGameTemplate() =>
        CreateRegistry().GetEntityTemplate(GameTemplateId);

    public static EntityTemplate CreatePlayerTemplate() =>
        CreateRegistry().GetEntityTemplate(PlayerTemplateId);

    public static EntityTemplate CreateSlimeTemplate() =>
        CreateRegistry().GetEntityTemplate(SlimeTemplateId);

    public static EntityTemplate CreateGiantSlimeTemplate() =>
        CreateRegistry().GetEntityTemplate(GiantSlimeTemplateId);

    public static EntityTemplate CreateRockTemplate() =>
        CreateRegistry().GetEntityTemplate(RockTemplateId);

    internal static EntitySpawnResult SpawnEntity(WorldState world, EntityTemplate template, EntitySpawnOptions options)
    {
        if (!world.TryGetNodeId(options.Location, out var nodeId))
        {
            throw new InvalidOperationException($"Cannot spawn {options.EntityId}: {options.Location} does not exist.");
        }

        if (world.Occupancy.ContainsKey(nodeId))
        {
            throw new InvalidOperationException($"Cannot spawn {options.EntityId}: {options.Location} is occupied.");
        }

        template = options.ModifyTemplate?.Invoke(template) ?? template;
        var entity = new Entity(
            options.EntityId,
            template.Name,
            nodeId,
            template.InventoryWidth,
            template.InventoryHeight,
            template.Bulk,
            template.Aperture,
            template.EnterPolicy,
            template.ExitPolicy,
            template.TopologyPolicy);

        AddEntity(world, entity, options.InventoryPlaneId, options.InventoryPlaneName);
        IEntityActionPlan? actionPlan = null;
        var actionPlans = new Dictionary<EntityId, IEntityActionPlan>();

        if (actionPlan is not null)
        {
            actionPlans[options.EntityId] = actionPlan;
        }

        if (template.CarriedEntities is not null && template.CarriedEntities.Count > 0)
        {
            if (world.GetInventoryPlaneId(options.EntityId) is not { } inventoryPlaneId)
            {
                throw new InvalidOperationException($"Cannot place carried entities for {options.EntityId}: template has no usable inventory.");
            }

            foreach (var carried in template.CarriedEntities)
            {
                if (carried.Template is null)
                {
                    throw new InvalidOperationException($"Cannot place carried entity {carried.EntityId}: template ID {carried.TemplateId} requires registry spawning.");
                }

                var carriedResult = SpawnEntity(
                    world,
                    carried.Template,
                    new EntitySpawnOptions(
                        carried.EntityId,
                        new PlaneCoord(inventoryPlaneId, carried.Coord)));

                foreach (var (entityId, carriedActionPlan) in carriedResult.ActionPlans)
                {
                    actionPlans[entityId] = carriedActionPlan;
                }
            }
        }

        return new EntitySpawnResult(options.EntityId, actionPlan, actionPlans);
    }

    private static void AddEntity(WorldState world, Entity entity, PlaneId? inventoryPlaneId = null, string? inventoryPlaneName = null)
    {
        if (entity.HasUsableInventory)
        {
            if (inventoryPlaneId is not { } planeId)
            {
                planeId = new PlaneId(entity.Id.Value);
            }

            AddRectangularPlane(world, new Plane(planeId, inventoryPlaneName ?? $"{entity.Name} Inventory", entity.InventoryWidth, entity.InventoryHeight));
            world.RegisterInventoryPlane(entity.Id, planeId);
        }

        world.Entities.Add(entity.Id, entity);
        world.Occupancy.Add(entity.OccupiedNodeId, entity.Id);
    }

    private static void AddRectangularPlane(WorldState world, Plane plane)
    {
        world.Planes.Add(plane.Id, plane);

        for (var y = 0; y < plane.Height; y++)
        {
            for (var x = 0; x < plane.Width; x++)
            {
                world.AddNode(plane.Id, new GridCoord(x, y));
            }
        }
    }
}
