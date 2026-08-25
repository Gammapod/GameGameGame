using GameGameGame.Content;
using GameGameGame.Core;
using Console = SadConsole.Console;

namespace GameGameGame.Frontend.SadConsole;

internal enum PlayFocusMode
{
    Grid,
    InspectionActions
}

internal sealed class PlayInspectionController(
    Console owner,
    PlayableScenarioSession session,
    PlayActionSessionController actionSession,
    SadConsoleDisplaySettings displaySettings,
    TilesetProfile tilesetProfile)
{
    private readonly PlayInspectionState _inspection = new();
    private EntityInspectionOverlayConsole? _overlay;
    private EntityId? _cachedEntityId;
    private EntityInspectionPanelModel? _cachedModel;
    private PlayHighlightState? _cachedHighlight;
    private PlayHighlightState? _cachedInventoryHighlight;
    private EntityId? _drawnEntityId;
    private FrontendRect? _drawnBounds;
    private bool _drawnEmpty;
    private bool _modelDirty = true;
    private bool _modelChanged = true;
    private int? _drawnActionIndex;
    private PlayFocusMode? _drawnFocusMode;

    public PlayFocusMode FocusMode { get; private set; } = PlayFocusMode.Grid;

    public int SelectedActionIndex { get; private set; }

    public PlayCellVisual? ResolveInspectedCell(PlayGridViewModel grid, GridCoord? previewCoord) =>
        _inspection.ResolveInspectedCell(grid, previewCoord);

    public void MarkWorldChanged() => _modelDirty = true;

    public void FocusActions()
    {
        FocusMode = PlayFocusMode.InspectionActions;
        SelectedActionIndex = FirstSelectableActionIndex();
        _drawnActionIndex = null;
    }

    public void ReturnToGrid()
    {
        FocusMode = PlayFocusMode.Grid;
        _drawnFocusMode = null;
    }

    public string MoveSelection(int delta)
    {
        var actions = _cachedModel?.Actions ?? [];
        if (actions.Count == 0)
        {
            return "No inspected actions.";
        }

        SelectedActionIndex = Math.Clamp(SelectedActionIndex + delta, 0, actions.Count - 1);
        _drawnActionIndex = null;
        return $"Action {SelectedActionIndex + 1}/{actions.Count}.";
    }

    public string ConfirmSelectedActionMessage()
    {
        var action = _cachedModel?.Actions.ElementAtOrDefault(SelectedActionIndex);
        var outcome = PlayActionCandidateResolver.ResolveSelection(action?.Candidate);
        return outcome.Kind switch
        {
            PlayActionCandidateOutcomeKind.NoSelection => "No inspected action selected.",
            PlayActionCandidateOutcomeKind.Explained => $"Action unavailable: {FrontendTextResolver.InspectionPrototype.Resolve(outcome.Message)}",
            PlayActionCandidateOutcomeKind.FollowUpNeeded => $"Selected action: {FrontendTextResolver.InspectionPrototype.Resolve(action!.Text)}. More input needed: {FrontendTextResolver.InspectionPrototype.Resolve(outcome.Message)}.",
            PlayActionCandidateOutcomeKind.ReadyToSubmit => $"Selected action: {FrontendTextResolver.InspectionPrototype.Resolve(outcome.Message)} (ready to submit).",
            _ => "No inspected action selected."
        };
    }

    public CellHighlightKind FocusedActionHighlightKind() =>
        PlayActionHighlightResolver.ForInspectionAction(_cachedModel?.Actions.ElementAtOrDefault(SelectedActionIndex));

    public EntityInspectionActionRow? SelectedActionRow => _cachedModel?.Actions.ElementAtOrDefault(SelectedActionIndex);

    public void Draw(FrontendRect? bounds, PlayGridViewModel grid, PlayCellVisual? inspectedCell, PlayHighlightState? highlight, PlayHighlightState? inventoryHighlight = null)
    {
        var model = ResolveModel(grid, inspectedCell, highlight, inventoryHighlight);
        SelectedActionIndex = ClampActionIndex(SelectedActionIndex);
        DrawOverlay(bounds, inspectedCell?.EntityId, model);
    }

    private EntityInspectionPanelModel? ResolveModel(PlayGridViewModel grid, PlayCellVisual? inspectedCell, PlayHighlightState? highlight, PlayHighlightState? inventoryHighlight)
    {
        var entityId = inspectedCell?.EntityId;
        if (entityId is null)
        {
            _modelChanged = _cachedModel is not null || _cachedEntityId is not null;
            _cachedEntityId = null;
            _cachedHighlight = null;
            _cachedInventoryHighlight = null;
            _cachedModel = null;
            _modelDirty = false;
            return null;
        }

        if (!_modelDirty && _cachedEntityId == entityId && _cachedHighlight == highlight && _cachedInventoryHighlight == inventoryHighlight && _cachedModel is not null)
        {
            _modelChanged = false;
            return _cachedModel;
        }

        _cachedEntityId = entityId;
        _cachedHighlight = highlight;
        _cachedInventoryHighlight = inventoryHighlight;
        _cachedModel = EntityInspectionPanelModelFactory.FromEntity(session, grid, inspectedCell!, actionSession.CurrentActionChoiceRequest, tilesetProfile, highlight, inventoryHighlight);
        _modelDirty = false;
        _modelChanged = true;
        return _cachedModel;
    }

    private void DrawOverlay(FrontendRect? bounds, EntityId? entityId, EntityInspectionPanelModel? model)
    {
        if (bounds is null)
        {
            ClearOverlay();
            return;
        }

        var geometry = OverlayPanelGeometry.HalfTileOffset(bounds, displaySettings);
        var needsRedraw = _overlay is null
            || _overlay.Width != bounds.Width
            || _overlay.Height != bounds.Height
            || _drawnBounds != bounds
            || _modelChanged
            || _drawnActionIndex != SelectedActionIndex
            || _drawnFocusMode != FocusMode
            || (model is null && !_drawnEmpty)
            || (model is not null && (_drawnEmpty || _drawnEntityId != entityId));

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

        _overlay.Draw(model, SelectedActionIndex, FocusMode == PlayFocusMode.InspectionActions, EntityInspectionPanelRenderOptions.OverflowAffordances);
        _drawnBounds = bounds;
        _drawnEmpty = model is null;
        _drawnEntityId = model is null ? null : entityId;
        _modelChanged = false;
        _drawnActionIndex = SelectedActionIndex;
        _drawnFocusMode = FocusMode;
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
        _drawnEmpty = false;
        _drawnEntityId = null;
        _drawnActionIndex = null;
        _drawnFocusMode = null;
    }

    private int FirstSelectableActionIndex()
    {
        var actions = _cachedModel?.Actions;
        if (actions is null || actions.Count == 0)
        {
            return 0;
        }

        var index = actions.ToList().FindIndex(action => action.Selectable);
        return index >= 0 ? index : 0;
    }

    private int ClampActionIndex(int index)
    {
        var count = _cachedModel?.Actions.Count ?? 0;
        return count == 0 ? 0 : Math.Clamp(index, 0, count - 1);
    }
}
