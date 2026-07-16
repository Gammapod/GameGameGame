using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed class ContentEditorService(EditableContentDocument document, Action? onChanged = null)
{
    public EditableContentDocument Document { get; } = document;

    public IReadOnlyList<EntityPresetEditorModel> ListEntityPresets()
    {
        var registry = Document.ToRegistry();

        return registry.EntityTemplates
            .OrderBy(entry => entry.Key.Value)
            .Select(entry => new EntityPresetEditorModel(
                entry.Key,
                entry.Value,
                registry.Presentations[entry.Key]))
            .ToList();
    }

    public ContentValidationResult Validate() => Document.ToRegistry().Validate();

    public void UpsertScenario(ScenarioDefinition scenario)
    {
        Document.UpsertScenario(scenario);
        onChanged?.Invoke();
    }

    public ScenarioDefinition GetScenario(string scenarioId) => Document.GetScenario(scenarioId);

    public EntityTemplateId CreateEntityPreset(string name)
        => new EntityTemplateEditorService(Document, onChanged).CreateEntityPreset(name);

    public EntityTemplateId DuplicateEntityPreset(EntityTemplateId sourceId, string name)
        => new EntityTemplateEditorService(Document, onChanged).DuplicateEntityPreset(sourceId, name);

    public IReadOnlyList<EntityTemplateReference> ListEntityTemplateReferences(EntityTemplateId id) =>
        Document.EntityTemplates
            .SelectMany(source => (source.Value.CarriedEntities ?? [])
                .Where(carried => carried.TemplateId == id.Value)
                .Select(carried => new EntityTemplateReference(
                    new EntityTemplateId(source.Key),
                    carried.EntityId is null ? null : new EntityId(carried.EntityId))))
            .ToList();

    public ContentEditorOperationResult DeleteEntityPreset(EntityTemplateId id)
        => new EntityTemplateEditorService(Document, onChanged).DeleteEntityPreset(id);

    public void SetDefaultActionPlan(EntityTemplateId templateId, ActionPlanTemplateId actionPlanId)
        => new EntityTemplateEditorService(Document, onChanged).SetDefaultActionPlan(templateId, actionPlanId);

    public void ClearDefaultActionPlan(EntityTemplateId templateId)
        => new EntityTemplateEditorService(Document, onChanged).ClearDefaultActionPlan(templateId);

    public EntityPresetEditorModel GetEntityPreset(EntityTemplateId id)
    {
        var registry = Document.ToRegistry();

        return new EntityPresetEditorModel(
            id,
            registry.EntityTemplates[id],
            registry.Presentations[id]);
    }

    public void UpdateEntityPreset(EntityTemplateId id, EntityTemplate template, EntityPresentation presentation)
        => new EntityTemplateEditorService(Document, onChanged).UpdateEntityPreset(id, template, presentation);

    public void PlaceCarriedEntity(EntityTemplateId parentTemplateId, EntityId entityId, EntityTemplateId templateId, GridCoord coord)
        => new CarriedEntityLayoutEditor(Document, onChanged).PlaceCarriedEntity(parentTemplateId, entityId, templateId, coord);

    public EntityId PlaceCarriedEntity(EntityTemplateId parentTemplateId, EntityTemplateId templateId)
        => new CarriedEntityLayoutEditor(Document, onChanged).PlaceCarriedEntity(parentTemplateId, templateId);

    public EntityId PlaceCarriedEntity(EntityTemplateId parentTemplateId, EntityTemplateId templateId, GridCoord coord)
        => new CarriedEntityLayoutEditor(Document, onChanged).PlaceCarriedEntity(parentTemplateId, templateId, coord);

    public IReadOnlyList<CarriedEntityEditorModel> ListCarriedEntities(EntityTemplateId parentTemplateId)
        => new CarriedEntityLayoutEditor(Document, onChanged).ListCarriedEntities(parentTemplateId);

    public GridCoord? FindFirstOpenInventoryCell(EntityTemplateId parentTemplateId)
        => new CarriedEntityLayoutEditor(Document, onChanged).FindFirstOpenInventoryCell(parentTemplateId);

    public ContentEditorOperationResult ValidateCarriedEntityPlacement(
        EntityTemplateId parentTemplateId,
        GridCoord coord,
        EntityId? movingEntityId = null)
        => new CarriedEntityLayoutEditor(Document, onChanged).ValidateCarriedEntityPlacement(parentTemplateId, coord, movingEntityId);

    public void MoveCarriedEntity(EntityTemplateId parentTemplateId, EntityId entityId, GridCoord coord)
        => new CarriedEntityLayoutEditor(Document, onChanged).MoveCarriedEntity(parentTemplateId, entityId, coord);

    public void RemoveCarriedEntity(EntityTemplateId parentTemplateId, EntityId entityId)
        => new CarriedEntityLayoutEditor(Document, onChanged).RemoveCarriedEntity(parentTemplateId, entityId);

    public void ReplaceCarriedEntityTemplate(EntityTemplateId parentTemplateId, EntityId entityId, EntityTemplateId templateId)
        => new CarriedEntityLayoutEditor(Document, onChanged).ReplaceCarriedEntityTemplate(parentTemplateId, entityId, templateId);

    public IReadOnlyList<ActionPlanEditorModel> ListActionPlans()
    {
        var registry = Document.ToRegistry();

        return registry.ActionPlanDescriptors
            .OrderBy(entry => entry.Key.Value)
            .Select(entry => new ActionPlanEditorModel(entry.Key, entry.Value))
            .ToList();
    }

    public IReadOnlyList<ActionStepDescriptor> ListActionSteps() =>
        ActionStepCatalog.Steps
            .Where(step => step.Tier == ActionStepAuthoringTier.Stable)
            .ToList();

    public ActionPlanPreview PreviewActionPlan(ActionPlanTemplateId planId, EntityTemplateId? entityTemplateId = null, bool includeYamlPreview = true)
        => new ActionPlanPreviewService(Document).Preview(planId, entityTemplateId, includeYamlPreview);

    public ActionPlanTemplateId CreateActionPlan(string name)
        => new ActionPlanEditorService(Document, onChanged).CreateActionPlan(name);

    public ActionPlanTemplateId CreatePassiveActionPlan(string name)
        => new ActionPlanEditorService(Document, onChanged).CreatePassiveActionPlan(name);

    public ActionPlanTemplateId DuplicateActionPlan(ActionPlanTemplateId sourceId, string name)
        => new ActionPlanEditorService(Document, onChanged).DuplicateActionPlan(sourceId, name);

    public IReadOnlyList<ActionPlanReference> ListActionPlanReferences(ActionPlanTemplateId id)
        => new ActionPlanEditorService(Document, onChanged).ListActionPlanReferences(id);

    public void SetActionPlanPrimitive(ActionPlanTemplateId planId, ActionPlanPrimitiveKind kind, ActionPlanId? fallbackPlanId = null)
        => new ActionPlanEditorService(Document, onChanged).SetActionPlanPrimitive(planId, kind, fallbackPlanId);

    public void ClearActionPlanPrimitive(ActionPlanTemplateId planId)
        => new ActionPlanEditorService(Document, onChanged).ClearActionPlanPrimitive(planId);

    public void SetActionPlanBehavior(ActionPlanTemplateId planId, IReadOnlyList<ActionPlanBehaviorStepKind> steps) =>
        SetActionPlanBehavior(
            planId,
            steps.Select(step => new ActionPlanBehaviorStepDescriptor(step)).ToList());

    public void SetActionPlanBehavior(ActionPlanTemplateId planId, IReadOnlyList<ActionPlanBehaviorStepDescriptor> steps)
        => new ActionPlanEditorService(Document, onChanged).SetActionPlanBehavior(planId, steps);

    public void ClearActionPlanBehavior(ActionPlanTemplateId planId)
        => new ActionPlanEditorService(Document, onChanged).ClearActionPlanBehavior(planId);

    public void AddActionPlanBehaviorStep(ActionPlanTemplateId planId, ActionPlanBehaviorStepKind kind)
        => new ActionPlanEditorService(Document, onChanged).AddActionPlanBehaviorStep(planId, kind);

    public void SetActionPlanBehaviorStepTargetSlot(ActionPlanTemplateId planId, int stepIndex, int? targetSlot)
        => new ActionPlanEditorService(Document, onChanged).SetActionPlanBehaviorStepTargetSlot(planId, stepIndex, targetSlot);

    public void SetActionPlanBehaviorStepTargetLabel(ActionPlanTemplateId planId, int stepIndex, string? targetLabel)
        => new ActionPlanEditorService(Document, onChanged).SetActionPlanBehaviorStepTargetLabel(planId, stepIndex, targetLabel);

    public void SetActionPlanBehaviorStepPlanId(ActionPlanTemplateId planId, int stepIndex, ActionPlanId? referencedPlanId)
        => new ActionPlanEditorService(Document, onChanged).SetActionPlanBehaviorStepPlanId(planId, stepIndex, referencedPlanId);

    public void SetActionPlanBehaviorStepDirectionMode(ActionPlanTemplateId planId, int stepIndex, ActionPlanMoveDirectionMode? directionMode)
        => new ActionPlanEditorService(Document, onChanged).SetActionPlanBehaviorStepDirectionMode(planId, stepIndex, directionMode);

    private static void EnsureStableAuthoringStep(ActionPlanBehaviorStepKind kind)
    {
        _ = ActionStepCatalog.Get(kind);
        if (!ActionStepCatalog.IsStableAuthoringStep(kind))
        {
            throw new InvalidOperationException($"Action step {kind} is legacy/advanced and is not available for canonical authoring.");
        }
    }

    public void MoveActionPlanBehaviorStep(ActionPlanTemplateId planId, int fromIndex, int toIndex)
        => new ActionPlanEditorService(Document, onChanged).MoveActionPlanBehaviorStep(planId, fromIndex, toIndex);

    public void RemoveActionPlanBehaviorStep(ActionPlanTemplateId planId, int index)
        => new ActionPlanEditorService(Document, onChanged).RemoveActionPlanBehaviorStep(planId, index);

    public PrimitiveActionPlanChain CreateMoveFacingPickupTargetChain(string moveFacingPlanName, string pickupTargetPlanName)
    {
        var pickupTargetPlanId = CreateActionPlan(pickupTargetPlanName);
        SetActionPlanPrimitive(pickupTargetPlanId, ActionPlanPrimitiveKind.PickupTarget);

        var moveFacingPlanId = CreateActionPlan(moveFacingPlanName);
        SetActionPlanPrimitive(
            moveFacingPlanId,
            ActionPlanPrimitiveKind.MoveFacing,
            new ActionPlanId(pickupTargetPlanId.Value));

        return new PrimitiveActionPlanChain(moveFacingPlanId, pickupTargetPlanId);
    }

    public ActionPlanTemplateId CreateMoveFacingPickupTargetBehavior(string behaviorPlanName)
    {
        var planId = CreateActionPlan(behaviorPlanName);
        SetActionPlanBehavior(
            planId,
            [ActionPlanBehaviorStepKind.MoveFacing, ActionPlanBehaviorStepKind.PickupTarget]);

        return planId;
    }

    public ContentEditorOperationResult DeleteActionPlan(ActionPlanTemplateId id)
        => new ActionPlanEditorService(Document, onChanged).DeleteActionPlan(id);

    public void AddActionPlanStep(ActionPlanTemplateId planId, ActionPlanStepDescriptor step)
    {
        var plan = GetActionPlanDto(planId);
        plan.Steps ??= [];
        plan.Steps.Add(EditableContentDocument.ActionPlanStepDescriptorDto.From(step));
        onChanged?.Invoke();
    }

    public void UpdateActionPlanStep(ActionPlanTemplateId planId, int index, ActionPlanStepDescriptor step)
    {
        var steps = GetActionPlanSteps(planId);
        steps[index] = EditableContentDocument.ActionPlanStepDescriptorDto.From(step);
        onChanged?.Invoke();
    }

    public void MoveActionPlanStep(ActionPlanTemplateId planId, int fromIndex, int toIndex)
    {
        var steps = GetActionPlanSteps(planId);
        var step = steps[fromIndex];
        steps.RemoveAt(fromIndex);
        steps.Insert(toIndex, step);
        onChanged?.Invoke();
    }

    public void RemoveActionPlanStep(ActionPlanTemplateId planId, int index)
    {
        GetActionPlanSteps(planId).RemoveAt(index);
        onChanged?.Invoke();
    }

    public void AddActionPlanCheck(ActionPlanTemplateId planId, int stepIndex, PlanCheckKind kind)
    {
        var steps = GetActionPlanSteps(planId);
        var step = steps[stepIndex].ToDescriptor();
        var checks = step.Checks.ToList();
        checks.Add(PlanPrimitiveCatalog.CreateDefaultCheck(kind));
        steps[stepIndex] = EditableContentDocument.ActionPlanStepDescriptorDto.From(step with { Checks = checks });
        onChanged?.Invoke();
    }

    public void UpdateActionPlanCheck(ActionPlanTemplateId planId, int stepIndex, int checkIndex, PlanCheckKind kind)
    {
        var steps = GetActionPlanSteps(planId);
        var step = steps[stepIndex].ToDescriptor();
        var checks = step.Checks.ToList();
        checks[checkIndex] = PlanPrimitiveCatalog.CreateDefaultCheck(kind);
        steps[stepIndex] = EditableContentDocument.ActionPlanStepDescriptorDto.From(step with { Checks = checks });
        onChanged?.Invoke();
    }

    public void SetActionPlanStepSuccessEffect(ActionPlanTemplateId planId, int stepIndex, PlanEffectKind kind) =>
        SetActionPlanStepEffect(planId, stepIndex, kind, updateSuccess: true);

    public void SetActionPlanStepFailureEffect(ActionPlanTemplateId planId, int stepIndex, PlanEffectKind kind) =>
        SetActionPlanStepEffect(planId, stepIndex, kind, updateSuccess: false);

    public void SetActionPlanStepSuccessEffect(ActionPlanTemplateId planId, int stepIndex, PlanEffectDescriptor effect) =>
        SetActionPlanStepEffect(planId, stepIndex, effect, updateSuccess: true);

    public void SetActionPlanStepFailureEffect(ActionPlanTemplateId planId, int stepIndex, PlanEffectDescriptor effect) =>
        SetActionPlanStepEffect(planId, stepIndex, effect, updateSuccess: false);

    public void SetActionPlanStepSuccessEffectMovementTarget(ActionPlanTemplateId planId, int stepIndex, MovementTargetDescriptor target) =>
        UpdateActionPlanStepEffect(planId, stepIndex, updateSuccess: true, effect => effect with { MovementTarget = target });

    public void SetActionPlanStepFailureEffectMovementTarget(ActionPlanTemplateId planId, int stepIndex, MovementTargetDescriptor target) =>
        UpdateActionPlanStepEffect(planId, stepIndex, updateSuccess: false, effect => effect with { MovementTarget = target });

    public void SetActionPlanStepSuccessEffectMovementDestination(ActionPlanTemplateId planId, int stepIndex, MovementDestinationDescriptor destination) =>
        UpdateActionPlanStepEffect(planId, stepIndex, updateSuccess: true, effect => effect with { MovementDestination = destination });

    public void SetActionPlanStepFailureEffectMovementDestination(ActionPlanTemplateId planId, int stepIndex, MovementDestinationDescriptor destination) =>
        UpdateActionPlanStepEffect(planId, stepIndex, updateSuccess: false, effect => effect with { MovementDestination = destination });

    private void SetActionPlanStepEffect(ActionPlanTemplateId planId, int stepIndex, PlanEffectKind kind, bool updateSuccess)
    {
        var steps = GetActionPlanSteps(planId);
        var step = steps[stepIndex].ToDescriptor();
        var effect = PlanPrimitiveCatalog.CreateDefaultEffect(kind);
        steps[stepIndex] = EditableContentDocument.ActionPlanStepDescriptorDto.From(updateSuccess
            ? step with { OnSuccess = effect }
            : step with { OnFailure = effect });
        onChanged?.Invoke();
    }

    private void SetActionPlanStepEffect(ActionPlanTemplateId planId, int stepIndex, PlanEffectDescriptor effect, bool updateSuccess)
    {
        var steps = GetActionPlanSteps(planId);
        var step = steps[stepIndex].ToDescriptor();
        steps[stepIndex] = EditableContentDocument.ActionPlanStepDescriptorDto.From(updateSuccess
            ? step with { OnSuccess = effect }
            : step with { OnFailure = effect });
        onChanged?.Invoke();
    }

    private void UpdateActionPlanStepEffect(
        ActionPlanTemplateId planId,
        int stepIndex,
        bool updateSuccess,
        Func<PlanEffectDescriptor, PlanEffectDescriptor> update)
    {
        var steps = GetActionPlanSteps(planId);
        var step = steps[stepIndex].ToDescriptor();
        var effect = updateSuccess ? step.OnSuccess : step.OnFailure;
        if (effect is null)
        {
            throw new InvalidOperationException($"Action plan {planId} step {stepIndex} has no {(updateSuccess ? "success" : "failure")} effect.");
        }

        var updated = update(effect);
        steps[stepIndex] = EditableContentDocument.ActionPlanStepDescriptorDto.From(updateSuccess
            ? step with { OnSuccess = updated }
            : step with { OnFailure = updated });
        onChanged?.Invoke();
    }

    public void SetDefaultPlanVariable(EntityTemplateId templateId, string variableName, PlanValueDescriptor value)
        => new EntityTemplateEditorService(Document, onChanged).SetDefaultPlanVariable(templateId, variableName, value);

    public IReadOnlyList<DefaultPlanVariableEditorModel> ListDefaultPlanVariables(EntityTemplateId templateId)
        => new EntityTemplateEditorService(Document, onChanged).ListDefaultPlanVariables(templateId);

    public void RemoveDefaultPlanVariable(EntityTemplateId templateId, string variableName)
        => new EntityTemplateEditorService(Document, onChanged).RemoveDefaultPlanVariable(templateId, variableName);

    public ActorActionStateDefaults GetActionStateDefaults(EntityTemplateId templateId)
        => new EntityTemplateEditorService(Document, onChanged).GetActionStateDefaults(templateId);

    public void SetInitialFacing(EntityTemplateId templateId, Direction facing)
        => new EntityTemplateEditorService(Document, onChanged).SetInitialFacing(templateId, facing);

    public void ClearInitialFacing(EntityTemplateId templateId)
        => new EntityTemplateEditorService(Document, onChanged).ClearInitialFacing(templateId);

    public IReadOnlyList<EntityTargetingRule> ListTargetingRules(EntityTemplateId templateId)
        => new EntityTemplateEditorService(Document, onChanged).ListTargetingRules(templateId);

    public void SetTargetingRule(EntityTemplateId templateId, EntityTargetingRule rule)
        => new EntityTemplateEditorService(Document, onChanged).SetTargetingRule(templateId, rule);

    public void RemoveTargetingRule(EntityTemplateId templateId, int slot)
        => new EntityTemplateEditorService(Document, onChanged).RemoveTargetingRule(templateId, slot);

    private EditableContentDocument.EntityTemplateDto GetTemplateDto(EntityTemplateId id) =>
        Document.EntityTemplates.TryGetValue(id.Value, out var template)
            ? template
            : throw new InvalidOperationException($"Entity template {id} does not exist.");

    private EditableContentDocument.ActionPlanDescriptorDto GetActionPlanDto(ActionPlanTemplateId id) =>
        Document.ActionPlans.TryGetValue(id.Value, out var plan)
            ? plan
            : throw new InvalidOperationException($"Action plan template {id} does not exist.");

    private List<EditableContentDocument.ActionPlanStepDescriptorDto> GetActionPlanSteps(ActionPlanTemplateId id)
    {
        var plan = GetActionPlanDto(id);
        plan.Primitive = null;
        plan.Behavior = null;
        plan.Steps ??= [];

        return plan.Steps;
    }

    public static string FormatActionPlanShape(ActionPlanShape shape) =>
        ActionPlanPreviewService.FormatActionPlanShape(shape);
}

public sealed record EntityPresetEditorModel(
    EntityTemplateId Id,
    EntityTemplate Template,
    EntityPresentation Presentation);

public sealed record ActionPlanEditorModel(
    ActionPlanTemplateId TemplateId,
    ActionPlanDescriptor Descriptor);

public sealed record ActionPlanReference(
    EntityTemplateId? EntityTemplateId,
    ActionPlanTemplateId? ActionPlanTemplateId,
    int? StepIndex);

public sealed record PrimitiveActionPlanChain(
    ActionPlanTemplateId MoveFacingPlanId,
    ActionPlanTemplateId PickupTargetPlanId);

public sealed record ActionPlanPreview(
    ActionPlanTemplateId PlanId,
    EntityTemplateId? EntityTemplateId,
    string Shape,
    IReadOnlyList<string> Guidance,
    IReadOnlyList<ActionPlanPreviewStep> ActionSteps,
    IReadOnlyList<string> StateHints,
    IReadOnlyList<string> ValidationDiagnostics,
    string YamlPreview);

public sealed record ActionPlanPreviewStep(
    ActionPlanBehaviorStepKind Kind,
    string DisplayName,
    string Hint,
    IReadOnlyList<PlanPrimitiveSlotDescriptor> RequiredState,
    IReadOnlyList<PlanPrimitiveSlotDescriptor> DefaultableState,
    IReadOnlyList<PlanPrimitiveSlotDescriptor> StateWrites,
    int? TargetSlot = null,
    string? TargetLabel = null,
    ActionPlanId? PlanId = null,
    ActionPlanMoveDirectionMode? DirectionMode = null);

public sealed record EntityTemplateReference(EntityTemplateId SourceTemplateId, EntityId? CarriedEntityId);

public sealed record DefaultPlanVariableEditorModel(string Name, PlanValueDescriptor Value);

public sealed record CarriedEntityEditorModel(
    EntityId EntityId,
    EntityTemplateId TemplateId,
    GridCoord Coord,
    EntityTemplate Template,
    EntityPresentation Presentation);

public sealed record ContentEditorOperationResult(string? ErrorMessage)
{
    public bool IsSuccess => ErrorMessage is null;

    public static ContentEditorOperationResult Success() => new(ErrorMessage: null);

    public static ContentEditorOperationResult Failure(string errorMessage) => new(errorMessage);
}
