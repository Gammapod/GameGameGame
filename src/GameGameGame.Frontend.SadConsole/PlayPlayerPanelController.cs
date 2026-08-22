using GameGameGame.Content;
using GameGameGame.Core;
using SadRogue.Primitives;
using Console = SadConsole.Console;

namespace GameGameGame.Frontend.SadConsole;

internal sealed class PlayPlayerPanelController(
    Console owner,
    PlayableScenarioSession session,
    PlayActionSessionController actionSession,
    SadConsoleDisplaySettings displaySettings,
    TilesetProfile tilesetProfile)
{
    private EntityInspectionOverlayConsole? _overlay;
    private EntityInspectionPanelModel? _cachedModel;
    private PlayHighlightState? _cachedHighlight;
    private PlayHighlightState? _cachedInventoryHighlight;
    private FrontendRect? _drawnBounds;
    private bool _modelDirty = true;
    private bool _modelChanged = true;
    private bool? _drawnFocused;
    private int _selectedActionIndex;

    public bool IsFocused { get; private set; }

    public void ToggleFocus()
    {
        IsFocused = !IsFocused;
        _drawnFocused = null;
    }

    public void ReturnToGrid()
    {
        IsFocused = false;
        _drawnFocused = null;
    }

    public void MarkWorldChanged() => _modelDirty = true;

    public EntityInspectionActionRow? SelectedActionRow => _cachedModel is { Actions.Count: > 0 } model
        ? model.Actions[Math.Clamp(_selectedActionIndex, 0, model.Actions.Count - 1)]
        : null;

    public string MoveSelection(int delta)
    {
        var count = _cachedModel?.Actions.Count ?? 0;
        if (count == 0)
        {
            return "No player actions.";
        }

        _selectedActionIndex = Math.Clamp(_selectedActionIndex + delta, 0, count - 1);
        _drawnFocused = null;
        return "Player panel action selected.";
    }

    public string ConfirmSelectedActionMessage() => SelectedActionRow is { Selectable: false, FailureReason: { } reason }
        ? FrontendTextResolver.InspectionPrototype.Resolve(reason)
        : "Player panel action semantics are deferred.";

    public void Draw(FrontendRect? bounds, PlayGridViewModel grid, PlayHighlightState? highlight, PlayHighlightState? inventoryHighlight = null)
    {
        if (bounds is null)
        {
            ClearOverlay();
            return;
        }

        var model = ResolveModel(grid, highlight, inventoryHighlight);
        DrawOverlay(bounds, model);
    }

    private EntityInspectionPanelModel ResolveModel(PlayGridViewModel grid, PlayHighlightState? highlight, PlayHighlightState? inventoryHighlight)
    {
        if (!_modelDirty && _cachedHighlight == highlight && _cachedInventoryHighlight == inventoryHighlight && _cachedModel is not null)
        {
            _modelChanged = false;
            return _cachedModel;
        }

        var actorId = actionSession.ControlledActorId;
        var visual = grid.Cells.FirstOrDefault(cell => cell.EntityId == actorId)
            ?? new PlayCellVisual(
                1,
                1,
                tilesetProfile.Roles.DefaultBackdrop,
                Color.Gray,
                Color.Black,
                ResolveEntityGlyph(actorId),
                Color.Yellow,
                actorId);
        _cachedModel = EntityInspectionPanelModelFactory.FromEntity(
            session,
            grid,
            visual,
            actionSession.CurrentActionChoiceRequest,
            tilesetProfile,
            highlight,
            inventoryHighlight);
        _cachedModel = _cachedModel with
        {
            Actions = InspectionActionChoiceProjector.ProjectPlayerInventory(actionSession.CurrentActionChoiceRequest)
        };
        _selectedActionIndex = Math.Clamp(_selectedActionIndex, 0, Math.Max(0, _cachedModel.Actions.Count - 1));
        _cachedHighlight = highlight;
        _cachedInventoryHighlight = inventoryHighlight;
        _modelDirty = false;
        _modelChanged = true;
        return _cachedModel;
    }

    private void DrawOverlay(FrontendRect bounds, EntityInspectionPanelModel model)
    {
        var geometry = OverlayPanelGeometry.HalfTileOffset(bounds, displaySettings);
        var needsRedraw = _overlay is null
            || _overlay.Width != bounds.Width
            || _overlay.Height != bounds.Height
            || _drawnBounds != bounds
            || _modelChanged
            || _drawnFocused != IsFocused;

        if (_overlay is null || _overlay.Width != bounds.Width || _overlay.Height != bounds.Height)
        {
            ClearOverlay();
            _overlay = new EntityInspectionOverlayConsole(geometry, displaySettings, tilesetProfile);
            owner.Children.Add(_overlay);
            needsRedraw = true;
        }

        _overlay.MoveTo(geometry);
        if (!needsRedraw)
        {
            return;
        }

        _overlay.Draw(model, selectedActionIndex: _selectedActionIndex, actionMenuFocused: IsFocused);
        _drawnBounds = bounds;
        _drawnFocused = IsFocused;
        _modelChanged = false;
    }

    private void ClearOverlay()
    {
        if (_overlay is null)
        {
            return;
        }

        owner.Children.Remove(_overlay);
        _overlay = null;
        _drawnBounds = null;
        _drawnFocused = null;
    }

    private int ResolveEntityGlyph(EntityId entityId)
    {
        if (session.Registry.TryGetTemplateIdForEntity(session.World, entityId, out var templateId)
            && session.Registry.Presentations.TryGetValue(templateId, out var presentation)
            && tilesetProfile.PresentationMappings.GlyphsByPresentationId.TryGetValue(presentation.PresentationId.Value, out var mappedGlyph))
        {
            return mappedGlyph;
        }

        return session.World.Entities.TryGetValue(entityId, out var entity) && !string.IsNullOrWhiteSpace(entity.Name)
            ? entity.Name[0]
            : '?';
    }
}
