using GameGameGame.Content;
using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class ScenarioBrowserScreenModelTests
{
    [Fact]
    public void ScenarioBrowserShowsWorkspaceDebugRoomCatalogEntry()
    {
        var catalog = WorkspaceScenarioCatalogService.BuildDefaultCatalog();

        var model = new ScenarioBrowserScreenModel(catalog);

        Assert.Contains(model.Entries, entry => entry.ScenarioId == "debug-room" && entry.IsWorkspaceBacked);
    }

    [Fact]
    public void ScenarioBrowserSelectionOpensPlayEditSelectorThenCreatesLaunchRequest()
    {
        var catalog = WorkspaceScenarioCatalogService.BuildDefaultCatalog();
        var model = new ScenarioBrowserScreenModel(catalog);
        while (model.SelectedEntry?.ScenarioId != "debug-room")
        {
            model.Handle(ScenarioBrowserCommand.Down);
        }

        var open = model.Handle(ScenarioBrowserCommand.Select);

        Assert.Equal(ScenarioBrowserResultKind.Stay, open.Kind);
        Assert.True(model.ActionSelectorOpen);
        Assert.Equal(ScenarioBrowserActionOption.Play, model.SelectedActionOption);

        var result = model.Handle(ScenarioBrowserCommand.Select);
        Assert.Equal(ScenarioBrowserResultKind.LaunchRequested, result.Kind);
        Assert.Equal("debug-room", result.Entry?.ScenarioId);
        Assert.NotNull(result.Entry?.EntryId);
    }

    [Fact]
    public void ScenarioBrowserPreservesDiagnosticsAsDisplayData()
    {
        var catalog = new WorkspaceScenarioCatalogResult(
            new ContentWorkspace([]),
            [],
            ["Workspace content path missing.yaml was not found."]);

        var model = new ScenarioBrowserScreenModel(catalog);

        Assert.Equal(["Workspace content path missing.yaml was not found."], model.Diagnostics);
        Assert.Empty(model.Entries);
        Assert.Equal(ScenarioBrowserResultKind.Stay, model.Handle(ScenarioBrowserCommand.Select).Kind);
    }

    [Fact]
    public void ScenarioBrowserViewportKeepsSelectedScenarioVisibleWhileScrolling()
    {
        var catalog = CatalogWithEntries(20);
        var model = new ScenarioBrowserScreenModel(catalog);

        for (var i = 0; i < 12; i++)
        {
            model.Handle(ScenarioBrowserCommand.Down);
        }

        var viewport = model.Viewport(5);

        Assert.Equal(12, model.SelectedIndex);
        Assert.InRange(viewport.SelectedVisibleIndex, 0, 4);
        Assert.Contains(model.SelectedEntry, viewport.Entries);
        Assert.True(viewport.HasItemsAbove);
        Assert.True(viewport.HasItemsBelow);
        Assert.Equal("13/20", viewport.PositionSummary(model.SelectedIndex, model.Entries.Count));
    }

    [Fact]
    public void ScenarioBrowserViewportReportsBottomWithoutScrollingPastEnd()
    {
        var catalog = CatalogWithEntries(8);
        var model = new ScenarioBrowserScreenModel(catalog);

        for (var i = 0; i < 20; i++)
        {
            model.Handle(ScenarioBrowserCommand.Down);
        }

        var viewport = model.Viewport(3);

        Assert.Equal(7, model.SelectedIndex);
        Assert.Equal(5, viewport.StartIndex);
        Assert.Equal(8, viewport.EndIndexExclusive);
        Assert.Equal(2, viewport.SelectedVisibleIndex);
        Assert.True(viewport.HasItemsAbove);
        Assert.False(viewport.HasItemsBelow);
    }

    [Fact]
    public void ScenarioBrowserMouseScrollDownMovesSelectionUpWithoutOwningLaunchSemantics()
    {
        var model = new ScenarioBrowserScreenModel(CatalogWithEntries(5), selectedIndex: 2);

        var result = model.Scroll(-1);

        Assert.Equal(ScenarioBrowserResultKind.Stay, result.Kind);
        Assert.Equal(1, model.SelectedIndex);
        Assert.Equal("scenario-01", model.SelectedEntry?.ScenarioId);
    }

    [Fact]
    public void ScenarioBrowserMouseScrollUpMovesSelectionDownWithoutOwningLaunchSemantics()
    {
        var model = new ScenarioBrowserScreenModel(CatalogWithEntries(5), selectedIndex: 2);

        model.Scroll(1);

        Assert.Equal(3, model.SelectedIndex);
        Assert.Equal("scenario-03", model.SelectedEntry?.ScenarioId);
    }

    [Fact]
    public void ScenarioBrowserMouseClickSelectsVisibleRowAndOpensSelector()
    {
        var model = new ScenarioBrowserScreenModel(CatalogWithEntries(10));
        for (var i = 0; i < 6; i++) model.Handle(ScenarioBrowserCommand.Down);
        var viewport = model.Viewport(5);

        var result = model.SelectVisibleRow(viewport, 4, launch: true);

        Assert.Equal(ScenarioBrowserResultKind.Stay, result.Kind);
        Assert.Equal(viewport.StartIndex + 4, model.SelectedIndex);
        Assert.True(model.ActionSelectorOpen);
        Assert.Equal(ScenarioBrowserActionOption.Play, model.SelectedActionOption);
    }

    [Fact]
    public void ScenarioBrowserMouseHoverHighlightsVisibleRowWithoutChangingSelectionOrViewport()
    {
        var model = new ScenarioBrowserScreenModel(CatalogWithEntries(10));
        for (var i = 0; i < 6; i++) model.Handle(ScenarioBrowserCommand.Down);
        var viewport = model.Viewport(5);

        var result = model.HoverVisibleRow(viewport, 4);

        Assert.Equal(ScenarioBrowserResultKind.Stay, result.Kind);
        Assert.Equal(6, model.SelectedIndex);
        Assert.Equal(viewport.StartIndex + 4, model.HoveredIndex);
        Assert.Equal(viewport.StartIndex, model.Viewport(5).StartIndex);
    }

    [Fact]
    public void ScenarioBrowserTracksKeyboardAndMouseInputModes()
    {
        var model = new ScenarioBrowserScreenModel(CatalogWithEntries(5));

        Assert.Equal(FrontendInputMode.Keyboard, model.ActiveInputMode);

        model.Scroll(1);
        Assert.Equal(FrontendInputMode.Mouse, model.ActiveInputMode);
        Assert.Contains("Input: Mouse", model.Footer);

        model.Handle(ScenarioBrowserCommand.Up);
        Assert.Equal(FrontendInputMode.Keyboard, model.ActiveInputMode);
        Assert.Contains("Input: Keyboard", model.Footer);
    }

    [Fact]
    public void ScenarioBrowserTracksGamepadInputModePlaceholder()
    {
        var model = new ScenarioBrowserScreenModel(CatalogWithEntries(5));

        model.HandleGamepad(ScenarioBrowserCommand.Down);

        Assert.Equal(FrontendInputMode.Gamepad, model.ActiveInputMode);
        Assert.Contains("Input: Gamepad", model.Footer);
    }

    [Fact]
    public void ScenarioBrowserEditOptionIsNonfunctionalPlaceholder()
    {
        var model = new ScenarioBrowserScreenModel(CatalogWithEntries(1));
        model.Handle(ScenarioBrowserCommand.Select);
        model.Handle(ScenarioBrowserCommand.Down);

        var result = model.Handle(ScenarioBrowserCommand.Select);

        Assert.Equal(ScenarioBrowserResultKind.Stay, result.Kind);
        Assert.Contains("placeholder", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(model.ActionSelectorOpen);
        Assert.Equal(ScenarioBrowserActionOption.Edit, model.SelectedActionOption);
    }

    [Fact]
    public void ScenarioBrowserModalFocusPreventsMouseSelectingUnderlyingScenarioRows()
    {
        var model = new ScenarioBrowserScreenModel(CatalogWithEntries(5));
        var open = model.Handle(ScenarioBrowserCommand.Select);
        var viewport = model.Viewport(5);

        var result = model.SelectVisibleRow(viewport, 3, launch: true);

        Assert.Equal(ScenarioBrowserResultKind.Stay, open.Kind);
        Assert.True(model.ActionSelectorOpen);
        Assert.Equal(0, model.SelectedIndex);
        Assert.Equal("scenario-00", model.SelectedEntry?.ScenarioId);
        Assert.Contains("focused", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScenarioBrowserModalFocusPreventsMouseHoverAndScrollChangingUnderlyingList()
    {
        var model = new ScenarioBrowserScreenModel(CatalogWithEntries(5), selectedIndex: 2);
        model.Handle(ScenarioBrowserCommand.Select);
        var viewport = model.Viewport(5);

        var hover = model.HoverVisibleRow(viewport, 4);
        var scroll = model.Scroll(1);

        Assert.True(model.ActionSelectorOpen);
        Assert.Equal(2, model.SelectedIndex);
        Assert.Null(model.HoveredIndex);
        Assert.Contains("focused", hover.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("focused", scroll.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static WorkspaceScenarioCatalogResult CatalogWithEntries(int count) => new(
        new ContentWorkspace([]),
        Enumerable.Range(0, count)
            .Select(index => new WorkspaceScenarioCatalogEntry(
                $"workspace:scenario-{index:00}",
                $"scenario-{index:00}",
                $"Scenario {index:00}",
                null,
                null,
                [],
                null,
                null,
                WorkspaceScenarioLaunchKind.Workspace))
            .ToList(),
        []);
}
