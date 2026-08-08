using GameGameGame.Core;

namespace GameGameGame.Content;

public static class MergedInventoryLayerDocumentMapper
{
    public static MergedInventoryLayerDefinition ToDefinition(
        MergedInventoryLayerId id,
        EditableContentDocument.MergedInventoryLayerDto dto) =>
        new(
            id,
            (dto.Spaces ?? [])
                .Select(space => new MergedInventorySpaceContribution(
                    new EntityId(Required(space.Owner, nameof(space.Owner))),
                    ToCoord(space.Origin)))
                .ToList());

    public static EditableContentDocument.MergedInventoryLayerDto ToDto(MergedInventoryLayerDefinition layer) => new()
    {
        Spaces = layer.Spaces
            .Select(space => new EditableContentDocument.MergedInventorySpaceContributionDto
            {
                Owner = space.OwnerId.Value,
                Origin = EditableContentDocument.GridCoordDto.From(space.Origin)
            })
            .ToList()
    };

    private static GridCoord ToCoord(EditableContentDocument.GridCoordDto? coord) =>
        coord is null ? throw Missing(nameof(coord)) : new GridCoord(coord.X, coord.Y);

    private static string Required(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw Missing(name) : value;

    private static InvalidOperationException Missing(string name) =>
        new($"YAML content field {name} is required.");
}
