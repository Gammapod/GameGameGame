namespace GameGameGame.Core;

public enum TraceStatus
{
    Info,
    Success,
    Failure,
    Skipped
}

public enum FailureReason
{
    None,
    ActorMissing,
    TargetMissing,
    ActorHasNoInventory,
    ActorInventoryUnusable,
    InvalidInventoryDestination,
    TargetIsActor,
    TargetNotAdjacent,
    InvalidPlacement,
    ApertureBlocked,
    TargetNotInInventory,
    InvalidDropDestination,
    MoveBlocked,
    MoveOutOfBounds,
    TargetHasNoInventory,
    TargetInventoryUnusable
}

public enum ActionSuccessCriterionKind
{
    Aperture
}

public sealed record ActionSuccessCriterion(
    ActionSuccessCriterionKind Kind,
    bool Satisfied,
    decimal? SuccessRatio,
    int? RequiredValue,
    int? AvailableValue,
    EntityId? SubjectEntityId = null,
    EntityId? LimitEntityId = null,
    string? Detail = null);

public sealed class TraceNode(
    string label,
    TraceStatus status,
    FailureReason reason = FailureReason.None,
    string? detail = null)
{
    public string Label { get; } = label;

    public TraceStatus Status { get; set; } = status;

    public FailureReason Reason { get; set; } = reason;

    public string? Detail { get; set; } = detail;

    public List<TraceNode> Children { get; } = [];

    public List<ActionSuccessCriterion> SuccessCriteria { get; } = [];

    public TraceNode Add(TraceNode child)
    {
        Children.Add(child);
        return this;
    }

    public static TraceNode Info(string label, string? detail = null) =>
        new(label, TraceStatus.Info, detail: detail);

    public static TraceNode Success(string label, string? detail = null) =>
        new(label, TraceStatus.Success, detail: detail);

    public static TraceNode Failure(string label, FailureReason reason, string? detail = null) =>
        new(label, TraceStatus.Failure, reason, detail);

    public static TraceNode Skipped(string label, string? detail = null) =>
        new(label, TraceStatus.Skipped, detail: detail);
}

public sealed record ActionEvaluation(bool CanExecute, TraceNode Trace);
