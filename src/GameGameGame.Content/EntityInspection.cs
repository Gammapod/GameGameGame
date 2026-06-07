using GameGameGame.Core;

namespace GameGameGame.Content;

public enum PresentationColor
{
    Default,
    Gray,
    White,
    Yellow,
    Cyan,
    Green,
    DarkGreen,
    Earth
}

public sealed record EntityInspectionPanel(
    EntityId EntityId,
    string Name,
    char Glyph,
    PresentationColor Color,
    string Address,
    IReadOnlyList<EntityInspectionProperty> Properties,
    InventoryInspectionGrid? InventoryGrid);

public sealed record EntityInspectionProperty(string Name, string Value);

public sealed record InventoryInspectionGrid(
    PlaneId PlaneId,
    int Width,
    int Height,
    IReadOnlyList<InventoryInspectionCell> Cells);

public sealed record InventoryInspectionCell(GridCoord Coord, EntityId? EntityId, char Glyph, PresentationColor Color);

public sealed record EntityInspectionAppearance(char Glyph, PresentationColor Color);

public sealed class EntityInspectionService(Func<EntityId, EntityInspectionAppearance>? getAppearance = null)
{
    public EntityInspectionPanel Inspect(WorldState world, EntityId entityId)
    {
        var entity = world.Entities[entityId];
        var weight = new WeightService();
        var appearance = GetAppearance(entity);

        var properties = new List<EntityInspectionProperty>
        {
            new("Id", entity.Id.ToString()),
            new("Location", world.GetEntityLocation(entityId).ToString()),
            new("Weight", entity.Weight.ToString()),
            new("Total Weight", weight.GetTotalWeight(world, entityId).ToString()),
            new("Carried Weight", weight.GetCarriedWeight(world, entityId).ToString()),
            new("Carrying Capacity", entity.CarryingCapacity.ToString()),
            new("Inventory Dimensions", $"{entity.InventoryWidth}x{entity.InventoryHeight}"),
            new("Inventory Plane", world.GetInventoryPlaneId(entityId)?.ToString() ?? "none"),
            new("Usable Inventory", entity.HasUsableInventory.ToString())
        };

        return new EntityInspectionPanel(
            entity.Id,
            entity.Name,
            appearance.Glyph,
            appearance.Color,
            world.FormatEntityAddress(entityId),
            properties,
            BuildInventoryGrid(world, entity));
    }

    public EntityId? FindEntityContainingPlane(WorldState world, PlaneId planeId)
    {
        foreach (var entity in world.Entities.Values)
        {
            if (world.GetInventoryPlaneId(entity.Id) == planeId)
            {
                return entity.Id;
            }
        }

        return null;
    }

    private InventoryInspectionGrid? BuildInventoryGrid(WorldState world, Entity entity)
    {
        if (world.GetInventoryPlaneId(entity.Id) is not { } inventoryPlaneId)
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
                    var appearance = GetAppearance(occupant);
                    cells.Add(new InventoryInspectionCell(coord, entityId, appearance.Glyph, appearance.Color));
                }
                else
                {
                    cells.Add(new InventoryInspectionCell(coord, null, '.', PresentationColor.Gray));
                }
            }
        }

        return new InventoryInspectionGrid(plane.Id, plane.Width, plane.Height, cells);
    }

    private EntityInspectionAppearance GetAppearance(Entity entity) =>
        getAppearance?.Invoke(entity.Id) ?? new EntityInspectionAppearance('?', PresentationColor.Gray);
}
