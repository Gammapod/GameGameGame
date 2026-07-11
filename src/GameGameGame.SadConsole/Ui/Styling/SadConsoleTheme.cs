namespace GameGameGame.SadConsoleApp.Ui.Styling;

internal sealed record SadConsoleTheme(
    string Name,
    PanelTheme Panel,
    ListTheme List,
    FieldTheme Field,
    FooterTheme Footer)
{
    public static SadConsoleTheme Default { get; } = new(
        "Default",
        PanelTheme.Default,
        ListTheme.Default,
        FieldTheme.Default,
        FooterTheme.Default);

    public static SadConsoleTheme Blueprint { get; } = new(
        "Blueprint",
        PanelTheme.Blueprint,
        ListTheme.Blueprint,
        FieldTheme.Blueprint,
        FooterTheme.Blueprint);

    public static IReadOnlyList<SadConsoleTheme> BuiltInThemes { get; } = [Default, Blueprint];
}

internal sealed record BorderGlyphTheme(
    char TopLeft,
    char TopRight,
    char BottomLeft,
    char BottomRight,
    char Horizontal,
    char Vertical)
{
    public static BorderGlyphTheme SquareAscii { get; } = new('+', '+', '+', '+', '-', '|');
    public static BorderGlyphTheme DoubleAscii { get; } = new('#', '#', '#', '#', '=', '!');
}

internal sealed record PanelTheme(
    string BorderUnselected,
    string BorderSelected,
    string BorderFocused,
    string BorderDisabled,
    string BorderError,
    string TitleText,
    string BodyText,
    string MutedText,
    BorderGlyphTheme BorderGlyphs)
{
    public static PanelTheme Default { get; } = new(
        BorderUnselected: "MutedBlue",
        BorderSelected: "Gold",
        BorderFocused: "HotPink",
        BorderDisabled: "DarkGray",
        BorderError: "Red",
        TitleText: "White",
        BodyText: "LightGray",
        MutedText: "Gray",
        BorderGlyphs: BorderGlyphTheme.SquareAscii);

    public static PanelTheme Blueprint { get; } = new(
        BorderUnselected: "Cyan",
        BorderSelected: "White",
        BorderFocused: "Orange",
        BorderDisabled: "DarkGray",
        BorderError: "Red",
        TitleText: "Cyan",
        BodyText: "White",
        MutedText: "LightGray",
        BorderGlyphs: BorderGlyphTheme.DoubleAscii);
}

internal sealed record ListTheme(
    string RowText,
    string SelectedRowText,
    string FocusedRowText,
    string EmptyText,
    string ScrollIndicator)
{
    public static ListTheme Default { get; } = new(
        RowText: "LightGray",
        SelectedRowText: "Gold",
        FocusedRowText: "HotPink",
        EmptyText: "Gray",
        ScrollIndicator: "Cyan");

    public static ListTheme Blueprint { get; } = new(
        RowText: "White",
        SelectedRowText: "Cyan",
        FocusedRowText: "Orange",
        EmptyText: "LightGray",
        ScrollIndicator: "Gold");
}

internal sealed record FieldTheme(
    string LabelText,
    string ValueText,
    string EditableText,
    string DirtyText,
    string InvalidText,
    string ReadOnlyText)
{
    public static FieldTheme Default { get; } = new(
        LabelText: "LightGray",
        ValueText: "White",
        EditableText: "Gold",
        DirtyText: "Orange",
        InvalidText: "Red",
        ReadOnlyText: "Gray");

    public static FieldTheme Blueprint { get; } = new(
        LabelText: "Cyan",
        ValueText: "White",
        EditableText: "Orange",
        DirtyText: "Gold",
        InvalidText: "Red",
        ReadOnlyText: "LightGray");
}

internal sealed record FooterTheme(
    string Background,
    string Text,
    string KeyText,
    string WarningText)
{
    public static FooterTheme Default { get; } = new(
        Background: "DarkBlue",
        Text: "White",
        KeyText: "Gold",
        WarningText: "Orange");

    public static FooterTheme Blueprint { get; } = new(
        Background: "Black",
        Text: "Cyan",
        KeyText: "White",
        WarningText: "Gold");
}
