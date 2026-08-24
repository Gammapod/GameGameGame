using GameGameGame.Core;

namespace GameGameGame.Content;

internal static class ActionPlanValidator
{
    public static void Validate(
        IReadOnlyDictionary<EntityTemplateId, EntityTemplate> entityTemplates,
        IReadOnlyDictionary<ActionPlanTemplateId, ActionPlanDescriptor> actionPlanTemplates,
        List<string> errors,
        List<ContentDiagnostic> diagnostics)
    {
        var planIds = actionPlanTemplates.Values.Select(plan => plan.Id).ToHashSet();

        foreach (var (templateId, descriptor) in actionPlanTemplates)
        {
            TryValidate(errors, $"Action plan template {templateId} ({descriptor.Id})", () => descriptor.Materialize());

            ValidateActionPlanShape(diagnostics, templateId, descriptor);
            ValidateBehaviorAuthoringSteps(diagnostics, templateId, descriptor);
            ValidateBehaviorTargetSlots(diagnostics, templateId, descriptor);
            ValidateBehaviorStepFields(diagnostics, templateId, descriptor, entityTemplates.Keys.ToHashSet());
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

    private static void ValidateBehaviorAuthoringSteps(
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
            if (ActionStepCatalog.IsRetiredLegacyTargetingOrCoordinateMovementStep(step.Kind))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.UnsupportedLegacyActionStep,
                    $"Action plan {descriptor.Id} action step {step.Kind} is legacy targeting/coordinate movement and is not supported for graph-first canonical authoring; use graph-first targeting rules and TargetPathMove instead.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    stepIndex: index));
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

            if (step.CounterpartyTargetSlot is <= 0)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidActionStepTargetSlot,
                    $"Action plan {descriptor.Id} action step {step.Kind} counterpartyTargetSlot must be greater than zero; found {step.CounterpartyTargetSlot}.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    stepIndex: index));
            }

            var targetReferenceCount = (step.TargetSlot is not null ? 1 : 0)
                + (!string.IsNullOrWhiteSpace(step.TargetLabel) ? 1 : 0)
                + (step.TargetSelf ? 1 : 0);
            if (targetReferenceCount > 1)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidActionStepTargetReference,
                    $"Action plan {descriptor.Id} action step {step.Kind} must use only one of targetLabel, targetSlot, or targetSelf.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    stepIndex: index));
            }

            var counterpartyTargetReferenceCount = (step.CounterpartyTargetSlot is not null ? 1 : 0)
                + (!string.IsNullOrWhiteSpace(step.CounterpartyTargetLabel) ? 1 : 0);
            if (counterpartyTargetReferenceCount > 1)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidActionStepTargetReference,
                    $"Action plan {descriptor.Id} action step {step.Kind} must use only one of counterpartyTargetLabel or counterpartyTargetSlot.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    stepIndex: index));
            }

            if (step.Kind != ActionPlanBehaviorStepKind.Transfer && counterpartyTargetReferenceCount > 0)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidActionStepTargetReference,
                    $"Action plan {descriptor.Id} action step {step.Kind} counterparty target references are only valid on Transfer steps.",
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

            if (step.CounterpartyTargetLabel is { } counterpartyLabel && string.IsNullOrWhiteSpace(counterpartyLabel))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidActionStepTargetReference,
                    $"Action plan {descriptor.Id} action step {step.Kind} counterpartyTargetLabel must not be blank.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    stepIndex: index));
            }
        }
    }

    private static void ValidateBehaviorStepFields(
        List<ContentDiagnostic> diagnostics,
        ActionPlanTemplateId actionPlanTemplateId,
        ActionPlanDescriptor descriptor,
        HashSet<EntityTemplateId> entityTemplateIds)
    {
        if (descriptor.Behavior is not { } behavior)
        {
            return;
        }

        for (var index = 0; index < behavior.Steps.Count; index++)
        {
            var step = behavior.Steps[index];
            if (step.Kind is ActionPlanBehaviorStepKind.Move or ActionPlanBehaviorStepKind.Push && step.DirectionMode is null)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidActionStepField,
                    $"Action plan {descriptor.Id} action step {step.Kind} requires directionMode.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    stepIndex: index));
            }

            if (step.Kind == ActionPlanBehaviorStepKind.Transfer
                && step.DirectionMode is null
                && step.CounterpartyTargetSlot is null
                && string.IsNullOrWhiteSpace(step.CounterpartyTargetLabel))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidActionStepField,
                    $"Action plan {descriptor.Id} action step {step.Kind} requires directionMode or counterparty target reference.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    stepIndex: index));
            }

            if (step.Kind == ActionPlanBehaviorStepKind.Transfer && step.TransferDirection is null)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidActionStepField,
                    $"Action plan {descriptor.Id} action step {step.Kind} requires transferDirection.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    stepIndex: index));
            }

            if (step.Kind is ActionPlanBehaviorStepKind.CreateEntity or ActionPlanBehaviorStepKind.PolymorphTarget)
            {
                if (string.IsNullOrWhiteSpace(step.TemplateId))
                {
                    AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.MissingTargetTemplateReference,
                        $"Action plan {descriptor.Id} action step {step.Kind} requires templateId.",
                        actionPlanTemplateId: actionPlanTemplateId,
                        actionPlanId: descriptor.Id,
                        stepIndex: index));
                }
                else if (!entityTemplateIds.Contains(new EntityTemplateId(step.TemplateId)))
                {
                    AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.MissingTargetTemplateReference,
                        $"Action plan {descriptor.Id} action step {step.Kind} references missing template {step.TemplateId}.",
                        actionPlanTemplateId: actionPlanTemplateId,
                        actionPlanId: descriptor.Id,
                        stepIndex: index));
                }
            }

            if (step.Kind == ActionPlanBehaviorStepKind.CreateEntity
                && step.CreatePlacement == CreateEntityPlacement.Facing
                && step.DirectionMode is null)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidActionStepField,
                    $"Action plan {descriptor.Id} action step {step.Kind} with Facing placement requires directionMode.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    stepIndex: index));
            }

            ValidateTargetPathMoveFields(diagnostics, actionPlanTemplateId, descriptor, step, index);

            ValidateBehaviorStepCosts(diagnostics, actionPlanTemplateId, descriptor, step, index, entityTemplateIds);
        }
    }

    private static void ValidateTargetPathMoveFields(
        List<ContentDiagnostic> diagnostics,
        ActionPlanTemplateId actionPlanTemplateId,
        ActionPlanDescriptor descriptor,
        ActionPlanBehaviorStepDescriptor step,
        int stepIndex)
    {
        if (step.Kind != ActionPlanBehaviorStepKind.TargetPathMove)
        {
            return;
        }

        if (step.PathMode is null)
        {
            AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                ContentDiagnosticCode.InvalidActionStepField,
                $"Action plan {descriptor.Id} action step {step.Kind} requires pathMode.",
                actionPlanTemplateId: actionPlanTemplateId,
                actionPlanId: descriptor.Id,
                stepIndex: stepIndex));
            return;
        }

        if (step.DesiredDistance is < 0)
        {
            AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                ContentDiagnosticCode.InvalidActionStepField,
                $"Action plan {descriptor.Id} action step {step.Kind} desiredDistance must be non-negative; found {step.DesiredDistance}.",
                actionPlanTemplateId: actionPlanTemplateId,
                actionPlanId: descriptor.Id,
                stepIndex: stepIndex));
        }

        if (step.PathMode is ActionPlanTargetPathMode.MaintainDistance or ActionPlanTargetPathMode.Orbit && step.DesiredDistance is null)
        {
            AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                ContentDiagnosticCode.InvalidActionStepField,
                $"Action plan {descriptor.Id} action step {step.Kind} pathMode {step.PathMode} requires desiredDistance.",
                actionPlanTemplateId: actionPlanTemplateId,
                actionPlanId: descriptor.Id,
                stepIndex: stepIndex));
        }

        if (step.PathMode is ActionPlanTargetPathMode.SeekAdjacency or ActionPlanTargetPathMode.FleeAdjacency && step.DesiredDistance is not null)
        {
            AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                ContentDiagnosticCode.InvalidActionStepField,
                $"Action plan {descriptor.Id} action step {step.Kind} pathMode {step.PathMode} does not support desiredDistance.",
                actionPlanTemplateId: actionPlanTemplateId,
                actionPlanId: descriptor.Id,
                stepIndex: stepIndex));
        }

        if (step.PathMode == ActionPlanTargetPathMode.Orbit && step.OrbitDirection is null)
        {
            AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                ContentDiagnosticCode.InvalidActionStepField,
                $"Action plan {descriptor.Id} action step {step.Kind} pathMode Orbit requires orbitDirection.",
                actionPlanTemplateId: actionPlanTemplateId,
                actionPlanId: descriptor.Id,
                stepIndex: stepIndex));
        }

        if (step.PathMode != ActionPlanTargetPathMode.Orbit && step.OrbitDirection is not null)
        {
            AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                ContentDiagnosticCode.InvalidActionStepField,
                $"Action plan {descriptor.Id} action step {step.Kind} orbitDirection is only supported by pathMode Orbit.",
                actionPlanTemplateId: actionPlanTemplateId,
                actionPlanId: descriptor.Id,
                stepIndex: stepIndex));
        }
    }

    private static void ValidateBehaviorStepCosts(
        List<ContentDiagnostic> diagnostics,
        ActionPlanTemplateId actionPlanTemplateId,
        ActionPlanDescriptor descriptor,
        ActionPlanBehaviorStepDescriptor step,
        int stepIndex,
        HashSet<EntityTemplateId> entityTemplateIds)
    {
        var seenTemplateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var costIndex = 0; costIndex < step.Costs.Count; costIndex++)
        {
            var cost = step.Costs[costIndex];
            if (string.IsNullOrWhiteSpace(cost.TemplateId))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingTargetTemplateReference,
                    $"Action plan {descriptor.Id} action step {step.Kind} cost {costIndex} requires templateId.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    stepIndex: stepIndex));
            }
            else
            {
                if (!entityTemplateIds.Contains(new EntityTemplateId(cost.TemplateId)))
                {
                    AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.MissingTargetTemplateReference,
                        $"Action plan {descriptor.Id} action step {step.Kind} cost {costIndex} references missing template {cost.TemplateId}.",
                        actionPlanTemplateId: actionPlanTemplateId,
                        actionPlanId: descriptor.Id,
                        stepIndex: stepIndex));
                }

                if (!seenTemplateIds.Add(cost.TemplateId))
                {
                    AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidActionStepField,
                        $"Action plan {descriptor.Id} action step {step.Kind} has duplicate cost template {cost.TemplateId}; combine duplicate costs into one entry with a summed quantity.",
                        actionPlanTemplateId: actionPlanTemplateId,
                        actionPlanId: descriptor.Id,
                        stepIndex: stepIndex));
                }
            }

            if (cost.Quantity <= 0)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidActionStepField,
                    $"Action plan {descriptor.Id} action step {step.Kind} cost {costIndex} quantity must be greater than zero; found {cost.Quantity}.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    stepIndex: stepIndex));
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
