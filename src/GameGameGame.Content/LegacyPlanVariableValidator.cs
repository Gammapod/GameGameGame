using GameGameGame.Core;

namespace GameGameGame.Content;

internal static class LegacyPlanVariableValidator
{
    public static void ValidatePlanVariables(
        List<string> errors,
        List<ContentDiagnostic> diagnostics,
        string subject,
        EntityTemplateId? entityTemplateId,
        ActionPlanTemplateId? actionPlanTemplateId,
        ActionPlanDescriptor plan,
        Dictionary<string, PlanValueKind> variables,
        IReadOnlyDictionary<ActionPlanId, ActionPlanDescriptor> plansById,
        HashSet<ActionPlanId> callStack)
    {
        if (!callStack.Add(plan.Id))
        {
            return;
        }

        foreach (var step in plan.Steps)
        {
            foreach (var check in step.Checks)
            {
                ValidatePrimitiveFields(diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, step, PlanPrimitiveCatalog.GetCheck(check.Kind).Fields, check, variables);
                ApplyPrimitiveWrites(PlanPrimitiveCatalog.GetCheck(check.Kind).Fields, check, variables);
            }

            ValidateEffectVariables(errors, diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, step, step.OnSuccess, variables, plansById, callStack);
            ValidateEffectVariables(errors, diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, step, step.OnFailure, variables, plansById, callStack);
        }

        callStack.Remove(plan.Id);
    }

    private static void ValidatePrimitiveFields(
        List<ContentDiagnostic> diagnostics,
        string subject,
        EntityTemplateId? entityTemplateId,
        ActionPlanTemplateId? actionPlanTemplateId,
        ActionPlanDescriptor plan,
        ActionPlanStepDescriptor step,
        IReadOnlyList<PlanPrimitiveFieldDescriptor> fields,
        object descriptor,
        Dictionary<string, PlanValueKind> variables)
    {
        foreach (var field in fields.Where(field => field.Kind == PlanPrimitiveFieldKind.VariableRead))
        {
            var variableName = GetVariableName(descriptor, field.Name);

            if (string.IsNullOrWhiteSpace(variableName) || field.ValueKind is not { } expectedKind)
            {
                continue;
            }

            if (!variables.TryGetValue(variableName, out var actualKind))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingPlanVariable,
                    $"{subject} step {step.Label} reads missing required variable {variableName}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    stepIndex: StepIndex(plan, step),
                    variableName: variableName,
                    expectedValueKind: expectedKind));
                continue;
            }

            if (actualKind != expectedKind)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.PlanVariableTypeMismatch,
                    $"{subject} step {step.Label} variable {variableName} expected {expectedKind} but found {actualKind}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    stepIndex: StepIndex(plan, step),
                    variableName: variableName,
                    expectedValueKind: expectedKind,
                    actualValueKind: actualKind));
            }
        }
    }

    private static void ApplyPrimitiveWrites(
        IReadOnlyList<PlanPrimitiveFieldDescriptor> fields,
        object descriptor,
        Dictionary<string, PlanValueKind> variables)
    {
        foreach (var field in fields.Where(field => field.Kind == PlanPrimitiveFieldKind.VariableWrite))
        {
            var variableName = GetVariableName(descriptor, field.Name);

            if (!string.IsNullOrWhiteSpace(variableName) && field.ValueKind is { } valueKind)
            {
                variables[variableName] = valueKind;
            }
        }
    }

    private static void ValidateEffectVariables(
        List<string> errors,
        List<ContentDiagnostic> diagnostics,
        string subject,
        EntityTemplateId? entityTemplateId,
        ActionPlanTemplateId? actionPlanTemplateId,
        ActionPlanDescriptor plan,
        ActionPlanStepDescriptor step,
        PlanEffectDescriptor? effect,
        Dictionary<string, PlanValueKind> variables,
        IReadOnlyDictionary<ActionPlanId, ActionPlanDescriptor> plansById,
        HashSet<ActionPlanId> callStack)
    {
        if (effect is null)
        {
            return;
        }

        var fields = PlanPrimitiveCatalog.GetEffect(effect.Kind).Fields;

        foreach (var field in fields.Where(field => field.Kind == PlanPrimitiveFieldKind.VariableRead))
        {
            var variableName = GetVariableName(effect, field.Name);

            if (string.IsNullOrWhiteSpace(variableName) || field.ValueKind is not { } expectedKind)
            {
                continue;
            }

            if (!variables.TryGetValue(variableName, out var actualKind))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingPlanVariable,
                    $"{subject} step {step.Label} reads missing required variable {variableName}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    stepIndex: StepIndex(plan, step),
                    variableName: variableName,
                    expectedValueKind: expectedKind));
                continue;
            }

            if (actualKind != expectedKind)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.PlanVariableTypeMismatch,
                    $"{subject} step {step.Label} variable {variableName} expected {expectedKind} but found {actualKind}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    stepIndex: StepIndex(plan, step),
                    variableName: variableName,
                    expectedValueKind: expectedKind,
                    actualValueKind: actualKind));
            }
        }

        foreach (var field in fields.Where(field => field.Kind == PlanPrimitiveFieldKind.VariableWrite))
        {
            var variableName = GetVariableName(effect, field.Name);

            if (string.IsNullOrWhiteSpace(variableName))
            {
                continue;
            }

            if (field.ValueKind is { } valueKind)
            {
                variables[variableName] = valueKind;
            }
            else if (effect.Kind == PlanEffectKind.SetVariable && effect.Value is not null)
            {
                variables[variableName] = GetPlanValueKind(effect.Value);
            }
        }

        if (effect.Kind == PlanEffectKind.CallPlan
            && effect.PlanId is { } planId
            && plansById.TryGetValue(planId, out var calledPlan))
        {
            ValidatePlanVariables(errors, diagnostics, subject, entityTemplateId, actionPlanTemplateId, calledPlan, variables, plansById, callStack);
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

    private static string? GetVariableName(object descriptor, string fieldName) =>
        descriptor switch
        {
            PlanCheckDescriptor check => fieldName switch
            {
                "directionVariable" => check.DirectionVariable,
                "targetVariable" => check.TargetVariable,
                _ => null
            },
            PlanEffectDescriptor effect => fieldName switch
            {
                "directionVariable" => effect.DirectionVariable,
                "targetVariable" => effect.TargetVariable,
                "variableName" => effect.VariableName,
                _ => null
            },
            _ => null
        };

    private static PlanValueKind GetPlanValueKind(PlanValue value) =>
        value switch
        {
            DirectionPlanValue => PlanValueKind.Direction,
            EntityPlanValue => PlanValueKind.Entity,
            CoordPlanValue => PlanValueKind.Coord,
            IntPlanValue => PlanValueKind.Int,
            _ => throw new InvalidOperationException($"Unsupported plan value type {value.GetType().Name}.")
        };
}
