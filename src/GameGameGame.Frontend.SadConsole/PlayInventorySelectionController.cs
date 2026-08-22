using GameGameGame.Core;

namespace GameGameGame.Frontend.SadConsole;

internal sealed class PlayInventorySelectionController(PlayActionSessionController actionSession)
{
    private enum InventorySelectionMode
    {
        None,
        PickupDestination,
        DropSource,
        DropDestination
    }

    private EntityId? _pickupTargetId;
    private EntityId? _dropSourceId;
    private InventorySelectionMode _mode;

    public bool IsActive => _mode != InventorySelectionMode.None;
    public GridCoord SelectedCoord { get; private set; }
    public CellHighlightKind TargetHighlightKind => _mode == InventorySelectionMode.DropDestination ? CellHighlightKind.Drop : CellHighlightKind.Pickup;
    public bool IsDropSourceSelection => _mode == InventorySelectionMode.DropSource;
    public bool IsDropDestinationSelection => _mode == InventorySelectionMode.DropDestination;

    public bool TryBeginPickup(EntityId targetId)
    {
        if (!TryResolvePlayerInventory(out var planeId, out var width, out var height))
        {
            return false;
        }

        _pickupTargetId = targetId;
        _dropSourceId = null;
        _mode = InventorySelectionMode.PickupDestination;
        SelectedCoord = FirstValidPickupDestination(targetId, planeId, width, height) ?? new GridCoord(0, 0);
        return true;
    }

    public bool TryBeginDropSource()
    {
        if (!TryResolvePlayerInventory(out var planeId, out var width, out var height))
        {
            return false;
        }

        _pickupTargetId = null;
        _dropSourceId = null;
        _mode = InventorySelectionMode.DropSource;
        SelectedCoord = FirstValidDropSource(planeId, width, height) ?? new GridCoord(0, 0);
        return true;
    }

    public void Cancel()
    {
        _pickupTargetId = null;
        _dropSourceId = null;
        _mode = InventorySelectionMode.None;
    }

    public bool CancelDropDestinationToSource()
    {
        if (_mode != InventorySelectionMode.DropDestination)
        {
            return false;
        }

        _dropSourceId = null;
        _mode = InventorySelectionMode.DropSource;
        if (TryResolvePlayerInventory(out var planeId, out var width, out var height))
        {
            SelectedCoord = FirstValidDropSource(planeId, width, height) ?? new GridCoord(0, 0);
        }

        return true;
    }

    public bool Move(Direction direction)
    {
        if (_mode == InventorySelectionMode.DropDestination)
        {
            var actor = actionSession.World.GetEntityLocation(actionSession.ControlledActorId).Coord;
            var nextDropCoord = actor.Offset(direction);

            if (nextDropCoord == SelectedCoord)
            {
                return false;
            }

            SelectedCoord = nextDropCoord;
            return true;
        }

        if (!TryResolvePlayerInventory(out _, out var width, out var height))
        {
            return false;
        }

        var next = SelectedCoord.Offset(direction);
        next = new GridCoord(Math.Clamp(next.X, 0, width - 1), Math.Clamp(next.Y, 0, height - 1));
        if (next == SelectedCoord)
        {
            return false;
        }

        SelectedCoord = next;
        return true;
    }

    public PlayHighlightState? InventoryHighlight()
    {
        if (!IsActive || _mode == InventorySelectionMode.DropDestination)
        {
            return null;
        }

        var kind = _mode == InventorySelectionMode.DropSource
            ? IsSelectedDropSourceValid() ? CellHighlightKind.Drop : CellHighlightKind.NoAction
            : IsSelectedPickupDestinationValid() ? CellHighlightKind.Pickup : CellHighlightKind.NoAction;
        return new PlayHighlightState(SelectedCoord, kind);
    }

    public PlayHighlightState? GridHighlight()
    {
        if (_mode != InventorySelectionMode.DropDestination)
        {
            return null;
        }

        return new PlayHighlightState(SelectedCoord, IsSelectedDropDestinationValid() ? CellHighlightKind.Drop : CellHighlightKind.NoAction);
    }

    public bool IsSelectedPickupDestinationValid()
    {
        if (_pickupTargetId is not { } targetId || !TryResolvePlayerInventory(out var planeId, out _, out _))
        {
            return false;
        }

        var destination = new PlaneCoord(planeId, SelectedCoord);
        return actionSession.World.GetOccupant(destination) is null
            && PickupChoice()?.Destinations(targetId).Any(option => option.Destination == destination && option.CanExecute) == true;
    }

    public ControlledActorCommandResult? ConfirmPickup()
    {
        if (_pickupTargetId is not { } targetId || !TryResolvePlayerInventory(out var planeId, out _, out _) || !IsSelectedPickupDestinationValid())
        {
            return null;
        }

        var result = actionSession.SubmitPickup(targetId, new PlaneCoord(planeId, SelectedCoord));
        if (result.Succeeded)
        {
            Cancel();
        }

        return result;
    }

    public bool ConfirmDropSource()
    {
        if (_mode != InventorySelectionMode.DropSource || !TryResolvePlayerInventory(out var planeId, out _, out _))
        {
            return false;
        }

        var occupant = actionSession.World.GetOccupant(new PlaneCoord(planeId, SelectedCoord));
        if (occupant is not { } sourceId || !IsDropSourceValid(sourceId))
        {
            return false;
        }

        _dropSourceId = sourceId;
        _mode = InventorySelectionMode.DropDestination;
        SelectedCoord = FirstValidDropDestination() ?? actionSession.World.GetEntityLocation(actionSession.ControlledActorId).Coord;
        return true;
    }

    public ControlledActorCommandResult? ConfirmDrop()
    {
        if (_mode != InventorySelectionMode.DropDestination || _dropSourceId is not { } sourceId || !IsSelectedDropDestinationValid())
        {
            return null;
        }

        var actorPlane = actionSession.World.GetEntityLocation(actionSession.ControlledActorId).PlaneId;
        var result = actionSession.SubmitDrop(sourceId, new PlaneCoord(actorPlane, SelectedCoord));
        if (result.Succeeded)
        {
            Cancel();
        }

        return result;
    }

    public bool IsSelectedDropSourceValid()
    {
        if (_mode != InventorySelectionMode.DropSource || !TryResolvePlayerInventory(out var planeId, out _, out _))
        {
            return false;
        }

        var occupant = actionSession.World.GetOccupant(new PlaneCoord(planeId, SelectedCoord));
        return occupant is { } sourceId && IsDropSourceValid(sourceId);
    }

    public bool IsSelectedDropDestinationValid()
    {
        if (_mode != InventorySelectionMode.DropDestination || _dropSourceId is not { } sourceId)
        {
            return false;
        }

        var actorPlane = actionSession.World.GetEntityLocation(actionSession.ControlledActorId).PlaneId;
        var destination = new PlaneCoord(actorPlane, SelectedCoord);
        return actionSession.World.GetOccupant(destination) is null
            && DropChoice()?.Destinations(sourceId).Any(option => option.Destination == destination && option.CanExecute) == true;
    }

    private GridCoord? FirstValidPickupDestination(EntityId targetId, PlaneId planeId, int width, int height)
    {
        var destinations = PickupChoice()?.Destinations(targetId)
            .Where(option => option.CanExecute && option.Destination.PlaneId == planeId)
            .Select(option => option.Destination.Coord)
            .ToHashSet() ?? [];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var coord = new GridCoord(x, y);
            if (destinations.Contains(coord) && actionSession.World.GetOccupant(new PlaneCoord(planeId, coord)) is null)
            {
                return coord;
            }
        }

        return null;
    }

    private ActionChoice? PickupChoice() => actionSession.CurrentActionChoiceRequest?.Choices.FirstOrDefault(choice => choice.Kind == ActionChoiceKind.Pickup);
    private ActionChoice? DropChoice() => actionSession.CurrentActionChoiceRequest?.Choices.FirstOrDefault(choice => choice.Kind == ActionChoiceKind.Drop);

    private GridCoord? FirstValidDropSource(PlaneId planeId, int width, int height)
    {
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var coord = new GridCoord(x, y);
            var occupant = actionSession.World.GetOccupant(new PlaneCoord(planeId, coord));
            if (occupant is { } sourceId && IsDropSourceValid(sourceId))
            {
                return coord;
            }
        }

        return null;
    }

    private bool IsDropSourceValid(EntityId sourceId) => DropChoice()?.EntityOptions.Any(option => option.TargetId == sourceId && option.CanExecute)
        == true
        && DropChoice()?.Destinations(sourceId).Any(option => option.CanExecute) == true;

    private GridCoord? FirstValidDropDestination() => ValidDropDestinationCoords().Cast<GridCoord?>().FirstOrDefault();

    private IEnumerable<GridCoord> ValidDropDestinationCoords()
    {
        if (_dropSourceId is not { } sourceId || !actionSession.World.Entities.ContainsKey(actionSession.ControlledActorId))
        {
            yield break;
        }

        var actorPlane = actionSession.World.GetEntityLocation(actionSession.ControlledActorId).PlaneId;
        foreach (var option in DropChoice()?.Destinations(sourceId) ?? [])
        {
            if (option.CanExecute && option.Destination.PlaneId == actorPlane && actionSession.World.GetOccupant(option.Destination) is null)
            {
                yield return option.Destination.Coord;
            }
        }
    }

    private bool TryResolvePlayerInventory(out PlaneId planeId, out int width, out int height)
    {
        if (actionSession.World.GetRegisteredInventoryPlaneId(actionSession.ControlledActorId) is { } inventoryPlaneId
            && actionSession.World.Planes.TryGetValue(inventoryPlaneId, out var plane))
        {
            planeId = inventoryPlaneId;
            width = plane.Width;
            height = plane.Height;
            return true;
        }

        planeId = default!;
        width = 0;
        height = 0;
        return false;
    }
}
