using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed class ActionPlanPreviewService(EditableContentDocument document)
{
    public ActionPlanPreview Preview(ActionPlanTemplateId planId, EntityTemplateId? entityTemplateId = null, bool includeYamlPreview = true)
    {
        var registry = document.ToRegistry();
        var plan = registry.ActionPlanDescriptors
            .Single(item => item.Key == planId)
            .Value;
        var validation = registry.Validate();
        var canonicalValidation = document.ValidateCanonicalAuthoring();
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
            includeYamlPreview ? document.SaveYaml() : string.Empty);
    }

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

    private static IReadOnlyList<string> GetActionPlanGuidance(ActionPlanDescriptor descriptor) =>
        ActionPlanShapeClassifier.Classify(descriptor) switch
        {
            ActionPlanShape.CanonicalBehaviorChain => ["Preferred canonical behavior chain. Author normal behavior as ordered engine-defined Action Steps."],
            ActionPlanShape.TransitionalPrimitivePlan => ["Compatibility primitive-backed fallback plan. Prefer canonical behavior chains for new authoring."],
            ActionPlanShape.LegacyLowLevelSteps => ["Legacy/advanced low-level steps/checks/effects. Keep only where canonical Action Steps cannot express the behavior yet."],
            ActionPlanShape.EmptyPassive => ["Passive plan with no current behavior. Add canonical Action Steps when the entity should act."],
            _ => ["Unknown action-plan shape. Validate before saving."]
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
                        step.TargetLabel,
                        step.PlanId,
                        step.DirectionMode,
                        step.TransferDirection);
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

    private ActorActionStateDefaults GetActionStateDefaults(EntityTemplateId templateId)
    {
        var template = document.EntityTemplates[templateId.Value];

        return new ActorActionStateDefaults(
            template.ActionStateDefaults?.Facing,
            string.IsNullOrWhiteSpace(template.ActionStateDefaults?.Target) ? null : new EntityId(template.ActionStateDefaults.Target));
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
}
