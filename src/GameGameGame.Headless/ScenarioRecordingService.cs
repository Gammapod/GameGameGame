using GameGameGame.Content;
using GameGameGame.Core;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GameGameGame.Headless;

public sealed record ScenarioRecordingRequest(
    string ScenarioId,
    int TurnCount,
    string OutputDirectory);

public sealed record ScenarioRecordingFrame(
    int FrameIndex,
    int TurnNumber,
    string PngPath);

public sealed record ScenarioRecordingReport(
    string ScenarioId,
    string Name,
    PlaneId ScenarioPlaneId,
    EntityId? PlayerEntityId,
    IReadOnlyList<ScenarioRecordingFrame> Frames,
    string? GifPath,
    IReadOnlyList<string> ValidationDiagnostics,
    IReadOnlyList<string> RuntimeObservations,
    IReadOnlyList<string> RuntimeFailures,
    IReadOnlyList<string> CapabilityGaps);

public static class ScenarioRecordingService
{
    public static ScenarioRecordingReport Record(EditableContentDocument document, ScenarioRecordingRequest request)
    {
        if (request.TurnCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Scenario recording turn count must be non-negative.");
        }

        var validationDiagnostics = new List<string>();
        if (string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            validationDiagnostics.Add("recording output directory is required.");
        }
        else if (!Directory.Exists(request.OutputDirectory))
        {
            validationDiagnostics.Add($"recording output directory does not exist: {request.OutputDirectory}.");
        }

        ScenarioDefinition definition;
        try
        {
            definition = document.GetScenario(request.ScenarioId);
        }
        catch (KeyNotFoundException ex)
        {
            validationDiagnostics.Add(ex.Message);
            return CreateReport(
                request,
                name: request.ScenarioId,
                scenarioPlaneId: ScenarioMaterializer.DefaultScenarioPlaneId,
                playerEntityId: null,
                frames: [],
                gifPath: null,
                validationDiagnostics,
                runtimeObservations: [],
                runtimeFailures: [],
                capabilityGaps: []);
        }

        if (validationDiagnostics.Count > 0)
        {
            return CreateReport(
                request,
                definition.Name,
                ScenarioMaterializer.DefaultScenarioPlaneId,
                definition.PlayerEntityId,
                frames: [],
                gifPath: null,
                validationDiagnostics,
                runtimeObservations: [],
                runtimeFailures: [],
                capabilityGaps: []);
        }

        var materialization = ScenarioMaterializer.Materialize(document, definition);
        validationDiagnostics.AddRange(materialization.ValidationDiagnostics);

        if (!materialization.CanPlay || materialization.ScenarioPlaneId is not { } scenarioPlaneId)
        {
            return CreateReport(
                request,
                definition.Name,
                materialization.ScenarioPlaneId ?? ScenarioMaterializer.DefaultScenarioPlaneId,
                definition.PlayerEntityId,
                frames: [],
                gifPath: null,
                validationDiagnostics,
                runtimeObservations: [],
                materialization.RuntimeFailures,
                materialization.CapabilityGaps);
        }

        var outputDirectory = Path.GetFullPath(request.OutputDirectory);
        var baseName = SanitizeFileName(request.ScenarioId);
        var frames = new List<ScenarioRecordingFrame>();
        var runtimeObservations = new List<string>();

        var renderer = new DebugScenarioFrameRenderer();

        AddFrame(frames, outputDirectory, baseName, frameIndex: 0, turnNumber: 0, path =>
            renderer.RenderPng(materialization.World, materialization.Registry, materialization.ActionPlans, scenarioPlaneId, definition.PlayerEntityId, frameIndex: 0, turnNumber: 0, definition.ScenarioId, definition.Name, path));
        RunFullScenarioTurns(materialization.World, materialization.Registry, materialization.ActionPlans, scenarioPlaneId, request.TurnCount, runtimeObservations, turnNumber =>
            AddFrame(frames, outputDirectory, baseName, frameIndex: turnNumber, turnNumber, path =>
                renderer.RenderPng(materialization.World, materialization.Registry, materialization.ActionPlans, scenarioPlaneId, definition.PlayerEntityId, turnNumber, turnNumber, definition.ScenarioId, definition.Name, path)));

        var gifPath = Path.Combine(outputDirectory, $"{baseName}.gif");
        WriteGif(frames, gifPath);
        return CreateReport(
            request,
            definition.Name,
            scenarioPlaneId,
            definition.PlayerEntityId,
            frames,
            gifPath,
            validationDiagnostics,
            runtimeObservations,
            materialization.RuntimeFailures,
            materialization.CapabilityGaps);
    }

    private static void RunFullScenarioTurns(
        WorldState world,
        PrototypeContentRegistry registry,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        PlaneId scenarioPlaneId,
        int turnCount,
        List<string> runtimeObservations,
        Action<int> afterTurn)
    {
        var actorOrder = GetScenarioActorsInInitiativeOrder(world, actionPlans, scenarioPlaneId);
        var movement = new MovementService();

        for (var turn = 1; turn <= turnCount; turn++)
        {
            for (var initiative = 0; initiative < actorOrder.Count; initiative++)
            {
                var actorId = actorOrder[initiative];
                if (!world.Entities.TryGetValue(actorId, out var entity) || !actionPlans.TryGetValue(actorId, out var actionPlan))
                {
                    continue;
                }

                TargetingService.RefreshTargets(world, registry, actorId);
                var resolution = ResolvePlan(world, actorId, actionPlan.PlanTurn(world, actorId, movement), movement);
                PostActionStateUpdater.ApplyFacingFromMovement(world, actorId, resolution.ActorMovementDirection);
                world.RecordTrace(resolution.Trace);

                if (resolution.ConsumesTurn)
                {
                    world.AdvanceTurn();
                }

                if (!resolution.Succeeded)
                {
                    runtimeObservations.Add($"Turn {turn}, initiative {initiative + 1}: {entity.Name} could not act.");
                }
            }

            afterTurn(turn);
        }
    }

    private static IReadOnlyList<EntityId> GetScenarioActorsInInitiativeOrder(
        WorldState world,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        PlaneId scenarioPlaneId) =>
        actionPlans.Keys
            .Where(world.Entities.ContainsKey)
            .Select(entityId => (EntityId: entityId, Location: world.GetEntityLocation(entityId)))
            .Where(entry => entry.Location.PlaneId == scenarioPlaneId)
            .OrderBy(entry => entry.Location.Coord.Y)
            .ThenBy(entry => entry.Location.Coord.X)
            .ThenBy(entry => entry.EntityId.Value, StringComparer.Ordinal)
            .Select(entry => entry.EntityId)
            .ToList();

    private static ActionResolution ResolvePlan(WorldState world, EntityId actorId, PlannedActionPlan plan, MovementService movement)
    {
        var actorName = world.Entities.TryGetValue(actorId, out var actor) ? actor.Name : actorId.ToString();
        var root = new TraceNode($"Resolve plan for {actorName}", TraceStatus.Info);

        foreach (var option in plan.Options)
        {
            var resolution = option.Resolve(world, actorId, movement);
            root.Add(resolution.Trace);

            if (resolution.ConsumesTurn)
            {
                root.Status = resolution.Succeeded ? TraceStatus.Success : TraceStatus.Failure;
                root.Detail = $"resolved {option.GetType().Name}";
                    return new ActionResolution(resolution.Succeeded, resolution.ConsumesTurn, resolution.ContinuePlan, root, resolution.ActorMovementDirection);
            }

            if (!resolution.ContinuePlan)
            {
                root.Status = resolution.Succeeded ? TraceStatus.Success : TraceStatus.Failure;
                root.Detail = $"stopped at {option.GetType().Name}";
                return new ActionResolution(resolution.Succeeded, resolution.ConsumesTurn, resolution.ContinuePlan, root, resolution.ActorMovementDirection);
            }
        }

        root.Status = TraceStatus.Failure;
        root.Detail = "no planned action could execute";
        return new ActionResolution(false, ConsumesTurn: false, ContinuePlan: false, root);
    }

    private static void AddFrame(List<ScenarioRecordingFrame> frames, string outputDirectory, string baseName, int frameIndex, int turnNumber, Action<string> writeFrame)
    {
        var path = Path.Combine(outputDirectory, $"{baseName}_frame_{frameIndex:000}.png");
        writeFrame(path);
        frames.Add(new ScenarioRecordingFrame(
            frameIndex,
            turnNumber,
            path));
    }

    private static void WriteGif(IReadOnlyList<ScenarioRecordingFrame> frames, string gifPath)
    {
        if (frames.Count == 0)
        {
            return;
        }

        using var gif = Image.Load<Rgba32>(frames[0].PngPath);
        gif.Metadata.GetGifMetadata().RepeatCount = 0;
        gif.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = 75;

        foreach (var frame in frames.Skip(1))
        {
            using var next = Image.Load<Rgba32>(frame.PngPath);
            next.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = 75;
            gif.Frames.AddFrame(next.Frames.RootFrame);
        }

        gif.SaveAsGif(gifPath);
    }

    private static ScenarioRecordingReport CreateReport(
        ScenarioRecordingRequest request,
        string name,
        PlaneId scenarioPlaneId,
        EntityId? playerEntityId,
        IReadOnlyList<ScenarioRecordingFrame> frames,
        string? gifPath,
        IReadOnlyList<string> validationDiagnostics,
        IReadOnlyList<string> runtimeObservations,
        IReadOnlyList<string> runtimeFailures,
        IReadOnlyList<string> capabilityGaps) =>
        new(
            request.ScenarioId,
            name,
            scenarioPlaneId,
            playerEntityId,
            frames,
            gifPath,
            validationDiagnostics,
            runtimeObservations,
            runtimeFailures,
            capabilityGaps);

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var characters = value.Select(character => invalid.Contains(character) ? '_' : character).ToArray();
        var sanitized = new string(characters).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "scenario-recording" : sanitized;
    }
}

internal sealed class DebugScenarioFrameRenderer
{
    private const int CellWidth = 12;
    private const int CellHeight = 18;
    private const int Margin = 12;
    private const int HeaderHeight = 72;
    private const int PaneGap = 12;
    private const int ImageWidth = 960;
    private const int ImageHeight = 640;

    private readonly Font font = CreateFont(size: 14);
    private readonly Font smallFont = CreateFont(size: 11);

    public void RenderPng(
        WorldState world,
        PrototypeContentRegistry registry,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        PlaneId scenarioPlaneId,
        EntityId playerEntityId,
        int frameIndex,
        int turnNumber,
        string scenarioId,
        string scenarioName,
        string path)
    {
        using var image = new Image<Rgba32>(ImageWidth, ImageHeight, ToColor(PresentationColor.Default));
        var inspector = new EntityInspectionService(entityId => registry.GetPresentationForEntity(entityId).ToInspectionAppearance());
        var playerPlaneId = world.GetEntityLocation(playerEntityId).PlaneId;
        var containerId = inspector.FindEntityContainingPlane(world, playerPlaneId) ?? ScenarioMaterializer.DefaultScenarioRootEntityId;
        if (!world.Entities.ContainsKey(containerId))
        {
            containerId = playerEntityId;
        }

        var left = new Rectangle(Margin, HeaderHeight, 616, ImageHeight - HeaderHeight - Margin);
        var right = new Rectangle(left.Right + PaneGap, HeaderHeight, ImageWidth - left.Right - PaneGap - Margin, ImageHeight - HeaderHeight - Margin);
        var visibleCenters = new Dictionary<EntityId, PointF>();

        image.Mutate(context =>
        {
            DrawHeader(context, scenarioId, scenarioName, frameIndex, turnNumber, world.TurnNumber);
            DrawPane(context, world, inspector, registry, actionPlans, containerId, left, "Current Container", visibleCenters);
            DrawPane(context, world, inspector, registry, actionPlans, playerEntityId, right, "Player", visibleCenters);
            DrawTargetArrows(context, world, visibleCenters);
        });

        image.SaveAsPng(path);
    }

    private void DrawHeader(IImageProcessingContext context, string scenarioId, string scenarioName, int frameIndex, int turnNumber, int worldTurnNumber)
    {
        context.DrawText($"Scenario recording: {scenarioId} ({scenarioName})", font, Color.White, new PointF(Margin, 10));
        context.DrawText($"Frame {frameIndex} | Simulated turn {turnNumber} | World turn counter {worldTurnNumber}", smallFont, Color.LightGray, new PointF(Margin, 34));
        context.DrawText("Debug view: metadata above, inventory plane center, carried initiative/info below", smallFont, Color.Gray, new PointF(Margin, 52));
    }

    private void DrawPane(
        IImageProcessingContext context,
        WorldState world,
        EntityInspectionService inspector,
        PrototypeContentRegistry registry,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        EntityId entityId,
        Rectangle bounds,
        string title,
        Dictionary<EntityId, PointF> visibleCenters)
    {
        context.Draw(Color.DarkSlateGray, 1, bounds);
        var panel = inspector.Inspect(world, entityId);
        var titleColor = ToColor(panel.Color);
        context.DrawText($"{title}: {panel.Name}", font, titleColor, new PointF(bounds.X + 8, bounds.Y + 8));
        context.DrawText($"{panel.Glyph} {panel.EntityId} @ {panel.Address}", smallFont, Color.LightGray, new PointF(bounds.X + 8, bounds.Y + 30));

        var propertyY = bounds.Y + 50;
        foreach (var property in panel.Properties.Take(6))
        {
            context.DrawText($"{property.Name}: {property.Value}", smallFont, Color.Gray, new PointF(bounds.X + 8, propertyY));
            propertyY += 15;
        }

        if (panel.InventoryGrid is not { } grid)
        {
            context.DrawText("Inventory: none", smallFont, Color.Gray, new PointF(bounds.X + 8, bounds.Y + 150));
            return;
        }

        var gridPixelWidth = grid.Width * CellWidth;
        var gridPixelHeight = grid.Height * CellHeight;
        var gridLeft = bounds.X + Math.Max(8, (bounds.Width - gridPixelWidth) / 2);
        var gridTop = bounds.Y + 156;
        context.DrawText($"Inventory: {grid.PlaneId} ({grid.Width}x{grid.Height})", smallFont, Color.LightGray, new PointF(bounds.X + 8, gridTop - 20));

        foreach (var cell in grid.Cells)
        {
            var cellBounds = new Rectangle(gridLeft + cell.Coord.X * CellWidth, gridTop + cell.Coord.Y * CellHeight, CellWidth, CellHeight);
            context.Fill(Color.Black, cellBounds);
            context.Draw(Color.DimGray, 1, cellBounds);
            var glyph = cell.EntityId is null ? '.' : cell.Glyph;
            context.DrawText(glyph.ToString(), smallFont, ToColor(cell.Color), new PointF(cellBounds.X + 2, cellBounds.Y + 1));

            if (cell.EntityId is { } occupantId)
            {
                visibleCenters[occupantId] = new PointF(cellBounds.X + cellBounds.Width / 2f, cellBounds.Y + cellBounds.Height / 2f);
                DrawFacingMarker(context, world.GetActionFacing(occupantId), cellBounds);
                DrawTargetMarker(context, world, occupantId, cellBounds);
            }
        }

        DrawCarriedInfo(context, world, registry, actionPlans, grid.PlaneId, bounds, gridTop + gridPixelHeight + 20);
    }

    private void DrawCarriedInfo(IImageProcessingContext context, WorldState world, PrototypeContentRegistry registry, IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans, PlaneId planeId, Rectangle bounds, int top)
    {
        context.DrawText("Order | Entity | State", smallFont, Color.White, new PointF(bounds.X + 8, top));
        var rows = LocalTurnOrderReport.Create(world, planeId, actionPlans, getGlyph: entityId => registry.GetPresentationForEntity(entityId).Glyph).Rows;
        var y = top + 16;
        foreach (var row in rows.Take(12))
        {
            var facing = world.GetActionFacing(row.EntityId)?.ToString() ?? "none";
            var target = world.GetActionTarget(row.EntityId)?.ToString() ?? "none";
            context.DrawText($"{(row.Order < 0 ? "--" : row.Order)} | {row.Glyph} {row.EntityName} | F={facing} T={target}", smallFont, Color.LightGray, new PointF(bounds.X + 8, y));
            y += 15;
        }
    }

    private static void DrawFacingMarker(IImageProcessingContext context, Direction? direction, Rectangle cellBounds)
    {
        if (direction is null)
        {
            return;
        }

        var color = Color.Yellow;
        switch (direction.Value)
        {
            case Direction.North:
                context.DrawLine(color, 1, new PointF(cellBounds.Left, cellBounds.Top), new PointF(cellBounds.Right, cellBounds.Top));
                break;
            case Direction.South:
                context.DrawLine(color, 1, new PointF(cellBounds.Left, cellBounds.Bottom - 1), new PointF(cellBounds.Right, cellBounds.Bottom - 1));
                break;
            case Direction.East:
                context.DrawLine(color, 1, new PointF(cellBounds.Right - 1, cellBounds.Top), new PointF(cellBounds.Right - 1, cellBounds.Bottom));
                break;
            case Direction.West:
                context.DrawLine(color, 1, new PointF(cellBounds.Left, cellBounds.Top), new PointF(cellBounds.Left, cellBounds.Bottom));
                break;
        }
    }

    private static void DrawTargetMarker(IImageProcessingContext context, WorldState world, EntityId entityId, Rectangle cellBounds)
    {
        var target = world.GetActionTarget(entityId);
        if (target is null || target.Value == entityId)
        {
            return;
        }

        context.Draw(Color.White, 1, new Rectangle(cellBounds.X - 3, cellBounds.Y - 3, 7, 7));
    }

    private static void DrawTargetArrows(IImageProcessingContext context, WorldState world, IReadOnlyDictionary<EntityId, PointF> visibleCenters)
    {
        foreach (var (entityId, start) in visibleCenters)
        {
            var target = world.GetActionTarget(entityId);
            if (target is null || target.Value == entityId || !visibleCenters.TryGetValue(target.Value, out var end))
            {
                continue;
            }

            context.DrawLine(Color.White, 1, start, end);
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var length = MathF.Max(1, MathF.Sqrt(dx * dx + dy * dy));
            var ux = dx / length;
            var uy = dy / length;
            var left = new PointF(end.X - ux * 6 - uy * 3, end.Y - uy * 6 + ux * 3);
            var right = new PointF(end.X - ux * 6 + uy * 3, end.Y - uy * 6 - ux * 3);
            context.DrawLine(Color.White, 1, end, left);
            context.DrawLine(Color.White, 1, end, right);
        }
    }

    private static Color ToColor(PresentationColor color) => color switch
    {
        PresentationColor.White => Color.White,
        PresentationColor.Yellow => Color.Yellow,
        PresentationColor.Cyan => Color.Cyan,
        PresentationColor.Green => Color.LimeGreen,
        PresentationColor.DarkGreen => Color.DarkGreen,
        PresentationColor.Earth => Color.SandyBrown,
        PresentationColor.Gray => Color.Gray,
        _ => Color.Black
    };

    private static Font CreateFont(float size)
    {
        if (SystemFonts.TryGet("Segoe UI", out var family)
            || SystemFonts.TryGet("Arial", out family)
            || SystemFonts.TryGet("Consolas", out family))
        {
            return family.CreateFont(size);
        }

        return SystemFonts.Families.First().CreateFont(size);
    }
}
