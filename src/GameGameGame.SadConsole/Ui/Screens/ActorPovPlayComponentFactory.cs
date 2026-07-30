using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Components;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal static class ActorPovPlayComponentFactory
{
    public static IReadOnlyList<IUiComponent> MainComponents(ActorPovPlayScreenModel model, bool showDebugLabels = false)
    {
        var currentPlace = CurrentPlaceComponent(model, showDebugLabels);
        return
        [
            .. ParentChainComponents(model, showDebugLabels, currentPlace as InventorySpaceComponent),
            currentPlace,
            .. WorldInspectionComponents(model, showDebugLabels, currentPlace as InventorySpaceComponent),
            ActorInventoryComponent(model, showDebugLabels),
            ActorInventoryInspectionComponent(model, showDebugLabels)
        ];
    }

    public static IReadOnlyList<IUiComponent> ParentChainComponents(
        ActorPovPlayScreenModel model,
        bool showDebugLabels = false,
        InventorySpaceComponent? currentPlaceComponent = null)
    {
        var region = model.Layout.ParentChain;
        if (region.IsOmitted || region.Bounds.Width == 0 || region.Bounds.Height == 0)
        {
            return [new PanelComponent(
                "actor-pov-parent-chain-unavailable",
                "Parent/location chain",
                model.Layout.DiagnosticsRegion.Bounds,
                ["Parent chain region is omitted by layout.", .. model.Diagnostics.Select(diagnostic => $"{diagnostic.Source}:{diagnostic.Code}: {diagnostic.Message}").Take(3)],
                UiComponentState.Unselected,
                "layout omitted")];
        }

        if (model.Projection.ParentChain.Count == 0)
        {
            return [new PanelComponent(
                "actor-pov-parent-chain",
                "Parent/location chain",
                region.Bounds,
                ["No visible parent locations."],
                UiComponentState.Unselected)];
        }

        var chain = model.Projection.ParentChain.ToList();
        var firstVisibleIndex = Math.Max(0, chain.Count - 3);
        var visibleNodes = chain.Skip(firstVisibleIndex).Take(3).ToList();
        var bands = ParentChainBands(region.Bounds, visibleNodes.Count);
        var options = showDebugLabels ? InventorySpaceRenderOptions.Labeled : InventorySpaceRenderOptions.Bare;
        var components = new List<IUiComponent>();
        var inventoryComponents = new List<(int ChainIndex, ActorPovChainNodeProjection Node, InventorySpaceComponent Component)>();

        for (var index = 0; index < visibleNodes.Count; index++)
        {
            var node = visibleNodes[index];
            var chainIndex = firstVisibleIndex + index;
            var band = bands[index];
            if (node.Entity.InventoryGrid is null)
            {
                components.Add(new PanelComponent(
                    $"actor-pov-parent-chain-{chainIndex}-unavailable",
                    node.Entity.Name,
                    band,
                    [$"{node.Entity.Name} has no drawable inventory grid."],
                    UiComponentState.Unselected,
                    "no inventory"));
                continue;
            }

            var view = InventorySpaceViewModel.FromProjection(
                $"0.actor-pov.parent-chain.{chainIndex}.inventory-space",
                node.Entity,
                model.ControlledActor.EntityId,
                cellMetrics: InventorySpaceCellMetrics.Default);
            var sizing = new InventorySpaceComponent(
                $"actor-pov-parent-chain-{chainIndex}-sizing",
                view.Title,
                SadConsoleRect.FromSize(0, 0, 1, 1),
                view,
                options: options);
            var bounds = CenteredClipped(band, sizing.RequiredWidth, sizing.RequiredHeight);
            var component = new InventorySpaceComponent(
                $"actor-pov-parent-chain-{chainIndex}-grid",
                view.Title,
                bounds,
                view,
                state: UiComponentState.Unselected,
                options: options);
            components.Add(component);
            inventoryComponents.Add((chainIndex, node, component));
        }

        if (ParentChainConnector(region.Bounds, firstVisibleIndex, inventoryComponents, currentPlaceComponent) is { } connector)
        {
            components.Add(new ConnectorLineComponent(
                "actor-pov-parent-chain-connectors",
                "Parent/location chain connectors",
                region.Bounds,
                connector));
        }

        return components;
    }

    public static IUiComponent ParentChainComponent(ActorPovPlayScreenModel model) =>
        ParentChainComponents(model).First();

    private static string FormatParentChainNode(int index, ActorPovChainNodeProjection node)
    {
        var child = node.ChildEntityId is { } childId
            ? $" -> {childId.Value}{FormatChildCoordinate(node)}"
            : string.Empty;
        return $"{index + 1}. {node.Entity.Glyph} {node.Entity.Name}{child}";
    }

    private static string FormatChildCoordinate(ActorPovChainNodeProjection node) =>
        node.ChildCoordinateInEntityInventory is { } coord ? $" at {coord}" : "";

    private static IReadOnlyList<SadConsoleRect> ParentChainBands(SadConsoleRect bounds, int count)
    {
        var slotCount = Math.Max(1, Math.Min(3, count));
        var baseHeight = bounds.Height / slotCount;
        var remainder = bounds.Height % slotCount;
        var bands = new List<SadConsoleRect>();
        var top = bounds.Top;
        for (var index = 0; index < slotCount; index++)
        {
            var height = baseHeight + (index < remainder ? 1 : 0);
            bands.Add(SadConsoleRect.FromSize(bounds.Left, top, bounds.Width, height));
            top += height;
        }

        return bands;
    }

    private static ConnectorLineViewModel? ParentChainConnector(
        SadConsoleRect parentChainBounds,
        int firstVisibleIndex,
        IReadOnlyList<(int ChainIndex, ActorPovChainNodeProjection Node, InventorySpaceComponent Component)> visible,
        InventorySpaceComponent? currentPlaceComponent)
    {
        var segments = new List<ConnectorLineSegment>();
        var byIndex = visible.ToDictionary(item => item.ChainIndex);
        foreach (var item in visible)
        {
            if (item.Node.ChildCoordinateInEntityInventory is not { } childCoord || !item.Component.View.IsVisible(childCoord))
            {
                continue;
            }

            if (byIndex.TryGetValue(item.ChainIndex + 1, out var child))
            {
                segments.Add(new ConnectorLineSegment(
                    $"parent-chain-{item.ChainIndex}-to-{child.ChainIndex}",
                    CenterOf(item.Component.CellBounds(childCoord), $"parent-chain-{item.ChainIndex}-owning-cell"),
                    new ConnectorLineEndpoint(
                        $"parent-chain-{child.ChainIndex}-node-top",
                        child.Component.Bounds.Left + (child.Component.Bounds.Width / 2),
                        child.Component.Bounds.Top,
                        AnchorX: 0.5f,
                        AnchorY: 0f),
                    PresentationColor.Cyan,
                    Layer: 1));
            }
        }

        if (currentPlaceComponent is not null
            && visible.LastOrDefault() is var immediateParent
            && immediateParent.Component is not null
            && immediateParent.Node.ChildCoordinateInEntityInventory is { } currentPlaceCoord
            && immediateParent.Component.View.IsVisible(currentPlaceCoord))
        {
            segments.Add(new ConnectorLineSegment(
                $"parent-chain-{immediateParent.ChainIndex}-to-current-place",
                CenterOf(immediateParent.Component.CellBounds(currentPlaceCoord), $"parent-chain-{immediateParent.ChainIndex}-current-place-owning-cell"),
                new ConnectorLineEndpoint(
                    "current-place-node-left-edge",
                    currentPlaceComponent.Bounds.Left,
                    currentPlaceComponent.Bounds.Top + (currentPlaceComponent.Bounds.Height / 2),
                    AnchorX: 0f,
                    AnchorY: 0.5f),
                PresentationColor.Cyan,
                Layer: 1));
        }

        if (firstVisibleIndex > 0 && visible.FirstOrDefault() is var top && top.Component is not null)
        {
            segments.Add(new ConnectorLineSegment(
                "parent-chain-more-ancestors-offscreen",
                new ConnectorLineEndpoint(
                    "parent-chain-top-node-top",
                    top.Component.Bounds.Left + (top.Component.Bounds.Width / 2),
                    top.Component.Bounds.Top,
                    AnchorX: 0.5f,
                    AnchorY: 0f),
                new ConnectorLineEndpoint(
                    "parent-chain-offscreen-ancestor",
                    top.Component.Bounds.Left + (top.Component.Bounds.Width / 2),
                    parentChainBounds.Top - 1,
                    AnchorX: 0.5f,
                    AnchorY: 1f),
                PresentationColor.Cyan,
                Layer: 0));
        }

        return segments.Count == 0
            ? null
            : new ConnectorLineViewModel(
                "actor-pov-parent-chain.connector",
                "Parent chain ownership links",
                segments,
                ConnectorLineFallbackGlyphs.Ascii);
    }

    private static ConnectorLineEndpoint CenterOf(SadConsoleRect bounds, string id) =>
        new(id, bounds.Left + (bounds.Width / 2), bounds.Top + (bounds.Height / 2));

    public static IUiComponent CurrentPlaceComponent(ActorPovPlayScreenModel model, bool showDebugLabels = false)
    {
        var region = model.Layout.CurrentPlace;
        if (region.IsOmitted || region.Bounds.Width == 0 || region.Bounds.Height == 0)
        {
            return new PanelComponent(
                "actor-pov-current-place-unavailable",
                "Current actor POV",
                model.Layout.DiagnosticsRegion.Bounds,
                ["Current POV region is omitted by layout.", .. model.Diagnostics.Select(diagnostic => $"{diagnostic.Source}:{diagnostic.Code}: {diagnostic.Message}").Take(3)],
                UiComponentState.Selected,
                "layout omitted");
        }

        if (model.CurrentPlace?.InventoryGrid is not { })
        {
            return new PanelComponent(
                "actor-pov-current-place-empty",
                "Current actor POV",
                region.Bounds,
                ["Controlled actor POV did not resolve a drawable current place.", .. model.Diagnostics.Select(diagnostic => $"{diagnostic.Source}:{diagnostic.Code}: {diagnostic.Message}").Take(4)],
                UiComponentState.Focused,
                "no current place");
        }

        var view = InventorySpaceViewModel.FromProjection(
            "0.actor-pov.current-place.inventory-space",
            model.CurrentPlace,
            model.ControlledActor.EntityId,
            cellMetrics: InventorySpaceCellMetrics.Default);
        var options = showDebugLabels ? InventorySpaceRenderOptions.Labeled : InventorySpaceRenderOptions.Bare;
        var sizing = new InventorySpaceComponent(
            "actor-pov-current-place-sizing",
            view.Title,
            SadConsoleRect.FromSize(0, 0, 1, 1),
            view,
            options: options);
        var bounds = CenteredClipped(region.Bounds, sizing.RequiredWidth, sizing.RequiredHeight);

        return new InventorySpaceComponent(
            "actor-pov-current-place-grid",
            view.Title,
            bounds,
            view,
            state: UiComponentState.Focused,
            options: options);
    }

    public static IUiComponent ActorInventoryComponent(ActorPovPlayScreenModel model, bool showDebugLabels = false)
    {
        var region = model.Layout.ActorInventory;
        if (region.IsOmitted || region.Bounds.Width == 0 || region.Bounds.Height == 0)
        {
            return new PanelComponent(
                "actor-pov-actor-inventory-unavailable",
                "Controlled actor inventory",
                model.Layout.DiagnosticsRegion.Bounds,
                ["Actor inventory region is omitted by layout.", .. model.Diagnostics.Select(diagnostic => $"{diagnostic.Source}:{diagnostic.Code}: {diagnostic.Message}").Take(3)],
                UiComponentState.Selected,
                "layout omitted");
        }

        if (model.ActorInventory?.InventoryGrid is not { })
        {
            return new PanelComponent(
                "actor-pov-actor-inventory-empty",
                "Controlled actor inventory",
                region.Bounds,
                ["Controlled actor has no drawable inventory grid.", $"Actor: {model.ControlledActor.Name} ({model.ControlledActor.EntityId})"],
                UiComponentState.Selected,
                "no actor inventory");
        }

        var view = InventorySpaceViewModel.FromProjection(
            "0.actor-pov.actor-inventory.inventory-space",
            model.ActorInventory,
            model.ControlledActor.EntityId,
            cellMetrics: InventorySpaceCellMetrics.Default);
        var options = showDebugLabels ? InventorySpaceRenderOptions.Labeled : InventorySpaceRenderOptions.Bare;
        var sizing = new InventorySpaceComponent(
            "actor-pov-actor-inventory-sizing",
            view.Title,
            SadConsoleRect.FromSize(0, 0, 1, 1),
            view,
            options: options);
        var bounds = CenteredClipped(region.Bounds, sizing.RequiredWidth, sizing.RequiredHeight);

        return new InventorySpaceComponent(
            "actor-pov-actor-inventory-grid",
            view.Title,
            bounds,
            view,
            state: UiComponentState.Selected,
            options: options);
    }

    public static IReadOnlyList<IUiComponent> WorldInspectionComponents(
        ActorPovPlayScreenModel model,
        bool showDebugLabels = false,
        InventorySpaceComponent? currentPlaceComponent = null)
    {
        var region = model.Layout.WorldInspection;
        if (region.IsOmitted || region.Bounds.Width == 0 || region.Bounds.Height == 0)
        {
            return [new PanelComponent(
                "actor-pov-world-inspection-empty",
                "Adjacent world inspection",
                region.Bounds,
                ["Adjacent world inspection region is omitted by layout."],
                UiComponentState.Unselected,
                "layout omitted")];
        }

        var actorCoord = model.ControlledActor.Location.Coord;
        var byDirection = model.Projection.WorldInspectionCandidates
            .GroupBy(candidate => DirectionFromActorCoord(actorCoord, candidate.CoordinateInSourceInventory))
            .Where(group => group.Key is not null)
            .ToDictionary(group => group.Key!.Value, group => group.First());
        var options = showDebugLabels ? InventorySpaceRenderOptions.Labeled : InventorySpaceRenderOptions.Bare;
        var components = new List<IUiComponent>();
        var inspectedComponents = new List<(Direction Direction, ActorPovInspectionCandidateProjection Candidate, InventorySpaceComponent Component)>();
        foreach (var slot in AdjacentInspectionSlots(region.Bounds))
        {
            byDirection.TryGetValue(slot.Direction, out var candidate);
            var component = AdjacentWorldInspectionComponent(
                slot.Direction,
                slot.Bounds,
                candidate,
                model.ControlledActor.EntityId,
                options);
            components.Add(component);
            if (candidate is not null && component is InventorySpaceComponent inspectedComponent)
            {
                inspectedComponents.Add((slot.Direction, candidate, inspectedComponent));
            }
        }

        if (WorldInspectionConnector(region.Bounds, currentPlaceComponent, inspectedComponents) is { } connector)
        {
            components.Add(new ConnectorLineComponent(
                "actor-pov-world-inspection-connectors",
                "Adjacent world inspection connectors",
                region.Bounds,
                connector));
        }

        return components;
    }

    public static IUiComponent WorldInspectionComponent(ActorPovPlayScreenModel model, bool showDebugLabels = false) =>
        WorldInspectionComponents(model, showDebugLabels).First();

    public static IUiComponent ActorInventoryInspectionComponent(ActorPovPlayScreenModel model, bool showDebugLabels = false) =>
        InspectionComponent(
            "actor-pov-actor-inventory-inspection-grid",
            "actor-pov-actor-inventory-inspection-empty",
            "Actor carried-item inspection chain",
            model.Layout.ActorInventoryInspection,
            model.SelectedCarriedInspectionCandidate?.Entity,
            model.ControlledActor.EntityId,
            model.Projection.CarriedInspectionCandidates.Count,
            "No selected carried-item inspection candidate.",
            showDebugLabels);

    public static IUiComponent DiagnosticsChromeComponent(ActorPovPlayScreenModel model)
    {
        var rows = new List<string>
        {
            $"focused: {model.PresentationState.FocusedRegionId ?? "none"}",
            $"world inspect: {model.PresentationState.SelectedWorldInspectionEntityId?.Value ?? "none"} / {model.Projection.WorldInspectionCandidates.Count} candidate(s)",
            $"carried inspect: {model.PresentationState.SelectedCarriedInspectionEntityId?.Value ?? "none"} / {model.Projection.CarriedInspectionCandidates.Count} candidate(s)"
        };

        var omittedRegions = model.Layout.Regions
            .Where(region => region.IsOmitted)
            .Select(region => ShortRegionId(region.Id))
            .ToList();
        rows.Add(omittedRegions.Count == 0
            ? "omitted regions: none"
            : $"omitted regions: {string.Join(", ", omittedRegions)}");

        rows.AddRange(model.Layout.Regions
            .Where(region => region.Role == ActorPovPlayRegionRole.Content)
            .Take(5)
            .Select(region => $"{ShortRegionId(region.Id)}: {FormatRect(region.Bounds)}"));
        rows.AddRange(model.Diagnostics
            .Take(4)
            .Select(diagnostic => $"{diagnostic.Source}:{diagnostic.Code}: {diagnostic.Message}"));

        return new PanelComponent(
            "actor-pov-diagnostics-chrome",
            "Actor POV layout diagnostics",
            model.Layout.DiagnosticsRegion.Bounds,
            rows,
            model.Diagnostics.Count == 0 ? UiComponentState.Unselected : UiComponentState.Error,
            model.Diagnostics.Count == 0 ? "ready" : $"{model.Diagnostics.Count} diagnostic(s)");
    }

    private static IUiComponent InspectionComponent(
        string gridId,
        string emptyId,
        string title,
        ActorPovPlayRegion region,
        EntityPanelProjection? selectedProjection,
        EntityId controlledActorId,
        int candidateCount,
        string emptyText,
        bool showDebugLabels)
    {
        if (region.IsOmitted || region.Bounds.Width == 0 || region.Bounds.Height == 0)
        {
            return new PanelComponent(
                emptyId,
                title,
                region.Bounds,
                [$"{title} region is omitted by layout."],
                UiComponentState.Unselected,
                "layout omitted");
        }

        if (selectedProjection?.InventoryGrid is not { })
        {
            return new PanelComponent(
                emptyId,
                title,
                region.Bounds,
                [emptyText, $"Candidates: {candidateCount}"],
                UiComponentState.Unselected,
                candidateCount == 0 ? "no candidates" : "no selection");
        }

        var view = InventorySpaceViewModel.FromProjection(
            $"0.{gridId}.inventory-space",
            selectedProjection,
            controlledActorId,
            cellMetrics: InventorySpaceCellMetrics.Default);
        var options = showDebugLabels ? InventorySpaceRenderOptions.Labeled : InventorySpaceRenderOptions.Bare;
        var sizing = new InventorySpaceComponent(
            $"{gridId}-sizing",
            view.Title,
            SadConsoleRect.FromSize(0, 0, 1, 1),
            view,
            options: options);
        var bounds = CenteredClipped(region.Bounds, sizing.RequiredWidth, sizing.RequiredHeight);

        return new InventorySpaceComponent(
            gridId,
            view.Title,
            bounds,
            view,
            state: UiComponentState.Selected,
            options: options);
    }

    private static IUiComponent AdjacentWorldInspectionComponent(
        Direction direction,
        SadConsoleRect bounds,
        ActorPovInspectionCandidateProjection? candidate,
        EntityId controlledActorId,
        InventorySpaceRenderOptions options)
    {
        var suffix = DirectionId(direction);
        var title = $"{DirectionLabel(direction)} inspection";
        if (candidate?.Entity.InventoryGrid is not { })
        {
            return new PanelComponent(
                $"actor-pov-world-inspection-{suffix}-empty",
                title,
                bounds,
                [$"No adjacent inventory space {DirectionLabel(direction)}."],
                UiComponentState.Unselected,
                "empty");
        }

        var view = InventorySpaceViewModel.FromProjection(
            $"0.actor-pov.world-inspection.{suffix}.inventory-space",
            candidate.Entity,
            controlledActorId,
            cellMetrics: InventorySpaceCellMetrics.Default);
        var sizing = new InventorySpaceComponent(
            $"actor-pov-world-inspection-{suffix}-sizing",
            view.Title,
            SadConsoleRect.FromSize(0, 0, 1, 1),
            view,
            options: options);
        var gridBounds = CenteredClipped(bounds, sizing.RequiredWidth, sizing.RequiredHeight);
        return new InventorySpaceComponent(
            $"actor-pov-world-inspection-{suffix}-grid",
            view.Title,
            gridBounds,
            view,
            state: UiComponentState.Selected,
            options: options);
    }

    private static ConnectorLineViewModel? WorldInspectionConnector(
        SadConsoleRect worldInspectionBounds,
        InventorySpaceComponent? currentPlaceComponent,
        IReadOnlyList<(Direction Direction, ActorPovInspectionCandidateProjection Candidate, InventorySpaceComponent Component)> inspectedComponents)
    {
        if (currentPlaceComponent is null || inspectedComponents.Count == 0)
        {
            return null;
        }

        var segments = new List<ConnectorLineSegment>();
        foreach (var inspected in inspectedComponents)
        {
            if (!currentPlaceComponent.View.IsVisible(inspected.Candidate.CoordinateInSourceInventory))
            {
                continue;
            }

            var sourceCell = currentPlaceComponent.CellBounds(inspected.Candidate.CoordinateInSourceInventory);
            var targetBounds = inspected.Component.Bounds;
            var suffix = DirectionId(inspected.Direction);
            segments.Add(new ConnectorLineSegment(
                $"current-place-{suffix}-to-world-inspection-{suffix}",
                CenterOf(sourceCell, $"current-place-{suffix}-entity-cell"),
                new ConnectorLineEndpoint(
                    $"world-inspection-{suffix}-node-left-edge",
                    targetBounds.Left,
                    targetBounds.Top + (targetBounds.Height / 2),
                    AnchorX: 0f,
                    AnchorY: 0.5f),
                PresentationColor.Cyan,
                Layer: 1));
        }

        return segments.Count == 0
            ? null
            : new ConnectorLineViewModel(
                "actor-pov-world-inspection.connector",
                "Adjacent world inspection ownership links",
                segments,
                ConnectorLineFallbackGlyphs.Ascii);
    }

    private static IReadOnlyList<(Direction Direction, SadConsoleRect Bounds)> AdjacentInspectionSlots(SadConsoleRect bounds)
    {
        var directions = new[]
        {
            Direction.NorthWest,
            Direction.North,
            Direction.NorthEast,
            Direction.East,
            Direction.SouthEast,
            Direction.South,
            Direction.SouthWest,
            Direction.West
        };
        var slots = new List<(Direction Direction, SadConsoleRect Bounds)>();
        var leftWidth = bounds.Width / 2;
        var rightWidth = bounds.Width - leftWidth;
        var baseHeight = bounds.Height / 4;
        var extraHeight = bounds.Height % 4;

        for (var column = 0; column < 2; column++)
        {
            var top = bounds.Top;
            var left = column == 0 ? bounds.Left : bounds.Left + leftWidth;
            var width = column == 0 ? leftWidth : rightWidth;
            for (var row = 0; row < 4; row++)
            {
                var height = baseHeight + (row < extraHeight ? 1 : 0);
                slots.Add((directions[(column * 4) + row], SadConsoleRect.FromSize(left, top, width, height)));
                top += height;
            }
        }

        return slots;
    }

    private static Direction? DirectionFromActorCoord(GridCoord actorCoord, GridCoord candidateCoord)
    {
        foreach (var direction in AdjacentDirections)
        {
            if (actorCoord.Offset(direction) == candidateCoord)
            {
                return direction;
            }
        }

        return null;
    }

    private static IReadOnlyList<Direction> AdjacentDirections { get; } =
    [
        Direction.NorthWest,
        Direction.North,
        Direction.NorthEast,
        Direction.East,
        Direction.SouthEast,
        Direction.South,
        Direction.SouthWest,
        Direction.West
    ];

    private static string DirectionId(Direction direction) => direction.ToString().ToLowerInvariant();

    private static string DirectionLabel(Direction direction) => direction switch
    {
        Direction.NorthWest => "northwest",
        Direction.North => "north",
        Direction.NorthEast => "northeast",
        Direction.East => "east",
        Direction.SouthEast => "southeast",
        Direction.South => "south",
        Direction.SouthWest => "southwest",
        Direction.West => "west",
        _ => direction.ToString()
    };

    private static SadConsoleRect CenteredClipped(SadConsoleRect region, int requiredWidth, int requiredHeight)
    {
        var width = Math.Min(region.Width, requiredWidth);
        var height = Math.Min(region.Height, requiredHeight);
        return SadConsoleRect.FromSize(
            region.Left + Math.Max(0, (region.Width - width) / 2),
            region.Top + Math.Max(0, (region.Height - height) / 2),
            width,
            height);
    }

    private static string FormatRect(SadConsoleRect rect) =>
        $"L{rect.Left},T{rect.Top},W{rect.Width},H{rect.Height}";

    private static string ShortRegionId(string id) => id.Replace("0.actor-pov.", string.Empty, StringComparison.Ordinal)
        .Replace("0.actor-pov-root", "root", StringComparison.Ordinal);
}
