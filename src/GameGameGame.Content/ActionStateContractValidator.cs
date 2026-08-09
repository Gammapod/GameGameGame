using GameGameGame.Core;

namespace GameGameGame.Content;

internal static class ActionStateContractValidator
{
    public static void ValidateTemplatePlanSlots(
        List<ContentDiagnostic> diagnostics,
        EntityTemplateId templateId,
        EntityTemplate template,
        ActionPlanTemplateId actionPlanTemplateId,
        ActionPlanDescriptor plan,
        IReadOnlyDictionary<ActionPlanId, ActionPlanDescriptor> plansById)
    {
        ValidatePlanSlots(
            diagnostics,
            $"Entity template {templateId} ({template.Name}) action plan {plan.Id}",
            templateId,
            actionPlanTemplateId,
            plan,
            GetInitialActionSlots(template),
            plansById,
            []);
    }

    private static Dictionary<ActionPlanSlot, PlanValueKind> GetInitialActionSlots(EntityTemplate template)
    {
        var slots = new Dictionary<ActionPlanSlot, PlanValueKind>();

        if (template.ActionStateDefaults?.Facing is not null)
        {
            slots[ActionPlanSlot.Facing] = PlanValueKind.Direction;
        }

        if (template.ActionStateDefaults?.Target is not null)
        {
            slots[ActionPlanSlot.Target] = PlanValueKind.Entity;
        }

        if (template.TargetingRules is { Count: > 0 })
        {
            slots[ActionPlanSlot.Target] = PlanValueKind.Entity;
        }

        if (template.DefaultPlanVariables is not null)
        {
            foreach (var (name, value) in template.DefaultPlanVariables)
            {
                if (string.Equals(name, "facing", StringComparison.Ordinal) && value.Kind == PlanValueKind.Direction)
                {
                    slots[ActionPlanSlot.Facing] = PlanValueKind.Direction;
                }

                if (string.Equals(name, "target", StringComparison.Ordinal) && value.Kind == PlanValueKind.Entity)
                {
                    slots[ActionPlanSlot.Target] = PlanValueKind.Entity;
                }
            }
        }

        return slots;
    }

    private static void ValidatePlanSlots(
        List<ContentDiagnostic> diagnostics,
        string subject,
        EntityTemplateId? entityTemplateId,
        ActionPlanTemplateId? actionPlanTemplateId,
        ActionPlanDescriptor plan,
        Dictionary<ActionPlanSlot, PlanValueKind> slots,
        IReadOnlyDictionary<ActionPlanId, ActionPlanDescriptor> plansById,
        HashSet<ActionPlanId> callStack)
    {
        if (!callStack.Add(plan.Id))
        {
            return;
        }

        if (plan.Primitive is { } primitive)
        {
            ApplyDefaultableState(GetPrimitiveSlotDefaultable(primitive.Kind), slots);
            ValidatePrimitiveSlotReads(diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, primitive, GetPrimitiveSlotReads(primitive.Kind), slots);
            ApplySlotWrites(GetPrimitiveSlotWrites(primitive.Kind), slots);

            if (primitive.FallbackPlanId is { } fallbackPlanId
                && plansById.TryGetValue(fallbackPlanId, out var fallbackPlan))
            {
                ValidatePlanSlots(diagnostics, subject, entityTemplateId, actionPlanTemplateId, fallbackPlan, slots, plansById, callStack);
            }
        }

        if (plan.Behavior is { } behavior)
        {
            for (var index = 0; index < behavior.Steps.Count; index++)
            {
                var step = behavior.Steps[index];
                if (ActionStepCatalog.IsRetiredLegacyTargetingOrCoordinateMovementStep(step.Kind))
                {
                    continue;
                }

                var metadata = ActionStepCatalog.Get(step.Kind);
                ApplyDefaultableState(metadata.DefaultableState, slots);
                ValidateBehaviorStepSlotReads(diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, step, index, metadata.RequiredState, slots);
                ApplySlotWrites(metadata.StateWrites, slots);
            }
        }

        foreach (var step in plan.Steps)
        {
            foreach (var check in step.Checks)
            {
                ValidateSlotReads(diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, step, PlanPrimitiveCatalog.GetCheck(check.Kind).SlotReads, slots);
                ApplySlotWrites(PlanPrimitiveCatalog.GetCheck(check.Kind).SlotWrites, slots);
            }

            ValidateEffectSlots(diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, step, step.OnSuccess, slots, plansById, callStack);
            ValidateEffectSlots(diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, step, step.OnFailure, slots, plansById, callStack);
        }

        callStack.Remove(plan.Id);
    }

    private static IReadOnlyList<PlanPrimitiveSlotDescriptor> GetPrimitiveSlotReads(ActionPlanPrimitiveKind kind) =>
        kind switch
        {
            ActionPlanPrimitiveKind.MoveFacing => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.Backstep => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.PickupTarget => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Target, PlanValueKind.Entity)],
            ActionPlanPrimitiveKind.TurnLeft => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.TurnRight => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.ReverseFacing => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            _ => []
        };

    private static IReadOnlyList<PlanPrimitiveSlotDescriptor> GetPrimitiveSlotWrites(ActionPlanPrimitiveKind kind) =>
        kind switch
        {
            ActionPlanPrimitiveKind.MoveFacing => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Target, PlanValueKind.Entity)],
            ActionPlanPrimitiveKind.Backstep => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Target, PlanValueKind.Entity)],
            ActionPlanPrimitiveKind.TurnLeft => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.TurnRight => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.ReverseFacing => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            _ => []
        };

    private static IReadOnlyList<PlanPrimitiveSlotDescriptor> GetPrimitiveSlotDefaultable(ActionPlanPrimitiveKind kind) =>
        kind switch
        {
            ActionPlanPrimitiveKind.MoveFacing => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.Backstep => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.PickupTarget => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Target, PlanValueKind.Entity)],
            ActionPlanPrimitiveKind.TurnLeft => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.TurnRight => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.ReverseFacing => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            _ => []
        };

    private static void ValidatePrimitiveSlotReads(
        List<ContentDiagnostic> diagnostics,
        string subject,
        EntityTemplateId? entityTemplateId,
        ActionPlanTemplateId? actionPlanTemplateId,
        ActionPlanDescriptor plan,
        ActionPlanPrimitiveDescriptor primitive,
        IReadOnlyList<PlanPrimitiveSlotDescriptor> reads,
        Dictionary<ActionPlanSlot, PlanValueKind> slots)
    {
        foreach (var read in reads)
        {
            if (!slots.TryGetValue(read.Slot, out var actualKind))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingPlanSlot,
                    $"{subject} primitive {primitive.Kind} reads missing required slot {read.Slot}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    actionPlanSlot: read.Slot,
                    expectedValueKind: read.ValueKind));
                continue;
            }

            if (actualKind != read.ValueKind)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.PlanVariableTypeMismatch,
                    $"{subject} primitive {primitive.Kind} slot {read.Slot} expected {read.ValueKind} but found {actualKind}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    actionPlanSlot: read.Slot,
                    expectedValueKind: read.ValueKind,
                    actualValueKind: actualKind));
            }
        }
    }

    private static void ValidateBehaviorStepSlotReads(
        List<ContentDiagnostic> diagnostics,
        string subject,
        EntityTemplateId? entityTemplateId,
        ActionPlanTemplateId? actionPlanTemplateId,
        ActionPlanDescriptor plan,
        ActionPlanBehaviorStepDescriptor step,
        int stepIndex,
        IReadOnlyList<PlanPrimitiveSlotDescriptor> reads,
        Dictionary<ActionPlanSlot, PlanValueKind> slots)
    {
        foreach (var read in reads)
        {
            if (!slots.TryGetValue(read.Slot, out var actualKind))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingPlanSlot,
                    $"{subject} action step {step.Kind} reads missing required slot {read.Slot}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    stepIndex: stepIndex,
                    actionPlanSlot: read.Slot,
                    expectedValueKind: read.ValueKind));
                continue;
            }

            if (actualKind != read.ValueKind)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.PlanVariableTypeMismatch,
                    $"{subject} action step {step.Kind} slot {read.Slot} expected {read.ValueKind} but found {actualKind}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    stepIndex: stepIndex,
                    actionPlanSlot: read.Slot,
                    expectedValueKind: read.ValueKind,
                    actualValueKind: actualKind));
            }
        }
    }

    private static void ValidateEffectSlots(
        List<ContentDiagnostic> diagnostics,
        string subject,
        EntityTemplateId? entityTemplateId,
        ActionPlanTemplateId? actionPlanTemplateId,
        ActionPlanDescriptor plan,
        ActionPlanStepDescriptor step,
        PlanEffectDescriptor? effect,
        Dictionary<ActionPlanSlot, PlanValueKind> slots,
        IReadOnlyDictionary<ActionPlanId, ActionPlanDescriptor> plansById,
        HashSet<ActionPlanId> callStack)
    {
        if (effect is null)
        {
            return;
        }

        var fields = PlanPrimitiveCatalog.GetEffect(effect.Kind);
        ValidateSlotReads(diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, step, fields.SlotReads, slots);
        ApplySlotWrites(fields.SlotWrites, slots);

        if (effect.Kind == PlanEffectKind.CallPlan
            && effect.PlanId is { } planId
            && plansById.TryGetValue(planId, out var calledPlan))
        {
            ValidatePlanSlots(diagnostics, subject, entityTemplateId, actionPlanTemplateId, calledPlan, slots, plansById, callStack);
        }
    }

    private static void ValidateSlotReads(
        List<ContentDiagnostic> diagnostics,
        string subject,
        EntityTemplateId? entityTemplateId,
        ActionPlanTemplateId? actionPlanTemplateId,
        ActionPlanDescriptor plan,
        ActionPlanStepDescriptor step,
        IReadOnlyList<PlanPrimitiveSlotDescriptor> reads,
        Dictionary<ActionPlanSlot, PlanValueKind> slots)
    {
        foreach (var read in reads)
        {
            if (!slots.TryGetValue(read.Slot, out var actualKind))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingPlanSlot,
                    $"{subject} step {step.Label} reads missing required slot {read.Slot}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    stepIndex: StepIndex(plan, step),
                    actionPlanSlot: read.Slot,
                    expectedValueKind: read.ValueKind));
                continue;
            }

            if (actualKind != read.ValueKind)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.PlanVariableTypeMismatch,
                    $"{subject} step {step.Label} slot {read.Slot} expected {read.ValueKind} but found {actualKind}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    stepIndex: StepIndex(plan, step),
                    actionPlanSlot: read.Slot,
                    expectedValueKind: read.ValueKind,
                    actualValueKind: actualKind));
            }
        }
    }

    private static void ApplySlotWrites(
        IReadOnlyList<PlanPrimitiveSlotDescriptor> writes,
        Dictionary<ActionPlanSlot, PlanValueKind> slots)
    {
        foreach (var write in writes)
        {
            slots[write.Slot] = write.ValueKind;
        }
    }

    private static void ApplyDefaultableState(
        IReadOnlyList<PlanPrimitiveSlotDescriptor> defaults,
        Dictionary<ActionPlanSlot, PlanValueKind> slots)
    {
        foreach (var defaultable in defaults)
        {
            slots.TryAdd(defaultable.Slot, defaultable.ValueKind);
        }
    }

    private static int StepIndex(ActionPlanDescriptor plan, ActionPlanStepDescriptor step)
    {
        for (var index = 0; index < plan.Steps.Count; index++)
        {
            if (ReferenceEquals(plan.Steps[index], step) || plan.Steps[index] == step)
            {
                return index;
            }
        }

        return -1;
    }

    private static void AddDiagnostic(List<ContentDiagnostic> diagnostics, ContentDiagnostic diagnostic)
    {
        if (!diagnostics.Contains(diagnostic))
        {
            diagnostics.Add(diagnostic);
        }
    }
}
