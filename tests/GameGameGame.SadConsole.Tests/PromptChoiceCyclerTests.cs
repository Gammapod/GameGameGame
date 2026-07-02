using GameGameGame.Core;
using GameGameGame.SadConsoleApp;

namespace GameGameGame.SadConsole.Tests;

public sealed class PromptChoiceCyclerTests
{
    private static readonly PlaneId Plane = new("plane");
    private static readonly PlaneId OtherPlane = new("other");

    [Fact]
    public void FirstValidCoordFiltersByPlaneAndUsesRowMajorOrder()
    {
        var candidates = new PlaneCoord?[]
        {
            new(OtherPlane, new GridCoord(0, 0)),
            new(Plane, new GridCoord(2, 1)),
            new(Plane, new GridCoord(1, 0)),
            new(Plane, new GridCoord(0, 0))
        };

        Assert.Equal(new GridCoord(0, 0), PromptChoiceCycler.FirstValidCoord(candidates, Plane));
    }

    [Fact]
    public void CycleFromUnknownSelectionChoosesFirstRowMajorCandidate()
    {
        var result = PromptChoiceCycler.Cycle(Candidates((2, 0), (0, 1), (1, 0)), Plane, new GridCoord(9, 9), "Pickup source");

        Assert.True(result.HasChoice);
        Assert.Equal(new GridCoord(1, 0), result.Cursor);
        Assert.Equal("Pickup source: selected (1,0). Tab cycles, Enter confirms.", result.Message);
    }

    [Fact]
    public void CycleWrapsFromLastCandidateToFirst()
    {
        var result = PromptChoiceCycler.Cycle(Candidates((0, 0), (1, 0)), Plane, new GridCoord(1, 0), "Inspect target");

        Assert.True(result.HasChoice);
        Assert.Equal(new GridCoord(0, 0), result.Cursor);
    }

    [Fact]
    public void CycleWithNoCandidatesReturnsExplicitNoChoiceMessageAndKeepsCursor()
    {
        var cursor = new GridCoord(3, 4);
        var result = PromptChoiceCycler.Cycle([new PlaneCoord(OtherPlane, new GridCoord(0, 0))], Plane, cursor, "Exit");

        Assert.False(result.HasChoice);
        Assert.Equal(cursor, result.Cursor);
        Assert.Equal("Exit: no valid choices.", result.Message);
    }

    private static IReadOnlyList<PlaneCoord?> Candidates(params (int X, int Y)[] coords) =>
        coords.Select(coord => (PlaneCoord?)new PlaneCoord(Plane, new GridCoord(coord.X, coord.Y))).ToList();
}
