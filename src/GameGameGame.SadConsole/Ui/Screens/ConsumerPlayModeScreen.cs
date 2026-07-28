using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Components;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed class ConsumerPlayModeScreen
{
    private static readonly InventorySpaceCellMetrics MainCurrentLocationMetrics = new(2, 2, 0);

    private readonly EntityPanelProjectionService _panelProjection;
    private readonly GameplaySessionController? _sessionController;
    private readonly PlayModeIntentController _intentController;

    private ConsumerPlayModeScreen(ScenarioCatalogEntry catalogEntry, PlayableScenarioSession? session, string? launchFailure)
    {
        CatalogEntry = catalogEntry;
        Session = session;
        LaunchFailure = launchFailure;
        _panelProjection = new EntityPanelProjectionService(ResolveInspectionAppearance, GetActionPlanDescriptorForEntity);
        _sessionController = session is not null ? new GameplaySessionController(session) : null;
        _intentController = new PlayModeIntentController(ResolveIntentCandidates);

        if (session is not null)
        {
            RefreshProjections();
        }
    }

    public ScenarioCatalogEntry CatalogEntry { get; }
    public PlayableScenarioSession? Session { get; }
    public string? LaunchFailure { get; }
    public EntityPanelProjection? ControlledActorProjection { get; private set; }
    public EntityPanelProjection? CurrentPlaceProjection { get; private set; }
    public EntityPanelProjection? LinkedInspectedSpaceProjection { get; private set; }
    public InventorySpaceViewModel? CurrentSpaceView { get; private set; }
    public InventorySpaceViewModel? ControlledActorInventoryView { get; private set; }
    public InventorySpaceViewModel? LinkedInspectedSpaceView { get; private set; }
    public EntityPanelProjection? LinkedActorInventoryItemProjection { get; private set; }
    public InventorySpaceViewModel? LinkedActorInventoryItemView { get; private set; }
    public string LastActionStatus { get; private set; } = "Ready.";
    public bool HasActivePrompt => _intentController.CurrentPrompt is not null;
    public IReadOnlyList<string> ActivePromptChoiceLabels => _intentController.CurrentPrompt?.Choices.Select(choice => choice.Label).ToList() ?? [];
    public string? ActivePromptFocusedChoiceLabel => _intentController.CurrentPrompt?.FocusedChoice?.Label;
    public IReadOnlyList<Direction> ActivePromptAcceptedDirections => _intentController.CurrentPrompt?.Choices.Select(choice => choice.ShortcutDirection).OfType<Direction>().ToList() ?? [];
    public bool ActivePromptAcceptsDirection(Direction direction) => _intentController.CurrentPrompt?.Choices.Any(choice => choice.ShortcutDirection == direction) == true;
    public string Title => "New Play Mode";
    public string Purpose => "Consumer-facing Play mode skeleton. Current-space component is active.";
    public string FooterText => "Arrows/Numpad: move | Esc: return to Scenario Selection | F12: toggle debug border";

    public static ConsumerPlayModeScreen Open(ScenarioCatalogEntry catalogEntry)
    {
        try
        {
            return new ConsumerPlayModeScreen(catalogEntry, PlayableScenarioLauncher.CreateFromCatalogEntry(catalogEntry), launchFailure: null);
        }
        catch (Exception ex)
        {
            return new ConsumerPlayModeScreen(catalogEntry, session: null, launchFailure: ex.Message);
        }
    }

    internal static ConsumerPlayModeScreen FromSession(ScenarioCatalogEntry catalogEntry, PlayableScenarioSession session) =>
        new(catalogEntry, session, launchFailure: null);

    public GameplayRuntimeSubmission SubmitMove(Direction direction)
    {
        if (_sessionController is null)
        {
            LastActionStatus = "Cannot move: session unavailable.";
            return new GameplayRuntimeSubmission(false, LastActionStatus, UsedCoreActionChoice: false);
        }

        var outcome = _intentController.HandleIntent(PlayModeIntentSeed.Move(direction));
        var result = outcome.Submission
            ?? new GameplayRuntimeSubmission(false, outcome.Message, UsedCoreActionChoice: false);
        if (!result.Succeeded)
        {
            var contextOutcome = _intentController.HandleIntent(new PlayModeIntentSeed(PlayModeIntentKind.ContextDirection, Direction: direction));
            if (contextOutcome.Kind != PlayModeIntentOutcomeKind.Explained || contextOutcome.Submission?.Succeeded == true || HasActivePrompt)
            {
                result = contextOutcome.Submission
                    ?? new GameplayRuntimeSubmission(false, contextOutcome.Message, UsedCoreActionChoice: true);
                LastActionStatus = contextOutcome.Submission is { } submission
                    ? submission.Succeeded ? contextOutcome.Message : $"{contextOutcome.Message}: {submission.FailureText ?? "failed"}"
                    : contextOutcome.Message;
            }
        }

        RefreshProjections();
        return result;
    }

    public PlayModeIntentOutcome SubmitDefaultAction()
    {
        var outcome = _intentController.HandleIntent(new PlayModeIntentSeed(PlayModeIntentKind.DefaultAction));
        if (outcome.Submission is not { Succeeded: true })
        {
            LastActionStatus = outcome.Submission is { } submission
                ? $"{outcome.Message}: {submission.FailureText ?? "failed"}"
                : outcome.Message;
        }
        RefreshProjections();
        return outcome;
    }

    public PlayModeIntentOutcome HandlePromptCommand(UiComponentCommand command)
    {
        var outcome = command switch
        {
            UiComponentCommand.Up => _intentController.MoveFocus(-1),
            UiComponentCommand.Down => _intentController.MoveFocus(1),
            UiComponentCommand.Select => _intentController.SelectFocused(),
            UiComponentCommand.Cancel => _intentController.Cancel(),
            _ => new PlayModeIntentOutcome(PlayModeIntentOutcomeKind.Explained, "Prompt only supports Up, Down, Select, and Cancel.")
        };

        if (outcome.Submission is not { Succeeded: true })
        {
            LastActionStatus = outcome.Submission is { } submission
                ? $"{outcome.Message}: {submission.FailureText ?? "failed"}"
                : outcome.Message;
        }
        RefreshProjections();
        return outcome;
    }

    public PlayModeIntentOutcome HandlePromptDirection(Direction direction)
    {
        var outcome = _intentController.SelectShortcutDirection(direction);
        if (outcome.Submission is not { Succeeded: true })
        {
            LastActionStatus = outcome.Submission is { } submission
                ? $"{outcome.Message}: {submission.FailureText ?? "failed"}"
                : outcome.Message;
        }
        RefreshProjections();
        return outcome;
    }

    public PlayModeIntentOutcome HandlePromptNavigationDirection(Direction direction)
    {
        var outcome = _intentController.MoveFocus(direction);
        LastActionStatus = outcome.Message;
        RefreshProjections();
        return outcome;
    }

    public IUiComponent? PromptComponent(SadConsoleRect drawableBounds)
    {
        if (_intentController.CurrentPrompt is not { } prompt)
        {
            return null;
        }

        if (prompt.CustomComponent is { } customComponent)
        {
            return customComponent(prompt, drawableBounds);
        }

        var items = prompt.Choices.Select((choice, index) => new SelectableListItem(
            $"prompt-choice-{index}",
            choice.Label,
            choice.Explanation ?? (choice.IsComplete ? "Enter: select" : "needs more information"),
            IsEnabled: choice.IsValid));
        var width = Math.Min(Math.Max(24, drawableBounds.Width), Math.Max(36, prompt.Choices.Max(choice => choice.Label.Length) + 6));
        var height = Math.Min(Math.Max(5, prompt.Choices.Count + 4), Math.Max(5, drawableBounds.Height));
        var component = new SelectableListComponent(
            "0.2.1-action-prompt",
            prompt.Title,
            SadConsoleRect.FromSize(0, 0, width, height),
            items,
            UiComponentState.Focused,
            visibleRowCount: Math.Max(1, height - 3));
        component.MoveSelection(prompt.FocusedIndex);
        return component;
    }

    public InventorySpaceComponent? CurrentSpaceGridComponent(SadConsoleRect drawableBounds, bool showDebugLabels)
    {
        if (CurrentSpaceView is not { } currentSpaceView)
        {
            return null;
        }

        var playLayout = ActorPovPlayLayout.Resolve(drawableBounds);
        var currentRegion = playLayout.CurrentPovRegion;
        currentSpaceView = FitCurrentSpaceViewToRegion(currentSpaceView, currentRegion, showDebugLabels);
        var bareSizing = new InventorySpaceComponent(
            "current-space-grid-sizing",
            currentSpaceView.Title,
            SadConsoleRect.FromSize(0, 0, 1, 1),
            currentSpaceView,
            options: InventorySpaceRenderOptions.Bare);

        var gridLeft = currentRegion.Left + Math.Max(0, (currentRegion.Width - bareSizing.RequiredWidth) / 2);
        var gridTop = currentRegion.Top + Math.Max(0, (currentRegion.Height - bareSizing.RequiredHeight) / 2);
        var bounds = SadConsoleRect.FromSize(
            gridLeft,
            gridTop,
            Math.Min(currentRegion.Width, bareSizing.RequiredWidth),
            Math.Min(currentRegion.Height, bareSizing.RequiredHeight));

        if (showDebugLabels)
        {
            bounds = SadConsoleRect.FromSize(
                Math.Max(currentRegion.Left, bounds.Left - 4),
                Math.Max(currentRegion.Top, bounds.Top - 1),
                Math.Min(currentRegion.Width, bounds.Width + 4),
                Math.Min(currentRegion.Height, bounds.Height + 1));
        }

        return new InventorySpaceComponent(
            "current-space-grid",
            currentSpaceView.Title,
            bounds,
            currentSpaceView,
            state: UiComponentState.Focused,
            options: showDebugLabels ? InventorySpaceRenderOptions.Labeled : InventorySpaceRenderOptions.Bare);
    }

    public IReadOnlyList<string> DebugRows() => DebugRows(drawableBounds: null, promptOverlayActive: HasActivePrompt);

    public IReadOnlyList<string> DebugRows(SadConsoleRect? drawableBounds, bool promptOverlayActive)
    {
        var rows = new List<string>();
        rows.AddRange(BuildStatusRows());
        rows.AddRange(BuildInteractionRows());
        rows.AddRange(BuildCurrentSpaceRows());
        rows.AddRange(BuildLinkedLayoutRows(drawableBounds, promptOverlayActive));
        rows.AddRange(BuildDiagnosticsRows());
        if (Session is null)
        {
            rows.Add($"Could not launch scenario: {LaunchFailure ?? "unknown"}");
            rows.Add($"content: {CatalogEntry.ContentPath}");
        }

        return rows;
    }

    public IReadOnlyList<IUiComponent> Components() => Components(SadConsoleRect.FromSize(1, 1, 118, 40));

    public IReadOnlyList<IUiComponent> Components(SadConsoleRect drawableBounds)
    {
        var components = new List<IUiComponent>();
        if (CurrentSpaceGridComponent(drawableBounds, showDebugLabels: false) is { } grid)
        {
            components.Add(grid);
        }

        if (ControlledActorInventoryComponent(drawableBounds, showDebugLabels: false) is { } inventory)
        {
            components.Add(inventory);
        }

        return components;
    }

    public InventorySpaceComponent? ControlledActorInventoryComponent(SadConsoleRect drawableBounds, bool showDebugLabels)
    {
        if (ControlledActorInventoryView is not { } inventoryView)
        {
            return null;
        }

        var playLayout = ActorPovPlayLayout.Resolve(drawableBounds);
        var region = playLayout.InventoryChainRegion;
        var options = showDebugLabels ? InventorySpaceRenderOptions.Labeled : InventorySpaceRenderOptions.Bare;
        var sizing = new InventorySpaceComponent(
            "controlled-actor-inventory-sizing",
            inventoryView.Title,
            SadConsoleRect.FromSize(0, 0, 1, 1),
            inventoryView,
            options: options);
        var bounds = SadConsoleRect.FromSize(
            region.Left,
            region.Top + Math.Max(0, (region.Height - sizing.RequiredHeight) / 2),
            Math.Min(region.Width, sizing.RequiredWidth),
            Math.Min(region.Height, sizing.RequiredHeight));
        return new InventorySpaceComponent(
            "controlled-actor-inventory-grid",
            inventoryView.Title,
            bounds,
            inventoryView,
            state: UiComponentState.Selected,
            options: options);
    }

    public LinkedPlaySpacePresentation? LinkedSpacePresentation(SadConsoleRect drawableBounds, bool showDebugLabels = false)
    {
        if (CurrentSpaceView is null)
        {
            return null;
        }

        var options = showDebugLabels ? InventorySpaceRenderOptions.Labeled : InventorySpaceRenderOptions.Bare;
        var playLayout = ActorPovPlayLayout.Resolve(drawableBounds);
        var currentSpaceView = FitCurrentSpaceViewToRegion(CurrentSpaceView, playLayout.CurrentPovRegion, showDebugLabels);
        var parentSizing = new InventorySpaceComponent(
            "current-space-grid-sizing",
            currentSpaceView.Title,
            SadConsoleRect.FromSize(0, 0, 1, 1),
            currentSpaceView,
            options: options);
        var childSizing = LinkedInspectedSpaceView is { } childView
            ? new InventorySpaceComponent(
                "linked-inspected-space-grid-sizing",
                childView.Title,
                SadConsoleRect.FromSize(0, 0, 1, 1),
                childView,
                options: options)
            : null;
        var inspectedCoord = LinkedInspectedSpaceProjection?.Location.Coord is { } coord && currentSpaceView.IsVisible(coord)
            ? coord
            : (GridCoord?)null;
        var layout = LinkedInventorySpaceLayout.ResolveActorPovAnchored(
            drawableBounds,
            playLayout.CurrentPovRegion,
            playLayout.InspectionChainRegion,
            parentSizing,
            childSizing,
            inspectedCoord);
        var nodes = new List<InventorySpaceComponent>();
        foreach (var node in layout.Nodes)
        {
            if (node.Role == LinkedInventorySpaceNodeRole.CurrentPlace)
            {
                nodes.Add(new InventorySpaceComponent(
                    "current-space-grid",
                    currentSpaceView.Title,
                    node.Bounds,
                    currentSpaceView,
                    state: UiComponentState.Focused,
                    options: options));
            }
            else if (node.Role == LinkedInventorySpaceNodeRole.LinkedInspectedSpace && LinkedInspectedSpaceView is { } inspectedView)
            {
                nodes.Add(new InventorySpaceComponent(
                    "linked-inspected-space-grid",
                    inspectedView.Title,
                    node.Bounds,
                    inspectedView,
                    state: UiComponentState.Selected,
                    options: options));
            }
        }

        var parentChain = BuildParentChainPresentation(playLayout, nodes.FirstOrDefault(node => node.Id == "current-space-grid"));
        var actorInventoryChain = BuildActorInventoryChainPresentation(playLayout);
        nodes.InsertRange(0, parentChain.Nodes);
        nodes.AddRange(actorInventoryChain.Nodes);
        var connector = CombineConnectors(parentChain.Connector, layout.Connector, actorInventoryChain.Connector);

        return new LinkedPlaySpacePresentation(nodes, connector, layout);
    }

    private ParentChainPresentation BuildActorInventoryChainPresentation(ActorPovPlayLayout playLayout)
    {
        var actorInventory = ControlledActorInventoryComponent(playLayout.DrawableBounds, showDebugLabels: false);
        if (actorInventory is null)
        {
            return ParentChainPresentation.Empty;
        }

        var nodes = new List<InventorySpaceComponent> { actorInventory };
        if (LinkedActorInventoryItemView is null || LinkedActorInventoryItemProjection?.Location.Coord is not { } carriedCoord || !actorInventory.View.IsVisible(carriedCoord))
        {
            return new ParentChainPresentation(nodes, null);
        }

        const int gap = 3;
        var region = playLayout.InventoryChainRegion;
        var childSizing = new InventorySpaceComponent(
            "actor-inventory-linked-item-sizing",
            LinkedActorInventoryItemView.Title,
            SadConsoleRect.FromSize(0, 0, 1, 1),
            LinkedActorInventoryItemView,
            options: InventorySpaceRenderOptions.Bare);
        var childLeft = actorInventory.Bounds.Left + actorInventory.Bounds.Width + gap;
        var remainingWidth = Math.Max(0, region.Left + region.Width - childLeft);
        if (remainingWidth <= 0 || childSizing.RequiredWidth > remainingWidth || childSizing.RequiredHeight > region.Height)
        {
            return new ParentChainPresentation(nodes, null);
        }

        var childBounds = SadConsoleRect.FromSize(
            childLeft,
            region.Top + Math.Max(0, (region.Height - childSizing.RequiredHeight) / 2),
            childSizing.RequiredWidth,
            childSizing.RequiredHeight);
        var child = new InventorySpaceComponent(
            "actor-inventory-linked-item-grid",
            LinkedActorInventoryItemView.Title,
            childBounds,
            LinkedActorInventoryItemView,
            state: UiComponentState.Selected,
            options: InventorySpaceRenderOptions.Bare);
        nodes.Add(child);

        var connector = new ConnectorLineViewModel(
            "actor-inventory-chain.connector",
            "Controlled actor inventory to inspected carried inventory",
            [new ConnectorLineSegment(
                "actor-inventory-chain-to-linked-item",
                CenterOf(actorInventory.CellBounds(carriedCoord), "actor-inventory-carried-item-cell"),
                LeftEdgeOf(child, "actor-inventory-linked-item-left-edge"),
                PresentationColor.Cyan,
                Layer: 1)],
            ConnectorLineFallbackGlyphs.Ascii);
        return new ParentChainPresentation(nodes, connector);
    }

    private ParentChainPresentation BuildParentChainPresentation(ActorPovPlayLayout playLayout, InventorySpaceComponent? currentNode)
    {
        if (Session is null || CurrentPlaceProjection?.Breadcrumb.Segments.Count is not > 1 || playLayout.ParentChainRegion.Width <= 0)
        {
            return ParentChainPresentation.Empty;
        }

        var actorId = _sessionController?.PlayerEntityId ?? Session.PlayerEntityId;
        var world = _sessionController?.World ?? Session.World;
        var breadcrumb = CurrentPlaceProjection.Breadcrumb.Segments;
        var parentSegments = breadcrumb.Take(breadcrumb.Count - 1).ToList();
        var componentsByEntityId = new Dictionary<EntityId, InventorySpaceComponent>();
        var orderedComponents = new List<InventorySpaceComponent>();
        var nextRight = playLayout.ParentChainRegion.Left + playLayout.ParentChainRegion.Width;
        const int gap = 3;

        foreach (var segment in parentSegments.AsEnumerable().Reverse())
        {
            var projection = _panelProjection.Project(world, segment.EntityId, Session.ActionPlans, actorId);
            if (projection.InventoryGrid is null)
            {
                continue;
            }

            var view = InventorySpaceViewModel.FromProjection(
                $"0.parent-chain.{segment.EntityId.Value}",
                projection,
                actorId,
                cellMetrics: InventorySpaceCellMetrics.Default,
                showFrame: false);
            var sizing = new InventorySpaceComponent(
                $"parent-chain-{segment.EntityId.Value}-sizing",
                view.Title,
                SadConsoleRect.FromSize(0, 0, 1, 1),
                view,
                options: InventorySpaceRenderOptions.Bare);
            var width = Math.Min(sizing.RequiredWidth, playLayout.ParentChainRegion.Width);
            var height = Math.Min(sizing.RequiredHeight, playLayout.ParentChainRegion.Height);
            var left = nextRight - width;
            if (left < playLayout.ParentChainRegion.Left)
            {
                break;
            }

            var bounds = SadConsoleRect.FromSize(
                left,
                playLayout.ParentChainRegion.Top + Math.Max(0, (playLayout.ParentChainRegion.Height - height) / 2),
                width,
                height);
            var component = new InventorySpaceComponent(
                $"parent-chain-{segment.EntityId.Value}",
                view.Title,
                bounds,
                view,
                state: UiComponentState.Unselected,
                options: InventorySpaceRenderOptions.Bare);
            componentsByEntityId[segment.EntityId] = component;
            orderedComponents.Insert(0, component);
            nextRight = left - gap;
        }

        var segments = new List<ConnectorLineSegment>();
        for (var index = 0; index < breadcrumb.Count - 1; index++)
        {
            var parentSegment = breadcrumb[index];
            var childSegment = breadcrumb[index + 1];
            if (!componentsByEntityId.TryGetValue(parentSegment.EntityId, out var parentComponent))
            {
                continue;
            }

            ConnectorLineEndpoint? end = null;
            if (componentsByEntityId.TryGetValue(childSegment.EntityId, out var childComponent))
            {
                end = LeftEdgeOf(childComponent, $"parent-chain-{childSegment.EntityId.Value}-left-edge");
            }
            else if (index == breadcrumb.Count - 2 && currentNode is not null)
            {
                end = LeftEdgeOf(currentNode, "current-place-left-edge");
            }

            if (end is null || childSegment.CoordinateInContainingPlane is not { } coord || !parentComponent.View.IsVisible(coord))
            {
                continue;
            }

            segments.Add(new ConnectorLineSegment(
                $"parent-chain-{parentSegment.EntityId.Value}-to-{childSegment.EntityId.Value}",
                CenterOf(parentComponent.CellBounds(coord), $"parent-chain-{parentSegment.EntityId.Value}-child-cell"),
                end,
                PresentationColor.Cyan,
                Layer: 1));
        }

        var connector = segments.Count == 0
            ? null
            : new ConnectorLineViewModel(
                "parent-chain.connector",
                "Parent containment chain to current place",
                segments,
                ConnectorLineFallbackGlyphs.Ascii);

        return new ParentChainPresentation(orderedComponents, connector);
    }

    private static ConnectorLineViewModel? CombineConnectors(params ConnectorLineViewModel?[] connectors)
    {
        var segments = connectors.Where(connector => connector is not null).SelectMany(connector => connector!.Segments).ToList();
        return segments.Count == 0
            ? null
            : new ConnectorLineViewModel("actor-pov-play.connectors", "Actor POV containment and inspection connectors", segments, ConnectorLineFallbackGlyphs.Ascii);
    }

    private static ConnectorLineEndpoint LeftEdgeOf(InventorySpaceComponent component, string id) =>
        new(id, component.Bounds.Left, component.Bounds.Top + component.Bounds.Height / 2, AnchorX: 0f, AnchorY: 0.5f);

    private static ConnectorLineEndpoint CenterOf(SadConsoleRect bounds, string id) =>
        new(id, bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);

    private static InventorySpaceViewModel FitCurrentSpaceViewToRegion(InventorySpaceViewModel view, SadConsoleRect region, bool showDebugLabels)
    {
        var rowLabelColumns = showDebugLabels ? 4 : 0;
        var columnLabelRows = showDebugLabels ? 1 : 0;
        var maxViewportWidth = Math.Max(1, (region.Width - rowLabelColumns) / Math.Max(1, view.CellMetrics.Width + view.CellMetrics.Gap));
        var maxViewportHeight = Math.Max(1, (region.Height - columnLabelRows) / Math.Max(1, view.CellMetrics.Height + view.CellMetrics.Gap));
        var viewportWidth = Math.Min(view.Width, maxViewportWidth);
        var viewportHeight = Math.Min(view.Height, maxViewportHeight);
        var focus = view.Decorators.FirstOrDefault(decorator => decorator.Role == InventorySpaceDecoratorRole.Controlled)?.Coord
            ?? view.Entities.FirstOrDefault()?.Coord
            ?? view.Viewport.Origin;
        var originX = Math.Clamp(focus.X - viewportWidth / 2, 0, Math.Max(0, view.Width - viewportWidth));
        var originY = Math.Clamp(focus.Y - viewportHeight / 2, 0, Math.Max(0, view.Height - viewportHeight));
        return view with
        {
            Viewport = new InventorySpaceViewport(new GridCoord(originX, originY), viewportWidth, viewportHeight)
        };
    }

    private IReadOnlyList<string> BuildStatusRows()
    {
        if (Session is null)
        {
            return [$"Scenario: {CatalogEntry.Name}", "Session unavailable."];
        }

        var actorName = ControlledActorProjection?.Name ?? Session.PlayerEntityId.Value;
        var placeName = CurrentPlaceProjection?.Name ?? "unresolved";
        var location = Session.World.Entities.ContainsKey(Session.PlayerEntityId)
            ? Session.World.GetEntityLocation(Session.PlayerEntityId).ToString()
            : "unavailable";

        return
        [
            $"Scenario: {Session.Name} ({Session.ScenarioId})",
            $"Controlled actor: {actorName} ({Session.PlayerEntityId})",
            $"Actor location: {location}",
            $"Current space: {placeName}",
            $"World turn: {Session.World.TurnNumber}"
        ];
    }

    private static SadConsoleRect CurrentSpaceBounds(SadConsoleRect drawableBounds, InventorySpaceViewModel view, IReadOnlyList<string> bodyRows)
    {
        var sizing = new InventorySpaceComponent(
            "0.2-sizing",
            view.Title,
            SadConsoleRect.FromSize(0, 0, 1, 1),
            view,
            bodyRows,
            UiComponentState.Focused,
            InventorySpaceRenderOptions.FramedDebug);
        var availableHeight = Math.Max(1, drawableBounds.Height - 13);
        return SadConsoleRect.FromSize(
            drawableBounds.Left,
            drawableBounds.Top + 12,
            Math.Max(1, drawableBounds.Width),
            Math.Min(availableHeight, Math.Min(27, Math.Max(10, sizing.RequiredHeight))));
    }

    private IReadOnlyList<string> BuildCurrentSpaceRows()
    {
        if (Session is null)
        {
            return ["Session unavailable."];
        }

        if (ControlledActorProjection?.PointOfView?.CurrentPlace is null)
        {
            return ["Controlled actor POV did not resolve a current containing inventory space."];
        }

        if (CurrentPlaceProjection?.InventoryGrid is not { } grid)
        {
            return [$"{CurrentPlaceProjection?.Name ?? "Current place"} has no inventory grid to display."];
        }

        var rows = new List<string>
        {
            $"plane: {grid.PlaneId}",
            $"size: {grid.Width}x{grid.Height}",
            $"contents: {grid.Cells.Count(cell => cell.EntityId is not null)} occupied cell(s)"
        };

        if (CurrentSpaceView is { } view)
        {
            rows.Add($"view: cells {view.CellMetrics.Width}x{view.CellMetrics.Height} gap {view.CellMetrics.Gap}; viewport {view.Viewport.Width}x{view.Viewport.Height} at {view.Viewport.Origin.X},{view.Viewport.Origin.Y}");
            rows.Add($"layers: backdrop + {view.Entities.Count} primary visual(s) + {view.Decorators.Count} decorator(s)");
        }

        if (LinkedInspectedSpaceProjection is { } child)
        {
            rows.Add($"linked inspected space: {child.Name} ({child.EntityId}) at {child.Location.Coord}");
        }

        return rows;
    }

    private IReadOnlyList<string> BuildLinkedLayoutRows(SadConsoleRect? drawableBounds, bool promptOverlayActive)
    {
        if (drawableBounds is not { } bounds)
        {
            return ["Linked layout: drawable bounds unavailable to screen-model diagnostics."];
        }

        if (LinkedSpacePresentation(bounds, showDebugLabels: false) is not { } presentation)
        {
            return ["Linked layout: unavailable; no current-space view."];
        }

        var rows = new List<string>
        {
            "Linked layout:",
            $"  drawable: {FormatRect(bounds)} | status: {presentation.Layout.Status}",
            $"  nodes: {presentation.Nodes.Count} | connector: {(presentation.Connector is null ? "none" : "smooth MonoGame preferred; tile fallback available")}",
            $"  connector render: {(presentation.Connector is null ? "none" : promptOverlayActive ? "suppressed while prompt overlay is active" : "MonoGame DrawCallCustom")}",
            $"  linked inspected space: {FormatLinkedInspectedSpace()}"
        };

        rows.AddRange(presentation.Layout.Nodes.Select(node => $"  node {node.Id}/{node.Role}: {FormatRect(node.Bounds)} clipped={node.IsClipped}"));
        if (presentation.Layout.ParentCellBounds is { } parentCell)
        {
            rows.Add($"  parent cell bounds: {FormatRect(parentCell)}");
        }

        if (presentation.Connector is { } connector)
        {
            foreach (var segment in connector.Segments)
            {
                rows.Add($"  connector {segment.Id}: {FormatEndpoint(segment.Start)} -> {FormatEndpoint(segment.End)} color={segment.Color} layer={segment.Layer}");
            }
        }

        var hitRegions = presentation.Layout.HitRegions.Take(4).Select(region => $"{region.Id}:{region.Kind}@{FormatRect(region.Bounds)}");
        rows.Add($"  hit regions: {string.Join("; ", hitRegions)}{(presentation.Layout.HitRegions.Count > 4 ? "; ..." : string.Empty)}");
        return rows;
    }

    private string FormatLinkedInspectedSpace() => LinkedInspectedSpaceProjection is { } child
        ? $"{child.Name} ({child.EntityId}) plane={child.Location.PlaneId} coord={child.Location.Coord} grid={child.InventoryGrid?.Width}x{child.InventoryGrid?.Height}"
        : "none";

    private static string FormatEndpoint(ConnectorLineEndpoint endpoint) =>
        $"{endpoint.Id}@({endpoint.CellX},{endpoint.CellY}) anchor=({endpoint.AnchorX:0.##},{endpoint.AnchorY:0.##})";

    private static string FormatRect(SadConsoleRect rect) =>
        $"L{rect.Left},T{rect.Top},W{rect.Width},H{rect.Height}";

    private IReadOnlyList<string> BuildInteractionRows()
    {
        var candidates = _intentController.LastResolvedCandidates;
        var validCount = candidates.Count(candidate => candidate.IsValid);
        var completeCount = candidates.Count(candidate => candidate.IsValid && candidate.IsComplete);
        var incompleteCount = candidates.Count(candidate => candidate.IsValid && !candidate.IsComplete);
        var invalidCount = candidates.Count - validCount;
        var prompt = _intentController.CurrentPrompt;
        var outcome = _intentController.LastOutcome;
        var rows = new List<string>
        {
            "Interaction:",
            $"  input: {_intentController.LastInputDescription} | decision: {FormatOutcomeKind(outcome?.Kind)}{FormatOutcomeMessage(outcome)}",
            $"  submission: {FormatSubmission(outcome)}",
            $"  prompt stack[{_intentController.PromptStack.Count}]: {FormatPromptStack()}",
            $"  focus: {FormatPromptFocus(prompt)} | shortcuts: {FormatPromptShortcuts(prompt)}",
            $"  candidates: {validCount}/{candidates.Count} valid | {completeCount} complete, {incompleteCount} incomplete, {invalidCount} invalid",
            $"  candidate sample: {FormatCandidateSample(candidates)}"
        };

        return rows;
    }

    private static string FormatCandidateSample(IReadOnlyList<PlayModeActionCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            return "none";
        }

        var labels = candidates
            .Take(3)
            .Select(candidate => $"{candidate.Label} [{FormatCandidateState(candidate)}]");
        return $"{string.Join("; ", labels)}{(candidates.Count > 3 ? "; ..." : string.Empty)}";
    }

    private static string FormatCandidateState(PlayModeActionCandidate candidate)
    {
        if (!candidate.IsValid)
        {
            return "invalid";
        }

        return candidate.IsComplete ? "complete" : "needs-refine";
    }

    private static string FormatOutcomeKind(PlayModeIntentOutcomeKind? kind) => kind switch
    {
        PlayModeIntentOutcomeKind.AutoSubmitted => "auto-submitted",
        PlayModeIntentOutcomeKind.PromptOpened => "opened prompt",
        PlayModeIntentOutcomeKind.Explained => "explained",
        PlayModeIntentOutcomeKind.Cancelled => "cancelled",
        PlayModeIntentOutcomeKind.SubmittedFromPrompt => "submitted from prompt",
        _ => "none"
    };

    private static string FormatOutcomeMessage(PlayModeIntentOutcome? outcome) =>
        string.IsNullOrWhiteSpace(outcome?.Message) ? string.Empty : $" | {outcome.Message}";

    private string FormatPromptStack()
    {
        if (_intentController.PromptStack.Count == 0)
        {
            return "none";
        }

        return string.Join(" > ", _intentController.PromptStack.Select(layer => layer.Title));
    }

    private static string FormatPromptFocus(PlayModePromptLayer? prompt)
    {
        if (prompt is null)
        {
            return "none";
        }

        var focusedPosition = prompt.Choices.Count == 0 ? "0/0" : $"{prompt.FocusedIndex + 1}/{prompt.Choices.Count}";
        var focusedLabel = prompt.FocusedChoice?.Label ?? "none";
        return $"{focusedPosition} {focusedLabel}";
    }

    private static string FormatPromptShortcuts(PlayModePromptLayer? prompt)
    {
        if (prompt is null)
        {
            return "none";
        }

        var shortcuts = prompt.Choices
            .Select(choice => choice.ShortcutDirection)
            .OfType<Direction>()
            .Distinct()
            .ToList();
        return shortcuts.Count == 0 ? "none" : string.Join(", ", shortcuts);
    }

    private static string FormatSubmission(PlayModeIntentOutcome? outcome)
    {
        if (outcome?.Submission is not { } submission)
        {
            return "none";
        }

        var path = submission.UsedCoreActionChoice ? "Core Action Choice" : "direct controlled command";
        return submission.Succeeded
            ? $"success | {path}"
            : $"failed | {path} | {submission.FailureText ?? "failed"}";
    }

    private IReadOnlyList<string> BuildDiagnosticsRows()
    {
        if (Session is null)
        {
            return [];
        }

        var rows = new List<string>();
        rows.AddRange(Session.ValidationDiagnostics);
        rows.AddRange(Session.RuntimeFailures);
        rows.AddRange(Session.CapabilityGaps);
        if (ControlledActorProjection?.PointOfView is { } pov)
        {
            rows.AddRange(pov.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
        }

        return rows.Take(4).ToList();
    }

    private void RefreshProjections()
    {
        if (Session is null)
        {
            ControlledActorProjection = null;
            CurrentPlaceProjection = null;
            LinkedInspectedSpaceProjection = null;
            LinkedActorInventoryItemProjection = null;
            CurrentSpaceView = null;
            ControlledActorInventoryView = null;
            LinkedInspectedSpaceView = null;
            LinkedActorInventoryItemView = null;
            return;
        }

        var actorId = _sessionController?.PlayerEntityId ?? Session.PlayerEntityId;
        var world = _sessionController?.World ?? Session.World;
        ControlledActorProjection = _panelProjection.Project(
            world,
            actorId,
            Session.ActionPlans,
            actorId);
        CurrentPlaceProjection = ControlledActorProjection.PointOfView?.CurrentPlace is { } currentPlace
            ? _panelProjection.Project(world, currentPlace.EntityId, Session.ActionPlans, actorId)
            : null;
        CurrentSpaceView = CurrentPlaceProjection?.InventoryGrid is not null
            ? InventorySpaceViewModel.FromProjection(
                "0.2.inventory-space",
                CurrentPlaceProjection,
                actorId,
                cellMetrics: MainCurrentLocationMetrics)
            : null;
        ControlledActorInventoryView = ControlledActorProjection.InventoryGrid is not null
            ? InventorySpaceViewModel.FromProjection(
                "0.inventory.controlled-actor",
                ControlledActorProjection,
                actorId,
                cellMetrics: InventorySpaceCellMetrics.Default)
            : null;
        LinkedInspectedSpaceProjection = ResolveFirstLinkedInspectedSpace(world, actorId);
        LinkedInspectedSpaceView = LinkedInspectedSpaceProjection?.InventoryGrid is not null
            ? InventorySpaceViewModel.FromProjection(
                "0.3.linked-inspected-space.inventory-space",
                LinkedInspectedSpaceProjection,
                actorId,
                cellMetrics: InventorySpaceCellMetrics.Default,
                showFrame: false)
            : null;
        LinkedActorInventoryItemProjection = ResolveFirstCarriedInspectedSpace(world, actorId);
        LinkedActorInventoryItemView = LinkedActorInventoryItemProjection?.InventoryGrid is not null
            ? InventorySpaceViewModel.FromProjection(
                "0.inventory.linked-carried-space.inventory-space",
                LinkedActorInventoryItemProjection,
                actorId,
                cellMetrics: InventorySpaceCellMetrics.Default,
                showFrame: false)
            : null;
    }

    private EntityPanelProjection? ResolveFirstCarriedInspectedSpace(WorldState world, EntityId actorId)
    {
        if (ControlledActorProjection?.InventoryGrid?.Cells is not { } cells)
        {
            return null;
        }

        foreach (var cell in cells.Where(cell => cell.EntityId is not null).OrderBy(cell => cell.Coord.Y).ThenBy(cell => cell.Coord.X))
        {
            var carriedId = cell.EntityId!.Value;
            var projection = _panelProjection.Project(world, carriedId, Session!.ActionPlans, actorId);
            if (projection.InventoryGrid is not null)
            {
                return projection;
            }
        }

        return null;
    }

    private EntityPanelProjection? ResolveFirstLinkedInspectedSpace(WorldState world, EntityId actorId)
    {
        if (CurrentPlaceProjection?.Contents.Count is not > 0)
        {
            return null;
        }

        foreach (var row in CurrentPlaceProjection.Contents.Where(row => row.EntityId != actorId))
        {
            var projection = _panelProjection.Project(world, row.EntityId, Session!.ActionPlans, actorId);
            if (projection.InventoryGrid is not null)
            {
                return projection;
            }
        }

        return null;
    }

    private IReadOnlyList<PlayModeActionCandidate> ResolveIntentCandidates(PlayModeIntentSeed seed)
    {
        if (seed.Kind == PlayModeIntentKind.MoveDirection && seed.Direction is { } direction)
        {
            return
            [
                new PlayModeActionCandidate(
                    $"Move {direction}",
                    IsValid: _sessionController is not null,
                    IsComplete: true,
                    Submit: () => SubmitMoveDirect(direction),
                    Explanation: _sessionController is null ? "Session unavailable." : null)
            ];
        }

        if (seed.Kind == PlayModeIntentKind.DefaultAction)
        {
            return BuildDefaultActionCandidates();
        }

        if (seed.Kind == PlayModeIntentKind.ContextDirection && seed.Direction is { } contextDirection)
        {
            return BuildContextDirectionCandidates(contextDirection);
        }

        return [];
    }

    private IReadOnlyList<PlayModeActionCandidate> BuildContextDirectionCandidates(Direction direction)
    {
        if (_sessionController?.CurrentActionChoiceRequest is not { } request)
        {
            return [];
        }

        var actorLocation = _sessionController.World.GetEntityLocation(_sessionController.PlayerEntityId);
        var contextCoord = new PlaneCoord(actorLocation.PlaneId, actorLocation.Coord.Offset(direction));
        var candidates = new List<PlayModeActionCandidate>();

        foreach (var choice in request.Choices.Where(choice => choice.Kind != ActionChoiceKind.Move))
        {
            candidates.AddRange(choice.Kind switch
            {
                ActionChoiceKind.Pickup => PickupCandidates(choice, option => option.Source == contextCoord),
                ActionChoiceKind.Enter => EnterCandidates(choice, option => option.Source == contextCoord),
                ActionChoiceKind.Exit => ExitCandidates(choice, option => option.Direction == direction),
                ActionChoiceKind.Transfer => TransferCandidates(choice, counterparty => counterparty.Direction == direction),
                _ => []
            });
        }

        return candidates;
    }

    private IReadOnlyList<PlayModeActionCandidate> BuildDefaultActionCandidates()
    {
        if (_sessionController?.CurrentActionChoiceRequest is not { } request)
        {
            return [];
        }

        var candidates = new List<PlayModeActionCandidate>();
        foreach (var choice in request.Choices.Where(choice => choice.Kind != ActionChoiceKind.Move))
        {
            candidates.AddRange(choice.Kind switch
            {
                ActionChoiceKind.Pickup => PickupCandidates(choice, _ => true),
                ActionChoiceKind.Drop => DropCandidates(choice),
                ActionChoiceKind.Enter => EnterCandidates(choice, _ => true),
                ActionChoiceKind.Exit => ExitCandidates(choice, _ => true),
                ActionChoiceKind.Transfer => TransferCandidates(choice, _ => true),
                _ => []
            });
        }

        return candidates;
    }

    private IReadOnlyList<PlayModeActionCandidate> PickupCandidates(ActionChoice choice, Func<ControlledActorEntityAffordance, bool> include)
    {
        var targets = choice.EntityOptions.Where(option => option.CanExecute && include(option)).ToList();
        return targets.Select(target =>
        {
            var destinations = choice.Destinations(target.TargetId).Where(destination => destination.CanExecute).ToList();
            return destinations.Count == 1
                ? new PlayModeActionCandidate(
                    $"Pick up {FormatEntityName(target.TargetId)}",
                    IsValid: true,
                    IsComplete: true,
                    Submit: () => SubmitPickupDirect(target.TargetId, destinations[0].Destination))
                : new PlayModeActionCandidate(
                    $"Pick up {FormatEntityName(target.TargetId)}",
                    IsValid: true,
                    IsComplete: false,
                    Explanation: destinations.Count == 0 ? "No valid destination." : "Choose destination.",
                    Refine: destinations.Count == 0 ? null : () => PickupDestinationCandidates(target.TargetId, destinations),
                    RefineTitle: $"Pick up {FormatEntityName(target.TargetId)}: choose destination",
                    RefinedPromptComponent: destinations.Count == 0 ? null : (prompt, bounds) => PlayerInventoryDestinationPanel(prompt.Title, destinations, prompt, bounds));
        }).ToList();
    }

    private IReadOnlyList<PlayModeActionCandidate> DropCandidates(ActionChoice choice)
    {
        var targets = choice.EntityOptions.Where(option => option.CanExecute).ToList();
        if (targets.Count == 0)
        {
            return [];
        }

        return
        [
            new PlayModeActionCandidate(
                "Drop item",
                IsValid: true,
                IsComplete: false,
                Explanation: "Choose carried item.",
                Refine: () => DropItemCandidates(choice, targets),
                RefineTitle: "Drop: choose item",
                RefinedPromptComponent: (prompt, bounds) => PlayerInventoryItemPanel(prompt.Title, targets, prompt, bounds))
        ];
    }

    private IReadOnlyList<PlayModeActionCandidate> DropItemCandidates(ActionChoice choice, IReadOnlyList<ControlledActorEntityAffordance> targets)
    {
        return targets.Select(target =>
        {
            var destinations = choice.Destinations(target.TargetId).Where(destination => destination.CanExecute).ToList();
            return destinations.Count == 1
                ? new PlayModeActionCandidate(
                    $"Drop {FormatEntityName(target.TargetId)}",
                    IsValid: true,
                    IsComplete: true,
                    Submit: () => SubmitDropDirect(target.TargetId, destinations[0].Destination))
                : new PlayModeActionCandidate(
                    $"Drop {FormatEntityName(target.TargetId)}",
                    IsValid: true,
                    IsComplete: false,
                    Explanation: destinations.Count == 0 ? "No valid destination." : "Choose destination.",
                    Refine: destinations.Count == 0 ? null : () => DropDestinationCandidates(target.TargetId, destinations),
                    RefineTitle: $"Drop {FormatEntityName(target.TargetId)}: choose destination",
                    RefinedPromptComponent: destinations.Count == 0 ? null : (prompt, bounds) => CurrentPlaceDestinationPanel(prompt.Title, destinations, prompt, bounds));
        }).ToList();
    }

    private IReadOnlyList<PlayModeActionCandidate> PickupDestinationCandidates(EntityId targetId, IReadOnlyList<ControlledActorDestinationAffordance> destinations) =>
        destinations
            .Where(destination => destination.CanExecute)
            .Select(destination => new PlayModeActionCandidate(
                $"to {FormatDestination(destination.Destination)}",
                IsValid: true,
                IsComplete: true,
                Submit: () => SubmitPickupDirect(targetId, destination.Destination),
                FocusCoord: destination.Destination.Coord))
            .ToList();

    private IReadOnlyList<PlayModeActionCandidate> DropDestinationCandidates(EntityId targetId, IReadOnlyList<ControlledActorDestinationAffordance> destinations) =>
        destinations
            .Where(destination => destination.CanExecute)
            .Select(destination => new PlayModeActionCandidate(
                $"to {FormatDestination(destination.Destination)}",
                IsValid: true,
                IsComplete: true,
                Submit: () => SubmitDropDirect(targetId, destination.Destination),
                ShortcutDirection: DirectionFromActorTo(destination.Destination),
                FocusCoord: destination.Destination.Coord))
            .ToList();

    private IUiComponent PlayerInventoryDestinationPanel(string title, IReadOnlyList<ControlledActorDestinationAffordance> destinations, PlayModePromptLayer prompt, SadConsoleRect bounds)
    {
        var validDestinations = destinations.Where(destination => destination.CanExecute).ToList();
        var selected = validDestinations.ElementAtOrDefault(Math.Clamp(prompt.FocusedIndex, 0, Math.Max(0, validDestinations.Count - 1)))?.Destination.Coord;
        return PlayerInventoryPanel(title, bounds, selected, focused: selected, ["Choose empty destination cell."]);
    }

    private IUiComponent PlayerInventoryItemPanel(string title, IReadOnlyList<ControlledActorEntityAffordance> targets, PlayModePromptLayer prompt, SadConsoleRect bounds)
    {
        var validTargets = targets.Where(target => target.CanExecute).ToList();
        var selected = validTargets.ElementAtOrDefault(Math.Clamp(prompt.FocusedIndex, 0, Math.Max(0, validTargets.Count - 1)))?.Source?.Coord;
        return PlayerInventoryPanel(title, bounds, selected, focused: selected, ["Choose carried item to drop."]);
    }

    private IUiComponent CurrentPlaceDestinationPanel(string title, IReadOnlyList<ControlledActorDestinationAffordance> destinations, PlayModePromptLayer prompt, SadConsoleRect bounds)
    {
        var validDestinations = destinations.Where(destination => destination.CanExecute).ToList();
        var selected = validDestinations.ElementAtOrDefault(Math.Clamp(prompt.FocusedIndex, 0, Math.Max(0, validDestinations.Count - 1)))?.Destination.Coord;
        return CurrentPlaceInventoryPanel(title, bounds, selected, focused: selected, ["Choose drop destination. Direction keys submit matching adjacent cells."]);
    }

    private IUiComponent PlayerInventoryPanel(string title, SadConsoleRect bounds, GridCoord? selected, GridCoord? focused, IReadOnlyList<string> rows)
    {
        var actorId = _sessionController?.PlayerEntityId ?? Session!.PlayerEntityId;
        var world = _sessionController?.World ?? Session!.World;
        var projection = _panelProjection.Project(world, actorId, Session!.ActionPlans, actorId);
        return InventoryPanelFromProjection("0.3-player-inventory-prompt", title, projection, bounds, selected, focused, rows);
    }

    private IUiComponent CurrentPlaceInventoryPanel(string title, SadConsoleRect bounds, GridCoord? selected, GridCoord? focused, IReadOnlyList<string> rows)
    {
        var actorId = _sessionController?.PlayerEntityId ?? Session!.PlayerEntityId;
        if (CurrentPlaceProjection is not { } projection)
        {
            return new PanelComponent("0.3-current-place-prompt", title, SadConsoleRect.FromSize(0, 0, Math.Min(48, bounds.Width), 6), ["Current place unavailable."], UiComponentState.Error);
        }

        return InventoryPanelFromProjection("0.3-current-place-prompt", title, projection, bounds, selected, focused, rows);
    }

    private static IUiComponent InventoryPanelFromProjection(string id, string title, EntityPanelProjection projection, SadConsoleRect bounds, GridCoord? selected, GridCoord? focused, IReadOnlyList<string> rows)
    {
        if (projection.InventoryGrid is null)
        {
            return new PanelComponent(id, title, SadConsoleRect.FromSize(0, 0, Math.Min(48, bounds.Width), 6), ["Inventory unavailable."], UiComponentState.Error);
        }

        var view = InventorySpaceViewModel.FromProjection(
            $"{id}-view",
            projection,
            selectedCoord: selected,
            focusedCoord: focused,
            cellMetrics: InventorySpaceCellMetrics.Default,
            showFrame: true);
        var sizing = new InventorySpaceComponent(id, title, SadConsoleRect.FromSize(0, 0, 1, 1), view, rows, UiComponentState.Focused, InventorySpaceRenderOptions.FramedDebug);
        return new InventorySpaceComponent(
            id,
            title,
            SadConsoleRect.FromSize(0, 0, Math.Min(bounds.Width, Math.Max(18, sizing.RequiredWidth)), Math.Min(bounds.Height, Math.Max(8, sizing.RequiredHeight))),
            view,
            rows,
            UiComponentState.Focused,
            InventorySpaceRenderOptions.FramedDebug);
    }

    private IReadOnlyList<PlayModeActionCandidate> EnterCandidates(ActionChoice choice, Func<ControlledActorEntityAffordance, bool> include) =>
        choice.EntityOptions
            .Where(option => option.CanExecute && include(option))
            .Select(target => new PlayModeActionCandidate(
                $"Enter {FormatEntityName(target.TargetId)}",
                IsValid: true,
                IsComplete: true,
                Submit: () => SubmitEnterDirect(target.TargetId)))
            .ToList();

    private IReadOnlyList<PlayModeActionCandidate> ExitCandidates(ActionChoice choice, Func<ActionChoiceDirectionOption, bool> include) =>
        choice.DirectionOptions
            .Where(option => option.CanExecute && include(option))
            .Select(option => new PlayModeActionCandidate(
                $"Exit {option.Direction}",
                IsValid: true,
                IsComplete: true,
                Submit: () => SubmitExitDirect(option.Direction)))
            .ToList();

    private IReadOnlyList<PlayModeActionCandidate> TransferCandidates(ActionChoice choice, Func<ActionChoiceTransferCounterpartyOption, bool> include)
    {
        var candidates = new List<PlayModeActionCandidate>();
        foreach (var counterparty in choice.TransferCounterparties.Where(counterparty => counterparty.CanExecute && include(counterparty)))
        {
            var items = choice.TransferItems(counterparty.CounterpartyId).Where(item => item.CanExecute).ToList();
            candidates.Add(new PlayModeActionCandidate(
                $"Transfer with {FormatEntityName(counterparty.CounterpartyId)}",
                IsValid: true,
                IsComplete: false,
                Explanation: items.Count == 0 ? "No transferable item." : "Choose transfer item.",
                Refine: items.Count == 0 ? null : () => TransferItemCandidates(counterparty.CounterpartyId, items),
                RefineTitle: $"Transfer with {FormatEntityName(counterparty.CounterpartyId)}",
                RefinedPromptComponent: items.Count == 0 ? null : (prompt, bounds) => TransferPanel(counterparty.CounterpartyId, items, prompt, bounds)));
        }

        return candidates;
    }

    private IReadOnlyList<PlayModeActionCandidate> TransferItemCandidates(EntityId counterpartyId, IReadOnlyList<ActionChoiceTransferItemOption> items) =>
        items
            .Where(item => item.CanExecute)
            .Select(item => new PlayModeActionCandidate(
                $"{FormatTransferDirection(item)} {FormatEntityName(item.MovingEntityId)}",
                IsValid: true,
                IsComplete: true,
                Submit: () => SubmitTransferDirect(counterpartyId, item.MovingEntityId)))
            .ToList();

    private IUiComponent TransferPanel(EntityId counterpartyId, IReadOnlyList<ActionChoiceTransferItemOption> items, PlayModePromptLayer prompt, SadConsoleRect drawableBounds)
    {
        var actorId = _sessionController?.PlayerEntityId ?? Session!.PlayerEntityId;
        var world = _sessionController?.World ?? Session!.World;
        var actorProjection = _panelProjection.Project(world, actorId, Session!.ActionPlans, actorId);
        var counterpartyProjection = _panelProjection.Project(world, counterpartyId, Session.ActionPlans, actorId);
        var validItems = items.Where(item => item.CanExecute).ToList();
        var selectedItem = validItems[Math.Clamp(prompt.FocusedIndex, 0, Math.Max(0, validItems.Count - 1))];
        var width = Math.Min(Math.Max(48, drawableBounds.Width), 78);
        var height = Math.Min(Math.Max(12, drawableBounds.Height), 22);

        return new TransferInventoryComparisonComponent(
            "0.3.1-transfer-panel",
            prompt.Title,
            SadConsoleRect.FromSize(0, 0, width, height),
            UiComponentState.Focused,
            BuildTransferInventorySide(actorProjection, actorProjection.InventoryGrid, validItems, selectedItem, actorProjection.InventoryGrid?.PlaneId),
            BuildTransferInventorySide(counterpartyProjection, counterpartyProjection.InventoryGrid, validItems, selectedItem, counterpartyProjection.InventoryGrid?.PlaneId),
            $"Selected: {FormatTransferDirection(selectedItem)} {FormatEntityName(selectedItem.MovingEntityId)}",
            "Controls: Up/Down choose item | Enter transfer | Esc back");
    }

    private static TransferInventorySideComponent BuildTransferInventorySide(
        EntityPanelProjection projection,
        InventoryInspectionGrid? grid,
        IReadOnlyList<ActionChoiceTransferItemOption> validItems,
        ActionChoiceTransferItemOption selectedItem,
        PlaneId? planeId)
    {
        if (grid is null || planeId is null)
        {
            return new TransferInventorySideComponent(
                $"{projection.Glyph} {projection.Name}",
                ["inventory unavailable"],
                0,
                0,
                [],
                new HashSet<GridCoord>(),
                null);
        }

        var validCoords = validItems
            .Where(item => item.Source.PlaneId == planeId.Value)
            .Select(item => item.Source.Coord)
            .ToHashSet();
        var selectedCoord = selectedItem.Source.PlaneId == planeId.Value ? selectedItem.Source.Coord : (GridCoord?)null;
        return new TransferInventorySideComponent(
            $"{projection.Glyph} {projection.Name}",
            [$"inventory: {grid.Width}x{grid.Height} {grid.PlaneId}", $"valid items: {validCoords.Count}"],
            grid.Width,
            grid.Height,
            grid.Cells.Select(cell => new InventoryGridCell(cell.Coord, cell.Glyph, cell.Color)).ToList(),
            validCoords,
            selectedCoord);
    }

    private GameplayRuntimeSubmission SubmitMoveDirect(Direction direction)
    {
        if (_sessionController is null)
        {
            LastActionStatus = "Cannot move: session unavailable.";
            return new GameplayRuntimeSubmission(false, LastActionStatus, UsedCoreActionChoice: false);
        }

        var result = _sessionController.SubmitMove(direction);
        LastActionStatus = result.Succeeded
            ? $"Moved {direction}."
            : $"Could not move {direction}: {result.FailureText ?? "blocked"}.";
        return result;
    }

    private GameplayRuntimeSubmission SubmitPickupDirect(EntityId targetId, PlaneCoord destination)
    {
        var result = _sessionController!.SubmitPickupActionChoice(targetId, destination);
        LastActionStatus = result.Succeeded
            ? $"Picked up {FormatEntityName(targetId)}."
            : $"Could not pick up {FormatEntityName(targetId)}: {result.FailureText ?? "failed"}.";
        return result;
    }

    private GameplayRuntimeSubmission SubmitDropDirect(EntityId targetId, PlaneCoord destination)
    {
        var result = _sessionController!.SubmitDropActionChoice(targetId, destination);
        LastActionStatus = result.Succeeded
            ? $"Dropped {FormatEntityName(targetId)}."
            : $"Could not drop {FormatEntityName(targetId)}: {result.FailureText ?? "failed"}.";
        return result;
    }

    private GameplayRuntimeSubmission SubmitEnterDirect(EntityId targetId)
    {
        var result = _sessionController!.SubmitEnterActionChoice(targetId);
        LastActionStatus = result.Succeeded
            ? $"Entered {FormatEntityName(targetId)}."
            : $"Could not enter {FormatEntityName(targetId)}: {result.FailureText ?? "failed"}.";
        return result;
    }

    private GameplayRuntimeSubmission SubmitExitDirect(Direction direction)
    {
        var result = _sessionController!.SubmitExitActionChoice(direction);
        LastActionStatus = result.Succeeded
            ? $"Exited {direction}."
            : $"Could not exit {direction}: {result.FailureText ?? "failed"}.";
        return result;
    }

    private GameplayRuntimeSubmission SubmitTransferDirect(EntityId counterpartyId, EntityId movingEntityId)
    {
        var result = _sessionController!.SubmitTransferActionChoice(counterpartyId, movingEntityId);
        LastActionStatus = result.Succeeded
            ? $"Transferred {FormatEntityName(movingEntityId)} with {FormatEntityName(counterpartyId)}."
            : $"Could not transfer {FormatEntityName(movingEntityId)} with {FormatEntityName(counterpartyId)}: {result.FailureText ?? "failed"}.";
        return result;
    }

    private string FormatEntityName(EntityId entityId)
    {
        var world = _sessionController?.World ?? Session?.World;
        return world is not null && world.Entities.TryGetValue(entityId, out var entity) ? entity.Name : entityId.Value;
    }

    private string FormatDestination(PlaneCoord destination)
    {
        if ((_sessionController?.PlayerEntityId ?? Session?.PlayerEntityId) is { } actorId)
        {
            var world = _sessionController?.World ?? Session?.World;
            if (world?.GetInventoryPlaneId(actorId) == destination.PlaneId)
            {
                return $"player inventory ({destination.Coord.X},{destination.Coord.Y})";
            }
        }

        if (CurrentPlaceProjection?.InventoryGrid?.PlaneId == destination.PlaneId)
        {
            return $"current space ({destination.Coord.X},{destination.Coord.Y})";
        }

        return $"{destination.PlaneId.Value}@({destination.Coord.X},{destination.Coord.Y})";
    }

    private Direction? DirectionFromActorTo(PlaneCoord destination)
    {
        if (_sessionController is null)
        {
            return null;
        }

        var actorLocation = _sessionController.World.GetEntityLocation(_sessionController.PlayerEntityId);
        if (actorLocation.PlaneId != destination.PlaneId)
        {
            return null;
        }

        var dx = destination.Coord.X - actorLocation.Coord.X;
        var dy = destination.Coord.Y - actorLocation.Coord.Y;
        return (dx, dy) switch
        {
            (0, -1) => Direction.North,
            (1, -1) => Direction.NorthEast,
            (1, 0) => Direction.East,
            (1, 1) => Direction.SouthEast,
            (0, 1) => Direction.South,
            (-1, 1) => Direction.SouthWest,
            (-1, 0) => Direction.West,
            (-1, -1) => Direction.NorthWest,
            _ => null
        };
    }

    private string FormatTransferDirection(ActionChoiceTransferItemOption item) => item.TransferDirection switch
    {
        TransferDirection.ActorToTarget => $"Give to {FormatEntityName(item.CounterpartyId)}",
        TransferDirection.TargetToActor => $"Take from {FormatEntityName(item.CounterpartyId)}",
        _ => "Transfer"
    };

    private EntityInspectionAppearance ResolveInspectionAppearance(EntityId entityId)
    {
        if (Session?.Registry.TryGetTemplateIdForEntity(entityId, out var templateId) == true
            && Session.Registry.Presentations.TryGetValue(templateId, out var presentation))
        {
            return presentation.ToInspectionAppearance();
        }

        return new EntityInspectionAppearance('?', PresentationColor.Gray);
    }

    private ActionPlanDescriptor? GetActionPlanDescriptorForEntity(EntityId entityId)
    {
        if (Session is null || !Session.Registry.TryGetTemplateIdForEntity(entityId, out var templateId))
        {
            return null;
        }

        var template = Session.Registry.GetEntityTemplate(templateId);
        return template.DefaultActionPlanId is { } planId
            && Session.Registry.ActionPlanDescriptors.TryGetValue(planId, out var descriptor)
                ? descriptor
                : null;
    }

}

internal sealed record LinkedPlaySpacePresentation(
    IReadOnlyList<InventorySpaceComponent> Nodes,
    ConnectorLineViewModel? Connector,
    LinkedInventorySpaceLayout Layout);

internal sealed record ParentChainPresentation(
    IReadOnlyList<InventorySpaceComponent> Nodes,
    ConnectorLineViewModel? Connector)
{
    public static ParentChainPresentation Empty { get; } = new([], null);
}
