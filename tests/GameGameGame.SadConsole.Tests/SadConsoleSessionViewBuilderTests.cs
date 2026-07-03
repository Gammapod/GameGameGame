using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp;

namespace GameGameGame.SadConsole.Tests;

public sealed class SadConsoleSessionViewBuilderTests
{
    [Fact]
    public void BuildLabelsGlobalLogAsActionLogWhenEmpty()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var view = Builder(session).Build(session, State(ShellMode.Play));

        Assert.Equal("Global action log", view.GlobalLog.Title);
        Assert.Equal("No action outcomes recorded yet.", view.GlobalLog.EmptyText);
        Assert.Empty(view.GlobalLog.Rows);
    }

    [Fact]
    public void BuildCarriesStructuredLogRowsWithGeneralActionTitle()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var outcome = Outcome("Player moved East", succeeded: true, session.PlayerEntityId);
        var view = Builder(session).Build(session, State(ShellMode.Play, actionLog: new ActionLogProjection([outcome])));

        Assert.Equal("Global action log", view.GlobalLog.Title);
        Assert.Equal([outcome], view.GlobalLog.Rows);
    }

    [Fact]
    public void BuildCarriesAutonomousActorRowsInGlobalActionLog()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var slime = new EntityId("slime");
        var autonomousFailure = Outcome("Slime: tried to move East, but blocked", succeeded: false, slime) with
        {
            ConsumedTurn = true,
            ActionStepAttempts = [Attempt("MoveTarget", TraceStatus.Failure, FailureReason.MoveBlocked, "blocked")]
        };

        var view = Builder(session).Build(session, State(ShellMode.Play, actionLog: new ActionLogProjection([autonomousFailure])));

        Assert.Equal([autonomousFailure], view.GlobalLog.Rows);
        Assert.Equal("FAIL: Slime: tried to move East, but blocked [MoveTarget failed: blocked]", ActionOutcomeTextFormatter.FormatGlobal(autonomousFailure));
    }

    [Fact]
    public void BuildPlayPromptPresentsUndoAsUnavailableAtFrameZero()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var view = Builder(session).Build(session, State(ShellMode.Play, canUndo: false));

        Assert.Contains("U undo (unavailable at frame 0)", view.PromptHint);
    }

    [Fact]
    public void BuildPlayPromptPresentsUndoAsAvailableWhenHistoryCanRollback()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var view = Builder(session).Build(session, State(ShellMode.Play, canUndo: true));

        Assert.Contains("U undo (available)", view.PromptHint);
    }

    [Fact]
    public void BuildCreatesPanelViewsFromVisiblePanelChain()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var view = Builder(session).Build(session, State(ShellMode.Play));

        Assert.NotEmpty(view.Panels);
        Assert.Equal(session.PlayerEntityId, view.Panels.Last().Projection.EntityId);
        Assert.Equal("Inspection", view.Panels.Last().Title);
        Assert.All(view.Panels, panel => Assert.True(panel.Bounds.Width > 0));
    }

    [Fact]
    public void BuildKeepsSelectionAndCursorAsPresentationState()
    {
        var session = PlayableScenarioLauncher.CreatePrototype();
        var playerLocation = session.World.GetEntityLocation(session.PlayerEntityId);
        var cursor = playerLocation.Coord;

        var view = Builder(session).Build(
            session,
            State(
                ShellMode.InspectSource,
                selectedEntity: session.PlayerEntityId,
                worldCursor: cursor));

        Assert.StartsWith($"Selected: {session.World.Entities[session.PlayerEntityId].Name}@", view.SelectedSummary);
        Assert.Equal("Inspect: gold cursor selects visible entities in the current container panel.", view.PromptHint);
        Assert.Contains(view.Panels, panel => panel.Cursor == cursor);
    }

    private static SadConsoleSessionViewBuilder Builder(PlayableScenarioSession session) => new(
        new EntityPanelProjectionService(entityId => session.Registry.GetPresentationForEntity(entityId).ToInspectionAppearance()),
        new ControlledActorAffordanceService(new MovementService()));

    private static SadConsoleSessionViewBuilderState State(
        ShellMode mode,
        EntityId? selectedEntity = null,
        EntityId? inspectedEntity = null,
        GridCoord? worldCursor = null,
        GridCoord? inventoryCursor = null,
        ActionLogProjection? actionLog = null,
        bool canUndo = false) => new(
        mode,
        "test message",
        selectedEntity,
        inspectedEntity,
        worldCursor ?? new GridCoord(0, 0),
        inventoryCursor ?? new GridCoord(0, 0),
        actionLog,
        canUndo);

    private static ActionOutcome Outcome(string sentence, bool succeeded, EntityId anchor) => new(
        null,
        anchor,
        anchor.Value,
        "test",
        succeeded,
        null,
        null,
        null,
        null,
        null,
        succeeded ? null : FailureReason.MoveBlocked,
        null,
        sentence,
        new HashSet<EntityId> { anchor },
        new HashSet<PlaneId>(),
        TraceNode.Info("test"));

    private static ActionStepAttempt Attempt(string stepKind, TraceStatus status, FailureReason? reason, string? detail) => new(
        1,
        stepKind,
        status,
        reason,
        detail,
        Continued: false,
        Stopped: true,
        [],
        [],
        [],
        TraceNode.Info(stepKind));
}
