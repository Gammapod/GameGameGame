using GameGameGame.Content;
using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Rendering;
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
        Assert.Contains("Enter opens Play/Debug/Edit", screen.FooterText());
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
    public void ScenarioSelectionCommandPanelRoutesPlayDebugAndEdit()
    {
        var screen = ScenarioSelectionScreen.FromCatalog(DemoCatalog());

        screen.Handle(UiComponentCommand.Select);
        var panel = Assert.IsType<SelectableListComponent>(screen.OverlayComponent());
        Assert.Equal(["Play", "Debug", "Edit"], panel.Items.Select(item => item.Label).ToArray());

        var play = screen.Handle(UiComponentCommand.Select);

        Assert.Equal(ScenarioSelectionResultKind.Play, play.Kind);
        Assert.Equal("scenario-one", play.Scenario?.ScenarioId);

        screen = ScenarioSelectionScreen.FromCatalog(DemoCatalog());
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        var debug = screen.Handle(UiComponentCommand.Select);

        Assert.Equal(ScenarioSelectionResultKind.Debug, debug.Kind);
        Assert.Equal("scenario-one", debug.Scenario?.ScenarioId);

        screen = ScenarioSelectionScreen.FromCatalog(DemoCatalog());
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        var edit = screen.Handle(UiComponentCommand.Select);

        Assert.Equal(ScenarioSelectionResultKind.Edit, edit.Kind);
        Assert.Equal("scenario-one", edit.Scenario?.ScenarioId);

        Assert.False(screen.CommandPanelOpen);
        Assert.Equal(0, screen.SelectedCommandIndex);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        Assert.Equal(1, screen.SelectedCommandIndex);
        screen.Handle(UiComponentCommand.Down);
        Assert.Equal(2, screen.SelectedCommandIndex);
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
    public void ScenarioSelectionFiltersCuratedSectionsWithLeftRight()
    {
        var catalog = new ScenarioCatalogResult(
            [
                new ScenarioCatalogEntry("legacy.yaml", "legacy-one", "Legacy One", "Legacy description", "legacy", ["beta"], "Beta exploration"),
                new ScenarioCatalogEntry("delta.yaml", "delta-one", "Delta One", "Delta description", "active-delta", ["canonical-action"], "Vertical slice")
            ],
            [],
            [
                new ScenarioCatalogSection(
                    "legacy",
                    "Legacy Beta",
                    "Legacy/prototype rooms.",
                    [new ScenarioCatalogEntry("legacy.yaml", "legacy-one", "Legacy One", "Legacy description", "legacy", ["beta"], "Beta exploration")],
                    "legacy"),
                new ScenarioCatalogSection(
                    "delta",
                    "Delta",
                    "Current vertical slices.",
                    [new ScenarioCatalogEntry("delta.yaml", "delta-one", "Delta One", "Delta description", "active-delta", ["canonical-action"], "Vertical slice")],
                    "active-delta")
            ]);
        var screen = ScenarioSelectionScreen.FromCatalog(catalog);

        var initialList = Assert.IsType<SelectableListComponent>(screen.ScenarioListComponent());
        Assert.Equal("legacy", screen.SelectedSection?.Id);
        Assert.Equal("legacy-one", initialList.SelectedItem?.Id);
        Assert.Contains("Left/Right changes curated section", screen.FooterText());
        Assert.Contains("Play/Debug/Edit", screen.FooterText());

        var moved = screen.Handle(UiComponentCommand.Right);
        var deltaList = Assert.IsType<SelectableListComponent>(screen.ScenarioListComponent());

        Assert.Equal(ScenarioSelectionResultKind.Stay, moved.Kind);
        Assert.Equal("delta", screen.SelectedSection?.Id);
        Assert.Equal("delta-one", screen.SelectedScenario?.ScenarioId);
        Assert.Contains("1.1 Scenarios: Delta", deltaList.Title);
        Assert.Equal("Delta One", deltaList.SelectedItem?.Label);
        Assert.Equal("Delta description", deltaList.SelectedItem?.Detail);
        Assert.True(deltaList.SelectedItem?.DetailOnNextLine);
        var rows = deltaList.RenderRows(GameGameGame.SadConsoleApp.Ui.Styling.SadConsoleTheme.Default);
        Assert.Contains(rows, row => row.Contains(">") && row.Contains("Delta One"));
        Assert.Contains(rows, row => row.Contains("    Delta description"));
        Assert.DoesNotContain(rows, row => row.Contains("[active-delta] Delta One"));
        Assert.DoesNotContain(rows, row => row.Contains("tags: canonical-action"));
    }

    [Fact]
    public void ScenarioSelectionListsCreateDestroyPolymorphFlagshipRoomFromManifest()
    {
        var catalog = ScenarioCatalog.LoadManifest(Path.Combine(AppContext.BaseDirectory, "Content", "Beta", "Manifest.yaml"));

        var delta = Assert.Single(catalog.Sections ?? [], section => section.Id == "delta");
        var flagship = Assert.Single(delta.Entries, entry => entry.ScenarioId == "delta-create-destroy-polymorph-flagship-room");
        var screen = ScenarioSelectionScreen.FromCatalog(catalog);
        while (screen.SelectedSection?.Id != "delta")
        {
            screen.Handle(UiComponentCommand.Right);
        }

        Assert.Equal("Create Destroy Polymorph Flagship Room", flagship.Name);
        Assert.Equal("active-delta", flagship.Status);
        Assert.Contains("entity-lifecycle", flagship.Tags ?? []);
        Assert.Contains(screen.VisibleScenarios, entry => entry.ScenarioId == flagship.ScenarioId);
    }

    [Fact]
    public void PlayModeDynamicLifecycleScenarioBuildsScreenModelWithoutMissingPresentationCrash()
    {
        var catalog = ScenarioCatalog.LoadManifest(Path.Combine(AppContext.BaseDirectory, "Content", "Beta", "Manifest.yaml"));
        var entry = catalog.Entries.Single(entry => entry.ScenarioId == "delta-create-destroy-polymorph-flagship-room");

        var screen = ConsumerPlayModeScreen.Open(entry);
        var model = screen.ActorPovModel(SadConsoleRect.FromSize(1, 1, 118, 40));

        Assert.Null(screen.LaunchFailure);
        Assert.NotNull(model);
        Assert.DoesNotContain(model!.Projection.Diagnostics, diagnostic => diagnostic.Message.Contains("presentation", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(screen.Components());
    }

    [Fact]
    public void StartupUsesScenarioSelectionByDefault()
    {
        var startup = SadConsoleStartup.FromArgs(["--content", "missing-file.yaml"]);

        Assert.False(startup.LaunchGallery);
        Assert.NotNull(startup.Catalog);
        Assert.Contains("missing-file.yaml", startup.Catalog.Diagnostics[0]);
    }

    [Fact]
    public void ScenarioSelectionRestoresKeyboardFocusAfterPlayModeExit()
    {
        var focus = ScenarioSelectionConsole.RestoredScenarioSelectionFocusForPlayExit();

        Assert.True(focus.UseKeyboard);
        Assert.True(focus.IsFocused);
        Assert.Equal(global::SadConsole.FocusBehavior.Set, focus.FocusedMode);
    }

    private static ScenarioCatalogResult DemoCatalog() => new([
        new ScenarioCatalogEntry("one.yaml", "scenario-one", "Scenario One", "First"),
        new ScenarioCatalogEntry("two.yaml", "scenario-two", "Scenario Two", "Second")
    ], []);
}
