using GameGameGame.Content;
using GameGameGame.SadConsoleApp.Ui.Components;
using Microsoft.Xna.Framework.Graphics;
using SadConsole.Host;
using XnaColor = Microsoft.Xna.Framework.Color;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace GameGameGame.SadConsoleApp.Ui.Rendering;

internal sealed class ConnectorLineDrawCallRenderer
{
    private Texture2D? _pixel;

    public void Draw(
        IReadOnlyList<ConnectorLineViewModel> connectors,
        int originX,
        int originY,
        int cellWidth,
        int cellHeight,
        bool drawEndpoints)
    {
        if (connectors.Count == 0)
        {
            return;
        }

        var pixel = Pixel();
        foreach (var segment in connectors.SelectMany(connector => connector.Segments).OrderBy(segment => segment.Layer))
        {
            var start = CellAnchor(originX, originY, cellWidth, cellHeight, segment.Start);
            var end = CellAnchor(originX, originY, cellWidth, cellHeight, segment.End);
            var color = XnaColorForPresentation(segment.Color);
            DrawPixelLine(pixel, start, end, color, thickness: 2f);
            if (drawEndpoints)
            {
                DrawEndpoint(pixel, start, XnaColor.White, radius: 3);
                DrawEndpoint(pixel, end, color, radius: 3);
            }
        }
    }

    private Texture2D Pixel()
    {
        if (_pixel is not null)
        {
            return _pixel;
        }

        _pixel = new Texture2D(Global.GraphicsDevice, 1, 1);
        _pixel.SetData([XnaColor.White]);
        return _pixel;
    }

    private static XnaVector2 CellAnchor(int originX, int originY, int cellWidth, int cellHeight, ConnectorLineEndpoint endpoint) =>
        endpoint.PixelX is { } pixelX && endpoint.PixelY is { } pixelY
            ? new XnaVector2(originX + pixelX, originY + pixelY)
            : new XnaVector2(originX + (endpoint.CellX * cellWidth) + (cellWidth * endpoint.AnchorX), originY + (endpoint.CellY * cellHeight) + (cellHeight * endpoint.AnchorY));

    private static void DrawPixelLine(Texture2D pixel, XnaVector2 start, XnaVector2 end, XnaColor color, float thickness)
    {
        var delta = end - start;
        var length = delta.Length();
        if (length <= 0.01f)
        {
            return;
        }

        var rotation = MathF.Atan2(delta.Y, delta.X);
        Global.SharedSpriteBatch.Draw(pixel, start, null, color, rotation, new XnaVector2(0f, 0.5f), new XnaVector2(length, thickness), SpriteEffects.None, 0f);
    }

    private static void DrawEndpoint(Texture2D pixel, XnaVector2 center, XnaColor color, int radius)
    {
        Global.SharedSpriteBatch.Draw(pixel, new Microsoft.Xna.Framework.Rectangle((int)center.X - radius, (int)center.Y - radius, radius * 2, radius * 2), color);
    }

    private static XnaColor XnaColorForPresentation(PresentationColor color) => color switch
    {
        PresentationColor.Gray => XnaColor.Gray,
        PresentationColor.White => XnaColor.White,
        PresentationColor.Yellow => XnaColor.Gold,
        PresentationColor.Cyan => XnaColor.Cyan,
        PresentationColor.Green => XnaColor.Green,
        PresentationColor.DarkGreen => XnaColor.DarkGreen,
        PresentationColor.Earth => XnaColor.SaddleBrown,
        PresentationColor.Default => XnaColor.White,
        _ => XnaColor.White
    };
}
