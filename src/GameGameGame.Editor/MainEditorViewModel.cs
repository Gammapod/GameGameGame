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
    private int _selectedBulk;
    private int _selectedAperture;
    private string _selectedGlyph = string.Empty;
    private string _entityPresetNameInput = string.Empty;
    private string _actionPlanNameInput = string.Empty;
    private PresentationColor _selectedColor = PresentationColor.Gray;
    private CarriedEntityListItem? _selectedCarriedEntity;
    private EntityPresetListItem? _selectedTemplateToPlace;
    private EntityPresetListItem? _selectedReplacementTemplate;
    private ActionPlanListItem? _selectedDefaultActionPlan;
    private ActionPlanListItem? _selectedActionPlan;
    private ActionStepCatalogListItem? _selectedActionStepToAdd;
    private ActionPlanBehaviorStepListItem? _selectedBehaviorStep;
    private ActionPlanStepListItem? _selectedActionPlanStep;
    private ActionPlanStepCheckListItem? _selectedActionPlanStepCheck;
    private string _actionPlanStepLabelInput = string.Empty;
    private PlanCheckKind _selectedCheckKind = PlanCheckKind.CanMove;
    private int _checkInventoryCoordX;
    private int _checkInventoryCoordY;
    private PlanEffectKind _selectedSuccessEffectKind = PlanEffectKind.Wait;
    private PlanEffectKind _selectedFailureEffectKind = PlanEffectKind.Wait;
    private int _successInventoryCoordX;
    private int _successInventoryCoordY;
    private int _failureInventoryCoordX;
    private int _failureInventoryCoordY;
    private ActionPlanListItem? _selectedSuccessCallPlan;
    private ActionPlanListItem? _selectedFailureCallPlan;
    private MovementTargetKind _selectedSuccessMovementTargetKind = MovementTargetKind.Self;
    private MovementTargetKind _selectedFailureMovementTargetKind = MovementTargetKind.CarriedInventoryCoord;
    private string _successMovementTargetEntityIdInput = string.Empty;
    private string _failureMovementTargetEntityIdInput = string.Empty;
    private int _successMovementTargetCoordX;
    private int _successMovementTargetCoordY;
    private int _failureMovementTargetCoordX;
    private int _failureMovementTargetCoordY;
    private MovementDestinationKind _selectedSuccessMovementDestinationKind = MovementDestinationKind.AdjacentToSelf;
    private MovementDestinationKind _selectedFailureMovementDestinationKind = MovementDestinationKind.AdjacentToSelf;
    private string _successMovementDestinationPlaneIdInput = "world";
    private string _failureMovementDestinationPlaneIdInput = "world";
    private int _successMovementDestinationCoordX;
    private int _successMovementDestinationCoordY;
    private int _failureMovementDestinationCoordX;
    private int _failureMovementDestinationCoordY;
    private string _successMovementDestinationOwnerIdInput = string.Empty;
    private string _failureMovementDestinationOwnerIdInput = string.Empty;
    private string _successMovementDestinationAnchorEntityIdInput = string.Empty;
    private string _failureMovementDestinationAnchorEntityIdInput = string.Empty;
    private Direction _successMovementDestinationDirection = Direction.South;
    private Direction _failureMovementDestinationDirection = Direction.South;
    private bool _hasInitialFacing;
    private Direction _selectedInitialFacing = Direction.West;
    private string? _statusMessage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<EntityPresetListItem> EntityPresets { get; } = [];

    public ObservableCollection<ActionPlanListItem> ActionPlans { get; } = [];

    public ObservableCollection<ActionPlanStepListItem> ActionPlanSteps { get; } = [];

    public ObservableCollection<ActionStepCatalogListItem> AvailableActionSteps { get; } = [];

    public ObservableCollection<ActionPlanBehaviorStepListItem> BehaviorSteps { get; } = [];

    public ObservableCollection<ActionPlanStepCheckListItem> ActionPlanStepChecks { get; } = [];

    public ObservableCollection<CarriedEntityListItem> CarriedEntities { get; } = [];

    public ObservableCollection<InventoryGridCell> InventoryGridCells { get; } = [];

    public ObservableCollection<string> ValidationMessages { get; } = [];

    public ObservableCollection<string> SelectedPresetDiagnostics { get; } = [];

    public ObservableCollection<string> SelectedActionPlanDiagnostics { get; } = [];

    public ObservableCollection<string> SelectedActionPlanStepDiagnostics { get; } = [];

    public ObservableCollection<string> YamlDiffLines { get; } = [];

    public IReadOnlyList<Direction> Directions { get; } = Enum.GetValues<Direction>();

    public IReadOnlyList<MovementTargetKind> MovementTargetKinds { get; } = Enum.GetValues<MovementTargetKind>();

    public IReadOnlyList<MovementDestinationKind> MovementDestinationKinds { get; } = Enum.GetValues<MovementDestinationKind>();

    public IReadOnlyList<PlanCheckKind> CheckKinds { get; } =
    [
        PlanCheckKind.CanMove,
        PlanCheckKind.BlockingEntity,
        PlanCheckKind.CanPickup
    ];

    public IReadOnlyList<PlanEffectKind> EffectKinds { get; } =
    [
        PlanEffectKind.Wait,
        PlanEffectKind.Teleport,
        PlanEffectKind.Move,
        PlanEffectKind.Pickup,
        PlanEffectKind.Drop,
        PlanEffectKind.ReverseDirection,
        PlanEffectKind.CallPlan
    ];

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

    public int SelectedBulk
    {
        get => _selectedBulk;
        set => SetField(ref _selectedBulk, value);
    }

    public int SelectedAperture
    {
        get => _selectedAperture;
        set => SetField(ref _selectedAperture, value);
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

    public string ActionPlanNameInput
    {
        get => _actionPlanNameInput;
        set => SetField(ref _actionPlanNameInput, value);
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

    public ActionPlanListItem? SelectedDefaultActionPlan
    {
        get => _selectedDefaultActionPlan;
        set => SetField(ref _selectedDefaultActionPlan, value);
    }

    public ActionPlanListItem? SelectedActionPlan
    {
        get => _selectedActionPlan;
        set
        {
            if (SetField(ref _selectedActionPlan, value))
            {
                RefreshActionPlanSteps();
                RefreshBehaviorSteps();
                OnPropertyChanged(nameof(SelectedActionPlanShape));
                OnPropertyChanged(nameof(SelectedActionPlanShapeDetail));
                OnPropertyChanged(nameof(IsLegacyActionPlanCompatibilityVisible));
                OnPropertyChanged(nameof(SelectedBehaviorChainSummary));
                OnPropertyChanged(nameof(SelectedBehaviorChainDefaultStateHints));
                RefreshSelectedDiagnostics();
            }
        }
    }

    public string SelectedActionPlanShape => SelectedActionPlan is null
        ? "No action plan selected"
        : ContentEditorService.FormatActionPlanShape(SelectedActionPlan.Shape);

    public bool IsLegacyActionPlanCompatibilityVisible =>
        SelectedActionPlan?.Shape == ActionPlanShape.LegacyLowLevelSteps;

    public string SelectedActionPlanShapeDetail => SelectedActionPlan?.Shape switch
    {
        ActionPlanShape.CanonicalBehaviorChain => "Recommended: this plan uses ordered engine-defined Action Steps. Keep authoring here for normal behavior work.",
        ActionPlanShape.TransitionalPrimitivePlan => "Compatibility: this plan uses primitive-backed fallback links. It still loads and runs, but canonical behavior chains are preferred for new authoring.",
        ActionPlanShape.LegacyLowLevelSteps => "Advanced compatibility: this plan uses low-level steps/checks/effects. Keep only when canonical Action Steps cannot express the behavior yet.",
        ActionPlanShape.EmptyPassive => "Passive: this plan has no behavior. Add canonical Action Steps when the entity should act on its turn.",
        ActionPlanShape.InvalidMixedShape => "Invalid: this plan mixes behavior shapes. Use only one of behavior, primitive, or low-level steps.",
        ActionPlanShape.InvalidEmptyBehaviorChain => "Invalid: this plan declares an empty behavior chain. Omit behavior or add at least one Action Step.",
        null => "Select an action plan to inspect its authoring shape.",
        _ => "Unknown plan shape. Validate the document before saving."
    };

    public string SelectedBehaviorChainSummary
    {
        get
        {
            if (SelectedActionPlan is null)
            {
                return "Select an action plan to inspect its canonical behavior chain.";
            }

            if (BehaviorSteps.Count == 0)
            {
                return "No canonical Action Steps. Use this section to author new behavior chains; legacy/advanced steps are below.";
            }

            return $"Canonical order: {string.Join(" -> ", BehaviorSteps.Select(step => step.DisplayName))}";
        }
    }

    public string SelectedBehaviorChainDefaultStateHints
    {
        get
        {
            if (BehaviorSteps.Count == 0)
            {
                return "Default-state hints appear here when the selected plan has canonical Action Steps.";
            }

            var hints = BehaviorSteps
                .SelectMany(step => ActionStepCatalog.Get(step.Kind).DefaultableState)
                .Select(FormatDefaultStateHint)
                .Distinct()
                .ToList();

            return hints.Count == 0
                ? "No defaultable state is required by the current canonical Action Steps."
                : $"Default-state hints: {string.Join("; ", hints)}";
        }
    }

    public ActionStepCatalogListItem? SelectedActionStepToAdd
    {
        get => _selectedActionStepToAdd;
        set => SetField(ref _selectedActionStepToAdd, value);
    }

    public ActionPlanBehaviorStepListItem? SelectedBehaviorStep
    {
        get => _selectedBehaviorStep;
        set
        {
            if (SetField(ref _selectedBehaviorStep, value))
            {
                OnPropertyChanged(nameof(SelectedBehaviorStepHint));
                RefreshSelectedDiagnostics();
            }
        }
    }

    public string SelectedBehaviorStepHint => SelectedBehaviorStep?.Description ?? string.Empty;

    public ActionPlanStepListItem? SelectedActionPlanStep
    {
        get => _selectedActionPlanStep;
        set
        {
            if (SetField(ref _selectedActionPlanStep, value) && value is not null)
            {
                ActionPlanStepLabelInput = value.Label;
                RefreshSelectedStepChecks(value.Index);
                PopulateEffectInputs(value.Index);
                RefreshSelectedDiagnostics();
            }
        }
    }

    public ActionPlanStepCheckListItem? SelectedActionPlanStepCheck
    {
        get => _selectedActionPlanStepCheck;
        set
        {
            if (!SetField(ref _selectedActionPlanStepCheck, value) || value is null)
            {
                return;
            }

            SelectedCheckKind = value.Kind;
            CheckInventoryCoordX = value.InventoryCoord?.X ?? 0;
            CheckInventoryCoordY = value.InventoryCoord?.Y ?? 0;
        }
    }

    public string ActionPlanStepLabelInput
    {
        get => _actionPlanStepLabelInput;
        set => SetField(ref _actionPlanStepLabelInput, value);
    }

    public PlanCheckKind SelectedCheckKind
    {
        get => _selectedCheckKind;
        set
        {
            if (!SetField(ref _selectedCheckKind, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsCheckInventoryCoordVisible));
        }
    }

    public bool IsCheckInventoryCoordVisible => SelectedCheckKind == PlanCheckKind.CanPickup;

    public int CheckInventoryCoordX
    {
        get => _checkInventoryCoordX;
        set => SetField(ref _checkInventoryCoordX, value);
    }

    public int CheckInventoryCoordY
    {
        get => _checkInventoryCoordY;
        set => SetField(ref _checkInventoryCoordY, value);
    }

    public PlanEffectKind SelectedSuccessEffectKind
    {
        get => _selectedSuccessEffectKind;
        set
        {
            if (!SetField(ref _selectedSuccessEffectKind, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsSuccessInventoryCoordVisible));
            OnPropertyChanged(nameof(IsSuccessMovementVisible));
            OnPropertyChanged(nameof(IsSuccessCallPlanVisible));
        }
    }

    public PlanEffectKind SelectedFailureEffectKind
    {
        get => _selectedFailureEffectKind;
        set
        {
            if (!SetField(ref _selectedFailureEffectKind, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsFailureInventoryCoordVisible));
            OnPropertyChanged(nameof(IsFailureMovementVisible));
            OnPropertyChanged(nameof(IsFailureCallPlanVisible));
        }
    }

    public bool IsSuccessInventoryCoordVisible => SelectedSuccessEffectKind == PlanEffectKind.Pickup;

    public bool IsSuccessMovementVisible => SelectedSuccessEffectKind is PlanEffectKind.Teleport or PlanEffectKind.Drop;

    public bool IsSuccessCallPlanVisible => SelectedSuccessEffectKind == PlanEffectKind.CallPlan;

    public bool IsFailureInventoryCoordVisible => SelectedFailureEffectKind == PlanEffectKind.Pickup;

    public bool IsFailureMovementVisible => SelectedFailureEffectKind is PlanEffectKind.Teleport or PlanEffectKind.Drop;

    public bool IsFailureCallPlanVisible => SelectedFailureEffectKind == PlanEffectKind.CallPlan;

    public int SuccessInventoryCoordX
    {
        get => _successInventoryCoordX;
        set => SetField(ref _successInventoryCoordX, value);
    }

    public int SuccessInventoryCoordY
    {
        get => _successInventoryCoordY;
        set => SetField(ref _successInventoryCoordY, value);
    }

    public int FailureInventoryCoordX
    {
        get => _failureInventoryCoordX;
        set => SetField(ref _failureInventoryCoordX, value);
    }

    public int FailureInventoryCoordY
    {
        get => _failureInventoryCoordY;
        set => SetField(ref _failureInventoryCoordY, value);
    }

    public ActionPlanListItem? SelectedSuccessCallPlan
    {
        get => _selectedSuccessCallPlan;
        set => SetField(ref _selectedSuccessCallPlan, value);
    }

    public ActionPlanListItem? SelectedFailureCallPlan
    {
        get => _selectedFailureCallPlan;
        set => SetField(ref _selectedFailureCallPlan, value);
    }

    public MovementTargetKind SelectedSuccessMovementTargetKind { get => _selectedSuccessMovementTargetKind; set => SetField(ref _selectedSuccessMovementTargetKind, value); }

    public MovementTargetKind SelectedFailureMovementTargetKind { get => _selectedFailureMovementTargetKind; set => SetField(ref _selectedFailureMovementTargetKind, value); }

    public string SuccessMovementTargetEntityIdInput { get => _successMovementTargetEntityIdInput; set => SetField(ref _successMovementTargetEntityIdInput, value); }

    public string FailureMovementTargetEntityIdInput { get => _failureMovementTargetEntityIdInput; set => SetField(ref _failureMovementTargetEntityIdInput, value); }

    public int SuccessMovementTargetCoordX { get => _successMovementTargetCoordX; set => SetField(ref _successMovementTargetCoordX, value); }

    public int SuccessMovementTargetCoordY { get => _successMovementTargetCoordY; set => SetField(ref _successMovementTargetCoordY, value); }

    public int FailureMovementTargetCoordX { get => _failureMovementTargetCoordX; set => SetField(ref _failureMovementTargetCoordX, value); }

    public int FailureMovementTargetCoordY { get => _failureMovementTargetCoordY; set => SetField(ref _failureMovementTargetCoordY, value); }

    public MovementDestinationKind SelectedSuccessMovementDestinationKind { get => _selectedSuccessMovementDestinationKind; set => SetField(ref _selectedSuccessMovementDestinationKind, value); }

    public MovementDestinationKind SelectedFailureMovementDestinationKind { get => _selectedFailureMovementDestinationKind; set => SetField(ref _selectedFailureMovementDestinationKind, value); }

    public string SuccessMovementDestinationPlaneIdInput { get => _successMovementDestinationPlaneIdInput; set => SetField(ref _successMovementDestinationPlaneIdInput, value); }

    public string FailureMovementDestinationPlaneIdInput { get => _failureMovementDestinationPlaneIdInput; set => SetField(ref _failureMovementDestinationPlaneIdInput, value); }

    public int SuccessMovementDestinationCoordX { get => _successMovementDestinationCoordX; set => SetField(ref _successMovementDestinationCoordX, value); }

    public int SuccessMovementDestinationCoordY { get => _successMovementDestinationCoordY; set => SetField(ref _successMovementDestinationCoordY, value); }

    public int FailureMovementDestinationCoordX { get => _failureMovementDestinationCoordX; set => SetField(ref _failureMovementDestinationCoordX, value); }

    public int FailureMovementDestinationCoordY { get => _failureMovementDestinationCoordY; set => SetField(ref _failureMovementDestinationCoordY, value); }

    public string SuccessMovementDestinationOwnerIdInput { get => _successMovementDestinationOwnerIdInput; set => SetField(ref _successMovementDestinationOwnerIdInput, value); }

    public string FailureMovementDestinationOwnerIdInput { get => _failureMovementDestinationOwnerIdInput; set => SetField(ref _failureMovementDestinationOwnerIdInput, value); }

    public string SuccessMovementDestinationAnchorEntityIdInput { get => _successMovementDestinationAnchorEntityIdInput; set => SetField(ref _successMovementDestinationAnchorEntityIdInput, value); }

    public string FailureMovementDestinationAnchorEntityIdInput { get => _failureMovementDestinationAnchorEntityIdInput; set => SetField(ref _failureMovementDestinationAnchorEntityIdInput, value); }

    public Direction SuccessMovementDestinationDirection { get => _successMovementDestinationDirection; set => SetField(ref _successMovementDestinationDirection, value); }

    public Direction FailureMovementDestinationDirection { get => _failureMovementDestinationDirection; set => SetField(ref _failureMovementDestinationDirection, value); }

    public bool HasInitialFacing
    {
        get => _hasInitialFacing;
        private set => SetField(ref _hasInitialFacing, value);
    }

    public Direction SelectedInitialFacing
    {
        get => _selectedInitialFacing;
        set => SetField(ref _selectedInitialFacing, value);
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
        SelectedBulk = preset.Template.Bulk;
        SelectedAperture = preset.Template.Aperture;
        SelectedGlyph = preset.Presentation.Glyph.ToString();
        SelectedColor = preset.Presentation.Color;
        SelectedDefaultActionPlan = preset.Template.DefaultActionPlanId is { } planId
            ? ActionPlans.SingleOrDefault(item => item.Id == planId)
            : null;
        RefreshActionStateDefaults(id);
        RefreshCarriedEntities(id);
        RefreshSelectedDiagnostics();
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
                Bulk = SelectedBulk,
                Aperture = SelectedAperture
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

    public void CreateActionPlan()
    {
        if (_session is null)
        {
            return;
        }

        var name = ActionPlanNameInput.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "Enter a name before creating an action plan.";
            return;
        }

        var id = _session.Editor.CreatePassiveActionPlan(name);
        RefreshFromSession();
        SelectedActionPlan = ActionPlans.SingleOrDefault(item => item.Id == id);
        StatusMessage = $"Created action plan {name}.";
    }

    public void DuplicateSelectedActionPlan()
    {
        if (_session is null || SelectedActionPlan is null)
        {
            return;
        }

        var sourceId = SelectedActionPlan.Id;
        var name = ActionPlanNameInput.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "Enter a name before duplicating an action plan.";
            return;
        }

        var duplicateId = _session.Editor.DuplicateActionPlan(sourceId, name);
        RefreshFromSession();
        SelectedActionPlan = ActionPlans.SingleOrDefault(item => item.Id == duplicateId);
        StatusMessage = $"Duplicated action plan {sourceId} as {name}.";
    }

    public void DeleteSelectedActionPlan()
    {
        if (_session is null || SelectedActionPlan is null)
        {
            return;
        }

        var selectedId = SelectedActionPlan.Id;
        var result = _session.Editor.DeleteActionPlan(selectedId);
        if (!result.IsSuccess)
        {
            StatusMessage = result.ErrorMessage;
            SelectedActionPlan = ActionPlans.SingleOrDefault(item => item.Id == selectedId);
            return;
        }

        RefreshFromSession();
        SelectedActionPlan = null;
        StatusMessage = $"Deleted action plan {selectedId}.";
    }

    public void AddMoveFacingBehaviorStepToSelectedActionPlan()
    {
        SelectedActionStepToAdd = AvailableActionSteps.SingleOrDefault(item => item.Kind == ActionPlanBehaviorStepKind.MoveFacing);
        AddSelectedBehaviorStepToSelectedActionPlan();
    }

    public void AddPickupTargetBehaviorStepToSelectedActionPlan()
    {
        SelectedActionStepToAdd = AvailableActionSteps.SingleOrDefault(item => item.Kind == ActionPlanBehaviorStepKind.PickupTarget);
        AddSelectedBehaviorStepToSelectedActionPlan();
    }

    public void AddSelectedBehaviorStepToSelectedActionPlan()
    {
        if (_session is null || SelectedActionPlan is null || SelectedActionStepToAdd is null)
        {
            return;
        }

        var planId = SelectedActionPlan.Id;
        var nextIndex = BehaviorSteps.Count;
        var stepKind = SelectedActionStepToAdd.Kind;
        var displayName = SelectedActionStepToAdd.DisplayName;
        _session.Editor.AddActionPlanBehaviorStep(planId, stepKind);
        RefreshFromSession();
        SelectedActionPlan = ActionPlans.SingleOrDefault(item => item.Id == planId);
        SelectBehaviorStep(nextIndex);
        StatusMessage = $"Added {displayName} action step.";
    }

    public void MoveSelectedBehaviorStepUp()
    {
        if (_session is null || SelectedActionPlan is null || SelectedBehaviorStep is null || SelectedBehaviorStep.Index == 0)
        {
            return;
        }

        var planId = SelectedActionPlan.Id;
        var fromIndex = SelectedBehaviorStep.Index;
        var toIndex = fromIndex - 1;
        _session.Editor.MoveActionPlanBehaviorStep(planId, fromIndex, toIndex);
        RefreshFromSession();
        SelectedActionPlan = ActionPlans.SingleOrDefault(item => item.Id == planId);
        SelectBehaviorStep(toIndex);
        StatusMessage = "Moved action step up.";
    }

    public void MoveSelectedBehaviorStepDown()
    {
        if (_session is null || SelectedActionPlan is null || SelectedBehaviorStep is null || SelectedBehaviorStep.Index >= BehaviorSteps.Count - 1)
        {
            return;
        }

        var planId = SelectedActionPlan.Id;
        var fromIndex = SelectedBehaviorStep.Index;
        var toIndex = fromIndex + 1;
        _session.Editor.MoveActionPlanBehaviorStep(planId, fromIndex, toIndex);
        RefreshFromSession();
        SelectedActionPlan = ActionPlans.SingleOrDefault(item => item.Id == planId);
        SelectBehaviorStep(toIndex);
        StatusMessage = "Moved action step down.";
    }

    public void RemoveSelectedBehaviorStep()
    {
        if (_session is null || SelectedActionPlan is null || SelectedBehaviorStep is null)
        {
            return;
        }

        var planId = SelectedActionPlan.Id;
        var removedIndex = SelectedBehaviorStep.Index;
        var removedName = SelectedBehaviorStep.DisplayName;
        _session.Editor.RemoveActionPlanBehaviorStep(planId, removedIndex);
        RefreshFromSession();
        SelectedActionPlan = ActionPlans.SingleOrDefault(item => item.Id == planId);
        if (BehaviorSteps.Count > 0)
        {
            SelectBehaviorStep(Math.Min(removedIndex, BehaviorSteps.Count - 1));
        }
        else
        {
            SelectedBehaviorStep = null;
        }

        StatusMessage = $"Removed {removedName} action step.";
    }

    public void ApplySelectedActionPlanStepLabel()
    {
        if (_session is null || SelectedActionPlan is null || SelectedActionPlanStep is null)
        {
            return;
        }

        var label = ActionPlanStepLabelInput.Trim();
        if (string.IsNullOrWhiteSpace(label))
        {
            StatusMessage = "Enter a step label before applying.";
            return;
        }

        var planId = SelectedActionPlan.Id;
        var stepIndex = SelectedActionPlanStep.Index;
        var step = GetSelectedActionPlanDescriptor().Steps[stepIndex];
        _session.Editor.UpdateActionPlanStep(planId, stepIndex, step with { Label = label });
        RefreshFromSession();
        SelectedActionPlan = ActionPlans.SingleOrDefault(item => item.Id == planId);
        SelectActionPlanStep(stepIndex);
        StatusMessage = $"Updated step label to {label}.";
    }

    public void AddWaitStepToSelectedActionPlan()
    {
        if (_session is null || SelectedActionPlan is null)
        {
            return;
        }

        var planId = SelectedActionPlan.Id;
        var nextIndex = GetSelectedActionPlanDescriptor().Steps.Count;
        _session.Editor.AddActionPlanStep(
            planId,
            new ActionPlanStepDescriptor($"wait {nextIndex + 1}", [], PlanEffectDescriptor.Wait(), OnFailure: null));
        RefreshFromSession();
        SelectedActionPlan = ActionPlans.SingleOrDefault(item => item.Id == planId);
        SelectActionPlanStep(nextIndex);
        StatusMessage = "Added wait step.";
    }

    public void MoveSelectedActionPlanStepUp()
    {
        if (_session is null || SelectedActionPlan is null || SelectedActionPlanStep is null || SelectedActionPlanStep.Index == 0)
        {
            return;
        }

        var planId = SelectedActionPlan.Id;
        var fromIndex = SelectedActionPlanStep.Index;
        var toIndex = fromIndex - 1;
        _session.Editor.MoveActionPlanStep(planId, fromIndex, toIndex);
        RefreshFromSession();
        SelectedActionPlan = ActionPlans.SingleOrDefault(item => item.Id == planId);
        SelectActionPlanStep(toIndex);
        StatusMessage = "Moved step up.";
    }

    public void MoveSelectedActionPlanStepDown()
    {
        if (_session is null || SelectedActionPlan is null || SelectedActionPlanStep is null || SelectedActionPlanStep.Index >= ActionPlanSteps.Count - 1)
        {
            return;
        }

        var planId = SelectedActionPlan.Id;
        var fromIndex = SelectedActionPlanStep.Index;
        var toIndex = fromIndex + 1;
        _session.Editor.MoveActionPlanStep(planId, fromIndex, toIndex);
        RefreshFromSession();
        SelectedActionPlan = ActionPlans.SingleOrDefault(item => item.Id == planId);
        SelectActionPlanStep(toIndex);
        StatusMessage = "Moved step down.";
    }

    public void RemoveSelectedActionPlanStep()
    {
        if (_session is null || SelectedActionPlan is null || SelectedActionPlanStep is null)
        {
            return;
        }

        var planId = SelectedActionPlan.Id;
        var removedIndex = SelectedActionPlanStep.Index;
        var removedLabel = SelectedActionPlanStep.Label;
        _session.Editor.RemoveActionPlanStep(planId, removedIndex);
        RefreshFromSession();
        SelectedActionPlan = ActionPlans.SingleOrDefault(item => item.Id == planId);
        if (ActionPlanSteps.Count > 0)
        {
            SelectActionPlanStep(Math.Min(removedIndex, ActionPlanSteps.Count - 1));
        }
        else
        {
            SelectedActionPlanStep = null;
            ActionPlanStepLabelInput = string.Empty;
        }

        StatusMessage = $"Removed step {removedLabel}.";
    }

    public void SetSelectedStepSuccessEffect()
    {
        if (SelectedSuccessEffectKind == PlanEffectKind.SetVariable)
        {
            StatusMessage = "SetVariable is legacy-only and cannot be authored from the editor.";
            return;
        }

        UpdateSelectedStepEffect(CreateEffect(SelectedSuccessEffectKind, success: true), updateSuccess: true);
    }

    public void SetSelectedStepFailureEffect()
    {
        if (SelectedFailureEffectKind == PlanEffectKind.SetVariable)
        {
            StatusMessage = "SetVariable is legacy-only and cannot be authored from the editor.";
            return;
        }

        UpdateSelectedStepEffect(CreateEffect(SelectedFailureEffectKind, success: false), updateSuccess: false);
    }

    public void ClearSelectedStepFailureEffect()
    {
        UpdateSelectedStepEffect(effect: null, updateSuccess: false);
    }

    public void AddCanMoveCheckToSelectedStep()
    {
        SelectedCheckKind = PlanCheckKind.CanMove;
        AddSelectedCheckToSelectedStep();
    }

    public void AddSelectedCheckToSelectedStep()
    {
        if (_session is null || SelectedActionPlan is null || SelectedActionPlanStep is null)
        {
            return;
        }

        var planId = SelectedActionPlan.Id;
        var stepIndex = SelectedActionPlanStep.Index;
        var step = GetSelectedActionPlanDescriptor().Steps[stepIndex];
        var checks = step.Checks.ToList();
        checks.Add(CreateCheck());
        _session.Editor.UpdateActionPlanStep(planId, stepIndex, step with { Checks = checks });
        RefreshFromSession();
        SelectedActionPlan = ActionPlans.SingleOrDefault(item => item.Id == planId);
        SelectActionPlanStep(stepIndex);
        SelectedActionPlanStepCheck = ActionPlanStepChecks.LastOrDefault();
        StatusMessage = $"Added {SelectedCheckKind} check to step {step.Label}.";
    }

    public void UpdateSelectedStepCheck()
    {
        if (_session is null || SelectedActionPlan is null || SelectedActionPlanStep is null || SelectedActionPlanStepCheck is null)
        {
            return;
        }

        var planId = SelectedActionPlan.Id;
        var stepIndex = SelectedActionPlanStep.Index;
        var checkIndex = SelectedActionPlanStepCheck.Index;
        var step = GetSelectedActionPlanDescriptor().Steps[stepIndex];
        var checks = step.Checks.ToList();
        checks[checkIndex] = CreateCheck();
        _session.Editor.UpdateActionPlanStep(planId, stepIndex, step with { Checks = checks });
        RefreshFromSession();
        SelectedActionPlan = ActionPlans.SingleOrDefault(item => item.Id == planId);
        SelectActionPlanStep(stepIndex);
        SelectActionPlanStepCheck(checkIndex);
        StatusMessage = $"Updated check {checkIndex + 1} for step {step.Label}.";
    }

    public void RemoveSelectedStepCheck()
    {
        if (_session is null || SelectedActionPlan is null || SelectedActionPlanStep is null || SelectedActionPlanStepCheck is null)
        {
            return;
        }

        var planId = SelectedActionPlan.Id;
        var stepIndex = SelectedActionPlanStep.Index;
        var checkIndex = SelectedActionPlanStepCheck.Index;
        var step = GetSelectedActionPlanDescriptor().Steps[stepIndex];
        var checks = step.Checks.ToList();
        checks.RemoveAt(checkIndex);
        _session.Editor.UpdateActionPlanStep(planId, stepIndex, step with { Checks = checks });
        RefreshFromSession();
        SelectedActionPlan = ActionPlans.SingleOrDefault(item => item.Id == planId);
        SelectActionPlanStep(stepIndex);
        if (ActionPlanStepChecks.Count > 0)
        {
            SelectActionPlanStepCheck(Math.Min(checkIndex, ActionPlanStepChecks.Count - 1));
        }
        else
        {
            SelectedActionPlanStepCheck = null;
        }

        StatusMessage = $"Removed check {checkIndex + 1} from step {step.Label}.";
    }

    public void MoveSelectedStepCheckUp()
    {
        if (SelectedActionPlanStepCheck is null || SelectedActionPlanStepCheck.Index == 0)
        {
            return;
        }

        MoveSelectedStepCheck(SelectedActionPlanStepCheck.Index - 1, "Moved check up.");
    }

    public void MoveSelectedStepCheckDown()
    {
        if (SelectedActionPlanStepCheck is null || SelectedActionPlanStepCheck.Index >= ActionPlanStepChecks.Count - 1)
        {
            return;
        }

        MoveSelectedStepCheck(SelectedActionPlanStepCheck.Index + 1, "Moved check down.");
    }

    public void AssignSelectedDefaultActionPlan()
    {
        if (_session is null || SelectedPreset is null || SelectedDefaultActionPlan is null)
        {
            return;
        }

        var presetId = SelectedPreset.Id;
        var presetName = SelectedPreset.Name;
        var plan = SelectedDefaultActionPlan;
        _session.Editor.SetDefaultActionPlan(presetId, plan.Id);
        RefreshFromSession();
        SelectEntityPreset(presetId);
        SelectedDefaultActionPlan = ActionPlans.SingleOrDefault(item => item.Id == plan.Id);
        StatusMessage = $"Assigned {plan.Id} to {presetName}.";
    }

    public void ClearSelectedDefaultActionPlan()
    {
        if (_session is null || SelectedPreset is null)
        {
            return;
        }

        var presetId = SelectedPreset.Id;
        var presetName = SelectedPreset.Name;
        _session.Editor.ClearDefaultActionPlan(presetId);
        RefreshFromSession();
        SelectEntityPreset(presetId);
        SelectedDefaultActionPlan = null;
        StatusMessage = $"Cleared default action plan for {presetName}.";
    }

    public void SetInitialFacing()
    {
        if (_session is null || SelectedPreset is null)
        {
            return;
        }

        var presetId = SelectedPreset.Id;
        _session.Editor.SetInitialFacing(presetId, SelectedInitialFacing);
        RefreshFromSession();
        SelectEntityPreset(presetId);
        StatusMessage = $"Set initial facing to {SelectedInitialFacing}.";
    }

    public void ClearInitialFacing()
    {
        if (_session is null || SelectedPreset is null)
        {
            return;
        }

        var presetId = SelectedPreset.Id;
        _session.Editor.ClearInitialFacing(presetId);
        RefreshFromSession();
        SelectEntityPreset(presetId);
        StatusMessage = "Cleared initial facing.";
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
        var selectedActionPlanId = SelectedActionPlan?.Id;
        EntityPresets.Clear();
        ActionPlans.Clear();
        ActionPlanSteps.Clear();
        BehaviorSteps.Clear();
        AvailableActionSteps.Clear();
        ActionPlanStepChecks.Clear();
        CarriedEntities.Clear();
        InventoryGridCells.Clear();
        ValidationMessages.Clear();
        SelectedPresetDiagnostics.Clear();
        SelectedActionPlanDiagnostics.Clear();
        SelectedActionPlanStepDiagnostics.Clear();
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

        foreach (var plan in _session.Editor.ListActionPlans())
        {
            ActionPlans.Add(new ActionPlanListItem(
                plan.TemplateId,
                plan.Descriptor.Id.Value,
                plan.Descriptor.Steps.Count,
                plan.Descriptor.Behavior?.Steps.Count ?? 0,
                ActionPlanShapeClassifier.Classify(plan.Descriptor)));
        }

        foreach (var step in _session.Editor.ListActionSteps())
        {
            AvailableActionSteps.Add(new ActionStepCatalogListItem(
                step.Kind,
                step.DisplayName,
                step.Description));
        }

        SelectedActionStepToAdd = AvailableActionSteps.FirstOrDefault();

        _selectedActionPlan = selectedActionPlanId is null
            ? null
            : ActionPlans.SingleOrDefault(item => item.Id == selectedActionPlanId.Value);
        OnPropertyChanged(nameof(SelectedActionPlan));
        OnPropertyChanged(nameof(SelectedActionPlanShape));
        OnPropertyChanged(nameof(SelectedActionPlanShapeDetail));
        OnPropertyChanged(nameof(IsLegacyActionPlanCompatibilityVisible));
        RefreshActionPlanSteps();
        RefreshBehaviorSteps();
        OnPropertyChanged(nameof(SelectedBehaviorChainSummary));
        OnPropertyChanged(nameof(SelectedBehaviorChainDefaultStateHints));

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
        RefreshSelectedDiagnostics();
    }

    private void RefreshActionStateDefaults(EntityTemplateId templateId)
    {
        HasInitialFacing = false;
        SelectedInitialFacing = Direction.West;

        if (_session is null)
        {
            return;
        }

        var defaults = _session.Editor.GetActionStateDefaults(templateId);
        if (defaults.Facing is { } facing)
        {
            HasInitialFacing = true;
            SelectedInitialFacing = facing;
        }
    }

    private void RefreshSelectedDiagnostics()
    {
        SelectedPresetDiagnostics.Clear();
        SelectedActionPlanDiagnostics.Clear();
        SelectedActionPlanStepDiagnostics.Clear();

        if (_session is null)
        {
            return;
        }

        var validation = _session.Editor.Validate();
        if (SelectedPreset is not null && EntityPresets.Any(item => item.Id == SelectedPreset.Id))
        {
            foreach (var diagnostic in validation.ForEntityTemplate(SelectedPreset.Id))
            {
                SelectedPresetDiagnostics.Add(diagnostic.Message);
            }
        }

        if (SelectedActionPlan is not null)
        {
            foreach (var diagnostic in validation.ForActionPlan(SelectedActionPlan.Id))
            {
                SelectedActionPlanDiagnostics.Add(diagnostic.Message);
            }
        }

        if (SelectedActionPlan is not null && SelectedActionPlanStep is not null)
        {
            foreach (var diagnostic in validation.ForActionPlanStep(SelectedActionPlan.Id, SelectedActionPlanStep.Index))
            {
                SelectedActionPlanStepDiagnostics.Add(diagnostic.Message);
            }
        }

        if (SelectedActionPlan is not null && SelectedBehaviorStep is not null)
        {
            foreach (var diagnostic in validation.ForActionPlanStep(SelectedActionPlan.Id, SelectedBehaviorStep.Index))
            {
                SelectedActionPlanStepDiagnostics.Add(diagnostic.Message);
            }
        }
    }

    private void RefreshActionPlanSteps()
    {
        ActionPlanSteps.Clear();
        ActionPlanStepChecks.Clear();
        SelectedActionPlanStep = null;
        SelectedActionPlanStepCheck = null;
        ActionPlanStepLabelInput = string.Empty;
        CheckInventoryCoordX = 0;
        CheckInventoryCoordY = 0;

        if (_session is null || SelectedActionPlan is null)
        {
            return;
        }

        var plan = _session.Editor.ListActionPlans().Single(item => item.TemplateId == SelectedActionPlan.Id);
        for (var index = 0; index < plan.Descriptor.Steps.Count; index++)
        {
            var step = plan.Descriptor.Steps[index];
            ActionPlanSteps.Add(new ActionPlanStepListItem(
                index,
                step.Label,
                step.Checks.Count == 0
                    ? "Checks: none"
                    : $"Checks: {string.Join(", ", step.Checks.Select(FormatCheck))}",
                step.OnSuccess is null ? "Success: none" : $"Success: {FormatEffect(step.OnSuccess)}",
                step.OnFailure is null ? "Failure: none" : $"Failure: {FormatEffect(step.OnFailure)}"));
        }
    }

    private void RefreshBehaviorSteps()
    {
        BehaviorSteps.Clear();
        SelectedBehaviorStep = null;

        if (_session is null || SelectedActionPlan is null)
        {
            return;
        }

        var plan = _session.Editor.ListActionPlans().Single(item => item.TemplateId == SelectedActionPlan.Id);
        var steps = plan.Descriptor.Behavior?.Steps ?? [];
        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            var metadata = ActionStepCatalog.Get(step.Kind);
            BehaviorSteps.Add(new ActionPlanBehaviorStepListItem(
                index,
                step.Kind,
                step.TargetSlot,
                metadata.DisplayName,
                metadata.Description,
                FormatSlots("Requires", metadata.RequiredState),
                FormatSlots("Defaults", metadata.DefaultableState),
                FormatSlots("Writes", metadata.StateWrites)));
        }

        OnPropertyChanged(nameof(SelectedBehaviorChainSummary));
        OnPropertyChanged(nameof(SelectedBehaviorChainDefaultStateHints));
    }

    private ActionPlanDescriptor GetSelectedActionPlanDescriptor() =>
        _session!.Editor.ListActionPlans().Single(item => item.TemplateId == SelectedActionPlan!.Id).Descriptor;

    private void SelectActionPlanStep(int index)
    {
        SelectedActionPlanStep = ActionPlanSteps.SingleOrDefault(item => item.Index == index);
    }

    private void SelectBehaviorStep(int index)
    {
        SelectedBehaviorStep = BehaviorSteps.SingleOrDefault(item => item.Index == index);
    }

    private void SelectActionPlanStepCheck(int index)
    {
        SelectedActionPlanStepCheck = ActionPlanStepChecks.SingleOrDefault(item => item.Index == index);
    }

    private void RefreshSelectedStepChecks(int stepIndex)
    {
        ActionPlanStepChecks.Clear();
        SelectedActionPlanStepCheck = null;
        CheckInventoryCoordX = 0;
        CheckInventoryCoordY = 0;

        if (_session is null || SelectedActionPlan is null)
        {
            return;
        }

        var step = GetSelectedActionPlanDescriptor().Steps[stepIndex];
        for (var index = 0; index < step.Checks.Count; index++)
        {
            var check = step.Checks[index];
            ActionPlanStepChecks.Add(new ActionPlanStepCheckListItem(
                index,
                check.Kind,
                check.DirectionVariable,
                check.TargetVariable,
                check.InventoryCoord,
                FormatCheck(check)));
        }
    }

    private void MoveSelectedStepCheck(int toIndex, string statusMessage)
    {
        if (_session is null || SelectedActionPlan is null || SelectedActionPlanStep is null || SelectedActionPlanStepCheck is null)
        {
            return;
        }

        var planId = SelectedActionPlan.Id;
        var stepIndex = SelectedActionPlanStep.Index;
        var fromIndex = SelectedActionPlanStepCheck.Index;
        var step = GetSelectedActionPlanDescriptor().Steps[stepIndex];
        var checks = step.Checks.ToList();
        var check = checks[fromIndex];
        checks.RemoveAt(fromIndex);
        checks.Insert(toIndex, check);
        _session.Editor.UpdateActionPlanStep(planId, stepIndex, step with { Checks = checks });
        RefreshFromSession();
        SelectedActionPlan = ActionPlans.SingleOrDefault(item => item.Id == planId);
        SelectActionPlanStep(stepIndex);
        SelectActionPlanStepCheck(toIndex);
        StatusMessage = statusMessage;
    }

    private void UpdateSelectedStepEffect(PlanEffectDescriptor? effect, bool updateSuccess)
    {
        if (_session is null || SelectedActionPlan is null || SelectedActionPlanStep is null)
        {
            return;
        }

        var planId = SelectedActionPlan.Id;
        var stepIndex = SelectedActionPlanStep.Index;
        var step = GetSelectedActionPlanDescriptor().Steps[stepIndex];
        var updated = updateSuccess
            ? step with { OnSuccess = effect }
            : step with { OnFailure = effect };
        _session.Editor.UpdateActionPlanStep(planId, stepIndex, updated);
        RefreshFromSession();
        SelectedActionPlan = ActionPlans.SingleOrDefault(item => item.Id == planId);
        SelectActionPlanStep(stepIndex);
        StatusMessage = updateSuccess
            ? $"Updated success effect for step {step.Label}."
            : effect is null
                ? $"Cleared failure effect for step {step.Label}."
                : $"Updated failure effect for step {step.Label}.";
    }

    private PlanEffectDescriptor CreateEffect(PlanEffectKind kind, bool success) =>
        kind switch
        {
            PlanEffectKind.Teleport => PlanEffectDescriptor.Teleport(GetMovementTarget(success), GetMovementDestination(success)),
            PlanEffectKind.Move => PlanEffectDescriptor.Move(),
            PlanEffectKind.Pickup => PlanEffectDescriptor.Pickup(GetInventoryCoord(success)),
            PlanEffectKind.Drop => PlanEffectDescriptor.Drop(GetMovementTarget(success), GetMovementDestination(success)),
            PlanEffectKind.ReverseDirection => PlanEffectDescriptor.ReverseDirection(consumesTurn: false, continuePlan: false),
            PlanEffectKind.Wait => PlanEffectDescriptor.Wait(),
            PlanEffectKind.CallPlan => PlanEffectDescriptor.CallPlan(GetCallPlan(success)),
            _ => throw new InvalidOperationException($"Unsupported effect kind {kind}.")
        };

    private PlanCheckDescriptor CreateCheck() =>
        SelectedCheckKind switch
        {
            PlanCheckKind.CanMove => PlanCheckDescriptor.CanMove(),
            PlanCheckKind.BlockingEntity => PlanCheckDescriptor.BlockingEntity(),
            PlanCheckKind.CanPickup => PlanCheckDescriptor.CanPickup(new GridCoord(CheckInventoryCoordX, CheckInventoryCoordY)),
            _ => throw new InvalidOperationException($"Unsupported check kind {SelectedCheckKind}.")
        };

    private GridCoord GetInventoryCoord(bool success) =>
        success ? new GridCoord(SuccessInventoryCoordX, SuccessInventoryCoordY) : new GridCoord(FailureInventoryCoordX, FailureInventoryCoordY);

    private MovementTargetDescriptor GetMovementTarget(bool success)
    {
        var kind = success ? SelectedSuccessMovementTargetKind : SelectedFailureMovementTargetKind;
        return kind switch
        {
            MovementTargetKind.Self => MovementTargetDescriptor.Self(),
            MovementTargetKind.CanonicalTarget => MovementTargetDescriptor.CanonicalTarget(),
            MovementTargetKind.Entity => MovementTargetDescriptor.Entity(new EntityId(Normalize(success ? SuccessMovementTargetEntityIdInput : FailureMovementTargetEntityIdInput, "target"))),
            MovementTargetKind.CarriedInventoryCoord => MovementTargetDescriptor.CarriedInventoryCoord(success
                ? new GridCoord(SuccessMovementTargetCoordX, SuccessMovementTargetCoordY)
                : new GridCoord(FailureMovementTargetCoordX, FailureMovementTargetCoordY)),
            _ => throw new InvalidOperationException($"Unsupported movement target kind {kind}.")
        };
    }

    private MovementDestinationDescriptor GetMovementDestination(bool success)
    {
        var kind = success ? SelectedSuccessMovementDestinationKind : SelectedFailureMovementDestinationKind;
        return kind switch
        {
            MovementDestinationKind.PlaneCoord => MovementDestinationDescriptor.Plane(new PlaneCoord(
                new PlaneId(Normalize(success ? SuccessMovementDestinationPlaneIdInput : FailureMovementDestinationPlaneIdInput, "world")),
                success ? new GridCoord(SuccessMovementDestinationCoordX, SuccessMovementDestinationCoordY) : new GridCoord(FailureMovementDestinationCoordX, FailureMovementDestinationCoordY))),
            MovementDestinationKind.InventorySlot => MovementDestinationDescriptor.InventorySlot(
                new EntityId(Normalize(success ? SuccessMovementDestinationOwnerIdInput : FailureMovementDestinationOwnerIdInput, "actor")),
                success ? new GridCoord(SuccessMovementDestinationCoordX, SuccessMovementDestinationCoordY) : new GridCoord(FailureMovementDestinationCoordX, FailureMovementDestinationCoordY)),
            MovementDestinationKind.AdjacentToSelf => MovementDestinationDescriptor.AdjacentToSelf(success ? SuccessMovementDestinationDirection : FailureMovementDestinationDirection),
            MovementDestinationKind.AdjacentToEntity => MovementDestinationDescriptor.AdjacentToEntity(
                new EntityId(Normalize(success ? SuccessMovementDestinationAnchorEntityIdInput : FailureMovementDestinationAnchorEntityIdInput, "target")),
                success ? SuccessMovementDestinationDirection : FailureMovementDestinationDirection),
            MovementDestinationKind.AdjacentToCanonicalTarget => MovementDestinationDescriptor.AdjacentToCanonicalTarget(success ? SuccessMovementDestinationDirection : FailureMovementDestinationDirection),
            _ => throw new InvalidOperationException($"Unsupported movement destination kind {kind}.")
        };
    }

    private static string Normalize(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private ActionPlanId GetCallPlan(bool success)
    {
        var plan = success ? SelectedSuccessCallPlan : SelectedFailureCallPlan;
        return new ActionPlanId((plan ?? ActionPlans.FirstOrDefault())?.Id.Value ?? "wait");
    }

    private void PopulateEffectInputs(int stepIndex)
    {
        if (_session is null || SelectedActionPlan is null)
        {
            return;
        }

        var step = GetSelectedActionPlanDescriptor().Steps[stepIndex];
        PopulateEffectInputs(step.OnSuccess, success: true);
        PopulateEffectInputs(step.OnFailure, success: false);
    }

    private void PopulateEffectInputs(PlanEffectDescriptor? effect, bool success)
    {
        if (effect is null)
        {
            if (success)
            {
                SelectedSuccessEffectKind = PlanEffectKind.Wait;
            }
            else
            {
                SelectedFailureEffectKind = PlanEffectKind.Wait;
            }

            return;
        }

        if (success)
        {
            SelectedSuccessEffectKind = effect.Kind;
            SuccessInventoryCoordX = effect.InventoryCoord?.X ?? 0;
            SuccessInventoryCoordY = effect.InventoryCoord?.Y ?? 0;
            PopulateMovementInputs(effect, success: true);
            SelectedSuccessCallPlan = effect.PlanId is { } planId ? ActionPlans.SingleOrDefault(item => item.Id.Value == planId.Value) : null;
        }
        else
        {
            SelectedFailureEffectKind = effect.Kind;
            FailureInventoryCoordX = effect.InventoryCoord?.X ?? 0;
            FailureInventoryCoordY = effect.InventoryCoord?.Y ?? 0;
            PopulateMovementInputs(effect, success: false);
            SelectedFailureCallPlan = effect.PlanId is { } planId ? ActionPlans.SingleOrDefault(item => item.Id.Value == planId.Value) : null;
        }
    }

    private void PopulateMovementInputs(PlanEffectDescriptor effect, bool success)
    {
        if (effect.MovementTarget is { } target)
        {
            if (success)
            {
                SelectedSuccessMovementTargetKind = target.Kind;
                SuccessMovementTargetEntityIdInput = target.EntityId?.Value ?? string.Empty;
                SuccessMovementTargetCoordX = target.InventoryCoord?.X ?? 0;
                SuccessMovementTargetCoordY = target.InventoryCoord?.Y ?? 0;
            }
            else
            {
                SelectedFailureMovementTargetKind = target.Kind;
                FailureMovementTargetEntityIdInput = target.EntityId?.Value ?? string.Empty;
                FailureMovementTargetCoordX = target.InventoryCoord?.X ?? 0;
                FailureMovementTargetCoordY = target.InventoryCoord?.Y ?? 0;
            }
        }

        if (effect.MovementDestination is { } destination)
        {
            if (success)
            {
                SelectedSuccessMovementDestinationKind = destination.Kind;
                SuccessMovementDestinationPlaneIdInput = destination.PlaneCoord?.PlaneId.Value ?? "world";
                SuccessMovementDestinationCoordX = destination.PlaneCoord?.Coord.X ?? destination.InventoryCoord?.X ?? 0;
                SuccessMovementDestinationCoordY = destination.PlaneCoord?.Coord.Y ?? destination.InventoryCoord?.Y ?? 0;
                SuccessMovementDestinationOwnerIdInput = destination.OwnerId?.Value ?? string.Empty;
                SuccessMovementDestinationAnchorEntityIdInput = destination.AnchorEntityId?.Value ?? string.Empty;
                SuccessMovementDestinationDirection = destination.Direction ?? Direction.South;
            }
            else
            {
                SelectedFailureMovementDestinationKind = destination.Kind;
                FailureMovementDestinationPlaneIdInput = destination.PlaneCoord?.PlaneId.Value ?? "world";
                FailureMovementDestinationCoordX = destination.PlaneCoord?.Coord.X ?? destination.InventoryCoord?.X ?? 0;
                FailureMovementDestinationCoordY = destination.PlaneCoord?.Coord.Y ?? destination.InventoryCoord?.Y ?? 0;
                FailureMovementDestinationOwnerIdInput = destination.OwnerId?.Value ?? string.Empty;
                FailureMovementDestinationAnchorEntityIdInput = destination.AnchorEntityId?.Value ?? string.Empty;
                FailureMovementDestinationDirection = destination.Direction ?? Direction.South;
            }
        }
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
        SelectedBulk = 0;
        SelectedAperture = 0;
        SelectedGlyph = string.Empty;
        SelectedColor = PresentationColor.Gray;
        SelectedDefaultActionPlan = null;
        HasInitialFacing = false;
        SelectedInitialFacing = Direction.West;
        CarriedEntities.Clear();
        InventoryGridCells.Clear();
        SelectedCarriedEntity = null;
    }

    private static string FormatCheck(PlanCheckDescriptor check)
    {
        var fields = new List<string>();
        if (!string.IsNullOrWhiteSpace(check.DirectionVariable))
        {
            fields.Add($"directionVariable={check.DirectionVariable}");
        }

        if (!string.IsNullOrWhiteSpace(check.TargetVariable))
        {
            fields.Add($"targetVariable={check.TargetVariable}");
        }

        if (check.InventoryCoord is { } coord)
        {
            fields.Add($"inventoryCoord={coord.X},{coord.Y}");
        }

        return fields.Count == 0 ? check.Kind.ToString() : $"{check.Kind}({string.Join(", ", fields)})";
    }

    private static string FormatSlots(string label, IReadOnlyList<PlanPrimitiveSlotDescriptor> slots) =>
        slots.Count == 0
            ? $"{label}: none"
            : $"{label}: {string.Join(", ", slots.Select(slot => $"{slot.Slot}:{slot.ValueKind}"))}";

    private static string FormatDefaultStateHint(PlanPrimitiveSlotDescriptor slot) =>
        slot.Slot switch
        {
            ActionPlanSlot.Facing => "Facing defaults to West when materialized for authored MoveFacing behavior",
            ActionPlanSlot.Target => "Target defaults to Self when target-based Action Steps need a valid initial target slot",
            _ => $"{slot.Slot} can be defaulted as {slot.ValueKind}"
        };

    private static string FormatEffect(PlanEffectDescriptor effect)
    {
        var fields = new List<string>();
        if (!string.IsNullOrWhiteSpace(effect.DirectionVariable))
        {
            fields.Add($"directionVariable={effect.DirectionVariable}");
        }

        if (!string.IsNullOrWhiteSpace(effect.TargetVariable))
        {
            fields.Add($"targetVariable={effect.TargetVariable}");
        }

        if (effect.InventoryCoord is { } coord)
        {
            fields.Add($"inventoryCoord={coord.X},{coord.Y}");
        }

        if (effect.PlanId is { } planId)
        {
            fields.Add($"planId={planId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(effect.VariableName))
        {
            fields.Add($"variableName={effect.VariableName}");
        }

        if (effect.Value is not null)
        {
            fields.Add($"value={effect.Value}");
        }

        if (effect.MovementTarget is { } movementTarget)
        {
            fields.Add($"movementTarget={FormatMovementTarget(movementTarget)}");
        }

        if (effect.MovementDestination is { } movementDestination)
        {
            fields.Add($"movementDestination={FormatMovementDestination(movementDestination)}");
        }

        if (effect.Kind is PlanEffectKind.ReverseDirection or PlanEffectKind.SetVariable)
        {
            fields.Add($"consumesTurn={effect.ConsumesTurn}");
            fields.Add($"continuePlan={effect.ContinuePlan}");
        }

        return fields.Count == 0 ? effect.Kind.ToString() : $"{effect.Kind}({string.Join(", ", fields)})";
    }

    private static string FormatMovementTarget(MovementTargetDescriptor target) =>
        target.Kind switch
        {
            MovementTargetKind.Entity => $"Entity:{target.EntityId}",
            MovementTargetKind.CarriedInventoryCoord => target.InventoryCoord is { } coord ? $"CarriedInventoryCoord:{coord.X},{coord.Y}" : "CarriedInventoryCoord",
            _ => target.Kind.ToString()
        };

    private static string FormatMovementDestination(MovementDestinationDescriptor destination) =>
        destination.Kind switch
        {
            MovementDestinationKind.PlaneCoord => destination.PlaneCoord is { } coord ? $"PlaneCoord:{coord.PlaneId}({coord.Coord.X},{coord.Coord.Y})" : "PlaneCoord",
            MovementDestinationKind.InventorySlot => destination.InventoryCoord is { } coord ? $"InventorySlot:{destination.OwnerId}:{coord.X},{coord.Y}" : $"InventorySlot:{destination.OwnerId}",
            MovementDestinationKind.AdjacentToSelf => $"AdjacentToSelf:{destination.Direction}",
            MovementDestinationKind.AdjacentToEntity => $"AdjacentToEntity:{destination.AnchorEntityId}:{destination.Direction}",
            MovementDestinationKind.AdjacentToCanonicalTarget => $"AdjacentToCanonicalTarget:{destination.Direction}",
            _ => destination.Kind.ToString()
        };

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

public sealed record ActionPlanListItem(
    ActionPlanTemplateId Id,
    string RuntimeId,
    int StepCount,
    int BehaviorStepCount,
    ActionPlanShape Shape)
{
    public override string ToString() => BehaviorStepCount > 0
        ? $"{Id} ({BehaviorStepCount} action steps)"
        : $"{Id} ({StepCount} steps)";
}

public sealed record ActionStepCatalogListItem(
    ActionPlanBehaviorStepKind Kind,
    string DisplayName,
    string Description)
{
    public override string ToString() => DisplayName;
}

public sealed record ActionPlanBehaviorStepListItem(
    int Index,
    ActionPlanBehaviorStepKind Kind,
    int? TargetSlot,
    string DisplayName,
    string Description,
    string RequiredStateSummary,
    string DefaultStateSummary,
    string StateWritesSummary)
{
    public override string ToString()
    {
        var targetSlot = TargetSlot is { } slot ? $" | Target Slot: {slot}" : string.Empty;
        return $"{Index}: {DisplayName}{targetSlot} | {RequiredStateSummary} | {DefaultStateSummary} | {StateWritesSummary}";
    }
}

public sealed record ActionPlanStepListItem(
    int Index,
    string Label,
    string ChecksSummary,
    string SuccessSummary,
    string FailureSummary)
{
    public override string ToString() => $"{Index}: {Label} | {ChecksSummary} | {SuccessSummary} | {FailureSummary}";
}

public sealed record ActionPlanStepCheckListItem(
    int Index,
    PlanCheckKind Kind,
    string? DirectionVariable,
    string? TargetVariable,
    GridCoord? InventoryCoord,
    string Summary)
{
    public override string ToString() => $"{Index + 1}: {Summary}";
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
