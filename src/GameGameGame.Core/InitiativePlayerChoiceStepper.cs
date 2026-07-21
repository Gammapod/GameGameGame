namespace GameGameGame.Core;

public sealed record InitiativePlayerChoiceStepResult(
    IReadOnlyList<SimulationHistoryActorLog> ActorLogs,
    ActionChoiceRequest? Request,
    int NextActorIndex,
    bool CompletedCycle,
    IReadOnlyList<string>? Diagnostics = null)
{
    public IReadOnlyList<string> Diagnostics { get; } = Diagnostics ?? [];
}

public sealed class InitiativePlayerChoiceStepper(
    MovementService movement,
    ActionChoiceService choices)
{
    public InitiativePlayerChoiceStepResult AdvanceUntilPlayerChoice(
        WorldState world,
        IReadOnlyList<EntityId> actorOrder,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        Func<EntityId, ActionPlanDescriptor?> getActionPlanDescriptor,
        int startIndex,
        Action<WorldState, EntityId>? beforePlan = null)
    {
        if (actorOrder.Count == 0)
        {
            return new InitiativePlayerChoiceStepResult([], Request: null, NextActorIndex: 0, CompletedCycle: true);
        }

        var logs = new List<SimulationHistoryActorLog>();
        var diagnostics = new List<string>();
        var index = NormalizeIndex(startIndex, actorOrder.Count);

        for (var visited = 0; visited < actorOrder.Count; visited++)
        {
            var actorId = actorOrder[index];
            if (!world.Entities.TryGetValue(actorId, out var entity))
            {
                index = NormalizeIndex(index + 1, actorOrder.Count);
                continue;
            }

            if (world.GetActionControlSource(actorId) == EntityControlSource.PlayerChoice)
            {
                var request = getActionPlanDescriptor(actorId) is { } descriptor
                    ? choices.CreateRequest(world, actorId, descriptor)
                    : null;
                if (request is null)
                {
                    diagnostics.Add($"PlayerChoice actor {actorId} has no Action Choice request.");
                }

                return new InitiativePlayerChoiceStepResult(logs, request, index, CompletedCycle: false, diagnostics);
            }

            if (actionPlans.TryGetValue(actorId, out var actionPlan))
            {
                beforePlan?.Invoke(world, actorId);
                var resolution = ActorTurnResolver.ResolvePlan(world, actorId, actionPlan.PlanTurn(world, actorId, movement), movement);
                PostActionStateUpdater.ApplyFacingFromMovement(world, actorId, resolution.ActorMovementDirection);
                world.RecordTrace(resolution.Trace);

                if (resolution.ConsumesTurn)
                {
                    world.AdvanceTurn();
                }

                logs.Add(new SimulationHistoryActorLog(
                    index,
                    actorId,
                    entity.Name,
                    resolution.Succeeded,
                    resolution.ConsumesTurn,
                    resolution.ContinuePlan,
                    TurnActionSummaryFormatter.FormatTrace(resolution.Trace, resolution.Succeeded),
                    resolution.Trace));
            }

            index = NormalizeIndex(index + 1, actorOrder.Count);
        }

        return new InitiativePlayerChoiceStepResult(logs, Request: null, index, CompletedCycle: true, diagnostics);
    }

    private static int NormalizeIndex(int index, int count) =>
        ((index % count) + count) % count;
}
