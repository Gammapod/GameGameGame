using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Styling;

namespace GameGameGame.SadConsole.Tests;

public sealed class SadConsoleFieldEditorOverlayTests
{
    [Fact]
    public void TextEntryOverlayEditsConfirmsAndCancelsBuffer()
    {
        var overlay = new TextEntryOverlayComponent(
            "name-edit",
            "Edit name",
            "name",
            "Mouse",
            new SadConsoleRect(0, 0, 40, 8),
            maxLength: 6,
            allowEmpty: false);

        overlay.InsertText(" House");
        overlay.Backspace();
        var confirm = overlay.Handle(UiComponentCommand.Select);
        var cancel = overlay.Handle(UiComponentCommand.Cancel);

        Assert.Equal("Mouse", overlay.Buffer);
        Assert.Equal(FieldEditorOverlayResultKind.Confirmed, confirm.Kind);
        Assert.Equal("Mouse", confirm.Value);
        Assert.Equal(FieldEditorOverlayResultKind.Cancelled, cancel.Kind);
        Assert.Equal("Mouse", cancel.Value);
    }

    [Fact]
    public void TextEntryOverlayReportsValidationAndBlocksConfirm()
    {
        var overlay = new TextEntryOverlayComponent(
            "name-edit",
            "Edit name",
            "name",
            string.Empty,
            new SadConsoleRect(0, 0, 40, 8),
            allowEmpty: false);

        var result = overlay.Handle(UiComponentCommand.Select);

        Assert.Equal(UiComponentState.Error, overlay.State);
        Assert.Equal(FieldEditorOverlayResultKind.Stay, result.Kind);
        Assert.Contains("required", result.Message);
    }

    [Fact]
    public void IntSetterOverlayIncrementsClampsConfirmsAndCancels()
    {
        var overlay = new IntSetterOverlayComponent(
            "range-edit",
            "Edit range",
            "target range",
            originalValue: 9,
            min: 0,
            max: 10,
            step: 2,
            bounds: new SadConsoleRect(0, 0, 40, 8));

        overlay.Handle(UiComponentCommand.Right);
        overlay.Handle(UiComponentCommand.Right);
        Assert.Equal(10, overlay.Value);

        overlay.Handle(UiComponentCommand.Left);
        Assert.Equal(8, overlay.Value);

        var confirm = overlay.Handle(UiComponentCommand.Select);
        var cancel = overlay.Handle(UiComponentCommand.Cancel);

        Assert.Equal(FieldEditorOverlayResultKind.Confirmed, confirm.Kind);
        Assert.Equal(8, confirm.Value);
        Assert.Equal(FieldEditorOverlayResultKind.Cancelled, cancel.Kind);
        Assert.Equal(9, cancel.Value);
    }

    [Fact]
    public void ChoicePickerOverlayMovesConfirmsAndCancels()
    {
        var overlay = new ChoicePickerOverlayComponent(
            "color-picker",
            "Pick color",
            "color",
            [
                new SelectableListItem("green", "Green", SampleColorToken: "Green"),
                new SelectableListItem("yellow", "Yellow", SampleColorToken: "Yellow"),
                new SelectableListItem("orange", "Orange", SampleColorToken: "Orange")
            ],
            new SadConsoleRect(0, 0, 40, 10));

        overlay.Handle(UiComponentCommand.Down);
        overlay.Handle(UiComponentCommand.Down);
        overlay.Handle(UiComponentCommand.Down);
        var confirm = overlay.Handle(UiComponentCommand.Select);
        var cancel = overlay.Handle(UiComponentCommand.Cancel);

        Assert.Equal(2, overlay.SelectedIndex);
        Assert.Equal("orange", overlay.SelectedChoice?.Id);
        Assert.Equal(FieldEditorOverlayResultKind.Confirmed, confirm.Kind);
        Assert.Equal("orange", confirm.Value?.Id);
        Assert.Equal(FieldEditorOverlayResultKind.Cancelled, cancel.Kind);
    }

    [Fact]
    public void FieldEditorOverlayRowsUseFriendlyPromptTextWithoutInlineControls()
    {
        var text = new TextEntryOverlayComponent("text", "Text", "name", "Player", new SadConsoleRect(0, 0, 40, 8));
        var integer = new IntSetterOverlayComponent("int", "Int", "range", 3, 0, 10, 1, new SadConsoleRect(0, 0, 40, 8));
        var choice = new ChoicePickerOverlayComponent("choice", "Choice", "color", [new SelectableListItem("green", "Green")], new SadConsoleRect(0, 0, 40, 8));

        var rows = text.RenderRows(SadConsoleTheme.Default)
            .Concat(integer.RenderRows(SadConsoleTheme.Default))
            .Concat(choice.RenderRows(SadConsoleTheme.Default))
            .ToList();

        Assert.Contains(rows, row => row.Contains("Enter the text for name:"));
        Assert.Contains(rows, row => row.Contains("Set range between 0 and 10:"));
        Assert.Contains(rows, row => row.Contains("Green"));
        Assert.DoesNotContain(rows, row => row.Contains("Esc: cancel"));
        Assert.DoesNotContain(rows, row => row.Contains("original:"));
    }

    [Fact]
    public void ChoicePickerOverlayCanRenderColorSampleToken()
    {
        var choice = new ChoicePickerOverlayComponent(
            "choice",
            "Choice",
            "color",
            [new SelectableListItem("green", "Green", SampleColorToken: "Green")],
            new SadConsoleRect(0, 0, 40, 8));

        var rows = choice.RenderRows(SadConsoleTheme.Default);

        Assert.Contains(rows, row => row.Contains("(Green) ■ Green"));
    }

    [Fact]
    public void ConfirmOverlayConfirmsSelectedActionOrCancelsWithBackAndEscape()
    {
        var overlay = new ConfirmOverlayComponent(
            "confirm",
            "Confirm delete",
            "Delete selected step?",
            new SadConsoleRect(0, 0, 40, 8),
            confirmLabel: "Delete");

        var confirm = overlay.Handle(UiComponentCommand.Select);
        overlay.Handle(UiComponentCommand.Right);
        var back = overlay.Handle(UiComponentCommand.Select);
        var escape = overlay.Handle(UiComponentCommand.Cancel);

        Assert.Equal(FieldEditorOverlayResultKind.Confirmed, confirm.Kind);
        Assert.Equal(FieldEditorOverlayResultKind.Cancelled, back.Kind);
        Assert.Equal(FieldEditorOverlayResultKind.Cancelled, escape.Kind);
    }

    [Fact]
    public void CommandPaletteOverlayWrapsChoicePickerWithCommandContext()
    {
        var palette = new CommandPaletteOverlayComponent(
            "commands",
            "Commands",
            "selected action step",
            [
                new SelectableListItem("insert", "Insert step"),
                new SelectableListItem("delete", "Delete step")
            ],
            new SadConsoleRect(0, 0, 40, 8));

        palette.Handle(UiComponentCommand.Down);
        var confirm = palette.Handle(UiComponentCommand.Select);
        var rows = palette.RenderRows(SadConsoleTheme.Default);

        Assert.Equal("delete", palette.SelectedCommand?.Id);
        Assert.Equal(FieldEditorOverlayResultKind.Confirmed, confirm.Kind);
        Assert.Equal("delete", confirm.Value?.Id);
        Assert.Contains(rows, row => row.Contains("Commands for selected action step:"));
    }
}
