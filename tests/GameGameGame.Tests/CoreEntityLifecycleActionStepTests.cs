using GameGameGame.Core;

namespace GameGameGame.Tests;

public sealed class CoreEntityLifecycleActionStepTests
{
    [Fact]
    public void CreateEntityCreatesTemplateInstanceInFirstOpenAdjacentCellByDefault()
    {
        var world = TestWorld.CreateWorld();
        world.RuntimeEntityTemplates.Add(
            "rat",
            new RuntimeEntityTemplate(
                TemplateId: "rat",
                Name: "Rat",
                InventoryWidth: 0,
                InventoryHeight: 0,
                Bulk: 1,
                Aperture: 1));
        var beforeEntityIds = world.Entities.Keys.ToHashSet();
        var plan = new ActionPlanDefinition(
            new ActionPlanId("create-rat"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.CreateEntity, TemplateId: "rat")
            ]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        var createdId = Assert.Single(world.Entities.Keys.Except(beforeEntityIds));
        var created = world.Entities[createdId];
        Assert.Equal("Rat", created.Name);
        Assert.Equal("rat", created.TemplateId);
        Assert.Equal(1, created.Bulk);
        Assert.Equal(1, created.Aperture);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)), world.GetEntityLocation(createdId));
    }

    [Fact]
    public void CreateEntityFacingPlacementUsesAuthoredDirectionMode()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.East);
        world.RuntimeEntityTemplates.Add("rat", new RuntimeEntityTemplate("rat", "Rat", 0, 0, 1, 1));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("create-rat-east"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(
                    ActionPlanBehaviorStepKind.CreateEntity,
                    TemplateId: "rat",
                    CreatePlacement: CreateEntityPlacement.Facing,
                    DirectionMode: ActionPlanMoveDirectionMode.Forward)
            ]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        var created = world.GetOccupant(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)));
        Assert.NotNull(created);
        Assert.Equal("rat", world.Entities[created!.Value].TemplateId);
    }

    [Fact]
    public void PolymorphTargetAppliesTemplateDefaultsAndPreservesRuntimeIdentityFacingInventoryAndTargets()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionFacing(TestWorld.SlimeId, Direction.South);
        world.SetActionTarget(TestWorld.SlimeId, 2, TestWorld.RockId);
        world.SetActionTarget(TestWorld.SlimeId, "food", TestWorld.RockId);
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { TemplateId = "egg" };
        world.RuntimeEntityTemplates.Add(
            "butterfly",
            new RuntimeEntityTemplate(
                TemplateId: "butterfly",
                Name: "Butterfly",
                InventoryWidth: 4,
                InventoryHeight: 4,
                Bulk: 1,
                Aperture: 2,
                DefaultActionPlanId: new ActionPlanId("butterfly-plan"),
                EnterPolicy: EntityEnterPolicy.FarthestFromOccupied,
                ExitPolicy: EntityExitPolicy.EdgeAlignedWithExitDirection,
                TopologyPolicy: EntityTopologyPolicy.ConnectsInwardAndOutward));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var originalLocation = world.GetEntityLocation(TestWorld.SlimeId);
        var originalInventoryPlane = world.GetRegisteredInventoryPlaneId(TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("polymorph"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PolymorphTarget, TemplateId: "butterfly")
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.True(world.Entities.ContainsKey(TestWorld.SlimeId));
        var morphed = world.Entities[TestWorld.SlimeId];
        Assert.Equal("butterfly", morphed.TemplateId);
        Assert.Equal("Butterfly", morphed.Name);
        Assert.Equal(1, morphed.Bulk);
        Assert.Equal(2, morphed.Aperture);
        Assert.Equal(1, morphed.InventoryWidth);
        Assert.Equal(1, morphed.InventoryHeight);
        Assert.Equal(EntityEnterPolicy.FarthestFromOccupied, morphed.EnterPolicy);
        Assert.Equal(EntityExitPolicy.EdgeAlignedWithExitDirection, morphed.ExitPolicy);
        Assert.Equal(EntityTopologyPolicy.ConnectsInwardAndOutward, morphed.TopologyPolicy);
        Assert.Equal(new ActionPlanId("butterfly-plan"), world.GetDefaultActionPlanId(TestWorld.SlimeId));
        Assert.Equal(Direction.South, world.GetActionFacing(TestWorld.SlimeId));
        Assert.Equal(TestWorld.RockId, world.GetActionTarget(TestWorld.SlimeId, 2));
        Assert.Equal(TestWorld.RockId, world.GetActionTarget(TestWorld.SlimeId, "food"));
        Assert.Equal(originalLocation, world.GetEntityLocation(TestWorld.SlimeId));
        Assert.Equal(originalInventoryPlane, world.GetRegisteredInventoryPlaneId(TestWorld.SlimeId));
        Assert.Equal(TestWorld.RockId, world.GetOccupant(new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));
    }
}
