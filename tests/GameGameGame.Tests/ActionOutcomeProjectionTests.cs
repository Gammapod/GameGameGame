using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class ActionOutcomeProjectionTests
{
    [Fact]
    public void ActionOutcomeProjectionRendersSuccessfulPickupFromStructuredCommandResult()
    {
        var world = TestWorld.CreateWorld();
        var service = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());
        var destination = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0));

        var commandResult = service.Execute(world, TestWorld.PlayerId, ControlledActorCommand.Pickup(TestWorld.SlimeId, destination));
        var outcome = ActionOutcomeProjection.FromCommandResult(world, commandResult);

        Assert.Equal(TestWorld.PlayerId, outcome.ActorId);
        Assert.Equal(TestWorld.SlimeId, outcome.TargetId);
        Assert.Equal(destination, outcome.Destination);
        Assert.True(outcome.Succeeded);
        Assert.Equal("pickup", outcome.ActionKind);
        Assert.Equal("Player picked up Slime", outcome.Sentence);
        Assert.Contains(TestWorld.PlayerId, outcome.AnchorEntityIds);
        Assert.Contains(TestWorld.SlimeId, outcome.AnchorEntityIds);
        Assert.Equal(commandResult.Trace, outcome.Trace);
    }

    [Fact]
    public void ActionOutcomeProjectionRendersFailedMoveWithoutParsingDisplaySummary()
    {
        var world = TestWorld.CreateWorld();
        var service = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        var commandResult = service.Execute(world, TestWorld.PlayerId, ControlledActorCommand.Move(Direction.North));
        var outcome = ActionOutcomeProjection.FromCommandResult(world, commandResult);

        Assert.False(outcome.Succeeded);
        Assert.Equal("move", outcome.ActionKind);
        Assert.Equal(Direction.North, outcome.Direction);
        Assert.Equal(FailureReason.InvalidPlacement, outcome.FailureReason);
        Assert.StartsWith("Player tried to move North, but", outcome.Sentence, StringComparison.Ordinal);
        Assert.Null(commandResult.TurnReport);
    }

    [Fact]
    public void ActionLogProjectionFiltersOutcomesByEntityAndPlaneAnchors()
    {
        var world = TestWorld.CreateWorld();
        var service = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());
        var destination = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0));
        var outcome = ActionOutcomeProjection.FromCommandResult(
            world,
            service.Execute(world, TestWorld.PlayerId, ControlledActorCommand.Pickup(TestWorld.SlimeId, destination)));

        var log = ActionLogProjection.FromOutcomes([outcome]);

        Assert.Single(log.Chronological);
        Assert.Contains(outcome, log.ForEntity(TestWorld.PlayerId));
        Assert.Contains(outcome, log.ForEntity(TestWorld.SlimeId));
        Assert.Contains(outcome, log.ForPlane(TestWorld.PlayerInventoryPlaneId));
        Assert.Empty(log.ForEntity(TestWorld.RockId));
    }

    [Fact]
    public void ActionLogQuerySelectReturnsEmptyForMissingLog()
    {
        var rows = ActionLogQueryService.Select(
            null,
            new ActionLogQuery(Order: ActionLogOrder.NewestFirst, MaxRows: 5));

        Assert.Empty(rows);
    }

    [Fact]
    public void ActionLogQuerySelectOrdersAndClipsAfterFiltering()
    {
        var actor = new EntityId("actor");
        var outcomes = new[]
        {
            Outcome("old success", true, actor, new PlaneId("plane")),
            Outcome("middle failure", false, actor, new PlaneId("plane")),
            Outcome("new success", true, actor, new PlaneId("plane"))
        };
        var log = ActionLogProjection.FromOutcomes(outcomes);

        var rows = ActionLogQueryService.Select(
            log,
            new ActionLogQuery(
                Succeeded: true,
                Order: ActionLogOrder.NewestFirst,
                MaxRows: 1));

        Assert.Equal(["new success"], rows.Select(row => row.Sentence));
        Assert.Equal(["old success", "middle failure", "new success"], log.Chronological.Select(row => row.Sentence));
    }

    [Fact]
    public void ActionLogQuerySelectFiltersByFailureEntityAndPlaneAnchors()
    {
        var actor = new EntityId("actor");
        var target = new EntityId("target");
        var other = new EntityId("other");
        var room = new PlaneId("room");
        var otherPlane = new PlaneId("other-plane");
        var actorFailure = Outcome("actor failed", false, actor, room);
        var targetFailure = Outcome("target failed", false, target, room);
        var otherFailure = Outcome("other failed", false, other, otherPlane);
        var success = Outcome("actor succeeded", true, actor, room);
        var log = ActionLogProjection.FromOutcomes([actorFailure, targetFailure, otherFailure, success]);

        var rows = ActionLogQueryService.Select(
            log,
            new ActionLogQuery(
                EntityAnchors: new HashSet<EntityId> { actor, target },
                PlaneAnchors: new HashSet<PlaneId> { room },
                Succeeded: false));

        Assert.Equal(["actor failed", "target failed"], rows.Select(row => row.Sentence));
    }

    [Fact]
    public void ActionLogQuerySelectDeduplicatesRowsMatchingEntityAndPlaneAnchors()
    {
        var actor = new EntityId("actor");
        var room = new PlaneId("room");
        var matchingBoth = Outcome("matching both", true, actor, room);
        var entityOnly = new ActionOutcome(
            null,
            actor,
            "actor",
            "test",
            true,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "entity only",
            new HashSet<EntityId> { actor },
            new HashSet<PlaneId>(),
            TraceNode.Info("entity only"));
        var planeOnly = Outcome("plane only", true, new EntityId("other"), room);
        var log = ActionLogProjection.FromOutcomes([matchingBoth, entityOnly, planeOnly]);

        var rows = ActionLogQueryService.Select(
            log,
            new ActionLogQuery(
                EntityAnchors: new HashSet<EntityId> { actor },
                PlaneAnchors: new HashSet<PlaneId> { room }));

        Assert.Equal(["matching both", "entity only", "plane only"], rows.Select(row => row.Sentence));
    }

    [Fact]
    public void ActionOutcomeProjectionExposesSuccessfulPickupApertureDegree()
    {
        var world = TestWorld.CreateWorld();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 11 };
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Bulk = 10 };
        var service = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        var commandResult = service.Execute(
            world,
            TestWorld.PlayerId,
            ControlledActorCommand.Pickup(TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        var outcome = ActionOutcomeProjection.FromCommandResult(world, commandResult);

        var criterion = Assert.Single(outcome.SuccessCriteria, fact => fact.Kind == ActionSuccessCriterionKind.Aperture);
        Assert.True(criterion.Satisfied);
        Assert.Equal(10, criterion.RequiredValue);
        Assert.Equal(11, criterion.AvailableValue);
        Assert.Equal(1.1m, criterion.SuccessRatio);
        Assert.Equal(TestWorld.SlimeId, criterion.SubjectEntityId);
        Assert.Equal(TestWorld.PlayerId, criterion.LimitEntityId);
    }

    private static ActionOutcome Outcome(string sentence, bool succeeded, EntityId actor, PlaneId planeId) => new(
        null,
        actor,
        actor.Value,
        "test",
        succeeded,
        null,
        null,
        null,
        null,
        null,
        succeeded ? null : FailureReason.MoveBlocked,
        null,
        sentence,
        new HashSet<EntityId> { actor },
        new HashSet<PlaneId> { planeId },
        TraceNode.Info(sentence));

    [Fact]
    public void ActionOutcomeProjectionExposesFailedEnterApertureDegree()
    {
        var world = TestWorld.CreateWorld();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Bulk = 10 };
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Aperture = 9 };
        var service = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        var commandResult = service.Execute(world, TestWorld.PlayerId, ControlledActorCommand.Enter(TestWorld.SlimeId));
        var outcome = ActionOutcomeProjection.FromCommandResult(world, commandResult);

        Assert.False(outcome.Succeeded);
        var criterion = Assert.Single(outcome.SuccessCriteria, fact => fact.Kind == ActionSuccessCriterionKind.Aperture);
        Assert.False(criterion.Satisfied);
        Assert.Equal(10, criterion.RequiredValue);
        Assert.Equal(9, criterion.AvailableValue);
        Assert.Equal(0.9m, criterion.SuccessRatio);
        Assert.Equal(TestWorld.PlayerId, criterion.SubjectEntityId);
        Assert.Equal(TestWorld.SlimeId, criterion.LimitEntityId);
    }
}
