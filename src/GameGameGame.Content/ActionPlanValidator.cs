using GameGameGame.Core;

namespace GameGameGame.Content;

internal static class ActionPlanValidator
{
    public static void Validate(
        IReadOnlyDictionary<ActionPlanTemplateId, ActionPlanDescriptor> actionPlanTemplates,
        List<string> errors,
        List<ContentDiagnostic> diagnostics)
    {
        var planIds = actionPlanTemplates.Values.Select(plan => plan.Id).ToHashSet();

        foreach (var (templateId, descriptor) in actionPlanTemplates)
        {
            TryValidate(errors, $"Action plan template {templateId} ({descriptor.Id})", () => descriptor.Materialize());

            ValidateActionPlanShape(diagnostics, templateId, descriptor);
            ValidateBehaviorTargetSlots(diagnostics, templateId, descriptor);
            ValidateBehaviorPlanReferences(diagnostics, templateId, descriptor, planIds);
            ValidatePrimitiveFallback(diagnostics, templateId, descriptor, planIds);

            foreach (var step in descriptor.Steps)
            {
                ValidateCalledPlan(diagnostics, templateId, descriptor, step, step.OnSuccess, planIds);
                ValidateCalledPlan(diagnostics, templateId, descriptor, step, step.OnFailure, planIds);
                ValidateMovementEffectDescriptor(diagnostics, templateId, descriptor, step, step.OnSuccess);
                ValidateMovementEffectDescriptor(diagnostics, templateId, descriptor, step, step.OnFailure);
            }
        }
    }

    private static void ValidateCalledPlan(
        List<ContentDiagnostic> diagnostics,
        ActionPlanTemplateId actionPlanTemplateId,
        ActionPlanDescriptor descriptor,
        ActionPlanStepDescriptor step,
        PlanEffectDescriptor? effect,
        HashSet<ActionPlanId> planIds)
    {
        if (effect?.Kind == PlanEffectKind.CallPlan && effect.PlanId is { } planId && !planIds.Contains(planId))
        {
            AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                ContentDiagnosticCode.MissingCalledPlan,
                $"Action plan {descriptor.Id} step {step.Label} calls missing plan {planId}.",
                actionPlanTemplateId: actionPlanTemplateId,
                actionPlanId: descriptor.Id,
                referencedActionPlanId: planId,
                stepIndex: StepIndex(descriptor, step)));
        }
    }

    private static void ValidateActionPlanShape(
        List<ContentDiagnostic> diagnostics,
        ActionPlanTemplateId actionPlanTemplateId,
        ActionPlanDescriptor descriptor)
    {
        var shape = ActionPlanShapeClassifier.Classify(descriptor);
        if (shape == ActionPlanShape.InvalidMixedShape)
        {
            AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                ContentDiagnosticCode.InvalidActionPlanShape,
                $"Action plan {descriptor.Id} declares multiple behavior shapes. Use only one of behavior, primitive, or low-level steps.",
                actionPlanTemplateId: actionPlanTemplateId,
                actionPlanId: descriptor.Id));
        }

        if (shape == ActionPlanShape.InvalidEmptyBehaviorChain)
        {
            AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                ContentDiagnosticCode.InvalidActionPlanShape,
                $"Action plan {descriptor.Id} declares an empty behavior chain. Omit behavior or add at least one Action Step.",
                actionPlanTemplateId: actionPlanTemplateId,
                actionPlanId: descriptor.Id));
        }
    }

    private static void ValidateBehaviorTargetSlots(
        List<ContentDiagnostic> diagnostics,
        ActionPlanTemplateId actionPlanTemplateId,
        ActionPlanDescriptor descriptor)
    {
        if (descriptor.Behavior is not { } behavior)
        {
            return;
        }

        for (var index = 0; index < behavior.Steps.Count; index++)
        {
            var step = behavior.Steps[index];
            if (step.TargetSlot is <= 0)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidActionStepTargetSlot,
                    $"Action plan {descriptor.Id} action step {step.Kind} targetSlot must be greater than zero; found {step.TargetSlot}.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    stepIndex: index));
            }

            if (step.TargetSlot is not null && !string.IsNullOrWhiteSpace(step.TargetLabel))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidActionStepTargetReference,
                    $"Action plan {descriptor.Id} action step {step.Kind} must use either targetLabel or targetSlot, not both.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    stepIndex: index));
            }

            if (step.TargetLabel is { } label && string.IsNullOrWhiteSpace(label))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidActionStepTargetReference,
                    $"Action plan {descriptor.Id} action step {step.Kind} targetLabel must not be blank.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    stepIndex: index));
            }
        }
    }

    private static void ValidateBehaviorPlanReferences(
        List<ContentDiagnostic> diagnostics,
        ActionPlanTemplateId actionPlanTemplateId,
        ActionPlanDescriptor descriptor,
        HashSet<ActionPlanId> planIds)
    {
        if (descriptor.Behavior is not { } behavior)
        {
            return;
        }

        for (var index = 0; index < behavior.Steps.Count; index++)
        {
            var step = behavior.Steps[index];
            if (!IsApplyPlanOverrideStep(step.Kind))
            {
                continue;
            }

            if (step.PlanId is not { } planId)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingActionPlanReference,
                    $"Action plan {descriptor.Id} action step {step.Kind} requires planId.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    stepIndex: index));
                continue;
            }

            if (!planIds.Contains(planId))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingActionPlanReference,
                    $"Action plan {descriptor.Id} action step {step.Kind} references missing plan {planId}.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    referencedActionPlanId: planId,
                    stepIndex: index));
            }
        }
    }

    private static bool IsApplyPlanOverrideStep(ActionPlanBehaviorStepKind kind) =>
        kind is ActionPlanBehaviorStepKind.ApplyPrePlan
            or ActionPlanBehaviorStepKind.ApplyMainPlan
            or ActionPlanBehaviorStepKind.ApplyPostPlan;

    private static void ValidatePrimitiveFallback(
        List<ContentDiagnostic> diagnostics,
        ActionPlanTemplateId actionPlanTemplateId,
        ActionPlanDescriptor descriptor,
        HashSet<ActionPlanId> planIds)
    {
        if (descriptor.Primitive?.FallbackPlanId is { } fallbackPlanId && !planIds.Contains(fallbackPlanId))
        {
            AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                ContentDiagnosticCode.MissingCalledPlan,
                $"Action plan {descriptor.Id} primitive {descriptor.Primitive.Kind} falls back to missing plan {fallbackPlanId}.",
                actionPlanTemplateId: actionPlanTemplateId,
                actionPlanId: descriptor.Id,
                referencedActionPlanId: fallbackPlanId));
        }
    }

    private static void ValidateMovementEffectDescriptor(
        List<ContentDiagnostic> diagnostics,
        ActionPlanTemplateId actionPlanTemplateId,
        ActionPlanDescriptor plan,
        ActionPlanStepDescriptor step,
        PlanEffectDescriptor? effect)
    {
        if (effect?.Kind is not (PlanEffectKind.Teleport or PlanEffectKind.Drop))
        {
            return;
        }

        if (effect.MovementTarget is null)
        {
            AddInvalidMovementDiagnostic(diagnostics, actionPlanTemplateId, plan, step, effect, "movementTarget is required.");
        }
        else if (GetMovementTargetError(effect.MovementTarget) is { } targetError)
        {
            AddInvalidMovementDiagnostic(diagnostics, actionPlanTemplateId, plan, step, effect, targetError);
        }

        if (effect.MovementDestination is null)
        {
            AddInvalidMovementDiagnostic(diagnostics, actionPlanTemplateId, plan, step, effect, "movementDestination is required.");
        }
        else if (GetMovementDestinationError(effect.MovementDestination) is { } destinationError)
        {
            AddInvalidMovementDiagnostic(diagnostics, actionPlanTemplateId, plan, step, effect, destinationError);
        }
    }

    private static string? GetMovementTargetError(MovementTargetDescriptor target) =>
        target.Kind switch
        {
            MovementTargetKind.Entity when target.EntityId is null => "movementTarget.entityId is required for Entity targets.",
            MovementTargetKind.CarriedInventoryCoord when target.InventoryCoord is null => "movementTarget.inventoryCoord is required for CarriedInventoryCoord targets.",
            _ => null
        };

    private static string? GetMovementDestinationError(MovementDestinationDescriptor destination) =>
        destination.Kind switch
        {
            MovementDestinationKind.PlaneCoord when destination.PlaneCoord is null => "movementDestination.planeCoord is required for PlaneCoord destinations.",
            MovementDestinationKind.InventorySlot when destination.OwnerId is null => "movementDestination.ownerId is required for InventorySlot destinations.",
            MovementDestinationKind.InventorySlot when destination.InventoryCoord is null => "movementDestination.inventoryCoord is required for InventorySlot destinations.",
            MovementDestinationKind.AdjacentToSelf when destination.Direction is null => "movementDestination.direction is required for AdjacentToSelf destinations.",
            MovementDestinationKind.AdjacentToEntity when destination.AnchorEntityId is null => "movementDestination.anchorEntityId is required for AdjacentToEntity destinations.",
            MovementDestinationKind.AdjacentToEntity when destination.Direction is null => "movementDestination.direction is required for AdjacentToEntity destinations.",
            MovementDestinationKind.AdjacentToCanonicalTarget when destination.Direction is null => "movementDestination.direction is required for AdjacentToCanonicalTarget destinations.",
            _ => null
        };

    private static void AddInvalidMovementDiagnostic(
        List<ContentDiagnostic> diagnostics,
        ActionPlanTemplateId actionPlanTemplateId,
        ActionPlanDescriptor plan,
        ActionPlanStepDescriptor step,
        PlanEffectDescriptor effect,
        string detail)
    {
        AddDiagnostic(diagnostics, ContentDiagnostic.Error(
            ContentDiagnosticCode.InvalidMovementDescriptor,
            $"Action plan {plan.Id} step {step.Label} has invalid {effect.Kind} movement descriptor: {detail}",
            actionPlanTemplateId: actionPlanTemplateId,
            actionPlanId: plan.Id,
            stepIndex: StepIndex(plan, step)));
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

    private static void TryValidate(List<string> errors, string subject, Action materialize)
    {
        try
        {
            materialize();
        }
        catch (Exception ex)
        {
            errors.Add($"{subject} is invalid: {ex.Message}");
        }
    }
}
