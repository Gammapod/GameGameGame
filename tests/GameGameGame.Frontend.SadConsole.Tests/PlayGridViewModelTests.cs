using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class PlayGridViewModelTests
{
    [Fact]
    public void PlayGridViewModelBuildsDebugRoomStartingLocationWithBackdropUnderEveryCell()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var tileset = TilesetProfileLoader.LoadCandii();

        var grid = PlayGridViewModel.FromSession(session, tileset);

        Assert.Equal(9, grid.Width);
        Assert.Equal(7, grid.Height);
        Assert.Equal(new GameGameGame.Core.EntityId("debugStartRoom"), grid.ContainerEntityId);
        Assert.Equal(63, grid.Cells.Count);
        Assert.All(grid.Cells, cell => Assert.Equal(tileset.Roles.DefaultBackdrop, cell.BackdropGlyph));
        Assert.Equal(new GameGameGame.Core.GridCoord(4, 3), grid.ControlledEntityCoord);
        var playerCell = grid.CellAt(4, 3);
        Assert.Equal(session.PlayerEntityId, playerCell.EntityId);
        Assert.Equal(219, playerCell.EntityGlyph);
        Assert.Equal(252, playerCell.FacingGlyph);
        Assert.Equal(global::SadConsole.Mirror.None, playerCell.FacingMirror);
    }

    [Fact]
    public void PlayGridAndInspectionInventoryUseContainerMaterialBackdrop()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              room:
                name: Stone Room
                inventoryWidth: 3
                inventoryHeight: 2
                bulk: 100
                aperture: 100
                material: stone
              player:
                name: Player
                inventoryWidth: 1
                inventoryHeight: 1
                bulk: 1
                aperture: 5
            presentations:
              room: { glyph: '#', color: Gray }
              player: { glyph: '@', color: Yellow }
            actionPlans: {}
            scenarios:
              material-room:
                name: Material Room
                scenarioRootEntityTemplateId: room
                playerEntityTemplateId: player
                playerEntityId: player
                playerStart: { x: 1, y: 1 }
            """);
        var session = PlayableScenarioLauncher.CreateFromDocument(document, "material-room");
        var tileset = TilesetProfileLoader.LoadCandii();

        var grid = PlayGridViewModel.FromSession(session, tileset);
        var inspectionCells = InspectionInventoryProjector.Project(session, session.ActiveContainerEntityId, tileset);

        Assert.All(grid.Cells, cell => Assert.Equal(tileset.Roles.GridCave, cell.BackdropGlyph));
        Assert.NotEmpty(inspectionCells);
        Assert.All(inspectionCells, cell => Assert.Equal(tileset.Roles.GridCave, cell.BackdropGlyph));
    }

    [Fact]
    public void TryCellAtUsesCoordinatesWithoutLinearSearchSemantics()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var tileset = TilesetProfileLoader.LoadCandii();
        var grid = PlayGridViewModel.FromSession(session, tileset);

        Assert.Same(grid.Cells[0], grid.TryCellAt(0, 0));
        Assert.Null(grid.TryCellAt(-1, 0));
        Assert.Null(grid.TryCellAt(grid.Width, grid.Height));
    }

    [Fact]
    public void PlayGridViewModelDefaultsControlledActorToLaunchPlayerForExistingCallers()
    {
        var playerId = new EntityId("launchPlayer");
        var controlledId = new EntityId("otherActor");
        var session = CreateTwoActorSession(playerId, controlledId);

        var grid = PlayGridViewModel.FromSession(session, TilesetProfileLoader.LoadCandii());

        Assert.Equal(playerId, grid.ControlledEntityId);
        Assert.Equal(new GridCoord(0, 0), grid.ControlledEntityCoord);
    }

    [Fact]
    public void PlayGridViewModelUsesSuppliedControlledActorForCoordIdentityAndStyling()
    {
        var playerId = new EntityId("launchPlayer");
        var controlledId = new EntityId("otherActor");
        var session = CreateTwoActorSession(playerId, controlledId);

        var grid = PlayGridViewModel.FromSession(session, TilesetProfileLoader.LoadCandii(), controlledEntityId: controlledId);

        Assert.Equal(controlledId, grid.ControlledEntityId);
        Assert.Equal(new GridCoord(1, 0), grid.ControlledEntityCoord);
        Assert.Equal(global::SadRogue.Primitives.Color.Yellow, grid.CellAt(1, 0).EntityForeground);
        Assert.Equal(global::SadRogue.Primitives.Color.White, grid.CellAt(0, 0).EntityForeground);
    }

    [Fact]
    public void PlayGridViewModelResolvesRenderedPlaneFromSuppliedControlledActor()
    {
        var playerPlane = new PlaneId("player-plane");
        var controlledPlane = new PlaneId("controlled-plane");
        var playerId = new EntityId("launchPlayer");
        var controlledId = new EntityId("otherActor");
        var world = new WorldState();
        AddPlane(world, playerPlane, 1, 1);
        AddPlane(world, controlledPlane, 2, 1);
        AddEntity(world, playerId, "Player", new PlaneCoord(playerPlane, new GridCoord(0, 0)));
        AddEntity(world, controlledId, "Actor", new PlaneCoord(controlledPlane, new GridCoord(1, 0)));
        var session = CreateSession("retargeted-plane", world, playerId, activePlaneId: playerPlane, activeContainerEntityId: playerId);

        var grid = PlayGridViewModel.FromSession(session, TilesetProfileLoader.LoadCandii(), controlledEntityId: controlledId);

        Assert.Equal(controlledPlane, grid.PlaneId);
        Assert.Equal(controlledId, grid.ControlledEntityId);
        Assert.Equal(new GridCoord(1, 0), grid.ControlledEntityCoord);
    }

    [Fact]
    public void PlayGridViewModelHidesCellsOutsideTopologyPovByDefault()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var tileset = TilesetProfileLoader.LoadCandii();
        var projection = new TopologyVisibilityProjectionService().Project(session.World, session.PlayerEntityId, maxDepth: 0, contextDepth: 10);

        var grid = PlayGridViewModel.FromSession(session, tileset, topologyVisibility: projection);

        var playerCell = grid.CellAt(4, 3);
        Assert.True(playerCell.IsInPointOfView);
        Assert.Null(grid.TryCellAt(0, 0));
    }

    [Fact]
    public void PlayGridViewModelCanShowCellsOutsideTopologyPovAsDimmedDebugContext()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var tileset = TilesetProfileLoader.LoadCandii();
        var projection = new TopologyVisibilityProjectionService().Project(session.World, session.PlayerEntityId, maxDepth: 0, contextDepth: 10);

        var grid = PlayGridViewModel.FromSession(session, tileset, topologyVisibility: projection, showOutsidePointOfViewContext: true);

        var playerCell = grid.CellAt(4, 3);
        var contextCell = grid.CellAt(0, 0);
        Assert.True(playerCell.IsInPointOfView);
        Assert.False(contextCell.IsInPointOfView);
        Assert.True(contextCell.IsDimmedByPointOfView);
        Assert.NotEqual(playerCell.BackdropForeground, contextCell.BackdropForeground);
    }

    [Fact]
    public void PlayGridViewModelIncludesVisibleCellsAcrossTopologySeams()
    {
        var path = Path.Combine(
            TestRepository.Root(),
            "src",
            "GameGameGame.Content",
            "Beta",
            "Topology",
            "RoomHallAlignedJoinShowcase.yaml");
        var session = PlayableScenarioLauncher.CreateFromFile(path, "beta-room-hall-aligned-join");
        var tileset = TilesetProfileLoader.LoadCandii();
        var projection = new TopologyVisibilityProjectionService().Project(session.World, session.PlayerEntityId, maxDepth: 2, contextDepth: 10);

        var grid = PlayGridViewModel.FromSession(session, tileset, topologyVisibility: projection);

        Assert.True(grid.Width >= 5);
        var hallCell = grid.TryCellAt(3, 1);
        Assert.NotNull(hallCell);
        Assert.True(hallCell.IsInPointOfView);
        Assert.NotNull(hallCell.TopologyNodeId);
        Assert.Equal(new TopologyLayoutCoord(new GridCoord(3, 1)), hallCell.LayoutCoord);
    }

    [Fact]
    public void PlayGridViewModelDoesNotDuplicatePlayerWhenTopologyLayoutDiffersFromSourcePlaneCoordinates()
    {
        var path = Path.Combine(
            TestRepository.Root(),
            "src",
            "GameGameGame.Content",
            "Beta",
            "Topology",
            "RoomHallAlignedJoinShowcase.yaml");
        var session = PlayableScenarioLauncher.CreateFromFile(path, "beta-room-hall-aligned-join");
        var tileset = TilesetProfileLoader.LoadCandii();
        MoveEntity(session.World, session.PlayerEntityId, new PlaneCoord(new PlaneId("roomHallHallAB"), new GridCoord(1, 0)));
        var projection = new TopologyVisibilityProjectionService().Project(session.World, session.PlayerEntityId, maxDepth: 2, contextDepth: 10);

        var grid = PlayGridViewModel.FromSession(session, tileset, topologyVisibility: projection);

        var playerCells = grid.Cells.Where(cell => cell.EntityId == session.PlayerEntityId).ToList();
        var playerCell = Assert.Single(playerCells);
        Assert.Equal(new GridCoord(4, 1), new GridCoord(playerCell.X, playerCell.Y));
        Assert.Equal(new GridCoord(4, 1), grid.ControlledEntityCoord);
    }

    [Fact]
    public void PlayGridViewModelDoesNotFillMergedLayerBoundingBoxGaps()
    {
        var path = Path.Combine(
            TestRepository.Root(),
            "src",
            "GameGameGame.Content",
            "Beta",
            "Topology",
            "MergedInventoryLayerShowcase.yaml");
        var session = PlayableScenarioLauncher.CreateFromFile(path, "delta-merged-inventory-layer-acceptance");
        var tileset = TilesetProfileLoader.LoadCandii();
        MoveEntity(session.World, session.PlayerEntityId, new PlaneCoord(new PlaneId("mergedLayerGateB"), new GridCoord(1, 0)));
        var projection = new TopologyVisibilityProjectionService().Project(session.World, session.PlayerEntityId, maxDepth: 10, contextDepth: 10);

        var grid = PlayGridViewModel.FromSession(session, tileset, topologyVisibility: projection);

        AssertNoLayoutCell(grid, new GridCoord(3, 0));
        AssertNoLayoutCell(grid, new GridCoord(4, 0));
        AssertNoLayoutCell(grid, new GridCoord(0, 2));
        AssertNoLayoutCell(grid, new GridCoord(2, 3));
    }

    [Fact]
    public void PlayGridViewModelDoesNotFillMergedLayerBoundingBoxGapsFromFirstContribution()
    {
        var path = Path.Combine(
            TestRepository.Root(),
            "src",
            "GameGameGame.Content",
            "Beta",
            "Topology",
            "MergedInventoryLayerShowcase.yaml");
        var session = PlayableScenarioLauncher.CreateFromFile(path, "delta-merged-inventory-layer-acceptance");
        var tileset = TilesetProfileLoader.LoadCandii();
        MoveEntity(session.World, session.PlayerEntityId, new PlaneCoord(new PlaneId("mergedLayerGateA"), new GridCoord(1, 0)));
        var projection = new TopologyVisibilityProjectionService().Project(session.World, session.PlayerEntityId, TopologyVisibilityProjectionService.DefaultPlayPovDepth, TopologyVisibilityProjectionService.DefaultPlayContextDepth);

        var grid = PlayGridViewModel.FromSession(session, tileset, topologyVisibility: projection);

        AssertNoLayoutCell(grid, new GridCoord(3, 0));
        AssertNoLayoutCell(grid, new GridCoord(4, 0));
        AssertNoLayoutCell(grid, new GridCoord(0, 2));
        AssertNoLayoutCell(grid, new GridCoord(2, 3));
    }

    [Fact]
    public void PlayGridViewModelDimsTopologyContextCellsOutsidePovRangeWhenDebugContextVisible()
    {
        var path = Path.Combine(
            TestRepository.Root(),
            "src",
            "GameGameGame.Content",
            "Beta",
            "Topology",
            "RoomHallAlignedJoinShowcase.yaml");
        var session = PlayableScenarioLauncher.CreateFromFile(path, "beta-room-hall-aligned-join");
        var tileset = TilesetProfileLoader.LoadCandii();
        var projection = new TopologyVisibilityProjectionService().Project(session.World, session.PlayerEntityId, maxDepth: 0, contextDepth: 2);

        var grid = PlayGridViewModel.FromSession(session, tileset, topologyVisibility: projection, showOutsidePointOfViewContext: true);

        var origin = grid.CellAt(1, 1);
        var context = grid.CellAt(2, 1);
        Assert.True(origin.IsInPointOfView);
        Assert.False(context.IsInPointOfView);
        Assert.True(context.IsDimmedByPointOfView);
    }

    [Fact]
    public void PlayGridViewModelReportsTopologyCellsThatShareOneDisplayCoordinate()
    {
        var world = new WorldState();
        AddPlane(world, new PlaneId("first"), 1, 1);
        AddPlane(world, new PlaneId("second"), 1, 1);
        var playerId = new EntityId("player");
        AddEntity(world, playerId, "Player", new PlaneCoord(new PlaneId("first"), new GridCoord(0, 0)));
        var session = new PlayableScenarioSession(
            "collision",
            "Collision",
            world,
            new PrototypeContentRegistry(new Dictionary<EntityTemplateId, EntityTemplate>(), new Dictionary<ActionPlanTemplateId, ActionPlanDescriptor>(), new Dictionary<EntityTemplateId, EntityPresentation>()),
            new Dictionary<EntityId, IEntityActionPlan>(),
            playerId,
            new PlaneId("first"),
            playerId,
            CanPlay: true,
            [],
            [],
            []);
        var firstCell = new TopologyVisibleCellProjection(
            new TopologyCellRef(new PlaneCoord(new PlaneId("first"), new GridCoord(0, 0))),
            0,
            null,
            null,
            null,
            new TopologyNodeId("first:0,0"),
            new TopologyLayoutCoord(new GridCoord(0, 0)));
        var secondCell = new TopologyVisibleCellProjection(
            new TopologyCellRef(new PlaneCoord(new PlaneId("second"), new GridCoord(0, 0))),
            1,
            null,
            Direction.East,
            TopologyEdgeKind.MergedInventoryLayer,
            new TopologyNodeId("second:0,0"),
            new TopologyLayoutCoord(new GridCoord(0, 0)));
        var projection = new TopologyVisibilityProjection(
            playerId,
            firstCell.Cell,
            1,
            [firstCell, secondCell],
            [],
            [firstCell, secondCell],
            []);

        var grid = PlayGridViewModel.FromSession(session, TilesetProfileLoader.LoadCandii(), topologyVisibility: projection);

        var diagnostic = Assert.Single(grid.Diagnostics, diagnostic => diagnostic.Code == PlayGridDiagnosticCode.DisplayCoordinateCollision);
        Assert.Equal(new GridCoord(0, 0), diagnostic.DisplayCoord);
        Assert.Contains(firstCell.Cell.SourceCoord, diagnostic.SourceCoords);
        Assert.Contains(secondCell.Cell.SourceCoord, diagnostic.SourceCoords);
        Assert.Single(grid.Cells);
    }

    [Fact]
    public void MovementPreviewCellDoesNotInspectEntityAtDisplayAdjacentButTopologyDisconnectedOverlap()
    {
        var world = new WorldState();
        var actorPlane = new PlaneId("actor-room");
        var overlapPlane = new PlaneId("overlap-room");
        AddPlane(world, actorPlane, 1, 1);
        AddPlane(world, overlapPlane, 1, 1);
        var actorId = new EntityId("actor");
        var overlapBagId = new EntityId("overlapBag");
        var actorSource = new PlaneCoord(actorPlane, new GridCoord(0, 0));
        var bagSource = new PlaneCoord(overlapPlane, new GridCoord(0, 0));
        AddEntity(world, actorId, "Actor", actorSource);
        AddEntity(world, overlapBagId, "Overlap Bag", bagSource);
        var actorCell = new TopologyVisibleCellProjection(
            new TopologyCellRef(actorSource),
            0,
            null,
            null,
            null,
            new TopologyNodeId("actor-room:0,0"),
            new TopologyLayoutCoord(new GridCoord(1, 0)));
        var bagCell = new TopologyVisibleCellProjection(
            new TopologyCellRef(bagSource),
            99,
            null,
            null,
            null,
            new TopologyNodeId("overlap-room:0,0"),
            new TopologyLayoutCoord(new GridCoord(0, 0)));
        var session = new PlayableScenarioSession(
            "overlap",
            "Overlap",
            world,
            new PrototypeContentRegistry(new Dictionary<EntityTemplateId, EntityTemplate>(), new Dictionary<ActionPlanTemplateId, ActionPlanDescriptor>(), new Dictionary<EntityTemplateId, EntityPresentation>()),
            new Dictionary<EntityId, IEntityActionPlan>(),
            actorId,
            actorPlane,
            actorId,
            CanPlay: true,
            [],
            [],
            []);
        var projection = new TopologyVisibilityProjection(actorId, actorCell.Cell, 0, [actorCell], [], [actorCell, bagCell], []);
        var grid = PlayGridViewModel.FromSession(session, TilesetProfileLoader.LoadCandii(), topologyVisibility: projection, showOutsidePointOfViewContext: true);

        var displayAdjacent = grid.TryCellAt(0, 0);
        var resolvedPreview = PlayModeConsole.ResolveMovementPreviewCell(world, actorId, Direction.West, grid);

        Assert.Equal(overlapBagId, displayAdjacent?.EntityId);
        Assert.Null(resolvedPreview);
    }

    [Fact]
    public void PlayGridViewModelDrawsInPovCellOverOverlappingContextCell()
    {
        var world = new WorldState();
        var actorPlane = new PlaneId("actor-room");
        var overlapPlane = new PlaneId("overlap-room");
        AddPlane(world, actorPlane, 1, 1);
        AddPlane(world, overlapPlane, 1, 1);
        var actorId = new EntityId("actor");
        var overlapBagId = new EntityId("overlapBag");
        var actorSource = new PlaneCoord(actorPlane, new GridCoord(0, 0));
        var bagSource = new PlaneCoord(overlapPlane, new GridCoord(0, 0));
        AddEntity(world, actorId, "Actor", actorSource);
        AddEntity(world, overlapBagId, "Overlap Bag", bagSource);
        var actorCell = new TopologyVisibleCellProjection(
            new TopologyCellRef(actorSource),
            0,
            null,
            null,
            null,
            new TopologyNodeId("actor-room:0,0"),
            new TopologyLayoutCoord(new GridCoord(0, 0)));
        var bagCell = new TopologyVisibleCellProjection(
            new TopologyCellRef(bagSource),
            99,
            null,
            null,
            null,
            new TopologyNodeId("overlap-room:0,0"),
            new TopologyLayoutCoord(new GridCoord(0, 0)));
        var session = new PlayableScenarioSession(
            "overlap",
            "Overlap",
            world,
            new PrototypeContentRegistry(new Dictionary<EntityTemplateId, EntityTemplate>(), new Dictionary<ActionPlanTemplateId, ActionPlanDescriptor>(), new Dictionary<EntityTemplateId, EntityPresentation>()),
            new Dictionary<EntityId, IEntityActionPlan>(),
            actorId,
            actorPlane,
            actorId,
            CanPlay: true,
            [],
            [],
            []);
        var projection = new TopologyVisibilityProjection(actorId, actorCell.Cell, 0, [actorCell], [], [actorCell, bagCell], []);

        var grid = PlayGridViewModel.FromSession(session, TilesetProfileLoader.LoadCandii(), topologyVisibility: projection);

        var displayed = grid.CellAt(0, 0);
        Assert.Equal(actorId, displayed.EntityId);
        Assert.True(displayed.IsInPointOfView);
        Assert.Equal(new GridCoord(0, 0), grid.ControlledEntityCoord);
    }

    [Fact]
    public void PlayGridViewModelNormalizesNegativeTopologyLayoutCoordinatesForRendering()
    {
        var world = new WorldState();
        var plane = new PlaneId("north-room");
        AddPlane(world, plane, 2, 1);
        var actorId = new EntityId("actor");
        var actorSource = new PlaneCoord(plane, new GridCoord(0, 0));
        var eastSource = new PlaneCoord(plane, new GridCoord(1, 0));
        AddEntity(world, actorId, "Actor", actorSource);
        var actorCell = new TopologyVisibleCellProjection(
            new TopologyCellRef(actorSource),
            0,
            null,
            null,
            null,
            new TopologyNodeId("north-room:0,0"),
            new TopologyLayoutCoord(new GridCoord(-4, -6)));
        var eastCell = new TopologyVisibleCellProjection(
            new TopologyCellRef(eastSource),
            1,
            actorCell.Cell,
            Direction.East,
            TopologyEdgeKind.DefaultGrid,
            new TopologyNodeId("north-room:1,0"),
            new TopologyLayoutCoord(new GridCoord(-3, -6)));
        var session = new PlayableScenarioSession(
            "negative-layout",
            "Negative Layout",
            world,
            new PrototypeContentRegistry(new Dictionary<EntityTemplateId, EntityTemplate>(), new Dictionary<ActionPlanTemplateId, ActionPlanDescriptor>(), new Dictionary<EntityTemplateId, EntityPresentation>()),
            new Dictionary<EntityId, IEntityActionPlan>(),
            actorId,
            plane,
            actorId,
            CanPlay: true,
            [],
            [],
            []);
        var projection = new TopologyVisibilityProjection(actorId, actorCell.Cell, 1, [actorCell, eastCell], [], [actorCell, eastCell], []);

        var grid = PlayGridViewModel.FromSession(session, TilesetProfileLoader.LoadCandii(), topologyVisibility: projection);

        Assert.Equal(2, grid.Width);
        Assert.Equal(1, grid.Height);
        Assert.Equal(new GridCoord(0, 0), grid.ControlledEntityCoord);
        Assert.Equal(new GridCoord(0, 0), grid.TryDisplayCoordForSource(actorSource));
        Assert.Equal(new GridCoord(1, 0), grid.TryDisplayCoordForSource(eastSource));
        Assert.Equal(new TopologyLayoutCoord(new GridCoord(-4, -6)), grid.CellAt(0, 0).LayoutCoord);
    }

    [Fact]
    public void PlayGridSurfacePresenterIdentifiesPreviouslyDrawnCellsMissingFromSparseTopologyModel()
    {
        var previous = new HashSet<(int X, int Y)> { (10, 10), (11, 10) };
        var model = new PlayGridViewModel(
            "Sparse",
            Width: 2,
            Height: 1,
            [new PlayCellVisual(1, 0, 223, global::SadRogue.Primitives.Color.Gray, global::SadRogue.Primitives.Color.Black)],
            new EntityId("player"),
            new GridCoord(1, 0),
            new PlaneId("plane"),
            null,
            []);

        var stale = PlayGridSurfacePresenter.ResolveStaleDrawnCoordinatesForSparseModel(
            previous,
            new FrontendRect(9, 10, 2, 1),
            model);

        Assert.Equal([(11, 10)], stale);
    }

    private static void MoveEntity(GameGameGame.Core.WorldState world, EntityId entityId, PlaneCoord destination)
    {
        var entity = world.Entities[entityId];
        world.Occupancy.Remove(entity.OccupiedNodeId);
        var nodeId = world.GetNodeId(destination);
        world.Occupancy.Add(nodeId, entityId);
        world.Entities[entityId] = entity with { OccupiedNodeId = nodeId };
    }

    private static PlayableScenarioSession CreateTwoActorSession(EntityId playerId, EntityId controlledId)
    {
        var planeId = new PlaneId("room");
        var world = new WorldState();
        AddPlane(world, planeId, 2, 1);
        AddEntity(world, playerId, "Player", new PlaneCoord(planeId, new GridCoord(0, 0)));
        AddEntity(world, controlledId, "Actor", new PlaneCoord(planeId, new GridCoord(1, 0)));

        return CreateSession("retargeted", world, playerId, planeId, playerId);
    }

    private static PlayableScenarioSession CreateSession(
        string scenarioId,
        WorldState world,
        EntityId playerId,
        PlaneId activePlaneId,
        EntityId activeContainerEntityId) =>
        new(
            scenarioId,
            scenarioId,
            world,
            new PrototypeContentRegistry(new Dictionary<EntityTemplateId, EntityTemplate>(), new Dictionary<ActionPlanTemplateId, ActionPlanDescriptor>(), new Dictionary<EntityTemplateId, EntityPresentation>()),
            new Dictionary<EntityId, IEntityActionPlan>(),
            playerId,
            activePlaneId,
            activeContainerEntityId,
            CanPlay: true,
            [],
            [],
            []);

    private static void AssertNoLayoutCell(PlayGridViewModel grid, GridCoord layoutCoord) =>
        Assert.DoesNotContain(grid.Cells, cell => cell.LayoutCoord == new TopologyLayoutCoord(layoutCoord));

    private static void AddPlane(WorldState world, PlaneId planeId, int width, int height)
    {
        world.Planes.Add(planeId, new Plane(planeId, planeId.Value, width, height));
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                world.AddNode(planeId, new GridCoord(x, y));
            }
        }
    }

    private static void AddEntity(WorldState world, EntityId entityId, string name, PlaneCoord location)
    {
        var nodeId = world.GetNodeId(location);
        world.Entities.Add(entityId, new Entity(entityId, name, nodeId, InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 1));
        world.Occupancy.Add(nodeId, entityId);
    }
}
