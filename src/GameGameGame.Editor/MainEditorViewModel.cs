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
    private string _actionPlanNameInput = string.Empty;
    private PresentationColor _selectedColor = PresentationColor.Gray;
    private CarriedEntityListItem? _selectedCarriedEntity;
    private EntityPresetListItem? _selectedTemplateToPlace;
    private EntityPresetListItem? _selectedReplacementTemplate;
    private ActionPlanListItem? _selectedDefaultActionPlan;
    private ActionPlanListItem? _selectedActionPlan;
    private ActionPlanStepListItem? _selectedActionPlanStep;
    private ActionPlanStepCheckListItem? _selectedActionPlanStepCheck;
    private string _actionPlanStepLabelInput = string.Empty;
    private PlanCheckKind _selectedCheckKind = PlanCheckKind.CanMove;
    private string _checkDirectionVariableInput = string.Empty;
    private string _checkTargetVariableInput = string.Empty;
    private PlanEffectKind _selectedSuccessEffectKind = PlanEffectKind.Wait;
    private PlanEffectKind _selectedFailureEffectKind = PlanEffectKind.Wait;
    private string _successDirectionVariableInput = string.Empty;
    private string _failureDirectionVariableInput = string.Empty;
    private ActionPlanListItem? _selectedSuccessCallPlan;
    private ActionPlanListItem? _selectedFailureCallPlan;
    private DefaultPlanVariableListItem? _selectedDefaultPlanVariable;
    private string _defaultVariableNameInput = string.Empty;
    private PlanValueKind _selectedDefaultVariableKind = PlanValueKind.Direction;
    private Direction _selectedDefaultVariableDirection = Direction.West;
    private string _defaultVariableEntityIdInput = string.Empty;
    private int _defaultVariableCoordX;
    private int _defaultVariableCoordY;
    private int _defaultVariableIntValue;
    private string? _statusMessage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<EntityPresetListItem> EntityPresets { get; } = [];

    public ObservableCollection<ActionPlanListItem> ActionPlans { get; } = [];

    public ObservableCollection<ActionPlanStepListItem> ActionPlanSteps { get; } = [];

    public ObservableCollection<ActionPlanStepCheckListItem> ActionPlanStepChecks { get; } = [];

    public ObservableCollection<DefaultPlanVariableListItem> DefaultPlanVariables { get; } = [];

    public ObservableCollection<CarriedEntityListItem> CarriedEntities { get; } = [];

    public ObservableCollection<InventoryGridCell> InventoryGridCells { get; } = [];

    public ObservableCollection<string> ValidationMessages { get; } = [];

    public ObservableCollection<string> YamlDiffLines { get; } = [];

    public IReadOnlyList<PlanValueKind> DefaultVariableKinds { get; } = Enum.GetValues<PlanValueKind>();

    public IReadOnlyList<Direction> Directions { get; } = Enum.GetValues<Direction>();

    public IReadOnlyList<PlanCheckKind> CheckKinds { get; } =
    [
        PlanCheckKind.CanMove,
        PlanCheckKind.BlockingEntity
    ];

    public IReadOnlyList<PlanEffectKind> EffectKinds { get; } =
    [
        PlanEffectKind.Wait,
        PlanEffectKind.Move,
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
            }
        }
    }

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
            CheckDirectionVariableInput = value.DirectionVariable ?? string.Empty;
            CheckTargetVariableInput = value.TargetVariable ?? string.Empty;
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

            OnPropertyChanged(nameof(IsCheckDirectionVariableVisible));
            OnPropertyChanged(nameof(IsCheckTargetVariableVisible));
        }
    }

    public bool IsCheckDirectionVariableVisible => SelectedCheckKind is PlanCheckKind.CanMove or PlanCheckKind.BlockingEntity;

    public bool IsCheckTargetVariableVisible => SelectedCheckKind == PlanCheckKind.BlockingEntity;

    public string CheckDirectionVariableInput
    {
        get => _checkDirectionVariableInput;
        set => SetField(ref _checkDirectionVariableInput, value);
    }

    public string CheckTargetVariableInput
    {
        get => _checkTargetVariableInput;
        set => SetField(ref _checkTargetVariableInput, value);
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

            OnPropertyChanged(nameof(IsSuccessDirectionVariableVisible));
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

            OnPropertyChanged(nameof(IsFailureDirectionVariableVisible));
            OnPropertyChanged(nameof(IsFailureCallPlanVisible));
        }
    }

    public bool IsSuccessDirectionVariableVisible => SelectedSuccessEffectKind == PlanEffectKind.Move;

    public bool IsSuccessCallPlanVisible => SelectedSuccessEffectKind == PlanEffectKind.CallPlan;

    public bool IsFailureDirectionVariableVisible => SelectedFailureEffectKind == PlanEffectKind.Move;

    public bool IsFailureCallPlanVisible => SelectedFailureEffectKind == PlanEffectKind.CallPlan;

    public string SuccessDirectionVariableInput
    {
        get => _successDirectionVariableInput;
        set => SetField(ref _successDirectionVariableInput, value);
    }

    public string FailureDirectionVariableInput
    {
        get => _failureDirectionVariableInput;
        set => SetField(ref _failureDirectionVariableInput, value);
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

    public DefaultPlanVariableListItem? SelectedDefaultPlanVariable
    {
        get => _selectedDefaultPlanVariable;
        set
        {
            if (!SetField(ref _selectedDefaultPlanVariable, value) || value is null)
            {
                return;
            }

            DefaultVariableNameInput = value.Name;
            SelectedDefaultVariableKind = value.Kind;
            SelectedDefaultVariableDirection = value.Value.DirectionValue ?? Direction.West;
            DefaultVariableEntityIdInput = value.Value.EntityValue?.Value ?? string.Empty;
            DefaultVariableCoordX = value.Value.CoordValue?.X ?? 0;
            DefaultVariableCoordY = value.Value.CoordValue?.Y ?? 0;
            DefaultVariableIntValue = value.Value.IntValue ?? 0;
        }
    }

    public string DefaultVariableNameInput
    {
        get => _defaultVariableNameInput;
        set => SetField(ref _defaultVariableNameInput, value);
    }

    public PlanValueKind SelectedDefaultVariableKind
    {
        get => _selectedDefaultVariableKind;
        set => SetField(ref _selectedDefaultVariableKind, value);
    }

    public Direction SelectedDefaultVariableDirection
    {
        get => _selectedDefaultVariableDirection;
        set => SetField(ref _selectedDefaultVariableDirection, value);
    }

    public string DefaultVariableEntityIdInput
    {
        get => _defaultVariableEntityIdInput;
        set => SetField(ref _defaultVariableEntityIdInput, value);
    }

    public int DefaultVariableCoordX
    {
        get => _defaultVariableCoordX;
        set => SetField(ref _defaultVariableCoordX, value);
    }

    public int DefaultVariableCoordY
    {
        get => _defaultVariableCoordY;
        set => SetField(ref _defaultVariableCoordY, value);
    }

    public int DefaultVariableIntValue
    {
        get => _defaultVariableIntValue;
        set => SetField(ref _defaultVariableIntValue, value);
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
        SelectedDefaultActionPlan = preset.Template.DefaultActionPlanId is { } planId
            ? ActionPlans.SingleOrDefault(item => item.Id == planId)
            : null;
        RefreshDefaultPlanVariables(id);
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

        var id = _session.Editor.CreateActionPlan(name);
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
        UpdateSelectedStepEffect(CreateEffect(SelectedSuccessEffectKind, success: true), updateSuccess: true);
    }

    public void SetSelectedStepFailureEffect()
    {
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
            CheckDirectionVariableInput = string.Empty;
            CheckTargetVariableInput = string.Empty;
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

    public void SetDefaultPlanVariable()
    {
        if (_session is null || SelectedPreset is null)
        {
            return;
        }

        var variableName = DefaultVariableNameInput.Trim();
        if (string.IsNullOrWhiteSpace(variableName))
        {
            StatusMessage = "Enter a variable name before setting a default variable.";
            return;
        }

        var presetId = SelectedPreset.Id;
        _session.Editor.SetDefaultPlanVariable(presetId, variableName, CreateDefaultVariableValue());
        RefreshFromSession();
        SelectEntityPreset(presetId);
        SelectedDefaultPlanVariable = DefaultPlanVariables.SingleOrDefault(item => item.Name == variableName);
        StatusMessage = $"Set default variable {variableName}.";
    }

    public void RemoveSelectedDefaultPlanVariable()
    {
        if (_session is null || SelectedPreset is null || SelectedDefaultPlanVariable is null)
        {
            return;
        }

        var presetId = SelectedPreset.Id;
        var variableName = SelectedDefaultPlanVariable.Name;
        try
        {
            _session.Editor.RemoveDefaultPlanVariable(presetId, variableName);
            RefreshFromSession();
            SelectEntityPreset(presetId);
            ClearDefaultVariableInputs();
            StatusMessage = $"Removed default variable {variableName}.";
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
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
        ActionPlanStepChecks.Clear();
        DefaultPlanVariables.Clear();
        CarriedEntities.Clear();
        InventoryGridCells.Clear();
        ValidationMessages.Clear();
        YamlDiffLines.Clear();
        SelectedCarriedEntity = null;
        SelectedDefaultPlanVariable = null;

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
            ActionPlans.Add(new ActionPlanListItem(plan.TemplateId, plan.Descriptor.Id.Value, plan.Descriptor.Steps.Count));
        }

        _selectedActionPlan = selectedActionPlanId is null
            ? null
            : ActionPlans.SingleOrDefault(item => item.Id == selectedActionPlanId.Value);
        OnPropertyChanged(nameof(SelectedActionPlan));
        RefreshActionPlanSteps();

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

    private void RefreshDefaultPlanVariables(EntityTemplateId templateId)
    {
        DefaultPlanVariables.Clear();
        SelectedDefaultPlanVariable = null;

        if (_session is null)
        {
            return;
        }

        foreach (var variable in _session.Editor.ListDefaultPlanVariables(templateId))
        {
            DefaultPlanVariables.Add(new DefaultPlanVariableListItem(
                variable.Name,
                variable.Value.Kind,
                variable.Value,
                FormatPlanValue(variable.Value)));
        }
    }

    private void RefreshActionPlanSteps()
    {
        ActionPlanSteps.Clear();
        ActionPlanStepChecks.Clear();
        SelectedActionPlanStep = null;
        SelectedActionPlanStepCheck = null;
        ActionPlanStepLabelInput = string.Empty;
        CheckDirectionVariableInput = string.Empty;

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

    private ActionPlanDescriptor GetSelectedActionPlanDescriptor() =>
        _session!.Editor.ListActionPlans().Single(item => item.TemplateId == SelectedActionPlan!.Id).Descriptor;

    private void SelectActionPlanStep(int index)
    {
        SelectedActionPlanStep = ActionPlanSteps.SingleOrDefault(item => item.Index == index);
    }

    private void SelectActionPlanStepCheck(int index)
    {
        SelectedActionPlanStepCheck = ActionPlanStepChecks.SingleOrDefault(item => item.Index == index);
    }

    private void RefreshSelectedStepChecks(int stepIndex)
    {
        ActionPlanStepChecks.Clear();
        SelectedActionPlanStepCheck = null;
        CheckDirectionVariableInput = string.Empty;
        CheckTargetVariableInput = string.Empty;

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
            PlanEffectKind.Move => PlanEffectDescriptor.Move(GetDirectionVariable(success)),
            PlanEffectKind.Wait => PlanEffectDescriptor.Wait(),
            PlanEffectKind.CallPlan => PlanEffectDescriptor.CallPlan(GetCallPlan(success)),
            _ => throw new InvalidOperationException($"Unsupported effect kind {kind}.")
        };

    private PlanCheckDescriptor CreateCheck() =>
        SelectedCheckKind switch
        {
            PlanCheckKind.CanMove => PlanCheckDescriptor.CanMove(NormalizeVariable(CheckDirectionVariableInput, "facing")),
            PlanCheckKind.BlockingEntity => PlanCheckDescriptor.BlockingEntity(
                NormalizeVariable(CheckDirectionVariableInput, "facing"),
                NormalizeVariable(CheckTargetVariableInput, "target")),
            _ => throw new InvalidOperationException($"Unsupported check kind {SelectedCheckKind}.")
        };

    private string GetDirectionVariable(bool success) =>
        NormalizeVariable(success ? SuccessDirectionVariableInput : FailureDirectionVariableInput, "facing");

    private ActionPlanId GetCallPlan(bool success)
    {
        var plan = success ? SelectedSuccessCallPlan : SelectedFailureCallPlan;
        return new ActionPlanId((plan ?? ActionPlans.FirstOrDefault())?.Id.Value ?? "wait");
    }

    private static string NormalizeVariable(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

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
            SuccessDirectionVariableInput = effect.DirectionVariable ?? string.Empty;
            SelectedSuccessCallPlan = effect.PlanId is { } planId ? ActionPlans.SingleOrDefault(item => item.Id.Value == planId.Value) : null;
        }
        else
        {
            SelectedFailureEffectKind = effect.Kind;
            FailureDirectionVariableInput = effect.DirectionVariable ?? string.Empty;
            SelectedFailureCallPlan = effect.PlanId is { } planId ? ActionPlans.SingleOrDefault(item => item.Id.Value == planId.Value) : null;
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
        SelectedWeight = 0;
        SelectedCarryingCapacity = 0;
        SelectedGlyph = string.Empty;
        SelectedColor = PresentationColor.Gray;
        SelectedDefaultActionPlan = null;
        DefaultPlanVariables.Clear();
        ClearDefaultVariableInputs();
        CarriedEntities.Clear();
        InventoryGridCells.Clear();
        SelectedCarriedEntity = null;
    }

    private PlanValueDescriptor CreateDefaultVariableValue() =>
        SelectedDefaultVariableKind switch
        {
            PlanValueKind.Direction => PlanValueDescriptor.Direction(SelectedDefaultVariableDirection),
            PlanValueKind.Entity => PlanValueDescriptor.Entity(new EntityId(DefaultVariableEntityIdInput.Trim())),
            PlanValueKind.Coord => PlanValueDescriptor.Coord(new GridCoord(DefaultVariableCoordX, DefaultVariableCoordY)),
            PlanValueKind.Int => PlanValueDescriptor.Int(DefaultVariableIntValue),
            _ => throw new InvalidOperationException($"Unsupported default variable kind {SelectedDefaultVariableKind}.")
        };

    private static string FormatPlanValue(PlanValueDescriptor value) =>
        value.Kind switch
        {
            PlanValueKind.Direction => value.DirectionValue?.ToString() ?? string.Empty,
            PlanValueKind.Entity => value.EntityValue?.Value ?? string.Empty,
            PlanValueKind.Coord => value.CoordValue is { } coord ? $"{coord.X},{coord.Y}" : string.Empty,
            PlanValueKind.Int => value.IntValue?.ToString() ?? string.Empty,
            _ => string.Empty
        };

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

        if (effect.Kind is PlanEffectKind.ReverseDirection or PlanEffectKind.SetVariable)
        {
            fields.Add($"consumesTurn={effect.ConsumesTurn}");
            fields.Add($"continuePlan={effect.ContinuePlan}");
        }

        return fields.Count == 0 ? effect.Kind.ToString() : $"{effect.Kind}({string.Join(", ", fields)})";
    }

    private void ClearDefaultVariableInputs()
    {
        SelectedDefaultPlanVariable = null;
        DefaultVariableNameInput = string.Empty;
        SelectedDefaultVariableKind = PlanValueKind.Direction;
        SelectedDefaultVariableDirection = Direction.West;
        DefaultVariableEntityIdInput = string.Empty;
        DefaultVariableCoordX = 0;
        DefaultVariableCoordY = 0;
        DefaultVariableIntValue = 0;
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

public sealed record ActionPlanListItem(
    ActionPlanTemplateId Id,
    string RuntimeId,
    int StepCount)
{
    public override string ToString() => $"{Id} ({StepCount} steps)";
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
    string Summary)
{
    public override string ToString() => $"{Index + 1}: {Summary}";
}

public sealed record DefaultPlanVariableListItem(
    string Name,
    PlanValueKind Kind,
    PlanValueDescriptor Value,
    string DisplayValue)
{
    public override string ToString() => $"{Name}: {Kind} = {DisplayValue}";
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
