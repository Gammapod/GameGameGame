using GameGameGame.Core;

namespace GameGameGame.Tests;

public sealed class SimulationTurnLogTests
{
    [Fact]
    public void LocalTurnReportListsPlayerThenActorsThenInertEntitiesWithPreviousActions()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var actionPlans = new Dictionary<EntityId, IEntityActionPlan>
        {
            [TestWorld.SlimeId] = new FixedEntityActionPlan(PlannedActionPlan.Single(new WaitAction()))
        };
        var turns = new TurnService(movement, actionPlans);

        turns.TakeActorTurnThenAdvance(
            world,
            TestWorld.PlayerId,
            PlannedActionPlan.Single(new MoveAction(Direction.West)));

        var report = LocalTurnOrderReport.Create(
            world,
            TestWorld.WorldPlaneId,
            actionPlans,
            TestWorld.PlayerId);

        Assert.Collection(
            report.Rows,
            row =>
            {
                Assert.Equal(0, row.Order);
                Assert.Equal(TestWorld.PlayerId, row.EntityId);
                Assert.Equal("Player", row.EntityName);
                Assert.Equal(LocalTurnParticipation.Player, row.Participation);
                Assert.Equal("Moved West", row.PreviousAction);
            },
            row =>
            {
                Assert.Equal(1, row.Order);
                Assert.Equal(TestWorld.SlimeId, row.EntityId);
                Assert.Equal(LocalTurnParticipation.Actor, row.Participation);
                Assert.Equal("Waited", row.PreviousAction);
            },
            row =>
            {
                Assert.Equal(-1, row.Order);
                Assert.Equal(TestWorld.RockId, row.EntityId);
                Assert.Equal(LocalTurnParticipation.Inert, row.Participation);
                Assert.Equal("----", row.PreviousAction);
            });
    }

    [Fact]
    public void LocalTurnReportCanFormatRowsAsPlainTextTable()
    {
        var report = new LocalTurnOrderReport(
            TestWorld.WorldPlaneId,
            [
                new LocalTurnOrderRow(0, TestWorld.PlayerId, "Player", '@', new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 2)), LocalTurnParticipation.Player, "Moved West"),
                new LocalTurnOrderRow(1, TestWorld.SlimeId, "Slime", 's', new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 1)), LocalTurnParticipation.Actor, "Waited"),
                new LocalTurnOrderRow(-1, TestWorld.RockId, "Rock", '*', new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 1)), LocalTurnParticipation.Inert, "----")
            ]);

        var lines = LocalTurnOrderReportFormatter.Format(report);

        Assert.Equal(
            [
                "Order# | Entity | Prev. Action",
                "0 | @ Player | Moved West",
                "1 | s Slime | Waited",
                "-- | * Rock | ----"
            ],
            lines);
    }

    [Fact]
    public void PreviousActionSummaryUsesPrimitiveActionInsteadOfResolvePlanWrapper()
    {
        var trace = new TraceNode("Resolve plan for Slime", TraceStatus.Failure);
        var planTrace = new TraceNode("Plan slimeWander", TraceStatus.Failure);
        var stepTrace = new TraceNode("Action Step MoveFacing", TraceStatus.Failure);
        var primitiveTrace = new TraceNode("Primitive MoveFacing", TraceStatus.Failure);
        stepTrace.Add(primitiveTrace);
        planTrace.Add(stepTrace);
        trace.Add(planTrace);

        var summary = TurnActionSummaryFormatter.FormatTrace(trace, succeeded: false);

        Assert.Equal("MoveFacing failed", summary);
    }

    private sealed class FixedEntityActionPlan(PlannedActionPlan plan) : IEntityActionPlan
    {
        public PlannedActionPlan PlanTurn(WorldState world, EntityId entityId, MovementService movement) => plan;
    }
}
