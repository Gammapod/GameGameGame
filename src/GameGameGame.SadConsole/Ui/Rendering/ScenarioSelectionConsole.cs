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
    public const int ScreenWidth = SadConsoleScreenMetrics.ScreenWidth;
    public const int ScreenHeight = SadConsoleScreenMetrics.ScreenHeight;

    private readonly ScenarioSelectionScreen _screen;
    private readonly SadConsoleTheme _theme;
    private readonly SadConsoleComponentRenderer _renderer;
    private ScenarioEditScreen? _scenarioEditScreen;
    private EntityTemplateEditScreen? _entityTemplateEditScreen;
    private InventoryGridEditScreen? _inventoryGridEditScreen;
    private ActionPlanEditScreen? _actionPlanEditScreen;
    private Console? _legacyPlayConsole;
    private string _message = "New Scenario Selection. Up/Down selects scenario. Enter opens Play/Edit. Esc exits.";

    public ScenarioSelectionConsole(SadConsoleStartup startup, SadConsoleTheme? theme = null) : base(ScreenWidth, ScreenHeight)
    {
        _theme = theme ?? SadConsoleTheme.Default;
        _renderer = new SadConsoleComponentRenderer(this, _theme);
        _screen = ScenarioSelectionScreen.FromCatalog(startup.Catalog);
        UseKeyboard = true;
        IsFocused = true;
        FocusedMode = FocusBehavior.Set;
        Redraw();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (_legacyPlayConsole is not null)
        {
            return _legacyPlayConsole.ProcessKeyboard(keyboard);
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
        _renderer.ClearOverlay();

        _legacyPlayConsole = LegacySimulationConsoleFactory.CreateForScenario(scenario);
        Children.Add(_legacyPlayConsole);
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
        _renderer.ClearSurface();
        if (_scenarioEditScreen is not null)
        {
            RedrawScenarioEdit();
            return;
        }

        _renderer.PrintClipped(1, 0, Width - 2, _screen.Title, Color.Yellow);
        _renderer.PrintClipped(1, 1, Width - 2, _screen.Purpose, Color.White);
        _renderer.PrintClipped(1, 2, Width - 2, _message, Color.Gray);

        foreach (var component in _screen.Components().Where(component => component.Id != "scenario-command-panel"))
        {
            _renderer.DrawComponent(component);
        }

        DrawOverlayLayer();

        DrawFooter();
        Surface.IsDirty = true;
    }

    private void RedrawScenarioEdit()
    {
        _renderer.ClearOverlay();

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
        _renderer.PrintClipped(1, 0, Width - 2, screen.Title, Color.Yellow);
        _renderer.PrintClipped(1, 1, Width - 2, screen.Purpose, Color.White);
        _renderer.PrintClipped(1, 2, Width - 2, _message, Color.Gray);
        foreach (var component in screen.Components())
        {
            _renderer.DrawComponent(component);
        }

        if (screen.OverlayComponent() is { } overlay)
        {
            _renderer.RenderOverlay(overlay);
        }
        else
        {
            _renderer.ClearOverlay();
        }

        var top = Height - 2;
        _renderer.PrintClipped(1, top, Width - 2, $"Theme: {_theme.Name} | {screen.FooterText()}", SadConsoleComponentRenderer.ColorFromToken(_theme.Footer.Text));
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
        _renderer.PrintClipped(1, 0, Width - 2, screen.Title, Color.Yellow);
        _renderer.PrintClipped(1, 1, Width - 2, screen.Purpose, Color.White);
        _renderer.PrintClipped(1, 2, Width - 2, _message, Color.Gray);
        foreach (var component in screen.Components())
        {
            _renderer.DrawComponent(component);
        }

        if (screen.OverlayComponent() is { } overlay)
        {
            _renderer.RenderOverlay(overlay);
        }
        else
        {
            _renderer.ClearOverlay();
        }

        var top = Height - 2;
        _renderer.PrintClipped(1, top, Width - 2, $"Theme: {_theme.Name} | {screen.FooterText()}", SadConsoleComponentRenderer.ColorFromToken(_theme.Footer.Text));
        Surface.IsDirty = true;
    }

    private void RedrawActionPlanEdit()
    {
        _renderer.ClearOverlay();

        var screen = _actionPlanEditScreen!;
        _renderer.PrintClipped(1, 0, Width - 2, screen.Title, Color.Yellow);
        _renderer.PrintClipped(1, 1, Width - 2, screen.Purpose, Color.White);
        _renderer.PrintClipped(1, 2, Width - 2, _message, Color.Gray);
        foreach (var component in screen.Components())
        {
            _renderer.DrawComponent(component);
        }

        if (screen.OverlayComponent() is { } overlay)
        {
            _renderer.RenderOverlay(overlay);
        }
        else
        {
            _renderer.ClearOverlay();
        }

        var top = Height - 2;
        _renderer.PrintClipped(1, top, Width - 2, $"Theme: {_theme.Name} | {screen.FooterText()}", SadConsoleComponentRenderer.ColorFromToken(_theme.Footer.Text));
        Surface.IsDirty = true;
    }

    private void RedrawInventoryGridEdit()
    {
        var screen = _inventoryGridEditScreen!;
        _renderer.PrintClipped(1, 0, Width - 2, screen.Title, Color.Yellow);
        _renderer.PrintClipped(1, 1, Width - 2, screen.Purpose, Color.White);
        _renderer.PrintClipped(1, 2, Width - 2, _message, Color.Gray);
        foreach (var component in screen.Components())
        {
            _renderer.DrawComponent(component);
        }

        if (screen.OverlayComponent() is { } overlay)
        {
            _renderer.RenderOverlay(overlay);
        }
        else
        {
            _renderer.ClearOverlay();
        }

        var top = Height - 2;
        _renderer.PrintClipped(1, top, Width - 2, $"Theme: {_theme.Name} | {screen.FooterText()}", SadConsoleComponentRenderer.ColorFromToken(_theme.Footer.Text));
        Surface.IsDirty = true;
    }

    private void DrawOverlayLayer()
    {
        var overlay = _screen.OverlayComponent();
        if (overlay is null)
        {
            _renderer.ClearOverlay();
            return;
        }

        _renderer.RenderOverlay(overlay);
    }

    private void DrawFooter()
    {
        var top = Height - 2;
        _renderer.PrintClipped(1, top, Width - 2, $"Theme: {_theme.Name} | {_screen.FooterText()}", SadConsoleComponentRenderer.ColorFromToken(_theme.Footer.Text));
    }

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

}
