using GameGameGame.Core;

namespace GameGameGame.Content;

internal sealed class ActionPlanEditorService(EditableContentDocument document, Action? onChanged = null)
{
    public ActionPlanTemplateId CreateActionPlan(string name)
    {
        var id = GenerateActionPlanTemplateId(name);
        document.ActionPlans[id.Value] = EditableContentDocument.ActionPlanDescriptorDto.From(
            new ActionPlanDescriptor(new ActionPlanId(id.Value), [new ActionPlanStepDescriptor("wait", [], PlanEffectDescriptor.Wait(), OnFailure: null)]));
        onChanged?.Invoke();
        return id;
    }

    public ActionPlanTemplateId CreatePassiveActionPlan(string name)
    {
        var id = GenerateActionPlanTemplateId(name);
        document.ActionPlans[id.Value] = EditableContentDocument.ActionPlanDescriptorDto.From(new ActionPlanDescriptor(new ActionPlanId(id.Value), []));
        onChanged?.Invoke();
        return id;
    }

    public ActionPlanTemplateId DuplicateActionPlan(ActionPlanTemplateId sourceId, string name)
    {
        var source = ListActionPlans().Single(plan => plan.TemplateId == sourceId).Descriptor;
        var duplicateId = GenerateActionPlanTemplateId(name);
        document.ActionPlans[duplicateId.Value] = EditableContentDocument.ActionPlanDescriptorDto.From(source with { Id = new ActionPlanId(duplicateId.Value) });
        onChanged?.Invoke();
        return duplicateId;
    }

    public ContentEditorOperationResult DeleteActionPlan(ActionPlanTemplateId id)
    {
        var references = ListActionPlanReferences(id);
        if (references.Count > 0)
        {
            return ContentEditorOperationResult.Failure(
                $"Cannot delete action plan {id}; it is referenced by {string.Join(", ", references.Select(reference => reference.ToString()))}.");
        }

        document.ActionPlans.Remove(id.Value);
        onChanged?.Invoke();
        return ContentEditorOperationResult.Success();
    }

    public IReadOnlyList<ActionPlanReference> ListActionPlanReferences(ActionPlanTemplateId id)
    {
        var references = document.EntityTemplates
            .Where(template => template.Value.DefaultActionPlanId == id.Value)
            .Select(template => new ActionPlanReference(new EntityTemplateId(template.Key), ActionPlanTemplateId: null, StepIndex: null))
            .ToList();

        foreach (var (planId, plan) in document.ActionPlans)
        {
            var steps = plan.Steps ?? [];
            if (plan.Primitive?.FallbackPlanId == id.Value)
            {
                references.Add(new ActionPlanReference(null, new ActionPlanTemplateId(planId), StepIndex: null));
            }

            for (var index = 0; index < steps.Count; index++)
            {
                var step = steps[index];
                if ((step.OnSuccess?.Kind == PlanEffectKind.CallPlan && step.OnSuccess.PlanId == id.Value)
                    || (step.OnFailure?.Kind == PlanEffectKind.CallPlan && step.OnFailure.PlanId == id.Value))
                {
                    references.Add(new ActionPlanReference(null, new ActionPlanTemplateId(planId), index));
                }
            }

            var behaviorSteps = plan.Behavior?.Steps ?? [];
            for (var index = 0; index < behaviorSteps.Count; index++)
            {
                if (behaviorSteps[index].PlanId == id.Value)
                {
                    references.Add(new ActionPlanReference(null, new ActionPlanTemplateId(planId), index));
                }
            }
        }

        return references;
    }

    public void SetActionPlanPrimitive(ActionPlanTemplateId planId, ActionPlanPrimitiveKind kind, ActionPlanId? fallbackPlanId = null)
    {
        var plan = GetActionPlanDto(planId);
        plan.Primitive = new EditableContentDocument.ActionPlanPrimitiveDescriptorDto { Kind = kind, FallbackPlanId = fallbackPlanId?.Value };
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

    public void SetActionPlanBehavior(ActionPlanTemplateId planId, IReadOnlyList<ActionPlanBehaviorStepDescriptor> steps)
    {
        var normalizedSteps = steps.Select(ApplyRequiredAuthoredOptionDefaults).ToList();
        foreach (var step in normalizedSteps)
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
        plan.Behavior = EditableContentDocument.ActionPlanBehaviorDescriptorDto.From(new ActionPlanBehaviorDescriptor(normalizedSteps));
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
        var step = ApplyRequiredAuthoredOptionDefaults(new ActionPlanBehaviorStepDescriptor(kind));
        steps.Add(EditableContentDocument.ActionPlanBehaviorStepDescriptorDto.From(step));
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
        if (targetSlot is not null)
        {
            steps[stepIndex].TargetLabel = null;
        }
        onChanged?.Invoke();
    }

    public void SetActionPlanBehaviorStepTargetLabel(ActionPlanTemplateId planId, int stepIndex, string? targetLabel)
    {
        if (targetLabel is { } label && string.IsNullOrWhiteSpace(label))
        {
            throw new InvalidOperationException($"Action plan {planId} action step {stepIndex} target label must not be blank.");
        }

        var steps = GetActionPlanBehaviorSteps(planId);
        _ = steps[stepIndex];
        steps[stepIndex].TargetLabel = targetLabel;
        if (targetLabel is not null)
        {
            steps[stepIndex].TargetSlot = null;
        }
        onChanged?.Invoke();
    }

    public void SetActionPlanBehaviorStepCounterpartyTargetSlot(ActionPlanTemplateId planId, int stepIndex, int? targetSlot)
    {
        if (targetSlot is <= 0)
        {
            throw new InvalidOperationException($"Action plan {planId} action step {stepIndex} counterparty target slot must be greater than zero.");
        }

        var steps = GetActionPlanBehaviorSteps(planId);
        var step = steps[stepIndex];
        if (step.Kind != ActionPlanBehaviorStepKind.Transfer && targetSlot is not null)
        {
            throw new InvalidOperationException($"Action plan {planId} action step {stepIndex} is {step.Kind}; only Transfer steps support counterparty target references.");
        }

        step.CounterpartyTargetSlot = targetSlot;
        if (targetSlot is not null)
        {
            step.CounterpartyTargetLabel = null;
        }

        onChanged?.Invoke();
    }

    public void SetActionPlanBehaviorStepCounterpartyTargetLabel(ActionPlanTemplateId planId, int stepIndex, string? targetLabel)
    {
        if (targetLabel is { } label && string.IsNullOrWhiteSpace(label))
        {
            throw new InvalidOperationException($"Action plan {planId} action step {stepIndex} counterparty target label must not be blank.");
        }

        var steps = GetActionPlanBehaviorSteps(planId);
        var step = steps[stepIndex];
        if (step.Kind != ActionPlanBehaviorStepKind.Transfer && targetLabel is not null)
        {
            throw new InvalidOperationException($"Action plan {planId} action step {stepIndex} is {step.Kind}; only Transfer steps support counterparty target references.");
        }

        step.CounterpartyTargetLabel = targetLabel;
        if (targetLabel is not null)
        {
            step.CounterpartyTargetSlot = null;
        }

        onChanged?.Invoke();
    }

    public void SetActionPlanBehaviorStepTargetSelf(ActionPlanTemplateId planId, int stepIndex, bool targetSelf)
    {
        var steps = GetActionPlanBehaviorSteps(planId);
        if (stepIndex < 0 || stepIndex >= steps.Count)
        {
            throw new InvalidOperationException($"Action plan {planId} has no behavior step {stepIndex}.");
        }

        steps[stepIndex].TargetSelf = targetSelf;
        if (targetSelf)
        {
            steps[stepIndex].TargetSlot = null;
            steps[stepIndex].TargetLabel = null;
        }

        onChanged?.Invoke();
    }

    public void SetActionPlanBehaviorStepPlanId(ActionPlanTemplateId planId, int stepIndex, ActionPlanId? referencedPlanId)
    {
        var steps = GetActionPlanBehaviorSteps(planId);
        _ = steps[stepIndex];
        steps[stepIndex].PlanId = referencedPlanId?.Value;
        onChanged?.Invoke();
    }

    public void SetActionPlanBehaviorStepDirectionMode(ActionPlanTemplateId planId, int stepIndex, ActionPlanMoveDirectionMode? directionMode)
    {
        var steps = GetActionPlanBehaviorSteps(planId);
        var step = steps[stepIndex];
        if (step.Kind is not (ActionPlanBehaviorStepKind.Move or ActionPlanBehaviorStepKind.Transfer) && directionMode is not null)
        {
            throw new InvalidOperationException($"Action plan {planId} action step {stepIndex} is {step.Kind}; only Move and Transfer steps support directionMode.");
        }

        step.DirectionMode = directionMode?.ToString();
        onChanged?.Invoke();
    }

    public void SetActionPlanBehaviorStepTransferDirection(ActionPlanTemplateId planId, int stepIndex, TransferDirection? transferDirection)
    {
        var steps = GetActionPlanBehaviorSteps(planId);
        var step = steps[stepIndex];
        if (step.Kind != ActionPlanBehaviorStepKind.Transfer && transferDirection is not null)
        {
            throw new InvalidOperationException($"Action plan {planId} action step {stepIndex} is {step.Kind}; only Transfer steps support transferDirection.");
        }

        step.TransferDirection = transferDirection?.ToString();
        onChanged?.Invoke();
    }

    public void SetActionPlanBehaviorStepTargetPathMode(ActionPlanTemplateId planId, int stepIndex, ActionPlanTargetPathMode? pathMode)
    {
        var steps = GetActionPlanBehaviorSteps(planId);
        var step = steps[stepIndex];
        if (step.Kind != ActionPlanBehaviorStepKind.TargetPathMove && pathMode is not null)
        {
            throw new InvalidOperationException($"Action plan {planId} action step {stepIndex} is {step.Kind}; only TargetPathMove steps support pathMode.");
        }

        step.PathMode = pathMode?.ToString();
        onChanged?.Invoke();
    }

    public void SetActionPlanBehaviorStepDesiredDistance(ActionPlanTemplateId planId, int stepIndex, int? desiredDistance)
    {
        if (desiredDistance is < 0)
        {
            throw new InvalidOperationException($"Action plan {planId} action step {stepIndex} desiredDistance must be non-negative; found {desiredDistance}.");
        }

        var steps = GetActionPlanBehaviorSteps(planId);
        var step = steps[stepIndex];
        if (step.Kind != ActionPlanBehaviorStepKind.TargetPathMove && desiredDistance is not null)
        {
            throw new InvalidOperationException($"Action plan {planId} action step {stepIndex} is {step.Kind}; only TargetPathMove steps support desiredDistance.");
        }

        step.DesiredDistance = desiredDistance;
        onChanged?.Invoke();
    }

    public void SetActionPlanBehaviorStepOrbitDirection(ActionPlanTemplateId planId, int stepIndex, ActionPlanOrbitDirection? orbitDirection)
    {
        var steps = GetActionPlanBehaviorSteps(planId);
        var step = steps[stepIndex];
        if (step.Kind != ActionPlanBehaviorStepKind.TargetPathMove && orbitDirection is not null)
        {
            throw new InvalidOperationException($"Action plan {planId} action step {stepIndex} is {step.Kind}; only TargetPathMove steps support orbitDirection.");
        }

        step.OrbitDirection = orbitDirection?.ToString();
        onChanged?.Invoke();
    }

    public void SetActionPlanBehaviorStepCosts(ActionPlanTemplateId planId, int stepIndex, IReadOnlyList<ActionStepCostDescriptor> costs)
    {
        foreach (var cost in costs)
        {
            if (cost.Quantity <= 0)
            {
                throw new InvalidOperationException($"Action plan {planId} action step {stepIndex} cost quantity must be greater than zero; found {cost.Quantity}.");
            }
        }

        var duplicate = costs
            .GroupBy(cost => cost.TemplateId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Action plan {planId} action step {stepIndex} has duplicate cost template {duplicate.Key}; combine duplicate costs into one entry with a summed quantity.");
        }

        var steps = GetActionPlanBehaviorSteps(planId);
        if (stepIndex < 0 || stepIndex >= steps.Count)
        {
            throw new InvalidOperationException($"Action plan {planId} has no behavior step {stepIndex}.");
        }

        steps[stepIndex].Costs = costs.Count == 0
            ? null
            : costs.Select(EditableContentDocument.ActionStepCostDescriptorDto.From).ToList();
        onChanged?.Invoke();
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

    private IReadOnlyList<ActionPlanEditorModel> ListActionPlans()
    {
        var registry = document.ToRegistry();
        return registry.ActionPlanDescriptors.OrderBy(entry => entry.Key.Value).Select(entry => new ActionPlanEditorModel(entry.Key, entry.Value)).ToList();
    }

    private EditableContentDocument.ActionPlanDescriptorDto GetActionPlanDto(ActionPlanTemplateId id) =>
        document.ActionPlans.TryGetValue(id.Value, out var plan)
            ? plan
            : throw new InvalidOperationException($"Action plan template {id} does not exist.");

    private List<EditableContentDocument.ActionPlanBehaviorStepDescriptorDto> GetActionPlanBehaviorSteps(ActionPlanTemplateId id)
    {
        var plan = GetActionPlanDto(id);
        plan.Primitive = null;
        plan.Steps = [];
        plan.Behavior ??= new EditableContentDocument.ActionPlanBehaviorDescriptorDto { Steps = [] };
        plan.Behavior.Steps ??= [];
        return plan.Behavior.Steps;
    }

    private void MaterializeBehaviorDefaultsForAssignedTemplates(ActionPlanTemplateId planId, EditableContentDocument.ActionPlanBehaviorDescriptorDto? behavior)
    {
        foreach (var template in document.EntityTemplates.Values.Where(template => template.DefaultActionPlanId == planId.Value))
        {
            MaterializeBehaviorDefaults(template, behavior);
        }
    }

    private static void MaterializeBehaviorDefaults(EditableContentDocument.EntityTemplateDto template, EditableContentDocument.ActionPlanBehaviorDescriptorDto? behavior)
    {
        if (behavior?.Steps is null || behavior.Steps.Count == 0)
        {
            return;
        }

        foreach (var step in behavior.Steps)
        {
            if (ActionStepCatalog.IsRetiredLegacyTargetingOrCoordinateMovementStep(step.Kind))
            {
                continue;
            }

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

    private static void EnsureStableAuthoringStep(ActionPlanBehaviorStepKind kind)
    {
        _ = ActionStepCatalog.Get(kind);
        if (!ActionStepCatalog.IsStableAuthoringStep(kind))
        {
            throw new InvalidOperationException($"Action step {kind} is legacy/advanced and is not available for canonical authoring.");
        }
    }

    private static ActionPlanBehaviorStepDescriptor ApplyRequiredAuthoredOptionDefaults(ActionPlanBehaviorStepDescriptor step) =>
        step.Kind switch
        {
            ActionPlanBehaviorStepKind.Move => step with
            {
                DirectionMode = step.DirectionMode ?? ActionPlanMoveDirectionMode.Forward
            },
            ActionPlanBehaviorStepKind.Transfer => step with
            {
                DirectionMode = step.DirectionMode ?? ActionPlanMoveDirectionMode.Forward,
                TransferDirection = step.TransferDirection ?? TransferDirection.TargetToActor
            },
            ActionPlanBehaviorStepKind.TargetPathMove => step with
            {
                PathMode = step.PathMode ?? ActionPlanTargetPathMode.SeekAdjacency
            },
            _ => step
        };

    private ActionPlanTemplateId GenerateActionPlanTemplateId(string name)
    {
        var baseId = ContentEditorIdHelpers.ToCamelCaseId(name);
        var candidate = baseId;
        var suffix = 2;
        while (document.ActionPlans.ContainsKey(candidate))
        {
            candidate = $"{baseId}{suffix}";
            suffix++;
        }
        return new ActionPlanTemplateId(candidate);
    }
}
