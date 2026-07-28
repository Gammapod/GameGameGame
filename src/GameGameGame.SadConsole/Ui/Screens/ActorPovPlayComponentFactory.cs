using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Components;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal static class ActorPovPlayComponentFactory
{
    public static IReadOnlyList<IUiComponent> MainComponents(ActorPovPlayScreenModel model, bool showDebugLabels = false) =>
        [
            ParentChainComponent(model),
            CurrentPlaceComponent(model, showDebugLabels),
            WorldInspectionComponent(model, showDebugLabels),
            ActorInventoryComponent(model, showDebugLabels),
            ActorInventoryInspectionComponent(model, showDebugLabels)
        ];

    public static IUiComponent ParentChainComponent(ActorPovPlayScreenModel model)
    {
        var region = model.Layout.ParentChain;
        if (region.IsOmitted || region.Bounds.Width == 0 || region.Bounds.Height == 0)
        {
            return new PanelComponent(
                "actor-pov-parent-chain-unavailable",
                "Parent/location chain",
                model.Layout.DiagnosticsRegion.Bounds,
                ["Parent chain region is omitted by layout.", .. model.Diagnostics.Select(diagnostic => $"{diagnostic.Source}:{diagnostic.Code}: {diagnostic.Message}").Take(3)],
                UiComponentState.Unselected,
                "layout omitted");
        }

        var maxNodeRows = Math.Max(1, region.Bounds.Height - 3);
        var visibleNodes = model.Projection.ParentChain.Take(maxNodeRows).ToList();
        var omittedCount = Math.Max(0, model.Projection.ParentChain.Count - visibleNodes.Count);
        var rows = visibleNodes
            .Select((node, index) => FormatParentChainNode(index, node))
            .ToList();

        if (omittedCount > 0)
        {
            if (rows.Count >= maxNodeRows)
            {
                rows[^1] = $"+{omittedCount + 1} hidden ancestor node(s)";
            }
            else
            {
                rows.Add($"+{omittedCount} hidden ancestor node(s)");
            }
        }

        return new PanelComponent(
            "actor-pov-parent-chain",
            "Parent/location chain",
            region.Bounds,
            rows.Count == 0 ? ["No visible parent locations."] : rows,
            UiComponentState.Unselected,
            omittedCount > 0 ? $"{omittedCount} omitted" : null);
    }

    private static string FormatParentChainNode(int index, ActorPovChainNodeProjection node)
    {
        var child = node.ChildEntityId is { } childId
            ? $" -> {childId.Value}{FormatChildCoordinate(node)}"
            : string.Empty;
        return $"{index + 1}. {node.Entity.Glyph} {node.Entity.Name}{child}";
    }

    private static string FormatChildCoordinate(ActorPovChainNodeProjection node) =>
        node.ChildCoordinateInEntityInventory is { } coord ? $" at {coord}" : "";

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

    public static IUiComponent WorldInspectionComponent(ActorPovPlayScreenModel model, bool showDebugLabels = false) =>
        InspectionComponent(
            "actor-pov-world-inspection-grid",
            "actor-pov-world-inspection-empty",
            "World inspection chain",
            model.Layout.WorldInspection,
            model.SelectedWorldInspectionCandidate?.Entity,
            model.ControlledActor.EntityId,
            model.Projection.WorldInspectionCandidates.Count,
            "No selected world inspection candidate.",
            showDebugLabels);

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
