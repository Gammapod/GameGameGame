using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Components;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed class ConsumerPlayModeScreen
{
    private readonly EntityPanelProjectionService _panelProjection;

    private ConsumerPlayModeScreen(ScenarioCatalogEntry catalogEntry, PlayableScenarioSession? session, string? launchFailure)
    {
        CatalogEntry = catalogEntry;
        Session = session;
        LaunchFailure = launchFailure;
        _panelProjection = new EntityPanelProjectionService(ResolveInspectionAppearance, GetActionPlanDescriptorForEntity);

        if (session is not null)
        {
            ControlledActorProjection = _panelProjection.Project(
                session.World,
                session.PlayerEntityId,
                session.ActionPlans,
                session.PlayerEntityId);
            CurrentPlaceProjection = ControlledActorProjection.PointOfView?.CurrentPlace is { } currentPlace
                ? _panelProjection.Project(session.World, currentPlace.EntityId, session.ActionPlans, session.PlayerEntityId)
                : null;
            CurrentSpaceView = CurrentPlaceProjection?.InventoryGrid is not null
                ? InventorySpaceViewModel.FromProjection(
                    "0.2.inventory-space",
                    CurrentPlaceProjection,
                    session.PlayerEntityId,
                    cellMetrics: InventorySpaceCellMetrics.Default)
                : null;
        }
    }

    public ScenarioCatalogEntry CatalogEntry { get; }
    public PlayableScenarioSession? Session { get; }
    public string? LaunchFailure { get; }
    public EntityPanelProjection? ControlledActorProjection { get; }
    public EntityPanelProjection? CurrentPlaceProjection { get; }
    public InventorySpaceViewModel? CurrentSpaceView { get; }
    public string Title => "New Play Mode";
    public string Purpose => "Consumer-facing Play mode skeleton. Current-space component is active.";
    public string FooterText => "Esc: return to Scenario Selection | F12: toggle debug border";

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

    public InventorySpaceComponent? CurrentSpaceGridComponent(SadConsoleRect drawableBounds, bool showDebugLabels)
    {
        if (CurrentSpaceView is not { } currentSpaceView)
        {
            return null;
        }

        var bareSizing = new InventorySpaceComponent(
            "current-space-grid-sizing",
            currentSpaceView.Title,
            SadConsoleRect.FromSize(0, 0, 1, 1),
            currentSpaceView,
            options: InventorySpaceRenderOptions.Bare);

        var gridLeft = drawableBounds.Left + Math.Max(0, (drawableBounds.Width - bareSizing.RequiredWidth) / 2);
        var gridTop = drawableBounds.Top + Math.Max(0, (drawableBounds.Height - bareSizing.RequiredHeight) / 2);
        var bounds = SadConsoleRect.FromSize(
            gridLeft,
            gridTop,
            Math.Min(drawableBounds.Width, bareSizing.RequiredWidth),
            Math.Min(drawableBounds.Height, bareSizing.RequiredHeight));

        if (showDebugLabels)
        {
            bounds = SadConsoleRect.FromSize(
                Math.Max(drawableBounds.Left, bounds.Left - 4),
                Math.Max(drawableBounds.Top, bounds.Top - 1),
                Math.Min(drawableBounds.Width, bounds.Width + 4),
                Math.Min(drawableBounds.Height, bounds.Height + 1));
        }

        return new InventorySpaceComponent(
            "current-space-grid",
            currentSpaceView.Title,
            bounds,
            currentSpaceView,
            state: UiComponentState.Focused,
            options: showDebugLabels ? InventorySpaceRenderOptions.Labeled : InventorySpaceRenderOptions.Bare);
    }

    public IReadOnlyList<string> DebugRows()
    {
        var rows = new List<string>();
        rows.AddRange(BuildStatusRows());
        rows.AddRange(BuildCurrentSpaceRows());
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
        return CurrentSpaceGridComponent(drawableBounds, showDebugLabels: false) is { } grid
            ? [grid]
            : [];
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

        return rows;
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
