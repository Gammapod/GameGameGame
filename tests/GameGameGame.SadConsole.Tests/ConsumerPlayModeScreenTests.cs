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
        var currentSpace = Assert.IsType<InventorySpaceComponent>(components.Single(component => component.Id == "current-space-grid"));
        Assert.Equal("current-space-grid", currentSpace.Id);
        Assert.Equal(UiComponentState.Focused, currentSpace.State);
        Assert.Same(InventorySpaceRenderOptions.Bare, currentSpace.Options);
        Assert.Empty(currentSpace.BodyRows);
        Assert.Equal(2, currentSpace.View.CellMetrics.Width);
        Assert.Equal(2, currentSpace.View.CellMetrics.Height);
        Assert.True(currentSpace.Bounds.Height >= currentSpace.RequiredHeight);
        var actorInventory = Assert.IsType<InventorySpaceComponent>(components.Single(component => component.Id == "controlled-actor-inventory-grid"));
        Assert.Equal(UiComponentState.Selected, actorInventory.State);
        Assert.Same(InventorySpaceRenderOptions.Bare, actorInventory.Options);

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
        var layout = ActorPovPlayLayout.Resolve(drawable);
        Assert.True(labeled.Bounds.Left >= layout.CurrentPovRegion.Left);
        Assert.True(labeled.Bounds.Top >= layout.CurrentPovRegion.Top);
        Assert.True(labeled.Bounds.Bottom <= layout.CurrentPovRegion.Bottom);
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
    public void ConsumerPlayModeUsesActorPovRegionsForCurrentSpaceAndActorInventory()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);
        var drawable = SadConsoleRect.FromSize(1, 1, 99, 36);
        var layout = ActorPovPlayLayout.Resolve(drawable);

        var current = Assert.IsType<InventorySpaceComponent>(screen.CurrentSpaceGridComponent(drawable, showDebugLabels: false));
        var inventory = Assert.IsType<InventorySpaceComponent>(screen.ControlledActorInventoryComponent(drawable, showDebugLabels: false));

        Assert.True(current.Bounds.Left >= layout.CurrentPovRegion.Left);
        Assert.True(current.Bounds.Top >= layout.CurrentPovRegion.Top);
        Assert.True(current.Bounds.Bottom <= layout.CurrentPovRegion.Bottom);
        Assert.True(current.Bounds.Width <= layout.CurrentPovRegion.Width);
        Assert.True(inventory.Bounds.Left >= layout.InventoryChainRegion.Left);
        Assert.True(inventory.Bounds.Top >= layout.InventoryChainRegion.Top);
        Assert.True(inventory.Bounds.Bottom <= layout.InventoryChainRegion.Bottom);
    }

    [Fact]
    public void ConsumerPlayModeLinkedSpaceFallsBackToSingleCurrentSpaceWhenChildCannotFit()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);
        var drawable = SadConsoleRect.FromSize(1, 1, 8, 6);

        var presentation = Assert.IsType<LinkedPlaySpacePresentation>(screen.LinkedSpacePresentation(drawable));

        Assert.Contains(presentation.Nodes, node => node.Id == "current-space-grid");
        Assert.DoesNotContain(presentation.Nodes, node => node.Id == "linked-inspected-space-grid");
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
        var current = Assert.IsType<InventorySpaceComponent>(presentation.Nodes.Single(node => node.Id == "current-space-grid"));
        var inspected = Assert.IsType<InventorySpaceComponent>(presentation.Nodes.Single(node => node.Id == "linked-inspected-space-grid"));
        Assert.Same(InventorySpaceRenderOptions.Bare, current.Options);
        Assert.Same(InventorySpaceRenderOptions.Bare, inspected.Options);
        Assert.NotNull(presentation.Connector);
        Assert.NotNull(screen.LinkedInspectedSpaceProjection);
        Assert.Contains(screen.DebugRows(), row => row.Contains("linked inspected space:"));
        var actorPovLayout = ActorPovPlayLayout.Resolve(drawable);
        Assert.True(presentation.Layout.Nodes[0].Bounds.Left >= actorPovLayout.CurrentPovRegion.Left);
        Assert.True(presentation.Layout.Nodes[0].Bounds.Bottom <= actorPovLayout.CurrentPovRegion.Bottom);
        Assert.True(presentation.Layout.Nodes[1].Bounds.Left >= actorPovLayout.InspectionChainRegion.Left);
        Assert.All(presentation.Nodes, node =>
        {
            Assert.True(node.Bounds.Left >= drawable.Left);
            Assert.True(node.Bounds.Top >= drawable.Top);
            Assert.True(node.Bounds.Bottom <= drawable.Bottom);
        });
    }

    [Fact]
    public void ConsumerPlayModeRendersActorInventoryChainFromBottomInventoryRegion()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), session);
        var drawable = SadConsoleRect.FromSize(1, 1, 120, 42);
        var layout = ActorPovPlayLayout.Resolve(drawable);

        var presentation = Assert.IsType<LinkedPlaySpacePresentation>(screen.LinkedSpacePresentation(drawable));

        var actorInventory = Assert.IsType<InventorySpaceComponent>(presentation.Nodes.Single(node => node.Id == "controlled-actor-inventory-grid"));
        Assert.True(actorInventory.Bounds.Left >= layout.InventoryChainRegion.Left);
        Assert.True(actorInventory.Bounds.Top >= layout.InventoryChainRegion.Top);
        Assert.True(actorInventory.Bounds.Bottom <= layout.InventoryChainRegion.Bottom);
        if (screen.LinkedActorInventoryItemProjection is not null)
        {
            var linkedItem = Assert.IsType<InventorySpaceComponent>(presentation.Nodes.Single(node => node.Id == "actor-inventory-linked-item-grid"));
            Assert.True(linkedItem.Bounds.Left > actorInventory.Bounds.Left);
            Assert.True(linkedItem.Bounds.Bottom <= layout.InventoryChainRegion.Bottom);
            Assert.NotNull(presentation.Connector);
            Assert.Contains(presentation.Connector!.Segments, segment => segment.Id == "actor-inventory-chain-to-linked-item");
        }
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
    public void ConsumerPlayModeRendersParentChainLeftOfCurrentPovWhenActorEntersNestedSpace()
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
        var drawable = SadConsoleRect.FromSize(1, 1, 160, 60);
        var layout = ActorPovPlayLayout.Resolve(drawable);

        var presentation = Assert.IsType<LinkedPlaySpacePresentation>(screen.LinkedSpacePresentation(drawable));

        var parent = Assert.IsType<InventorySpaceComponent>(presentation.Nodes.First(node => node.Id.StartsWith("parent-chain-", StringComparison.Ordinal)));
        Assert.True(parent.Bounds.Left >= layout.ParentChainRegion.Left);
        Assert.True(parent.Bounds.Bottom <= layout.ParentChainRegion.Bottom);
        Assert.Equal(InventorySpaceCellMetrics.Default, parent.View.CellMetrics);
        Assert.NotNull(presentation.Connector);
        Assert.Contains(presentation.Connector!.Segments, segment => segment.Id.StartsWith("parent-chain-", StringComparison.Ordinal));
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
}
