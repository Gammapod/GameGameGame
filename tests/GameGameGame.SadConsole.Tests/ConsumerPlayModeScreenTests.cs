using GameGameGame.Content;
using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Screens;

namespace GameGameGame.SadConsole.Tests;

public sealed class ConsumerPlayModeScreenTests
{
    [Fact]
    public void ConsumerPlayModeBuildsCenteredBareCurrentSpaceGridFromSession()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);

        var components = screen.Components();

        Assert.Equal("New Play Mode", screen.Title);
        Assert.NotNull(screen.ControlledActorProjection);
        Assert.NotNull(screen.CurrentPlaceProjection);
        Assert.NotNull(screen.CurrentSpaceView);
        var currentSpace = Assert.IsType<InventorySpaceComponent>(Assert.Single(components));
        Assert.Equal("current-space-grid", currentSpace.Id);
        Assert.Equal(UiComponentState.Focused, currentSpace.State);
        Assert.Same(InventorySpaceRenderOptions.Bare, currentSpace.Options);
        Assert.Empty(currentSpace.BodyRows);
        Assert.Equal(1, currentSpace.View.CellMetrics.Width);
        Assert.True(currentSpace.Bounds.Height >= currentSpace.RequiredHeight);

        var debugRows = screen.DebugRows();
        Assert.Contains(debugRows, row => row.Contains("Controlled actor:"));
        Assert.Contains(debugRows, row => row.Contains("Current space:"));
        Assert.Contains(debugRows, row => row.StartsWith("plane:", StringComparison.Ordinal));
        Assert.Contains(debugRows, row => row.StartsWith("size:", StringComparison.Ordinal));
        Assert.Contains(debugRows, row => row.StartsWith("view:", StringComparison.Ordinal));
        Assert.Contains(debugRows, row => row.StartsWith("layers:", StringComparison.Ordinal));
    }

    [Fact]
    public void ConsumerPlayModeCanOverlayDebugLabelsWithoutMovingGridCells()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);
        var drawable = SadConsoleRect.FromSize(1, 1, 100, 35);

        var bare = Assert.IsType<InventorySpaceComponent>(screen.CurrentSpaceGridComponent(drawable, showDebugLabels: false));
        var labeled = Assert.IsType<InventorySpaceComponent>(screen.CurrentSpaceGridComponent(drawable, showDebugLabels: true));

        Assert.Same(InventorySpaceRenderOptions.Bare, bare.Options);
        Assert.Same(InventorySpaceRenderOptions.Labeled, labeled.Options);
        Assert.Equal(bare.Bounds.Left, labeled.Bounds.Left + 4);
        Assert.Equal(bare.Bounds.Top, labeled.Bounds.Top + 1);
    }

    [Fact]
    public void ConsumerPlayModeLayoutResolvesDrawableAreaInsideOneTileBorder()
    {
        var layout = ConsumerPlayModeLayout.FromCellSize(80, 45);

        Assert.Equal(80, layout.Width);
        Assert.Equal(45, layout.Height);
        Assert.Equal(181, layout.BorderGlyph);
        Assert.Equal(1, layout.DrawableBounds.Left);
        Assert.Equal(1, layout.DrawableBounds.Top);
        Assert.Equal(78, layout.DrawableBounds.Width);
        Assert.Equal(43, layout.DrawableBounds.Height);
        Assert.Equal(global::SadRogue.Primitives.Color.Black, layout.BorderForeground);
    }

    [Fact]
    public void ConsumerPlayModeLayoutDebugToggleChangesOnlyBorderColor()
    {
        var normal = ConsumerPlayModeLayout.FromCellSize(80, 45);
        var debug = normal.WithDebugVisible(true);

        Assert.False(normal.DebugVisible);
        Assert.True(debug.DebugVisible);
        Assert.Equal(normal.Width, debug.Width);
        Assert.Equal(normal.Height, debug.Height);
        Assert.Equal(normal.DrawableBounds, debug.DrawableBounds);
        Assert.Equal(normal.BorderGlyph, debug.BorderGlyph);
        Assert.Equal(global::SadRogue.Primitives.Color.Red, debug.BorderForeground);
        Assert.Equal(global::SadRogue.Primitives.Color.Black, debug.BorderBackground);
    }

    [Fact]
    public void ConsumerPlayModeComponentsStayInsideDrawableBounds()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);
        var drawable = SadConsoleRect.FromSize(1, 1, 100, 35);

        var components = screen.Components(drawable);

        Assert.All(components, component =>
        {
            Assert.True(component.Bounds.Left >= drawable.Left);
            Assert.True(component.Bounds.Top >= drawable.Top);
            Assert.True(component.Bounds.Bottom <= drawable.Bottom);
            Assert.True(component.Bounds.Width <= drawable.Width);
        });
    }

    [Fact]
    public void ConsumerPlayModeReportsLaunchFailureInDebugRows()
    {
        var screen = ConsumerPlayModeScreen.Open(new ScenarioCatalogEntry("missing-file.yaml", "missing", "Missing", "Missing file"));

        Assert.Empty(screen.Components());

        Assert.Contains(screen.DebugRows(), row => row.Contains("Could not launch scenario"));
    }

    private static ScenarioCatalogEntry DemoEntry() => new("prototype", "prototype", "Prototype", "Prototype session");
}
