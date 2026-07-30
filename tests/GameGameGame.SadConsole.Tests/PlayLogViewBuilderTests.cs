using GameGameGame.Core;
using GameGameGame.Content;
using GameGameGame.SadConsoleApp;

namespace GameGameGame.SadConsole.Tests;

public sealed class PlayLogViewBuilderTests
{
    [Fact]
    public void BuildReturnsHonestEmptyRowForMissingLog()
    {
        var rows = PlayLogViewBuilder.Build(null, PlayLogScope.Global, currentLocationPlaneId: null, maxRows: 3);

        var row = Assert.Single(rows);
        Assert.Equal(PlayLogViewBuilder.EmptyText, row.Text);
        Assert.True(row.IsMuted);
    }

    [Fact]
    public void BuildGlobalIncludesSuccessesAndFailuresNewestFirstAndClipped()
    {
        var room = new PlaneId("room");
        var actor = new EntityId("actor");
        var log = ActionLogProjection.FromOutcomes([
            Outcome("old success", true, actor, room, 1),
            Outcome("middle failure", false, actor, room, 2),
            Outcome("new success", true, actor, room, 3)
        ]);

        var rows = PlayLogViewBuilder.Build(log, PlayLogScope.Global, currentLocationPlaneId: null, maxRows: 2);

        Assert.Equal(["T3: OK: new success (no turn)", "T2: FAIL: middle failure (no turn)"], rows.Select(row => row.Text));
        Assert.Equal([true, false], rows.Select(row => row.Succeeded));
        Assert.Equal([actor, actor], rows.Select(row => row.ActorId));
    }

    [Fact]
    public void BuildCurrentLocationFiltersByPlaneAnchor()
    {
        var currentRoom = new PlaneId("current-room");
        var otherRoom = new PlaneId("other-room");
        var actor = new EntityId("actor");
        var log = ActionLogProjection.FromOutcomes([
            Outcome("current success", true, actor, currentRoom, 1),
            Outcome("other failure", false, actor, otherRoom, 2),
            Outcome("current failure", false, actor, currentRoom, 3)
        ]);

        var rows = PlayLogViewBuilder.Build(log, PlayLogScope.CurrentLocation, currentRoom, maxRows: 5);

        Assert.Equal(["T3: FAIL: current failure (no turn)", "T1: OK: current success (no turn)"], rows.Select(row => row.Text));
    }

    [Fact]
    public void BuildCurrentLocationWithoutPlaneDoesNotFallBackToGlobalLog()
    {
        var log = ActionLogProjection.FromOutcomes([
            Outcome("global row", true, new EntityId("actor"), new PlaneId("room"), 1)
        ]);

        var rows = PlayLogViewBuilder.Build(log, PlayLogScope.CurrentLocation, currentLocationPlaneId: null, maxRows: 3);

        var row = Assert.Single(rows);
        Assert.Equal(PlayLogViewBuilder.EmptyText, row.Text);
        Assert.True(row.IsMuted);
    }

    [Fact]
    public void BuildReturnsNoRowsWhenNoRowsFit()
    {
        var rows = PlayLogViewBuilder.Build(null, PlayLogScope.Global, currentLocationPlaneId: null, maxRows: 0);

        Assert.Empty(rows);
    }

    [Fact]
    public void CurrentRegionActivityShowsOnlyCurrentPlaceSuccessesNewestFirst()
    {
        var room = new PlaneId("room");
        var otherRoom = new PlaneId("other-room");
        var actor = new EntityId("actor");
        var panel = Panel(room, [
            Outcome("old success", true, actor, room, 1),
            Outcome("current failure", false, actor, room, 2),
            Outcome("other success", true, actor, otherRoom, 3),
            Outcome("new success", true, actor, room, 4)
        ]);

        var rows = CurrentRegionActivityViewBuilder.Build(panel, maxRows: 3);

        Assert.Equal(["Recent successes", "OK: new success (no turn)", "OK: old success (no turn)"], rows);
    }

    [Fact]
    public void CurrentRegionActivityShowsQuietEmptyTextWhenNoSuccessesExist()
    {
        var room = new PlaneId("room");
        var panel = Panel(room, [Outcome("failure", false, new EntityId("actor"), room, 1)]);

        var rows = CurrentRegionActivityViewBuilder.Build(panel, maxRows: 3);

        Assert.Equal(["Recent successes", CurrentRegionActivityViewBuilder.EmptyText], rows);
    }

    private static ActionOutcome Outcome(string sentence, bool succeeded, EntityId actor, PlaneId planeId, int turnNumber) => new(
        turnNumber,
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

    private static EntityPanelProjection Panel(PlaneId planeId, IReadOnlyList<ActionOutcome> localLog) => new(
        new EntityId("panel"),
        "Panel",
        '#',
        PresentationColor.Gray,
        new PlaneCoord(planeId, new GridCoord(0, 0)),
        new EntityContainmentPath(new EntityId("panel"), EntityContainmentPathStatus.Complete, [], [], []),
        [],
        new EntityPanelActionStateProjection(null, null, new Dictionary<int, EntityId>()),
        null,
        new InventoryInspectionGrid(planeId, 3, 3, []),
        [],
        localLog);
}
