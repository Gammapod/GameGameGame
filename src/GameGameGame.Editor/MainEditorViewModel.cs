using GameGameGame.Content;
using GameGameGame.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GameGameGame.Editor;

public sealed class MainEditorViewModel : INotifyPropertyChanged
{
    private ContentEditorSession? _session;
    private EntityPresetListItem? _selectedPreset;
    private string _selectedName = string.Empty;
    private int _selectedInventoryWidth;
    private int _selectedInventoryHeight;
    private int _selectedWeight;
    private int _selectedCarryingCapacity;
    private string _selectedGlyph = string.Empty;
    private string _entityPresetNameInput = string.Empty;
    private PresentationColor _selectedColor = PresentationColor.Gray;
    private CarriedEntityListItem? _selectedCarriedEntity;
    private EntityPresetListItem? _selectedTemplateToPlace;
    private EntityPresetListItem? _selectedReplacementTemplate;
    private string? _statusMessage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<EntityPresetListItem> EntityPresets { get; } = [];

    public ObservableCollection<CarriedEntityListItem> CarriedEntities { get; } = [];

    public ObservableCollection<InventoryGridCell> InventoryGridCells { get; } = [];

    public ObservableCollection<string> ValidationMessages { get; } = [];

    public ObservableCollection<string> YamlDiffLines { get; } = [];

    public string? FilePath => _session?.FilePath;

    public bool IsDirty => _session?.IsDirty ?? false;

    public string YamlPreview => _session?.GetYamlPreview() ?? string.Empty;

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public EntityPresetListItem? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (SetField(ref _selectedPreset, value) && value is not null)
            {
                SelectEntityPreset(value.Id);
            }
        }
    }

    public string SelectedName
    {
        get => _selectedName;
        set => SetField(ref _selectedName, value);
    }

    public int SelectedInventoryWidth
    {
        get => _selectedInventoryWidth;
        set => SetField(ref _selectedInventoryWidth, value);
    }

    public int SelectedInventoryHeight
    {
        get => _selectedInventoryHeight;
        set => SetField(ref _selectedInventoryHeight, value);
    }

    public int SelectedWeight
    {
        get => _selectedWeight;
        set => SetField(ref _selectedWeight, value);
    }

    public int SelectedCarryingCapacity
    {
        get => _selectedCarryingCapacity;
        set => SetField(ref _selectedCarryingCapacity, value);
    }

    public string SelectedGlyph
    {
        get => _selectedGlyph;
        set => SetField(ref _selectedGlyph, value);
    }

    public PresentationColor SelectedColor
    {
        get => _selectedColor;
        set => SetField(ref _selectedColor, value);
    }

    public string EntityPresetNameInput
    {
        get => _entityPresetNameInput;
        set => SetField(ref _entityPresetNameInput, value);
    }

    public CarriedEntityListItem? SelectedCarriedEntity
    {
        get => _selectedCarriedEntity;
        set => SetField(ref _selectedCarriedEntity, value);
    }

    public EntityPresetListItem? SelectedTemplateToPlace
    {
        get => _selectedTemplateToPlace;
        set => SetField(ref _selectedTemplateToPlace, value);
    }

    public EntityPresetListItem? SelectedReplacementTemplate
    {
        get => _selectedReplacementTemplate;
        set => SetField(ref _selectedReplacementTemplate, value);
    }

    public ContentEditorSessionOpenResult OpenFile(string path)
    {
        var result = ContentEditorSession.OpenFile(path);

        if (!result.IsSuccess)
        {
            StatusMessage = result.ErrorMessage;
            return result;
        }

        _session = result.Session;
        RefreshFromSession();
        StatusMessage = $"Opened {path}";

        return result;
    }

    public ContentEditorFileOperationResult Save()
    {
        if (_session is null)
        {
            return ContentEditorFileOperationResult.Failure("No content file is open.");
        }

        var result = _session.Save();
        RefreshFromSession();
        StatusMessage = result.IsSuccess ? "Saved." : result.ErrorMessage;

        return result;
    }

    public ContentEditorFileOperationResult SaveAs(string path)
    {
        if (_session is null)
        {
            return ContentEditorFileOperationResult.Failure("No content file is open.");
        }

        var result = _session.SaveAs(path);
        RefreshFromSession();
        StatusMessage = result.IsSuccess ? "Saved as." : result.ErrorMessage;

        return result;
    }

    public ContentEditorFileOperationResult Reload()
    {
        if (_session is null)
        {
            return ContentEditorFileOperationResult.Failure("No content file is open.");
        }

        var selectedPresetId = SelectedPreset?.Id;
        var result = _session.Reload();
        RefreshFromSession();
        if (result.IsSuccess && selectedPresetId is not null && EntityPresets.Any(item => item.Id == selectedPresetId.Value))
        {
            SelectEntityPreset(selectedPresetId.Value);
        }
        else if (result.IsSuccess)
        {
            ClearSelectedEntityPreset();
        }

        StatusMessage = result.IsSuccess ? "Reloaded." : result.ErrorMessage;

        return result;
    }

    public void CreateNewDocument()
    {
        _session = ContentEditorSession.CreateNew();
        RefreshFromSession();
        ClearSelectedEntityPreset();
        StatusMessage = "Created new content document.";
    }

    public void SelectEntityPreset(EntityTemplateId id)
    {
        if (_session is null)
        {
            return;
        }

        var preset = _session.Editor.GetEntityPreset(id);
        _selectedPreset = EntityPresets.SingleOrDefault(item => item.Id == id);
        OnPropertyChanged(nameof(SelectedPreset));
        SelectedName = preset.Template.Name;
        SelectedInventoryWidth = preset.Template.InventoryWidth;
        SelectedInventoryHeight = preset.Template.InventoryHeight;
        SelectedWeight = preset.Template.Weight;
        SelectedCarryingCapacity = preset.Template.CarryingCapacity;
        SelectedGlyph = preset.Presentation.Glyph.ToString();
        SelectedColor = preset.Presentation.Color;
        RefreshCarriedEntities(id);
    }

    public void ApplySelectedEntityPresetEdits()
    {
        if (_session is null || SelectedPreset is null)
        {
            return;
        }

        var selectedPresetId = SelectedPreset.Id;
        var current = _session.Editor.GetEntityPreset(selectedPresetId);
        var glyph = string.IsNullOrEmpty(SelectedGlyph) ? '?' : SelectedGlyph[0];
        _session.Editor.UpdateEntityPreset(
            selectedPresetId,
            current.Template with
            {
                Name = SelectedName,
                InventoryWidth = SelectedInventoryWidth,
                InventoryHeight = SelectedInventoryHeight,
                Weight = SelectedWeight,
                CarryingCapacity = SelectedCarryingCapacity
            },
            new EntityPresentation(glyph, SelectedColor));

        RefreshFromSession();
        SelectEntityPreset(selectedPresetId);
        StatusMessage = $"Applied edits to {SelectedName}.";
    }

    public void CreateEntityPreset()
    {
        if (_session is null)
        {
            return;
        }

        var name = EntityPresetNameInput.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "Enter a name before creating an entity preset.";
            return;
        }

        var id = _session.Editor.CreateEntityPreset(name);
        RefreshFromSession();
        SelectEntityPreset(id);
        StatusMessage = $"Created {name}.";
    }

    public void DuplicateSelectedEntityPreset()
    {
        if (_session is null || SelectedPreset is null)
        {
            return;
        }

        var sourceName = SelectedPreset.Name;
        var sourceId = SelectedPreset.Id;
        var name = EntityPresetNameInput.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "Enter a name before duplicating an entity preset.";
            return;
        }

        var duplicateId = _session.Editor.DuplicateEntityPreset(sourceId, name);
        RefreshFromSession();
        SelectEntityPreset(duplicateId);
        StatusMessage = $"Duplicated {sourceName} as {name}.";
    }

    public void DeleteSelectedEntityPreset()
    {
        if (_session is null || SelectedPreset is null)
        {
            return;
        }

        var selectedId = SelectedPreset.Id;
        var selectedName = SelectedPreset.Name;
        var result = _session.Editor.DeleteEntityPreset(selectedId);
        if (!result.IsSuccess)
        {
            StatusMessage = result.ErrorMessage;
            SelectEntityPreset(selectedId);
            return;
        }

        RefreshFromSession();
        ClearSelectedEntityPreset();
        StatusMessage = $"Deleted {selectedName}.";
    }

    public void PlaceSelectedTemplateInInventory()
    {
        if (_session is null || SelectedPreset is null || SelectedTemplateToPlace is null)
        {
            return;
        }

        var parentId = SelectedPreset.Id;
        var templateToPlace = SelectedTemplateToPlace;

        try
        {
            var placedEntityId = _session.Editor.PlaceCarriedEntity(parentId, templateToPlace.Id);
            RefreshFromSession();
            SelectEntityPreset(parentId);
            SelectedCarriedEntity = CarriedEntities.SingleOrDefault(item => item.EntityId == placedEntityId);
            StatusMessage = $"Placed {templateToPlace.Name}.";
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    public void ClickInventoryGridCell(InventoryGridCell cell)
    {
        if (_session is null || SelectedPreset is null)
        {
            return;
        }

        if (cell.CarriedEntityId is { } carriedEntityId)
        {
            SelectedCarriedEntity = CarriedEntities.SingleOrDefault(item => item.EntityId == carriedEntityId);
            if (SelectedCarriedEntity is not null)
            {
                StatusMessage = $"Selected {SelectedCarriedEntity.TemplateName} at {cell.Coord.X},{cell.Coord.Y}.";
            }

            return;
        }

        var parentId = SelectedPreset.Id;

        try
        {
            if (SelectedCarriedEntity is not null)
            {
                var movedEntityId = SelectedCarriedEntity.EntityId;
                var movedName = SelectedCarriedEntity.TemplateName;
                _session.Editor.MoveCarriedEntity(parentId, movedEntityId, cell.Coord);
                RefreshFromSession();
                SelectEntityPreset(parentId);
                SelectedCarriedEntity = CarriedEntities.SingleOrDefault(item => item.EntityId == movedEntityId);
                StatusMessage = $"Moved {movedName} to {cell.Coord.X},{cell.Coord.Y}.";
                return;
            }

            if (SelectedTemplateToPlace is not null)
            {
                var templateToPlace = SelectedTemplateToPlace;
                var placedEntityId = _session.Editor.PlaceCarriedEntity(parentId, templateToPlace.Id, cell.Coord);
                RefreshFromSession();
                SelectEntityPreset(parentId);
                SelectedCarriedEntity = CarriedEntities.SingleOrDefault(item => item.EntityId == placedEntityId);
                StatusMessage = $"Placed {templateToPlace.Name} at {cell.Coord.X},{cell.Coord.Y}.";
                return;
            }

            StatusMessage = "Select a template to place, or select a carried entity to move.";
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    public void RemoveSelectedCarriedEntity()
    {
        if (_session is null || SelectedPreset is null || SelectedCarriedEntity is null)
        {
            return;
        }

        var parentId = SelectedPreset.Id;
        var removed = SelectedCarriedEntity.TemplateName;

        try
        {
            _session.Editor.RemoveCarriedEntity(parentId, SelectedCarriedEntity.EntityId);
            RefreshFromSession();
            SelectEntityPreset(parentId);
            StatusMessage = $"Removed {removed}.";
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    public void ReplaceSelectedCarriedEntityTemplate()
    {
        if (_session is null || SelectedPreset is null || SelectedCarriedEntity is null || SelectedReplacementTemplate is null)
        {
            return;
        }

        var parentId = SelectedPreset.Id;
        var carriedEntityId = SelectedCarriedEntity.EntityId;
        var replacementTemplate = SelectedReplacementTemplate;

        try
        {
            _session.Editor.ReplaceCarriedEntityTemplate(parentId, carriedEntityId, replacementTemplate.Id);
            RefreshFromSession();
            SelectEntityPreset(parentId);
            SelectedCarriedEntity = CarriedEntities.SingleOrDefault(item => item.EntityId == carriedEntityId);
            StatusMessage = $"Replaced carried entity with {replacementTemplate.Name}.";
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private void RefreshFromSession()
    {
        EntityPresets.Clear();
        CarriedEntities.Clear();
        InventoryGridCells.Clear();
        ValidationMessages.Clear();
        YamlDiffLines.Clear();
        SelectedCarriedEntity = null;

        if (_session is null)
        {
            return;
        }

        foreach (var preset in _session.Editor.ListEntityPresets())
        {
            EntityPresets.Add(new EntityPresetListItem(
                preset.Id,
                preset.Template.Name,
                preset.Presentation.Glyph,
                preset.Presentation.Color));
        }

        foreach (var message in _session.Editor.Validate().Errors)
        {
            ValidationMessages.Add(message);
        }

        foreach (var line in _session.GetYamlDiff().Lines)
        {
            YamlDiffLines.Add(line);
        }

        OnPropertyChanged(nameof(FilePath));
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(YamlPreview));
    }

    private void RefreshCarriedEntities(EntityTemplateId parentTemplateId)
    {
        CarriedEntities.Clear();
        InventoryGridCells.Clear();
        SelectedCarriedEntity = null;

        if (_session is null)
        {
            return;
        }

        foreach (var carried in _session.Editor.ListCarriedEntities(parentTemplateId))
        {
            CarriedEntities.Add(new CarriedEntityListItem(
                carried.EntityId,
                carried.TemplateId,
                carried.Coord,
                carried.Template.Name,
                carried.Presentation.Glyph,
                carried.Presentation.Color));
        }

        var occupiedCells = CarriedEntities.ToDictionary(carried => carried.Coord);
        for (var y = 0; y < SelectedInventoryHeight; y++)
        {
            for (var x = 0; x < SelectedInventoryWidth; x++)
            {
                var coord = new GridCoord(x, y);
                if (occupiedCells.TryGetValue(coord, out var carried))
                {
                    InventoryGridCells.Add(new InventoryGridCell(
                        coord,
                        carried.EntityId,
                        carried.TemplateId,
                        carried.TemplateName,
                        carried.Glyph,
                        $"{carried.Glyph} {carried.TemplateName}"));
                }
                else
                {
                    InventoryGridCells.Add(new InventoryGridCell(
                        coord,
                        CarriedEntityId: null,
                        TemplateId: null,
                        TemplateName: null,
                        Glyph: null,
                        DisplayText: "."));
                }
            }
        }
    }

    private void ClearSelectedEntityPreset()
    {
        _selectedPreset = null;
        OnPropertyChanged(nameof(SelectedPreset));
        SelectedName = string.Empty;
        SelectedInventoryWidth = 0;
        SelectedInventoryHeight = 0;
        SelectedWeight = 0;
        SelectedCarryingCapacity = 0;
        SelectedGlyph = string.Empty;
        SelectedColor = PresentationColor.Gray;
        CarriedEntities.Clear();
        InventoryGridCells.Clear();
        SelectedCarriedEntity = null;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record EntityPresetListItem(
    EntityTemplateId Id,
    string Name,
    char Glyph,
    PresentationColor Color)
{
    public override string ToString() => $"{Name} ({Id})";
}

public sealed record CarriedEntityListItem(
    EntityId EntityId,
    EntityTemplateId TemplateId,
    GridCoord Coord,
    string TemplateName,
    char Glyph,
    PresentationColor Color)
{
    public override string ToString() => $"{EntityId}: {TemplateName} at {Coord}";
}

public sealed record InventoryGridCell(
    GridCoord Coord,
    EntityId? CarriedEntityId,
    EntityTemplateId? TemplateId,
    string? TemplateName,
    char? Glyph,
    string DisplayText)
{
    public bool IsOccupied => CarriedEntityId is not null;

    public override string ToString() => $"{Coord.X},{Coord.Y}: {DisplayText}";
}
