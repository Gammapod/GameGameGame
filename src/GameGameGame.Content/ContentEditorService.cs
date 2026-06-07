using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed class ContentEditorService(EditableContentDocument document)
{
    public EditableContentDocument Document { get; } = document;

    public IReadOnlyList<EntityPresetEditorModel> ListEntityPresets()
    {
        var registry = Document.ToRegistry();

        return registry.EntityTemplates
            .OrderBy(entry => entry.Key.Value)
            .Select(entry => new EntityPresetEditorModel(
                entry.Key,
                entry.Value,
                registry.Presentations[entry.Key]))
            .ToList();
    }

    public ContentValidationResult Validate() => Document.ToRegistry().Validate();

    public EntityPresetEditorModel GetEntityPreset(EntityTemplateId id)
    {
        var registry = Document.ToRegistry();

        return new EntityPresetEditorModel(
            id,
            registry.EntityTemplates[id],
            registry.Presentations[id]);
    }

    public void UpdateEntityPreset(EntityTemplateId id, EntityTemplate template, EntityPresentation presentation)
    {
        Document.EntityTemplates[id.Value] = EditableContentDocument.EntityTemplateDto.From(template);
        Document.Presentations[id.Value] = EditableContentDocument.EntityPresentationDto.From(presentation);
    }

    public void PlaceCarriedEntity(EntityTemplateId parentTemplateId, EntityId entityId, EntityTemplateId templateId, GridCoord coord)
    {
        var template = GetTemplateDto(parentTemplateId);
        template.CarriedEntities ??= [];
        template.CarriedEntities.Add(new EditableContentDocument.CarriedEntityTemplateDto
        {
            EntityId = entityId.Value,
            TemplateId = templateId.Value,
            Coord = EditableContentDocument.GridCoordDto.From(coord)
        });
    }

    public void MoveCarriedEntity(EntityTemplateId parentTemplateId, EntityId entityId, GridCoord coord)
    {
        var template = GetTemplateDto(parentTemplateId);
        var carried = template.CarriedEntities?.SingleOrDefault(carried => carried.EntityId == entityId.Value)
            ?? throw new InvalidOperationException($"Entity template {parentTemplateId} does not carry entity {entityId}.");

        carried.Coord = EditableContentDocument.GridCoordDto.From(coord);
    }

    public IReadOnlyList<ActionPlanEditorModel> ListActionPlans()
    {
        var registry = Document.ToRegistry();

        return registry.ActionPlanDescriptors
            .OrderBy(entry => entry.Key.Value)
            .Select(entry => new ActionPlanEditorModel(entry.Key, entry.Value))
            .ToList();
    }

    public void AddActionPlanStep(ActionPlanTemplateId planId, ActionPlanStepDescriptor step)
    {
        var plan = GetActionPlanDto(planId);
        plan.Steps ??= [];
        plan.Steps.Add(EditableContentDocument.ActionPlanStepDescriptorDto.From(step));
    }

    public void UpdateActionPlanStep(ActionPlanTemplateId planId, int index, ActionPlanStepDescriptor step)
    {
        var steps = GetActionPlanSteps(planId);
        steps[index] = EditableContentDocument.ActionPlanStepDescriptorDto.From(step);
    }

    public void MoveActionPlanStep(ActionPlanTemplateId planId, int fromIndex, int toIndex)
    {
        var steps = GetActionPlanSteps(planId);
        var step = steps[fromIndex];
        steps.RemoveAt(fromIndex);
        steps.Insert(toIndex, step);
    }

    public void RemoveActionPlanStep(ActionPlanTemplateId planId, int index)
    {
        GetActionPlanSteps(planId).RemoveAt(index);
    }

    public void SetDefaultPlanVariable(EntityTemplateId templateId, string variableName, PlanValueDescriptor value)
    {
        var template = GetTemplateDto(templateId);
        template.DefaultPlanVariables ??= [];
        template.DefaultPlanVariables[variableName] = EditableContentDocument.PlanValueDescriptorDto.From(value);
    }

    private EditableContentDocument.EntityTemplateDto GetTemplateDto(EntityTemplateId id) =>
        Document.EntityTemplates.TryGetValue(id.Value, out var template)
            ? template
            : throw new InvalidOperationException($"Entity template {id} does not exist.");

    private EditableContentDocument.ActionPlanDescriptorDto GetActionPlanDto(ActionPlanTemplateId id) =>
        Document.ActionPlans.TryGetValue(id.Value, out var plan)
            ? plan
            : throw new InvalidOperationException($"Action plan template {id} does not exist.");

    private List<EditableContentDocument.ActionPlanStepDescriptorDto> GetActionPlanSteps(ActionPlanTemplateId id)
    {
        var plan = GetActionPlanDto(id);
        plan.Steps ??= [];

        return plan.Steps;
    }
}

public sealed record EntityPresetEditorModel(
    EntityTemplateId Id,
    EntityTemplate Template,
    EntityPresentation Presentation);

public sealed record ActionPlanEditorModel(
    ActionPlanTemplateId TemplateId,
    ActionPlanDescriptor Descriptor);
