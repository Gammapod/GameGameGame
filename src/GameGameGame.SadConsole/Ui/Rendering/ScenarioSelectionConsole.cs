using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Screens;
using GameGameGame.SadConsoleApp.Ui.Styling;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using Console = SadConsole.Console;

namespace GameGameGame.SadConsoleApp.Ui.Rendering;

internal sealed class ScenarioSelectionConsole : Console
{
    public const int ScreenWidth = 120;
    public const int ScreenHeight = 42;

    private readonly ScenarioSelectionScreen _screen;
    private readonly SadConsoleTheme _theme;
    private Console? _overlayLayer;
    private ScenarioEditScreen? _scenarioEditScreen;
    private EntityTemplateEditScreen? _entityTemplateEditScreen;
    private InventoryGridEditScreen? _inventoryGridEditScreen;
    private ActionPlanEditScreen? _actionPlanEditScreen;
    private SadConsoleShell? _legacyPlayShell;
    private bool _overlayAttached;
    private string _message = "New Scenario Selection. Up/Down selects scenario. Enter opens Play/Edit. Esc exits.";

    public ScenarioSelectionConsole(SadConsoleStartup startup, SadConsoleTheme? theme = null) : base(ScreenWidth, ScreenHeight)
    {
        _theme = theme ?? SadConsoleTheme.Default;
        _screen = ScenarioSelectionScreen.FromCatalog(startup.Catalog);
        UseKeyboard = true;
        IsFocused = true;
        FocusedMode = FocusBehavior.Set;
        Redraw();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (_legacyPlayShell is not null)
        {
            return _legacyPlayShell.ProcessKeyboard(keyboard);
        }

        if (_entityTemplateEditScreen?.IsTextEntryOverlayActive == true)
        {
            if (keyboard.IsKeyReleased(Keys.Back))
            {
                _message = _entityTemplateEditScreen.Backspace().Message;
                Redraw();
                return true;
            }

            var typed = ReadTypedCharacters(keyboard);
            if (!string.IsNullOrEmpty(typed))
            {
                _message = _entityTemplateEditScreen.InsertText(typed).Message;
                Redraw();
                return true;
            }
        }

        if (_scenarioEditScreen?.IsTextEntryOverlayActive == true)
        {
            if (keyboard.IsKeyReleased(Keys.Back))
            {
                _message = _scenarioEditScreen.Backspace().Message;
                Redraw();
                return true;
            }

            var typed = ReadTypedCharacters(keyboard);
            if (!string.IsNullOrEmpty(typed))
            {
                _message = _scenarioEditScreen.InsertText(typed).Message;
                Redraw();
                return true;
            }
        }

        if (_actionPlanEditScreen?.IsTextEntryOverlayActive == true)
        {
            if (keyboard.IsKeyReleased(Keys.Back))
            {
                _message = _actionPlanEditScreen.Backspace().Message;
                Redraw();
                return true;
            }

            var typed = ReadTypedCharacters(keyboard);
            if (!string.IsNullOrEmpty(typed))
            {
                _message = _actionPlanEditScreen.InsertText(typed).Message;
                Redraw();
                return true;
            }
        }

        if (_scenarioEditScreen is not null && keyboard.IsKeyReleased(Keys.S))
        {
            var result = _scenarioEditScreen.Save();
            _message = result.Message;
            Redraw();
            return true;
        }

        if (_inventoryGridEditScreen is not null)
        {
            if (keyboard.IsKeyReleased(Keys.Delete) || keyboard.IsKeyReleased(Keys.Back)) HandleInventoryGridEdit(InventoryGridEditCommand.Delete);
            else if (keyboard.IsKeyReleased(Keys.Space)) HandleInventoryGridEdit(InventoryGridEditCommand.Move);
            else if (keyboard.IsKeyReleased(Keys.C)) HandleInventoryGridEdit(InventoryGridEditCommand.Copy);
            else if (keyboard.IsKeyReleased(Keys.Tab)) HandleInventoryGridEdit(InventoryGridEditCommand.OpenBrushPicker);
            else if (keyboard.IsKeyReleased(Keys.Up)) HandleInventoryGridEdit(UiComponentCommand.Up);
            else if (keyboard.IsKeyReleased(Keys.Down)) HandleInventoryGridEdit(UiComponentCommand.Down);
            else if (keyboard.IsKeyReleased(Keys.Left)) HandleInventoryGridEdit(UiComponentCommand.Left);
            else if (keyboard.IsKeyReleased(Keys.Right)) HandleInventoryGridEdit(UiComponentCommand.Right);
            else if (keyboard.IsKeyReleased(Keys.Enter)) HandleInventoryGridEdit(UiComponentCommand.Select);
            else if (keyboard.IsKeyReleased(Keys.Escape)) HandleInventoryGridEdit(UiComponentCommand.Cancel);
            else return false;

            Redraw();
            return true;
        }

        if (_actionPlanEditScreen is not null)
        {
            if (keyboard.IsKeyReleased(Keys.Delete) || keyboard.IsKeyReleased(Keys.Back)) HandleActionPlanEdit(ActionPlanEditCommand.Delete);
            else if (keyboard.IsKeyReleased(Keys.I)) HandleActionPlanEdit(ActionPlanEditCommand.Insert);
            else if (keyboard.IsKeyReleased(Keys.Space)) HandleActionPlanEdit(ActionPlanEditCommand.ToggleMoveMode);
            else if (keyboard.IsKeyReleased(Keys.Up)) Handle(UiComponentCommand.Up);
            else if (keyboard.IsKeyReleased(Keys.Down)) Handle(UiComponentCommand.Down);
            else if (keyboard.IsKeyReleased(Keys.Left)) Handle(UiComponentCommand.Left);
            else if (keyboard.IsKeyReleased(Keys.Right)) Handle(UiComponentCommand.Right);
            else if (keyboard.IsKeyReleased(Keys.Enter)) Handle(UiComponentCommand.Select);
            else if (keyboard.IsKeyReleased(Keys.Escape)) Handle(UiComponentCommand.Cancel);
            else return false;

            Redraw();
            return true;
        }

        if (keyboard.IsKeyReleased(Keys.Up)) Handle(UiComponentCommand.Up);
        else if (keyboard.IsKeyReleased(Keys.Down)) Handle(UiComponentCommand.Down);
        else if (keyboard.IsKeyReleased(Keys.Left)) Handle(UiComponentCommand.Left);
        else if (keyboard.IsKeyReleased(Keys.Right)) Handle(UiComponentCommand.Right);
        else if (keyboard.IsKeyReleased(Keys.Enter)) Handle(UiComponentCommand.Select);
        else if (keyboard.IsKeyReleased(Keys.Escape)) Handle(UiComponentCommand.Cancel);
        else return false;

        Redraw();
        return true;
    }

    private void Handle(UiComponentCommand command)
    {
        if (_scenarioEditScreen is not null)
        {
            HandleScenarioEdit(command);
            return;
        }

        var result = _screen.Handle(command);
        _message = result.Message;

        if (result.Kind is ScenarioSelectionResultKind.Exit)
        {
            SadConsole.Game.Instance.MonoGameInstance.Exit();
            return;
        }

        // Play/Edit are intentionally visible routing results for this phase. The next
        // screens will consume these results once rebuilt on the component API.
        if (result.Kind is ScenarioSelectionResultKind.Play or ScenarioSelectionResultKind.Edit)
        {
            _message = result.Message;
            if (result.Kind == ScenarioSelectionResultKind.Play && result.Scenario is { } playScenario)
            {
                LaunchLegacyPlay(playScenario);
            }
            else if (result.Kind == ScenarioSelectionResultKind.Edit && result.Scenario is { } scenario)
            {
                _scenarioEditScreen = ScenarioEditScreen.Open(scenario).Screen;
                _message = $"Opened Scenario Edit for {scenario.Name}.";
            }
        }
    }

    private void LaunchLegacyPlay(GameGameGame.Content.ScenarioCatalogEntry scenario)
    {
        if (_overlayAttached)
        {
            Children.Remove(_overlayLayer!);
            _overlayAttached = false;
        }

        _legacyPlayShell = new SadConsoleShell(new SadConsoleStartup(
            DirectSession: null,
            Catalog: null,
            Error: null,
            DirectContentPath: scenario.ContentPath,
            DirectScenarioId: scenario.ScenarioId,
            LaunchLegacyBetaEditor: true,
            LaunchDirectSimulation: true));
        Children.Add(_legacyPlayShell);
        _legacyPlayShell.IsFocused = true;
        _legacyPlayShell.FocusedMode = FocusBehavior.Set;
        _message = $"Launched legacy Play mode for {scenario.Name}.";
    }

    private void HandleScenarioEdit(UiComponentCommand command)
    {
        if (_actionPlanEditScreen is not null)
        {
            HandleActionPlanEdit(command);
            return;
        }

        if (_inventoryGridEditScreen is not null)
        {
            HandleInventoryGridEdit(command);
            return;
        }

        if (_entityTemplateEditScreen is not null)
        {
            HandleEntityTemplateEdit(command);
            return;
        }

        if (_scenarioEditScreen is null)
        {
            return;
        }

        var result = _scenarioEditScreen.Handle(command);
        _message = result.Message;
        if (result.Kind == ScenarioEditResultKind.ReturnToScenarioSelection)
        {
            _scenarioEditScreen = null;
            _message = "Returned to Scenario Selection.";
        }
        else if (result.Kind == ScenarioEditResultKind.OpenEntityTemplate && result.EntityTemplateId is { } templateId)
        {
            _entityTemplateEditScreen = _scenarioEditScreen.OpenEntityTemplateEditScreen(templateId);
            _message = _entityTemplateEditScreen is null
                ? $"Could not open Entity Template screen for {templateId}."
                : $"Opened Entity Template screen for {templateId}.";
        }
        else if (result.Kind == ScenarioEditResultKind.OpenActionPlan && result.ActionPlanId is { } actionPlanId)
        {
            _actionPlanEditScreen = _scenarioEditScreen.OpenActionPlanEditScreen(actionPlanId, ActionPlanEditReturnDestination.ScenarioEdit);
            _message = _actionPlanEditScreen is null
                ? $"Could not open Action Plan screen for {actionPlanId}."
                : $"Opened Action Plan screen for {actionPlanId}.";
        }
    }

    private void HandleEntityTemplateEdit(UiComponentCommand command)
    {
        if (_entityTemplateEditScreen is null)
        {
            return;
        }

        var result = _entityTemplateEditScreen.Handle(command);
        _message = result.Message;
        if (result.Kind == EntityTemplateEditResultKind.ReturnToScenarioEdit)
        {
            _entityTemplateEditScreen = null;
            _message = "Returned to Scenario Edit.";
        }
        else if (result.Kind == EntityTemplateEditResultKind.OpenActionPlan && result.ActionPlanId is { } actionPlanId)
        {
            _actionPlanEditScreen = _scenarioEditScreen?.OpenActionPlanEditScreen(actionPlanId, ActionPlanEditReturnDestination.EntityTemplateEdit);
            _message = _actionPlanEditScreen is null
                ? $"Could not open Action Plan screen for {actionPlanId}."
                : $"Opened Action Plan screen for {actionPlanId}.";
        }
        else if (result.Kind == EntityTemplateEditResultKind.OpenInventoryGrid)
        {
            _inventoryGridEditScreen = _entityTemplateEditScreen.OpenInventoryGridEditScreen();
            _message = "Opened Inventory Grid editor.";
        }
    }

    private void HandleInventoryGridEdit(UiComponentCommand command)
    {
        if (_inventoryGridEditScreen is null) return;

        var result = _inventoryGridEditScreen.Handle(command);
        _message = result.Message;
        if (result.Kind == InventoryGridEditResultKind.ReturnToEntityTemplateEdit)
        {
            _inventoryGridEditScreen = null;
            _message = result.Message;
        }
    }

    private void HandleInventoryGridEdit(InventoryGridEditCommand command)
    {
        if (_inventoryGridEditScreen is null) return;

        var result = _inventoryGridEditScreen.Handle(command);
        _message = result.Message;
    }

    private void HandleActionPlanEdit(UiComponentCommand command)
    {
        if (_actionPlanEditScreen is null)
        {
            return;
        }

        var result = _actionPlanEditScreen.Handle(command);
        _message = result.Message;
        if (result.Kind == ActionPlanEditResultKind.Return)
        {
            _actionPlanEditScreen = null;
            _message = result.Message;
        }
    }

    private void HandleActionPlanEdit(ActionPlanEditCommand command)
    {
        if (_actionPlanEditScreen is null)
        {
            return;
        }

        var result = _actionPlanEditScreen.Handle(command);
        _message = result.Message;
    }

    private void Redraw()
    {
        ClearSurface();
        if (_scenarioEditScreen is not null)
        {
            RedrawScenarioEdit();
            return;
        }

        PrintClipped(1, 0, Width - 2, _screen.Title, Color.Yellow);
        PrintClipped(1, 1, Width - 2, _screen.Purpose, Color.White);
        PrintClipped(1, 2, Width - 2, _message, Color.Gray);

        foreach (var component in _screen.Components().Where(component => component.Id != "scenario-command-panel"))
        {
            DrawComponent(component);
        }

        DrawOverlayLayer();

        DrawFooter();
        Surface.IsDirty = true;
    }

    private void RedrawScenarioEdit()
    {
        if (_overlayAttached)
        {
            Children.Remove(_overlayLayer!);
            _overlayAttached = false;
        }

        if (_actionPlanEditScreen is not null)
        {
            RedrawActionPlanEdit();
            return;
        }

        if (_inventoryGridEditScreen is not null)
        {
            RedrawInventoryGridEdit();
            return;
        }

        if (_entityTemplateEditScreen is not null)
        {
            RedrawEntityTemplateEdit();
            return;
        }

        var screen = _scenarioEditScreen!;
        PrintClipped(1, 0, Width - 2, screen.Title, Color.Yellow);
        PrintClipped(1, 1, Width - 2, screen.Purpose, Color.White);
        PrintClipped(1, 2, Width - 2, _message, Color.Gray);
        foreach (var component in screen.Components())
        {
            DrawComponent(component);
        }

        if (screen.OverlayComponent() is { } overlay)
        {
            RenderOverlay(overlay);
        }
        else if (_overlayAttached)
        {
            Children.Remove(_overlayLayer!);
            _overlayAttached = false;
        }

        var top = Height - 2;
        PrintClipped(1, top, Width - 2, $"Theme: {_theme.Name} | {screen.FooterText()}", ColorFromToken(_theme.Footer.Text));
        Surface.IsDirty = true;
    }

    private void RedrawEntityTemplateEdit()
    {
        if (_actionPlanEditScreen is not null)
        {
            RedrawActionPlanEdit();
            return;
        }

        var screen = _entityTemplateEditScreen!;
        PrintClipped(1, 0, Width - 2, screen.Title, Color.Yellow);
        PrintClipped(1, 1, Width - 2, screen.Purpose, Color.White);
        PrintClipped(1, 2, Width - 2, _message, Color.Gray);
        foreach (var component in screen.Components())
        {
            DrawComponent(component);
        }

        if (screen.OverlayComponent() is { } overlay)
        {
            RenderOverlay(overlay);
        }
        else if (_overlayAttached)
        {
            Children.Remove(_overlayLayer!);
            _overlayAttached = false;
        }

        var top = Height - 2;
        PrintClipped(1, top, Width - 2, $"Theme: {_theme.Name} | {screen.FooterText()}", ColorFromToken(_theme.Footer.Text));
        Surface.IsDirty = true;
    }

    private void RedrawActionPlanEdit()
    {
        if (_overlayAttached)
        {
            Children.Remove(_overlayLayer!);
            _overlayAttached = false;
        }

        var screen = _actionPlanEditScreen!;
        PrintClipped(1, 0, Width - 2, screen.Title, Color.Yellow);
        PrintClipped(1, 1, Width - 2, screen.Purpose, Color.White);
        PrintClipped(1, 2, Width - 2, _message, Color.Gray);
        foreach (var component in screen.Components())
        {
            DrawComponent(component);
        }

        if (screen.OverlayComponent() is { } overlay)
        {
            RenderOverlay(overlay);
        }
        else if (_overlayAttached)
        {
            Children.Remove(_overlayLayer!);
            _overlayAttached = false;
        }

        var top = Height - 2;
        PrintClipped(1, top, Width - 2, $"Theme: {_theme.Name} | {screen.FooterText()}", ColorFromToken(_theme.Footer.Text));
        Surface.IsDirty = true;
    }

    private void RedrawInventoryGridEdit()
    {
        var screen = _inventoryGridEditScreen!;
        PrintClipped(1, 0, Width - 2, screen.Title, Color.Yellow);
        PrintClipped(1, 1, Width - 2, screen.Purpose, Color.White);
        PrintClipped(1, 2, Width - 2, _message, Color.Gray);
        foreach (var component in screen.Components())
        {
            DrawComponent(component);
        }

        if (screen.OverlayComponent() is { } overlay)
        {
            RenderOverlay(overlay);
        }
        else if (_overlayAttached)
        {
            Children.Remove(_overlayLayer!);
            _overlayAttached = false;
        }

        var top = Height - 2;
        PrintClipped(1, top, Width - 2, $"Theme: {_theme.Name} | {screen.FooterText()}", ColorFromToken(_theme.Footer.Text));
        Surface.IsDirty = true;
    }

    private void DrawOverlayLayer()
    {
        var overlay = _screen.OverlayComponent();
        if (overlay is null)
        {
            if (_overlayAttached)
            {
                Children.Remove(_overlayLayer!);
                _overlayAttached = false;
            }

            return;
        }

        RenderOverlay(overlay);
    }

    private void RenderOverlay(IUiComponent overlay)
    {
        var bounds = overlay.Bounds;
        if (_overlayLayer is null || _overlayLayer.Width != bounds.Width || _overlayLayer.Height != bounds.Height)
        {
            if (_overlayAttached)
            {
                Children.Remove(_overlayLayer!);
                _overlayAttached = false;
            }

            _overlayLayer = new Console(bounds.Width, bounds.Height);
        }

        _overlayLayer.Position = new Point(bounds.Left, bounds.Top);
        if (!_overlayAttached)
        {
            Children.Add(_overlayLayer);
            _overlayAttached = true;
        }

        ClearSurface(_overlayLayer);
        DrawComponent(_overlayLayer, overlay, localBounds: true);
        _overlayLayer.Surface.IsDirty = true;
    }

    private void DrawFooter()
    {
        var top = Height - 2;
        PrintClipped(1, top, Width - 2, $"Theme: {_theme.Name} | {_screen.FooterText()}", ColorFromToken(_theme.Footer.Text));
    }

    private void DrawComponent(IUiComponent component)
    {
        DrawComponent(this, component, localBounds: false);
    }

    private void DrawComponent(Console target, IUiComponent component, bool localBounds)
    {
        if (component is InventoryGridComponent inventoryGrid)
        {
            DrawInventoryGridComponent(target, inventoryGrid, localBounds);
            return;
        }

        if (component is InventorySummaryComponent inventorySummary)
        {
            DrawInventorySummaryComponent(target, inventorySummary, localBounds);
            return;
        }

        var border = ColorFromToken(component.State.BorderColor(_theme));
        var bounds = localBounds ? new SadConsoleRect(0, 0, target.Width, target.Height) : component.Bounds;
        DrawBox(target, bounds, border, _theme.Panel.BorderGlyphs);
        PrintClipped(target, bounds.Left + 2, bounds.Top, Math.Max(0, bounds.Width - 4), component.Title, ColorFromToken(_theme.Panel.TitleText));

        var rows = component.RenderRows(_theme).Skip(1).ToList();
        var maxRows = Math.Max(0, bounds.Height - 2);
        for (var index = 0; index < rows.Count && index < maxRows; index++)
        {
            var row = rows[index];
            var visibleRow = ComponentGalleryConsole.StripStyleTokens(row);
            PrintClipped(target, bounds.Left + 1, bounds.Top + 1 + index, Math.Max(0, bounds.Width - 2), visibleRow, ColorForRow(row, component.State));
            ComponentGalleryConsole.DrawColorSampleGlyph(target, bounds.Left + 1, bounds.Top + 1 + index, Math.Max(0, bounds.Width - 2), visibleRow, row);
        }
    }

    private void DrawInventorySummaryComponent(Console target, InventorySummaryComponent component, bool localBounds)
    {
        var border = ColorFromToken(component.State.BorderColor(_theme));
        var bounds = localBounds ? new SadConsoleRect(0, 0, target.Width, target.Height) : component.Bounds;
        DrawBox(target, bounds, border, _theme.Panel.BorderGlyphs);
        PrintClipped(target, bounds.Left + 2, bounds.Top, Math.Max(0, bounds.Width - 4), component.Title, ColorFromToken(_theme.Panel.TitleText));

        var leftWidth = Math.Min(42, Math.Max(0, bounds.Width - 4));
        for (var index = 0; index < component.Rows.Count && index < Math.Max(0, bounds.Height - 2); index++)
        {
            var row = component.Rows[index];
            PrintClipped(target, bounds.Left + 1, bounds.Top + 1 + index, leftWidth, row, ColorForRow(row, component.State));
        }

        var previewLeft = bounds.Left + leftWidth + 3;
        var previewTop = bounds.Top + 1;
        var previewRight = bounds.Left + bounds.Width - 2;
        var previewBottom = bounds.Bottom - 2;
        if (previewLeft > previewRight || previewTop > previewBottom)
        {
            return;
        }

        PrintClipped(target, previewLeft, previewTop, previewRight - previewLeft + 1, "read-only inventory preview", Color.DarkGray);
        if (component.GridWidth <= 0 || component.GridHeight <= 0)
        {
            PrintClipped(target, previewLeft, previewTop + 1, previewRight - previewLeft + 1, "no usable grid", Color.DarkGray);
            return;
        }

        var gridLeft = previewLeft + 3;
        var gridTop = previewTop + 2;
        var cellWidth = 3;
        var cells = component.Cells.ToDictionary(cell => cell.Coord);
        for (var y = 0; y < component.GridHeight && gridTop + y <= previewBottom; y++)
        {
            PrintClipped(target, previewLeft, gridTop + y, 3, $"{y,2}:", Color.DarkGray);
            for (var x = 0; x < component.GridWidth; x++)
            {
                var xPos = gridLeft + x * cellWidth;
                if (xPos + 2 > previewRight) break;

                var coord = new GameGameGame.Core.GridCoord(x, y);
                var cell = cells.GetValueOrDefault(coord);
                var glyph = cell?.Glyph ?? '.';
                var foreground = cell?.Color is { } color ? ColorFromToken(color.ToString()) : Color.DarkGray;
                SetCell(target, xPos, gridTop + y, ' ', foreground, Color.Black);
                SetCell(target, xPos + 1, gridTop + y, glyph, foreground, Color.Black);
                SetCell(target, xPos + 2, gridTop + y, ' ', foreground, Color.Black);
            }
        }
    }

    private void DrawInventoryGridComponent(Console target, InventoryGridComponent component, bool localBounds)
    {
        var border = ColorFromToken(component.State.BorderColor(_theme));
        var bounds = localBounds ? new SadConsoleRect(0, 0, target.Width, target.Height) : component.Bounds;
        DrawBox(target, bounds, border, _theme.Panel.BorderGlyphs);
        PrintClipped(target, bounds.Left + 2, bounds.Top, Math.Max(0, bounds.Width - 4), component.Title, ColorFromToken(_theme.Panel.TitleText));

        var rows = component.RenderRows(_theme).Skip(1).Where(row => !row.Contains("grid cells are rendered", StringComparison.Ordinal)).ToList();
        for (var index = 0; index < rows.Count && index < Math.Max(0, bounds.Height - 2); index++)
        {
            PrintClipped(target, bounds.Left + 1, bounds.Top + 1 + index, Math.Max(0, bounds.Width - 2), rows[index], Color.White);
        }

        if (component.GridWidth <= 0 || component.GridHeight <= 0)
        {
            return;
        }

        var gridLeft = bounds.Left + 4;
        var gridTop = bounds.Top + 4;
        var cellWidth = 3;
        var cells = component.Cells.ToDictionary(cell => cell.Coord);
        for (var y = 0; y < component.GridHeight && gridTop + y < bounds.Bottom - 1; y++)
        {
            PrintClipped(target, bounds.Left + 1, gridTop + y, 3, $"{y,2}:", Color.DarkGray);
            for (var x = 0; x < component.GridWidth; x++)
            {
                var coord = new GameGameGame.Core.GridCoord(x, y);
                var xPos = gridLeft + x * cellWidth;
                if (xPos >= bounds.Left + bounds.Width - 1) break;

                var cell = cells.GetValueOrDefault(coord);
                var glyph = cell?.Glyph ?? '.';
                var foreground = cell?.Color is { } color ? ColorFromToken(color.ToString()) : Color.DarkGray;
                var isCursor = coord == component.Cursor;
                SetCell(target, xPos, gridTop + y, ' ', foreground, isCursor ? Color.DarkBlue : Color.Black);
                SetCell(target, xPos + 1, gridTop + y, glyph, isCursor ? Color.Yellow : foreground, isCursor ? Color.DarkBlue : Color.Black);
                SetCell(target, xPos + 2, gridTop + y, ' ', foreground, isCursor ? Color.DarkBlue : Color.Black);
            }
        }
    }

    private void DrawBox(Console target, SadConsoleRect rect, Color color, BorderGlyphTheme glyphs)
    {
        var right = rect.Left + rect.Width - 1;
        var bottom = rect.Bottom - 1;
        for (var x = rect.Left; x <= right; x++)
        {
            SetCell(target, x, rect.Top, x == rect.Left ? glyphs.TopLeft : x == right ? glyphs.TopRight : glyphs.Horizontal, color, Color.Black);
            SetCell(target, x, bottom, x == rect.Left ? glyphs.BottomLeft : x == right ? glyphs.BottomRight : glyphs.Horizontal, color, Color.Black);
        }

        for (var y = rect.Top + 1; y < bottom; y++)
        {
            SetCell(target, rect.Left, y, glyphs.Vertical, color, Color.Black);
            SetCell(target, right, y, glyphs.Vertical, color, Color.Black);
        }
    }

    private Color ColorForRow(string row, UiComponentState componentState)
    {
        if (row.Contains(_theme.List.FocusedRowText, StringComparison.Ordinal)) return ColorFromToken(_theme.List.FocusedRowText);
        foreach (var token in SadConsoleKnownColorTokens.Values)
        {
            if (row.Contains($"({token})", StringComparison.OrdinalIgnoreCase)) return ColorFromToken(token);
        }
        if (row.Contains(_theme.List.SelectedRowText, StringComparison.Ordinal)) return ColorFromToken(_theme.List.SelectedRowText);
        if (row.Contains(_theme.List.EmptyText, StringComparison.Ordinal)) return ColorFromToken(_theme.List.EmptyText);
        return componentState == UiComponentState.Focused ? Color.White : Color.LightGray;
    }

    private static Color ColorFromToken(string token) => ComponentGalleryConsole.ColorFromToken(token);

    private static string ReadTypedCharacters(Keyboard keyboard)
    {
        var chars = new List<char>();
        foreach (var key in keyboard.KeysPressed)
        {
            if (key.Character != 0 && !char.IsControl(key.Character))
            {
                chars.Add(key.Character);
            }
        }

        return new string(chars.ToArray());
    }

    private void PrintClipped(int x, int y, int width, string text, Color color)
    {
        PrintClipped(this, x, y, width, text, color);
    }

    private static void PrintClipped(Console target, int x, int y, int width, string text, Color color)
    {
        if (y < 0 || y >= target.Height || x >= target.Width || width <= 0) return;
        var clipped = text.Length <= width ? text : text[..Math.Max(0, width - 1)];
        PrintText(target, x, y, clipped.PadRight(Math.Max(0, width)), color);
    }

    private static void PrintText(Console target, int x, int y, string text, Color foreground)
    {
        for (var index = 0; index < text.Length && x + index < target.Width; index++)
        {
            SetCell(target, x + index, y, text[index], foreground, Color.Black);
        }
    }

    private void ClearSurface()
    {
        for (var y = 0; y < ScreenHeight; y++)
        {
            for (var x = 0; x < ScreenWidth; x++)
            {
                SetCell(x, y, ' ', Color.White, Color.Black);
            }
        }
    }

    private static void ClearSurface(Console target)
    {
        for (var y = 0; y < target.Height; y++)
        {
            for (var x = 0; x < target.Width; x++)
            {
                SetCell(target, x, y, ' ', Color.White, Color.Black);
            }
        }
    }

    private void SetCell(int x, int y, int glyph, Color foreground, Color background)
    {
        SetCell(this, x, y, glyph, foreground, background);
    }

    private static void SetCell(Console target, int x, int y, int glyph, Color foreground, Color background)
    {
        if (x < 0 || y < 0 || x >= target.Width || y >= target.Height)
        {
            return;
        }

        target.Surface[x, y].Glyph = glyph;
        target.Surface[x, y].Foreground = foreground;
        target.Surface[x, y].Background = background;
    }
}
