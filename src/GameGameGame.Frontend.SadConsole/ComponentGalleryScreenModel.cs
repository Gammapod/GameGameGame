namespace GameGameGame.Frontend.SadConsole;

internal enum ComponentGalleryCommand
{
    Up,
    Down,
    Select,
    Cancel
}

internal enum ComponentGalleryExampleKind
{
    SelectorPopup,
    ToastPopup,
    StaticPlayRenderer,
    MoveAnimation,
    MoveAnimationQueue,
    EntityInspectionPanel
}

internal enum ComponentGalleryResultKind
{
    Stay,
    ExitRequested,
    SelectorPopupRequested,
    ToastRequested,
    StaticPlayRendererSelected,
    MoveAnimationSelected,
    MoveAnimationQueueSelected,
    EntityInspectionPanelSelected
}

internal sealed record ComponentGalleryExample(
    string Id,
    string Title,
    string Description,
    ComponentGalleryExampleKind Kind);

internal sealed record ComponentGalleryResult(
    ComponentGalleryResultKind Kind,
    string Message);

internal sealed class ComponentGalleryScreenModel
{
    private readonly List<ComponentGalleryExample> _examples;
    private int _selectedIndex;
    private int? _hoveredIndex;

    public ComponentGalleryScreenModel(IReadOnlyList<ComponentGalleryExample>? examples = null, int selectedIndex = 0)
    {
        _examples = examples?.ToList() ?? DefaultExamples().ToList();
        _selectedIndex = _examples.Count == 0 ? 0 : Math.Clamp(selectedIndex, 0, _examples.Count - 1);
    }

    public IReadOnlyList<ComponentGalleryExample> Examples => _examples;
    public int SelectedIndex => _selectedIndex;
    public int? HoveredIndex => _hoveredIndex;
    public bool SelectorPopupOpen { get; private set; }
    public ComponentGalleryExample? SelectedExample => _examples.Count == 0 ? null : _examples[_selectedIndex];
    public string Footer => SelectorPopupOpen
        ? "Enter: activate focused popup option  Esc: close popup"
        : "Up/Down: Move  Enter/Click: Run example  Esc: Back to browser";

    public static IReadOnlyList<ComponentGalleryExample> DefaultExamples() =>
    [
        new("selector-popup", "Selector popup", "Offset modal child surface; modal focus blocks underlying list input.", ComponentGalleryExampleKind.SelectorPopup),
        new("toast-popup", "Toast popup", "Offset warning notification; auto-dismisses after four seconds.", ComponentGalleryExampleKind.ToastPopup),
        new("static-play-renderer", "Static Play renderer", "Layered backdrop/entity/accent/status/highlight rendering through a camera.", ComponentGalleryExampleKind.StaticPlayRenderer),
        new("move-animation", "Move animation", "Simple adjacent-cell slide preview for an entity visual bundle.", ComponentGalleryExampleKind.MoveAnimation),
        new("move-animation-queue", "Move animation queue", "Sequential initiative-order slide playback with final redraw signal.", ComponentGalleryExampleKind.MoveAnimationQueue),
        new("entity-inspection-panel", "Entity inspection panel", "Popup status/actions/inventory layout with mixed-scale playspace regions.", ComponentGalleryExampleKind.EntityInspectionPanel)
    ];

    public ComponentGalleryResult Handle(ComponentGalleryCommand command)
    {
        if (SelectorPopupOpen)
        {
            return HandleSelectorPopup(command);
        }

        return command switch
        {
            ComponentGalleryCommand.Up => Move(-1),
            ComponentGalleryCommand.Down => Move(1),
            ComponentGalleryCommand.Select => ActivateSelected(),
            ComponentGalleryCommand.Cancel => new ComponentGalleryResult(ComponentGalleryResultKind.ExitRequested, "Gallery closed."),
            _ => new ComponentGalleryResult(ComponentGalleryResultKind.Stay, "Use Up/Down, Select, or Cancel.")
        };
    }

    public ComponentGalleryResult HoverExample(int index)
    {
        if (SelectorPopupOpen)
        {
            return new ComponentGalleryResult(ComponentGalleryResultKind.Stay, "Selector popup focused; gallery hover is inactive.");
        }

        if (index < 0 || index >= _examples.Count)
        {
            _hoveredIndex = null;
            return new ComponentGalleryResult(ComponentGalleryResultKind.Stay, "No gallery example is under the mouse.");
        }

        _hoveredIndex = index;
        return new ComponentGalleryResult(ComponentGalleryResultKind.Stay, $"Hover example: {_examples[index].Title}.");
    }

    public ComponentGalleryResult SelectExample(int index)
    {
        if (SelectorPopupOpen)
        {
            return new ComponentGalleryResult(ComponentGalleryResultKind.Stay, "Selector popup focused; close it before selecting another example.");
        }

        if (index < 0 || index >= _examples.Count)
        {
            return new ComponentGalleryResult(ComponentGalleryResultKind.Stay, "No gallery example is under the mouse.");
        }

        _selectedIndex = index;
        _hoveredIndex = null;
        return ActivateSelected();
    }

    public IReadOnlyList<string> SelectorPopupRows() =>
    [
        "Selector popup example",
        "This uses the reusable offset OverlayPanelConsole pattern.",
        "Mouse/keyboard focus stays on the popup until Cancel.",
        string.Empty,
        "> Primary action",
        "  Secondary action"
    ];

    public ToastNotificationState CreateToastExample() => new([
        "Warning",
        "Toast popup example",
        "Offset modal-style notification.",
        "Auto-dismisses after 4 seconds."
    ]);

    private ComponentGalleryResult HandleSelectorPopup(ComponentGalleryCommand command)
    {
        return command switch
        {
            ComponentGalleryCommand.Cancel => CloseSelectorPopup(),
            ComponentGalleryCommand.Select => new ComponentGalleryResult(ComponentGalleryResultKind.Stay, "Selector popup primary action activated."),
            _ => new ComponentGalleryResult(ComponentGalleryResultKind.Stay, "Selector popup focused; use Enter or Esc.")
        };
    }

    private ComponentGalleryResult Move(int delta)
    {
        if (_examples.Count == 0)
        {
            return new ComponentGalleryResult(ComponentGalleryResultKind.Stay, "No gallery examples are available.");
        }

        _selectedIndex = Math.Clamp(_selectedIndex + delta, 0, _examples.Count - 1);
        _hoveredIndex = null;
        return new ComponentGalleryResult(ComponentGalleryResultKind.Stay, $"Selected example: {SelectedExample?.Title}.");
    }

    private ComponentGalleryResult ActivateSelected()
    {
        return SelectedExample?.Kind switch
        {
            ComponentGalleryExampleKind.SelectorPopup => OpenSelectorPopup(),
            ComponentGalleryExampleKind.ToastPopup => new ComponentGalleryResult(ComponentGalleryResultKind.ToastRequested, "Toast popup example shown."),
            ComponentGalleryExampleKind.StaticPlayRenderer => new ComponentGalleryResult(ComponentGalleryResultKind.StaticPlayRendererSelected, "Static Play renderer example selected."),
            ComponentGalleryExampleKind.MoveAnimation => new ComponentGalleryResult(ComponentGalleryResultKind.MoveAnimationSelected, "Move animation example selected."),
            ComponentGalleryExampleKind.MoveAnimationQueue => new ComponentGalleryResult(ComponentGalleryResultKind.MoveAnimationQueueSelected, "Move animation queue example selected."),
            ComponentGalleryExampleKind.EntityInspectionPanel => new ComponentGalleryResult(ComponentGalleryResultKind.EntityInspectionPanelSelected, "Entity inspection panel example selected."),
            _ => new ComponentGalleryResult(ComponentGalleryResultKind.Stay, "No gallery example is selected.")
        };
    }

    private ComponentGalleryResult OpenSelectorPopup()
    {
        SelectorPopupOpen = true;
        _hoveredIndex = null;
        return new ComponentGalleryResult(ComponentGalleryResultKind.SelectorPopupRequested, "Selector popup example opened.");
    }

    private ComponentGalleryResult CloseSelectorPopup()
    {
        SelectorPopupOpen = false;
        return new ComponentGalleryResult(ComponentGalleryResultKind.Stay, "Selector popup example closed.");
    }
}
