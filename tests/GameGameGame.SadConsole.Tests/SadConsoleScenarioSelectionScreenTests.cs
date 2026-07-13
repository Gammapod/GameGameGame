using GameGameGame.Content;
using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Screens;

namespace GameGameGame.SadConsole.Tests;

public sealed class SadConsoleScenarioSelectionScreenTests
{
    [Fact]
    public void ScenarioSelectionStartsWithFocusedScenarioListOnly()
    {
        var screen = ScenarioSelectionScreen.FromCatalog(DemoCatalog());

        var components = screen.Components();

        Assert.False(screen.CommandPanelOpen);
        Assert.Equal("Scenario One", screen.SelectedScenario?.Name);
        Assert.Single(components);
        Assert.Equal("scenario-list", components[0].Id);
        Assert.Equal(UiComponentState.Focused, components[0].State);
        Assert.Contains("Enter opens Play/Edit", screen.FooterText());
    }

    [Fact]
    public void ScenarioSelectionMovesScenarioAndOpensCommandPanel()
    {
        var screen = ScenarioSelectionScreen.FromCatalog(DemoCatalog());

        var moved = screen.Handle(UiComponentCommand.Down);
        var opened = screen.Handle(UiComponentCommand.Select);

        Assert.Equal(ScenarioSelectionResultKind.Stay, moved.Kind);
        Assert.Equal("Scenario Two", screen.SelectedScenario?.Name);
        Assert.Equal(ScenarioSelectionResultKind.Stay, opened.Kind);
        Assert.True(screen.CommandPanelOpen);
        Assert.Equal(2, screen.Components().Count);
        Assert.Equal("scenario-command-panel", screen.Components()[1].Id);
        Assert.Equal(UiComponentState.Focused, screen.Components()[1].State);
        Assert.Contains("Command panel focused", screen.FooterText());
    }

    [Fact]
    public void ScenarioSelectionUsesOverlayCommandPanelWithoutShrinkingScenarioList()
    {
        var screen = ScenarioSelectionScreen.FromCatalog(DemoCatalog());
        var initialList = screen.ScenarioListComponent();

        screen.Handle(UiComponentCommand.Select);
        var listWithOverlayOpen = screen.ScenarioListComponent();
        var overlay = screen.OverlayComponent();

        Assert.Equal(116, initialList.Bounds.Width);
        Assert.Equal(116, listWithOverlayOpen.Bounds.Width);
        Assert.NotNull(overlay);
        Assert.Equal("scenario-command-panel", overlay.Id);
        Assert.Equal(new SadConsoleRect(76, 4, 41, 13), overlay.Bounds);
    }

    [Fact]
    public void ScenarioSelectionCommandPanelRoutesPlayAndEditOnly()
    {
        var screen = ScenarioSelectionScreen.FromCatalog(DemoCatalog());

        screen.Handle(UiComponentCommand.Select);
        var play = screen.Handle(UiComponentCommand.Select);

        Assert.Equal(ScenarioSelectionResultKind.Play, play.Kind);
        Assert.Equal("scenario-one", play.Scenario?.ScenarioId);

        screen = ScenarioSelectionScreen.FromCatalog(DemoCatalog());
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        var edit = screen.Handle(UiComponentCommand.Select);

        Assert.Equal(ScenarioSelectionResultKind.Edit, edit.Kind);
        Assert.Equal("scenario-one", edit.Scenario?.ScenarioId);

        Assert.Equal(1, screen.SelectedCommandIndex);
        screen.Handle(UiComponentCommand.Down);
        Assert.Equal(1, screen.SelectedCommandIndex);
    }

    [Fact]
    public void ScenarioSelectionEscClosesCommandPanelBeforeExit()
    {
        var screen = ScenarioSelectionScreen.FromCatalog(DemoCatalog());

        screen.Handle(UiComponentCommand.Select);
        var close = screen.Handle(UiComponentCommand.Cancel);
        var exit = screen.Handle(UiComponentCommand.Cancel);

        Assert.Equal(ScenarioSelectionResultKind.Stay, close.Kind);
        Assert.False(screen.CommandPanelOpen);
        Assert.Equal(ScenarioSelectionResultKind.Exit, exit.Kind);
    }

    [Fact]
    public void ScenarioSelectionShowsCatalogDiagnostics()
    {
        var screen = ScenarioSelectionScreen.FromCatalog(new ScenarioCatalogResult([], ["bad catalog"]));

        Assert.Contains(screen.Components(), component => component.Id == "catalog-diagnostics" && component.State == UiComponentState.Error);
        Assert.Contains("No scenario", screen.Handle(UiComponentCommand.Select).Message);
    }

    [Fact]
    public void StartupUsesScenarioSelectionByDefault()
    {
        var startup = SadConsoleStartup.FromArgs(["--content", "missing-file.yaml"]);

        Assert.False(startup.LaunchGallery);
        Assert.NotNull(startup.Catalog);
        Assert.Contains("missing-file.yaml", startup.Catalog.Diagnostics[0]);
    }

    private static ScenarioCatalogResult DemoCatalog() => new([
        new ScenarioCatalogEntry("one.yaml", "scenario-one", "Scenario One", "First"),
        new ScenarioCatalogEntry("two.yaml", "scenario-two", "Scenario Two", "Second")
    ], []);
}
