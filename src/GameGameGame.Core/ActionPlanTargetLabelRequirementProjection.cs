namespace GameGameGame.Core;

public sealed record ActionPlanTargetLabelRequirement(
    string Label,
    IReadOnlyList<int> StepIndexes,
    IReadOnlyList<ActionPlanBehaviorStepKind> StepKinds);

public static class ActionPlanTargetLabelRequirementProjection
{
    public static IReadOnlyList<ActionPlanTargetLabelRequirement> Project(ActionPlanDescriptor descriptor)
    {
        if (descriptor.Behavior?.Steps.Count is not > 0)
        {
            return [];
        }

        var orderedLabels = new List<string>();
        var byLabel = new Dictionary<string, (List<int> StepIndexes, List<ActionPlanBehaviorStepKind> StepKinds)>(StringComparer.Ordinal);

        for (var index = 0; index < descriptor.Behavior.Steps.Count; index++)
        {
            var step = descriptor.Behavior.Steps[index];
            AddLabel(step.TargetLabel, index, step.Kind, orderedLabels, byLabel);
            AddLabel(step.CounterpartyTargetLabel, index, step.Kind, orderedLabels, byLabel);
        }

        return orderedLabels
            .Select(label =>
            {
                var requirement = byLabel[label];
                return new ActionPlanTargetLabelRequirement(
                    label,
                    requirement.StepIndexes,
                    requirement.StepKinds);
            })
            .ToList();
    }

    private static void AddLabel(
        string? label,
        int stepIndex,
        ActionPlanBehaviorStepKind stepKind,
        List<string> orderedLabels,
        Dictionary<string, (List<int> StepIndexes, List<ActionPlanBehaviorStepKind> StepKinds)> byLabel)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return;
        }

            if (!byLabel.TryGetValue(label, out var existing))
            {
                existing = ([], []);
                byLabel.Add(label, existing);
                orderedLabels.Add(label);
            }

            existing.StepIndexes.Add(stepIndex);
            existing.StepKinds.Add(stepKind);
    }
}
