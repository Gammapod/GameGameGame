using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed partial class EditableContentDocument
{
    private void CanonicalizeLegacyActionPlanVariableFields()
    {
        CanonicalizeLegacyActionStateDefaults();

        foreach (var plan in ActionPlans.Values)
        {
            foreach (var step in plan.Steps ?? [])
            {
                foreach (var check in step.Checks ?? [])
                {
                    CanonicalizeLegacyCheckVariableFields(check);
                }

                if (step.OnSuccess is not null)
                {
                    CanonicalizeLegacyEffectVariableFields(step.OnSuccess);
                }

                if (step.OnFailure is not null)
                {
                    CanonicalizeLegacyEffectVariableFields(step.OnFailure);
                }
            }
        }
    }

    private void CanonicalizeLegacyActionStateDefaults()
    {
        foreach (var template in EntityTemplates.Values)
        {
            if (template.DefaultPlanVariables is null)
            {
                continue;
            }

            if (template.DefaultPlanVariables.TryGetValue("facing", out var facing)
                && facing.Kind == PlanValueKind.Direction
                && facing.DirectionValue is { } direction)
            {
                template.ActionStateDefaults ??= new ActorActionStateDefaultsDto();
                template.ActionStateDefaults.Facing ??= direction;
                template.DefaultPlanVariables.Remove("facing");
            }

            if (template.DefaultPlanVariables.Count == 0)
            {
                template.DefaultPlanVariables = null;
            }
        }
    }

    private static void CanonicalizeLegacyCheckVariableFields(PlanCheckDescriptorDto check)
    {
        switch (check.Kind)
        {
            case PlanCheckKind.CanMove:
                check.DirectionVariable = ClearIfCanonicalFacing(check.DirectionVariable);
                break;
            case PlanCheckKind.BlockingEntity:
                if (IsCanonicalFacing(check.DirectionVariable) && IsCanonicalTarget(check.TargetVariable))
                {
                    check.DirectionVariable = null;
                    check.TargetVariable = null;
                }
                break;
            case PlanCheckKind.CanPickup:
                check.TargetVariable = ClearIfCanonicalTarget(check.TargetVariable);
                break;
        }
    }

    private static void CanonicalizeLegacyEffectVariableFields(PlanEffectDescriptorDto effect)
    {
        switch (effect.Kind)
        {
            case PlanEffectKind.Move:
                effect.DirectionVariable = ClearIfCanonicalFacing(effect.DirectionVariable);
                break;
            case PlanEffectKind.Pickup:
                effect.TargetVariable = ClearIfCanonicalTarget(effect.TargetVariable);
                break;
            case PlanEffectKind.ReverseDirection:
                effect.DirectionVariable = ClearIfCanonicalFacing(effect.DirectionVariable);
                break;
        }
    }

    private static string? ClearIfCanonicalFacing(string? value) =>
        IsCanonicalFacing(value) ? null : value;

    private static string? ClearIfCanonicalTarget(string? value) =>
        IsCanonicalTarget(value) ? null : value;

    private static bool IsCanonicalFacing(string? value) =>
        string.Equals(value, "facing", StringComparison.Ordinal);

    private static bool IsCanonicalTarget(string? value) =>
        string.Equals(value, "target", StringComparison.Ordinal);
}
