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

    private static PlayControlIntent Read(IReadOnlyList<Keys>? keysDown = null, IReadOnlyList<Keys>? keysReleased = null, bool hasPreview = false) =>
        PlayInputController.ReadKeys(
            keysDown ?? [],
            keysReleased ?? [],
            cancelReleased: keysReleased?.Contains(Keys.Escape) == true,
            confirmReleased: keysReleased?.Any(key => key is Keys.Space or Keys.Enter) == true,
            hasPreview);
}
