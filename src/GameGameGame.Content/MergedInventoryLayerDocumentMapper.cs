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
                .ToList(),
            (dto.Joins ?? [])
                .Select(join => new MergedInventoryAlignedJoin(
                    ToEndpoint(join.From),
                    ToEndpoint(join.To),
                    join.Align))
                .ToList());

    public static EditableContentDocument.MergedInventoryLayerDto ToDto(MergedInventoryLayerDefinition layer) => new()
    {
        Spaces = layer.Spaces
            .Select(space => new EditableContentDocument.MergedInventorySpaceContributionDto
            {
                Owner = space.OwnerId.Value,
                Origin = EditableContentDocument.GridCoordDto.From(space.Origin)
            })
            .ToList(),
        Joins = (layer.Joins ?? [])
            .Select(join => new EditableContentDocument.MergedInventoryAlignedJoinDto
            {
                From = ToDto(join.From),
                To = ToDto(join.To),
                Align = join.Align
            })
            .ToList()
    };

    private static MergedInventoryJoinEndpoint ToEndpoint(EditableContentDocument.MergedInventoryJoinEndpointDto? endpoint) =>
        endpoint is null
            ? throw Missing(nameof(endpoint))
            : new MergedInventoryJoinEndpoint(new EntityId(Required(endpoint.Owner, nameof(endpoint.Owner))), endpoint.Edge);

    private static EditableContentDocument.MergedInventoryJoinEndpointDto ToDto(MergedInventoryJoinEndpoint endpoint) => new()
    {
        Owner = endpoint.OwnerId.Value,
        Edge = endpoint.Edge
    };

    private static GridCoord ToCoord(EditableContentDocument.GridCoordDto? coord) =>
        coord is null ? throw Missing(nameof(coord)) : new GridCoord(coord.X, coord.Y);

    private static string Required(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw Missing(name) : value;

    private static InvalidOperationException Missing(string name) =>
        new($"YAML content field {name} is required.");
}
