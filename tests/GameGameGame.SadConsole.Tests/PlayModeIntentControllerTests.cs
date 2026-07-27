using GameGameGame.SadConsoleApp;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Screens;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Styling;

namespace GameGameGame.SadConsole.Tests;

public sealed class PlayModeIntentControllerTests
{
    [Fact]
    public void HandleIntentAutoSubmitsOneCompleteValidCandidate()
    {
        var submitted = false;
        var controller = new PlayModeIntentController(_ =>
        [
            new PlayModeActionCandidate(
                "Move East",
                IsValid: true,
                IsComplete: true,
                Submit: () =>
                {
                    submitted = true;
                    return new GameplayRuntimeSubmission(true, null, UsedCoreActionChoice: false);
                })
        ]);

        var outcome = controller.HandleIntent(new PlayModeIntentSeed(PlayModeIntentKind.DefaultAction));

        Assert.True(submitted);
        Assert.Equal(PlayModeIntentOutcomeKind.AutoSubmitted, outcome.Kind);
        Assert.True(outcome.Submission?.Succeeded);
        Assert.Empty(controller.PromptStack);
    }

    [Fact]
    public void HandleIntentOpensPromptForMultipleValidCandidates()
    {
        var controller = new PlayModeIntentController(_ =>
        [
            new PlayModeActionCandidate("Pick up rock", IsValid: true, IsComplete: true),
            new PlayModeActionCandidate("Enter chest", IsValid: true, IsComplete: true)
        ]);

        var outcome = controller.HandleIntent(new PlayModeIntentSeed(PlayModeIntentKind.DefaultAction));

        Assert.Equal(PlayModeIntentOutcomeKind.PromptOpened, outcome.Kind);
        var prompt = Assert.Single(controller.PromptStack);
        Assert.Equal("Choose action", prompt.Title);
        Assert.Equal(2, prompt.Choices.Count);
    }

    [Fact]
    public void HandleIntentOpensPromptForOneIncompleteCandidate()
    {
        var controller = new PlayModeIntentController(_ =>
        [
            new PlayModeActionCandidate("Pick up which item?", IsValid: true, IsComplete: false)
        ]);

        var outcome = controller.HandleIntent(new PlayModeIntentSeed(PlayModeIntentKind.DefaultAction));

        Assert.Equal(PlayModeIntentOutcomeKind.PromptOpened, outcome.Kind);
        var prompt = Assert.Single(controller.PromptStack);
        Assert.Equal("Pick up which item?", Assert.Single(prompt.Choices).Label);
    }

    [Fact]
    public void SelectFocusedIncompleteCandidateWithRefinementPushesNestedPrompt()
    {
        var controller = new PlayModeIntentController(_ =>
        [
            new PlayModeActionCandidate(
                "Pick up rock",
                IsValid: true,
                IsComplete: false,
                RefineTitle: "Pick up rock: choose destination",
                Refine: () =>
                [
                    new PlayModeActionCandidate("to inventory@(0,0)", IsValid: true, IsComplete: true)
                ])
        ]);
        controller.HandleIntent(new PlayModeIntentSeed(PlayModeIntentKind.DefaultAction));

        var outcome = controller.SelectFocused();

        Assert.Equal(PlayModeIntentOutcomeKind.PromptOpened, outcome.Kind);
        Assert.Equal(2, controller.PromptStack.Count);
        Assert.Equal("Pick up rock: choose destination", controller.CurrentPrompt?.Title);
        Assert.Equal("to inventory@(0,0)", controller.CurrentPrompt?.FocusedChoice?.Label);
    }

    [Fact]
    public void RefinedPromptCanCarryCustomComponentFactory()
    {
        var controller = new PlayModeIntentController(_ =>
        [
            new PlayModeActionCandidate(
                "Transfer with chest",
                IsValid: true,
                IsComplete: false,
                RefineTitle: "Transfer with chest",
                Refine: () => [new PlayModeActionCandidate("Give rock", IsValid: true, IsComplete: true)],
                RefinedPromptComponent: (prompt, bounds) => new PanelComponent("transfer-test", prompt.Title, bounds, [prompt.FocusedChoice?.Label ?? "none"]))
        ]);
        controller.HandleIntent(new PlayModeIntentSeed(PlayModeIntentKind.DefaultAction));

        controller.SelectFocused();
        var component = controller.CurrentPrompt!.CustomComponent!(controller.CurrentPrompt, SadConsoleRect.FromSize(2, 3, 20, 5));

        Assert.Equal("transfer-test", component.Id);
        Assert.Equal("Transfer with chest", component.Title);
        Assert.Equal(SadConsoleRect.FromSize(2, 3, 20, 5), component.Bounds);
        Assert.Contains("Give rock", component.RenderRows(SadConsoleTheme.Default));
    }

    [Fact]
    public void SelectFocusedCompleteCandidateClearsNestedPromptStack()
    {
        var controller = new PlayModeIntentController(_ =>
        [
            new PlayModeActionCandidate(
                "Pick up rock",
                IsValid: true,
                IsComplete: false,
                Refine: () =>
                [
                    new PlayModeActionCandidate("to inventory@(0,0)", IsValid: true, IsComplete: true, Submit: () => new GameplayRuntimeSubmission(true, null, false))
                ])
        ]);
        controller.HandleIntent(new PlayModeIntentSeed(PlayModeIntentKind.DefaultAction));
        controller.SelectFocused();

        controller.SelectFocused();

        Assert.Empty(controller.PromptStack);
    }

    [Fact]
    public void MoveFocusDirectionUsesCandidateFocusCoordinates()
    {
        var controller = new PlayModeIntentController(_ =>
        [
            new PlayModeActionCandidate("0,0", IsValid: true, IsComplete: true, FocusCoord: new GridCoord(0, 0)),
            new PlayModeActionCandidate("1,0", IsValid: true, IsComplete: true, FocusCoord: new GridCoord(1, 0)),
            new PlayModeActionCandidate("0,1", IsValid: true, IsComplete: true, FocusCoord: new GridCoord(0, 1))
        ]);
        controller.HandleIntent(new PlayModeIntentSeed(PlayModeIntentKind.DefaultAction));

        controller.MoveFocus(Direction.East);

        Assert.Equal("1,0", controller.CurrentPrompt?.FocusedChoice?.Label);
    }

    [Fact]
    public void HandleIntentExplainsWhenNoValidCandidatesExist()
    {
        var controller = new PlayModeIntentController(_ =>
        [
            new PlayModeActionCandidate("Blocked", IsValid: false, IsComplete: true)
        ]);

        var outcome = controller.HandleIntent(new PlayModeIntentSeed(PlayModeIntentKind.DefaultAction));

        Assert.Equal(PlayModeIntentOutcomeKind.Explained, outcome.Kind);
        Assert.Empty(controller.PromptStack);
    }

    [Fact]
    public void CancelUnwindsOnePromptLayerWithoutSubmitting()
    {
        var submitted = false;
        var controller = new PlayModeIntentController(_ =>
        [
            new PlayModeActionCandidate("Pick up rock", IsValid: true, IsComplete: true, Submit: () =>
            {
                submitted = true;
                return new GameplayRuntimeSubmission(true, null, UsedCoreActionChoice: false);
            }),
            new PlayModeActionCandidate("Enter chest", IsValid: true, IsComplete: true)
        ]);
        controller.HandleIntent(new PlayModeIntentSeed(PlayModeIntentKind.DefaultAction));

        var outcome = controller.Cancel();

        Assert.False(submitted);
        Assert.Equal(PlayModeIntentOutcomeKind.Cancelled, outcome.Kind);
        Assert.Empty(controller.PromptStack);
    }

    [Fact]
    public void MoveFocusChangesFocusedPromptChoice()
    {
        var controller = new PlayModeIntentController(_ =>
        [
            new PlayModeActionCandidate("Pick up rock", IsValid: true, IsComplete: true),
            new PlayModeActionCandidate("Enter chest", IsValid: true, IsComplete: true)
        ]);
        controller.HandleIntent(new PlayModeIntentSeed(PlayModeIntentKind.DefaultAction));

        controller.MoveFocus(1);

        Assert.Equal("Enter chest", controller.CurrentPrompt?.FocusedChoice?.Label);
    }

    [Fact]
    public void ControllerRemembersLastIntentCandidatesOutcomeAndPromptInput()
    {
        var controller = new PlayModeIntentController(_ =>
        [
            new PlayModeActionCandidate("Pick up rock", IsValid: true, IsComplete: true),
            new PlayModeActionCandidate("Enter chest", IsValid: true, IsComplete: true),
            new PlayModeActionCandidate("Blocked", IsValid: false, IsComplete: true, Explanation: "blocked")
        ]);

        var outcome = controller.HandleIntent(new PlayModeIntentSeed(PlayModeIntentKind.ContextDirection, Direction: Direction.North));

        Assert.Equal("ContextDirection North", controller.LastInputDescription);
        Assert.Equal(3, controller.LastResolvedCandidates.Count);
        Assert.Same(outcome, controller.LastOutcome);
        Assert.Equal(PlayModeIntentOutcomeKind.PromptOpened, controller.LastOutcome?.Kind);

        controller.MoveFocus(Direction.South);

        Assert.Equal("prompt Navigate South", controller.LastInputDescription);
    }
}
