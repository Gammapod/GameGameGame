namespace GameGameGame.Frontend.SadConsole;

internal sealed record ScenarioBrowserLayout(
    FrontendRect Bounds,
    int TitleY,
    int SummaryY,
    int HeadingY,
    int ListY,
    int ListHeight,
    int MessageY,
    int FooterY,
    int TextX,
    int TextWidth)
{
    public static ScenarioBrowserLayout Resolve(FrontendRect drawableBounds)
    {
        var textX = drawableBounds.X + 1;
        var textWidth = Math.Max(0, drawableBounds.Width - 2);
        var footerY = drawableBounds.Bottom - 1;
        var messageY = drawableBounds.Bottom - 2;
        var listY = drawableBounds.Y + 5;
        var listHeight = Math.Max(0, messageY - listY - 1);

        return new ScenarioBrowserLayout(
            drawableBounds,
            TitleY: drawableBounds.Y,
            SummaryY: drawableBounds.Y + 1,
            HeadingY: drawableBounds.Y + 3,
            ListY: listY,
            ListHeight: listHeight,
            MessageY: messageY,
            FooterY: footerY,
            TextX: textX,
            TextWidth: textWidth);
    }
}
