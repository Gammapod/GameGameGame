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
    public void ActionChoiceRequestExposesPickupTargetsAndInventoryDestinationsFromAuthoredPickupStep()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var plan = MovePlan(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget));
        var service = new ActionChoiceService(new MovementService());

        var request = service.CreateRequest(world, TestWorld.PlayerId, plan);

        var choice = Assert.Single(request!.Choices);
        Assert.Equal(ActionChoiceKind.Pickup, choice.Kind);
        Assert.Equal(0, choice.StepIndex);
        var source = Assert.Single(choice.EntityOptions, option => option.TargetId == TestWorld.SlimeId);
        Assert.True(source.CanExecute);
        var diagonalSource = Assert.Single(choice.EntityOptions, option => option.TargetId == TestWorld.RockId);
        Assert.True(diagonalSource.CanExecute);
        var destination = Assert.Single(choice.Destinations(TestWorld.SlimeId), option => option.Destination == new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));
        Assert.True(destination.CanExecute);
    }

    [Fact]
    public void ActionChoiceRequestTreatsTransformAliasesAsPickupAndDropChoices()
    {
        var movement = new MovementService();
        var world = TestWorld.CreateWorld();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var plan = MovePlan(
            new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.TransformAdjacentToInventory),
            new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.TransformInventoryToAdjacent));
        var service = new ActionChoiceService(movement);

        var request = service.CreateRequest(world, TestWorld.PlayerId, plan);

        Assert.NotNull(request);
        var pickup = Assert.Single(request!.Choices, choice => choice.Kind == ActionChoiceKind.Pickup);
        Assert.Equal(0, pickup.StepIndex);
        Assert.Contains(pickup.EntityOptions, option => option.TargetId == TestWorld.SlimeId && option.CanExecute);
        var drop = Assert.Single(request.Choices, choice => choice.Kind == ActionChoiceKind.Drop);
        Assert.Equal(1, drop.StepIndex);
        Assert.Contains(drop.EntityOptions, option => option.TargetId == TestWorld.RockId && option.CanExecute);
    }

    [Fact]
    public void ActionChoiceRequestExposesDropSourcesAndAdjacentDestinationsFromAuthoredDropStep()
    {
        var movement = new MovementService();
        var world = TestWorld.CreateWorld();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var plan = MovePlan(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DropFacing));
        var service = new ActionChoiceService(movement);

        var request = service.CreateRequest(world, TestWorld.PlayerId, plan);

        var choice = Assert.Single(request!.Choices);
        Assert.Equal(ActionChoiceKind.Drop, choice.Kind);
        Assert.Equal(0, choice.StepIndex);
        var source = Assert.Single(choice.EntityOptions, option => option.TargetId == TestWorld.RockId);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), source.Source);
        var destination = Assert.Single(choice.Destinations(TestWorld.RockId), option => option.Destination == new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)));
        Assert.True(destination.CanExecute);
        Assert.DoesNotContain(choice.Destinations(TestWorld.RockId), option => option.Destination == new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4)));
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

    [Fact]
    public void SubmitPickupChoiceUsesSelectedTargetAndInventorySlot()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var plan = MovePlan(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget));
        var service = new ActionChoiceService(new MovementService());
        var request = service.CreateRequest(world, TestWorld.PlayerId, plan)!;
        var destination = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 0));

        var result = service.SubmitPickupChoice(world, request, TestWorld.SlimeId, destination, new Dictionary<EntityId, IEntityActionPlan>());

        Assert.True(result.Succeeded);
        Assert.True(result.AdvancedTurn);
        Assert.Equal(1, world.TurnNumber);
        Assert.Equal(destination, world.GetEntityLocation(TestWorld.SlimeId));
        Assert.Equal(ControlledActorCommandKind.Pickup, result.Kind);
        Assert.Equal(TestWorld.SlimeId, result.TargetId);
        Assert.Equal(destination, result.Destination);
    }

    [Fact]
    public void SubmitDropChoiceUsesSelectedCarriedEntityAndAdjacentDestination()
    {
        var movement = new MovementService();
        var world = TestWorld.CreateWorld();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var plan = MovePlan(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DropFacing));
        var service = new ActionChoiceService(movement);
        var request = service.CreateRequest(world, TestWorld.PlayerId, plan)!;
        var destination = new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2));

        var result = service.SubmitDropChoice(world, request, TestWorld.RockId, destination, new Dictionary<EntityId, IEntityActionPlan>());

        Assert.True(result.Succeeded);
        Assert.True(result.AdvancedTurn);
        Assert.Equal(1, world.TurnNumber);
        Assert.Equal(destination, world.GetEntityLocation(TestWorld.RockId));
        Assert.Equal(ControlledActorCommandKind.Drop, result.Kind);
        Assert.Equal(TestWorld.RockId, result.TargetId);
        Assert.Equal(destination, result.Destination);
    }

    [Fact]
    public void SubmitPickupChoiceThroughHistoryAdvancesAndLogsStructuredOutcome()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var history = SimulationHistorySession.Start(world, TestWorld.PlayerId, TestWorld.WorldPlaneId);
        var plan = MovePlan(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget));
        var service = new ActionChoiceService(new MovementService());
        var request = service.CreateRequest(world, TestWorld.PlayerId, plan)!;
        var destination = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0));

        var result = history.SubmitPickupActionChoice(service, request, TestWorld.SlimeId, destination, new Dictionary<EntityId, IEntityActionPlan>());

        Assert.True(result.Succeeded);
        Assert.True(result.AdvancedTurn);
        Assert.Equal(1, history.CurrentFrameIndex);
        Assert.Equal(1, world.TurnNumber);
        Assert.Equal(destination, world.GetEntityLocation(TestWorld.SlimeId));
        var interval = Assert.Single(history.Intervals);
        Assert.Equal(ControlledActorCommandKind.Pickup, interval.ControlledResult?.Kind);
        Assert.Empty(history.CurrentFrameLogEntries);
    }

    [Fact]
    public void SubmitDropChoiceThroughHistoryFailureLogsWithoutAdvancing()
    {
        var movement = new MovementService();
        var world = TestWorld.CreateWorld();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var history = SimulationHistorySession.Start(world, TestWorld.PlayerId, TestWorld.WorldPlaneId);
        var plan = MovePlan(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DropFacing));
        var service = new ActionChoiceService(movement);
        var request = service.CreateRequest(world, TestWorld.PlayerId, plan)!;
        var blockedDestination = world.GetEntityLocation(TestWorld.SlimeId);

        var result = history.SubmitDropActionChoice(service, request, TestWorld.RockId, blockedDestination, new Dictionary<EntityId, IEntityActionPlan>());

        Assert.False(result.Succeeded);
        Assert.False(result.AdvancedTurn);
        Assert.Equal(0, history.CurrentFrameIndex);
        Assert.Equal(0, world.TurnNumber);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
        Assert.Empty(history.Intervals);
        var entry = Assert.Single(history.CurrentFrameLogEntries);
        Assert.Equal(ControlledActorCommandKind.Drop, entry.ControlledResult.Kind);
    }

    private static ActionPlanDescriptor MovePlan(params ActionPlanBehaviorStepDescriptor[] steps) =>
        new(new ActionPlanId("choice-plan"), [], Behavior: new ActionPlanBehaviorDescriptor(steps));
}
