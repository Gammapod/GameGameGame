using GameGameGame.Core;
using GameGameGame.Frontend.SadConsole;
using SadConsole.Input;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class PlayInputControllerTests
{
    [Fact]
    public void ConfirmIntentCarriesHeldDirectionSoSpaceWorksWhileAiming()
    {
        var intent = Read(keysDown: [Keys.Right], keysReleased: [Keys.Space]);

        Assert.Equal(PlayControlIntentKind.ConfirmMove, intent.Kind);
        Assert.Equal(Direction.East, intent.Direction);
    }

    [Fact]
    public void HeldDirectionProducesAimIntent()
    {
        var intent = Read(keysDown: [Keys.Up, Keys.Left]);

        Assert.Equal(PlayControlIntentKind.AimMove, intent.Kind);
        Assert.Equal(Direction.NorthWest, intent.Direction);
    }

    [Fact]
    public void MovementKeyReleaseClearsExistingAim()
    {
        var intent = Read(keysReleased: [Keys.Right], hasPreview: true);

        Assert.Equal(PlayControlIntentKind.ClearMoveAim, intent.Kind);
    }

    [Fact]
    public void IKeyReleaseTogglesPlayerPanelFocus()
    {
        var intent = Read(keysReleased: [Keys.I]);

        Assert.Equal(PlayControlIntentKind.TogglePlayerPanel, intent.Kind);
    }

    [Fact]
    public void InspectionFocusConsumesMovementKeysInsteadOfProducingGridInput()
    {
        var intent = PlayInspectionInputController.ReadKeys([Keys.Right]);

        Assert.Equal(PlayInspectionInputIntentKind.Consume, intent.Kind);
    }

    [Theory]
    [InlineData(Keys.Escape, (int)PlayInspectionInputIntentKind.ReturnToGrid)]
    [InlineData(Keys.Up, (int)PlayInspectionInputIntentKind.PreviousAction)]
    [InlineData(Keys.Down, (int)PlayInspectionInputIntentKind.NextAction)]
    [InlineData(Keys.Enter, (int)PlayInspectionInputIntentKind.ConfirmAction)]
    [InlineData(Keys.Space, (int)PlayInspectionInputIntentKind.ConfirmAction)]
    public void InspectionFocusMapsOnlyPanelCommands(Keys releasedKey, int expected)
    {
        var intent = PlayInspectionInputController.ReadKeys([releasedKey]);

        Assert.Equal((PlayInspectionInputIntentKind)expected, intent.Kind);
    }

    [Fact]
    public void InspectionFocusConsumesLeftDirectionInsteadOfReturningToGrid()
    {
        var intent = PlayInspectionInputController.ReadKeys([Keys.Left]);

        Assert.Equal(PlayInspectionInputIntentKind.Consume, intent.Kind);
    }

    private static PlayControlIntent Read(IReadOnlyList<Keys>? keysDown = null, IReadOnlyList<Keys>? keysReleased = null, bool hasPreview = false) =>
        PlayInputController.ReadKeys(
            keysDown ?? [],
            keysReleased ?? [],
            cancelReleased: keysReleased?.Contains(Keys.Escape) == true,
            playerPanelToggleReleased: keysReleased?.Contains(Keys.I) == true,
            confirmReleased: keysReleased?.Any(key => key is Keys.Space or Keys.Enter) == true,
            hasPreview);
}
