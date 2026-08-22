using GameGameGame.Core;

namespace GameGameGame.Frontend.SadConsole;

internal enum PlayActionCandidateSourceKind
{
    InspectionEntity,
    PlayerInventory
}

internal sealed record PlayActionCandidateSource(
    PlayActionCandidateSourceKind Kind,
    EntityId? EntityId = null)
{
    public static PlayActionCandidateSource InspectionEntity(EntityId entityId) => new(PlayActionCandidateSourceKind.InspectionEntity, entityId);
    public static PlayActionCandidateSource PlayerInventory() => new(PlayActionCandidateSourceKind.PlayerInventory);
}

internal sealed record PlayActionPromptChoice(
    FrontendTextMessage Text,
    bool IsValid,
    Direction? ShortcutDirection = null,
    GridCoord? FocusCoord = null);

internal sealed record PlayActionPromptLayer(
    FrontendTextMessage Title,
    IReadOnlyList<PlayActionPromptChoice> Choices,
    int FocusedIndex = 0)
{
    public PlayActionPromptChoice? FocusedChoice =>
        FocusedIndex >= 0 && FocusedIndex < Choices.Count ? Choices[FocusedIndex] : null;
}

internal sealed record PlayActionCandidate(
    PlayActionCandidateSource Source,
    ActionChoiceKind Kind,
    FrontendTextMessage Text,
    bool IsValid,
    bool IsComplete,
    PlayActionPromptLayer? Prompt = null,
    FrontendTextMessage? Explanation = null)
{
    public bool OpensPrompt => IsValid && !IsComplete && Prompt is not null;
}

internal enum PlayActionCandidateOutcomeKind
{
    NoSelection,
    Explained,
    ReadyToSubmit,
    FollowUpNeeded
}

internal sealed record PlayActionCandidateOutcome(
    PlayActionCandidateOutcomeKind Kind,
    FrontendTextMessage Message,
    PlayActionCandidate? Candidate = null,
    PlayActionPromptLayer? Prompt = null);

internal static class PlayActionCandidateResolver
{
    public static PlayActionCandidateOutcome ResolveSelection(PlayActionCandidate? candidate)
    {
        if (candidate is null)
        {
            return new PlayActionCandidateOutcome(
                PlayActionCandidateOutcomeKind.NoSelection,
                FrontendTextMessage.Create(FrontendTextIds.PlayActionNoSelection));
        }

        if (!candidate.IsValid)
        {
            return new PlayActionCandidateOutcome(
                PlayActionCandidateOutcomeKind.Explained,
                candidate.Explanation ?? FrontendTextMessage.Create(FrontendTextIds.PlayActionUnavailable, ("reason", "unavailable")),
                candidate);
        }

        if (candidate.OpensPrompt)
        {
            return new PlayActionCandidateOutcome(
                PlayActionCandidateOutcomeKind.FollowUpNeeded,
                candidate.Prompt!.Title,
                candidate,
                candidate.Prompt);
        }

        return new PlayActionCandidateOutcome(
            PlayActionCandidateOutcomeKind.ReadyToSubmit,
            candidate.Text,
            candidate);
    }
}

internal static class PlayActionCandidateProjector
{
    public static IReadOnlyList<PlayActionCandidate> ForInspectedEntity(ActionChoiceRequest? request, EntityId targetId)
    {
        if (request is null)
        {
            return [];
        }

        var candidates = new List<PlayActionCandidate>();
        foreach (var choice in request.Choices)
        {
            candidates.AddRange(choice.Kind switch
            {
                ActionChoiceKind.Pickup => PickupCandidates(choice, targetId),
                ActionChoiceKind.Enter => EntityOptionCandidates(choice, targetId),
                ActionChoiceKind.Push => PushCandidates(choice, targetId),
                ActionChoiceKind.Transfer => TransferCandidates(choice, targetId),
                ActionChoiceKind.Drop => EntityOptionCandidates(choice, targetId),
                _ => EntityOptionCandidates(choice, targetId)
            });
        }

        return candidates
            .GroupBy(candidate => (candidate.Kind, candidate.Source.Kind, candidate.Source.EntityId))
            .Select(group => group.FirstOrDefault(candidate => candidate.IsValid) ?? group.First())
            .ToList();
    }

    public static IReadOnlyList<PlayActionCandidate> ForPlayerInventory(ActionChoiceRequest? request)
    {
        if (request is null)
        {
            return [];
        }

        var candidates = new List<PlayActionCandidate>();
        if (request.Choices.FirstOrDefault(choice => choice.Kind == ActionChoiceKind.Drop) is { } dropChoice)
        {
            var anyValidSource = dropChoice.EntityOptions.Any(option => option.CanExecute && dropChoice.Destinations(option.TargetId).Any(destination => destination.CanExecute));
            candidates.Add(new PlayActionCandidate(
                    PlayActionCandidateSource.PlayerInventory(),
                    ActionChoiceKind.Drop,
                    FrontendTextMessage.Create(FrontendTextIds.InspectionActionGeneric, ("actionName", "Drop"), ("targetName", "item")),
                    anyValidSource,
                    IsComplete: false,
                    Explanation: anyValidSource ? null : FrontendTextMessage.Create(FrontendTextIds.PlayActionUnavailable, ("reason", "No droppable item."))));
        }

        if (request.Choices.FirstOrDefault(choice => choice.Kind == ActionChoiceKind.Exit) is { } exitChoice)
        {
            var anyValidDirection = exitChoice.DirectionOptions.Any(option => option.CanExecute);
            candidates.Add(new PlayActionCandidate(
                PlayActionCandidateSource.PlayerInventory(),
                ActionChoiceKind.Exit,
                FrontendTextMessage.Create(FrontendTextIds.InspectionActionGeneric, ("actionName", "Exit"), ("targetName", "container")),
                anyValidDirection,
                IsComplete: false,
                Explanation: anyValidDirection ? null : FrontendTextMessage.Create(FrontendTextIds.PlayActionUnavailable, ("reason", "No valid exit."))));
        }

        return candidates;
    }

    private static IEnumerable<PlayActionCandidate> PickupCandidates(ActionChoice choice, EntityId targetId)
    {
        foreach (var option in choice.EntityOptions.Where(option => option.TargetId == targetId))
        {
            if (!option.CanExecute)
            {
                yield return Unavailable(choice.Kind, targetId, option.FailureReason, option.FailureDetail);
                continue;
            }

            var destinations = choice.Destinations(targetId).Where(destination => destination.CanExecute).ToList();
            yield return destinations.Count == 1
                ? Complete(choice.Kind, targetId)
                : WithPrompt(
                    choice.Kind,
                    targetId,
                    FrontendTextMessage.Create(FrontendTextIds.PlayActionPromptPickupDestination, ("targetName", targetId.Value)),
                    destinations.Select(destination => new PlayActionPromptChoice(
                        FrontendTextMessage.Create(FrontendTextIds.PlayActionPromptDestination, ("coord", FormatCoord(destination.Destination.Coord))),
                        true,
                        FocusCoord: destination.Destination.Coord)).ToList(),
                    destinations.Count == 0 ? "No valid destination." : null);
        }
    }

    private static IEnumerable<PlayActionCandidate> PushCandidates(ActionChoice choice, EntityId targetId)
    {
        foreach (var option in choice.EntityOptions.Where(option => option.TargetId == targetId))
        {
            if (!option.CanExecute)
            {
                yield return Unavailable(choice.Kind, targetId, option.FailureReason, option.FailureDetail);
                continue;
            }

            var directions = choice.PushDirections(targetId).Where(direction => direction.CanExecute).ToList();
            yield return directions.Count == 1
                ? Complete(choice.Kind, targetId)
                : WithPrompt(
                    choice.Kind,
                    targetId,
                    FrontendTextMessage.Create(FrontendTextIds.PlayActionPromptPushDirection, ("targetName", targetId.Value)),
                    directions.Select(direction => new PlayActionPromptChoice(
                        FrontendTextMessage.Create(FrontendTextIds.PlayActionPromptDirection, ("direction", direction.Direction)),
                        true,
                        direction.Direction,
                        direction.Destination?.Coord)).ToList(),
                    directions.Count == 0 ? "No valid push direction." : null);
        }
    }

    private static IEnumerable<PlayActionCandidate> TransferCandidates(ActionChoice choice, EntityId targetId)
    {
        foreach (var option in choice.TransferCounterparties.Where(option => option.CounterpartyId == targetId))
        {
            if (!option.CanExecute)
            {
                yield return Unavailable(choice.Kind, targetId, option.FailureReason, option.FailureDetail);
                continue;
            }

            var items = choice.TransferItems(targetId).Where(item => item.CanExecute).ToList();
            yield return WithPrompt(
                choice.Kind,
                targetId,
                FrontendTextMessage.Create(FrontendTextIds.PlayActionPromptTransferItem, ("targetName", targetId.Value)),
                items.Select(item => new PlayActionPromptChoice(
                    FrontendTextMessage.Create(FrontendTextIds.PlayActionPromptTransferItemChoice, ("entityId", item.MovingEntityId.Value)),
                    true)).ToList(),
                items.Count == 0 ? "No transferable item." : null);
        }
    }

    private static IEnumerable<PlayActionCandidate> EntityOptionCandidates(ActionChoice choice, EntityId targetId)
    {
        foreach (var option in choice.EntityOptions.Where(option => option.TargetId == targetId))
        {
            yield return option.CanExecute
                ? Complete(choice.Kind, targetId)
                : Unavailable(choice.Kind, targetId, option.FailureReason, option.FailureDetail);
        }
    }

    private static PlayActionCandidate Complete(ActionChoiceKind kind, EntityId targetId) => new(
        PlayActionCandidateSource.InspectionEntity(targetId),
        kind,
        ActionText(kind, targetId),
        IsValid: true,
        IsComplete: true);

    private static PlayActionCandidate WithPrompt(ActionChoiceKind kind, EntityId targetId, FrontendTextMessage title, IReadOnlyList<PlayActionPromptChoice> choices, string? emptyExplanation) =>
        choices.Count == 0
            ? new PlayActionCandidate(
                PlayActionCandidateSource.InspectionEntity(targetId),
                kind,
                ActionText(kind, targetId),
                IsValid: false,
                IsComplete: false,
                Explanation: FrontendTextMessage.Create(FrontendTextIds.PlayActionUnavailable, ("reason", emptyExplanation ?? "unavailable")))
            : new PlayActionCandidate(
                PlayActionCandidateSource.InspectionEntity(targetId),
                kind,
                ActionText(kind, targetId),
                IsValid: true,
                IsComplete: false,
                Prompt: new PlayActionPromptLayer(title, choices));

    private static PlayActionCandidate Unavailable(ActionChoiceKind kind, EntityId targetId, FailureReason? failureReason, string? failureDetail) => new(
        PlayActionCandidateSource.InspectionEntity(targetId),
        kind,
        ActionText(kind, targetId),
        IsValid: false,
        IsComplete: false,
        Explanation: FrontendTextMessage.Create(FrontendTextIds.PlayActionUnavailable, ("reason", FailureText(failureReason, failureDetail))));

    internal static FrontendTextMessage ActionText(ActionChoiceKind kind, EntityId targetId) => kind switch
    {
        ActionChoiceKind.Pickup => FrontendTextMessage.Create(FrontendTextIds.InspectionActionPickup, ("targetName", targetId.Value)),
        ActionChoiceKind.Drop => FrontendTextMessage.Create(FrontendTextIds.InspectionActionDrop, ("targetName", targetId.Value)),
        ActionChoiceKind.Enter => FrontendTextMessage.Create(FrontendTextIds.InspectionActionEnter, ("targetName", targetId.Value)),
        ActionChoiceKind.Push => FrontendTextMessage.Create(FrontendTextIds.InspectionActionPush, ("targetName", targetId.Value)),
        ActionChoiceKind.Transfer => FrontendTextMessage.Create(FrontendTextIds.InspectionActionTransfer, ("targetName", targetId.Value)),
        _ => FrontendTextMessage.Create(FrontendTextIds.InspectionActionGeneric, ("actionName", kind), ("targetName", targetId.Value))
    };

    private static string FailureText(FailureReason? failureReason, string? failureDetail) =>
        !string.IsNullOrWhiteSpace(failureDetail) ? failureDetail : failureReason?.ToString() ?? "unavailable";

    private static string FormatCoord(GridCoord coord) => $"{coord.X},{coord.Y}";
}
