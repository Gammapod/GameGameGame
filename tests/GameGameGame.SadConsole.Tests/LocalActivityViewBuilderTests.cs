using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp;

namespace GameGameGame.SadConsole.Tests;

public sealed class LocalActivityViewBuilderTests
{
    [Fact]
    public void BuildShowsHonestEmptyTextForNoLocalActivity()
    {
        var rows = LocalActivityViewBuilder.Build(Panel(contents: [], localLog: []), maxRows: 4);

        Assert.Equal(["Local activity", LocalActivityViewBuilder.EmptyText], rows.Select(row => row.Text));
        Assert.True(rows[1].IsMuted);
    }

    [Fact]
    public void BuildPreservesContentRowsAndPreviousActionSnippets()
    {
        var actor = new EntityId("actor");
        var rows = LocalActivityViewBuilder.Build(
            Panel(
                contents: [ContentRow(0, actor, "Slime", 's', "Slime bumped the wall")],
                localLog: []),
            maxRows: 4);

        Assert.Equal(["Local activity", "0. s Slime [Actor]", "└ Slime bumped the wall"], rows.Select(row => row.Text));
        Assert.True(rows[2].IsPositive);
    }

    [Fact]
    public void BuildSuppressesLocalLogRowsThatDoNotBelongToVisibleContent()
    {
        var contentEntity = new EntityId("content");
        var separateEntity = new EntityId("separate");
        var rows = LocalActivityViewBuilder.Build(
            Panel(
                contents: [ContentRow(0, contentEntity, "Crate", 'c')],
                localLog: [Outcome("Crate already shown", true, contentEntity), Outcome("Spark fizzled", false, separateEntity)]),
            maxRows: 6);

        Assert.Equal(["Local activity", "0. c Crate [Inert]", "└ OK: Crate already shown (no turn)"], rows.Select(row => row.Text));
        Assert.True(rows[2].IsPositive);
    }

    [Fact]
    public void BuildShowsEmptyTextWhenOnlyPanelEntityLogExistsWithoutVisibleContents()
    {
        var player = new EntityId("player");

        var rows = LocalActivityViewBuilder.Build(
            Panel(
                contents: [],
                localLog: [Outcome("Player moved East", true, player)]),
            maxRows: 4);

        Assert.Equal(["Local activity", LocalActivityViewBuilder.EmptyText], rows.Select(row => row.Text));
        Assert.True(rows[1].IsMuted);
    }

    [Fact]
    public void BuildShowsAutonomousFailureForAnchoredContentRowWithFailureStyling()
    {
        var slime = new EntityId("slime");
        var failure = Outcome("Slime: tried to move East, but blocked", false, slime) with
        {
            ConsumedTurn = true,
            ActionStepAttempts = [Attempt("MoveTarget", TraceStatus.Failure, FailureReason.MoveBlocked, "blocked")]
        };

        var rows = LocalActivityViewBuilder.Build(
            Panel(
                contents: [ContentRow(0, slime, "Slime", 's', "stale previous action")],
                localLog: [failure]),
            maxRows: 4);

        Assert.Equal(["Local activity", "0. s Slime [Actor]", "└ FAIL: 1. MoveTarget - blocked [stopped]"], rows.Select(row => row.Text));
        Assert.True(rows[2].IsWarning);
        Assert.False(rows[2].IsPositive);
    }

    [Fact]
    public void BuildShowsFullCanonicalActionStepChainForAnchoredContentRow()
    {
        var slime = new EntityId("slime");
        var outcome = Outcome("Big Slime: ReverseFacing", true, slime) with
        {
            ConsumedTurn = true,
            ActionStepAttempts =
            [
                Attempt(
                    1,
                    "MoveFacing",
                    TraceStatus.Failure,
                    FailureReason.InvalidPlacement,
                    "blocked by wall",
                    continued: true,
                    stopped: false,
                    stateReads: ["Facing=East"],
                    stateWrites: ["Target=wall"]),
                Attempt(
                    2,
                    "ReverseFacing",
                    TraceStatus.Success,
                    null,
                    null,
                    continued: false,
                    stopped: true,
                    stateWrites: ["Facing=West"])
            ]
        };

        var rows = LocalActivityViewBuilder.Build(
            Panel(
                contents: [ContentRow(1, slime, "Big Slime", 'S', "stale previous action")],
                localLog: [outcome]),
            maxRows: 8);

        Assert.Equal(
            [
                "Local activity",
                "1. S Big Slime [Actor]",
                "├ FAIL: 1. MoveFacing - blocked by wall [continued]",
                "└ OK: 2. ReverseFacing [stopped]"
            ],
            rows.Select(row => row.Text));
        Assert.True(rows[2].IsWarning);
        Assert.True(rows[3].IsPositive);
    }

    [Fact]
    public void BuildShowsAutonomousSuccessForAnchoredContentRow()
    {
        var slime = new EntityId("slime");
        var success = Outcome("Slime: moved East", true, slime) with { ConsumedTurn = true };

        var rows = LocalActivityViewBuilder.Build(
            Panel(
                contents: [ContentRow(0, slime, "Slime", 's')],
                localLog: [success]),
            maxRows: 4);

        Assert.Equal(["Local activity", "0. s Slime [Inert]", "└ OK: Slime: moved East"], rows.Select(row => row.Text));
        Assert.True(rows[2].IsPositive);
    }

    private static EntityPanelProjection Panel(IReadOnlyList<EntityPanelContentRow> contents, IReadOnlyList<ActionOutcome> localLog) => new(
        new EntityId("panel"),
        "Panel",
        '#',
        PresentationColor.Gray,
        new PlaneCoord(new PlaneId("plane"), new GridCoord(0, 0)),
        new EntityContainmentPath(new EntityId("panel"), EntityContainmentPathStatus.Complete, [], [], []),
        [],
        new EntityPanelActionStateProjection(null, null, new Dictionary<int, EntityId>()),
        null,
        null,
        contents,
        localLog);

    private static EntityPanelContentRow ContentRow(int order, EntityId entityId, string name, char glyph, string previousAction = "") => new(
        order,
        entityId,
        name,
        glyph,
        new PlaneCoord(new PlaneId("plane"), new GridCoord(order, 0)),
        previousAction.Length == 0 ? LocalTurnParticipation.Inert : LocalTurnParticipation.Actor,
        previousAction);

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

    private static ActionStepAttempt Attempt(string stepKind, TraceStatus status, FailureReason? reason, string? detail) =>
        Attempt(1, stepKind, status, reason, detail, continued: false, stopped: true);

    private static ActionStepAttempt Attempt(
        int order,
        string stepKind,
        TraceStatus status,
        FailureReason? reason,
        string? detail,
        bool continued,
        bool stopped,
        IReadOnlyList<string>? stateReads = null,
        IReadOnlyList<string>? stateWrites = null,
        IReadOnlyList<string>? results = null) => new(
        order,
        stepKind,
        status,
        reason,
        detail,
        continued,
        stopped,
        stateReads ?? [],
        stateWrites ?? [],
        results ?? [],
        TraceNode.Info(stepKind));
}
