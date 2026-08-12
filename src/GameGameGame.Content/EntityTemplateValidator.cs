using GameGameGame.Core;

namespace GameGameGame.Content;

internal static class EntityTemplateValidator
{
    public static void Validate(
        IReadOnlyDictionary<EntityTemplateId, EntityTemplate> entityTemplates,
        IReadOnlyDictionary<ActionPlanTemplateId, ActionPlanDescriptor> actionPlanTemplates,
        IReadOnlyDictionary<EntityTemplateId, EntityPresentation> presentations,
        List<string> errors,
        List<ContentDiagnostic> diagnostics)
    {
        foreach (var (templateId, template) in entityTemplates)
        {
            if (!presentations.ContainsKey(templateId))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingPresentation,
                    $"Entity template {templateId} ({template.Name}) has no presentation.",
                    entityTemplateId: templateId));
            }

            ValidateActionPlanTemplateReference(diagnostics, actionPlanTemplates, templateId, template, template.DefaultActionPlanId, nameof(template.DefaultActionPlanId));
            ValidateTargetingRules(diagnostics, entityTemplates, actionPlanTemplates, templateId, template);

            if (template.DefaultPlanVariables is not null)
            {
                foreach (var (name, value) in template.DefaultPlanVariables)
                {
                    TryValidate(errors, $"Entity template {templateId} ({template.Name}) default variable {name}", () => value.Materialize());
                }
            }

            if (template.CarriedEntities is null)
            {
                continue;
            }

            ValidateCarriedEntityLayout(diagnostics, templateId, template);

            foreach (var carried in template.CarriedEntities)
            {
                if (carried.TemplateId is { } carriedTemplateId && !entityTemplates.ContainsKey(carriedTemplateId))
                {
                    AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.MissingCarriedEntityTemplateReference,
                        $"Entity template {templateId} ({template.Name}) carries {carried.EntityId} with missing template {carriedTemplateId}.",
                        entityTemplateId: templateId,
                        carriedEntityId: carried.EntityId,
                        referencedEntityTemplateId: carriedTemplateId));
                }
            }
        }
    }

    private static void ValidateTargetingRules(
        List<ContentDiagnostic> diagnostics,
        IReadOnlyDictionary<EntityTemplateId, EntityTemplate> entityTemplates,
        IReadOnlyDictionary<ActionPlanTemplateId, ActionPlanDescriptor> actionPlanTemplates,
        EntityTemplateId templateId,
        EntityTemplate template)
    {
        if (template.TargetingRules is null || template.TargetingRules.Count == 0)
        {
            return;
        }

        var slots = new HashSet<int>();
        var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in template.TargetingRules)
        {
            if (rule.Slot <= 0)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidTargetingRule,
                    $"Entity template {templateId} ({template.Name}) targeting rule slot must be greater than zero; found {rule.Slot}.",
                    entityTemplateId: templateId));
            }

            if (!slots.Add(rule.Slot))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidTargetingRule,
                    $"Entity template {templateId} ({template.Name}) has duplicate targeting rule slot {rule.Slot}.",
                    entityTemplateId: templateId));
            }

            if (rule.Label is { } label)
            {
                if (string.IsNullOrWhiteSpace(label))
                {
                    AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidTargetingRule,
                        $"Entity template {templateId} ({template.Name}) targeting rule slot {rule.Slot} label must not be blank.",
                        entityTemplateId: templateId));
                }
                else if (!labels.Add(label))
                {
                    AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidTargetingRule,
                        $"Entity template {templateId} ({template.Name}) has duplicate targeting rule label {label}.",
                        entityTemplateId: templateId));
                }
            }

            if (rule.Range < 0)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidTargetingRule,
                    $"Entity template {templateId} ({template.Name}) targeting rule slot {rule.Slot} range must be zero or greater; found {rule.Range}.",
                    entityTemplateId: templateId));
            }

            if (rule.TargetTemplateId is null && rule.TargetCapabilities.Count == 0)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidTargetingRule,
                    $"Entity template {templateId} ({template.Name}) targeting rule slot {rule.Slot} must declare a target template, at least one target capability, or both.",
                    entityTemplateId: templateId));
            }

            if (rule.TargetTemplateId is { } targetTemplateId && !entityTemplates.ContainsKey(targetTemplateId))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingTargetTemplateReference,
                    $"Entity template {templateId} ({template.Name}) targeting rule slot {rule.Slot} references missing target template {targetTemplateId}.",
                    entityTemplateId: templateId));
            }

            foreach (var capability in rule.TargetCapabilities)
            {
                if (!EntityInteractionAffordanceService.IsSupportedTargetCapability(capability))
                {
                    AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidTargetingRule,
                        $"Entity template {templateId} ({template.Name}) targeting rule slot {rule.Slot} references unsupported target capability {capability}.",
                        entityTemplateId: templateId));
                    continue;
                }

                if (!TemplatePlanUsesTargetCapability(actionPlanTemplates, template, rule, capability))
                {
                    AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidTargetingRule,
                        $"Entity template {templateId} ({template.Name}) targeting rule slot {rule.Slot} capability {capability} is not consumed by its default action plan with the same target label/slot.",
                        entityTemplateId: templateId));
                }
            }
        }
    }

    private static bool TemplatePlanUsesTargetCapability(
        IReadOnlyDictionary<ActionPlanTemplateId, ActionPlanDescriptor> actionPlanTemplates,
        EntityTemplate template,
        EntityTargetingRule rule,
        ActionPlanBehaviorStepKind capability)
    {
        if (template.DefaultActionPlanId is not { } planId
            || !actionPlanTemplates.TryGetValue(planId, out var plan)
            || plan.Behavior?.Steps is not { Count: > 0 } steps)
        {
            return false;
        }

        return steps.Any(step =>
            step.Kind == capability
            && TargetReferenceMatchesRule(step, rule));
    }

    private static bool TargetReferenceMatchesRule(ActionPlanBehaviorStepDescriptor step, EntityTargetingRule rule)
    {
        if (!string.IsNullOrWhiteSpace(rule.Label)
            && string.Equals(step.TargetLabel, rule.Label, StringComparison.Ordinal))
        {
            return true;
        }

        return (step.TargetSlot ?? 1) == rule.Slot && string.IsNullOrWhiteSpace(step.TargetLabel);
    }

    private static void ValidateCarriedEntityLayout(
        List<ContentDiagnostic> diagnostics,
        EntityTemplateId templateId,
        EntityTemplate template)
    {
        if (template.CarriedEntities is null || template.CarriedEntities.Count == 0)
        {
            return;
        }

        var entityIds = new HashSet<EntityId>();
        var occupiedCoords = new Dictionary<GridCoord, EntityId>();
        var hasUsableInventory = template.InventoryWidth > 0 && template.InventoryHeight > 0;

        foreach (var carried in template.CarriedEntities)
        {
            if (!entityIds.Add(carried.EntityId))
            {
                var message = $"Entity template {templateId} ({template.Name}) has duplicate carried entity ID {carried.EntityId}.";
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.DuplicateCarriedEntityId,
                    message,
                    entityTemplateId: templateId,
                    carriedEntityId: carried.EntityId));
            }

            if (!hasUsableInventory)
            {
                var message = $"Entity template {templateId} ({template.Name}) carries {carried.EntityId} but has no usable inventory.";
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.CarriedEntityWithoutUsableInventory,
                    message,
                    entityTemplateId: templateId,
                    carriedEntityId: carried.EntityId));
                continue;
            }

            if (carried.Coord.X < 0
                || carried.Coord.Y < 0
                || carried.Coord.X >= template.InventoryWidth
                || carried.Coord.Y >= template.InventoryHeight)
            {
                var message = $"Entity template {templateId} ({template.Name}) carries {carried.EntityId} at {carried.Coord}, outside inventory bounds {template.InventoryWidth}x{template.InventoryHeight}.";
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InventoryOutOfBounds,
                    message,
                    entityTemplateId: templateId,
                    carriedEntityId: carried.EntityId,
                    coord: carried.Coord));
                continue;
            }

            if (occupiedCoords.TryGetValue(carried.Coord, out var existingEntityId))
            {
                var message = $"Entity template {templateId} ({template.Name}) carried entities {existingEntityId} and {carried.EntityId} overlap at {carried.Coord}.";
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InventoryOverlap,
                    message,
                    entityTemplateId: templateId,
                    carriedEntityId: carried.EntityId,
                    relatedEntityId: existingEntityId,
                    coord: carried.Coord));
                continue;
            }

            occupiedCoords[carried.Coord] = carried.EntityId;
        }
    }

    private static void ValidateActionPlanTemplateReference(
        List<ContentDiagnostic> diagnostics,
        IReadOnlyDictionary<ActionPlanTemplateId, ActionPlanDescriptor> actionPlanTemplates,
        EntityTemplateId templateId,
        EntityTemplate template,
        ActionPlanTemplateId? actionPlanTemplateId,
        string fieldName)
    {
        if (actionPlanTemplateId is { } id && !actionPlanTemplates.ContainsKey(id))
        {
            AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                ContentDiagnosticCode.MissingActionPlanReference,
                $"Entity template {templateId} ({template.Name}) references missing {fieldName} {id}.",
                entityTemplateId: templateId,
                actionPlanTemplateId: id));
        }
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
