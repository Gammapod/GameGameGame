using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed record MergedInventoryResolvedJoinEndpoint(EntityId OwnerId, GridCoord SourceCoord, Direction Direction);

public sealed record MergedInventoryResolvedSourceCellLink(
    MergedInventoryResolvedJoinEndpoint First,
    MergedInventoryResolvedJoinEndpoint Second);

public static class MergedInventoryAlignedJoinResolver
{
    public static IReadOnlyList<MergedInventoryResolvedSourceCellLink> Resolve(
        MergedInventoryLayerDefinition layer,
        IReadOnlyDictionary<EntityId, EntityTemplate> templatesByOwnerId,
        IList<string>? errors = null)
    {
        var result = new List<MergedInventoryResolvedSourceCellLink>();
        foreach (var join in layer.Joins ?? [])
        {
            if (!TryResolveEndpoint(layer.Id, join.From, templatesByOwnerId, errors, out var from) ||
                !TryResolveEndpoint(layer.Id, join.To, templatesByOwnerId, errors, out var to))
            {
                continue;
            }

            if (join.Align != MergedInventoryJoinAlignment.Center)
            {
                errors?.Add($"Merged inventory layer {layer.Id} join from {join.From.OwnerId} {join.From.Edge} to {join.To.OwnerId} {join.To.Edge} uses unsupported alignment {join.Align}.");
                continue;
            }

            result.Add(new MergedInventoryResolvedSourceCellLink(from, to));
        }

        return result;
    }

    private static bool TryResolveEndpoint(
        MergedInventoryLayerId layerId,
        MergedInventoryJoinEndpoint endpoint,
        IReadOnlyDictionary<EntityId, EntityTemplate> templatesByOwnerId,
        IList<string>? errors,
        out MergedInventoryResolvedJoinEndpoint resolved)
    {
        if (!templatesByOwnerId.TryGetValue(endpoint.OwnerId, out var template))
        {
            errors?.Add($"Merged inventory layer {layerId} join references unknown owner entity {endpoint.OwnerId}.");
            resolved = default!;
            return false;
        }

        if (!template.HasUsableInventory())
        {
            errors?.Add($"Merged inventory layer {layerId} join owner {endpoint.OwnerId} has no usable inventory space.");
            resolved = default!;
            return false;
        }

        if (!IsCardinal(endpoint.Edge))
        {
            errors?.Add($"Merged inventory layer {layerId} join owner {endpoint.OwnerId} edge {endpoint.Edge} must be cardinal for the first aligned-join slice.");
            resolved = default!;
            return false;
        }

        resolved = new MergedInventoryResolvedJoinEndpoint(endpoint.OwnerId, CenterEdgeCoord(template, endpoint.Edge), endpoint.Edge);
        return true;
    }

    private static GridCoord CenterEdgeCoord(EntityTemplate template, Direction edge) => edge switch
    {
        Direction.North => new GridCoord(template.InventoryWidth / 2, 0),
        Direction.East => new GridCoord(template.InventoryWidth - 1, template.InventoryHeight / 2),
        Direction.South => new GridCoord(template.InventoryWidth / 2, template.InventoryHeight - 1),
        Direction.West => new GridCoord(0, template.InventoryHeight / 2),
        _ => new GridCoord(0, 0)
    };

    private static bool IsCardinal(Direction direction) =>
        direction is Direction.North or Direction.East or Direction.South or Direction.West;
}
