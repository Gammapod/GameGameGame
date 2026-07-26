using GameGameGame.Content;
using GameGameGame.Core;
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
            new FocusTarget("text-entry-overlay"),
            new FocusTarget("int-setter-overlay"),
            new FocusTarget("choice-picker-overlay"),
            new FocusTarget("confirm-overlay"),
            new FocusTarget("candii-tileset"),
            new FocusTarget("play-mode-components"),
            new FocusTarget("inventory-space"),
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
            TextEntryOverlayExample(),
            IntSetterOverlayExample(),
            ChoicePickerOverlayExample(),
            ConfirmOverlayExample(),
            CandiiTilesetExample(),
            PlayModeComponentMapExample(),
            InventorySpaceExample(),
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
            new SadConsoleRect(1, 3, 36, 14),
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
            new SadConsoleRect(40, 3, 37, 14),
            [
                new SelectableListItem("scenario", "Scenario row", "opens Play/Debug/Edit; Esc cancels/back"),
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
            new SadConsoleRect(1, 16, 37, 31),
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
            new SadConsoleRect(79, 3, 38, 12),
            [
                $"background token: {_theme.Footer.Background}",
                $"text token: {_theme.Footer.Text}",
                $"key token: {_theme.Footer.KeyText}",
                controls
            ],
            state,
            "Footer wording should always describe current focus controls.");
    }

    private PanelComponent PlayModeComponentMapExample()
    {
        return new PanelComponent(
            "play-mode-components",
            "Play mode component map",
            new SadConsoleRect(1, 32, 76, 42),
            [
                "0 Play-mode screen",
                "0.1 HUD: status/context, not action choices",
                "0.2 Current place: spatial target/destination selection",
                "0.2.1 Action selector: Enter selects, Esc closes/back",
                "0.3 Inspection panel: inspected entity/player inventory selection",
                "Select/Cancel follows a stack: submenu -> prior selection -> action selector -> play."
            ],
            _focusRouter.StateFor("play-mode-components"),
            "Player action prompts consume Core choice facts; highlights are hints.");
    }

    private InventorySpaceComponent InventorySpaceExample()
    {
        var view = new InventorySpaceViewModel(
            "gallery.inventory-space.view",
            "Gallery inventory space",
            new PlaneId("galleryPlane"),
            Width: 5,
            Height: 4,
            InventorySpaceCellMetrics.Default,
            InventorySpaceViewport.Full(5, 4),
            new InventorySpaceBackdropLayer(new InventorySpaceVisualLayer(160, PresentationColor.Gray, ForegroundRgb: 0x808080, BackgroundRgb: 0x404040)),
            [
                new InventorySpaceEntityVisual(new GridCoord(1, 1), new EntityId("gallery-player"), new InventorySpaceVisualLayer('@', PresentationColor.Yellow), Accent: null, InventorySpaceVisualPlacement.Default),
                new InventorySpaceEntityVisual(new GridCoord(3, 2), new EntityId("gallery-box"), new InventorySpaceVisualLayer('B', PresentationColor.Earth), Accent: null, InventorySpaceVisualPlacement.Default)
            ],
            [
                new InventorySpaceDecorator(new GridCoord(1, 1), InventorySpaceDecoratorRole.Controlled, new EntityId("gallery-player"), new InventorySpaceVisualLayer('*', PresentationColor.Cyan), Priority: 100),
                new InventorySpaceDecorator(new GridCoord(3, 2), InventorySpaceDecoratorRole.Warning, new EntityId("gallery-box"), new InventorySpaceVisualLayer('!', PresentationColor.Yellow), Priority: 50)
            ],
            new InventorySpaceFrame(Visible: true, Title: "Gallery inventory space", Color: PresentationColor.Yellow));

        return new InventorySpaceComponent(
            "inventory-space",
            "Inventory-space component",
            SadConsoleRect.FromSize(40, 32, 37, 12),
            view,
            [
                "backdrop glyph 160 fg 808080 bg 404040",
                "entity glyphs preserve identity",
                "decorators are separate overlay facts",
                "profile: framed debug"
            ],
            _focusRouter.StateFor("inventory-space"),
            InventorySpaceRenderOptions.FramedDebug);
    }

    private PanelComponent CandiiTilesetExample()
    {
        return new PanelComponent(
            "candii-tileset",
            "Candii 8x8 tileset preview",
            new SadConsoleRect(79, 24, 38, 39),
            [
                "Uses Candii.tileset.json role mappings.",
                "Inner preview is a child Console with square 8x8 cells.",
                "Blank, text spaces, and borders come from the profile."
            ],
            _focusRouter.StateFor("candii-tileset"),
            "First square-tile baseline smoke test.");
    }

    private TextEntryOverlayComponent TextEntryOverlayExample()
    {
        var component = new TextEntryOverlayComponent(
            "text-entry-overlay",
            "Text entry overlay",
            "entity name",
            "Fleeing Mouse",
            new SadConsoleRect(40, 16, 37, 26),
            maxLength: 32,
            allowEmpty: false);
        component.InsertText("!");
        return component;
    }

    private IntSetterOverlayComponent IntSetterOverlayExample()
    {
        var component = new IntSetterOverlayComponent(
            "int-setter-overlay",
            "Int setter overlay",
            "target range",
            originalValue: 3,
            min: 0,
            max: 10,
            step: 1,
            bounds: new SadConsoleRect(79, 16, 38, 26));
        component.Handle(UiComponentCommand.Right);
        return component;
    }

    private ChoicePickerOverlayComponent ChoicePickerOverlayExample()
    {
        var component = new ChoicePickerOverlayComponent(
            "choice-picker-overlay",
            "Choice picker overlay",
            "color",
            [
                new SelectableListItem("green", "Green", SampleColorToken: "Green"),
                new SelectableListItem("yellow", "Yellow", SampleColorToken: "Yellow"),
                new SelectableListItem("orange", "Orange", SampleColorToken: "Orange")
            ],
            new SadConsoleRect(40, 27, 77, 39));
        component.Handle(UiComponentCommand.Down);
        return component;
    }

    private ConfirmOverlayComponent ConfirmOverlayExample()
    {
        var component = new ConfirmOverlayComponent(
            "confirm-overlay",
            "Confirm overlay",
            "Delete selected action step?",
            new SadConsoleRect(79, 13, 38, 23),
            confirmLabel: "Delete",
            backLabel: "Back");
        component.Handle(UiComponentCommand.Right);
        return component;
    }

    private static string BorderGlyphPreview(BorderGlyphTheme glyphs) =>
        $"{glyphs.TopLeft}{glyphs.Horizontal}{glyphs.TopRight} {glyphs.Vertical} {glyphs.BottomLeft}{glyphs.Horizontal}{glyphs.BottomRight}";
}
