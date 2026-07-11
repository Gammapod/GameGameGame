using GameGameGame.SadConsoleApp.Ui.Styling;
using GameGameGame.SadConsoleApp;

namespace GameGameGame.SadConsoleApp.Ui.Components;

internal sealed class TextEntryOverlayComponent : IUiComponent
{
    private readonly Func<string, string?>? _validate;

    public TextEntryOverlayComponent(
        string id,
        string title,
        string label,
        string originalValue,
        SadConsoleRect bounds,
        int? maxLength = null,
        bool allowEmpty = true,
        Func<string, string?>? validate = null)
    {
        Id = id;
        Title = title;
        Label = label;
        OriginalValue = originalValue;
        Buffer = originalValue;
        Bounds = bounds;
        MaxLength = maxLength;
        AllowEmpty = allowEmpty;
        _validate = validate;
    }

    public string Id { get; }
    public string Title { get; }
    public string Label { get; }
    public string OriginalValue { get; }
    public string Buffer { get; private set; }
    public SadConsoleRect Bounds { get; }
    public UiComponentState State => ValidationMessage is null ? UiComponentState.Focused : UiComponentState.Error;
    public int? MaxLength { get; }
    public bool AllowEmpty { get; }
    public string? ValidationMessage => Validate(Buffer);

    public void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        var next = Buffer + text;
        Buffer = MaxLength is { } max ? next[..Math.Min(max, next.Length)] : next;
    }

    public void Backspace()
    {
        if (Buffer.Length > 0)
        {
            Buffer = Buffer[..^1];
        }
    }

    public TextEntryOverlayResult Handle(UiComponentCommand command) => command switch
    {
        UiComponentCommand.Select => ValidationMessage is null
            ? TextEntryOverlayResult.Confirmed(Buffer)
            : TextEntryOverlayResult.Stay(ValidationMessage),
        UiComponentCommand.Cancel => TextEntryOverlayResult.Cancelled(OriginalValue),
        _ => TextEntryOverlayResult.Stay("Type text, Enter confirms, Esc cancels.")
    };

    public IReadOnlyList<string> RenderRows(SadConsoleTheme theme)
    {
        var rows = new List<string>
        {
            $"[{State.BorderColor(theme)}] {Title}",
            $"Enter the text for {Label}:",
            $"({theme.Field.EditableText}) {Buffer}_"
        };
        if (ValidationMessage is { } validation)
        {
            rows.Add($"({theme.Field.InvalidText}) {validation}");
        }

        return rows;
    }

    private string? Validate(string value)
    {
        if (!AllowEmpty && string.IsNullOrWhiteSpace(value))
        {
            return "Value is required.";
        }

        return _validate?.Invoke(value);
    }
}

internal sealed record TextEntryOverlayResult(FieldEditorOverlayResultKind Kind, string Value, string Message)
{
    public static TextEntryOverlayResult Stay(string message) => new(FieldEditorOverlayResultKind.Stay, string.Empty, message);
    public static TextEntryOverlayResult Confirmed(string value) => new(FieldEditorOverlayResultKind.Confirmed, value, "Confirmed text entry.");
    public static TextEntryOverlayResult Cancelled(string originalValue) => new(FieldEditorOverlayResultKind.Cancelled, originalValue, "Cancelled text entry.");
}

internal sealed class IntSetterOverlayComponent : IUiComponent
{
    public IntSetterOverlayComponent(string id, string title, string label, int originalValue, int min, int max, int step, SadConsoleRect bounds)
    {
        Id = id;
        Title = title;
        Label = label;
        OriginalValue = Math.Clamp(originalValue, min, max);
        Value = OriginalValue;
        Min = min;
        Max = max;
        Step = Math.Max(1, step);
        Bounds = bounds;
    }

    public string Id { get; }
    public string Title { get; }
    public string Label { get; }
    public int OriginalValue { get; }
    public int Value { get; private set; }
    public int Min { get; }
    public int Max { get; }
    public int Step { get; }
    public SadConsoleRect Bounds { get; }
    public UiComponentState State => UiComponentState.Focused;

    public FieldEditorValueResult<int> Handle(UiComponentCommand command)
    {
        switch (command)
        {
            case UiComponentCommand.Left:
            case UiComponentCommand.Down:
                Value = Math.Clamp(Value - Step, Min, Max);
                return FieldEditorValueResult<int>.Stay($"{Label}: {Value}");
            case UiComponentCommand.Right:
            case UiComponentCommand.Up:
                Value = Math.Clamp(Value + Step, Min, Max);
                return FieldEditorValueResult<int>.Stay($"{Label}: {Value}");
            case UiComponentCommand.Select:
                return FieldEditorValueResult<int>.Confirmed(Value, "Confirmed integer value.");
            case UiComponentCommand.Cancel:
                return FieldEditorValueResult<int>.Cancelled(OriginalValue, "Cancelled integer edit.");
            default:
                return FieldEditorValueResult<int>.Stay("Left/Right changes value, Enter confirms, Esc cancels.");
        }
    }

    public IReadOnlyList<string> RenderRows(SadConsoleTheme theme) =>
    [
        $"[{State.BorderColor(theme)}] {Title}",
        $"Set {Label} between {Min} and {Max}:",
        $"({theme.Field.EditableText}) [ {Value} ]"
    ];
}

internal sealed class ChoicePickerOverlayComponent : IUiComponent
{
    private readonly List<SelectableListItem> _choices;

    public ChoicePickerOverlayComponent(string id, string title, string label, IEnumerable<SelectableListItem> choices, SadConsoleRect bounds, int selectedIndex = 0)
    {
        Id = id;
        Title = title;
        Label = label;
        _choices = choices.ToList();
        Bounds = bounds;
        SelectedIndex = _choices.Count == 0 ? 0 : Math.Clamp(selectedIndex, 0, _choices.Count - 1);
    }

    public string Id { get; }
    public string Title { get; }
    public string Label { get; }
    public SadConsoleRect Bounds { get; }
    public UiComponentState State => _choices.Count == 0 ? UiComponentState.Disabled : UiComponentState.Focused;
    public int SelectedIndex { get; private set; }
    public SelectableListItem? SelectedChoice => _choices.Count == 0 ? null : _choices[SelectedIndex];

    public FieldEditorValueResult<SelectableListItem?> Handle(UiComponentCommand command)
    {
        switch (command)
        {
            case UiComponentCommand.Up:
            case UiComponentCommand.Left:
                Move(-1);
                return FieldEditorValueResult<SelectableListItem?>.Stay($"Selected choice: {SelectedChoice?.Label ?? "none"}.");
            case UiComponentCommand.Down:
            case UiComponentCommand.Right:
                Move(1);
                return FieldEditorValueResult<SelectableListItem?>.Stay($"Selected choice: {SelectedChoice?.Label ?? "none"}.");
            case UiComponentCommand.Select:
                return FieldEditorValueResult<SelectableListItem?>.Confirmed(SelectedChoice, $"Confirmed choice: {SelectedChoice?.Label ?? "none"}.");
            case UiComponentCommand.Cancel:
                return FieldEditorValueResult<SelectableListItem?>.Cancelled(null, "Cancelled choice picker.");
            default:
                return FieldEditorValueResult<SelectableListItem?>.Stay("Up/Down chooses, Enter confirms, Esc cancels.");
        }
    }

    public IReadOnlyList<string> RenderRows(SadConsoleTheme theme)
    {
        var rows = new List<string>
        {
            $"[{State.BorderColor(theme)}] {Title}",
            $"({theme.Field.LabelText}) {Label}"
        };
        if (_choices.Count == 0)
        {
            rows.Add($"({theme.List.EmptyText}) no choices available");
        }
        else
        {
            rows.AddRange(_choices.Select((choice, index) =>
                $"{(index == SelectedIndex ? ">" : " ")} ({(index == SelectedIndex ? theme.List.FocusedRowText : theme.List.RowText)}) {FormatChoice(choice)}"));
        }
        return rows;
    }

    private static string FormatChoice(SelectableListItem choice)
    {
        var sample = string.IsNullOrWhiteSpace(choice.SampleColorToken) ? string.Empty : $"({choice.SampleColorToken}) ■ ";
        var detail = string.IsNullOrWhiteSpace(choice.Detail) ? string.Empty : $" - {choice.Detail}";
        return $"{sample}{choice.Label}{detail}";
    }

    private void Move(int delta)
    {
        if (_choices.Count == 0) return;
        SelectedIndex = Math.Clamp(SelectedIndex + delta, 0, _choices.Count - 1);
    }
}

internal sealed record FieldEditorValueResult<T>(FieldEditorOverlayResultKind Kind, T Value, string Message)
{
    public static FieldEditorValueResult<T> Stay(string message) => new(FieldEditorOverlayResultKind.Stay, default!, message);
    public static FieldEditorValueResult<T> Confirmed(T value, string message) => new(FieldEditorOverlayResultKind.Confirmed, value, message);
    public static FieldEditorValueResult<T> Cancelled(T value, string message) => new(FieldEditorOverlayResultKind.Cancelled, value, message);
}

internal enum FieldEditorOverlayResultKind
{
    Stay,
    Confirmed,
    Cancelled
}

internal sealed class ConfirmOverlayComponent : IUiComponent
{
    private readonly string _confirmLabel;
    private readonly string _backLabel;
    private int _selectedIndex;

    public ConfirmOverlayComponent(string id, string title, string message, SadConsoleRect bounds, string confirmLabel = "Confirm", string backLabel = "Back")
    {
        Id = id;
        Title = title;
        Message = message;
        Bounds = bounds;
        _confirmLabel = confirmLabel;
        _backLabel = backLabel;
    }

    public string Id { get; }
    public string Title { get; }
    public string Message { get; }
    public SadConsoleRect Bounds { get; }
    public UiComponentState State => UiComponentState.Focused;
    public bool IsConfirmSelected => _selectedIndex == 0;

    public ConfirmOverlayResult Handle(UiComponentCommand command)
    {
        switch (command)
        {
            case UiComponentCommand.Left:
            case UiComponentCommand.Up:
                _selectedIndex = 0;
                return ConfirmOverlayResult.Stay($"Selected {_confirmLabel}.");
            case UiComponentCommand.Right:
            case UiComponentCommand.Down:
                _selectedIndex = 1;
                return ConfirmOverlayResult.Stay($"Selected {_backLabel}.");
            case UiComponentCommand.Select:
                return _selectedIndex == 0
                    ? ConfirmOverlayResult.Confirmed($"Confirmed: {Message}")
                    : ConfirmOverlayResult.Cancelled("Went back without confirming.");
            case UiComponentCommand.Cancel:
                return ConfirmOverlayResult.Cancelled("Went back without confirming.");
            default:
                return ConfirmOverlayResult.Stay("Choose Confirm or Back.");
        }
    }

    public IReadOnlyList<string> RenderRows(SadConsoleTheme theme) =>
    [
        $"[{State.BorderColor(theme)}] {Title}",
        Message,
        string.Empty,
        $"{(_selectedIndex == 0 ? ">" : " ")} ({(_selectedIndex == 0 ? theme.List.FocusedRowText : theme.List.RowText)}) {_confirmLabel}",
        $"{(_selectedIndex == 1 ? ">" : " ")} ({(_selectedIndex == 1 ? theme.List.FocusedRowText : theme.List.RowText)}) {_backLabel}"
    ];
}

internal sealed record ConfirmOverlayResult(FieldEditorOverlayResultKind Kind, string Message)
{
    public static ConfirmOverlayResult Stay(string message) => new(FieldEditorOverlayResultKind.Stay, message);
    public static ConfirmOverlayResult Confirmed(string message) => new(FieldEditorOverlayResultKind.Confirmed, message);
    public static ConfirmOverlayResult Cancelled(string message) => new(FieldEditorOverlayResultKind.Cancelled, message);
}

internal sealed class CommandPaletteOverlayComponent : IUiComponent
{
    private readonly ChoicePickerOverlayComponent _picker;

    public CommandPaletteOverlayComponent(string id, string title, string contextLabel, IEnumerable<SelectableListItem> commands, SadConsoleRect bounds)
    {
        Id = id;
        Title = title;
        ContextLabel = contextLabel;
        _picker = new ChoicePickerOverlayComponent(id, title, contextLabel, commands, bounds);
    }

    public string Id { get; }
    public string Title { get; }
    public string ContextLabel { get; }
    public SadConsoleRect Bounds => _picker.Bounds;
    public UiComponentState State => _picker.State;
    public int SelectedIndex => _picker.SelectedIndex;
    public SelectableListItem? SelectedCommand => _picker.SelectedChoice;

    public FieldEditorValueResult<SelectableListItem?> Handle(UiComponentCommand command) => _picker.Handle(command);

    public IReadOnlyList<string> RenderRows(SadConsoleTheme theme)
    {
        var rows = _picker.RenderRows(theme).ToList();
        if (rows.Count > 1)
        {
            rows[1] = $"Commands for {ContextLabel}:";
        }

        return rows;
    }
}
