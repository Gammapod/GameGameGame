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
    public const int MinimumWidth = 24;
    public const int PreferredWidth = 32;
    public const int MaximumWidth = 36;
    public const double MaximumViewportWidthFraction = 0.40d;
    public const int MinimumHeight = 16;
    public const int MaximumHeight = 24;
    public const int MinimumActionRegionHeight = 6;
    public const int MaximumActionRegionHeight = 8;
    public const int MinimumInventoryRegionHeight = 2;
    public const int MaximumInventoryRegionHeight = 6;
    public const double InventoryHeightToWidthRatio = 0.75d;

    public static FrontendRect ResolveResponsiveBounds(FrontendRect available, int anchorRightPadding = 4, int anchorTopPadding = 0)
    {
        var fractionalMaxWidth = Math.Max(MinimumWidth, (int)Math.Floor(available.Width * MaximumViewportWidthFraction));
        var maxWidth = Math.Min(MaximumWidth, fractionalMaxWidth);
        var width = Math.Min(maxWidth, Math.Max(MinimumWidth, Math.Min(PreferredWidth, available.Width - Math.Max(0, anchorRightPadding * 2))));
        var height = Math.Min(MaximumHeight, Math.Max(MinimumHeight, available.Height - Math.Max(0, anchorTopPadding + 1)));
        return new FrontendRect(
            Math.Max(available.X, available.Right - width - Math.Max(0, anchorRightPadding)),
            Math.Min(available.Bottom - height + 1, available.Y + Math.Max(0, anchorTopPadding)),
            width,
            height);
    }

    public static EntityInspectionPanelLayout Resolve(FrontendRect bounds, bool showInventory)
    {
        var portrait = new FrontendRect(bounds.X + 1, bounds.Y + 1, 6, 6);
        var verticalSeparatorX = portrait.Right + 1;
        var status = new FrontendRect(verticalSeparatorX + 1, bounds.Y + 1, Math.Max(0, bounds.Right - verticalSeparatorX - 2), 6);
        var actionSeparatorY = portrait.Bottom + 1;
        var inventorySeparatorY = showInventory ? actionSeparatorY + MinimumActionRegionHeight + 1 : (int?)null;
        var actionsBottom = inventorySeparatorY is { } separatorY ? separatorY - 1 : bounds.Bottom - 1;
        var actions = new FrontendRect(bounds.X + 1, actionSeparatorY + 1, bounds.Width - 2, Math.Max(0, actionsBottom - actionSeparatorY));
        var inventory = inventorySeparatorY is { } inventoryY
            ? new FrontendRect(bounds.X + 1, inventoryY + 1, Math.Min(10, bounds.Width - 2), Math.Min(6, bounds.Bottom - inventoryY - 1))
            : null;

        return new EntityInspectionPanelLayout(bounds, portrait, status, actions, inventory, verticalSeparatorX, actionSeparatorY, inventorySeparatorY);
    }

    public static EntityInspectionPanelLayout ResolveAdaptive(FrontendRect bounds, EntityInspectionPanelModel model)
    {
        var portrait = new FrontendRect(bounds.X + 1, bounds.Y + 1, 6, 6);
        var verticalSeparatorX = portrait.Right + 1;
        var status = new FrontendRect(verticalSeparatorX + 1, bounds.Y + 1, Math.Max(0, bounds.Right - verticalSeparatorX - 2), 6);
        var actionSeparatorY = portrait.Bottom + 1;
        var availableRows = Math.Max(0, bounds.Bottom - actionSeparatorY - 1);
        var desiredActionHeight = ResolveDesiredActionRegionHeight(model.Actions.Count);

        if (!model.HasInventory)
        {
            var actionsOnly = new FrontendRect(bounds.X + 1, actionSeparatorY + 1, bounds.Width - 2, Math.Min(availableRows, desiredActionHeight));
            return new EntityInspectionPanelLayout(bounds, portrait, status, actionsOnly, null, verticalSeparatorX, actionSeparatorY, null);
        }

        var inventoryWidth = Math.Min(ResolveDesiredInventoryRegionWidth(model), bounds.Width - 2);
        var desiredInventoryHeight = ResolveDesiredInventoryRegionHeight(model, inventoryWidth);
        var actionHeight = Math.Min(desiredActionHeight, Math.Max(0, availableRows));
        var remainingRows = availableRows - actionHeight - 1;
        if (remainingRows < MinimumInventoryRegionHeight && availableRows >= MinimumActionRegionHeight + 1 + MinimumInventoryRegionHeight)
        {
            actionHeight = Math.Max(MinimumActionRegionHeight, availableRows - 1 - MinimumInventoryRegionHeight);
            remainingRows = availableRows - actionHeight - 1;
        }

        if (remainingRows < MinimumInventoryRegionHeight)
        {
            var expandedActions = new FrontendRect(bounds.X + 1, actionSeparatorY + 1, bounds.Width - 2, Math.Max(0, availableRows));
            return new EntityInspectionPanelLayout(bounds, portrait, status, expandedActions, null, verticalSeparatorX, actionSeparatorY, null);
        }

        var inventorySeparatorY = actionSeparatorY + actionHeight + 1;
        var actions = new FrontendRect(bounds.X + 1, actionSeparatorY + 1, bounds.Width - 2, actionHeight);
        var inventory = new FrontendRect(
            bounds.X + 1,
            inventorySeparatorY + 1,
            inventoryWidth,
            Math.Min(desiredInventoryHeight, remainingRows));
        return new EntityInspectionPanelLayout(bounds, portrait, status, actions, inventory, verticalSeparatorX, actionSeparatorY, inventorySeparatorY);
    }

    internal static int ResolveDesiredActionRegionHeight(int actionCount)
    {
        var visibleActionPreference = Math.Min(Math.Max(0, actionCount), 5);
        var overflowReserve = actionCount > visibleActionPreference ? 1 : 0;
        return Math.Clamp(1 + visibleActionPreference + overflowReserve, MinimumActionRegionHeight, MaximumActionRegionHeight);
    }

    internal static int ResolveDesiredInventoryRegionWidth(EntityInspectionPanelModel model)
    {
        if (model.DesiredVisibleInventoryWidth is { } desiredWidth)
        {
            return Math.Max(2, desiredWidth * 2);
        }

        var contentWidth = Math.Max(1, model.InventoryCells.Count == 0 ? 1 : model.InventoryCells.Max(cell => cell.X) + 1);
        return Math.Max(2, contentWidth * 2);
    }

    internal static int ResolveDesiredInventoryRegionHeight(EntityInspectionPanelModel model, int inventoryRegionWidth)
    {
        var contentWidth = Math.Max(1, model.InventoryCells.Count == 0 ? 1 : model.InventoryCells.Max(cell => cell.X) + 1);
        var contentHeight = Math.Max(1, model.InventoryCells.Count == 0 ? 1 : model.InventoryCells.Max(cell => cell.Y) + 1);
        var visibleWidth = Math.Max(1, inventoryRegionWidth / 2);
        if (contentWidth <= visibleWidth)
        {
            return Math.Max(MinimumInventoryRegionHeight, contentHeight * 2);
        }

        var ratioHeight = Math.Max(1, (int)Math.Ceiling(visibleWidth * InventoryHeightToWidthRatio));
        var visibleHeight = Math.Min(contentHeight, ratioHeight);
        return Math.Max(MinimumInventoryRegionHeight, visibleHeight * 2);
    }
}

internal sealed record EntityInspectionActionRow(FrontendTextMessage Text, bool Selectable, FrontendTextMessage? FailureReason = null, PlayActionCandidate? Candidate = null);

internal sealed record EntityInspectionPanelRenderOptions(bool ShowOverflowAffordances)
{
    public static EntityInspectionPanelRenderOptions Default { get; } = new(false);
    public static EntityInspectionPanelRenderOptions OverflowAffordances { get; } = new(true);
}

internal sealed record EntityInspectionActionViewport(
    int StartIndex,
    int ActionCount,
    int SelectedVisibleIndex,
    int HiddenAbove,
    int HiddenBelow)
{
    public bool HasItemsAbove => HiddenAbove > 0;
    public bool HasItemsBelow => HiddenBelow > 0;
}

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
    IReadOnlyList<EntityInspectionActionRow> Actions,
    string Description = "",
    int? DesiredVisibleInventoryWidth = null)
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
        ],
        "Compact example entity used as the baseline inspection panel gallery sample.");

    public static EntityInspectionPanelModel ResponsiveStressGalleryExample()
    {
        var inventory = new List<EntityInspectionPortraitCell>();
        for (var y = 0; y < 6; y++)
        for (var x = 0; x < 8; x++)
        {
            inventory.Add(new EntityInspectionPortraitCell(x, y, 160, Color.DimGray, Color.Black, x % 3 == 0 ? 254 : null, x % 3 == 0 ? Color.LightGreen : null));
        }

        return GalleryExample() with
        {
            EntityName = "Overstuffed Actor Inventory Container With A Deliberately Long Name",
            InventoryCells = inventory,
            Actions =
            [
                new EntityInspectionActionRow(FrontendTextMessage.Create(FrontendTextIds.InspectionActionPush, ("targetName", "the long-name crate")), Selectable: true),
                new EntityInspectionActionRow(FrontendTextMessage.Create(FrontendTextIds.InspectionActionPickup, ("targetName", "small gear")), Selectable: true),
                new EntityInspectionActionRow(FrontendTextMessage.Create(FrontendTextIds.InspectionActionDrop, ("targetName", "worn tool belt")), Selectable: true),
                new EntityInspectionActionRow(FrontendTextMessage.Create(FrontendTextIds.InspectionActionEnter, ("targetName", "nested portable pocket realm")), Selectable: true),
                new EntityInspectionActionRow(FrontendTextMessage.Create(FrontendTextIds.InspectionActionUnavailable, ("action", "Transfer all inventory into the nearby oversized destination"), ("reason", "destination cannot accept bulk")), Selectable: false),
                new EntityInspectionActionRow(FrontendTextMessage.Create(FrontendTextIds.InspectionActionUnavailable, ("action", "Push"), ("reason", "blocked by current facing")), Selectable: false)
            ],
            Description = "This stress sample checks Batch 1 responsive rules: the panel is anchored, capped to a maximum width, long descriptive status text wraps inside the status region, the action list clips to its region, and the actor inventory grid is clipped to the visible reserved cells."
        };
    }

    public static EntityInspectionPanelModel LargeInventoryGalleryExample()
    {
        var inventory = new List<EntityInspectionPortraitCell>();
        for (var y = 0; y < 20; y++)
        for (var x = 0; x < 20; x++)
        {
            var hasEntity = (x + y) % 4 == 0;
            inventory.Add(new EntityInspectionPortraitCell(
                x,
                y,
                160,
                Color.DimGray,
                Color.Black,
                hasEntity ? 128 + (x + y) % 10 : null,
                hasEntity ? Color.LightGreen : null));
        }

        return ResponsiveStressGalleryExample() with
        {
            EntityName = "Twenty By Twenty Inventory Container",
            InventoryCells = inventory,
            Actions =
            [
                new EntityInspectionActionRow(FrontendTextMessage.Create(FrontendTextIds.InspectionActionPickup, ("targetName", "visible item")), Selectable: true),
                new EntityInspectionActionRow(FrontendTextMessage.Create(FrontendTextIds.InspectionActionDrop, ("targetName", "carried item")), Selectable: true),
                new EntityInspectionActionRow(FrontendTextMessage.Create(FrontendTextIds.InspectionActionTransfer, ("targetName", "large inventory")), Selectable: true)
            ],
            Description = "Large inventory stress sample with 20x20 inventory content. Use this to judge whether the current capped panel leaves enough visual room."
        };
    }
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
        bool actionMenuFocused = false,
        EntityInspectionPanelRenderOptions? options = null)
    {
        options ??= EntityInspectionPanelRenderOptions.Default;
        var background = backgroundOverride ?? Color.Black;
        PanelRenderer.DrawPanel(target, layout.Bounds, tilesetProfile.Roles.PanelBorder, Color.Gold, background);
        PrintClipped(target, layout.Bounds.X + 3, layout.Bounds.Y, layout.Bounds.Width - 6, model.EntityName, Color.White, background, tilesetProfile);
        DrawSeparators(target, layout, tilesetProfile.Roles.PanelBorder, Color.Gold, background);
        DrawReservedPlayspaceRegion(target, layout.PortraitRegion, tilesetProfile, background);
        var text = FrontendTextResolver.InspectionPrototype;
        DrawStatus(target, layout.StatusRegion, model, tilesetProfile, background, text, options);
        DrawActions(target, layout.ActionsRegion, model, tilesetProfile, background, text, selectedActionIndex, actionMenuFocused, options);
        if (layout.InventoryRegion is { } inventory)
        {
            DrawReservedInventoryRegion(target, inventory, tilesetProfile, background);
        }
    }

    private static void DrawStatus(global::SadConsole.Console target, FrontendRect region, EntityInspectionPanelModel model, TilesetProfile tilesetProfile, Color background, FrontendTextResolver text, EntityInspectionPanelRenderOptions options)
    {
        if (region.Width <= 0 || region.Height <= 0) return;
        var lines = ResolveStatusLines(model, text, region.Width);

        for (var i = 0; i < lines.Count && i < region.Height; i++)
        {
            PrintClipped(target, region.X, region.Y + i, region.Width, lines[i], Color.White, background, tilesetProfile);
        }

        if (options.ShowOverflowAffordances && lines.Count > region.Height)
        {
            SetGlyph(target, region.Right, region.Bottom, tilesetProfile.Roles.DownChevron, Color.Yellow, background);
        }
    }

    internal static IReadOnlyList<string> ResolveStatusLines(EntityInspectionPanelModel model, FrontendTextResolver text, int width)
    {
        var lines = new List<string>
        {
            text.Resolve(FrontendTextMessage.Create(FrontendTextIds.InspectionStatAperture, ("value", model.Aperture))),
            text.Resolve(FrontendTextMessage.Create(FrontendTextIds.InspectionStatBulk, ("value", model.Bulk)))
        };

        if (!string.IsNullOrWhiteSpace(model.Description))
        {
            lines.AddRange(WrapText(model.Description, width));
        }

        return lines;
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

    private static void DrawActions(global::SadConsole.Console target, FrontendRect region, EntityInspectionPanelModel model, TilesetProfile tilesetProfile, Color background, FrontendTextResolver text, int? selectedActionIndex, bool actionMenuFocused, EntityInspectionPanelRenderOptions options)
    {
        PrintClipped(target, region.X, region.Y, region.Width, text.Resolve(FrontendTextMessage.Create(FrontendTextIds.InspectionActionsHeader)), Color.Yellow, background, tilesetProfile);
        var viewport = ResolveActionViewport(model.Actions.Count, region.Height, selectedActionIndex ?? 0, options.ShowOverflowAffordances);
        var y = region.Y + 1;
        if (viewport.HasItemsAbove)
        {
            DrawOverflowCount(target, new FrontendRect(region.X, y, region.Width, 1), viewport.HiddenAbove, tilesetProfile, background, OverflowDirection.Up);
            y++;
        }

        for (var visibleIndex = 0; visibleIndex < viewport.ActionCount; visibleIndex++, y++)
        {
            var absoluteIndex = viewport.StartIndex + visibleIndex;
            var action = model.Actions[absoluteIndex];
            var selected = actionMenuFocused && selectedActionIndex == absoluteIndex;
            var prefix = selected ? "> " : action.Selectable ? "  " : "~ ";
            var suffix = action.Selectable ? string.Empty : " ~";
            var color = selected ? Color.LightCyan : action.Selectable ? Color.Cyan : Color.Gray;
            PrintClipped(target, region.X, y, region.Width, prefix + text.Resolve(action.Text) + suffix, color, background, tilesetProfile);
        }

        if (viewport.HasItemsBelow)
        {
            DrawOverflowCount(target, new FrontendRect(region.X, region.Bottom, region.Width, 1), viewport.HiddenBelow, tilesetProfile, background, OverflowDirection.Down);
        }
    }

    internal static EntityInspectionActionViewport ResolveActionViewport(int actionCount, int regionHeight, int selectedIndex, bool showOverflowAffordances)
    {
        var contentRows = Math.Max(0, regionHeight - 1);
        if (actionCount <= 0 || contentRows <= 0)
        {
            return new EntityInspectionActionViewport(0, 0, 0, 0, Math.Max(0, actionCount));
        }

        if (!showOverflowAffordances || actionCount <= contentRows)
        {
            var count = Math.Min(actionCount, contentRows);
            return new EntityInspectionActionViewport(0, count, Math.Clamp(selectedIndex, 0, Math.Max(0, count - 1)), 0, Math.Max(0, actionCount - count));
        }

        var selected = Math.Clamp(selectedIndex, 0, actionCount - 1);
        var actionRows = contentRows;
        var start = 0;
        var hasAbove = false;
        var hasBelow = false;
        for (var pass = 0; pass < 3; pass++)
        {
            start = Math.Clamp(selected - actionRows / 2, 0, Math.Max(0, actionCount - actionRows));
            hasAbove = start > 0;
            hasBelow = start + actionRows < actionCount;
            actionRows = Math.Max(0, contentRows - (hasAbove ? 1 : 0) - (hasBelow ? 1 : 0));
        }

        start = Math.Clamp(selected - actionRows / 2, 0, Math.Max(0, actionCount - actionRows));
        hasAbove = start > 0;
        hasBelow = start + actionRows < actionCount;
        actionRows = Math.Max(0, contentRows - (hasAbove ? 1 : 0) - (hasBelow ? 1 : 0));
        start = Math.Clamp(selected - actionRows / 2, 0, Math.Max(0, actionCount - actionRows));

        return new EntityInspectionActionViewport(
            start,
            Math.Min(actionRows, Math.Max(0, actionCount - start)),
            selected - start,
            start,
            Math.Max(0, actionCount - start - actionRows));
    }

    internal static int ResolveHiddenActionAffordanceCount(int actionCount, int regionHeight)
    {
        var viewport = ResolveActionViewport(actionCount, regionHeight, selectedIndex: 0, showOverflowAffordances: true);
        return viewport.HiddenAbove + viewport.HiddenBelow;
    }

    private static void DrawReservedInventoryRegion(global::SadConsole.Console target, FrontendRect region, TilesetProfile tilesetProfile, Color background)
    {
        for (var y = 0; y < region.Height; y++)
        for (var x = 0; x < region.Width; x++)
            SetGlyph(target, region.X + x, region.Y + y, tilesetProfile.Blank, Color.Black, background);
    }

    internal static (int VisibleCells, int HiddenCells, bool HasHiddenRight, bool HasHiddenBelow) ResolveInventoryOverflow(FrontendRect inventoryRegion, IReadOnlyList<EntityInspectionPortraitCell> inventoryCells)
    {
        var viewport = EntityInspectionPlayspaceOverlayPresenter.ResolveInventoryViewport(inventoryRegion, inventoryCells);
        var visibleCells = inventoryCells.Count(cell => cell.X < viewport.ContentWidth && cell.Y < viewport.ContentHeight);
        var contentWidth = inventoryCells.Count == 0 ? 0 : inventoryCells.Max(cell => cell.X) + 1;
        var contentHeight = inventoryCells.Count == 0 ? 0 : inventoryCells.Max(cell => cell.Y) + 1;
        return (visibleCells, Math.Max(0, inventoryCells.Count - visibleCells), contentWidth > viewport.ChildWidth, contentHeight > viewport.ChildHeight);
    }

    private enum OverflowDirection
    {
        Up,
        Down
    }

    private static void DrawOverflowCount(global::SadConsole.Console target, FrontendRect region, int hiddenCount, TilesetProfile tilesetProfile, Color background, OverflowDirection direction)
    {
        if (region.Width <= 0 || region.Height <= 0 || hiddenCount <= 0) return;
        var label = $"+{hiddenCount}";
        var width = Math.Min(region.Width, label.Length + 1);
        var x = region.Right - width + 1;
        if (direction == OverflowDirection.Up)
        {
            SetGlyph(target, x, region.Bottom, tilesetProfile.Roles.UpChevron, Color.Yellow, background);
            PrintClipped(target, x + 1, region.Bottom, width - 1, label, Color.Yellow, background, tilesetProfile);
        }
        else
        {
            PrintClipped(target, x, region.Bottom, width - 1, label, Color.Yellow, background, tilesetProfile);
            SetGlyph(target, region.Right, region.Bottom, tilesetProfile.Roles.DownChevron, Color.Yellow, background);
        }
    }

    internal static IReadOnlyList<string> WrapText(string text, int width)
    {
        if (width <= 0 || string.IsNullOrWhiteSpace(text)) return [];
        var lines = new List<string>();
        foreach (var paragraph in text.Split('\n'))
        {
            var remaining = paragraph.Trim();
            while (remaining.Length > width)
            {
                var breakAt = remaining.LastIndexOf(' ', Math.Min(width, remaining.Length - 1));
                if (breakAt <= 0) breakAt = width;
                lines.Add(remaining[..breakAt].TrimEnd());
                remaining = remaining[breakAt..].TrimStart();
            }

            if (remaining.Length > 0)
            {
                lines.Add(remaining);
            }
        }

        return lines;
    }

    private static void PrintClipped(global::SadConsole.Console target, int x, int y, int width, string text, Color foreground, Color background, TilesetProfile tilesetProfile)
    {
        var glyphs = FrontendTextClipping.ToClippedGlyphs(text, width, tilesetProfile);
        for (var index = 0; index < glyphs.Count; index++)
        {
            SetGlyph(target, x + index, y, glyphs[index], foreground, background);
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
