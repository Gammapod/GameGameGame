using GameGameGame.Core;

namespace GameGameGame.Content;

internal sealed class FrontendActionPlanMutationService(
    ContentEditorSession session,
    Func<FrontendEditorSnapshot> getSnapshot)
{
    public FrontendEditorMutationResult CreateActionPlan(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return FrontendEditorMutationResult.Failure("Action plan name is required.", getSnapshot());
        }

        try
        {
            var id = session.Editor.CreateActionPlan(name.Trim());
            return FrontendEditorMutationResult.Success(
                $"Created action plan {id.Value}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not create action plan {name.Trim()}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult CreatePassiveActionPlan(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return FrontendEditorMutationResult.Failure("Action plan name is required.", getSnapshot());
        }

        try
        {
            var id = session.Editor.CreatePassiveActionPlan(name.Trim());
            return FrontendEditorMutationResult.Success(
                $"Created passive action plan {id.Value}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not create passive action plan {name.Trim()}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult DuplicateActionPlan(string sourceActionPlanId, string name)
    {
        if (string.IsNullOrWhiteSpace(sourceActionPlanId))
        {
            return FrontendEditorMutationResult.Failure("Source action plan id is required.", getSnapshot());
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return FrontendEditorMutationResult.Failure("Action plan name is required.", getSnapshot());
        }

        if (session.Document.ActionPlans.ContainsKey(sourceActionPlanId) is false)
        {
            return FrontendEditorMutationResult.Failure($"Source action plan {sourceActionPlanId} does not exist.", getSnapshot());
        }

        try
        {
            var id = session.Editor.DuplicateActionPlan(new ActionPlanTemplateId(sourceActionPlanId), name.Trim());
            return FrontendEditorMutationResult.Success(
                $"Duplicated action plan {sourceActionPlanId} as {id.Value}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not duplicate action plan {sourceActionPlanId}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult DeleteActionPlan(string actionPlanId)
    {
        if (string.IsNullOrWhiteSpace(actionPlanId))
        {
            return FrontendEditorMutationResult.Failure("Action plan id is required.", getSnapshot());
        }

        if (session.Document.ActionPlans.ContainsKey(actionPlanId) is false)
        {
            return FrontendEditorMutationResult.Failure($"Action plan {actionPlanId} does not exist.", getSnapshot());
        }

        try
        {
            var result = session.Editor.DeleteActionPlan(new ActionPlanTemplateId(actionPlanId));
            if (!result.IsSuccess)
            {
                return FrontendEditorMutationResult.Failure(
                    result.ErrorMessage ?? $"Could not delete action plan {actionPlanId}.",
                    getSnapshot());
            }

            return FrontendEditorMutationResult.Success(
                $"Deleted action plan {actionPlanId}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not delete action plan {actionPlanId}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult ReplaceActionPlanStep(
        string actionPlanId,
        int stepIndex,
        ActionPlanBehaviorStepKind kind)
    {
        var validationError = ValidateActionPlanStepMutation(actionPlanId, kind);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, getSnapshot());
        }

        try
        {
            var planId = new ActionPlanTemplateId(actionPlanId);
            var steps = GetEditableBehaviorSteps(planId);
            if (stepIndex < 0 || stepIndex >= steps.Count)
            {
                return FrontendEditorMutationResult.Failure(
                    $"Action plan {actionPlanId} step index {stepIndex} is outside editable step range 0..{Math.Max(steps.Count - 1, 0)}.",
                    getSnapshot());
            }

            steps[stepIndex] = new ActionPlanBehaviorStepDescriptor(kind);
            session.Editor.SetActionPlanBehavior(planId, steps);
            return FrontendEditorMutationResult.Success(
                $"Replaced action plan {actionPlanId} step {stepIndex} with {ActionStepCatalog.Get(kind).DisplayName}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not replace action plan {actionPlanId} step {stepIndex}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult InsertActionPlanStep(
        string actionPlanId,
        int insertIndex,
        ActionPlanBehaviorStepKind kind)
    {
        var validationError = ValidateActionPlanStepMutation(actionPlanId, kind);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, getSnapshot());
        }

        try
        {
            var planId = new ActionPlanTemplateId(actionPlanId);
            var steps = GetEditableBehaviorSteps(planId, allowEmptyPassive: true);
            if (insertIndex < 0 || insertIndex > steps.Count)
            {
                return FrontendEditorMutationResult.Failure(
                    $"Action plan {actionPlanId} insert index {insertIndex} is outside editable insert range 0..{steps.Count}.",
                    getSnapshot());
            }

            steps.Insert(insertIndex, new ActionPlanBehaviorStepDescriptor(kind));
            session.Editor.SetActionPlanBehavior(planId, steps);
            return FrontendEditorMutationResult.Success(
                $"Inserted {ActionStepCatalog.Get(kind).DisplayName} into action plan {actionPlanId} at {insertIndex}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not insert action step into action plan {actionPlanId}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult RemoveActionPlanStep(string actionPlanId, int stepIndex)
    {
        var validationError = ValidateActionPlanMutation(actionPlanId);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, getSnapshot());
        }

        try
        {
            var planId = new ActionPlanTemplateId(actionPlanId);
            var steps = GetEditableBehaviorSteps(planId);
            if (stepIndex < 0 || stepIndex >= steps.Count)
            {
                return FrontendEditorMutationResult.Failure(
                    $"Action plan {actionPlanId} step index {stepIndex} is outside editable step range 0..{Math.Max(steps.Count - 1, 0)}.",
                    getSnapshot());
            }

            var removed = steps[stepIndex];
            session.Editor.RemoveActionPlanBehaviorStep(planId, stepIndex);
            return FrontendEditorMutationResult.Success(
                $"Removed {ActionStepCatalog.Get(removed.Kind).DisplayName} from action plan {actionPlanId} at {stepIndex}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not remove action plan {actionPlanId} step {stepIndex}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult MoveActionPlanStep(string actionPlanId, int fromIndex, int toIndex)
    {
        var validationError = ValidateActionPlanMutation(actionPlanId);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, getSnapshot());
        }

        try
        {
            var planId = new ActionPlanTemplateId(actionPlanId);
            var steps = GetEditableBehaviorSteps(planId);
            if (fromIndex < 0 || fromIndex >= steps.Count)
            {
                return FrontendEditorMutationResult.Failure(
                    $"Action plan {actionPlanId} from index {fromIndex} is outside editable step range 0..{Math.Max(steps.Count - 1, 0)}.",
                    getSnapshot());
            }

            if (toIndex < 0 || toIndex >= steps.Count)
            {
                return FrontendEditorMutationResult.Failure(
                    $"Action plan {actionPlanId} to index {toIndex} is outside editable step range 0..{Math.Max(steps.Count - 1, 0)}.",
                    getSnapshot());
            }

            session.Editor.MoveActionPlanBehaviorStep(planId, fromIndex, toIndex);
            return FrontendEditorMutationResult.Success(
                $"Moved action plan {actionPlanId} step from {fromIndex} to {toIndex}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not move action plan {actionPlanId} step from {fromIndex} to {toIndex}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult SetActionPlanStepTargetLabel(
        string actionPlanId,
        int stepIndex,
        string? targetLabel)
    {
        var validationError = ValidateActionPlanMutation(actionPlanId);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, getSnapshot());
        }

        try
        {
            var planId = new ActionPlanTemplateId(actionPlanId);
            var steps = GetEditableBehaviorSteps(planId);
            if (stepIndex < 0 || stepIndex >= steps.Count)
            {
                return FrontendEditorMutationResult.Failure(
                    $"Action plan {actionPlanId} step index {stepIndex} is outside editable step range 0..{Math.Max(steps.Count - 1, 0)}.",
                    getSnapshot());
            }

            var normalizedLabel = targetLabel?.Trim();
            var labelValidationError = ValidateActionPlanStepTargetLabel(normalizedLabel);
            if (labelValidationError is not null)
            {
                return FrontendEditorMutationResult.Failure(labelValidationError, getSnapshot());
            }

            session.Editor.SetActionPlanBehaviorStepTargetLabel(planId, stepIndex, normalizedLabel);
            var displayLabel = normalizedLabel is null ? "cleared" : $"set to {normalizedLabel}";
            return FrontendEditorMutationResult.Success(
                $"Action plan {actionPlanId} step {stepIndex} target label {displayLabel}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not set action plan {actionPlanId} step {stepIndex} target label: {ex.Message}",
                getSnapshot());
        }
    }

    private string? ValidateActionPlanStepMutation(string actionPlanId, ActionPlanBehaviorStepKind kind)
    {
        var validationError = ValidateActionPlanMutation(actionPlanId);
        if (validationError is not null)
        {
            return validationError;
        }

        _ = ActionStepCatalog.Get(kind);
        if (ActionStepCatalog.IsStableAuthoringStep(kind) is false)
        {
            return $"Action step {kind} is not available for canonical authoring.";
        }

        return null;
    }

    private string? ValidateActionPlanMutation(string actionPlanId)
    {
        if (string.IsNullOrWhiteSpace(actionPlanId))
        {
            return "Action plan id is required.";
        }

        if (session.Document.ActionPlans.ContainsKey(actionPlanId) is false)
        {
            return $"Action plan {actionPlanId} does not exist.";
        }

        return null;
    }

    private static string? ValidateActionPlanStepTargetLabel(string? targetLabel)
    {
        if (targetLabel is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(targetLabel))
        {
            return "Action step target label must not be blank.";
        }

        if (targetLabel.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9') is false)
        {
            return "Action step target label must be lowercase alphanumeric with no spaces.";
        }

        return null;
    }

    private List<ActionPlanBehaviorStepDescriptor> GetEditableBehaviorSteps(
        ActionPlanTemplateId planId,
        bool allowEmptyPassive = false)
    {
        var descriptor = session.Editor.ListActionPlans()
            .Single(plan => plan.TemplateId == planId)
            .Descriptor;
        var shape = ActionPlanShapeClassifier.Classify(descriptor);

        if (descriptor.Behavior is { } behavior)
        {
            return behavior.Steps.ToList();
        }

        if (allowEmptyPassive && shape == ActionPlanShape.EmptyPassive)
        {
            return [];
        }

        throw new InvalidOperationException($"Action plan {planId} is {ContentEditorService.FormatActionPlanShape(shape)}; only canonical behavior chains are editable in this slice.");
    }
}
