using GameGameGame.Core;

namespace GameGameGame.Content;

internal sealed class EntityTemplateEditorService(EditableContentDocument document, Action? onChanged = null)
{
    public EntityTemplateId CreateEntityPreset(string name)
    {
        var id = document.AddEntityTemplate(
            name,
            new EntityTemplate(name, InventoryWidth: 0, InventoryHeight: 0, Bulk: 0, Aperture: 0),
            new EntityPresentation('?', PresentationColor.Gray));
        onChanged?.Invoke();

        return id;
    }

    public EntityTemplateId DuplicateEntityPreset(EntityTemplateId sourceId, string name)
    {
        var preset = GetEntityPreset(sourceId);
        var duplicateId = document.AddEntityTemplate(
            name,
            preset.Template with
            {
                Name = name,
                CarriedEntities = DuplicateCarriedEntities(preset.Template.CarriedEntities, name)
            },
            preset.Presentation);
        onChanged?.Invoke();

        return duplicateId;
    }

    public ContentEditorOperationResult DeleteEntityPreset(EntityTemplateId id)
    {
        var references = ListEntityTemplateReferences(id);

        if (references.Count > 0)
        {
            return ContentEditorOperationResult.Failure(
                $"Cannot delete entity template {id}; it is referenced by {string.Join(", ", references.Select(reference => reference.ToString()))}.");
        }

        document.EntityTemplates.Remove(id.Value);
        document.Presentations.Remove(id.Value);
        onChanged?.Invoke();

        return ContentEditorOperationResult.Success();
    }

    public void SetDefaultActionPlan(EntityTemplateId templateId, ActionPlanTemplateId actionPlanId)
    {
        var template = GetTemplateDto(templateId);
        template.DefaultActionPlanId = actionPlanId.Value;
        MaterializeBehaviorDefaults(template, GetActionPlanDto(actionPlanId).Behavior);
        onChanged?.Invoke();
    }

    public void ClearDefaultActionPlan(EntityTemplateId templateId)
    {
        GetTemplateDto(templateId).DefaultActionPlanId = null;
        onChanged?.Invoke();
    }

    public void UpdateEntityPreset(EntityTemplateId id, EntityTemplate template, EntityPresentation presentation)
    {
        document.EntityTemplates[id.Value] = EditableContentDocument.EntityTemplateDto.From(template);
        document.Presentations[id.Value] = EditableContentDocument.EntityPresentationDto.From(presentation);
        onChanged?.Invoke();
    }

    public void SetDefaultPlanVariable(EntityTemplateId templateId, string variableName, PlanValueDescriptor value)
    {
        var template = GetTemplateDto(templateId);
        template.DefaultPlanVariables ??= [];
        template.DefaultPlanVariables[variableName] = EditableContentDocument.PlanValueDescriptorDto.From(value);
        onChanged?.Invoke();
    }

    public IReadOnlyList<DefaultPlanVariableEditorModel> ListDefaultPlanVariables(EntityTemplateId templateId)
    {
        var template = GetEntityPreset(templateId).Template;
        return (template.DefaultPlanVariables ?? new Dictionary<string, PlanValueDescriptor>())
            .OrderBy(entry => entry.Key)
            .Select(entry => new DefaultPlanVariableEditorModel(entry.Key, entry.Value))
            .ToList();
    }

    public void RemoveDefaultPlanVariable(EntityTemplateId templateId, string variableName)
    {
        var template = GetTemplateDto(templateId);
        if (template.DefaultPlanVariables is null || !template.DefaultPlanVariables.Remove(variableName))
        {
            throw new InvalidOperationException($"Entity template {templateId} has no default variable {variableName}.");
        }

        if (template.DefaultPlanVariables.Count == 0)
        {
            template.DefaultPlanVariables = null;
        }

        onChanged?.Invoke();
    }

    public ActorActionStateDefaults GetActionStateDefaults(EntityTemplateId templateId)
    {
        var template = GetTemplateDto(templateId);

        return new ActorActionStateDefaults(
            template.ActionStateDefaults?.Facing,
            string.IsNullOrWhiteSpace(template.ActionStateDefaults?.Target) ? null : new EntityId(template.ActionStateDefaults.Target));
    }

    public void SetInitialFacing(EntityTemplateId templateId, Direction facing)
    {
        var template = GetTemplateDto(templateId);
        template.ActionStateDefaults ??= new EditableContentDocument.ActorActionStateDefaultsDto();
        template.ActionStateDefaults.Facing = facing;
        onChanged?.Invoke();
    }

    public void ClearInitialFacing(EntityTemplateId templateId)
    {
        var template = GetTemplateDto(templateId);
        if (template.ActionStateDefaults is null)
        {
            return;
        }

        template.ActionStateDefaults.Facing = null;
        if (template.ActionStateDefaults.Target is null)
        {
            template.ActionStateDefaults = null;
        }

        onChanged?.Invoke();
    }

    public IReadOnlyList<EntityTargetingRule> ListTargetingRules(EntityTemplateId templateId)
    {
        var template = GetEntityPreset(templateId).Template;
        return (template.TargetingRules ?? [])
            .OrderBy(rule => rule.Slot)
            .ToList();
    }

    public void SetTargetingRule(EntityTemplateId templateId, EntityTargetingRule rule)
    {
        if (rule.Slot <= 0)
        {
            throw new InvalidOperationException($"Entity template {templateId} targeting rule slot must be greater than zero.");
        }

        if (rule.Range < 0)
        {
            throw new InvalidOperationException($"Entity template {templateId} targeting rule slot {rule.Slot} range must be zero or greater.");
        }

        var template = GetTemplateDto(templateId);
        template.TargetingRules ??= [];
        template.TargetingRules.RemoveAll(existing => existing.Slot == rule.Slot);
        template.TargetingRules.Add(EditableContentDocument.EntityTargetingRuleDto.From(rule));
        template.TargetingRules = template.TargetingRules.OrderBy(existing => existing.Slot).ToList();
        onChanged?.Invoke();
    }

    public void RemoveTargetingRule(EntityTemplateId templateId, int slot)
    {
        var template = GetTemplateDto(templateId);
        if (template.TargetingRules is null || template.TargetingRules.RemoveAll(rule => rule.Slot == slot) == 0)
        {
            throw new InvalidOperationException($"Entity template {templateId} has no targeting rule slot {slot}.");
        }

        if (template.TargetingRules.Count == 0)
        {
            template.TargetingRules = null;
        }

        onChanged?.Invoke();
    }

    private EntityPresetEditorModel GetEntityPreset(EntityTemplateId id)
    {
        var registry = document.ToRegistry();

        return new EntityPresetEditorModel(id, registry.EntityTemplates[id], registry.Presentations[id]);
    }

    private IReadOnlyList<EntityTemplateReference> ListEntityTemplateReferences(EntityTemplateId id) =>
        document.EntityTemplates
            .SelectMany(source => (source.Value.CarriedEntities ?? [])
                .Where(carried => carried.TemplateId == id.Value)
                .Select(carried => new EntityTemplateReference(
                    new EntityTemplateId(source.Key),
                    carried.EntityId is null ? null : new EntityId(carried.EntityId))))
            .ToList();

    private EditableContentDocument.EntityTemplateDto GetTemplateDto(EntityTemplateId id) =>
        document.EntityTemplates.TryGetValue(id.Value, out var template)
            ? template
            : throw new InvalidOperationException($"Entity template {id} does not exist.");

    private EditableContentDocument.ActionPlanDescriptorDto GetActionPlanDto(ActionPlanTemplateId id) =>
        document.ActionPlans.TryGetValue(id.Value, out var plan)
            ? plan
            : throw new InvalidOperationException($"Action plan template {id} does not exist.");

    private static void MaterializeBehaviorDefaults(
        EditableContentDocument.EntityTemplateDto template,
        EditableContentDocument.ActionPlanBehaviorDescriptorDto? behavior)
    {
        if (behavior?.Steps is null || behavior.Steps.Count == 0)
        {
            return;
        }

        foreach (var step in behavior.Steps)
        {
            var metadata = ActionStepCatalog.Get(step.Kind);
            foreach (var defaultable in metadata.DefaultableState)
            {
                if (defaultable.Slot == ActionPlanSlot.Facing && defaultable.ValueKind == PlanValueKind.Direction)
                {
                    template.ActionStateDefaults ??= new EditableContentDocument.ActorActionStateDefaultsDto();
                    template.ActionStateDefaults.Facing ??= Direction.West;
                }
            }
        }
    }

    private static IReadOnlyList<CarriedEntityTemplate>? DuplicateCarriedEntities(
        IReadOnlyList<CarriedEntityTemplate>? carriedEntities,
        string duplicateName)
    {
        if (carriedEntities is null || carriedEntities.Count == 0)
        {
            return null;
        }

        var idPrefix = ContentEditorIdHelpers.ToCamelCaseId(duplicateName);
        return carriedEntities
            .Select(carried => new CarriedEntityTemplate(
                new EntityId($"{idPrefix}{ContentEditorIdHelpers.UppercaseFirst(carried.EntityId.Value)}"),
                carried.TemplateId ?? throw new InvalidOperationException($"Carried entity {carried.EntityId} has no template ID."),
                carried.Coord,
                carried.Controller))
            .ToList();
    }
}
