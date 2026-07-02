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
    public void BuildPlacesRemainingUnanchoredLocalLogRowsAfterContents()
    {
        var contentEntity = new EntityId("content");
        var separateEntity = new EntityId("separate");
        var rows = LocalActivityViewBuilder.Build(
            Panel(
                contents: [ContentRow(0, contentEntity, "Crate", 'c')],
                localLog: [Outcome("Crate already shown", true, contentEntity), Outcome("Spark fizzled", false, separateEntity)]),
            maxRows: 6);

        Assert.Equal(["Local activity", "0. c Crate [Inert]", "└ Spark fizzled"], rows.Select(row => row.Text));
        Assert.True(rows[2].IsWarning);
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
}
