namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed record ActorPovPlayLayout(
    int Width,
    int Height,
    SadConsoleRect DrawableBounds,
    IReadOnlyList<ActorPovPlayRegion> Regions,
    IReadOnlyList<ActorPovPlayLayoutDiagnostic> Diagnostics)
{
    public ActorPovPlayRegion Root => Region(ActorPovPlayRegionIds.Root);
    public ActorPovPlayRegion ParentChain => Region(ActorPovPlayRegionIds.ParentChain);
    public ActorPovPlayRegion CurrentPlace => Region(ActorPovPlayRegionIds.CurrentPlace);
    public ActorPovPlayRegion WorldInspection => Region(ActorPovPlayRegionIds.WorldInspection);
    public ActorPovPlayRegion ActorInventory => Region(ActorPovPlayRegionIds.ActorInventory);
    public ActorPovPlayRegion ActorInventoryInspection => Region(ActorPovPlayRegionIds.ActorInventoryInspection);
    public ActorPovPlayRegion Chrome => Region(ActorPovPlayRegionIds.Chrome);
    public ActorPovPlayRegion Connectors => Region(ActorPovPlayRegionIds.Connectors);
    public ActorPovPlayRegion DiagnosticsRegion => Region(ActorPovPlayRegionIds.Diagnostics);

    private ActorPovPlayRegion Region(string id) => Regions.Single(region => region.Id == id);
}

internal sealed record ActorPovPlayRegion(
    string Id,
    string Title,
    SadConsoleRect Bounds,
    int Layer,
    ActorPovPlayRegionRole Role,
    bool IsOmitted = false);

internal enum ActorPovPlayRegionRole
{
    Root,
    Content,
    Chrome,
    ConnectorOverlay,
    DiagnosticOverlay
}

internal sealed record ActorPovPlayLayoutDiagnostic(string Code, string Message);

internal static class ActorPovPlayRegionIds
{
    public const string Root = "0.actor-pov-root";
    public const string ParentChain = "0.actor-pov.parent-chain";
    public const string CurrentPlace = "0.actor-pov.current-place";
    public const string WorldInspection = "0.actor-pov.world-inspection";
    public const string ActorInventory = "0.actor-pov.actor-inventory";
    public const string ActorInventoryInspection = "0.actor-pov.actor-inventory-inspection";
    public const string Chrome = "0.actor-pov.chrome";
    public const string Connectors = "0.actor-pov.connectors";
    public const string Diagnostics = "0.actor-pov.diagnostics";
}

internal static class ActorPovPlayLayoutResolver
{
    private const int RegionGap = 1;
    private const int MinimumUsefulWidth = 24;
    private const int MinimumUsefulHeight = 12;
    private const int MinimumCurrentPovSize = 8;
    private const int MinimumBottomHeight = 5;
    private const int DesiredBottomHeight = 8;
    private const int DiagnosticsMaxWidth = 64;
    private const int DiagnosticsMaxHeight = 7;

    public static ActorPovPlayLayout Resolve(SadConsoleRect drawableBounds)
    {
        var width = Math.Max(0, drawableBounds.Width);
        var height = Math.Max(0, drawableBounds.Height);
        var diagnostics = new List<ActorPovPlayLayoutDiagnostic>();

        if (width < MinimumUsefulWidth || height < MinimumUsefulHeight)
        {
            diagnostics.Add(new ActorPovPlayLayoutDiagnostic(
                "actor-pov.layout.too-small",
                $"Drawable bounds {width}x{height} are too small for Actor POV content regions; content regions are omitted."));

            return new ActorPovPlayLayout(
                width,
                height,
                drawableBounds,
                BuildOmittedRegions(drawableBounds),
                diagnostics);
        }

        var bottomHeight = Math.Min(DesiredBottomHeight, Math.Max(MinimumBottomHeight, height / 3));
        var upperHeight = height - RegionGap - bottomHeight;
        if (upperHeight < MinimumCurrentPovSize)
        {
            upperHeight = MinimumCurrentPovSize;
            bottomHeight = Math.Max(0, height - RegionGap - upperHeight);
            diagnostics.Add(new ActorPovPlayLayoutDiagnostic(
                "actor-pov.layout.bottom-compressed",
                "Actor inventory band was compressed to preserve the current POV region."));
        }

        var upperBounds = SadConsoleRect.FromSize(drawableBounds.Left, drawableBounds.Top, width, upperHeight);
        var bottomTop = upperBounds.Bottom + RegionGap;
        var bottomBounds = SadConsoleRect.FromSize(drawableBounds.Left, bottomTop, width, Math.Max(0, drawableBounds.Bottom - bottomTop));

        var currentSize = Math.Min(upperBounds.Height, Math.Max(MinimumCurrentPovSize, Math.Min(width / 2, upperBounds.Height)));
        var currentLeft = drawableBounds.Left + Math.Max(0, (width - currentSize) / 2);
        var currentTop = upperBounds.Top + Math.Max(0, (upperBounds.Height - currentSize) / 2);
        var currentBounds = SadConsoleRect.FromSize(currentLeft, currentTop, currentSize, currentSize);

        var parentWidth = Math.Max(0, currentBounds.Left - drawableBounds.Left - RegionGap);
        var worldLeft = currentBounds.Left + currentBounds.Width + RegionGap;
        var worldWidth = Math.Max(0, drawableBounds.Left + width - worldLeft);
        var parentBounds = SadConsoleRect.FromSize(drawableBounds.Left, upperBounds.Top, parentWidth, upperBounds.Height);
        var worldBounds = SadConsoleRect.FromSize(worldLeft, upperBounds.Top, worldWidth, upperBounds.Height);

        var actorInventoryWidth = Math.Max(0, Math.Min(width, Math.Max(currentSize, (width * 2) / 5)));
        var actorInventoryBounds = SadConsoleRect.FromSize(bottomBounds.Left, bottomBounds.Top, actorInventoryWidth, bottomBounds.Height);
        var carriedLeft = actorInventoryBounds.Left + actorInventoryBounds.Width + RegionGap;
        var carriedWidth = Math.Max(0, bottomBounds.Left + bottomBounds.Width - carriedLeft);
        var carriedBounds = SadConsoleRect.FromSize(carriedLeft, bottomBounds.Top, carriedWidth, bottomBounds.Height);

        var diagnosticBounds = SadConsoleRect.FromSize(
            drawableBounds.Left + Math.Max(0, width - Math.Min(DiagnosticsMaxWidth, width)),
            drawableBounds.Top,
            Math.Min(DiagnosticsMaxWidth, width),
            Math.Min(DiagnosticsMaxHeight, height));

        return new ActorPovPlayLayout(
            width,
            height,
            drawableBounds,
            [
                new ActorPovPlayRegion(ActorPovPlayRegionIds.Root, "Actor POV Play root", drawableBounds, 0, ActorPovPlayRegionRole.Root),
                new ActorPovPlayRegion(ActorPovPlayRegionIds.ParentChain, "Parent/location chain", parentBounds, 1, ActorPovPlayRegionRole.Content, IsOmitted: parentBounds.Width == 0 || parentBounds.Height == 0),
                new ActorPovPlayRegion(ActorPovPlayRegionIds.CurrentPlace, "Current actor POV", currentBounds, 1, ActorPovPlayRegionRole.Content),
                new ActorPovPlayRegion(ActorPovPlayRegionIds.WorldInspection, "World inspection chain", worldBounds, 1, ActorPovPlayRegionRole.Content, IsOmitted: worldBounds.Width == 0 || worldBounds.Height == 0),
                new ActorPovPlayRegion(ActorPovPlayRegionIds.ActorInventory, "Controlled actor inventory", actorInventoryBounds, 1, ActorPovPlayRegionRole.Content, IsOmitted: actorInventoryBounds.Width == 0 || actorInventoryBounds.Height == 0),
                new ActorPovPlayRegion(ActorPovPlayRegionIds.ActorInventoryInspection, "Actor carried-item inspection chain", carriedBounds, 1, ActorPovPlayRegionRole.Content, IsOmitted: carriedBounds.Width == 0 || carriedBounds.Height == 0),
                new ActorPovPlayRegion(ActorPovPlayRegionIds.Chrome, "Actor POV chrome", drawableBounds, 5, ActorPovPlayRegionRole.Chrome),
                new ActorPovPlayRegion(ActorPovPlayRegionIds.Connectors, "Actor POV connectors", drawableBounds, 6, ActorPovPlayRegionRole.ConnectorOverlay),
                new ActorPovPlayRegion(ActorPovPlayRegionIds.Diagnostics, "Actor POV diagnostics", diagnosticBounds, 20, ActorPovPlayRegionRole.DiagnosticOverlay)
            ],
            diagnostics);
    }

    private static IReadOnlyList<ActorPovPlayRegion> BuildOmittedRegions(SadConsoleRect drawableBounds)
    {
        var omitted = SadConsoleRect.FromSize(drawableBounds.Left, drawableBounds.Top, 0, 0);
        var diagnosticsBounds = SadConsoleRect.FromSize(drawableBounds.Left, drawableBounds.Top, Math.Max(0, drawableBounds.Width), Math.Min(3, Math.Max(0, drawableBounds.Height)));
        return [
            new ActorPovPlayRegion(ActorPovPlayRegionIds.Root, "Actor POV Play root", drawableBounds, 0, ActorPovPlayRegionRole.Root),
            new ActorPovPlayRegion(ActorPovPlayRegionIds.ParentChain, "Parent/location chain", omitted, 1, ActorPovPlayRegionRole.Content, IsOmitted: true),
            new ActorPovPlayRegion(ActorPovPlayRegionIds.CurrentPlace, "Current actor POV", omitted, 1, ActorPovPlayRegionRole.Content, IsOmitted: true),
            new ActorPovPlayRegion(ActorPovPlayRegionIds.WorldInspection, "World inspection chain", omitted, 1, ActorPovPlayRegionRole.Content, IsOmitted: true),
            new ActorPovPlayRegion(ActorPovPlayRegionIds.ActorInventory, "Controlled actor inventory", omitted, 1, ActorPovPlayRegionRole.Content, IsOmitted: true),
            new ActorPovPlayRegion(ActorPovPlayRegionIds.ActorInventoryInspection, "Actor carried-item inspection chain", omitted, 1, ActorPovPlayRegionRole.Content, IsOmitted: true),
            new ActorPovPlayRegion(ActorPovPlayRegionIds.Chrome, "Actor POV chrome", drawableBounds, 5, ActorPovPlayRegionRole.Chrome),
            new ActorPovPlayRegion(ActorPovPlayRegionIds.Connectors, "Actor POV connectors", drawableBounds, 6, ActorPovPlayRegionRole.ConnectorOverlay),
            new ActorPovPlayRegion(ActorPovPlayRegionIds.Diagnostics, "Actor POV diagnostics", diagnosticsBounds, 20, ActorPovPlayRegionRole.DiagnosticOverlay)
        ];
    }
}
