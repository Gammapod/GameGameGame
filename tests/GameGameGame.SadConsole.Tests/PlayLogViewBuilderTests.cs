using GameGameGame.Core;
using GameGameGame.Content;
using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Components;

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

    [Fact]
    public void HoverHitTestFindsVisibleInventoryEntityAndFiltersRecentSuccesses()
    {
        var plane = new PlaneId("room");
        var actor = new EntityId("actor");
        var chest = new EntityId("chest");
        var component = InventoryComponent(plane, chest);
        var log = ActionLogProjection.FromOutcomes([
            Outcome("old chest success", true, chest, plane, 1),
            Outcome("actor success", true, actor, plane, 2),
            Outcome("chest failure", false, chest, plane, 3),
            Outcome("new chest success", true, chest, plane, 4)
        ]);
        var cell = component.CellBounds(new GridCoord(1, 0));

        var hover = PlayEntityHoverHitTester.HitTest(cell.Left, cell.Top, [component], log, maxRecentSuccessRows: 2);

        Assert.NotNull(hover);
        Assert.Equal(chest, hover.EntityId);
        Assert.Equal("Chest", hover.EntityName);
        Assert.Equal(new GridCoord(1, 0), hover.Coord);
        Assert.Equal("new chest success", hover.LastSuccessfulLog);
    }

    [Fact]
    public void HoverTooltipPrefersBelowSubjectCenteredAndClampedInsideDrawableBounds()
    {
        var hover = new PlayEntityHoverInfo(
            "component",
            "Current place",
            new EntityId("chest"),
            "Chest",
            new PlaneId("room"),
            new GridCoord(1, 0),
            SadConsoleRect.FromSize(25, 2, 1, 1),
            "opened");
        var drawable = SadConsoleRect.FromSize(1, 1, 80, 12);

        var tooltip = PlayEntityHoverTooltipBuilder.Build(hover, drawable, mouseX: 19, mouseY: 2);
        var tooltipComponent = Assert.IsType<PlayEntityTooltipComponent>(tooltip);

        Assert.NotNull(tooltip);
        Assert.Equal("actor-pov-hover-tooltip", tooltip.Id);
        Assert.Equal(hover.CellBounds.Bottom + 1, tooltip.Bounds.Top);
        Assert.Equal(hover.CellBounds.Left + (hover.CellBounds.Width / 2) - (tooltip.Bounds.Width / 2), tooltip.Bounds.Left);
        Assert.True(tooltip.Bounds.Left >= drawable.Left);
        Assert.True(tooltip.Bounds.Top >= drawable.Top);
        Assert.True(tooltip.Bounds.Left + tooltip.Bounds.Width <= drawable.Left + drawable.Width);
        Assert.True(tooltip.Bounds.Bottom <= drawable.Bottom);
        Assert.Equal(["Chest opened"], tooltipComponent.BodyRows);
    }

    [Fact]
    public void HoverTooltipMovesToAvoidClippingAndUsesTranslucentBackgroundContract()
    {
        var hover = new PlayEntityHoverInfo(
            "component",
            "Current place",
            new EntityId("chest"),
            "Chest",
            new PlaneId("room"),
            new GridCoord(1, 0),
            SadConsoleRect.FromSize(19, 7, 1, 1),
            "Chest opened");
        var drawable = SadConsoleRect.FromSize(1, 1, 20, 8);

        var tooltip = Assert.IsType<PlayEntityTooltipComponent>(PlayEntityHoverTooltipBuilder.Build(hover, drawable, mouseX: 19, mouseY: 7));

        Assert.True(tooltip.Bounds.Left >= drawable.Left);
        Assert.True(tooltip.Bounds.Top >= drawable.Top);
        Assert.True(tooltip.Bounds.Left + tooltip.Bounds.Width <= drawable.Left + drawable.Width);
        Assert.True(tooltip.Bounds.Bottom <= drawable.Bottom);
        Assert.InRange(tooltip.BackgroundAlpha, (byte)1, (byte)254);
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

    private static InventorySpaceComponent InventoryComponent(PlaneId planeId, EntityId entityId)
    {
        var view = new InventorySpaceViewModel(
            "test-view",
            "Current place",
            planeId,
            Width: 3,
            Height: 1,
            InventorySpaceCellMetrics.Default,
            InventorySpaceViewport.Full(3, 1),
            new InventorySpaceBackdropLayer(new InventorySpaceVisualLayer('.', PresentationColor.Gray)),
            [new InventorySpaceEntityVisual(new GridCoord(1, 0), entityId, new InventorySpaceVisualLayer('c', PresentationColor.Earth), Accent: null, InventorySpaceVisualPlacement.Default, "Chest")],
            [],
            new InventorySpaceFrame(false, null, PresentationColor.Gray));

        return new InventorySpaceComponent(
            "test-grid",
            "Current place",
            SadConsoleRect.FromSize(10, 2, 3, 1),
            view,
            options: InventorySpaceRenderOptions.Bare);
    }
}
