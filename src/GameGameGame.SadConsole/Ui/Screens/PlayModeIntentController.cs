using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Components;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal enum PlayModeIntentKind
{
    MoveDirection,
    DefaultAction,
    ContextDirection,
    ContextCell,
    ContextEntity,
    RequestedVerb
}

internal enum PlayModeIntentOutcomeKind
{
    AutoSubmitted,
    PromptOpened,
    Explained,
    Cancelled,
    SubmittedFromPrompt
}

internal sealed record PlayModeIntentSeed(
    PlayModeIntentKind Kind,
    Direction? Direction = null,
    GridCoord? Cell = null,
    EntityId? EntityId = null,
    string? Verb = null)
{
    public static PlayModeIntentSeed Move(Direction direction) => new(PlayModeIntentKind.MoveDirection, Direction: direction);
}

internal sealed record PlayModeActionCandidate(
    string Label,
    bool IsValid,
    bool IsComplete,
    Func<GameplayRuntimeSubmission>? Submit = null,
    string? Explanation = null,
    Func<IReadOnlyList<PlayModeActionCandidate>>? Refine = null,
    string? RefineTitle = null,
    Func<PlayModePromptLayer, SadConsoleRect, IUiComponent>? RefinedPromptComponent = null,
    Direction? ShortcutDirection = null,
    GridCoord? FocusCoord = null);

internal sealed record PlayModePromptLayer(
    string Title,
    IReadOnlyList<PlayModeActionCandidate> Choices,
    int FocusedIndex = 0,
    Func<PlayModePromptLayer, SadConsoleRect, IUiComponent>? CustomComponent = null)
{
    public PlayModeActionCandidate? FocusedChoice =>
        FocusedIndex >= 0 && FocusedIndex < Choices.Count ? Choices[FocusedIndex] : null;
}

internal sealed record PlayModeIntentOutcome(
    PlayModeIntentOutcomeKind Kind,
    string Message,
    GameplayRuntimeSubmission? Submission = null,
    PlayModePromptLayer? Prompt = null);

internal sealed class PlayModeIntentController
{
    private readonly Func<PlayModeIntentSeed, IReadOnlyList<PlayModeActionCandidate>> _resolveCandidates;
    private readonly List<PlayModePromptLayer> _promptStack = [];

    public PlayModeIntentController(Func<PlayModeIntentSeed, IReadOnlyList<PlayModeActionCandidate>> resolveCandidates)
    {
        _resolveCandidates = resolveCandidates;
    }

    public IReadOnlyList<PlayModePromptLayer> PromptStack => _promptStack;
    public PlayModePromptLayer? CurrentPrompt => _promptStack.Count > 0 ? _promptStack[^1] : null;
    public string LastInputDescription { get; private set; } = "none";
    public IReadOnlyList<PlayModeActionCandidate> LastResolvedCandidates { get; private set; } = [];
    public PlayModeIntentOutcome? LastOutcome { get; private set; }

    public PlayModeIntentOutcome HandleIntent(PlayModeIntentSeed seed)
    {
        LastInputDescription = DescribeSeed(seed);
        LastResolvedCandidates = _resolveCandidates(seed).ToList();
        var validCandidates = LastResolvedCandidates
            .Where(candidate => candidate.IsValid)
            .ToList();

        if (validCandidates.Count == 0)
        {
            return Remember(new PlayModeIntentOutcome(
                PlayModeIntentOutcomeKind.Explained,
                "No valid action is available for that input."));
        }

        if (validCandidates.Count == 1 && validCandidates[0].IsComplete)
        {
            var candidate = validCandidates[0];
            var submission = candidate.Submit?.Invoke()
                ?? new GameplayRuntimeSubmission(false, "Candidate has no submission handler.", UsedCoreActionChoice: false);
            return Remember(new PlayModeIntentOutcome(
                PlayModeIntentOutcomeKind.AutoSubmitted,
                candidate.Label,
                submission));
        }

        var prompt = new PlayModePromptLayer(PromptTitle(seed), validCandidates);
        _promptStack.Add(prompt);
        return Remember(new PlayModeIntentOutcome(
            PlayModeIntentOutcomeKind.PromptOpened,
            prompt.Title,
            Prompt: prompt));
    }

    public PlayModeIntentOutcome SelectFocused()
    {
        LastInputDescription = "prompt Select";
        if (CurrentPrompt is not { } prompt || prompt.FocusedChoice is not { } choice)
        {
            return Remember(new PlayModeIntentOutcome(PlayModeIntentOutcomeKind.Explained, "No prompt is active."));
        }

        if (!choice.IsComplete)
        {
            if (choice.Refine is { } refine)
            {
                var choices = refine().Where(candidate => candidate.IsValid).ToList();
                if (choices.Count == 0)
                {
                    return Remember(new PlayModeIntentOutcome(PlayModeIntentOutcomeKind.Explained, choice.Explanation ?? "No valid refined choices are available."));
                }

                var nextPrompt = new PlayModePromptLayer(choice.RefineTitle ?? choice.Label, choices, CustomComponent: choice.RefinedPromptComponent);
                _promptStack.Add(nextPrompt);
                LastResolvedCandidates = choices;
                return Remember(new PlayModeIntentOutcome(PlayModeIntentOutcomeKind.PromptOpened, nextPrompt.Title, Prompt: nextPrompt));
            }

            return Remember(new PlayModeIntentOutcome(PlayModeIntentOutcomeKind.Explained, choice.Explanation ?? "That choice needs more information."));
        }

        var submission = choice.Submit?.Invoke()
            ?? new GameplayRuntimeSubmission(false, "Candidate has no submission handler.", UsedCoreActionChoice: false);
        _promptStack.Clear();
        return Remember(new PlayModeIntentOutcome(
            PlayModeIntentOutcomeKind.SubmittedFromPrompt,
            choice.Label,
            submission));
    }

    public PlayModeIntentOutcome SelectShortcutDirection(Direction direction)
    {
        LastInputDescription = $"prompt Direction {direction}";
        if (CurrentPrompt is not { } prompt)
        {
            return Remember(new PlayModeIntentOutcome(PlayModeIntentOutcomeKind.Explained, "No prompt is active."));
        }

        var matchIndex = prompt.Choices.ToList().FindIndex(choice => choice.ShortcutDirection == direction);
        if (matchIndex < 0)
        {
            return Remember(new PlayModeIntentOutcome(PlayModeIntentOutcomeKind.Explained, $"No prompt choice accepts {direction}."));
        }

        _promptStack[^1] = prompt with { FocusedIndex = matchIndex };
        var outcome = SelectFocused();
        LastInputDescription = $"prompt Direction {direction}";
        return outcome;
    }

    public PlayModeIntentOutcome MoveFocus(int delta)
    {
        return MoveFocusByDelta(delta, delta < 0 ? "prompt Up" : "prompt Down");
    }

    private PlayModeIntentOutcome MoveFocusByDelta(int delta, string inputDescription)
    {
        LastInputDescription = inputDescription;
        if (delta == 0 || CurrentPrompt is not { } prompt || prompt.Choices.Count == 0)
        {
            return Remember(new PlayModeIntentOutcome(PlayModeIntentOutcomeKind.Explained, "No prompt is active."));
        }

        var nextIndex = Math.Clamp(prompt.FocusedIndex + delta, 0, prompt.Choices.Count - 1);
        var nextPrompt = prompt with { FocusedIndex = nextIndex };
        _promptStack[^1] = nextPrompt;
        return Remember(new PlayModeIntentOutcome(PlayModeIntentOutcomeKind.PromptOpened, nextPrompt.FocusedChoice?.Label ?? nextPrompt.Title, Prompt: nextPrompt));
    }

    public PlayModeIntentOutcome MoveFocus(Direction direction)
    {
        LastInputDescription = $"prompt Navigate {direction}";
        if (CurrentPrompt is not { } prompt || prompt.Choices.Count == 0)
        {
            return Remember(new PlayModeIntentOutcome(PlayModeIntentOutcomeKind.Explained, "No prompt is active."));
        }

        if (prompt.FocusedChoice?.FocusCoord is { } origin)
        {
            var destination = origin.Offset(direction);
            var exactIndex = prompt.Choices.ToList().FindIndex(choice => choice.FocusCoord == destination);
            if (exactIndex >= 0)
            {
                var exactPrompt = prompt with { FocusedIndex = exactIndex };
                _promptStack[^1] = exactPrompt;
                return Remember(new PlayModeIntentOutcome(PlayModeIntentOutcomeKind.PromptOpened, exactPrompt.FocusedChoice?.Label ?? exactPrompt.Title, Prompt: exactPrompt));
            }
        }

        return direction switch
        {
            Direction.North or Direction.West => MoveFocusByDelta(-1, $"prompt Navigate {direction}"),
            Direction.South or Direction.East => MoveFocusByDelta(1, $"prompt Navigate {direction}"),
            _ => Remember(new PlayModeIntentOutcome(PlayModeIntentOutcomeKind.PromptOpened, prompt.FocusedChoice?.Label ?? prompt.Title, Prompt: prompt))
        };
    }

    public PlayModeIntentOutcome Cancel()
    {
        LastInputDescription = "prompt Cancel";
        if (_promptStack.Count == 0)
        {
            return Remember(new PlayModeIntentOutcome(PlayModeIntentOutcomeKind.Cancelled, "No prompt is active."));
        }

        _promptStack.RemoveAt(_promptStack.Count - 1);
        return Remember(new PlayModeIntentOutcome(PlayModeIntentOutcomeKind.Cancelled, "Prompt cancelled."));
    }

    public void ClearPrompts()
    {
        _promptStack.Clear();
    }

    private PlayModeIntentOutcome Remember(PlayModeIntentOutcome outcome)
    {
        LastOutcome = outcome;
        return outcome;
    }

    private static string DescribeSeed(PlayModeIntentSeed seed) => seed.Kind switch
    {
        PlayModeIntentKind.MoveDirection => $"MoveDirection {seed.Direction}",
        PlayModeIntentKind.DefaultAction => "DefaultAction",
        PlayModeIntentKind.ContextDirection => $"ContextDirection {seed.Direction}",
        PlayModeIntentKind.ContextCell => $"ContextCell {seed.Cell}",
        PlayModeIntentKind.ContextEntity => $"ContextEntity {seed.EntityId}",
        PlayModeIntentKind.RequestedVerb => $"RequestedVerb {seed.Verb}",
        _ => seed.Kind.ToString()
    };

    private static string PromptTitle(PlayModeIntentSeed seed) => seed.Kind switch
    {
        PlayModeIntentKind.MoveDirection => $"Choose action for {seed.Direction}",
        PlayModeIntentKind.DefaultAction => "Choose action",
        PlayModeIntentKind.ContextDirection => $"Choose action toward {seed.Direction}",
        PlayModeIntentKind.ContextCell => $"Choose action at {seed.Cell}",
        PlayModeIntentKind.ContextEntity => $"Choose action for {seed.EntityId}",
        PlayModeIntentKind.RequestedVerb => $"Choose {seed.Verb ?? "action"}",
        _ => "Choose action"
    };
}
