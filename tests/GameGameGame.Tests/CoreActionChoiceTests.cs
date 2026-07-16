using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class CoreActionChoiceTests
{
    [Fact]
    public void ActionChoiceRequestRequiresPlayerChoiceControlSource()
    {
        var world = TestWorld.CreateWorld();
        var plan = MovePlan(
            new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Move, DirectionMode: ActionPlanMoveDirectionMode.Forward));
        var service = new ActionChoiceService(new MovementService());

        var automatic = service.CreateRequest(world, TestWorld.PlayerId, plan);
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var playerChoice = service.CreateRequest(world, TestWorld.PlayerId, plan);

        Assert.Null(automatic);
        Assert.NotNull(playerChoice);
    }

    [Fact]
    public void ActionChoiceRequestCoalescesCanonicalMoveStepsIntoOneEightDirectionChoice()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var plan = MovePlan(
            new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Move, DirectionMode: ActionPlanMoveDirectionMode.Forward),
            new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Move, DirectionMode: ActionPlanMoveDirectionMode.Back));
        var service = new ActionChoiceService(new MovementService());

        var request = service.CreateRequest(world, TestWorld.PlayerId, plan);

        var choice = Assert.Single(request!.Choices);
        Assert.Equal(ActionChoiceKind.Move, choice.Kind);
        Assert.Equal(0, choice.StepIndex);
        Assert.Equal(8, choice.DirectionOptions.Count);
        Assert.Contains(choice.DirectionOptions, option => option.Direction == Direction.NorthEast && option.Destination == new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 1)));
    }

    [Fact]
    public void SubmitMoveChoiceSuccessAdvancesAndSetsFacing()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var plan = MovePlan(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Move, DirectionMode: ActionPlanMoveDirectionMode.Forward));
        var service = new ActionChoiceService(new MovementService());
        var request = service.CreateRequest(world, TestWorld.PlayerId, plan)!;

        var result = service.SubmitMoveChoice(world, request, Direction.SouthEast, new Dictionary<EntityId, IEntityActionPlan>());

        Assert.True(result.Succeeded);
        Assert.True(result.AdvancedTurn);
        Assert.Equal(1, world.TurnNumber);
        Assert.Equal(new GridCoord(2, 3), world.GetEntityLocation(TestWorld.PlayerId).Coord);
        Assert.Equal(Direction.SouthEast, world.GetActionFacing(TestWorld.PlayerId));
    }

    [Fact]
    public void SubmitMoveChoiceFailureLogsWithoutAdvancing()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.West);
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var history = SimulationHistorySession.Start(world, TestWorld.PlayerId, TestWorld.WorldPlaneId);
        var plan = MovePlan(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Move, DirectionMode: ActionPlanMoveDirectionMode.Forward));
        var service = new ActionChoiceService(new MovementService());
        var request = service.CreateRequest(world, TestWorld.PlayerId, plan)!;

        var result = history.SubmitActionChoice(service, request, Direction.North, new Dictionary<EntityId, IEntityActionPlan>());

        Assert.False(result.Succeeded);
        Assert.False(result.AdvancedTurn);
        Assert.Equal(0, history.CurrentFrameIndex);
        Assert.Equal(0, world.TurnNumber);
        Assert.Equal(Direction.West, world.GetActionFacing(TestWorld.PlayerId));
        Assert.Single(history.CurrentFrameLogEntries);
    }

    private static ActionPlanDescriptor MovePlan(params ActionPlanBehaviorStepDescriptor[] steps) =>
        new(new ActionPlanId("choice-plan"), [], Behavior: new ActionPlanBehaviorDescriptor(steps));
}
