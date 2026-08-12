using GameGameGame.Core;

namespace GameGameGame.Content;

internal sealed record ContentReferenceIndex(
    IReadOnlyList<ContentSymbol> Symbols,
    IReadOnlyList<ContentReference> References)
{
    public static ContentReferenceIndex Build(EditableContentDocument document, ContentCompileOptions? options)
    {
        var symbols = new List<ContentSymbol>();
        var references = new List<ContentReference>();
        var entityTemplateIds = document.EntityTemplates.Keys.ToHashSet(StringComparer.Ordinal);
        var actionPlanIds = document.ActionPlans.Keys.ToHashSet(StringComparer.Ordinal);
        var presentationDefinitionIds = BuiltInPresentationCatalog.Presentations.Keys.Select(id => id.Value)
            .Concat((document.PresentationCatalog ?? []).Keys)
            .ToHashSet(StringComparer.Ordinal);
        var paletteIds = BuiltInPresentationCatalog.Palettes.Keys.Select(id => id.Value)
            .Concat((document.Palettes ?? []).Keys)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var (id, template) in document.EntityTemplates.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            symbols.Add(Symbol(ContentSymbolKind.EntityTemplate, id, template.Name ?? id, options));
            AddTemplateReferences(references, id, template, entityTemplateIds, actionPlanIds, options);
        }

        foreach (var (id, presentation) in document.Presentations.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            symbols.Add(Symbol(ContentSymbolKind.Presentation, id, id, options));
            references.Add(Reference(
                ContentReferenceKind.PresentationForTemplate,
                ContentSymbolKind.Presentation,
                id,
                ContentSymbolKind.EntityTemplate,
                id,
                Resolve(entityTemplateIds, id),
                options: options));

            if (!string.IsNullOrWhiteSpace(presentation.PresentationId))
            {
                references.Add(Reference(
                    ContentReferenceKind.PresentationId,
                    ContentSymbolKind.Presentation,
                    id,
                    ContentSymbolKind.PresentationDefinition,
                    presentation.PresentationId,
                    Resolve(presentationDefinitionIds, presentation.PresentationId),
                    options: options));
            }

            if (!string.IsNullOrWhiteSpace(presentation.PaletteId))
            {
                references.Add(Reference(
                    ContentReferenceKind.PaletteId,
                    ContentSymbolKind.Presentation,
                    id,
                    ContentSymbolKind.Palette,
                    presentation.PaletteId,
                    Resolve(paletteIds, presentation.PaletteId),
                    options: options));
            }
        }

        foreach (var (id, definition) in (document.PresentationCatalog ?? []).OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            symbols.Add(Symbol(ContentSymbolKind.PresentationDefinition, id, definition.Name ?? id, options));
        }

        foreach (var (id, definition) in (document.Palettes ?? []).OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            symbols.Add(Symbol(ContentSymbolKind.Palette, id, definition.Name ?? id, options));
        }

        foreach (var (id, plan) in document.ActionPlans.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            symbols.Add(Symbol(ContentSymbolKind.ActionPlan, id, plan.Id ?? id, options));
            AddActionPlanReferences(references, id, plan, entityTemplateIds, actionPlanIds, options);
        }

        foreach (var (id, scenario) in document.Scenarios.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            symbols.Add(Symbol(ContentSymbolKind.Scenario, id, scenario.Name ?? id, options));
            AddScenarioReferences(references, id, scenario, entityTemplateIds, options);
        }

        foreach (var (id, layer) in document.MergedLayers.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            symbols.Add(Symbol(ContentSymbolKind.MergedInventoryLayer, id, id, options));
            AddMergedLayerReferences(references, id, layer, options);
        }

        foreach (var symbol in CollectAuthoredEntityInstanceSymbols(document, options))
        {
            symbols.Add(symbol);
        }

        return new ContentReferenceIndex(symbols, references);
    }

    private static void AddTemplateReferences(
        List<ContentReference> references,
        string sourceId,
        EditableContentDocument.EntityTemplateDto template,
        HashSet<string> entityTemplateIds,
        HashSet<string> actionPlanIds,
        ContentCompileOptions? options)
    {
        if (!string.IsNullOrWhiteSpace(template.DefaultActionPlanId))
        {
            references.Add(Reference(
                ContentReferenceKind.DefaultActionPlan,
                ContentSymbolKind.EntityTemplate,
                sourceId,
                ContentSymbolKind.ActionPlan,
                template.DefaultActionPlanId,
                Resolve(actionPlanIds, template.DefaultActionPlanId),
                options: options));
        }

        foreach (var carried in template.CarriedEntities ?? [])
        {
            if (!string.IsNullOrWhiteSpace(carried.TemplateId))
            {
                references.Add(Reference(
                    ContentReferenceKind.CarriedEntityTemplate,
                    ContentSymbolKind.EntityTemplate,
                    sourceId,
                    ContentSymbolKind.EntityTemplate,
                    carried.TemplateId,
                    Resolve(entityTemplateIds, carried.TemplateId),
                    relatedEntityId: string.IsNullOrWhiteSpace(carried.EntityId) ? null : new EntityId(carried.EntityId),
                    options: options));
            }
        }

        foreach (var rule in template.TargetingRules ?? [])
        {
            AddTargetingRuleReference(references, sourceId, rule, entityTemplateIds, options);
        }

        foreach (var rule in template.Targeting?.Rules ?? [])
        {
            AddTargetingRuleReference(references, sourceId, rule, entityTemplateIds, options);
        }
    }

    private static void AddTargetingRuleReference(
        List<ContentReference> references,
        string sourceId,
        EditableContentDocument.EntityTargetingRuleDto rule,
        HashSet<string> entityTemplateIds,
        ContentCompileOptions? options)
    {
        if (string.IsNullOrWhiteSpace(rule.TargetTemplateId))
        {
            return;
        }

        references.Add(Reference(
            ContentReferenceKind.TargetingTargetTemplate,
            ContentSymbolKind.EntityTemplate,
            sourceId,
            ContentSymbolKind.EntityTemplate,
            rule.TargetTemplateId,
            Resolve(entityTemplateIds, rule.TargetTemplateId),
            options: options));
    }

    private static void AddActionPlanReferences(
        List<ContentReference> references,
        string sourceId,
        EditableContentDocument.ActionPlanDescriptorDto plan,
        HashSet<string> entityTemplateIds,
        HashSet<string> actionPlanIds,
        ContentCompileOptions? options)
    {
        if (plan.Primitive?.FallbackPlanId is { } fallbackPlanId)
        {
            references.Add(Reference(
                ContentReferenceKind.BehaviorStepPlan,
                ContentSymbolKind.ActionPlan,
                sourceId,
                ContentSymbolKind.ActionPlan,
                fallbackPlanId,
                Resolve(actionPlanIds, fallbackPlanId),
                options: options));
        }

        for (var index = 0; index < (plan.Behavior?.Steps ?? []).Count; index++)
        {
            var step = plan.Behavior!.Steps![index];
            if (!string.IsNullOrWhiteSpace(step.PlanId))
            {
                references.Add(Reference(
                    ContentReferenceKind.BehaviorStepPlan,
                    ContentSymbolKind.ActionPlan,
                    sourceId,
                    ContentSymbolKind.ActionPlan,
                    step.PlanId,
                    Resolve(actionPlanIds, step.PlanId),
                    stepIndex: index,
                    options: options));
            }

            if (!string.IsNullOrWhiteSpace(step.TemplateId))
            {
                references.Add(Reference(
                    ContentReferenceKind.BehaviorStepTemplate,
                    ContentSymbolKind.ActionPlan,
                    sourceId,
                    ContentSymbolKind.EntityTemplate,
                    step.TemplateId,
                    Resolve(entityTemplateIds, step.TemplateId),
                    stepIndex: index,
                    options: options));
            }

            foreach (var cost in step.Costs ?? [])
            {
                if (string.IsNullOrWhiteSpace(cost.TemplateId))
                {
                    continue;
                }

                references.Add(Reference(
                    ContentReferenceKind.BehaviorStepCostTemplate,
                    ContentSymbolKind.ActionPlan,
                    sourceId,
                    ContentSymbolKind.EntityTemplate,
                    cost.TemplateId,
                    Resolve(entityTemplateIds, cost.TemplateId),
                    stepIndex: index,
                    options: options));
            }
        }

        for (var index = 0; index < (plan.Steps ?? []).Count; index++)
        {
            var step = plan.Steps![index];
            foreach (var effect in new[] { step.OnSuccess, step.OnFailure })
            {
                if (effect?.PlanId is { } planId)
                {
                    references.Add(Reference(
                        ContentReferenceKind.BehaviorStepPlan,
                        ContentSymbolKind.ActionPlan,
                        sourceId,
                        ContentSymbolKind.ActionPlan,
                        planId,
                        Resolve(actionPlanIds, planId),
                        stepIndex: index,
                        options: options));
                }
            }
        }
    }

    private static void AddScenarioReferences(
        List<ContentReference> references,
        string sourceId,
        EditableContentDocument.ScenarioDefinitionDto scenario,
        HashSet<string> entityTemplateIds,
        ContentCompileOptions? options)
    {
        if (!string.IsNullOrWhiteSpace(scenario.ScenarioRootEntityTemplateId))
        {
            references.Add(Reference(
                ContentReferenceKind.ScenarioRootTemplate,
                ContentSymbolKind.Scenario,
                sourceId,
                ContentSymbolKind.EntityTemplate,
                scenario.ScenarioRootEntityTemplateId,
                Resolve(entityTemplateIds, scenario.ScenarioRootEntityTemplateId),
                options: options));
        }

        if (!string.IsNullOrWhiteSpace(scenario.PlayerEntityTemplateId))
        {
            references.Add(Reference(
                ContentReferenceKind.ScenarioPlayerTemplate,
                ContentSymbolKind.Scenario,
                sourceId,
                ContentSymbolKind.EntityTemplate,
                scenario.PlayerEntityTemplateId,
                Resolve(entityTemplateIds, scenario.PlayerEntityTemplateId),
                options: options));
        }
    }

    private static void AddMergedLayerReferences(
        List<ContentReference> references,
        string sourceId,
        EditableContentDocument.MergedInventoryLayerDto layer,
        ContentCompileOptions? options)
    {
        foreach (var space in layer.Spaces ?? [])
        {
            if (string.IsNullOrWhiteSpace(space.Owner))
            {
                continue;
            }

            references.Add(Reference(
                ContentReferenceKind.MergedLayerOwner,
                ContentSymbolKind.MergedInventoryLayer,
                sourceId,
                ContentSymbolKind.AuthoredEntityInstance,
                space.Owner,
                ContentReferenceResolution.Resolved,
                relatedEntityId: new EntityId(space.Owner),
                options: options));
        }
    }

    private static IEnumerable<ContentSymbol> CollectAuthoredEntityInstanceSymbols(EditableContentDocument document, ContentCompileOptions? options)
    {
        foreach (var (_, template) in document.EntityTemplates)
        {
            foreach (var carried in template.CarriedEntities ?? [])
            {
                if (!string.IsNullOrWhiteSpace(carried.EntityId))
                {
                    yield return Symbol(ContentSymbolKind.AuthoredEntityInstance, carried.EntityId, carried.EntityId, options);
                }
            }
        }
    }

    private static ContentSymbol Symbol(ContentSymbolKind kind, string id, string displayName, ContentCompileOptions? options) =>
        new(kind, id, displayName, options?.DocumentId, options?.SourcePath);

    private static ContentReference Reference(
        ContentReferenceKind kind,
        ContentSymbolKind sourceKind,
        string sourceId,
        ContentSymbolKind targetKind,
        string targetId,
        ContentReferenceResolution resolution,
        int? stepIndex = null,
        EntityId? relatedEntityId = null,
        ContentCompileOptions? options = null) =>
        new(kind, sourceKind, sourceId, targetKind, targetId, resolution, stepIndex, relatedEntityId, options?.DocumentId, options?.SourcePath);

    private static ContentReferenceResolution Resolve(HashSet<string> ids, string id) =>
        ids.Contains(id) ? ContentReferenceResolution.Resolved : ContentReferenceResolution.Missing;
}
