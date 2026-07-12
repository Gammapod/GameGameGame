using GameGameGame.Core;

namespace GameGameGame.Content;

internal sealed class CarriedEntityLayoutEditor(EditableContentDocument document, Action? onChanged = null)
{
    public void PlaceCarriedEntity(EntityTemplateId parentTemplateId, EntityId entityId, EntityTemplateId templateId, GridCoord coord)
    {
        var placement = ValidateCarriedEntityPlacement(parentTemplateId, coord);
        if (!placement.IsSuccess)
        {
            throw new InvalidOperationException(placement.ErrorMessage);
        }

        var template = GetTemplateDto(parentTemplateId);
        template.CarriedEntities ??= [];
        template.CarriedEntities.Add(new EditableContentDocument.CarriedEntityTemplateDto
        {
            EntityId = entityId.Value,
            TemplateId = templateId.Value,
            Coord = EditableContentDocument.GridCoordDto.From(coord)
        });
        onChanged?.Invoke();
    }

    public EntityId PlaceCarriedEntity(EntityTemplateId parentTemplateId, EntityTemplateId templateId)
    {
        var coord = FindFirstOpenInventoryCell(parentTemplateId)
            ?? throw new InvalidOperationException($"Entity template {parentTemplateId} has no open inventory cell.");

        return PlaceCarriedEntity(parentTemplateId, templateId, coord);
    }

    public EntityId PlaceCarriedEntity(EntityTemplateId parentTemplateId, EntityTemplateId templateId, GridCoord coord)
    {
        var entityId = GenerateCarriedEntityId(parentTemplateId, templateId);
        PlaceCarriedEntity(parentTemplateId, entityId, templateId, coord);
        return entityId;
    }

    public IReadOnlyList<CarriedEntityEditorModel> ListCarriedEntities(EntityTemplateId parentTemplateId)
    {
        var registry = document.ToRegistry();
        var parent = registry.EntityTemplates[parentTemplateId];

        return (parent.CarriedEntities ?? [])
            .Where(carried => carried.TemplateId is not null)
            .Select(carried =>
            {
                var templateId = carried.TemplateId!.Value;
                return new CarriedEntityEditorModel(
                    carried.EntityId,
                    templateId,
                    carried.Coord,
                    registry.EntityTemplates[templateId],
                    registry.Presentations[templateId]);
            })
            .ToList();
    }

    public GridCoord? FindFirstOpenInventoryCell(EntityTemplateId parentTemplateId)
    {
        var template = GetTemplateDto(parentTemplateId);
        var occupied = (template.CarriedEntities ?? [])
            .Where(carried => carried.Coord is not null)
            .Select(carried => new GridCoord(carried.Coord!.X, carried.Coord.Y))
            .ToHashSet();

        for (var y = 0; y < template.InventoryHeight; y++)
        {
            for (var x = 0; x < template.InventoryWidth; x++)
            {
                var coord = new GridCoord(x, y);
                if (!occupied.Contains(coord))
                {
                    return coord;
                }
            }
        }

        return null;
    }

    public ContentEditorOperationResult ValidateCarriedEntityPlacement(
        EntityTemplateId parentTemplateId,
        GridCoord coord,
        EntityId? movingEntityId = null)
    {
        var template = GetTemplateDto(parentTemplateId);
        if (template.InventoryWidth <= 0 || template.InventoryHeight <= 0)
        {
            return ContentEditorOperationResult.Failure(
                $"Cannot place carried entity; {parentTemplateId} has no usable inventory.");
        }

        if (coord.X < 0 || coord.Y < 0 || coord.X >= template.InventoryWidth || coord.Y >= template.InventoryHeight)
        {
            return ContentEditorOperationResult.Failure(
                $"Cannot place carried entity at {coord.X},{coord.Y}; it is outside inventory bounds {template.InventoryWidth}x{template.InventoryHeight} for {parentTemplateId}.");
        }

        var carriedEntities = template.CarriedEntities ?? [];
        if (movingEntityId is not null && carriedEntities.All(carried => carried.EntityId != movingEntityId.Value.Value))
        {
            return ContentEditorOperationResult.Failure(
                $"Entity template {parentTemplateId} does not carry entity {movingEntityId.Value}.");
        }

        var occupant = carriedEntities.FirstOrDefault(carried =>
            carried.Coord is not null
            && carried.Coord.X == coord.X
            && carried.Coord.Y == coord.Y
            && (movingEntityId is null || carried.EntityId != movingEntityId.Value.Value));
        if (occupant is not null)
        {
            return ContentEditorOperationResult.Failure(
                $"Cannot place carried entity at {coord.X},{coord.Y}; cell is already occupied by {occupant.EntityId}.");
        }

        return ContentEditorOperationResult.Success();
    }

    public void MoveCarriedEntity(EntityTemplateId parentTemplateId, EntityId entityId, GridCoord coord)
    {
        var template = GetTemplateDto(parentTemplateId);
        var carried = template.CarriedEntities?.SingleOrDefault(carried => carried.EntityId == entityId.Value)
            ?? throw new InvalidOperationException($"Entity template {parentTemplateId} does not carry entity {entityId}.");
        var placement = ValidateCarriedEntityPlacement(parentTemplateId, coord, entityId);
        if (!placement.IsSuccess)
        {
            throw new InvalidOperationException(placement.ErrorMessage);
        }

        carried.Coord = EditableContentDocument.GridCoordDto.From(coord);
        onChanged?.Invoke();
    }

    public void RemoveCarriedEntity(EntityTemplateId parentTemplateId, EntityId entityId)
    {
        var template = GetTemplateDto(parentTemplateId);
        var carried = template.CarriedEntities?.SingleOrDefault(carried => carried.EntityId == entityId.Value)
            ?? throw new InvalidOperationException($"Entity template {parentTemplateId} does not carry entity {entityId}.");

        template.CarriedEntities!.Remove(carried);
        if (template.CarriedEntities.Count == 0)
        {
            template.CarriedEntities = null;
        }

        onChanged?.Invoke();
    }

    public void ReplaceCarriedEntityTemplate(EntityTemplateId parentTemplateId, EntityId entityId, EntityTemplateId templateId)
    {
        var template = GetTemplateDto(parentTemplateId);
        var carried = template.CarriedEntities?.SingleOrDefault(carried => carried.EntityId == entityId.Value)
            ?? throw new InvalidOperationException($"Entity template {parentTemplateId} does not carry entity {entityId}.");

        carried.TemplateId = templateId.Value;
        onChanged?.Invoke();
    }

    private EditableContentDocument.EntityTemplateDto GetTemplateDto(EntityTemplateId id) =>
        document.EntityTemplates.TryGetValue(id.Value, out var template)
            ? template
            : throw new InvalidOperationException($"Entity template {id} does not exist.");

    private EntityId GenerateCarriedEntityId(EntityTemplateId parentTemplateId, EntityTemplateId templateId)
    {
        var parentPrefix = ContentEditorIdHelpers.ToCamelCaseId(GetTemplateDto(parentTemplateId).Name ?? parentTemplateId.Value);
        var templateName = document.EntityTemplates.TryGetValue(templateId.Value, out var template)
            ? template.Name ?? templateId.Value
            : templateId.Value;
        var baseId = $"{parentPrefix}{ContentEditorIdHelpers.UppercaseFirst(ContentEditorIdHelpers.ToCamelCaseId(templateName))}";
        var candidate = baseId;
        var suffix = 2;
        var existingIds = (GetTemplateDto(parentTemplateId).CarriedEntities ?? [])
            .Select(carried => carried.EntityId)
            .ToHashSet();

        while (existingIds.Contains(candidate))
        {
            candidate = $"{baseId}{suffix}";
            suffix++;
        }

        return new EntityId(candidate);
    }

}
