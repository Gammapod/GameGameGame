using GameGameGame.Core;
using SadRogue.Primitives;

namespace GameGameGame.Frontend.SadConsole;

internal sealed record EntityInspectionPanelLayout(
    FrontendRect Bounds,
    FrontendRect PortraitRegion,
    FrontendRect StatusRegion,
    FrontendRect ActionsRegion,
    FrontendRect? InventoryRegion,
    int VerticalSeparatorX,
    int ActionSeparatorY,
    int? InventorySeparatorY)
{
    public static EntityInspectionPanelLayout Resolve(FrontendRect bounds, bool showInventory)
    {
        var portrait = new FrontendRect(bounds.X + 1, bounds.Y + 1, 6, 6);
        var verticalSeparatorX = portrait.Right + 1;
        var status = new FrontendRect(verticalSeparatorX + 1, bounds.Y + 1, Math.Max(0, bounds.Right - verticalSeparatorX - 2), 6);
        var actionSeparatorY = portrait.Bottom + 1;
        var inventorySeparatorY = showInventory ? actionSeparatorY + 5 : (int?)null;
        var actionsBottom = inventorySeparatorY is { } separatorY ? separatorY - 1 : bounds.Bottom - 1;
        var actions = new FrontendRect(bounds.X + 1, actionSeparatorY + 1, bounds.Width - 2, Math.Max(0, actionsBottom - actionSeparatorY));
        var inventory = inventorySeparatorY is { } inventoryY
            ? new FrontendRect(bounds.X + 1, inventoryY + 1, Math.Min(10, bounds.Width - 2), Math.Min(6, bounds.Bottom - inventoryY - 1))
            : null;

        return new EntityInspectionPanelLayout(bounds, portrait, status, actions, inventory, verticalSeparatorX, actionSeparatorY, inventorySeparatorY);
    }
}

internal sealed record EntityInspectionActionRow(FrontendTextMessage Text, bool Selectable, FrontendTextMessage? FailureReason = null, PlayActionCandidate? Candidate = null);

internal sealed record PlayTransferSelectionRow(EntityId MovingEntityId, string Verb, string EntityName, bool IsSelected);

internal sealed record EntityInspectionPortraitCell(
    int X,
    int Y,
    int BackdropGlyph,
    Color BackdropForeground,
    Color BackdropBackground,
    int? EntityGlyph = null,
    Color? EntityForeground = null,
    int? FacingGlyph = null,
    global::SadConsole.Mirror FacingMirror = global::SadConsole.Mirror.None,
    CellHighlightKind? HighlightKind = null)
{
    public bool IsHighlighted => HighlightKind is not null;
}

internal sealed record EntityInspectionPanelModel(
    string EntityName,
    int Aperture,
    int Bulk,
    bool HasInventory,
    IReadOnlyList<EntityInspectionPortraitCell> PortraitCells,
    IReadOnlyList<EntityInspectionPortraitCell> InventoryCells,
    IReadOnlyList<EntityInspectionActionRow> Actions)
{
    public static EntityInspectionPanelModel GalleryExample() => new(
        "Debug Push Block",
        Aperture: 1,
        Bulk: 5,
        HasInventory: true,
        PortraitCells:
        [
            new(0, 0, 160, Color.DimGray, Color.Black),
            new(1, 0, 160, Color.DimGray, Color.Black),
            new(2, 0, 160, Color.DimGray, Color.Black),
            new(0, 1, 160, Color.DimGray, Color.Black),
            new(1, 1, 160, Color.DimGray, Color.Black, 254, Color.LightGray),
            new(2, 1, 160, Color.DimGray, Color.Black),
            new(0, 2, 160, Color.DimGray, Color.Black),
            new(1, 2, 160, Color.DimGray, Color.Black),
            new(2, 2, 160, Color.DimGray, Color.Black)
        ],
        [],
        [
            new EntityInspectionActionRow(FrontendTextMessage.Create(FrontendTextIds.InspectionActionPush, ("targetName", "Debug Push Block")), Selectable: true),
            new EntityInspectionActionRow(
                FrontendTextMessage.Create(FrontendTextIds.InspectionActionUnavailable, ("action", "Pickup Debug Push Block"), ("reason", "non-portable")),
                Selectable: false,
                FrontendTextMessage.Create("inspection.failure.nonPortable"))
        ]);
}

internal static class EntityInspectionPanelRenderer
{
    public static void Draw(
        global::SadConsole.Console target,
        EntityInspectionPanelLayout layout,
        EntityInspectionPanelModel model,
        TilesetProfile tilesetProfile,
        Color? backgroundOverride = null,
        int? selectedActionIndex = null,
        bool actionMenuFocused = false)
    {
        var background = backgroundOverride ?? Color.Black;
        PanelRenderer.DrawPanel(target, layout.Bounds, tilesetProfile.Roles.PanelBorder, Color.Gold, background);
        PrintClipped(target, layout.Bounds.X + 3, layout.Bounds.Y, layout.Bounds.Width - 6, model.EntityName, Color.White, background, tilesetProfile);
        DrawSeparators(target, layout, tilesetProfile.Roles.PanelBorder, Color.Gold, background);
        DrawReservedPlayspaceRegion(target, layout.PortraitRegion, tilesetProfile, background);
        var text = FrontendTextResolver.InspectionPrototype;
        PrintClipped(target, layout.StatusRegion.X, layout.StatusRegion.Y, layout.StatusRegion.Width, text.Resolve(FrontendTextMessage.Create(FrontendTextIds.InspectionStatAperture, ("value", model.Aperture))), Color.White, background, tilesetProfile);
        PrintClipped(target, layout.StatusRegion.X, layout.StatusRegion.Y + 1, layout.StatusRegion.Width, text.Resolve(FrontendTextMessage.Create(FrontendTextIds.InspectionStatBulk, ("value", model.Bulk))), Color.White, background, tilesetProfile);
        DrawActions(target, layout.ActionsRegion, model, tilesetProfile, background, text, selectedActionIndex, actionMenuFocused);
        if (layout.InventoryRegion is { } inventory)
        {
            DrawReservedInventoryRegion(target, inventory, tilesetProfile, background);
        }
    }

    private static void DrawSeparators(global::SadConsole.Console target, EntityInspectionPanelLayout layout, TileBorderGlyphSet border, Color foreground, Color background)
    {
        for (var y = layout.Bounds.Y + 1; y < layout.ActionSeparatorY; y++)
        {
            SetGlyph(target, layout.VerticalSeparatorX, y, border.Vertical, foreground, background);
        }

        DrawHorizontalSeparator(target, layout.Bounds, layout.ActionSeparatorY, border, foreground, background);
        SetGlyph(target, layout.VerticalSeparatorX, layout.Bounds.Y, border.HorizontalWithSouthVertical, foreground, background);
        SetGlyph(target, layout.VerticalSeparatorX, layout.ActionSeparatorY, border.HorizontalWithNorthVertical, foreground, background);
        if (layout.InventorySeparatorY is { } inventoryY)
        {
            DrawHorizontalSeparator(target, layout.Bounds, inventoryY, border, foreground, background);
        }
    }

    private static void DrawHorizontalSeparator(global::SadConsole.Console target, FrontendRect bounds, int y, TileBorderGlyphSet border, Color foreground, Color background)
    {
        for (var x = bounds.X + 1; x < bounds.Right; x++)
        {
            SetGlyph(target, x, y, border.Horizontal, foreground, background);
        }

        SetGlyph(target, bounds.X, y, border.VerticalWithEastHorizontal, foreground, background);
        SetGlyph(target, bounds.Right, y, border.VerticalWithWestHorizontal, foreground, background);
    }

    private static void DrawReservedPlayspaceRegion(global::SadConsole.Console target, FrontendRect region, TilesetProfile tilesetProfile, Color background)
    {
        for (var y = 0; y < region.Height; y++)
        for (var x = 0; x < region.Width; x++)
            SetGlyph(target, region.X + x, region.Y + y, tilesetProfile.Blank, Color.Black, background);

    }

    private static void DrawActions(global::SadConsole.Console target, FrontendRect region, EntityInspectionPanelModel model, TilesetProfile tilesetProfile, Color background, FrontendTextResolver text, int? selectedActionIndex, bool actionMenuFocused)
    {
        PrintClipped(target, region.X, region.Y, region.Width, text.Resolve(FrontendTextMessage.Create(FrontendTextIds.InspectionActionsHeader)), Color.Yellow, background, tilesetProfile);
        for (var i = 0; i < model.Actions.Count && i + 1 < region.Height; i++)
        {
            var action = model.Actions[i];
            var selected = actionMenuFocused && selectedActionIndex == i;
            var prefix = selected ? "> " : action.Selectable ? "  " : "~ ";
            var suffix = action.Selectable ? string.Empty : " ~";
            var color = selected ? Color.LightCyan : action.Selectable ? Color.Cyan : Color.Gray;
            PrintClipped(target, region.X, region.Y + i + 1, region.Width, prefix + text.Resolve(action.Text) + suffix, color, background, tilesetProfile);
        }
    }

    private static void DrawReservedInventoryRegion(global::SadConsole.Console target, FrontendRect region, TilesetProfile tilesetProfile, Color background)
    {
        for (var y = 0; y < region.Height; y++)
        for (var x = 0; x < region.Width; x++)
            SetGlyph(target, region.X + x, region.Y + y, tilesetProfile.Blank, Color.Black, background);
    }

    private static void PrintClipped(global::SadConsole.Console target, int x, int y, int width, string text, Color foreground, Color background, TilesetProfile tilesetProfile)
    {
        var clipped = text.Length <= width ? text : text[..Math.Max(0, width)];
        for (var index = 0; index < clipped.Length; index++)
        {
            SetGlyph(target, x + index, y, tilesetProfile.ResolveTextGlyph(clipped[index]), foreground, background);
        }
    }

    private static void SetGlyph(global::SadConsole.Console target, int x, int y, int glyph, Color foreground, Color background)
    {
        if (x < 0 || y < 0 || x >= target.Width || y >= target.Height) return;
        target.Surface[x, y].Glyph = glyph;
        target.Surface[x, y].Foreground = foreground;
        target.Surface[x, y].Background = background;
    }
}
