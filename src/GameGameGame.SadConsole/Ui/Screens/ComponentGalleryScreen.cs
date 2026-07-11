using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Navigation;
using GameGameGame.SadConsoleApp.Ui.Styling;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed class ComponentGalleryScreen
{
    private readonly SadConsoleTheme _theme;
    private readonly FocusRouter _focusRouter;

    private ComponentGalleryScreen(SadConsoleTheme theme)
    {
        _theme = theme;
        _focusRouter = new FocusRouter([
            new FocusTarget("panel-states"),
            new FocusTarget("lists"),
            new FocusTarget("fields"),
            new FocusTarget("footer")
        ]);
    }

    public string Title => "SadConsole Component Gallery";
    public string Purpose => "Phase 1 review artifact: verify shared component states before rebuilding real screens.";
    public string? SelectedComponentId => _focusRouter.SelectedComponentId;
    public string? FocusedComponentId => _focusRouter.FocusedComponentId;

    public static ComponentGalleryScreen CreateDefault(SadConsoleTheme? theme = null) => new(theme ?? SadConsoleTheme.Default);

    public FocusRouterResult Handle(UiComponentCommand command) => _focusRouter.Handle(command);

    public IReadOnlyList<IUiComponent> Components()
    {
        return [
            PanelStates(),
            ListStates(),
            FieldStates(),
            FooterPanel()
        ];
    }

    public IReadOnlyList<string> RenderReviewRows()
    {
        var rows = new List<string>
        {
            Title,
            Purpose,
            $"Selected: {SelectedComponentId ?? "none"}; Focused: {FocusedComponentId ?? "none"}",
            string.Empty
        };

        foreach (var component in Components())
        {
            rows.AddRange(component.RenderRows(_theme));
            rows.Add(string.Empty);
        }

        return rows;
    }

    private PanelComponent PanelStates()
    {
        return new PanelComponent(
            "panel-states",
            "Panels / border states",
            new SadConsoleRect(1, 3, 38, 16),
            [
                $"theme: {_theme.Name}",
                $"border glyphs: {BorderGlyphPreview(_theme.Panel.BorderGlyphs)}",
                $"unselected border: {_theme.Panel.BorderUnselected}",
                $"selected border: {_theme.Panel.BorderSelected}",
                $"focused border: {_theme.Panel.BorderFocused}",
                $"disabled border: {_theme.Panel.BorderDisabled}",
                $"error border: {_theme.Panel.BorderError}"
            ],
            _focusRouter.StateFor("panel-states"),
            "Every visible component should have one of these border states.");
    }

    private SelectableListComponent ListStates()
    {
        var list = new SelectableListComponent(
            "lists",
            "Selectable lists",
            new SadConsoleRect(41, 3, 38, 16),
            [
                new SelectableListItem("scenario", "Scenario row", "opens Play/Edit; Esc cancels/back"),
                new SelectableListItem("entity", "Entity template row", "opens entity editor"),
                new SelectableListItem("action-plan", "Action plan row", "opens action-plan editor"),
                new SelectableListItem("disabled", "Disabled row", "not selectable later", IsEnabled: false),
                new SelectableListItem("overflow", "Overflow row", "forces scroll indicator")
            ],
            _focusRouter.StateFor("lists"),
            visibleRowCount: 4);
        list.MoveSelection(1);
        return list;
    }

    private FieldGroupComponent FieldStates()
    {
        return new FieldGroupComponent(
            "fields",
            "Editable fields",
            new SadConsoleRect(1, 18, 58, 34),
            [
                new EditableFieldComponent("readonly", "scenario root", "root-template", EditableFieldMode.ReadOnly),
                new EditableFieldComponent("editable", "name", "Player", EditableFieldMode.Editable),
                new EditableFieldComponent("editing", "glyph", "@", EditableFieldMode.Editing),
                new EditableFieldComponent("dirty", "color", "Yellow", EditableFieldMode.Editable, IsDirty: true),
                new EditableFieldComponent("invalid", "target range", "999", EditableFieldMode.Editable, ValidationMessage: "range must be 0-10")
            ],
            _focusRouter.StateFor("fields"));
    }

    private PanelComponent FooterPanel()
    {
        var state = _focusRouter.StateFor("footer");
        var controls = FocusedComponentId is null
            ? "No component focused: arrows select a component; Select focuses; Cancel leaves gallery."
            : "Component focused: arrows route to component; Select activates; Cancel releases focus.";
        return new PanelComponent(
            "footer",
            "Context footer",
            new SadConsoleRect(61, 18, 56, 34),
            [
                $"background token: {_theme.Footer.Background}",
                $"text token: {_theme.Footer.Text}",
                $"key token: {_theme.Footer.KeyText}",
                controls
            ],
            state,
            "Footer wording should always describe current focus controls.");
    }

    private static string BorderGlyphPreview(BorderGlyphTheme glyphs) =>
        $"{glyphs.TopLeft}{glyphs.Horizontal}{glyphs.TopRight} {glyphs.Vertical} {glyphs.BottomLeft}{glyphs.Horizontal}{glyphs.BottomRight}";
}
