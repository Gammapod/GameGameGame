using GameGameGame.Content;
using GameGameGame.SadConsoleApp.Ui.Screens;
using GameGameGame.SadConsoleApp.Ui.Styling;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using Console = SadConsole.Console;

namespace GameGameGame.SadConsoleApp.Ui.Rendering;

internal sealed class GameplayMockConsole : Console
{
    private readonly SadConsoleTheme _theme;
    private readonly SadConsoleComponentRenderer _renderer;
    private Console? _hudLayer;
    private readonly GameplayMockScreen? _screen;
    private string _message;

    public GameplayMockConsole(SadConsoleStartup startup, SadConsoleTheme? theme = null) : base(SadConsoleScreenMetrics.ScreenWidth, SadConsoleScreenMetrics.ScreenHeight)
    {
        _theme = theme ?? SadConsoleTheme.Default;
        _renderer = new SadConsoleComponentRenderer(this, _theme);
        UseKeyboard = true;
        IsFocused = true;
        FocusedMode = FocusBehavior.Set;

        try
        {
            if (string.IsNullOrWhiteSpace(startup.DirectContentPath) || string.IsNullOrWhiteSpace(startup.DirectScenarioId))
            {
                _message = "Play mock requires --play-mock <file> <scenario-id>.";
            }
            else
            {
                var session = PlayableScenarioLauncher.CreateFromFile(startup.DirectContentPath, startup.DirectScenarioId);
                _screen = new GameplayMockScreen(session);
                _message = "Turn-0 Play UX mock. I cycles inspect targets. Esc exits.";
            }
        }
        catch (Exception ex)
        {
            _message = $"Could not launch Play UX mock: {ex.Message}";
        }

        Redraw();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyReleased(Keys.Escape))
        {
            SadConsole.Game.Instance.MonoGameInstance.Exit();
            return true;
        }

        if (keyboard.IsKeyReleased(Keys.I) && _screen is not null)
        {
            _message = _screen.InspectNextEntity();
            Redraw();
            return true;
        }

        return false;
    }

    private void Redraw()
    {
        _renderer.ClearSurface();
        if (_screen is null)
        {
            _renderer.PrintClipped(1, 0, Width - 2, "Play UX Mock", Color.Yellow);
            _renderer.PrintClipped(1, 2, Width - 2, _message, Color.Red);
            Surface.IsDirty = true;
            return;
        }

        var frame = _screen.BuildFrame(Width, Height);
        foreach (var component in frame.Components.Where(component => component.Id is not "current-place" and not "inspected-entity"))
        {
            _renderer.DrawComponent(component);
        }

        DrawInventoryPanel(
            frame.CurrentPlaceProjection,
            frame.CurrentPlaceBounds,
            frame.CurrentPlaceProjection is null ? "Current place viewport" : $"Current place: {frame.CurrentPlaceProjection.Name}",
            [.. new[] { "player POV current place", $"room size: {frame.CurrentRoomSizeLabel}", frame.CurrentPlaceProjection?.InventoryGrid is { } currentGrid ? $"inventory: {currentGrid.Width}x{currentGrid.Height} {currentGrid.PlaneId}" : "inventory: none" }, .. frame.CurrentPlaceEntityRows],
            [],
            [$"log: {_message}"],
            Color.Gold);

        if (frame.InspectedProjection is { } inspectedProjection)
        {
            var rows = new List<string>
            {
                $"{inspectedProjection.Glyph} {inspectedProjection.Name}",
                $"id: {inspectedProjection.EntityId}",
                $"path: {FormatBreadcrumb(inspectedProjection)}"
            };
            rows.AddRange(inspectedProjection.Properties.Take(3).Select(property => $"{property.Name}: {property.Value}"));
            DrawInventoryPanel(
                inspectedProjection,
                frame.InspectionBounds,
                "Inspected entity panel",
                rows,
                [.. frame.InspectedTargetingRows, .. frame.InspectedActionPlanRows],
                [],
                Color.HotPink);
        }

        DrawHud(frame);
        Surface.IsDirty = true;
    }

    private void DrawInventoryPanel(EntityPanelProjection? projection, SadConsoleRect bounds, string title, IReadOnlyList<string> rows, IReadOnlyList<string> afterGridRows, IReadOnlyList<string> footerRows, Color border)
    {
        DrawBox(bounds, border);
        PrintOnMain(bounds.Left + 2, bounds.Top, Math.Max(0, bounds.Width - 4), title, Color.White);

        var y = bounds.Top + 1;
        foreach (var row in rows)
        {
            if (y >= bounds.Bottom - 2) break;
            PrintOnMain(bounds.Left + 1, y++, Math.Max(0, bounds.Width - 2), row, Color.LightGray);
        }

        if (projection?.InventoryGrid is not { } grid)
        {
            PrintOnMain(bounds.Left + 1, y, Math.Max(0, bounds.Width - 2), "no usable inventory", Color.DarkGray);
            return;
        }

        foreach (var footer in footerRows.Select((row, index) => (row, index)))
        {
            var footerY = bounds.Bottom - 2 - footerRows.Count + footer.index;
            if (footerY > bounds.Top && footerY < bounds.Bottom - 1)
            {
                PrintOnMain(bounds.Left + 1, footerY, Math.Max(0, bounds.Width - 2), footer.row, Color.Gray);
            }
        }

        var cellWidth = 3;
        var gridPixelWidth = grid.Width * cellWidth;
        var footerReservedRows = footerRows.Count == 0 ? 0 : footerRows.Count + 1;
        var gridAreaTop = Math.Min(bounds.Bottom - 2, y + 1);
        var gridAreaBottom = Math.Max(gridAreaTop, bounds.Bottom - 1 - footerReservedRows);
        var gridLeft = Math.Max(bounds.Left + 4, bounds.Left + ((bounds.Width - gridPixelWidth) / 2));
        var gridTop = Math.Max(gridAreaTop, gridAreaTop + ((gridAreaBottom - gridAreaTop - grid.Height) / 2));
        var cells = grid.Cells.ToDictionary(cell => cell.Coord);
        var gridBottom = gridTop;
        for (var row = 0; row < grid.Height && gridTop + row < gridAreaBottom; row++)
        {
            gridBottom = gridTop + row + 1;
            PrintOnMain(bounds.Left + 1, gridTop + row, 3, $"{row,2}:", Color.DarkGray);
            for (var column = 0; column < grid.Width; column++)
            {
                var x = gridLeft + column * cellWidth;
                if (x + 2 >= bounds.Left + bounds.Width - 1) break;

                var coord = new GameGameGame.Core.GridCoord(column, row);
                var cell = cells.GetValueOrDefault(coord);
                var foreground = cell is null ? Color.DarkGray : ColorForPresentation(cell.Color);
                var glyph = cell?.Glyph ?? '.';
                var background = cell?.EntityId == _screen?.PlayerEntityId ? Color.DarkBlue : Color.Black;
                SetMainCell(x, gridTop + row, ' ', foreground, background);
                SetMainCell(x + 1, gridTop + row, glyph, cell?.EntityId == _screen?.PlayerEntityId ? Color.Yellow : foreground, background);
                SetMainCell(x + 2, gridTop + row, ' ', foreground, background);
            }
        }

        var afterGridY = gridBottom + 1;
        foreach (var row in afterGridRows)
        {
            if (afterGridY >= gridAreaBottom) break;
            PrintOnMain(bounds.Left + 1, afterGridY++, Math.Max(0, bounds.Width - 2), row, Color.LightGray);
        }
    }

    private void DrawBox(SadConsoleRect rect, Color color)
    {
        var right = rect.Left + rect.Width - 1;
        var bottom = rect.Bottom - 1;
        for (var x = rect.Left; x <= right; x++)
        {
            SetMainCell(x, rect.Top, x == rect.Left ? '+' : x == right ? '+' : '-', color, Color.Black);
            SetMainCell(x, bottom, x == rect.Left ? '+' : x == right ? '+' : '-', color, Color.Black);
        }

        for (var y = rect.Top + 1; y < bottom; y++)
        {
            SetMainCell(rect.Left, y, '|', color, Color.Black);
            SetMainCell(right, y, '|', color, Color.Black);
        }
    }

    private void PrintOnMain(int x, int y, int width, string text, Color color)
    {
        if (width <= 0) return;
        var clipped = text.Length <= width ? text : text[..Math.Max(0, width - 1)];
        for (var index = 0; index < clipped.Length && x + index < Width; index++)
        {
            SetMainCell(x + index, y, clipped[index], color, Color.Black);
        }
    }

    private void SetMainCell(int x, int y, int glyph, Color foreground, Color background)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        Surface[x, y].Glyph = glyph;
        Surface[x, y].Foreground = foreground;
        Surface[x, y].Background = background;
    }

    private static Color ColorForPresentation(PresentationColor color) => color switch
    {
        PresentationColor.Gray => Color.Gray,
        PresentationColor.White => Color.White,
        PresentationColor.Yellow => Color.Yellow,
        PresentationColor.Cyan => Color.Cyan,
        PresentationColor.Green => Color.Green,
        PresentationColor.DarkGreen => Color.DarkGreen,
        PresentationColor.Earth => Color.SaddleBrown,
        _ => Color.White
    };

    private static string FormatBreadcrumb(EntityPanelProjection projection) =>
        string.Join(" > ", projection.Breadcrumb.Segments.Select(segment => segment.EntityId.Value));

    private void DrawHud(GameplayMockFrame frame)
    {
        var bounds = frame.HudBounds;
        EnsureHudLayer(bounds);
        ClearConsole(_hudLayer!);
        DrawHudBox(new SadConsoleRect(0, 0, bounds.Width, bounds.Height), Color.Gold);
        PrintOnHud(2, 0, Math.Max(0, bounds.Width - 4), "Player HUD", Color.White);
        PrintOnHud(1, 1, Math.Max(0, bounds.Width - 2), frame.Title, Color.Yellow);
        for (var index = 0; index < frame.HudRows.Count && index < bounds.Height - 3; index++)
        {
            PrintOnHud(1, 2 + index, Math.Max(0, bounds.Width - 2), frame.HudRows[index], index == frame.HudRows.Count - 1 ? Color.Gray : Color.White);
        }

        _hudLayer!.Surface.IsDirty = true;
    }

    private void EnsureHudLayer(SadConsoleRect bounds)
    {
        if (_hudLayer is not null && _hudLayer.Width == bounds.Width && _hudLayer.Height == bounds.Height)
        {
            _hudLayer.Position = new Point(bounds.Left, bounds.Top);
            return;
        }

        if (_hudLayer is not null)
        {
            Children.Remove(_hudLayer);
        }

        _hudLayer = new Console(bounds.Width, bounds.Height)
        {
            Position = new Point(bounds.Left, bounds.Top)
        };
        Children.Add(_hudLayer);
    }

    private void DrawHudBox(SadConsoleRect rect, Color color)
    {
        var right = rect.Left + rect.Width - 1;
        var bottom = rect.Bottom - 1;
        for (var x = rect.Left; x <= right; x++)
        {
            SetHudCell(x, rect.Top, x == rect.Left ? '+' : x == right ? '+' : '-', color, Color.Black);
            SetHudCell(x, bottom, x == rect.Left ? '+' : x == right ? '+' : '-', color, Color.Black);
        }

        for (var y = rect.Top + 1; y < bottom; y++)
        {
            SetHudCell(rect.Left, y, '|', color, Color.Black);
            SetHudCell(right, y, '|', color, Color.Black);
        }
    }

    private void PrintOnHud(int x, int y, int width, string text, Color color)
    {
        if (width <= 0 || _hudLayer is null) return;
        var clipped = text.Length <= width ? text : text[..Math.Max(0, width - 1)];
        var hudLayer = _hudLayer;
        for (var index = 0; index < clipped.Length && x + index < hudLayer.Width; index++)
        {
            SetHudCell(x + index, y, clipped[index], color, Color.Black);
        }
    }

    private void SetHudCell(int x, int y, int glyph, Color foreground, Color background)
    {
        if (_hudLayer is null || x < 0 || y < 0 || x >= _hudLayer.Width || y >= _hudLayer.Height) return;
        _hudLayer.Surface[x, y].Glyph = glyph;
        _hudLayer.Surface[x, y].Foreground = foreground;
        _hudLayer.Surface[x, y].Background = background;
    }

    private static void ClearConsole(Console console)
    {
        for (var y = 0; y < console.Height; y++)
        {
            for (var x = 0; x < console.Width; x++)
            {
                console.Surface[x, y].Glyph = ' ';
                console.Surface[x, y].Foreground = Color.White;
                console.Surface[x, y].Background = Color.Black;
            }
        }
    }
}
