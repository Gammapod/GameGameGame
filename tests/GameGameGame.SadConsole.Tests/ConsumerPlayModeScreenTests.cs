using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Rendering;
using GameGameGame.SadConsoleApp.Ui.Screens;
using SadConsole.Input;

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
        Assert.Equal(14, components.Count);
        var parentChain = Assert.IsType<PanelComponent>(components.Single(component => component.Id == "actor-pov-parent-chain"));
        var currentSpace = Assert.IsType<InventorySpaceComponent>(components.Single(component => component.Id == "actor-pov-current-place-grid"));
        Assert.Equal("actor-pov-current-place-grid", currentSpace.Id);
        Assert.Equal(UiComponentState.Focused, currentSpace.State);
        Assert.Same(InventorySpaceRenderOptions.Bare, currentSpace.Options);
        Assert.Empty(currentSpace.BodyRows);
        var activity = Assert.IsType<PanelComponent>(components.Single(component => component.Id == "actor-pov-current-region-activity"));
        Assert.Equal("Current location activity", activity.Title);
        Assert.Equal($"T{screen.WorldTurnNumber}", activity.HeaderRight);
        Assert.Equal(["Recent successes", CurrentRegionActivityViewBuilder.EmptyText], activity.BodyRows);
        Assert.Equal(1, currentSpace.View.CellMetrics.Width);
        Assert.True(currentSpace.Bounds.Height >= currentSpace.RequiredHeight);
        var actorInventory = Assert.IsType<InventorySpaceComponent>(components.Single(component => component.Id == "actor-pov-actor-inventory-grid"));
        var worldInspection = components
            .Where(component => component.Id.StartsWith("actor-pov-world-inspection-", StringComparison.Ordinal)
                && component.Id != "actor-pov-world-inspection-connectors")
            .ToList();
        var worldInspectionConnector = Assert.IsType<ConnectorLineComponent>(components.Single(component => component.Id == "actor-pov-world-inspection-connectors"));
        var carriedInspection = components.Single(component => component.Id is "actor-pov-actor-inventory-inspection-grid" or "actor-pov-actor-inventory-inspection-empty");
        var actorPovModel = screen.ActorPovModel(SadConsoleRect.FromSize(1, 1, 118, 40))!;
        AssertInside(actorPovModel.Layout.ParentChain.Bounds, parentChain.Bounds);
        Assert.Equal(UiComponentState.Selected, actorInventory.State);
        Assert.Same(InventorySpaceRenderOptions.Bare, actorInventory.Options);
        AssertInside(actorPovModel.Layout.CurrentPlace.Bounds, currentSpace.Bounds);
        AssertInside(actorPovModel.Layout.CurrentPlace.Bounds, activity.Bounds);
        Assert.True(activity.Bounds.Top > currentSpace.Bounds.Top);
        Assert.Equal(8, worldInspection.Count);
        Assert.All(worldInspection, component => AssertInside(actorPovModel.Layout.WorldInspection.Bounds, component.Bounds));
        AssertInside(actorPovModel.Layout.WorldInspection.Bounds, worldInspectionConnector.Bounds);
        AssertInside(actorPovModel.Layout.ActorInventory.Bounds, actorInventory.Bounds);
        AssertInside(actorPovModel.Layout.ActorInventoryInspection.Bounds, carriedInspection.Bounds);

        var debugRows = screen.DebugRows();
        Assert.Contains(debugRows, row => row.Contains("Controlled actor:"));
        Assert.Contains(debugRows, row => row.Contains("Current space:"));
        Assert.Contains(debugRows, row => row.StartsWith("plane:", StringComparison.Ordinal));
        Assert.Contains(debugRows, row => row.StartsWith("size:", StringComparison.Ordinal));
        Assert.Contains(debugRows, row => row.StartsWith("view:", StringComparison.Ordinal));
        Assert.Contains(debugRows, row => row.StartsWith("layers:", StringComparison.Ordinal));

        Assert.Equal(InventorySpaceZoom.Huge32, currentSpace.DisplayProfile?.SpaceZoom);
        Assert.Equal(32, currentSpace.DisplayProfile?.CellPixelSize);
        Assert.Equal(InventorySpaceZoom.Large24, actorInventory.DisplayProfile?.SpaceZoom);
        Assert.Equal(24, actorInventory.DisplayProfile?.CellPixelSize);
        Assert.Equal(1, actorInventory.DisplayProfile?.CellGapPixels);
    }

    [Fact]
    public void ConsumerPlayModeCyclesLeftRegionBetweenParentChainAndLogPanels()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);
        var drawable = SadConsoleRect.FromSize(1, 1, 100, 35);
        Assert.True(screen.SubmitMove(Direction.South).Succeeded, screen.LastActionStatus);

        var parentComponents = screen.Components(drawable);
        Assert.Equal(LeftRegionMode.ParentLocationChain, screen.LeftRegionMode);
        Assert.Contains(parentComponents, component => component.Id == "actor-pov-parent-chain");
        Assert.DoesNotContain(parentComponents, component => component.Id.StartsWith("actor-pov-left-log", StringComparison.Ordinal));

        var globalStatus = screen.CycleLeftRegionMode();
        var globalComponents = screen.Components(drawable);

        Assert.Equal(LeftRegionMode.GlobalLog, screen.LeftRegionMode);
        Assert.Contains("full log", globalStatus, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(globalComponents, component => component.Id == "actor-pov-parent-chain");
        var globalLog = Assert.IsType<PanelComponent>(globalComponents.Single(component => component.Id == "actor-pov-left-log-global"));
        Assert.Equal("Log: All", globalLog.Title);
        Assert.Equal($"T{screen.WorldTurnNumber}", globalLog.HeaderRight);
        Assert.Contains(globalLog.BodyRows, row => row.Contains("moved South", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("L: left region", screen.FooterText);

        screen.CycleLeftRegionMode();
        var currentLayerComponents = screen.Components(drawable);

        Assert.Equal(LeftRegionMode.CurrentLayerLog, screen.LeftRegionMode);
        var currentLayerLog = Assert.IsType<PanelComponent>(currentLayerComponents.Single(component => component.Id == "actor-pov-left-log-current-layer"));
        Assert.Equal("Log: Current layer", currentLayerLog.Title);
        Assert.Equal($"T{screen.WorldTurnNumber}", currentLayerLog.HeaderRight);
        Assert.Contains(currentLayerLog.BodyRows, row => row.Contains("moved South", StringComparison.OrdinalIgnoreCase));

        screen.CycleLeftRegionMode();
        var currentLocationComponents = screen.Components(drawable);

        Assert.Equal(LeftRegionMode.CurrentLocationLog, screen.LeftRegionMode);
        var currentLocationLog = Assert.IsType<PanelComponent>(currentLocationComponents.Single(component => component.Id == "actor-pov-left-log-current-location"));
        Assert.Equal("Log: Current location", currentLocationLog.Title);
        Assert.Equal($"T{screen.WorldTurnNumber}", currentLocationLog.HeaderRight);
        Assert.Contains(currentLocationLog.BodyRows, row => row.Contains("moved South", StringComparison.OrdinalIgnoreCase));

        screen.CycleLeftRegionMode();
        var restoredComponents = screen.Components(drawable);

        Assert.Equal(LeftRegionMode.ParentLocationChain, screen.LeftRegionMode);
        Assert.Contains(restoredComponents, component => component.Id == "actor-pov-parent-chain");
    }

    [Fact]
    public void ConsumerPlayModeDelaysHoverTooltipUntilHoverSettles()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);
        var drawable = SadConsoleRect.FromSize(1, 1, 118, 40);
        var currentGrid = Assert.IsType<InventorySpaceComponent>(screen.Components(drawable).Single(component => component.Id == "actor-pov-current-place-grid"));
        var entity = currentGrid.View.Entities.First(entity => entity.DisplayName is not null);
        var geometry = InventorySpacePresentationGeometry.FromComponent(currentGrid, rootCellWidthPixels: 16, rootCellHeightPixels: 16);
        var cell = geometry.CellPixelBounds(entity.Coord);
        var hoverX = cell.Left / 16;
        var hoverY = cell.Top / 16;

        Assert.True(screen.SetHoverCell(hoverX, hoverY));
        Assert.False(screen.HoverTooltipReady);
        Assert.Null(screen.BuildRenderFrame(drawable, debugVisible: false, rootCellWidthPixels: 16, rootCellHeightPixels: 16).TooltipOverlay);

        Assert.False(screen.AdvanceHoverTooltipDelay(ConsumerPlayModeScreen.HoverTooltipDelay - TimeSpan.FromMilliseconds(1)));
        Assert.Null(screen.BuildRenderFrame(drawable, debugVisible: false, rootCellWidthPixels: 16, rootCellHeightPixels: 16).TooltipOverlay);

        Assert.True(screen.AdvanceHoverTooltipDelay(TimeSpan.FromMilliseconds(1)));
        Assert.True(screen.HoverTooltipReady);
        Assert.IsType<PlayEntityTooltipComponent>(screen.BuildRenderFrame(drawable, debugVisible: false, rootCellWidthPixels: 16, rootCellHeightPixels: 16).TooltipOverlay);

        Assert.True(screen.SetHoverCell(hoverX + 1, hoverY));
        Assert.False(screen.HoverTooltipReady);
    }

    [Fact]
    public void ConsumerPlayModeLeftRegionLogRowsAreClippedToPanelBodyHeight()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);
        var drawable = SadConsoleRect.FromSize(1, 1, 100, 35);
        screen.CycleLeftRegionMode();
        for (var index = 0; index < 6; index++)
        {
            Assert.True(screen.SubmitWait().Succeeded, screen.LastActionStatus);
        }

        var log = Assert.IsType<PanelComponent>(screen.Components(drawable).Single(component => component.Id == "actor-pov-left-log-global"));

        Assert.True(log.BodyRows.Count <= Math.Max(0, log.Bounds.Height - 2));
        Assert.StartsWith("T", log.BodyRows.FirstOrDefault() ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void ConsumerPlayModeCurrentPlaceShowsPersistentSuccessOnlyActivityRows()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);
        var drawable = SadConsoleRect.FromSize(1, 1, 100, 35);

        Assert.True(screen.SubmitMove(Direction.South).Succeeded, screen.LastActionStatus);

        var activity = Assert.IsType<PanelComponent>(screen.Components(drawable).Single(component => component.Id == "actor-pov-current-region-activity"));
        Assert.Equal($"T{screen.WorldTurnNumber}", activity.HeaderRight);
        Assert.Contains("Recent successes", activity.BodyRows);
        Assert.Contains(activity.BodyRows, row => row.Contains("moved South", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(activity.BodyRows, row => row.Contains("FAIL", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ConsumerPlayModeWorldInspectionRegionIsTwoColumnsByFourAdjacentDirections()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);
        var drawable = SadConsoleRect.FromSize(1, 1, 100, 35);
        var model = screen.ActorPovModel(drawable)!;

        var components = ActorPovPlayComponentFactory.WorldInspectionComponents(model).ToDictionary(component => component.Id);
        var region = model.Layout.WorldInspection.Bounds;

        Assert.Equal(8, components.Count);
        var northwest = ComponentForDirection(components.Values, "northwest");
        var north = ComponentForDirection(components.Values, "north");
        var southeast = ComponentForDirection(components.Values, "southeast");
        var southwest = ComponentForDirection(components.Values, "southwest");
        var west = ComponentForDirection(components.Values, "west");
        var columnWidth = region.Width / 2;
        var rowHeight = region.Height / 4;
        AssertInside(SadConsoleRect.FromSize(region.Left, region.Top, columnWidth, rowHeight + region.Height % 4), northwest.Bounds);
        AssertInside(SadConsoleRect.FromSize(region.Left, region.Top + rowHeight + 1, columnWidth, rowHeight), north.Bounds);
        Assert.True(north.Bounds.Top > northwest.Bounds.Top);
        AssertInside(SadConsoleRect.FromSize(region.Left + columnWidth, region.Top, region.Width - columnWidth, rowHeight + region.Height % 4), southeast.Bounds);
        AssertInside(SadConsoleRect.FromSize(region.Left + columnWidth, region.Top + (rowHeight * 3) + region.Height % 4, region.Width - columnWidth, rowHeight), west.Bounds);
        Assert.True(west.Bounds.Top > southwest.Bounds.Top);
    }

    [Fact]
    public void ConsumerPlayModeCurrentSpaceUsesRuntimeTemplatePresentationWhenEntityPolymorphs()
    {
        var (path, session) = LifecycleFlagshipSession();
        var eggId = new EntityId("lifecycleEgg");
        session.World.Entities[eggId] = session.World.Entities[eggId] with
        {
            Name = "Butterfly",
            TemplateId = "lifecycleButterfly"
        };

        var screen = ConsumerPlayModeScreen.FromSession(new ScenarioCatalogEntry(path, session.ScenarioId, session.Name, session.Name), session);

        var visual = Assert.Single(screen.CurrentSpaceView!.Entities, entity => entity.EntityId == eggId);
        Assert.Equal('c', visual.Primary.Glyph);
        Assert.Equal(PresentationColor.Green, visual.Primary.Foreground);
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
    public void ConsumerPlayModeExposesActorPovDiagnosticsChromeForDebugRendering()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);
        var drawable = SadConsoleRect.FromSize(1, 1, 100, 35);

        var chrome = Assert.IsType<PanelComponent>(screen.ActorPovDiagnosticsChromeComponent(drawable));

        Assert.Equal("actor-pov-diagnostics-chrome", chrome.Id);
        AssertInside(screen.ActorPovModel(drawable)!.Layout.DiagnosticsRegion.Bounds, chrome.Bounds);
        Assert.Contains(chrome.BodyRows, row => row.Contains("focused:", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ConsumerPlayModeLinkedSpaceFallsBackToSingleCurrentSpaceWhenChildCannotFit()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);
        var drawable = SadConsoleRect.FromSize(1, 1, 8, 6);

        var presentation = Assert.IsType<LinkedPlaySpacePresentation>(screen.LinkedSpacePresentation(drawable));

        Assert.Single(presentation.Nodes);
        Assert.Equal("current-space-grid", presentation.Nodes.Single().Id);
        Assert.Null(presentation.Connector);
        Assert.True(presentation.Layout.Status is LinkedInventorySpaceLayoutStatus.SingleNode or LinkedInventorySpaceLayoutStatus.ChildOmitted or LinkedInventorySpaceLayoutStatus.Clipped);
    }

    [Fact]
    public void ConsumerPlayModeLinkedSpaceCanPresentLinkedInspectedInventoryBesideCurrentSpace()
    {
        var (path, session) = SizeCalibrationSession();
        var screen = ConsumerPlayModeScreen.FromSession(new ScenarioCatalogEntry(path, session.ScenarioId, session.Name, session.Name), session);
        var drawable = SadConsoleRect.FromSize(1, 1, 100, 35);

        var presentation = Assert.IsType<LinkedPlaySpacePresentation>(screen.LinkedSpacePresentation(drawable));

        Assert.Equal(LinkedInventorySpaceLayoutStatus.LinkedTwoSpace, presentation.Layout.Status);
        Assert.Collection(
            presentation.Nodes,
            current =>
            {
                Assert.Equal("current-space-grid", current.Id);
                Assert.Same(InventorySpaceRenderOptions.Bare, current.Options);
            },
            inspected =>
            {
                Assert.Equal("linked-inspected-space-grid", inspected.Id);
                Assert.Same(InventorySpaceRenderOptions.Bare, inspected.Options);
            });
        Assert.NotNull(presentation.Connector);
        Assert.NotNull(screen.LinkedInspectedSpaceProjection);
        Assert.Contains(screen.DebugRows(), row => row.Contains("linked inspected space:"));
        Assert.All(presentation.Nodes, node =>
        {
            Assert.True(node.Bounds.Left >= drawable.Left);
            Assert.True(node.Bounds.Top >= drawable.Top);
            Assert.True(node.Bounds.Bottom <= drawable.Bottom);
        });
    }

    [Fact]
    public void ConsumerPlayModeDebugRowsIncludeLinkedLayoutDiagnosticsForDeveloperReview()
    {
        var (path, session) = SizeCalibrationSession();
        var screen = ConsumerPlayModeScreen.FromSession(new ScenarioCatalogEntry(path, session.ScenarioId, session.Name, session.Name), session);
        var drawable = SadConsoleRect.FromSize(1, 1, 100, 35);

        var rows = screen.DebugRows(drawable, promptOverlayActive: false);

        Assert.Contains(rows, row => row == "Linked layout:");
        Assert.Contains(rows, row => row.Contains("status: LinkedTwoSpace"));
        Assert.Contains(rows, row => row.Contains("connector: smooth MonoGame preferred; tile fallback available"));
        Assert.Contains(rows, row => row.Contains("connector render: MonoGame DrawCallCustom"));
        Assert.Contains(rows, row => row.Contains("node current-place/CurrentPlace"));
        Assert.Contains(rows, row => row.Contains("node linked-inspected-space/LinkedInspectedSpace"));
        Assert.Contains(rows, row => row.Contains("parent cell bounds:"));
        Assert.Contains(rows, row => row.Contains("connector current-place-to-linked-inspected-space:"));
        Assert.Contains(rows, row => row.Contains("hit regions:"));
    }

    [Fact]
    public void ConsumerPlayModePromptOverlayDoesNotMoveLinkedLayoutAndSuppressesSmoothConnectorDiagnostic()
    {
        var (path, session) = SizeCalibrationSession();
        var screen = ConsumerPlayModeScreen.FromSession(new ScenarioCatalogEntry(path, session.ScenarioId, session.Name, session.Name), session);
        var drawable = SadConsoleRect.FromSize(1, 1, 100, 35);
        var before = Assert.IsType<LinkedPlaySpacePresentation>(screen.LinkedSpacePresentation(drawable));

        MoveAdjacentToWeightBulk0(screen);
        screen.SubmitDefaultAction();
        if (!screen.HasActivePrompt)
        {
            SelectPromptUntilClosed(screen);
            screen.SubmitDefaultAction();
        }
        var prompt = Assert.IsAssignableFrom<IUiComponent>(screen.PromptComponent(drawable));
        var after = Assert.IsType<LinkedPlaySpacePresentation>(screen.LinkedSpacePresentation(drawable));
        var rows = screen.DebugRows(drawable, promptOverlayActive: true);

        Assert.DoesNotContain(prompt.Id, after.Nodes.Select(node => node.Id));
        Assert.Equal(before.Nodes.Select(node => node.Bounds).ToList(), after.Nodes.Select(node => node.Bounds).ToList());
        Assert.Contains(rows, row => row.Contains("connector render: suppressed while prompt overlay is active"));
    }

    [Fact]
    public void ConsumerPlayModeBuildsRenderFrameWithNamedCaptureRegions()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);
        var drawable = SadConsoleRect.FromSize(1, 1, 100, 35);

        var normal = screen.BuildRenderFrame(drawable, debugVisible: false);
        var debug = screen.BuildRenderFrame(drawable, debugVisible: true);

        Assert.False(normal.DebugVisible);
        Assert.Equal(drawable, normal.DrawableBounds);
        Assert.NotEmpty(normal.MainDrawableComponents);
        Assert.Empty(normal.DebugComponents);
        Assert.Null(normal.DiagnosticsChromeComponent);
        Assert.Empty(normal.DebugRows);
        Assert.Contains(normal.CaptureRegions(), region => region.Id == "actor-pov-current-place-grid");

        Assert.True(debug.DebugVisible);
        Assert.NotEmpty(debug.DebugComponents);
        Assert.NotNull(debug.DiagnosticsChromeComponent);
        Assert.NotEmpty(debug.DebugRows);
        Assert.Contains(debug.CaptureRegions(), region => region.Id == "actor-pov-actor-inventory-grid");
    }

    [Fact]
    public void ConsumerPlayModeF9DebugLineToggleAddsOrangeTargetingConnectors()
    {
        var (path, session) = SizeCalibrationSession();
        var screen = ConsumerPlayModeScreen.FromSession(new ScenarioCatalogEntry(path, session.ScenarioId, session.Name, session.Name), session);
        MoveAdjacentToWeightBulk0(screen);
        var target = new EntityId("debugWeightBulk0");
        session.World.SetActionTarget(session.PlayerEntityId, target);
        var drawable = SadConsoleRect.FromSize(1, 1, 100, 35);

        var hiddenFrame = screen.BuildRenderFrame(drawable, debugVisible: false);

        Assert.False(screen.DebugLinesVisible);
        Assert.DoesNotContain(hiddenFrame.MainConnectors, connector => connector.Id == "actor-pov-targeting-connectors");

        screen.ToggleDebugLines();
        var visibleFrame = screen.BuildRenderFrame(drawable, debugVisible: false);

        Assert.True(screen.DebugLinesVisible);
        Assert.Contains("F9: debug lines", screen.FooterText);
        var targeting = Assert.Single(visibleFrame.MainConnectors, connector => connector.Id == "actor-pov-targeting-connectors");
        var segment = Assert.Single(targeting.View.Segments);
        Assert.Equal($"targeting-{session.PlayerEntityId.Value}-to-{target.Value}", segment.Id);
        Assert.Equal(PresentationColor.Earth, segment.Color);
    }

    [Fact]
    public void ConsumerPlayModeReportsLaunchFailureInDebugRows()
    {
        var screen = ConsumerPlayModeScreen.Open(new ScenarioCatalogEntry("missing-file.yaml", "missing", "Missing", "Missing file"));

        Assert.Empty(screen.Components());

        Assert.Contains(screen.DebugRows(), row => row.Contains("Could not launch scenario"));
    }

    [Fact]
    public void ConsumerPlayModeSubmitMoveMovesControlledActorAndRefreshesCurrentSpace()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);
        var before = session.World.GetEntityLocation(session.PlayerEntityId);

        var result = screen.SubmitMove(Direction.South);

        Assert.True(result.Succeeded, result.FailureText);
        var after = session.World.GetEntityLocation(session.PlayerEntityId);
        Assert.Equal(before.Coord.Offset(Direction.South), after.Coord);
        Assert.Contains("Moved South", screen.LastActionStatus);
        Assert.NotNull(screen.CurrentSpaceView);
        Assert.Contains(screen.DebugRows(), row => row.Contains($"Actor location: {after}"));
    }

    [Fact]
    public void ConsumerPlayModeSubmitWaitAdvancesTurnAndRefreshesStatus()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);

        var result = screen.SubmitWait();

        Assert.True(result.Succeeded, result.FailureText);
        Assert.Equal("Waited.", screen.LastActionStatus);
        Assert.True(screen.UndoPreviousFrame());
    }

    [Fact]
    public void ConsumerPlayModeUndoPreviousFrameRollsBackPlayerAction()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);
        var before = session.World.GetEntityLocation(session.PlayerEntityId);
        screen.SubmitMove(Direction.South);

        var undone = screen.UndoPreviousFrame();

        Assert.True(undone);
        Assert.Equal(before, session.World.GetEntityLocation(session.PlayerEntityId));
        Assert.Equal("Undid previous frame.", screen.LastActionStatus);
    }

    [Fact]
    public void ConsumerPlayModeDebugRowsIncludeInteractionDiagnostics()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);

        screen.SubmitMove(Direction.South);
        var rows = screen.DebugRows();

        Assert.Contains(rows, row => row == "Interaction:");
        Assert.Contains(rows, row => row.Contains("input: MoveDirection South | decision: auto-submitted", StringComparison.Ordinal));
        Assert.Contains(rows, row => row == "  prompt stack[0]: none");
        Assert.Contains(rows, row => row == "  focus: none | shortcuts: none");
        Assert.Contains(rows, row => row == "  candidates: 1/1 valid | 1 complete, 0 incomplete, 0 invalid");
        Assert.Contains(rows, row => row.Contains("submission: success | direct controlled command", StringComparison.Ordinal));
    }

    [Fact]
    public void ConsumerPlayModeDirectionInputSupportsArrowsAndNumpadDiagonals()
    {
        Assert.Equal(Direction.North, ConsumerPlayModeConsole.ReadDirectionKey(Keys.Up));
        Assert.Equal(Direction.South, ConsumerPlayModeConsole.ReadDirectionKey(Keys.NumPad2));
        Assert.Equal(Direction.West, ConsumerPlayModeConsole.ReadDirectionKey(Keys.NumPad4));
        Assert.Equal(Direction.East, ConsumerPlayModeConsole.ReadDirectionKey(Keys.NumPad6));
        Assert.Equal(Direction.NorthWest, ConsumerPlayModeConsole.ReadDirectionKey(Keys.NumPad7));
        Assert.Equal(Direction.NorthEast, ConsumerPlayModeConsole.ReadDirectionKey(Keys.NumPad9));
        Assert.Equal(Direction.SouthWest, ConsumerPlayModeConsole.ReadDirectionKey(Keys.NumPad1));
        Assert.Equal(Direction.SouthEast, ConsumerPlayModeConsole.ReadDirectionKey(Keys.NumPad3));
        Assert.Null(ConsumerPlayModeConsole.ReadDirectionKey(Keys.NumPad5));
        Assert.True(ConsumerPlayModeConsole.IsWaitKey(Keys.Space));
        Assert.True(ConsumerPlayModeConsole.IsWaitKey(Keys.D5));
        Assert.True(ConsumerPlayModeConsole.IsWaitKey(Keys.NumPad5));
    }

    [Fact]
    public void ConsumerPlayModePromptComponentIsAbsentUntilPromptIsActive()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);

        Assert.False(screen.HasActivePrompt);
        Assert.Null(screen.PromptComponent(SadConsoleRect.FromSize(1, 1, 80, 25)));
    }

    [Fact]
    public void ConsumerPlayModeDefaultActionExplainsWhenNoSharedChoiceIsAvailable()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);

        var outcome = screen.SubmitDefaultAction();

        Assert.Equal(PlayModeIntentOutcomeKind.Explained, outcome.Kind);
        Assert.False(screen.HasActivePrompt);
        Assert.Contains("No valid action", screen.LastActionStatus);
    }

    [Fact]
    public void ConsumerPlayModeDefaultActionUsesSharedChoicesInSizeCalibrationScenario()
    {
        var (path, session) = SizeCalibrationSession();
        var screen = ConsumerPlayModeScreen.FromSession(new ScenarioCatalogEntry(path, session.ScenarioId, session.Name, session.Name), session);
        screen.SubmitMove(Direction.North);

        var outcome = screen.SubmitDefaultAction();

        Assert.NotEqual(PlayModeIntentOutcomeKind.Explained, outcome.Kind);
        Assert.True(outcome.Submission?.Succeeded == true || screen.HasActivePrompt);
    }

    [Fact]
    public void ConsumerPlayModeBumpDirectionFallsBackToContextCandidates()
    {
        var (path, session) = SizeCalibrationSession();
        var screen = ConsumerPlayModeScreen.FromSession(new ScenarioCatalogEntry(path, session.ScenarioId, session.Name, session.Name), session);
        Assert.True(screen.SubmitMove(Direction.North).Succeeded);

        var result = screen.SubmitMove(Direction.North);

        Assert.True(result.Succeeded || screen.HasActivePrompt, screen.LastActionStatus);
        Assert.DoesNotContain("Could not move North", screen.LastActionStatus);
    }

    [Fact]
    public void ConsumerPlayModeOffEdgeMoveFallsBackToExitContext()
    {
        var (path, session) = SizeCalibrationSession();
        var screen = ConsumerPlayModeScreen.FromSession(new ScenarioCatalogEntry(path, session.ScenarioId, session.Name, session.Name), session);
        Assert.True(screen.SubmitMove(Direction.North).Succeeded);
        var enterResult = screen.SubmitMove(Direction.North);
        if (screen.HasActivePrompt)
        {
            screen.HandlePromptCommand(UiComponentCommand.Select);
        }

        Assert.True(enterResult.Succeeded || screen.LastActionStatus.Contains("Enter", StringComparison.OrdinalIgnoreCase), screen.LastActionStatus);

        var exitResult = screen.SubmitMove(Direction.North);

        Assert.True(exitResult.Succeeded || screen.HasActivePrompt, screen.LastActionStatus);
        Assert.DoesNotContain("Could not move North", screen.LastActionStatus);
    }

    [Fact]
    public void ConsumerPlayModeSizeCalibrationCanReachPickupAndDropThroughPrompts()
    {
        var (path, session) = SizeCalibrationSession();
        var screen = ConsumerPlayModeScreen.FromSession(new ScenarioCatalogEntry(path, session.ScenarioId, session.Name, session.Name), session);
        MoveAdjacentToWeightBulk0(screen);
        var item = new EntityId("debugWeightBulk0");
        var originalPlane = session.World.GetEntityLocation(item).PlaneId;

        var pickup = screen.SubmitDefaultAction();
        SelectPromptUntilClosed(screen);

        Assert.True(pickup.Kind != PlayModeIntentOutcomeKind.Explained || session.World.GetEntityLocation(item).PlaneId != originalPlane, screen.LastActionStatus);
        Assert.NotEqual(originalPlane, session.World.GetEntityLocation(item).PlaneId);

        var drop = screen.SubmitDefaultAction();
        MovePromptSelectionTo(screen, "Drop");
        screen.HandlePromptCommand(UiComponentCommand.Select);
        Assert.IsType<InventorySpaceComponent>(screen.PromptComponent(SadConsoleRect.FromSize(1, 1, 80, 25)));
        screen.HandlePromptCommand(UiComponentCommand.Select);
        if (screen.HasActivePrompt && screen.ActivePromptAcceptedDirections.FirstOrDefault() is { } direction)
        {
            screen.HandlePromptDirection(direction);
        }
        SelectPromptUntilClosed(screen);

        Assert.NotEqual(PlayModeIntentOutcomeKind.Explained, drop.Kind);
        Assert.Equal(originalPlane, session.World.GetEntityLocation(item).PlaneId);
    }

    [Fact]
    public void ConsumerPlayModeSizeCalibrationCanReachTransferPanelAndSubmitTransfer()
    {
        var (path, session) = SizeCalibrationSession();
        var screen = ConsumerPlayModeScreen.FromSession(new ScenarioCatalogEntry(path, session.ScenarioId, session.Name, session.Name), session);
        MoveAdjacentToWeightBulk0(screen);
        var item = new EntityId("debugWeightBulk0");
        screen.SubmitDefaultAction();
        SelectPromptUntilClosed(screen);
        Assert.NotEqual("world", session.World.GetEntityLocation(item).PlaneId.Value);

        var context = screen.SubmitMove(Direction.South);
        MovePromptSelectionTo(screen, "Transfer");
        screen.HandlePromptCommand(UiComponentCommand.Select);

        Assert.True(screen.HasActivePrompt, screen.LastActionStatus);
        Assert.IsType<TransferInventoryComparisonComponent>(screen.PromptComponent(SadConsoleRect.FromSize(1, 1, 80, 25)));
        screen.HandlePromptCommand(UiComponentCommand.Select);

        Assert.False(screen.HasActivePrompt);
        Assert.Contains("Transferred", screen.LastActionStatus, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("debugChestAperture0", session.World.GetEntityLocation(item).PlaneId.Value);
    }

    [Fact]
    public void ConsumerPlayModeSizeCalibrationCanPushNestedBagThroughContextPrompt()
    {
        var (path, session) = SizeCalibrationSession();
        var screen = ConsumerPlayModeScreen.FromSession(new ScenarioCatalogEntry(path, session.ScenarioId, session.Name, session.Name), session);
        var bag = new EntityId("debugNestedBag1");
        MoveAdjacentWestOfNestedBag1(screen);
        var actorBefore = session.World.GetEntityLocation(session.PlayerEntityId);

        var context = screen.SubmitMove(Direction.East);
        MovePromptSelectionTo(screen, "Push");
        screen.HandlePromptCommand(UiComponentCommand.Select);
        screen.HandlePromptDirection(Direction.East);

        Assert.True(context.Succeeded || screen.LastActionStatus.Contains("Push", StringComparison.OrdinalIgnoreCase), screen.LastActionStatus);
        Assert.False(screen.HasActivePrompt);
        Assert.Contains("Pushed", screen.LastActionStatus, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(actorBefore, session.World.GetEntityLocation(session.PlayerEntityId));
        Assert.Equal(new GridCoord(5, 5), session.World.GetEntityLocation(bag).Coord);
    }

    [Fact]
    public void ConsumerPlayModeCanonicalPushShowcaseCanPushPlayerBlock()
    {
        var (path, session) = CanonicalPushSession();
        var screen = ConsumerPlayModeScreen.FromSession(new ScenarioCatalogEntry(path, session.ScenarioId, session.Name, session.Name), session);
        var block = new EntityId("playerPushBlock");
        var actorBefore = session.World.GetEntityLocation(session.PlayerEntityId);

        var context = screen.SubmitMove(Direction.East);
        if (screen.HasActivePrompt)
        {
            MovePromptSelectionTo(screen, "Push");
            screen.HandlePromptCommand(UiComponentCommand.Select);
            if (screen.HasActivePrompt)
            {
                screen.HandlePromptDirection(Direction.East);
            }
        }

        Assert.True(context.Succeeded || screen.LastActionStatus.Contains("Push", StringComparison.OrdinalIgnoreCase), screen.LastActionStatus);
        Assert.False(screen.HasActivePrompt);
        Assert.Contains("Pushed", screen.LastActionStatus, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(actorBefore, session.World.GetEntityLocation(session.PlayerEntityId));
        Assert.Equal(new GridCoord(5, 2), session.World.GetEntityLocation(block).Coord);
    }

    [Fact]
    public void ConsumerPlayModePickupDestinationPromptUsesDirectionKeysForSelectionNotMovement()
    {
        var (path, session) = SizeCalibrationSession();
        var screen = ConsumerPlayModeScreen.FromSession(new ScenarioCatalogEntry(path, session.ScenarioId, session.Name, session.Name), session);
        MoveAdjacentToWeightBulk0(screen);
        var actorBefore = session.World.GetEntityLocation(session.PlayerEntityId);

        screen.SubmitDefaultAction();
        SelectPromptUntilTitleContains(screen, "choose destination");
        var beforeLabel = screen.ActivePromptFocusedChoiceLabel;

        screen.HandlePromptNavigationDirection(Direction.East);

        Assert.Equal(actorBefore, session.World.GetEntityLocation(session.PlayerEntityId));
        Assert.NotEqual(beforeLabel, screen.ActivePromptFocusedChoiceLabel);
        Assert.IsType<InventorySpaceComponent>(screen.PromptComponent(SadConsoleRect.FromSize(1, 1, 80, 25)));
    }

    private static ScenarioCatalogEntry DemoEntry() => new("prototype", "prototype", "Prototype", "Prototype session");

    private static IUiComponent ComponentForDirection(IEnumerable<IUiComponent> components, string direction) =>
        components.Single(component => component.Id.Contains($"-{direction}-", StringComparison.Ordinal));

    private static void MoveAdjacentToWeightBulk0(ConsumerPlayModeScreen screen)
    {
        Assert.True(screen.SubmitMove(Direction.North).Succeeded);
        Assert.True(screen.SubmitMove(Direction.East).Succeeded);
        Assert.True(screen.SubmitMove(Direction.North).Succeeded);
        Assert.True(screen.SubmitMove(Direction.North).Succeeded);
        Assert.True(screen.SubmitMove(Direction.North).Succeeded);
        Assert.True(screen.SubmitMove(Direction.North).Succeeded);
        Assert.True(screen.SubmitMove(Direction.West).Succeeded);
    }

    private static void MoveAdjacentWestOfNestedBag1(ConsumerPlayModeScreen screen)
    {
        Assert.True(screen.SubmitMove(Direction.North).Succeeded);
        Assert.True(screen.SubmitMove(Direction.East).Succeeded);
        Assert.True(screen.SubmitMove(Direction.East).Succeeded);
        Assert.True(screen.SubmitMove(Direction.North).Succeeded);
    }

    private static void SelectPromptUntilClosed(ConsumerPlayModeScreen screen, int maxSelections = 4)
    {
        for (var index = 0; index < maxSelections && screen.HasActivePrompt; index++)
        {
            screen.HandlePromptCommand(UiComponentCommand.Select);
        }
    }

    private static void SelectPromptUntilTitleContains(ConsumerPlayModeScreen screen, string text, int maxSelections = 4)
    {
        for (var index = 0; index < maxSelections && screen.HasActivePrompt; index++)
        {
            if (screen.PromptComponent(SadConsoleRect.FromSize(1, 1, 80, 25))?.Title.Contains(text, StringComparison.OrdinalIgnoreCase) == true)
            {
                return;
            }

            screen.HandlePromptCommand(UiComponentCommand.Select);
        }
    }

    private static void MovePromptSelectionTo(ConsumerPlayModeScreen screen, string labelPrefix)
    {
        for (var index = 0; index < screen.ActivePromptChoiceLabels.Count; index++)
        {
            if (screen.ActivePromptChoiceLabels.ElementAtOrDefault(index)?.StartsWith(labelPrefix, StringComparison.OrdinalIgnoreCase) == true)
            {
                return;
            }

            screen.HandlePromptCommand(UiComponentCommand.Down);
        }
    }

    private static (string Path, PlayableScenarioSession Session) SizeCalibrationSession()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Content",
            "Beta",
            "Debug",
            "CanonicalDebugRooms.yaml");
        return (path, PlayableScenarioLauncher.CreateFromFile(path, "canonical-debug-size-calibration-room"));
    }

    private static (string Path, PlayableScenarioSession Session) CanonicalPushSession()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Content",
            "Beta",
            "CanonicalActions",
            "CanonicalPushShowcase.yaml");
        return (path, PlayableScenarioLauncher.CreateFromFile(path, "beta-canonical-push-player-interaction"));
    }

    private static (string Path, PlayableScenarioSession Session) LifecycleFlagshipSession()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Content",
            "Beta",
            "EntityLifecycle",
            "CreateDestroyPolymorphShowcase.yaml");
        return (path, PlayableScenarioLauncher.CreateFromFile(path, "delta-create-destroy-polymorph-flagship-room"));
    }

    private static void AssertInside(SadConsoleRect outer, SadConsoleRect inner)
    {
        Assert.True(inner.Left >= outer.Left);
        Assert.True(inner.Top >= outer.Top);
        Assert.True(inner.Left + inner.Width <= outer.Left + outer.Width);
        Assert.True(inner.Bottom <= outer.Bottom);
    }
}
