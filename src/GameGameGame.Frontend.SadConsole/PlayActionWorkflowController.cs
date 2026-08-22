using GameGameGame.Core;

namespace GameGameGame.Frontend.SadConsole;

internal enum PlayActionWorkflowKind
{
    None,
    PickupDestination,
    DropSource,
    DropDestination,
    ExitDestination,
    TransferItem
}

internal sealed record PlayActionSelectionOption(
    string Label,
    bool IsValid,
    bool IsSelected,
    PlayHighlightState? Highlight = null,
    EntityId? EntityId = null,
    Direction? Direction = null);

internal sealed class PlayActionWorkflowController(PlayActionSessionController actionSession)
{

    private EntityId? _pickupTargetId;
    private EntityId? _dropSourceId;
    private EntityId? _transferCounterpartyId;
    private int _transferItemIndex;
    private PlayActionWorkflowKind _mode;

    public PlayActionWorkflowKind Mode => _mode;
    public bool IsActive => _mode != PlayActionWorkflowKind.None;
    public GridCoord SelectedCoord { get; private set; }
    public CellHighlightKind TargetHighlightKind => _mode == PlayActionWorkflowKind.DropDestination ? CellHighlightKind.Drop : CellHighlightKind.Pickup;
    public bool IsDropSourceSelection => _mode == PlayActionWorkflowKind.DropSource;
    public bool IsDropDestinationSelection => _mode == PlayActionWorkflowKind.DropDestination;
    public bool IsExitDestinationSelection => _mode == PlayActionWorkflowKind.ExitDestination;
    public bool IsTransferItemSelection => _mode == PlayActionWorkflowKind.TransferItem;

    public bool TryBeginPickup(EntityId targetId)
    {
        if (!TryResolvePlayerInventory(out var planeId, out var width, out var height))
        {
            return false;
        }

        _pickupTargetId = targetId;
        _dropSourceId = null;
        _mode = PlayActionWorkflowKind.PickupDestination;
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
        _mode = PlayActionWorkflowKind.DropSource;
        SelectedCoord = FirstValidDropSource(planeId, width, height) ?? new GridCoord(0, 0);
        return true;
    }

    public bool TryBeginExitDestination()
    {
        var first = ExitChoice()?.DirectionOptions.FirstOrDefault(option => option.CanExecute);
        if (first is null)
        {
            return false;
        }

        _pickupTargetId = null;
        _dropSourceId = null;
        _transferCounterpartyId = null;
        _mode = PlayActionWorkflowKind.ExitDestination;
        SelectedCoord = first.Destination?.Coord ?? new GridCoord(0, 0);
        return true;
    }

    public bool TryBeginTransferItems(EntityId counterpartyId)
    {
        if (!TransferChoices().Any(choice => choice.TransferCounterparties.Any(option => option.CounterpartyId == counterpartyId && option.CanExecute))
            || ValidTransferItems(counterpartyId).Count == 0)
        {
            return false;
        }

        _pickupTargetId = null;
        _dropSourceId = null;
        _transferCounterpartyId = counterpartyId;
        _transferItemIndex = 0;
        _mode = PlayActionWorkflowKind.TransferItem;
        return true;
    }

    public void Cancel()
    {
        _pickupTargetId = null;
        _dropSourceId = null;
        _transferCounterpartyId = null;
        _mode = PlayActionWorkflowKind.None;
    }

    public bool CancelDropDestinationToSource()
    {
        if (_mode != PlayActionWorkflowKind.DropDestination)
        {
            return false;
        }

        _dropSourceId = null;
        _mode = PlayActionWorkflowKind.DropSource;
        if (TryResolvePlayerInventory(out var planeId, out var width, out var height))
        {
            SelectedCoord = FirstValidDropSource(planeId, width, height) ?? new GridCoord(0, 0);
        }

        return true;
    }

    public bool Move(Direction direction)
    {
        if (_mode == PlayActionWorkflowKind.TransferItem)
        {
            return false;
        }

        if (_mode == PlayActionWorkflowKind.ExitDestination)
        {
            if (ExitChoice()?.DirectionOptions.FirstOrDefault(option => option.Direction == direction) is not { } option)
            {
                return false;
            }

            SelectedCoord = option.Destination?.Coord ?? SelectedCoord;
            return true;
        }

        if (_mode == PlayActionWorkflowKind.DropDestination)
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

    public bool SelectDirection(Direction direction) => Move(direction);

    public bool SelectNextOption(int delta) => _mode == PlayActionWorkflowKind.TransferItem
        ? MoveTransferItem(delta)
        : false;

    public ControlledActorCommandResult? ConfirmCurrentSubmission() => _mode switch
    {
        PlayActionWorkflowKind.PickupDestination => ConfirmPickup(),
        PlayActionWorkflowKind.DropDestination => ConfirmDrop(),
        PlayActionWorkflowKind.ExitDestination => ConfirmExit(),
        PlayActionWorkflowKind.TransferItem => ConfirmTransfer(),
        _ => null
    };

    public PlayHighlightState? InventoryHighlight()
    {
        if (!IsActive || _mode is PlayActionWorkflowKind.DropDestination or PlayActionWorkflowKind.ExitDestination or PlayActionWorkflowKind.TransferItem)
        {
            return null;
        }

        var kind = _mode == PlayActionWorkflowKind.DropSource
            ? IsSelectedDropSourceValid() ? CellHighlightKind.Drop : CellHighlightKind.NoAction
            : IsSelectedPickupDestinationValid() ? CellHighlightKind.Pickup : CellHighlightKind.NoAction;
        return new PlayHighlightState(SelectedCoord, kind);
    }

    public PlayHighlightState? GridHighlight()
    {
        if (_mode != PlayActionWorkflowKind.DropDestination && _mode != PlayActionWorkflowKind.ExitDestination)
        {
            return null;
        }

        if (_mode == PlayActionWorkflowKind.ExitDestination)
        {
            return new PlayHighlightState(SelectedCoord, IsSelectedExitDestinationValid() ? CellHighlightKind.Exit : CellHighlightKind.NoAction);
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
        if (_mode != PlayActionWorkflowKind.DropSource || !TryResolvePlayerInventory(out var planeId, out _, out _))
        {
            return false;
        }

        var occupant = actionSession.World.GetOccupant(new PlaneCoord(planeId, SelectedCoord));
        if (occupant is not { } sourceId || !IsDropSourceValid(sourceId))
        {
            return false;
        }

        _dropSourceId = sourceId;
        _mode = PlayActionWorkflowKind.DropDestination;
        SelectedCoord = FirstValidDropDestination() ?? actionSession.World.GetEntityLocation(actionSession.ControlledActorId).Coord;
        return true;
    }

    public ControlledActorCommandResult? ConfirmDrop()
    {
        if (_mode != PlayActionWorkflowKind.DropDestination || _dropSourceId is not { } sourceId || !IsSelectedDropDestinationValid())
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

    public ControlledActorCommandResult? ConfirmExit()
    {
        if (_mode != PlayActionWorkflowKind.ExitDestination || SelectedExitDirection() is not { } direction)
        {
            return null;
        }

        var result = actionSession.SubmitExit(direction);
        if (result.Succeeded)
        {
            Cancel();
        }

        return result;
    }

    public bool MoveTransferItem(int delta)
    {
        if (_mode != PlayActionWorkflowKind.TransferItem || _transferCounterpartyId is not { } counterpartyId)
        {
            return false;
        }

        var items = ValidTransferItems(counterpartyId);
        if (items.Count == 0)
        {
            return false;
        }

        var next = Math.Clamp(_transferItemIndex + delta, 0, items.Count - 1);
        if (next == _transferItemIndex)
        {
            return false;
        }

        _transferItemIndex = next;
        return true;
    }

    public string TransferItemSummary()
    {
        if (_mode != PlayActionWorkflowKind.TransferItem || _transferCounterpartyId is not { } counterpartyId)
        {
            return "No transfer item selected.";
        }

        var items = ValidTransferItems(counterpartyId);
        if (items.Count == 0)
        {
            return "No transferable items.";
        }

        var item = items[Math.Clamp(_transferItemIndex, 0, items.Count - 1)];
        var verb = item.TransferDirection == TransferDirection.ActorToTarget ? "Give" : "Take";
        return $"{verb} {TransferItemLabel(item.MovingEntityId)} ({_transferItemIndex + 1}/{items.Count}). Enter transfers.";
    }

    public ControlledActorCommandResult? ConfirmTransfer()
    {
        if (_mode != PlayActionWorkflowKind.TransferItem || _transferCounterpartyId is not { } counterpartyId)
        {
            return null;
        }

        var items = ValidTransferItems(counterpartyId);
        if (items.Count == 0)
        {
            return null;
        }

        var item = items[Math.Clamp(_transferItemIndex, 0, items.Count - 1)];
        var result = actionSession.SubmitTransfer(counterpartyId, item.MovingEntityId);
        if (result.Succeeded)
        {
            Cancel();
        }

        return result;
    }

    public IReadOnlyList<PlayTransferSelectionRow> TransferSelectionRows()
    {
        return TransferSelectionOptions()
            .Select(option => new PlayTransferSelectionRow(
                option.EntityId ?? new EntityId(option.Label),
                option.Label.Split(':')[0],
                option.EntityId?.Value ?? option.Label,
                option.IsSelected))
            .ToList();
    }

    public IReadOnlyList<PlayActionSelectionOption> TransferSelectionOptions()
    {
        if (_mode != PlayActionWorkflowKind.TransferItem || _transferCounterpartyId is not { } counterpartyId)
        {
            return [];
        }

        var items = ValidTransferItems(counterpartyId);
        return items.Select((item, index) =>
            {
                var selected = index == _transferItemIndex;
                return new PlayActionSelectionOption(
                    $"{(item.TransferDirection == TransferDirection.ActorToTarget ? "Give" : "Take")}: {TransferItemLabel(item.MovingEntityId)}",
                    item.CanExecute,
                    selected,
                    selected ? new PlayHighlightState(actionSession.World.GetEntityLocation(item.MovingEntityId).Coord, CellHighlightKind.Transfer) : null,
                    item.MovingEntityId);
            })
            .ToList();
    }

    public PlayHighlightState? TransferInventoryHighlightFor(EntityId ownerId)
    {
        if (_mode != PlayActionWorkflowKind.TransferItem || SelectedTransferItem() is not { } item || item.OwnerEntityId != ownerId)
        {
            return null;
        }

        return actionSession.World.Entities.ContainsKey(item.MovingEntityId)
            ? new PlayHighlightState(actionSession.World.GetEntityLocation(item.MovingEntityId).Coord, CellHighlightKind.Transfer)
            : null;
    }

    public bool IsSelectedDropSourceValid()
    {
        if (_mode != PlayActionWorkflowKind.DropSource || !TryResolvePlayerInventory(out var planeId, out _, out _))
        {
            return false;
        }

        var occupant = actionSession.World.GetOccupant(new PlaneCoord(planeId, SelectedCoord));
        return occupant is { } sourceId && IsDropSourceValid(sourceId);
    }

    public bool IsSelectedDropDestinationValid()
    {
        if (_mode != PlayActionWorkflowKind.DropDestination || _dropSourceId is not { } sourceId)
        {
            return false;
        }

        var actorPlane = actionSession.World.GetEntityLocation(actionSession.ControlledActorId).PlaneId;
        var destination = new PlaneCoord(actorPlane, SelectedCoord);
        return actionSession.World.GetOccupant(destination) is null
            && DropChoice()?.Destinations(sourceId).Any(option => option.Destination == destination && option.CanExecute) == true;
    }

    public bool IsSelectedExitDestinationValid() => SelectedExitDirection() is not null;

    public PlaneId? ExitDestinationPlaneId() => ExitChoice()?.DirectionOptions.FirstOrDefault(option => option.CanExecute && option.Destination is not null)?.Destination?.PlaneId;

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
    private ActionChoice? ExitChoice() => actionSession.CurrentActionChoiceRequest?.Choices.FirstOrDefault(choice => choice.Kind == ActionChoiceKind.Exit);
    private IEnumerable<ActionChoice> TransferChoices() => actionSession.CurrentActionChoiceRequest?.Choices.Where(choice => choice.Kind == ActionChoiceKind.Transfer) ?? [];

    private IReadOnlyList<ActionChoiceTransferItemOption> ValidTransferItems(EntityId counterpartyId) =>
        TransferChoices()
            .SelectMany(choice => choice.TransferItems(counterpartyId))
            .Where(item => item.CanExecute)
            .GroupBy(item => item.MovingEntityId)
            .Select(group => group.First())
            .ToList();

    private ActionChoiceTransferItemOption? SelectedTransferItem()
    {
        if (_transferCounterpartyId is not { } counterpartyId)
        {
            return null;
        }

        var items = ValidTransferItems(counterpartyId);
        return items.Count == 0 ? null : items[Math.Clamp(_transferItemIndex, 0, items.Count - 1)];
    }

    private static string TransferItemLabel(EntityId entityId) => entityId.Value;

    private Direction? SelectedExitDirection() => ExitChoice()?.DirectionOptions.FirstOrDefault(option =>
            option.CanExecute &&
            option.Destination is { } destination &&
            destination.Coord == SelectedCoord)
        ?.Direction;

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
