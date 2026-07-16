namespace GameGameGame.Core;

public sealed record SimulationHistoryFrame(
    int FrameIndex,
    int WorldTurnNumber,
    EntityId ControlledEntityId,
    PlaneId ActivePlaneId,
    EntityId? ActiveContainerId,
    WorldState Snapshot);

public sealed record SimulationHistoryInterval(
    int FromFrameIndex,
    int ToFrameIndex,
    ControlledActorCommandResult? ControlledResult,
    IReadOnlyList<SimulationHistoryActorLog> ActorLogs);

public sealed record SimulationHistoryActorLog(
    int Order,
    EntityId ActorId,
    string ActorName,
    bool Succeeded,
    bool ConsumedTurn,
    bool ContinuePlan,
    string Summary,
    TraceNode Trace);

public sealed record SimulationHistoryFrameLogEntry(
    int FrameIndex,
    ControlledActorCommandResult ControlledResult);

public sealed class SimulationHistorySession
{
    private readonly List<SimulationHistoryFrame> _frames;
    private readonly List<SimulationHistoryInterval> _intervals = [];
    private readonly Dictionary<int, List<SimulationHistoryFrameLogEntry>> _frameLogEntries = [];

    private SimulationHistorySession(WorldState world, List<SimulationHistoryFrame> frames)
    {
        World = world;
        _frames = frames;
        CurrentFrameIndex = 0;
    }

    public WorldState World { get; }

    public int CurrentFrameIndex { get; private set; }

    public bool CanRollback => CurrentFrameIndex > 0;

    public SimulationHistoryFrame CurrentFrame => _frames[CurrentFrameIndex];

    public IReadOnlyList<SimulationHistoryFrame> Frames => _frames;

    public IReadOnlyList<SimulationHistoryInterval> Intervals => _intervals;

    public IReadOnlyList<SimulationHistoryFrameLogEntry> CurrentFrameLogEntries => GetFrameLogEntries(CurrentFrameIndex);

    public IReadOnlyList<SimulationHistoryFrameLogEntry> GetFrameLogEntries(int frameIndex) =>
        _frameLogEntries.TryGetValue(frameIndex, out var entries) ? entries : [];

    public static SimulationHistorySession Start(
        WorldState world,
        EntityId controlledEntityId,
        PlaneId activePlaneId,
        EntityId? activeContainerId = null)
    {
        var frame = new SimulationHistoryFrame(
            FrameIndex: 0,
            WorldTurnNumber: world.TurnNumber,
            controlledEntityId,
            activePlaneId,
            activeContainerId,
            world.Clone());

        return new SimulationHistorySession(world, [frame]);
    }

    public void RollbackToFrame(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= _frames.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(frameIndex), "History frame does not exist.");
        }

        var frame = _frames[frameIndex];
        World.RestoreFrom(frame.Snapshot);

        if (_frames.Count > frameIndex + 1)
        {
            _frames.RemoveRange(frameIndex + 1, _frames.Count - frameIndex - 1);
        }

        _intervals.RemoveAll(interval => interval.FromFrameIndex >= frameIndex || interval.ToFrameIndex > frameIndex);

        foreach (var loggedFrameIndex in _frameLogEntries.Keys.Where(loggedFrameIndex => loggedFrameIndex > frameIndex).ToList())
        {
            _frameLogEntries.Remove(loggedFrameIndex);
        }

        CurrentFrameIndex = frameIndex;
    }

    public bool RollbackPreviousFrame()
    {
        if (!CanRollback)
        {
            return false;
        }

        RollbackToFrame(CurrentFrameIndex - 1);
        return true;
    }

    public ControlledActorCommandResult SubmitControlledCommand(
        ControlledActorCommandService commands,
        ControlledActorCommand command)
    {
        var fromFrameIndex = CurrentFrameIndex;
        var controlledEntityId = CurrentFrame.ControlledEntityId;
        var result = commands.Execute(World, controlledEntityId, command);

        if (!result.Succeeded || !result.AdvancedTurn)
        {
            AddFrameLogEntry(fromFrameIndex, result);
            return result;
        }

        var nextFrameIndex = fromFrameIndex + 1;
        var activePlaneId = World.GetEntityLocation(controlledEntityId).PlaneId;
        var nextFrame = new SimulationHistoryFrame(
            nextFrameIndex,
            World.TurnNumber,
            controlledEntityId,
            activePlaneId,
            FindContainerForPlane(activePlaneId),
            World.Clone());

        _frames.Add(nextFrame);
        _intervals.Add(new SimulationHistoryInterval(fromFrameIndex, nextFrameIndex, result, CreateActorLogs(result)));
        CurrentFrameIndex = nextFrameIndex;

        return result;
    }

    public ControlledActorCommandResult SubmitActionChoice(
        ActionChoiceService choices,
        ActionChoiceRequest request,
        Direction direction,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        Action<WorldState, EntityId>? beforePlan = null)
    {
        if (request.ActorId != CurrentFrame.ControlledEntityId)
        {
            throw new InvalidOperationException($"Action choice actor {request.ActorId} does not match current controlled entity {CurrentFrame.ControlledEntityId}.");
        }

        var fromFrameIndex = CurrentFrameIndex;
        var result = choices.SubmitMoveChoice(World, request, direction, actionPlans, beforePlan);

        if (!result.Succeeded || !result.AdvancedTurn)
        {
            AddFrameLogEntry(fromFrameIndex, result);
            return result;
        }

        var nextFrameIndex = fromFrameIndex + 1;
        var activePlaneId = World.GetEntityLocation(CurrentFrame.ControlledEntityId).PlaneId;
        var nextFrame = new SimulationHistoryFrame(
            nextFrameIndex,
            World.TurnNumber,
            CurrentFrame.ControlledEntityId,
            activePlaneId,
            FindContainerForPlane(activePlaneId),
            World.Clone());

        _frames.Add(nextFrame);
        _intervals.Add(new SimulationHistoryInterval(fromFrameIndex, nextFrameIndex, result, CreateActorLogs(result)));
        CurrentFrameIndex = nextFrameIndex;

        return result;
    }

    private static IReadOnlyList<SimulationHistoryActorLog> CreateActorLogs(ControlledActorCommandResult result)
    {
        if (result.TurnReport is null)
        {
            return [];
        }

        return result.TurnReport.Actions
            .Select((action, index) => new SimulationHistoryActorLog(
                index,
                action.ActorId,
                action.ActorName,
                action.Succeeded,
                action.ConsumedTurn,
                ContinuePlan: false,
                action.Summary,
                action.Trace))
            .ToList();
    }

    public SimulationHistoryInterval RecordActorInterval(
        IReadOnlyList<SimulationHistoryActorLog> actorLogs,
        PlaneId activePlaneId,
        EntityId? activeContainerId = null)
    {
        var fromFrameIndex = CurrentFrameIndex;
        var nextFrameIndex = fromFrameIndex + 1;
        var nextFrame = new SimulationHistoryFrame(
            nextFrameIndex,
            World.TurnNumber,
            CurrentFrame.ControlledEntityId,
            activePlaneId,
            activeContainerId,
            World.Clone());
        var interval = new SimulationHistoryInterval(fromFrameIndex, nextFrameIndex, ControlledResult: null, actorLogs);

        _frames.Add(nextFrame);
        _intervals.Add(interval);
        CurrentFrameIndex = nextFrameIndex;

        return interval;
    }

    private void AddFrameLogEntry(int frameIndex, ControlledActorCommandResult result)
    {
        if (!_frameLogEntries.TryGetValue(frameIndex, out var entries))
        {
            entries = [];
            _frameLogEntries[frameIndex] = entries;
        }

        entries.Add(new SimulationHistoryFrameLogEntry(frameIndex, result));
    }

    private EntityId? FindContainerForPlane(PlaneId planeId)
    {
        foreach (var (entityId, inventoryPlaneId) in World.InventoryPlanes)
        {
            if (inventoryPlaneId == planeId)
            {
                return entityId;
            }
        }

        return null;
    }
}
