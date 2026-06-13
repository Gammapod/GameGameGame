using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class YamlContentLoaderTests
{
    [Fact]
    public void YamlContentLoaderCreatesRegistryFromDeclarativeContent()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              rock:
                name: Rock
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 3
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 3
                defaultActionPlanId: wait
                defaultPlanVariables:
                  facing:
                    kind: Direction
                    directionValue: West
            presentations:
              rock:
                glyph: '*'
                color: Earth
              slime:
                glyph: s
                color: Green
            actionPlans:
              wait:
                id: wait
                steps:
                  - label: wait
                    checks: []
                    onSuccess:
                      kind: Wait
            """);

        var result = registry.Validate();

        Assert.True(result.IsValid);
        Assert.Equal("Slime", registry.GetEntityTemplate(new EntityTemplateId("slime")).Name);
        Assert.Equal('s', registry.GetPresentation(new EntityTemplateId("slime")).Glyph);
        Assert.Equal(PlanEffectKind.Wait, registry.GetActionPlanDescriptor(new ActionPlanTemplateId("wait")).Steps.Single().OnSuccess!.Kind);
        var variables = registry.GetEntityTemplate(new EntityTemplateId("slime")).DefaultPlanVariables!;
        Assert.Equal(Direction.West, variables["facing"].DirectionValue);
    }

    [Fact]
    public void YamlContentLoaderCanLoadRegistryFromFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"game-content-{Guid.NewGuid():N}.yaml");

        try
        {
            File.WriteAllText(
                path,
                """
                entityTemplates:
                  rock:
                    name: Rock
                    inventoryWidth: 0
                    inventoryHeight: 0
                    weight: 3
                    carryingCapacity: 3
                presentations:
                  rock:
                    glyph: '*'
                    color: Earth
                actionPlans: {}
                """);

            var registry = YamlContentLoader.LoadRegistryFile(path);

            Assert.True(registry.Validate().IsValid);
            Assert.Equal("Rock", registry.GetEntityTemplate(new EntityTemplateId("rock")).Name);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void YamlContentLoaderLoadsCanonicalActionPlanDescriptorsWithoutVariableNames()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: canonical
                defaultPlanVariables:
                  facing:
                    kind: Direction
                    directionValue: South
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              canonical:
                id: canonical
                steps:
                  - label: move facing
                    checks:
                      - kind: CanMove
                    onSuccess:
                      kind: Move
                  - label: reverse
                    checks: []
                    onSuccess:
                      kind: ReverseDirection
                      consumesTurn: false
                      continuePlan: false
            """);

        var result = registry.Validate();
        var descriptor = registry.GetActionPlanDescriptor(new ActionPlanTemplateId("canonical"));

        Assert.True(result.IsValid);
        Assert.Null(descriptor.Steps[0].Checks.Single().DirectionVariable);
        Assert.Null(descriptor.Steps[0].OnSuccess!.DirectionVariable);
        Assert.Null(descriptor.Steps[1].OnSuccess!.DirectionVariable);
        Assert.IsType<CanMoveCheck>(descriptor.Steps[0].Checks.Single().Materialize());
        Assert.IsType<MoveEffect>(descriptor.Steps[0].OnSuccess!.Materialize());
    }

    [Fact]
    public void YamlContentLoaderLoadsCanonicalActorActionStateDefaults()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                actionStateDefaults:
                  facing: East
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans: {}
            """);

        var template = registry.GetEntityTemplate(new EntityTemplateId("slime"));

        Assert.Equal(Direction.East, template.ActionStateDefaults!.Facing);
    }

    [Fact]
    public void YamlContentLoaderLoadsMovementPrimitiveDescriptors()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              movement:
                id: movement
                steps:
                  - label: teleport rock
                    checks: []
                    onSuccess:
                      kind: Teleport
                      movementTarget:
                        kind: Entity
                        entityId: rock
                      movementDestination:
                        kind: PlaneCoord
                        planeCoord:
                          planeId: world
                          coord:
                            x: 4
                            y: 2
                  - label: drop carried
                    checks: []
                    onSuccess:
                      kind: Drop
                      movementTarget:
                        kind: CarriedInventoryCoord
                        inventoryCoord:
                          x: 0
                          y: 1
                      movementDestination:
                        kind: AdjacentToSelf
                        direction: East
            """);

        var descriptor = registry.GetActionPlanDescriptor(new ActionPlanTemplateId("movement"));
        var teleport = descriptor.Steps[0].OnSuccess!;
        var drop = descriptor.Steps[1].OnSuccess!;

        Assert.Equal(PlanEffectKind.Teleport, teleport.Kind);
        Assert.Equal(MovementTargetKind.Entity, teleport.MovementTarget!.Kind);
        Assert.Equal(new EntityId("rock"), teleport.MovementTarget.EntityId);
        Assert.Equal(MovementDestinationKind.PlaneCoord, teleport.MovementDestination!.Kind);
        Assert.Equal(new PlaneCoord(new PlaneId("world"), new GridCoord(4, 2)), teleport.MovementDestination.PlaneCoord);
        Assert.Equal(PlanEffectKind.Drop, drop.Kind);
        Assert.Equal(MovementTargetKind.CarriedInventoryCoord, drop.MovementTarget!.Kind);
        Assert.Equal(new GridCoord(0, 1), drop.MovementTarget.InventoryCoord);
        Assert.Equal(MovementDestinationKind.AdjacentToSelf, drop.MovementDestination!.Kind);
        Assert.Equal(Direction.East, drop.MovementDestination.Direction);
    }

    [Fact]
    public void SpawnedActionPlanUsesCanonicalInitialFacingDefault()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              actor:
                name: Actor
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
                defaultActionPlanId: moveFacing
                actionStateDefaults:
                  facing: South
            presentations:
              actor:
                glyph: a
                color: Green
            actionPlans:
              moveFacing:
                id: moveFacing
                steps:
                  - label: move facing
                    checks:
                      - kind: CanMove
                    onSuccess:
                      kind: Move
            """);
        var world = TestWorld.CreateWorld();
        var actorId = new EntityId("actor");
        var spawn = registry.SpawnEntity(
            world,
            new EntityTemplateId("actor"),
            new EntitySpawnOptions(actorId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(3, 1))));
        var turns = new TurnService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        var acted = turns.ResolvePlan(world, actorId, spawn.ActionPlan!.PlanTurn(world, actorId, new MovementService()));

        Assert.True(acted);
        Assert.Equal("Actor@world(3,2)", world.FormatEntityAddress(actorId));
        Assert.True(TraceContains(world.LastTrace!, "Read slot Facing"));
    }

    private static bool TraceContains(TraceNode trace, string label) =>
        trace.Label == label || trace.Children.Any(child => TraceContains(child, label));
}
