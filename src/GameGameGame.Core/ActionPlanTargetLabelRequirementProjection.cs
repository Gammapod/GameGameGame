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
            if (string.IsNullOrWhiteSpace(step.TargetLabel))
            {
                continue;
            }

            var label = step.TargetLabel;
            if (!byLabel.TryGetValue(label, out var existing))
            {
                existing = ([], []);
                byLabel.Add(label, existing);
                orderedLabels.Add(label);
            }

            existing.StepIndexes.Add(index);
            existing.StepKinds.Add(step.Kind);
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
}
