using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed class FrontendEditorSnapshotBuilder(ContentEditorSession session)
{
    public FrontendEditorSnapshot Build()
    {
        var validation = session.Editor.Validate();
        var canonicalValidation = session.Document.ValidateCanonicalAuthoring();
        var diagnostics = validation.Diagnostics
            .Concat(canonicalValidation.Diagnostics)
            .Select(FrontendEditorDiagnostic.From)
            .ToList();

        return new FrontendEditorSnapshot(
            session.FilePath,
            session.IsDirty,
            ListScenarios(),
            ListEntityTemplates(diagnostics),
            ListActionPlans(),
            ListMergedInventoryLayers(),
            ListAvailableActionSteps(),
            diagnostics,
            session.GetYamlPreview(),
            session.GetYamlDiff().Lines);
    }

    private IReadOnlyList<FrontendEditorScenarioSummary> ListScenarios() =>
        session.Document.Scenarios
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry =>
            {
                var scenario = entry.Value.ToDefinition(entry.Key);
                return new FrontendEditorScenarioSummary(
                    scenario.ScenarioId,
                    scenario.Name,
                    scenario.ScenarioRootEntityTemplateId.Value,
                    scenario.PlayerEntityTemplateId?.Value,
                    scenario.PlayerEntityId?.Value,
                    scenario.PlayerStart ?? new GridCoord(0, 0),
                    scenario.PlayerControls.ToDictionary(
                        entry => entry.Key,
                        entry => (IReadOnlyList<string>)entry.Value.Select(entityId => entityId.Value).ToList(),
                        StringComparer.Ordinal))
                {
                    AuthoredPlayerStart = scenario.PlayerStart
                };
            })
            .ToList();

    private IReadOnlyList<FrontendEditorMergedInventoryLayerSummary> ListMergedInventoryLayers() =>
        session.Editor.ListMergedInventoryLayers()
            .Select(layer => new FrontendEditorMergedInventoryLayerSummary(
                layer.Id.Value,
                layer.Spaces
                    .Select(space => new FrontendEditorMergedInventorySpaceSummary(space.OwnerId.Value, space.Origin))
                    .ToList()))
            .ToList();

    private IReadOnlyList<FrontendEditorEntityTemplateSummary> ListEntityTemplates(IReadOnlyList<FrontendEditorDiagnostic> diagnostics) =>
        session.Editor.ListEntityPresets()
            .Select(model =>
            {
                var targetingSource = model.Template.Targeting is not null
                    ? FrontendEditorTargetingSource.TargetingProfile
                    : (model.Template.TargetingRules is { Count: > 0 } ? FrontendEditorTargetingSource.LegacyTargetingRules : FrontendEditorTargetingSource.None);
                var profileDefaultLocality = model.Template.Targeting?.DefaultLocality?.Origins ?? [TargetingLocalityOrigin.CurrentPlace];
                var effectiveRules = model.Template.Targeting is { } profile
                    ? profile.Rules.Select(rule => (Rule: rule, Range: profile.Range, EffectiveLocality: rule.Locality?.Origins ?? profileDefaultLocality))
                    : (model.Template.TargetingRules ?? []).Select(rule => (Rule: rule, Range: rule.Range, EffectiveLocality: rule.Locality?.Origins ?? (IReadOnlyList<TargetingLocalityOrigin>)[TargetingLocalityOrigin.CurrentPlace]));
                var targetingRules = effectiveRules
                    .OrderBy(entry => entry.Rule.Slot)
                    .ThenBy(entry => entry.Rule.Label ?? string.Empty, StringComparer.Ordinal)
                    .Select(entry => new FrontendEditorTargetingRuleSummary(
                        entry.Rule.Slot,
                        entry.Rule.Label,
                        entry.Rule.Hint,
                        entry.Rule.TargetTemplateId?.Value,
                        entry.Rule.TargetTemplateId is { } targetTemplateId ? TryGetTemplateName(targetTemplateId.Value) : null,
                        entry.Range,
                        entry.Rule.TargetCapabilities,
                        entry.Rule.Locality?.Origins,
                        entry.EffectiveLocality))
                    .ToList();
                var targetRequirements = GetTargetingRequirements(model.Template.DefaultActionPlanId, targetingRules);
                var requirementLabels = targetRequirements
                    .Select(requirement => requirement.Label)
                    .ToHashSet(StringComparer.Ordinal);
                var orphanedRules = model.Template.DefaultActionPlanId is null
                    ? []
                    : targetingRules
                        .Where(rule => rule.Label is null || !requirementLabels.Contains(rule.Label))
                        .ToList();

                return new FrontendEditorEntityTemplateSummary(
                    model.Id.Value,
                    model.Template.Name,
                    model.Presentation.Glyph,
                    model.Presentation.Color,
                    model.Template.InventoryWidth,
                    model.Template.InventoryHeight,
                    model.Template.Bulk,
                    model.Template.Aperture,
                    model.Template.DefaultActionPlanId?.Value,
                    new FrontendEditorActionStateDefaultsSummary(
                        model.Template.ActionStateDefaults?.Facing,
                        model.Template.ActionStateDefaults?.Target?.Value),
                    targetingRules,
                    (model.Template.CarriedEntities ?? [])
                        .OrderBy(carried => carried.Coord.Y)
                        .ThenBy(carried => carried.Coord.X)
                        .ThenBy(carried => carried.EntityId.Value, StringComparer.Ordinal)
                        .Select(carried => new FrontendEditorCarriedEntitySummary(
                            carried.EntityId.Value,
                            carried.TemplateId?.Value,
                            carried.TemplateId is null ? null : TryGetTemplateName(carried.TemplateId.Value.Value),
                            carried.TemplateId is null ? null : TryGetGlyph(carried.TemplateId.Value.Value),
                            carried.TemplateId is null ? null : TryGetColor(carried.TemplateId.Value.Value),
                            carried.Coord,
                            diagnostics
                                .Where(diagnostic => diagnostic.EntityTemplateId == model.Id.Value
                                    && diagnostic.CarriedEntityId == carried.EntityId.Value)
                                .ToList(),
                            carried.Controller))
                        .Select(summary => summary with
                        {
                            PresentationId = summary.TemplateId is null ? null : TryGetPresentationId(summary.TemplateId),
                            PaletteId = summary.TemplateId is null ? null : TryGetPaletteId(summary.TemplateId)
                        })
                        .ToList(),
                    diagnostics
                        .Where(diagnostic => diagnostic.EntityTemplateId == model.Id.Value)
                        .ToList())
                {
                    PresentationId = model.Presentation.PresentationId,
                    PaletteId = model.Presentation.PaletteId,
                    EnterPolicy = model.Template.EnterPolicy,
                    EffectiveEnterPolicy = model.Template.EffectiveEnterPolicy,
                    ExitPolicy = model.Template.ExitPolicy,
                    EffectiveExitPolicy = model.Template.EffectiveExitPolicy,
                    TopologyPolicy = model.Template.TopologyPolicy,
                    TargetingRequirements = targetRequirements,
                    OrphanedTargetingRules = orphanedRules,
                    TargetingSource = targetingSource,
                    TargetingProfile = model.Template.Targeting is null
                        ? null
                        : new FrontendEditorTargetingProfileSummary(model.Template.Targeting.Range, profileDefaultLocality)
                };
            })
            .ToList();

    private IReadOnlyList<FrontendEditorTargetingRequirementSummary> GetTargetingRequirements(
        ActionPlanTemplateId? defaultActionPlanId,
        IReadOnlyList<FrontendEditorTargetingRuleSummary> targetingRules)
    {
        if (defaultActionPlanId is null
            || !session.Document.ActionPlans.TryGetValue(defaultActionPlanId.Value.Value, out var plan))
        {
            return [];
        }

        var rulesByLabel = targetingRules
            .Where(rule => rule.Label is not null)
            .GroupBy(rule => rule.Label!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.OrderBy(rule => rule.Slot).First(), StringComparer.Ordinal);

        return ActionPlanTargetLabelRequirementProjection.Project(plan.ToDescriptor(defaultActionPlanId.Value.Value))
            .Select(requirement =>
            {
                rulesByLabel.TryGetValue(requirement.Label, out var rule);
                return new FrontendEditorTargetingRequirementSummary(
                    requirement.Label,
                    requirement.StepIndexes,
                    requirement.StepKinds,
                    rule is not null,
                    rule);
            })
            .ToList();
    }

    private string? TryGetTemplateName(string templateId) =>
        session.Document.EntityTemplates.TryGetValue(templateId, out var template)
            ? template.Name ?? templateId
            : null;

    private char? TryGetGlyph(string templateId) =>
        session.Document.Presentations.TryGetValue(templateId, out var presentation)
            && !string.IsNullOrEmpty(presentation.Glyph)
                ? presentation.Glyph[0]
                : null;

    private PresentationId? TryGetPresentationId(string templateId) =>
        session.Document.Presentations.TryGetValue(templateId, out var presentation)
            && !string.IsNullOrWhiteSpace(presentation.PresentationId)
                ? new PresentationId(presentation.PresentationId)
                : null;

    private PaletteId? TryGetPaletteId(string templateId) =>
        session.Document.Presentations.TryGetValue(templateId, out var presentation)
            && !string.IsNullOrWhiteSpace(presentation.PaletteId)
                ? new PaletteId(presentation.PaletteId)
                : null;

    private PresentationColor? TryGetColor(string templateId) =>
        session.Document.Presentations.TryGetValue(templateId, out var presentation)
            ? presentation.Color
            : null;

    private IReadOnlyList<FrontendEditorActionPlanSummary> ListActionPlans() =>
        session.Editor.ListActionPlans()
            .Select(model => new FrontendEditorActionPlanSummary(
                model.TemplateId.Value,
                ContentEditorService.FormatActionPlanShape(ActionPlanShapeClassifier.Classify(model.Descriptor)),
                GetActionSteps(model.Descriptor),
                GetActionStepNames(model.Descriptor))
            {
                TargetLabelRequirements = ActionPlanTargetLabelRequirementProjection.Project(model.Descriptor)
                    .Select(requirement => new FrontendEditorActionPlanTargetLabelRequirementSummary(
                        requirement.Label,
                        requirement.StepIndexes,
                        requirement.StepKinds))
                    .ToList()
            })
            .ToList();

    private IReadOnlyList<FrontendEditorAvailableActionStepSummary> ListAvailableActionSteps() =>
        session.Editor.ListActionSteps()
            .Select(step => new FrontendEditorAvailableActionStepSummary(
                step.Kind,
                step.DisplayName,
                step.Description))
            .ToList();

    private IReadOnlyList<FrontendEditorActionPlanStepSummary> GetActionSteps(ActionPlanDescriptor descriptor)
    {
        if (descriptor.Behavior?.Steps.Count > 0)
        {
            return descriptor.Behavior.Steps
                .Select((step, index) =>
                {
                    var metadata = ActionStepCatalog.Get(step.Kind);
                    var consumesTargetReference = metadata.RequiredState
                        .Any(state => state.Slot == ActionPlanSlot.Target);
                    return new FrontendEditorActionPlanStepSummary(
                        index,
                        step.Kind,
                        metadata.DisplayName,
                        step.TargetLabel,
                        step.TargetSlot,
                        step.TargetSelf,
                        consumesTargetReference,
                        step.Costs,
                        FormatCostSummary(step.Costs),
                        step.PathMode,
                        step.DesiredDistance,
                        step.OrbitDirection);
                })
                .ToList();
        }

        return [];
    }

    private string? FormatCostSummary(IReadOnlyList<ActionStepCostDescriptor> costs)
    {
        if (costs.Count == 0)
        {
            return null;
        }

        return "Cost: " + string.Join(", ", costs.Select(cost => $"{cost.Quantity}× {TryGetTemplateName(cost.TemplateId) ?? cost.TemplateId}"));
    }

    private static IReadOnlyList<string> GetActionStepNames(ActionPlanDescriptor descriptor)
    {
        if (descriptor.Behavior?.Steps.Count > 0)
        {
            return descriptor.Behavior.Steps
                .Select(step => ActionStepCatalog.Get(step.Kind).DisplayName)
                .ToList();
        }

        if (descriptor.Primitive is { } primitive)
        {
            return [primitive.Kind.ToString()];
        }

        if (descriptor.Steps.Count > 0)
        {
            return descriptor.Steps.Select(step => step.Label).ToList();
        }

        return [];
    }
}
