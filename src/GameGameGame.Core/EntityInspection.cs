namespace GameGameGame.Core;

public sealed record EntityInspectionPanel(
    EntityId EntityId,
    string Name,
    char Glyph,
    ConsoleColor Color,
    string Address,
    IReadOnlyList<EntityInspectionProperty> Properties,
    InventoryInspectionGrid? InventoryGrid);

public sealed record EntityInspectionProperty(string Name, string Value);

public sealed record InventoryInspectionGrid(
    PlaneId PlaneId,
    int Width,
    int Height,
    IReadOnlyList<InventoryInspectionCell> Cells);

public sealed record InventoryInspectionCell(GridCoord Coord, EntityId? EntityId, char Glyph, ConsoleColor Color);

public sealed class EntityInspectionService
{
    public EntityInspectionPanel Inspect(WorldState world, EntityId entityId)
    {
        var entity = world.Entities[entityId];
        var weight = new WeightService();

        var properties = new List<EntityInspectionProperty>
        {
            new("Id", entity.Id.ToString()),
            new("Location", world.GetEntityLocation(entityId).ToString()),
            new("Weight", entity.Weight.ToString()),
            new("Total Weight", weight.GetTotalWeight(world, entityId).ToString()),
            new("Carried Weight", weight.GetCarriedWeight(world, entityId).ToString()),
            new("Carrying Capacity", entity.CarryingCapacity.ToString()),
            new("Inventory Dimensions", $"{entity.InventoryWidth}x{entity.InventoryHeight}"),
            new("Inventory Plane", entity.InventoryPlaneId?.ToString() ?? "none"),
            new("Usable Inventory", entity.HasUsableInventory.ToString())
        };

        return new EntityInspectionPanel(
            entity.Id,
            entity.Name,
            entity.Glyph,
            entity.Color,
            world.FormatEntityAddress(entityId),
            properties,
            BuildInventoryGrid(world, entity));
    }

    public EntityId? FindEntityContainingPlane(WorldState world, PlaneId planeId)
    {
        foreach (var entity in world.Entities.Values)
        {
            if (entity.InventoryPlaneId == planeId)
            {
                return entity.Id;
            }
        }

        return null;
    }

    private static InventoryInspectionGrid? BuildInventoryGrid(WorldState world, Entity entity)
    {
        if (!entity.HasUsableInventory || entity.InventoryPlaneId is not { } inventoryPlaneId)
        {
            return null;
        }

        if (!world.Planes.TryGetValue(inventoryPlaneId, out var plane))
        {
            return null;
        }

        var cells = new List<InventoryInspectionCell>();

        for (var y = 0; y < plane.Height; y++)
        {
            for (var x = 0; x < plane.Width; x++)
            {
                var coord = new GridCoord(x, y);
                var occupantId = world.GetOccupant(new PlaneCoord(plane.Id, coord));

                if (occupantId is { } entityId)
                {
                    var occupant = world.Entities[entityId];
                    cells.Add(new InventoryInspectionCell(coord, entityId, occupant.Glyph, occupant.Color));
                }
                else
                {
                    cells.Add(new InventoryInspectionCell(coord, null, '.', ConsoleColor.DarkGray));
                }
            }
        }

        return new InventoryInspectionGrid(plane.Id, plane.Width, plane.Height, cells);
    }
}
