using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Styling;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed record GameplayMockFrame(
    string Title,
    EntityPanelProjection PlayerProjection,
    EntityPanelProjection? CurrentPlaceProjection,
    EntityPanelProjection? InspectedProjection,
    SadConsoleRect CurrentPlaceBounds,
    SadConsoleRect HudBounds,
    SadConsoleRect InspectionBounds,
    IReadOnlyList<IUiComponent> Components,
    IReadOnlyList<string> HudRows,
    IReadOnlyList<string> Diagnostics);

internal sealed class GameplayMockScreen
{
    private readonly PlayableScenarioSession _session;
    private readonly EntityPanelProjectionService _panelProjection;
    private EntityId? _inspectedEntityId;

    public GameplayMockScreen(PlayableScenarioSession session)
    {
        _session = session;
        _panelProjection = new EntityPanelProjectionService(entityId =>
            session.Registry.GetPresentationForEntity(entityId).ToInspectionAppearance());
    }

    public EntityId PlayerEntityId => _session.PlayerEntityId;
    public EntityId? InspectedEntityId => _inspectedEntityId;

    public GameplayMockFrame BuildFrame(int width, int height)
    {
        var safeWidth = Math.Max(40, width);
        var safeHeight = Math.Max(18, height);
        var playerProjection = _panelProjection.Project(_session.World, _session.PlayerEntityId, _session.ActionPlans, _session.PlayerEntityId);
        var diagnostics = new List<string>();
        diagnostics.AddRange(_session.ValidationDiagnostics);
        diagnostics.AddRange(_session.RuntimeFailures);
        diagnostics.AddRange(_session.CapabilityGaps);

        if (playerProjection.PointOfView is null)
        {
            diagnostics.Add("Player POV projection is unavailable.");
        }
        else
        {
            diagnostics.AddRange(playerProjection.PointOfView.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
        }

        var currentPlaceProjection = playerProjection.PointOfView?.CurrentPlace is { } currentPlace
            ? _panelProjection.Project(_session.World, currentPlace.EntityId, _session.ActionPlans, _session.PlayerEntityId)
            : null;

        var hudWidth = Math.Clamp(safeWidth / 5, 20, Math.Max(20, safeWidth - 42));
        var hudBounds = SadConsoleRect.FromSize(0, 0, hudWidth, safeHeight);
        var contentLeft = hudBounds.Left + hudBounds.Width + 1;
        var contentWidth = Math.Max(20, safeWidth - contentLeft - 1);
        var inspectionHeight = Math.Max(8, safeHeight / 3);
        var inspectionTop = Math.Max(8, safeHeight - inspectionHeight);
        var currentPlaceBounds = SadConsoleRect.FromSize(contentLeft, 0, contentWidth, inspectionTop);
        var inspectionBounds = SadConsoleRect.FromSize(
            contentLeft,
            inspectionTop,
            contentWidth,
            Math.Max(0, safeHeight - inspectionTop));

        var components = new List<IUiComponent>();
        components.Add(BuildCurrentPlaceComponent(currentPlaceProjection, currentPlaceBounds));
        EntityPanelProjection? inspectedProjection = null;
        if (_inspectedEntityId is { } inspectedEntityId && _session.World.Entities.ContainsKey(inspectedEntityId))
        {
            inspectedProjection = _panelProjection.Project(_session.World, inspectedEntityId, _session.ActionPlans, _session.PlayerEntityId);
            components.Add(BuildInspectionComponent(inspectedProjection, inspectionBounds));
        }

        if (diagnostics.Count > 0)
        {
            components.Add(new PanelComponent(
                "play-mock-diagnostics",
                "POV / setup diagnostics",
                SadConsoleRect.FromSize(contentLeft, Math.Max(1, currentPlaceBounds.Bottom - 7), Math.Min(60, contentWidth), Math.Min(6, currentPlaceBounds.Height - 2)),
                diagnostics.Take(4).ToList(),
                UiComponentState.Error));
        }

        return new GameplayMockFrame(
            $"Play UX Mock | {_session.Name} | turn-0 frame only",
            playerProjection,
            currentPlaceProjection,
            inspectedProjection,
            currentPlaceBounds,
            hudBounds,
            inspectionBounds,
            components,
            BuildHudRows(playerProjection, currentPlaceProjection),
            diagnostics);
    }

    public string InspectNextEntity()
    {
        var frame = BuildFrame(SadConsoleScreenMetrics.ScreenWidth, SadConsoleScreenMetrics.ScreenHeight);
        var candidates = frame.CurrentPlaceProjection?.InventoryGrid?.Cells
            .Select(cell => cell.EntityId)
            .Where(entityId => entityId is not null && entityId != _session.PlayerEntityId)
            .Select(entityId => entityId!.Value)
            .Distinct()
            .OrderBy(entityId => entityId.Value, StringComparer.Ordinal)
            .ToList() ?? [];

        if (candidates.Count == 0)
        {
            _inspectedEntityId = null;
            return "No non-player entity is visible in the current POV place.";
        }

        var nextIndex = _inspectedEntityId is null ? 0 : (candidates.IndexOf(_inspectedEntityId.Value) + 1) % candidates.Count;
        _inspectedEntityId = candidates[Math.Max(0, nextIndex)];
        return $"Inspecting {_session.World.Entities[_inspectedEntityId.Value].Name}.";
    }

    public void ClearInspection() => _inspectedEntityId = null;

    private static IUiComponent BuildCurrentPlaceComponent(EntityPanelProjection? currentPlaceProjection, SadConsoleRect bounds)
    {
        if (currentPlaceProjection?.InventoryGrid is not { } grid)
        {
            return new PanelComponent(
                "current-place",
                "Current place viewport",
                bounds,
                ["Player POV did not resolve to a usable current-place inventory."],
                UiComponentState.Error);
        }

        return new InventoryGridComponent(
            "current-place",
            $"Current place: {currentPlaceProjection.Name}",
            bounds,
            [$"plane: {grid.PlaneId}", $"inventory: {grid.Width}x{grid.Height}", "centered from player POV"],
            UiComponentState.Selected,
            grid.Width,
            grid.Height,
            new GridCoord(0, 0),
            grid.Cells.Select(cell => new InventoryGridCell(cell.Coord, cell.Glyph, cell.Color)).ToList());
    }

    private static IUiComponent BuildInspectionComponent(EntityPanelProjection projection, SadConsoleRect bounds)
    {
        var rows = new List<string>
        {
            $"{projection.Glyph} {projection.Name}",
            $"id: {projection.EntityId}",
            $"path: {FormatBreadcrumbFromCurrentPlace(projection)}"
        };
        rows.AddRange(projection.Properties.Take(4).Select(property => $"{property.Name}: {property.Value}"));
        if (projection.InventoryGrid is { } grid)
        {
            rows.Add($"inventory: {grid.Width}x{grid.Height} {grid.PlaneId}");
        }

        return new PanelComponent("inspected-entity", "Inspected entity panel", bounds, rows, UiComponentState.Focused);
    }

    private static IReadOnlyList<string> BuildHudRows(EntityPanelProjection playerProjection, EntityPanelProjection? currentPlaceProjection)
    {
        var rows = new List<string>
        {
            $"Player: {playerProjection.Glyph} {playerProjection.Name} ({playerProjection.EntityId})",
            $"Facing: {playerProjection.ActionState.Facing?.ToString() ?? "none"} | Target: {playerProjection.ActionState.Target?.ToString() ?? "none"}",
            $"Current place: {currentPlaceProjection?.Name ?? "none"}"
        };

        if (playerProjection.PointOfView?.CurrentPlace is { } place)
        {
            rows.Add($"Bulk/aperture: {place.ObserverBulk}/{place.PlaceAperture} ratio {place.BulkToApertureRatio?.ToString() ?? "n/a"}");
            rows.Add($"POV rule: {place.SelectionRule}");
        }
        else
        {
            rows.Add("POV rule: unresolved");
        }

        rows.Add("Mock controls: I inspect | F11 fullscreen | Esc exits | no turns advance");
        return rows;
    }

    private static string FormatBreadcrumbFromCurrentPlace(EntityPanelProjection projection) =>
        string.Join(" > ", projection.Breadcrumb.Segments.Select(segment => segment.EntityId.Value));
}
