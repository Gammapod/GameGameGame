using GameGameGame.SadConsoleApp.Ui.Styling;
using GameGameGame.SadConsoleApp;

namespace GameGameGame.SadConsoleApp.Ui.Components;

internal enum EditableFieldMode
{
    ReadOnly,
    Editable,
    Editing
}

internal sealed record EditableFieldComponent(
    string Id,
    string Label,
    string Value,
    EditableFieldMode Mode = EditableFieldMode.Editable,
    bool IsDirty = false,
    string? ValidationMessage = null)
{
    public bool IsValid => string.IsNullOrWhiteSpace(ValidationMessage);
    public bool CanEdit => Mode is EditableFieldMode.Editable or EditableFieldMode.Editing;

    public EditableFieldComponent BeginEdit() => CanEdit ? this with { Mode = EditableFieldMode.Editing } : this;

    public EditableFieldComponent EndEdit(string newValue, bool dirty = true) => CanEdit
        ? this with { Value = newValue, Mode = EditableFieldMode.Editable, IsDirty = dirty }
        : this;

    public string Render(SadConsoleTheme theme)
    {
        var color = !IsValid
            ? theme.Field.InvalidText
            : IsDirty
                ? theme.Field.DirtyText
                : Mode == EditableFieldMode.ReadOnly
                    ? theme.Field.ReadOnlyText
                    : Mode == EditableFieldMode.Editing
                        ? theme.Field.EditableText
                        : theme.Field.ValueText;
        var editMarker = Mode == EditableFieldMode.Editing ? "*" : Mode == EditableFieldMode.Editable ? ":" : "=";
        var validation = IsValid ? string.Empty : $" ! {ValidationMessage}";
        return $"({theme.Field.LabelText}) {Label}{editMarker} ({color}) {Value}{validation}";
    }
}

internal sealed record FieldGroupComponent(
    string Id,
    string Title,
    SadConsoleRect Bounds,
    IReadOnlyList<EditableFieldComponent> Fields,
    UiComponentState State = UiComponentState.Unselected) : IUiComponent
{
    public IReadOnlyList<string> RenderRows(SadConsoleTheme theme)
    {
        var rows = new List<string> { $"[{State.BorderColor(theme)}] {Title}" };
        rows.AddRange(Fields.Count == 0 ? [$"({theme.Panel.MutedText}) no fields"] : Fields.Select(field => field.Render(theme)));
        return rows;
    }
}
