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
    public void ActionChoiceRequestExposesEnterTargetsFromAuthoredEnterStep()
    {
        var world = TestWorld.CreateWorld();
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Aperture = 20 };
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var plan = MovePlan(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.EnterTarget));
        var service = new ActionChoiceService(new MovementService());

        var request = service.CreateRequest(world, TestWorld.PlayerId, plan);

        var choice = Assert.Single(request!.Choices);
        Assert.Equal(ActionChoiceKind.Enter, choice.Kind);
        var target = Assert.Single(choice.EntityOptions, option => option.TargetId == TestWorld.SlimeId);
        Assert.True(target.CanExecute);
    }

    [Fact]
    public void ActionChoiceRequestExposesExitDirectionsFromAuthoredExitStep()
    {
        var movement = new MovementService();
        var world = TestWorld.CreateWorld();
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var plan = MovePlan(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.ExitFacing));
        var service = new ActionChoiceService(movement);

        var request = service.CreateRequest(world, TestWorld.PlayerId, plan);

        var choice = Assert.Single(request!.Choices);
        Assert.Equal(ActionChoiceKind.Exit, choice.Kind);
        var south = Assert.Single(choice.DirectionOptions, option => option.Direction == Direction.South);
        Assert.True(south.CanExecute);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 2)), south.Destination);
    }

    [Fact]
    public void ActionChoiceRequestExposesNonParameterizedAuthoredStepsForCoreSubmission()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var plan = MovePlan(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Backstep));
        var service = new ActionChoiceService(new MovementService());

        var request = service.CreateRequest(world, TestWorld.PlayerId, plan);

        var choice = Assert.Single(request!.Choices);
        Assert.Equal(ActionChoiceKind.AuthoredStep, choice.Kind);
        Assert.Equal(0, choice.StepIndex);
        Assert.Empty(choice.DirectionOptions);
        Assert.Empty(choice.EntityOptions);
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
    public void SubmitEnterChoiceUsesSelectedTarget()
    {
        var world = TestWorld.CreateWorld();
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Aperture = 20 };
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var plan = MovePlan(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.EnterTarget));
        var service = new ActionChoiceService(new MovementService());
        var request = service.CreateRequest(world, TestWorld.PlayerId, plan)!;

        var result = service.SubmitEnterChoice(world, request, TestWorld.SlimeId, new Dictionary<EntityId, IEntityActionPlan>());

        Assert.True(result.Succeeded);
        Assert.True(result.AdvancedTurn);
        Assert.Equal(ControlledActorCommandKind.Enter, result.Kind);
        Assert.Equal(TestWorld.SlimeId, result.TargetId);
        Assert.Equal(TestWorld.SlimeInventoryPlaneId, world.GetEntityLocation(TestWorld.PlayerId).PlaneId);
    }

    [Fact]
    public void SubmitExitChoiceUsesSelectedDirection()
    {
        var movement = new MovementService();
        var world = TestWorld.CreateWorld();
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var plan = MovePlan(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.ExitFacing));
        var service = new ActionChoiceService(movement);
        var request = service.CreateRequest(world, TestWorld.PlayerId, plan)!;

        var result = service.SubmitExitChoice(world, request, Direction.South, new Dictionary<EntityId, IEntityActionPlan>());

        Assert.True(result.Succeeded);
        Assert.True(result.AdvancedTurn);
        Assert.Equal(ControlledActorCommandKind.Exit, result.Kind);
        Assert.Equal(Direction.South, result.Direction);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 2)), world.GetEntityLocation(TestWorld.PlayerId));
    }

    [Fact]
    public void SubmitAuthoredStepChoiceExecutesThroughCoreServiceAndAdvancesWhenConsuming()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.East);
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var step = new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Backstep);
        var plan = MovePlan(step);
        var service = new ActionChoiceService(new MovementService());
        var request = service.CreateRequest(world, TestWorld.PlayerId, plan)!;

        var result = service.SubmitAuthoredStepChoice(world, request, 0, step);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(1, world.TurnNumber);
        Assert.Equal(new GridCoord(0, 2), world.GetEntityLocation(TestWorld.PlayerId).Coord);
        Assert.Equal(Direction.West, world.GetActionFacing(TestWorld.PlayerId));
        Assert.NotNull(world.LastTrace);
    }

    [Fact]
    public void SubmitAuthoredStepChoiceThroughHistoryRecordsActorInterval()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.East);
        world.SetActionControlSource(TestWorld.PlayerId, EntityControlSource.PlayerChoice);
        var history = SimulationHistorySession.Start(world, TestWorld.PlayerId, TestWorld.WorldPlaneId);
        var step = new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Backstep);
        var plan = MovePlan(step);
        var service = new ActionChoiceService(new MovementService());
        var request = service.CreateRequest(world, TestWorld.PlayerId, plan)!;

        var result = history.SubmitAuthoredActionStepChoice(service, request, 0, step);

        Assert.True(result.Succeeded);
        Assert.Equal(1, history.CurrentFrameIndex);
        Assert.Equal(1, world.TurnNumber);
        var interval = Assert.Single(history.Intervals);
        var log = Assert.Single(interval.ActorLogs);
        Assert.Equal(TestWorld.PlayerId, log.ActorId);
        Assert.Equal(nameof(ActionPlanBehaviorStepKind.Backstep), log.Summary);
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
