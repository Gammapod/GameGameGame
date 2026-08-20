using GameGameGame.Core;

namespace GameGameGame.Frontend.SadConsole;

internal sealed class PlayInventorySelectionController(PlayActionSessionController actionSession)
{
    private EntityId? _pickupTargetId;

    public bool IsActive => _pickupTargetId is not null;
    public GridCoord SelectedCoord { get; private set; }
    public CellHighlightKind TargetHighlightKind => CellHighlightKind.Pickup;

    public bool TryBeginPickup(EntityId targetId)
    {
        if (!TryResolvePlayerInventory(out var planeId, out var width, out var height))
        {
            return false;
        }

        _pickupTargetId = targetId;
        SelectedCoord = FirstValidPickupDestination(targetId, planeId, width, height) ?? new GridCoord(0, 0);
        return true;
    }

    public void Cancel() => _pickupTargetId = null;

    public bool Move(Direction direction)
    {
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
        if (!IsActive)
        {
            return null;
        }

        return new PlayHighlightState(SelectedCoord, IsSelectedPickupDestinationValid() ? CellHighlightKind.Pickup : CellHighlightKind.NoAction);
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
