using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class InitiativePlayerChoiceStepperTests
{
    private static readonly PlaneId PlaneId = new("arena");
    private static readonly EntityId AutomaticId = new("automatic");
    private static readonly EntityId FirstPlayerId = new("firstPlayer");
    private static readonly EntityId SecondPlayerId = new("secondPlayer");

    [Fact]
    public void InitiativeStepperRunsAutomaticActorsBeforeLaterPlayerChoiceActor()
    {
        var world = CreateWorld();
        world.SetActionControlSource(FirstPlayerId, EntityControlSource.PlayerChoice);
        var plans = new Dictionary<EntityId, IEntityActionPlan>
        {
            [AutomaticId] = new FixedEntityActionPlan(PlannedActionPlan.Single(new MoveAction(Direction.East)))
        };
        var descriptors = new Dictionary<EntityId, ActionPlanDescriptor>
        {
            [FirstPlayerId] = MoveDescriptor()
        };
        var stepper = new InitiativePlayerChoiceStepper(new MovementService(), new ActionChoiceService(new MovementService()));

        var result = stepper.AdvanceUntilPlayerChoice(
            world,
            [AutomaticId, FirstPlayerId],
            plans,
            actorId => descriptors.TryGetValue(actorId, out var descriptor) ? descriptor : null,
            startIndex: 0);

        Assert.Equal(new PlaneCoord(PlaneId, new GridCoord(1, 0)), world.GetEntityLocation(AutomaticId));
        Assert.Single(result.ActorLogs, log => log.ActorId == AutomaticId);
        Assert.NotNull(result.Request);
        Assert.Equal(FirstPlayerId, result.Request!.ActorId);
        Assert.Equal(1, result.NextActorIndex);
    }

    [Fact]
    public void InitiativeStepperPromptsMultiplePlayerChoiceActorsInOrder()
    {
        var world = CreateWorld();
        world.SetActionControlSource(FirstPlayerId, EntityControlSource.PlayerChoice);
        world.SetActionControlSource(SecondPlayerId, EntityControlSource.PlayerChoice);
        var descriptors = new Dictionary<EntityId, ActionPlanDescriptor>
        {
            [FirstPlayerId] = MoveDescriptor(),
            [SecondPlayerId] = MoveDescriptor()
        };
        var stepper = new InitiativePlayerChoiceStepper(new MovementService(), new ActionChoiceService(new MovementService()));

        var first = stepper.AdvanceUntilPlayerChoice(
            world,
            [AutomaticId, FirstPlayerId, SecondPlayerId],
            new Dictionary<EntityId, IEntityActionPlan>(),
            actorId => descriptors.TryGetValue(actorId, out var descriptor) ? descriptor : null,
            startIndex: 0);
        var second = stepper.AdvanceUntilPlayerChoice(
            world,
            [AutomaticId, FirstPlayerId, SecondPlayerId],
            new Dictionary<EntityId, IEntityActionPlan>(),
            actorId => descriptors.TryGetValue(actorId, out var descriptor) ? descriptor : null,
            startIndex: first.NextActorIndex + 1);

        Assert.Equal(FirstPlayerId, first.Request?.ActorId);
        Assert.Equal(SecondPlayerId, second.Request?.ActorId);
    }

    [Fact]
    public void InitiativeStepperAdvancesPlayerlessTurnsWithoutPrompt()
    {
        var world = CreateWorld();
        var plans = new Dictionary<EntityId, IEntityActionPlan>
        {
            [AutomaticId] = new FixedEntityActionPlan(PlannedActionPlan.Single(new MoveAction(Direction.East)))
        };
        var stepper = new InitiativePlayerChoiceStepper(new MovementService(), new ActionChoiceService(new MovementService()));

        var result = stepper.AdvanceUntilPlayerChoice(
            world,
            [AutomaticId],
            plans,
            _ => null,
            startIndex: 0);

        Assert.Null(result.Request);
        Assert.True(result.CompletedCycle);
        Assert.Equal(new PlaneCoord(PlaneId, new GridCoord(1, 0)), world.GetEntityLocation(AutomaticId));
    }

    [Fact]
    public void InitiativeStepperReportsPlayerChoiceActorWithoutRequest()
    {
        var world = CreateWorld();
        world.SetActionControlSource(FirstPlayerId, EntityControlSource.PlayerChoice);
        var stepper = new InitiativePlayerChoiceStepper(new MovementService(), new ActionChoiceService(new MovementService()));

        var result = stepper.AdvanceUntilPlayerChoice(
            world,
            [FirstPlayerId],
            new Dictionary<EntityId, IEntityActionPlan>(),
            _ => null,
            startIndex: 0);

        Assert.Null(result.Request);
        Assert.False(result.CompletedCycle);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Contains("PlayerChoice actor firstPlayer has no Action Choice request", StringComparison.Ordinal));
    }

    private static WorldState CreateWorld()
    {
        var world = new WorldState();
        world.Planes.Add(PlaneId, new Plane(PlaneId, "Arena", 5, 1));
        for (var x = 0; x < 5; x++)
        {
            world.AddNode(PlaneId, new GridCoord(x, 0));
        }

        AddEntity(world, AutomaticId, "Automatic", new GridCoord(0, 0));
        AddEntity(world, FirstPlayerId, "First Player", new GridCoord(2, 0));
        AddEntity(world, SecondPlayerId, "Second Player", new GridCoord(4, 0));
        return world;
    }

    private static void AddEntity(WorldState world, EntityId entityId, string name, GridCoord coord)
    {
        var location = new PlaneCoord(PlaneId, coord);
        var nodeId = world.GetNodeId(location);
        world.Entities.Add(entityId, new Entity(entityId, name, nodeId, InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 1));
        world.Occupancy.Add(nodeId, entityId);
    }

    private static ActionPlanDescriptor MoveDescriptor() =>
        new(
            new ActionPlanId("move"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Move)]));

    private sealed class FixedEntityActionPlan(PlannedActionPlan plan) : IEntityActionPlan
    {
        public PlannedActionPlan PlanTurn(WorldState world, EntityId entityId, MovementService movement) => plan;
    }
}
