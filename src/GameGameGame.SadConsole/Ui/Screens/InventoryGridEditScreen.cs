using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Styling;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed class InventoryGridEditScreen
{
    private readonly FrontendEditorService? _service;
    private readonly Action<FrontendEditorSnapshot>? _snapshotMutated;
    private FrontendEditorEntityTemplateSummary _template;
    private readonly List<FrontendEditorEntityTemplateSummary> _entityTemplates;
    private GridCoord _cursor = new(0, 0);
    private string? _brushTemplateId;
    private string? _movingEntityId;
    private GridCoord? _movingFrom;
    private IUiComponent? _overlay;

    private InventoryGridEditScreen(
        FrontendEditorEntityTemplateSummary template,
        IReadOnlyList<FrontendEditorEntityTemplateSummary> entityTemplates,
        FrontendEditorService? service,
        Action<FrontendEditorSnapshot>? snapshotMutated)
    {
        _template = template;
        _entityTemplates = entityTemplates.ToList();
        _service = service;
        _snapshotMutated = snapshotMutated;
        _brushTemplateId = BrushOptions().FirstOrDefault()?.TemplateId;
    }

    public string Title => $"Edit Inventory Grid: {_template.Name}";
    public string Purpose => "Place, delete, move, copy, and inspect authored carried entities in this template inventory.";
    public GridCoord Cursor => _cursor;
    public string? BrushTemplateId => _brushTemplateId;
    public string? MovingEntityId => _movingEntityId;

    public static InventoryGridEditScreen FromSnapshot(
        FrontendEditorSnapshot snapshot,
        string templateId,
        FrontendEditorService? service = null,
        Action<FrontendEditorSnapshot>? snapshotMutated = null)
    {
        var template = snapshot.EntityTemplates.First(template => template.TemplateId == templateId);
        return new InventoryGridEditScreen(template, snapshot.EntityTemplates, service, snapshotMutated);
    }

    public IReadOnlyList<IUiComponent> Components() => [GridPanel(), BrushPanel(), InspectionPanel()];

    public IUiComponent? OverlayComponent() => _overlay;

    public string FooterText() => _overlay is not null
        ? "Brush picker: Up/Down chooses. Enter confirms. Esc cancels."
        : _movingEntityId is not null
            ? "Move mode: arrows move cursor. Enter/Space places carried entity. Esc cancels move."
            : "Inventory grid: arrows move cursor. Enter places brush. Delete removes. Space moves. C copies. Tab chooses brush. Esc backs out.";

    public InventoryGridEditResult Handle(UiComponentCommand command)
    {
        if (_overlay is ChoicePickerOverlayComponent picker)
        {
            var pickerResult = picker.Handle(command);
            if (pickerResult.Kind == FieldEditorOverlayResultKind.Confirmed && pickerResult.Value is { } choice)
            {
                _brushTemplateId = choice.Id;
                _overlay = null;
                return InventoryGridEditResult.Stay($"Selected inventory brush {choice.Label}.");
            }

            if (pickerResult.Kind == FieldEditorOverlayResultKind.Cancelled)
            {
                _overlay = null;
            }

            return InventoryGridEditResult.Stay(pickerResult.Message);
        }

        return command switch
        {
            UiComponentCommand.Up => MoveCursor(0, -1),
            UiComponentCommand.Down => MoveCursor(0, 1),
            UiComponentCommand.Left => MoveCursor(-1, 0),
            UiComponentCommand.Right => MoveCursor(1, 0),
            UiComponentCommand.Select => _movingEntityId is null ? PlaceBrushAtCursor() : PlaceMovingEntityAtCursor(),
            UiComponentCommand.Cancel => _movingEntityId is null
                ? InventoryGridEditResult.Return("Returned to Entity Template Edit.")
                : CancelMove(),
            _ => InventoryGridEditResult.Stay("Use inventory-grid controls.")
        };
    }

    public InventoryGridEditResult Handle(InventoryGridEditCommand command) => command switch
    {
        InventoryGridEditCommand.Delete => DeleteAtCursor(),
        InventoryGridEditCommand.Move => _movingEntityId is null ? BeginMoveAtCursor() : PlaceMovingEntityAtCursor(),
        InventoryGridEditCommand.Copy => CopyBrushFromCursor(),
        InventoryGridEditCommand.OpenBrushPicker => OpenBrushPicker(),
        _ => InventoryGridEditResult.Stay("Use inventory-grid controls.")
    };

    private InventoryGridEditResult MoveCursor(int dx, int dy)
    {
        if (_template.InventoryWidth <= 0 || _template.InventoryHeight <= 0)
        {
            _cursor = new GridCoord(0, 0);
            return InventoryGridEditResult.Stay("Template has no usable inventory grid.");
        }

        _cursor = new GridCoord(
            Math.Clamp(_cursor.X + dx, 0, _template.InventoryWidth - 1),
            Math.Clamp(_cursor.Y + dy, 0, _template.InventoryHeight - 1));
        return InventoryGridEditResult.Stay($"Inventory cursor: {_cursor.X},{_cursor.Y}.");
    }

    private InventoryGridEditResult PlaceBrushAtCursor()
    {
        if (_service is null) return InventoryGridEditResult.Stay("Inventory placement requires a service-backed editor screen.");
        if (string.IsNullOrWhiteSpace(_brushTemplateId)) return InventoryGridEditResult.Stay("Choose an inventory brush before placing.");

        var result = _service.OverwriteTemplateInInventory(_template.TemplateId, _brushTemplateId, _cursor);
        ReplaceAfterMutation(result.Snapshot);
        return InventoryGridEditResult.Stay(result.StatusMessage);
    }

    private InventoryGridEditResult DeleteAtCursor()
    {
        if (_service is null) return InventoryGridEditResult.Stay("Inventory deletion requires a service-backed editor screen.");
        if (CarriedAtCursor() is not { } carried) return InventoryGridEditResult.Stay($"No carried entity at {_cursor.X},{_cursor.Y} to delete.");

        var result = _service.RemoveCarriedEntity(_template.TemplateId, carried.EntityId);
        ReplaceAfterMutation(result.Snapshot);
        return InventoryGridEditResult.Stay(result.StatusMessage);
    }

    private InventoryGridEditResult BeginMoveAtCursor()
    {
        if (CarriedAtCursor() is not { } carried) return InventoryGridEditResult.Stay($"No carried entity at {_cursor.X},{_cursor.Y} to move.");

        _movingEntityId = carried.EntityId;
        _movingFrom = carried.Coord;
        return InventoryGridEditResult.Stay($"Move mode: picked up {carried.TemplateName ?? carried.TemplateId ?? carried.EntityId} from {_cursor.X},{_cursor.Y}.");
    }

    private InventoryGridEditResult PlaceMovingEntityAtCursor()
    {
        if (_service is null) return InventoryGridEditResult.Stay("Inventory move requires a service-backed editor screen.");
        if (_movingEntityId is null) return InventoryGridEditResult.Stay("No carried entity is being moved.");

        if (CarriedAtCursor() is { } occupant && occupant.EntityId != _movingEntityId)
        {
            var remove = _service.RemoveCarriedEntity(_template.TemplateId, occupant.EntityId);
            ReplaceAfterMutation(remove.Snapshot);
            if (!remove.IsSuccess) return InventoryGridEditResult.Stay(remove.StatusMessage);
        }

        var movingEntityId = _movingEntityId;
        var result = _service.MoveCarriedEntity(_template.TemplateId, movingEntityId, _cursor);
        ReplaceAfterMutation(result.Snapshot);
        if (result.IsSuccess)
        {
            _movingEntityId = null;
            _movingFrom = null;
        }

        return InventoryGridEditResult.Stay(result.StatusMessage);
    }

    private InventoryGridEditResult CancelMove()
    {
        var message = _movingFrom is { } from
            ? $"Cancelled move; carried entity remains at {from.X},{from.Y}."
            : "Cancelled move.";
        _movingEntityId = null;
        _movingFrom = null;
        return InventoryGridEditResult.Stay(message);
    }

    private InventoryGridEditResult CopyBrushFromCursor()
    {
        if (CarriedAtCursor() is not { } carried || string.IsNullOrWhiteSpace(carried.TemplateId))
        {
            return InventoryGridEditResult.Stay($"No carried template at {_cursor.X},{_cursor.Y} to copy into brush.");
        }

        _brushTemplateId = carried.TemplateId;
        return InventoryGridEditResult.Stay($"Copied brush from {_cursor.X},{_cursor.Y}: {carried.TemplateName ?? carried.TemplateId}.");
    }

    private InventoryGridEditResult OpenBrushPicker()
    {
        var options = BrushOptions().ToList();
        if (options.Count == 0) return InventoryGridEditResult.Stay("No other entity templates are available as inventory brushes.");

        var selectedIndex = Math.Max(0, options.FindIndex(option => option.TemplateId == _brushTemplateId));
        _overlay = new ChoicePickerOverlayComponent(
            "inventory-brush-picker",
            "Choose inventory brush",
            "brush",
            options.Select(option => new SelectableListItem(option.TemplateId, option.Name, option.TemplateId)),
            SadConsoleRect.FromSize(34, 8, 58, 14),
            selectedIndex);
        return InventoryGridEditResult.Stay("Opened inventory brush picker.");
    }

    private FrontendEditorCarriedEntitySummary? CarriedAtCursor() =>
        _template.CarriedEntities.FirstOrDefault(carried => carried.Coord == _cursor);

    private IEnumerable<FrontendEditorEntityTemplateSummary> BrushOptions() =>
        _entityTemplates.Where(template => template.TemplateId != _template.TemplateId);

    private void ReplaceAfterMutation(FrontendEditorSnapshot snapshot)
    {
        _template = snapshot.EntityTemplates.First(template => template.TemplateId == _template.TemplateId);
        _entityTemplates.Clear();
        _entityTemplates.AddRange(snapshot.EntityTemplates);
        _snapshotMutated?.Invoke(snapshot);
    }

    private IUiComponent GridPanel()
    {
        var rows = new List<string>
        {
            $"cursor: {_cursor.X},{_cursor.Y} | brush: {BrushLabel()} | mode: {(_movingEntityId is null ? "place" : $"move {_movingEntityId}")}"
        };

        if (_template.InventoryWidth <= 0 || _template.InventoryHeight <= 0)
        {
            rows.Add("No usable inventory grid. Increase width/height in 3.3 metadata first.");
        }

        return new InventoryGridComponent(
            "inventory-grid",
            "3.3.2 Inventory grid",
            new SadConsoleRect(1, 4, 82, 36),
            rows,
            UiComponentState.Focused,
            _template.InventoryWidth,
            _template.InventoryHeight,
            _cursor,
            _template.CarriedEntities.Select(item => new InventoryGridCell(item.Coord, item.Glyph ?? '?', item.Color)).ToList());
    }

    private PanelComponent BrushPanel()
    {
        var rows = new List<string>
        {
            $"current brush: {BrushLabel()}",
            "Tab opens brush picker.",
            "Enter silently overwrites occupied cells.",
            "Delete removes the carried entity under cursor.",
            "Space picks up/places a carried entity.",
            "C copies cursor entity template into brush.",
            "Current cell is inspected below."
        };

        if (_movingEntityId is not null)
        {
            rows.Add($"moving: {_movingEntityId} from {_movingFrom?.X},{_movingFrom?.Y}");
        }

        return new PanelComponent(
            "inventory-grid-help",
            "Inventory grid controls",
            new SadConsoleRect(85, 4, 32, 22),
            rows,
            UiComponentState.Selected);
    }

    private PanelComponent InspectionPanel()
    {
        var rows = CarriedAtCursor() is { } carried
            ? new List<string>
            {
                $"cell: {_cursor.X},{_cursor.Y}",
                $"entity: {carried.EntityId}",
                $"template: {carried.TemplateName ?? carried.TemplateId ?? "unbound"}",
                $"template id: {carried.TemplateId ?? "(none)"}",
                $"glyph/color: {carried.Glyph?.ToString() ?? "?"} / {carried.Color?.ToString() ?? "?"}"
            }
            :
            [
                $"cell: {_cursor.X},{_cursor.Y}",
                "empty cell"
            ];

        return new PanelComponent(
            "inventory-grid-inspection",
            "Current cell",
            new SadConsoleRect(85, 24, 32, 36),
            rows,
            UiComponentState.Selected);
    }

    private string BrushLabel()
    {
        if (string.IsNullOrWhiteSpace(_brushTemplateId)) return "(none)";
        var template = _entityTemplates.FirstOrDefault(template => template.TemplateId == _brushTemplateId);
        return template is null ? _brushTemplateId : $"{template.Name} ({template.TemplateId})";
    }
}

internal sealed class InventoryGridComponent : IUiComponent
{
    public InventoryGridComponent(
        string id,
        string title,
        SadConsoleRect bounds,
        IReadOnlyList<string> rows,
        UiComponentState state,
        int gridWidth,
        int gridHeight,
        GridCoord cursor,
        IReadOnlyList<InventoryGridCell> cells)
    {
        Id = id;
        Title = title;
        Bounds = bounds;
        BodyRows = rows;
        State = state;
        GridWidth = gridWidth;
        GridHeight = gridHeight;
        Cursor = cursor;
        Cells = cells;
    }

    public string Id { get; }
    public string Title { get; }
    public SadConsoleRect Bounds { get; }
    public UiComponentState State { get; }
    public IReadOnlyList<string> BodyRows { get; }
    public int GridWidth { get; }
    public int GridHeight { get; }
    public GridCoord Cursor { get; }
    public IReadOnlyList<InventoryGridCell> Cells { get; }

    public IReadOnlyList<string> RenderRows(SadConsoleTheme theme)
    {
        var rows = new List<string> { $"[{State.BorderColor(theme)}] {Title}" };
        rows.AddRange(BodyRows);
        rows.Add("grid cells are rendered by the SadConsole renderer");
        return rows;
    }
}

internal sealed record InventoryGridCell(GridCoord Coord, char Glyph, PresentationColor? Color);

internal enum InventoryGridEditCommand
{
    Delete,
    Move,
    Copy,
    OpenBrushPicker
}

internal sealed record InventoryGridEditResult(InventoryGridEditResultKind Kind, string Message)
{
    public static InventoryGridEditResult Stay(string message) => new(InventoryGridEditResultKind.Stay, message);
    public static InventoryGridEditResult Return(string message) => new(InventoryGridEditResultKind.ReturnToEntityTemplateEdit, message);
}

internal enum InventoryGridEditResultKind
{
    Stay,
    ReturnToEntityTemplateEdit
}
