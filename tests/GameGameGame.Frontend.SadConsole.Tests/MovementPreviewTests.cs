using GameGameGame.Core;
using GameGameGame.Frontend.SadConsole;
using SadConsole.Input;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class MovementPreviewTests
{
    [Theory]
    [InlineData(Keys.NumPad7, Direction.NorthWest)]
    [InlineData(Keys.NumPad8, Direction.North)]
    [InlineData(Keys.NumPad9, Direction.NorthEast)]
    [InlineData(Keys.NumPad4, Direction.West)]
    [InlineData(Keys.NumPad6, Direction.East)]
    [InlineData(Keys.NumPad1, Direction.SouthWest)]
    [InlineData(Keys.NumPad2, Direction.South)]
    [InlineData(Keys.NumPad3, Direction.SouthEast)]
    public void NumpadMovementKeysAimEightDirections(Keys key, Direction expected)
    {
        Assert.Equal(expected, MovementPreviewKeyboardReader.ReadHeldDirection([key]));
    }

    [Fact]
    public void HeldCardinalKeysCombineIntoDiagonalMovementPreview()
    {
        Assert.Equal(Direction.NorthEast, MovementPreviewKeyboardReader.ReadHeldDirection([Keys.Up, Keys.Right]));
        Assert.Equal(Direction.SouthWest, MovementPreviewKeyboardReader.ReadHeldDirection([Keys.Down, Keys.Left]));
        Assert.Equal(Direction.NorthEast, MovementPreviewKeyboardReader.ReadHeldDirection([Keys.W, Keys.D]));
    }

    [Fact]
    public void MovementPreviewComputesDestinationFromCurrentActorCoord()
    {
        var preview = new MovementPreviewState();
        preview.Set(Direction.NorthEast);

        Assert.True(preview.TryDestination(new GridCoord(4, 3), out var destination));
        Assert.Equal(new GridCoord(5, 2), destination);
    }

    [Fact]
    public void MovementPreviewCanBeReplacedAndCleared()
    {
        var preview = new MovementPreviewState();

        preview.Set(Direction.North);
        preview.Set(Direction.West);
        Assert.Equal(Direction.West, preview.Direction);

        preview.Clear();
        Assert.False(preview.HasPreview);
    }

    [Fact]
    public void MovementPreviewReaderRecognizesMovementKeysForReleaseClearing()
    {
        Assert.True(MovementPreviewKeyboardReader.IsMovementKey(Keys.Up));
        Assert.True(MovementPreviewKeyboardReader.IsMovementKey(Keys.NumPad9));
        Assert.True(MovementPreviewKeyboardReader.IsMovementKey(Keys.A));
        Assert.False(MovementPreviewKeyboardReader.IsMovementKey(Keys.Space));
    }

    [Fact]
    public void ConfirmMovementUsesPreviewDirectionBeforeFacingFallback()
    {
        var preview = new MovementPreviewState();
        preview.Set(Direction.West);

        var direction = MovementPreviewConfirmation.ResolveDirection(preview, Direction.North);

        Assert.Equal(Direction.West, direction);
    }

    [Fact]
    public void ConfirmMovementFallsBackToCurrentFacingWhenNoPreviewExists()
    {
        var preview = new MovementPreviewState();

        var direction = MovementPreviewConfirmation.ResolveDirection(preview, Direction.NorthEast);

        Assert.Equal(Direction.NorthEast, direction);
    }

    [Fact]
    public void ConfirmMovementCanUseHeldDirectionBeforeFacingFallback()
    {
        var preview = new MovementPreviewState();

        var direction = MovementPreviewConfirmation.ResolveAfterApplyingHeldDirection(preview, Direction.East, Direction.North);

        Assert.Equal(Direction.East, direction);
        Assert.Equal(Direction.East, preview.Direction);
    }
}
