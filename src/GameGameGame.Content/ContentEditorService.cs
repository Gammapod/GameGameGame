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
    {
        var id = Document.AddEntityTemplate(
            name,
            new EntityTemplate(
                name,
                InventoryWidth: 0,
                InventoryHeight: 0,
                Bulk: 0,
                Aperture: 0),
            new EntityPresentation('?', PresentationColor.Gray));
        onChanged?.Invoke();

        return id;
    }

    public EntityTemplateId DuplicateEntityPreset(EntityTemplateId sourceId, string name)
    {
        var preset = GetEntityPreset(sourceId);
        var duplicateId = Document.AddEntityTemplate(
            name,
            preset.Template with
            {
                Name = name,
                CarriedEntities = DuplicateCarriedEntities(preset.Template.CarriedEntities, name)
            },
            preset.Presentation);
        onChanged?.Invoke();

        return duplicateId;
    }

    public IReadOnlyList<EntityTemplateReference> ListEntityTemplateReferences(EntityTemplateId id) =>
        Document.EntityTemplates
            .SelectMany(source => (source.Value.CarriedEntities ?? [])
                .Where(carried => carried.TemplateId == id.Value)
                .Select(carried => new EntityTemplateReference(
                    new EntityTemplateId(source.Key),
                    carried.EntityId is null ? null : new EntityId(carried.EntityId))))
            .ToList();

    public ContentEditorOperationResult DeleteEntityPreset(EntityTemplateId id)
    {
        var references = ListEntityTemplateReferences(id);

        if (references.Count > 0)
        {
            return ContentEditorOperationResult.Failure(
                $"Cannot delete entity template {id}; it is referenced by {string.Join(", ", references.Select(reference => reference.ToString()))}.");
        }

        Document.EntityTemplates.Remove(id.Value);
        Document.Presentations.Remove(id.Value);
        onChanged?.Invoke();

        return ContentEditorOperationResult.Success();
    }

    public void SetDefaultActionPlan(EntityTemplateId templateId, ActionPlanTemplateId actionPlanId)
    {
        var template = GetTemplateDto(templateId);
        template.DefaultActionPlanId = actionPlanId.Value;
        MaterializeBehaviorDefaults(template, GetActionPlanDto(actionPlanId).Behavior);
        onChanged?.Invoke();
    }

    public void ClearDefaultActionPlan(EntityTemplateId templateId)
    {
        GetTemplateDto(templateId).DefaultActionPlanId = null;
        onChanged?.Invoke();
    }

    public EntityPresetEditorModel GetEntityPreset(EntityTemplateId id)
    {
        var registry = Document.ToRegistry();

        return new EntityPresetEditorModel(
            id,
            registry.EntityTemplates[id],
            registry.Presentations[id]);
    }

    public void UpdateEntityPreset(EntityTemplateId id, EntityTemplate template, EntityPresentation presentation)
    {
        Document.EntityTemplates[id.Value] = EditableContentDocument.EntityTemplateDto.From(template);
        Document.Presentations[id.Value] = EditableContentDocument.EntityPresentationDto.From(presentation);
        onChanged?.Invoke();
    }

    public void PlaceCarriedEntity(EntityTemplateId parentTemplateId, EntityId entityId, EntityTemplateId templateId, GridCoord coord)
    {
        var placement = ValidateCarriedEntityPlacement(parentTemplateId, coord);
        if (!placement.IsSuccess)
        {
            throw new InvalidOperationException(placement.ErrorMessage);
        }

        var template = GetTemplateDto(parentTemplateId);
        template.CarriedEntities ??= [];
        template.CarriedEntities.Add(new EditableContentDocument.CarriedEntityTemplateDto
        {
            EntityId = entityId.Value,
            TemplateId = templateId.Value,
            Coord = EditableContentDocument.GridCoordDto.From(coord)
        });
        onChanged?.Invoke();
    }

    public EntityId PlaceCarriedEntity(EntityTemplateId parentTemplateId, EntityTemplateId templateId)
    {
        var coord = FindFirstOpenInventoryCell(parentTemplateId)
            ?? throw new InvalidOperationException($"Entity template {parentTemplateId} has no open inventory cell.");

        return PlaceCarriedEntity(parentTemplateId, templateId, coord);
    }

    public EntityId PlaceCarriedEntity(EntityTemplateId parentTemplateId, EntityTemplateId templateId, GridCoord coord)
    {
        var entityId = GenerateCarriedEntityId(parentTemplateId, templateId);

        PlaceCarriedEntity(parentTemplateId, entityId, templateId, coord);

        return entityId;
    }

    public IReadOnlyList<CarriedEntityEditorModel> ListCarriedEntities(EntityTemplateId parentTemplateId)
    {
        var registry = Document.ToRegistry();
        var parent = registry.EntityTemplates[parentTemplateId];

        return (parent.CarriedEntities ?? [])
            .Where(carried => carried.TemplateId is not null)
            .Select(carried =>
            {
                var templateId = carried.TemplateId!.Value;
                return new CarriedEntityEditorModel(
                    carried.EntityId,
                    templateId,
                    carried.Coord,
                    registry.EntityTemplates[templateId],
                    registry.Presentations[templateId]);
            })
            .ToList();
    }

    public GridCoord? FindFirstOpenInventoryCell(EntityTemplateId parentTemplateId)
    {
        var template = GetTemplateDto(parentTemplateId);
        var occupied = (template.CarriedEntities ?? [])
            .Where(carried => carried.Coord is not null)
            .Select(carried => new GridCoord(carried.Coord!.X, carried.Coord.Y))
            .ToHashSet();

        for (var y = 0; y < template.InventoryHeight; y++)
        {
            for (var x = 0; x < template.InventoryWidth; x++)
            {
                var coord = new GridCoord(x, y);
                if (!occupied.Contains(coord))
                {
                    return coord;
                }
            }
        }

        return null;
    }

    public ContentEditorOperationResult ValidateCarriedEntityPlacement(
        EntityTemplateId parentTemplateId,
        GridCoord coord,
        EntityId? movingEntityId = null)
    {
        var template = GetTemplateDto(parentTemplateId);
        if (template.InventoryWidth <= 0 || template.InventoryHeight <= 0)
        {
            return ContentEditorOperationResult.Failure(
                $"Cannot place carried entity; {parentTemplateId} has no usable inventory.");
        }

        if (coord.X < 0 || coord.Y < 0 || coord.X >= template.InventoryWidth || coord.Y >= template.InventoryHeight)
        {
            return ContentEditorOperationResult.Failure(
                $"Cannot place carried entity at {coord.X},{coord.Y}; it is outside inventory bounds {template.InventoryWidth}x{template.InventoryHeight} for {parentTemplateId}.");
        }

        var carriedEntities = template.CarriedEntities ?? [];
        if (movingEntityId is not null && carriedEntities.All(carried => carried.EntityId != movingEntityId.Value.Value))
        {
            return ContentEditorOperationResult.Failure(
                $"Entity template {parentTemplateId} does not carry entity {movingEntityId.Value}.");
        }

        var occupant = carriedEntities.FirstOrDefault(carried =>
            carried.Coord is not null
            && carried.Coord.X == coord.X
            && carried.Coord.Y == coord.Y
            && (movingEntityId is null || carried.EntityId != movingEntityId.Value.Value));
        if (occupant is not null)
        {
            return ContentEditorOperationResult.Failure(
                $"Cannot place carried entity at {coord.X},{coord.Y}; cell is already occupied by {occupant.EntityId}.");
        }

        return ContentEditorOperationResult.Success();
    }

    public void MoveCarriedEntity(EntityTemplateId parentTemplateId, EntityId entityId, GridCoord coord)
    {
        var template = GetTemplateDto(parentTemplateId);
        var carried = template.CarriedEntities?.SingleOrDefault(carried => carried.EntityId == entityId.Value)
            ?? throw new InvalidOperationException($"Entity template {parentTemplateId} does not carry entity {entityId}.");
        var placement = ValidateCarriedEntityPlacement(parentTemplateId, coord, entityId);
        if (!placement.IsSuccess)
        {
            throw new InvalidOperationException(placement.ErrorMessage);
        }

        carried.Coord = EditableContentDocument.GridCoordDto.From(coord);
        onChanged?.Invoke();
    }

    public void RemoveCarriedEntity(EntityTemplateId parentTemplateId, EntityId entityId)
    {
        var template = GetTemplateDto(parentTemplateId);
        var carried = template.CarriedEntities?.SingleOrDefault(carried => carried.EntityId == entityId.Value)
            ?? throw new InvalidOperationException($"Entity template {parentTemplateId} does not carry entity {entityId}.");

        template.CarriedEntities!.Remove(carried);
        if (template.CarriedEntities.Count == 0)
        {
            template.CarriedEntities = null;
        }

        onChanged?.Invoke();
    }

    public void ReplaceCarriedEntityTemplate(EntityTemplateId parentTemplateId, EntityId entityId, EntityTemplateId templateId)
    {
        var template = GetTemplateDto(parentTemplateId);
        var carried = template.CarriedEntities?.SingleOrDefault(carried => carried.EntityId == entityId.Value)
            ?? throw new InvalidOperationException($"Entity template {parentTemplateId} does not carry entity {entityId}.");

        carried.TemplateId = templateId.Value;
        onChanged?.Invoke();
    }

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

    public ActionPlanPreview PreviewActionPlan(ActionPlanTemplateId planId, EntityTemplateId? entityTemplateId = null)
    {
        var plan = ListActionPlans().Single(item => item.TemplateId == planId).Descriptor;
        var validation = Validate();
        var canonicalValidation = Document.ValidateCanonicalAuthoring();
        var diagnostics = validation.ForActionPlan(planId)
            .Concat(canonicalValidation.ForActionPlan(planId))
            .Concat(entityTemplateId is { } templateId
                ? validation.ForEntityTemplate(templateId).Concat(canonicalValidation.ForEntityTemplate(templateId))
                : [])
            .Select(diagnostic => diagnostic.Message)
            .Distinct()
            .ToList();

        return new ActionPlanPreview(
            planId,
            entityTemplateId,
            FormatActionPlanShape(ActionPlanShapeClassifier.Classify(plan)),
            GetActionPlanGuidance(plan),
            GetActionPlanPreviewSteps(plan),
            GetActionPlanStateHints(plan, entityTemplateId),
            diagnostics,
            Document.SaveYaml());
    }

    public ActionPlanTemplateId CreateActionPlan(string name)
    {
        var id = GenerateActionPlanTemplateId(name);
        Document.ActionPlans[id.Value] = EditableContentDocument.ActionPlanDescriptorDto.From(
            new ActionPlanDescriptor(
                new ActionPlanId(id.Value),
                [new ActionPlanStepDescriptor("wait", [], PlanEffectDescriptor.Wait(), OnFailure: null)]));
        onChanged?.Invoke();

        return id;
    }

    public ActionPlanTemplateId CreatePassiveActionPlan(string name)
    {
        var id = GenerateActionPlanTemplateId(name);
        Document.ActionPlans[id.Value] = EditableContentDocument.ActionPlanDescriptorDto.From(
            new ActionPlanDescriptor(new ActionPlanId(id.Value), []));
        onChanged?.Invoke();

        return id;
    }

    public ActionPlanTemplateId DuplicateActionPlan(ActionPlanTemplateId sourceId, string name)
    {
        var source = ListActionPlans().Single(plan => plan.TemplateId == sourceId).Descriptor;
        var duplicateId = GenerateActionPlanTemplateId(name);
        Document.ActionPlans[duplicateId.Value] = EditableContentDocument.ActionPlanDescriptorDto.From(
            source with { Id = new ActionPlanId(duplicateId.Value) });
        onChanged?.Invoke();

        return duplicateId;
    }

    public IReadOnlyList<ActionPlanReference> ListActionPlanReferences(ActionPlanTemplateId id)
    {
        var references = Document.EntityTemplates
            .Where(template => template.Value.DefaultActionPlanId == id.Value)
            .Select(template => new ActionPlanReference(
                EntityTemplateId: new EntityTemplateId(template.Key),
                ActionPlanTemplateId: null,
                StepIndex: null))
            .ToList();

        foreach (var (planId, plan) in Document.ActionPlans)
        {
            var steps = plan.Steps ?? [];
            if (plan.Primitive?.FallbackPlanId == id.Value)
            {
                references.Add(new ActionPlanReference(
                    EntityTemplateId: null,
                    ActionPlanTemplateId: new ActionPlanTemplateId(planId),
                    StepIndex: null));
            }

            for (var index = 0; index < steps.Count; index++)
            {
                var step = steps[index];
                if ((step.OnSuccess?.Kind == PlanEffectKind.CallPlan && step.OnSuccess.PlanId == id.Value)
                    || (step.OnFailure?.Kind == PlanEffectKind.CallPlan && step.OnFailure.PlanId == id.Value))
                {
                    references.Add(new ActionPlanReference(
                        EntityTemplateId: null,
                        ActionPlanTemplateId: new ActionPlanTemplateId(planId),
                        StepIndex: index));
                }
            }

            var behaviorSteps = plan.Behavior?.Steps ?? [];
            for (var index = 0; index < behaviorSteps.Count; index++)
            {
                if (behaviorSteps[index].PlanId == id.Value)
                {
                    references.Add(new ActionPlanReference(
                        EntityTemplateId: null,
                        ActionPlanTemplateId: new ActionPlanTemplateId(planId),
                        StepIndex: index));
                }
            }
        }

        return references;
    }

    public void SetActionPlanPrimitive(ActionPlanTemplateId planId, ActionPlanPrimitiveKind kind, ActionPlanId? fallbackPlanId = null)
    {
        var plan = GetActionPlanDto(planId);
        plan.Primitive = new EditableContentDocument.ActionPlanPrimitiveDescriptorDto
        {
            Kind = kind,
            FallbackPlanId = fallbackPlanId?.Value
        };
        plan.Behavior = null;
        plan.Steps = [];
        onChanged?.Invoke();
    }

    public void ClearActionPlanPrimitive(ActionPlanTemplateId planId)
    {
        var plan = GetActionPlanDto(planId);
        plan.Primitive = null;
        plan.Steps ??= [];
        onChanged?.Invoke();
    }

    public void SetActionPlanBehavior(ActionPlanTemplateId planId, IReadOnlyList<ActionPlanBehaviorStepKind> steps) =>
        SetActionPlanBehavior(
            planId,
            steps.Select(step => new ActionPlanBehaviorStepDescriptor(step)).ToList());

    public void SetActionPlanBehavior(ActionPlanTemplateId planId, IReadOnlyList<ActionPlanBehaviorStepDescriptor> steps)
    {
        foreach (var step in steps)
        {
            EnsureStableAuthoringStep(step.Kind);
        }

        var plan = GetActionPlanDto(planId);
        if (steps.Count == 0)
        {
            ClearActionPlanBehavior(planId);
            return;
        }

        plan.Primitive = null;
        plan.Steps = [];
        plan.Behavior = EditableContentDocument.ActionPlanBehaviorDescriptorDto.From(new ActionPlanBehaviorDescriptor(steps));
        MaterializeBehaviorDefaultsForAssignedTemplates(planId, plan.Behavior);
        onChanged?.Invoke();
    }

    public void ClearActionPlanBehavior(ActionPlanTemplateId planId)
    {
        var plan = GetActionPlanDto(planId);
        plan.Behavior = null;
        plan.Steps ??= [];
        onChanged?.Invoke();
    }

    public void AddActionPlanBehaviorStep(ActionPlanTemplateId planId, ActionPlanBehaviorStepKind kind)
    {
        EnsureStableAuthoringStep(kind);
        var steps = GetActionPlanBehaviorSteps(planId);
        steps.Add(new EditableContentDocument.ActionPlanBehaviorStepDescriptorDto { Kind = kind });
        MaterializeBehaviorDefaultsForAssignedTemplates(planId, GetActionPlanDto(planId).Behavior);
        onChanged?.Invoke();
    }

    public void SetActionPlanBehaviorStepTargetSlot(ActionPlanTemplateId planId, int stepIndex, int? targetSlot)
    {
        if (targetSlot is <= 0)
        {
            throw new InvalidOperationException($"Action plan {planId} action step {stepIndex} target slot must be greater than zero.");
        }

        var steps = GetActionPlanBehaviorSteps(planId);
        _ = steps[stepIndex];
        steps[stepIndex].TargetSlot = targetSlot;
        onChanged?.Invoke();
    }

    public void SetActionPlanBehaviorStepPlanId(ActionPlanTemplateId planId, int stepIndex, ActionPlanId? referencedPlanId)
    {
        var steps = GetActionPlanBehaviorSteps(planId);
        _ = steps[stepIndex];
        steps[stepIndex].PlanId = referencedPlanId?.Value;
        onChanged?.Invoke();
    }

    private static void EnsureStableAuthoringStep(ActionPlanBehaviorStepKind kind)
    {
        _ = ActionStepCatalog.Get(kind);
        if (!ActionStepCatalog.IsStableAuthoringStep(kind))
        {
            throw new InvalidOperationException($"Action step {kind} is legacy/advanced and is not available for canonical authoring.");
        }
    }

    public void MoveActionPlanBehaviorStep(ActionPlanTemplateId planId, int fromIndex, int toIndex)
    {
        var steps = GetActionPlanBehaviorSteps(planId);
        var step = steps[fromIndex];
        steps.RemoveAt(fromIndex);
        steps.Insert(toIndex, step);
        onChanged?.Invoke();
    }

    public void RemoveActionPlanBehaviorStep(ActionPlanTemplateId planId, int index)
    {
        var steps = GetActionPlanBehaviorSteps(planId);
        steps.RemoveAt(index);
        if (steps.Count == 0)
        {
            GetActionPlanDto(planId).Behavior = null;
        }

        onChanged?.Invoke();
    }

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
    {
        var references = ListActionPlanReferences(id);
        if (references.Count > 0)
        {
            return ContentEditorOperationResult.Failure(
                $"Cannot delete action plan {id}; it is referenced by {string.Join(", ", references.Select(reference => reference.ToString()))}.");
        }

        Document.ActionPlans.Remove(id.Value);
        onChanged?.Invoke();

        return ContentEditorOperationResult.Success();
    }

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
    {
        var template = GetTemplateDto(templateId);
        template.DefaultPlanVariables ??= [];
        template.DefaultPlanVariables[variableName] = EditableContentDocument.PlanValueDescriptorDto.From(value);
        onChanged?.Invoke();
    }

    public IReadOnlyList<DefaultPlanVariableEditorModel> ListDefaultPlanVariables(EntityTemplateId templateId)
    {
        var template = GetEntityPreset(templateId).Template;
        return (template.DefaultPlanVariables ?? new Dictionary<string, PlanValueDescriptor>())
            .OrderBy(entry => entry.Key)
            .Select(entry => new DefaultPlanVariableEditorModel(entry.Key, entry.Value))
            .ToList();
    }

    public void RemoveDefaultPlanVariable(EntityTemplateId templateId, string variableName)
    {
        var template = GetTemplateDto(templateId);
        if (template.DefaultPlanVariables is null || !template.DefaultPlanVariables.Remove(variableName))
        {
            throw new InvalidOperationException($"Entity template {templateId} has no default variable {variableName}.");
        }

        if (template.DefaultPlanVariables.Count == 0)
        {
            template.DefaultPlanVariables = null;
        }

        onChanged?.Invoke();
    }

    public ActorActionStateDefaults GetActionStateDefaults(EntityTemplateId templateId)
    {
        var template = GetTemplateDto(templateId);

        return new ActorActionStateDefaults(
            template.ActionStateDefaults?.Facing,
            string.IsNullOrWhiteSpace(template.ActionStateDefaults?.Target) ? null : new EntityId(template.ActionStateDefaults.Target));
    }

    public void SetInitialFacing(EntityTemplateId templateId, Direction facing)
    {
        var template = GetTemplateDto(templateId);
        template.ActionStateDefaults ??= new EditableContentDocument.ActorActionStateDefaultsDto();
        template.ActionStateDefaults.Facing = facing;
        onChanged?.Invoke();
    }

    public void ClearInitialFacing(EntityTemplateId templateId)
    {
        var template = GetTemplateDto(templateId);
        if (template.ActionStateDefaults is null)
        {
            return;
        }

        template.ActionStateDefaults.Facing = null;
        if (template.ActionStateDefaults.Target is null)
        {
            template.ActionStateDefaults = null;
        }

        onChanged?.Invoke();
    }

    public IReadOnlyList<EntityTargetingRule> ListTargetingRules(EntityTemplateId templateId)
    {
        var template = GetEntityPreset(templateId).Template;
        return (template.TargetingRules ?? [])
            .OrderBy(rule => rule.Slot)
            .ToList();
    }

    public void SetTargetingRule(EntityTemplateId templateId, EntityTargetingRule rule)
    {
        if (rule.Slot <= 0)
        {
            throw new InvalidOperationException($"Entity template {templateId} targeting rule slot must be greater than zero.");
        }

        if (rule.Range < 0)
        {
            throw new InvalidOperationException($"Entity template {templateId} targeting rule slot {rule.Slot} range must be zero or greater.");
        }

        var template = GetTemplateDto(templateId);
        template.TargetingRules ??= [];
        template.TargetingRules.RemoveAll(existing => existing.Slot == rule.Slot);
        template.TargetingRules.Add(EditableContentDocument.EntityTargetingRuleDto.From(rule));
        template.TargetingRules = template.TargetingRules.OrderBy(existing => existing.Slot).ToList();
        onChanged?.Invoke();
    }

    public void RemoveTargetingRule(EntityTemplateId templateId, int slot)
    {
        var template = GetTemplateDto(templateId);
        if (template.TargetingRules is null || template.TargetingRules.RemoveAll(rule => rule.Slot == slot) == 0)
        {
            throw new InvalidOperationException($"Entity template {templateId} has no targeting rule slot {slot}.");
        }

        if (template.TargetingRules.Count == 0)
        {
            template.TargetingRules = null;
        }

        onChanged?.Invoke();
    }

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

    private List<EditableContentDocument.ActionPlanBehaviorStepDescriptorDto> GetActionPlanBehaviorSteps(ActionPlanTemplateId id)
    {
        var plan = GetActionPlanDto(id);
        plan.Primitive = null;
        plan.Steps = [];
        plan.Behavior ??= new EditableContentDocument.ActionPlanBehaviorDescriptorDto { Steps = [] };
        plan.Behavior.Steps ??= [];

        return plan.Behavior.Steps;
    }

    private void MaterializeBehaviorDefaultsForAssignedTemplates(
        ActionPlanTemplateId planId,
        EditableContentDocument.ActionPlanBehaviorDescriptorDto? behavior)
    {
        foreach (var template in Document.EntityTemplates.Values.Where(template => template.DefaultActionPlanId == planId.Value))
        {
            MaterializeBehaviorDefaults(template, behavior);
        }
    }

    private static void MaterializeBehaviorDefaults(
        EditableContentDocument.EntityTemplateDto template,
        EditableContentDocument.ActionPlanBehaviorDescriptorDto? behavior)
    {
        if (behavior?.Steps is null || behavior.Steps.Count == 0)
        {
            return;
        }

        foreach (var step in behavior.Steps)
        {
            var metadata = ActionStepCatalog.Get(step.Kind);
            foreach (var defaultable in metadata.DefaultableState)
            {
                if (defaultable.Slot == ActionPlanSlot.Facing && defaultable.ValueKind == PlanValueKind.Direction)
                {
                    template.ActionStateDefaults ??= new EditableContentDocument.ActorActionStateDefaultsDto();
                    template.ActionStateDefaults.Facing ??= Direction.West;
                }
            }
        }
    }

    private static IReadOnlyList<string> GetActionPlanGuidance(ActionPlanDescriptor descriptor) =>
        ActionPlanShapeClassifier.Classify(descriptor) switch
        {
            ActionPlanShape.CanonicalBehaviorChain => ["Preferred canonical behavior chain. Author normal behavior as ordered engine-defined Action Steps."],
            ActionPlanShape.TransitionalPrimitivePlan => ["Compatibility primitive-backed fallback plan. Prefer canonical behavior chains for new authoring."],
            ActionPlanShape.LegacyLowLevelSteps => ["Legacy/advanced low-level steps/checks/effects. Keep only where canonical Action Steps cannot express the behavior yet."],
            ActionPlanShape.EmptyPassive => ["Passive plan with no current behavior. Add canonical Action Steps when the entity should act."],
            _ => ["Unknown action-plan shape. Validate before saving."]
        };

    public static string FormatActionPlanShape(ActionPlanShape shape) =>
        shape switch
        {
            ActionPlanShape.CanonicalBehaviorChain => "Canonical Behavior Chain",
            ActionPlanShape.TransitionalPrimitivePlan => "Transitional Primitive Plan",
            ActionPlanShape.LegacyLowLevelSteps => "Legacy / Advanced Low-Level Steps",
            ActionPlanShape.EmptyPassive => "Empty / Passive",
            ActionPlanShape.InvalidMixedShape => "Invalid Mixed Shape",
            ActionPlanShape.InvalidEmptyBehaviorChain => "Invalid Empty Behavior Chain",
            _ => "Unknown"
        };

    private static IReadOnlyList<ActionPlanPreviewStep> GetActionPlanPreviewSteps(ActionPlanDescriptor descriptor)
    {
        if (descriptor.Behavior?.Steps.Count > 0)
        {
            return descriptor.Behavior.Steps
                .Select(step =>
                {
                    var metadata = ActionStepCatalog.Get(step.Kind);
                    return new ActionPlanPreviewStep(
                        step.Kind,
                        metadata.DisplayName,
                        metadata.Description,
                        metadata.RequiredState,
                        metadata.DefaultableState,
                        metadata.StateWrites,
                        step.TargetSlot,
                        step.PlanId);
                })
                .ToList();
        }

        return [];
    }

    private IReadOnlyList<string> GetActionPlanStateHints(ActionPlanDescriptor descriptor, EntityTemplateId? entityTemplateId)
    {
        if (descriptor.Behavior?.Steps.Count is not > 0)
        {
            return [];
        }

        ActorActionStateDefaults? defaults = entityTemplateId is { } templateId
            ? GetActionStateDefaults(templateId)
            : null;
        var hints = new List<string>();

        foreach (var step in descriptor.Behavior.Steps)
        {
            var metadata = ActionStepCatalog.Get(step.Kind);
            foreach (var state in metadata.DefaultableState)
            {
                var hint = FormatPreviewStateHint(state, defaults);
                if (!hints.Contains(hint))
                {
                    hints.Add(hint);
                }
            }
        }

        return hints;
    }

    private static string FormatPreviewStateHint(PlanPrimitiveSlotDescriptor state, ActorActionStateDefaults? defaults) =>
        state.Slot switch
        {
            ActionPlanSlot.Facing => defaults?.Facing is { } facing
                ? $"Facing={facing}"
                : "Facing=West (defaultable)",
            ActionPlanSlot.Target => defaults?.Target is { } target
                ? $"Target={target}"
                : "Target=Self (defaultable)",
            _ => $"{state.Slot} ({state.ValueKind})"
        };

    private static IReadOnlyList<CarriedEntityTemplate>? DuplicateCarriedEntities(
        IReadOnlyList<CarriedEntityTemplate>? carriedEntities,
        string duplicateName)
    {
        if (carriedEntities is null || carriedEntities.Count == 0)
        {
            return null;
        }

        var idPrefix = ToCamelCaseId(duplicateName);
        return carriedEntities
            .Select(carried => new CarriedEntityTemplate(
                new EntityId($"{idPrefix}{UppercaseFirst(carried.EntityId.Value)}"),
                carried.TemplateId ?? throw new InvalidOperationException($"Carried entity {carried.EntityId} has no template ID."),
                carried.Coord))
            .ToList();
    }

    private static string ToCamelCaseId(string name)
    {
        var result = string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select((part, index) => index == 0
                ? char.ToLowerInvariant(part[0]) + part[1..]
                : char.ToUpperInvariant(part[0]) + part[1..]));

        return string.IsNullOrWhiteSpace(result) ? "entity" : result;
    }

    private static string UppercaseFirst(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToUpperInvariant(value[0]) + value[1..];

    private EntityId GenerateCarriedEntityId(EntityTemplateId parentTemplateId, EntityTemplateId templateId)
    {
        var parentPrefix = ToCamelCaseId(GetTemplateDto(parentTemplateId).Name ?? parentTemplateId.Value);
        var templateName = Document.EntityTemplates.TryGetValue(templateId.Value, out var template)
            ? template.Name ?? templateId.Value
            : templateId.Value;
        var baseId = $"{parentPrefix}{UppercaseFirst(ToCamelCaseId(templateName))}";
        var candidate = baseId;
        var suffix = 2;
        var existingIds = (GetTemplateDto(parentTemplateId).CarriedEntities ?? [])
            .Select(carried => carried.EntityId)
            .ToHashSet();

        while (existingIds.Contains(candidate))
        {
            candidate = $"{baseId}{suffix}";
            suffix++;
        }

        return new EntityId(candidate);
    }

    private ActionPlanTemplateId GenerateActionPlanTemplateId(string name)
    {
        var baseId = ToCamelCaseId(name);
        var candidate = baseId;
        var suffix = 2;
        while (Document.ActionPlans.ContainsKey(candidate))
        {
            candidate = $"{baseId}{suffix}";
            suffix++;
        }

        return new ActionPlanTemplateId(candidate);
    }
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
    ActionPlanId? PlanId = null);

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
