using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Console)]
public sealed class ConsoleScenarioLaunchTests
{
    private static readonly string FeedbackManifestPath = FindRepositoryPath(
        "src",
        "GameGameGame.Content",
        "Beta",
        "FeedbackManifest.yaml");

    [Fact]
    public void ScenarioCatalogListsScenariosFromDocument()
    {
        var document = CreateConsoleDocument("catalog-one", "Catalog One", new GridCoord(0, 0));

        var catalog = ScenarioCatalog.BuildFromDocument("Scenarios/One.yaml", document);

        var entry = Assert.Single(catalog.Entries);
        Assert.Equal("Scenarios/One.yaml", entry.ContentPath);
        Assert.Equal("catalog-one", entry.ScenarioId);
        Assert.Equal("Catalog One", entry.Name);
        Assert.Empty(catalog.Diagnostics);
    }

    [Fact]
    public void ScenarioCatalogDiscoversFolderAndRoundTripsManifest()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var firstPath = Path.Combine(directory, "First.yaml");
            var nestedDirectory = Path.Combine(directory, "Nested");
            Directory.CreateDirectory(nestedDirectory);
            var secondPath = Path.Combine(nestedDirectory, "Second.yaml");
            var manifestPath = Path.Combine(directory, ScenarioCatalog.ManifestFileName);
            File.WriteAllText(firstPath, CreateConsoleDocument("catalog-first", "Catalog First", new GridCoord(0, 0)).SaveYaml());
            File.WriteAllText(secondPath, CreateConsoleDocument("catalog-second", "Catalog Second", new GridCoord(1, 0)).SaveYaml());

            var discovered = ScenarioCatalog.DiscoverFolder(directory);
            var loaded = ScenarioCatalog.LoadManifest(manifestPath);

            Assert.Equal(
                ["catalog-first", "catalog-second"],
                discovered.Entries.Select(entry => entry.ScenarioId).Order().ToArray());
            Assert.True(File.Exists(manifestPath));
            Assert.Equal(discovered.Entries.OrderBy(entry => entry.ScenarioId).ToArray(), loaded.Entries.OrderBy(entry => entry.ScenarioId).ToArray());
            Assert.Empty(discovered.Diagnostics);
            Assert.Empty(loaded.Diagnostics);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ScenarioCatalogPreservesManifestDescriptionsWhenRediscoveringFolder()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var contentPath = Path.Combine(directory, "Described.yaml");
            var manifestPath = Path.Combine(directory, ScenarioCatalog.ManifestFileName);
            File.WriteAllText(contentPath, CreateConsoleDocument("catalog-described", "Catalog Described", new GridCoord(0, 0)).SaveYaml());
            ScenarioCatalog.SaveManifest(
                new ScenarioCatalogResult(
                    [new ScenarioCatalogEntry(contentPath, "catalog-described", "Catalog Described", "Shows the described scenario.")],
                    []),
                manifestPath);

            var discovered = ScenarioCatalog.DiscoverFolder(directory);
            var loaded = ScenarioCatalog.LoadManifest(manifestPath);

            Assert.Equal("Shows the described scenario.", Assert.Single(discovered.Entries).Description);
            Assert.Equal("Shows the described scenario.", Assert.Single(loaded.Entries).Description);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ScenarioCatalogLoadsCuratedManifestSectionsAndEntryMetadata()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var deltaPath = Path.Combine(directory, "Delta.yaml");
            var legacyPath = Path.Combine(directory, "Legacy.yaml");
            var manifestPath = Path.Combine(directory, ScenarioCatalog.ManifestFileName);
            File.WriteAllText(deltaPath, CreateConsoleDocument("delta-canonical-move-outcomes", "Delta Canonical Move Outcomes", new GridCoord(0, 0)).SaveYaml());
            File.WriteAllText(legacyPath, CreateConsoleDocument("legacy-beta-targeting-acquire-target", "Legacy Beta Targeting Acquire Target", new GridCoord(1, 0)).SaveYaml());
            File.WriteAllText(manifestPath, """
                sections:
                - id: delta
                  name: Delta
                  description: Vertical-slice requirements scenarios.
                  entries:
                  - contentPath: Delta.yaml
                    scenarioId: delta-canonical-move-outcomes
                    name: Delta Canonical Move Outcomes
                    description: Demonstrates canonical move reviewer outcomes; authored for Delta vertical-slice review with no known caveats.
                    status: active-delta
                    tags: [canonical, move]
                    source: Canonical action vertical slice.
                - id: legacy
                  name: Legacy Beta
                  description: Legacy/prototype behavior explorations.
                  entries:
                  - contentPath: Legacy.yaml
                    scenarioId: legacy-beta-targeting-acquire-target
                    name: Legacy Beta Targeting Acquire Target
                    description: Preserves the prototype acquire-target exploration; legacy status with known prototype Action Step caveats.
                    status: legacy
                    tags: [legacy, targeting]
                """);

            var catalog = ScenarioCatalog.LoadManifest(manifestPath);

            Assert.NotNull(catalog.Sections);
            var sections = catalog.Sections!;
            Assert.Equal(["delta", "legacy"], sections.Select(section => section.Id).ToArray());
            Assert.Equal(["delta-canonical-move-outcomes", "legacy-beta-targeting-acquire-target"], catalog.Entries.Select(entry => entry.ScenarioId).ToArray());
            var delta = catalog.Entries[0];
            Assert.Equal(deltaPath, delta.ContentPath);
            Assert.Equal("active-delta", delta.Status);
            Assert.Equal(["canonical", "move"], delta.Tags);
            Assert.Equal("Canonical action vertical slice.", delta.Source);
            Assert.Empty(catalog.Diagnostics);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ScenarioCatalogValidationReportsCuratedManifestIssuesAndUnclassifiedCandidates()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var classifiedPath = Path.Combine(directory, "Classified.yaml");
            var unclassifiedPath = Path.Combine(directory, "Unclassified.yaml");
            var manifestPath = Path.Combine(directory, ScenarioCatalog.ManifestFileName);
            File.WriteAllText(classifiedPath, CreateConsoleDocument("delta-canonical-move-outcomes", "Delta Canonical Move Outcomes", new GridCoord(0, 0)).SaveYaml());
            File.WriteAllText(unclassifiedPath, CreateConsoleDocument("user-new-room", "User New Room", new GridCoord(1, 0)).SaveYaml());
            File.WriteAllText(manifestPath, """
                sections:
                - id: legacy
                  name: Legacy Beta
                  entries:
                  - contentPath: Classified.yaml
                    scenarioId: delta-canonical-move-outcomes
                    name: Delta Canonical Move Outcomes
                    status: active-delta
                - id: delta
                  name: Delta
                  entries:
                  - contentPath: Classified.yaml
                    scenarioId: delta-canonical-move-outcomes
                    name: Duplicate Delta Canonical Move Outcomes
                    description: Duplicate entry to prove duplicate scenario ID validation.
                    status: active-delta
                """);

            var validation = ScenarioCatalog.ValidateManifest(manifestPath, directory);

            Assert.False(validation.IsValid);
            Assert.Contains(validation.Diagnostics, diagnostic => diagnostic.Contains("requires a description", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(validation.Diagnostics, diagnostic => diagnostic.Contains("appears more than once", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(validation.Diagnostics, diagnostic => diagnostic.Contains("status active-delta does not belong in section legacy", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(validation.Diagnostics, diagnostic => diagnostic.Contains("unclassified scenario user-new-room", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void FeedbackScenarioManifestContainsOnlyTesterFacingScenarios()
    {
        var validation = ScenarioCatalog.ValidateManifest(FeedbackManifestPath);
        var catalog = ScenarioCatalog.LoadManifest(FeedbackManifestPath);
        var forbiddenStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "legacy",
            "user"
        };
        var forbiddenTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "debug",
            "logs",
            "log-testing",
            "validation",
            "legacy",
            "user-generated"
        };

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Diagnostics));
        Assert.NotEmpty(catalog.Entries);
        Assert.All(catalog.Sections ?? [], section => Assert.Contains(section.Id, new[] { "delta", "canonical" }));
        Assert.All(catalog.Entries, entry =>
        {
            Assert.False(forbiddenStatuses.Contains(entry.Status ?? string.Empty), $"{entry.ScenarioId} has feedback-forbidden status {entry.Status}.");
            Assert.DoesNotContain(entry.Tags ?? [], tag => forbiddenTags.Contains(tag));
        });
    }

    [Fact]
    public void FeedbackScenarioManifestEntriesLaunchAsPlayableSessions()
    {
        var catalog = ScenarioCatalog.LoadManifest(FeedbackManifestPath);

        Assert.Empty(catalog.Diagnostics);
        Assert.NotEmpty(catalog.Entries);
        foreach (var entry in catalog.Entries)
        {
            var session = PlayableScenarioLauncher.CreateFromCatalogEntry(entry);

            Assert.True(session.CanPlay, $"{entry.ScenarioId} should be playable.");
            Assert.Empty(session.ValidationDiagnostics);
            Assert.Empty(session.RuntimeFailures);
            Assert.Empty(session.CapabilityGaps);
        }
    }

    [Fact]
    public void ScenarioCatalogScanServiceDiscoversFolderAndWritesManifest()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var contentPath = Path.Combine(directory, "Scanned.yaml");
            var manifestPath = Path.Combine(directory, "ScannedManifest.yaml");
            File.WriteAllText(contentPath, CreateConsoleDocument("catalog-scanned", "Catalog Scanned", new GridCoord(0, 0)).SaveYaml());

            var result = ScenarioCatalogScanService.Scan(directory, manifestPath);

            Assert.Equal(directory, result.FolderPath);
            Assert.Equal(manifestPath, result.OutputPath);
            Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics));
            Assert.Equal(1, result.EntryCount);
            Assert.Contains($"Wrote 1 scenario entries to {manifestPath}.", result.Messages);
            Assert.True(File.Exists(manifestPath));
            Assert.Equal("catalog-scanned", Assert.Single(ScenarioCatalog.LoadManifest(manifestPath).Entries).ScenarioId);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ScenarioCatalogRebasesRepositoryManifestPathsWhenLoadedFromPackagedContent()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var nestedDirectory = Path.Combine(directory, "CurrentTools");
            Directory.CreateDirectory(nestedDirectory);
            var contentPath = Path.Combine(nestedDirectory, "Packaged.yaml");
            var manifestPath = Path.Combine(directory, ScenarioCatalog.ManifestFileName);
            File.WriteAllText(contentPath, CreateConsoleDocument("catalog-packaged", "Catalog Packaged", new GridCoord(0, 0)).SaveYaml());
            File.WriteAllText(manifestPath, """
                scenarios:
                - contentPath: src\GameGameGame.Content\Beta\CurrentTools\Packaged.yaml
                  scenarioId: catalog-packaged
                  name: Catalog Packaged
                """);

            var entry = Assert.Single(ScenarioCatalog.LoadManifest(manifestPath).Entries);

            Assert.Equal(contentPath, entry.ContentPath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void PlayableScenarioLauncherBuildsFrontendNeutralSessionFromPersistedScenario()
    {
        var document = CreateConsoleDocument("playable-alpha", "Playable Alpha", new GridCoord(1, 0));

        var session = PlayableScenarioLauncher.CreateFromDocument(document, "playable-alpha");

        Assert.Equal("playable-alpha", session.ScenarioId);
        Assert.Equal("Playable Alpha", session.Name);
        Assert.Equal(new EntityId("playable-alpha-player"), session.PlayerEntityId);
        Assert.Equal([new EntityId("playable-alpha-player")], session.PlayerControls["player-1"]);
        Assert.Equal(new PlaneId("scenarioRoot"), session.ActivePlaneId);
        Assert.Equal(new EntityId("scenarioRoot"), session.ActiveContainerEntityId);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(1, 0)), session.World.GetEntityLocation(session.PlayerEntityId));
        Assert.NotNull(session.World.GetInventoryPlaneId(session.PlayerEntityId));
        Assert.True(session.CanPlay);
        Assert.Empty(session.ValidationDiagnostics);
        Assert.Empty(session.RuntimeFailures);
        Assert.Empty(session.CapabilityGaps);
    }

    [Fact]
    public void PlayableScenarioLauncherBuildsFreshSessionFromCatalogEntry()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "PlayableCatalogLaunch.yaml");
            File.WriteAllText(path, CreateConsoleDocument("playable-catalog-launch", "Playable Catalog Launch", new GridCoord(0, 0)).SaveYaml());
            var entry = ScenarioCatalog.DiscoverFolder(directory).Entries.Single();

            var firstSession = PlayableScenarioLauncher.CreateFromCatalogEntry(entry);
            var movement = new MovementService();
            new TurnService(movement, firstSession.ActionPlans).TakeActorTurnThenAdvance(
                firstSession.World,
                firstSession.PlayerEntityId,
                PlannedActionPlan.Single(new MoveAction(Direction.East)));
            var secondSession = PlayableScenarioLauncher.CreateFromCatalogEntry(entry);

            Assert.Equal(new PlaneCoord(secondSession.ActivePlaneId, new GridCoord(0, 0)), secondSession.World.GetEntityLocation(secondSession.PlayerEntityId));
            Assert.NotEqual(firstSession.World.GetEntityLocation(firstSession.PlayerEntityId), secondSession.World.GetEntityLocation(secondSession.PlayerEntityId));
            Assert.Equal(entry.ScenarioId, secondSession.ScenarioId);
            Assert.Equal(entry.Name, secondSession.Name);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void PlayableScenarioLauncherFocusesAuthoredPlayerControllerWhenNoLegacyPlayerIsInserted()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var roomId = editor.CreateEntityPreset("Controller Launch Room");
        editor.UpdateEntityPreset(
            roomId,
            new EntityTemplate("Controller Launch Room", InventoryWidth: 2, InventoryHeight: 1, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        var actorId = editor.CreateEntityPreset("Controller Launch Actor");
        editor.UpdateEntityPreset(
            actorId,
            new EntityTemplate("Controller Launch Actor", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 5),
            new EntityPresentation('@', PresentationColor.Yellow));
        editor.PlaceCarriedEntity(roomId, new EntityId("authoredPlayer"), actorId, new GridCoord(0, 0));
        editor.SetCarriedEntityController(roomId, new EntityId("authoredPlayer"), EntityController.Player);
        editor.UpsertScenario(new ScenarioDefinition(
            "controller-launch",
            "Controller Launch",
            roomId,
            PlayerEntityTemplateId: null,
            PlayerEntityId: null,
            PlayerStart: null));

        var session = PlayableScenarioLauncher.CreateFromDocument(document, "controller-launch");

        Assert.Equal(new EntityId("authoredPlayer"), session.PlayerEntityId);
        Assert.Equal([new EntityId("authoredPlayer")], session.PlayerControls["player-1"]);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(0, 0)), session.World.GetEntityLocation(session.PlayerEntityId));
        Assert.Empty(session.ValidationDiagnostics);
        Assert.True(session.CanPlay);
    }

    [Fact]
    public void PlayableScenarioLauncherIgnoresSkippedLegacyPlayerIdWhenAuthoredControllerExists()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var roomId = editor.CreateEntityPreset("Skipped Legacy Room");
        editor.UpdateEntityPreset(
            roomId,
            new EntityTemplate("Skipped Legacy Room", InventoryWidth: 2, InventoryHeight: 1, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        var actorId = editor.CreateEntityPreset("Authored Controlled Actor");
        editor.UpdateEntityPreset(
            actorId,
            new EntityTemplate("Authored Controlled Actor", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 5),
            new EntityPresentation('@', PresentationColor.Yellow));
        var legacyPlayerId = editor.CreateEntityPreset("Skipped Legacy Player");
        editor.PlaceCarriedEntity(roomId, new EntityId("authoredPlayer"), actorId, new GridCoord(0, 0));
        editor.SetCarriedEntityController(roomId, new EntityId("authoredPlayer"), EntityController.Player);
        editor.UpsertScenario(new ScenarioDefinition(
            "skipped-legacy-controller-launch",
            "Skipped Legacy Controller Launch",
            roomId,
            legacyPlayerId,
            new EntityId("legacyPlayer"),
            new GridCoord(1, 0)));

        var session = PlayableScenarioLauncher.CreateFromDocument(document, "skipped-legacy-controller-launch");

        Assert.False(session.World.Entities.ContainsKey(new EntityId("legacyPlayer")));
        Assert.Equal(new EntityId("authoredPlayer"), session.PlayerEntityId);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(0, 0)), session.World.GetEntityLocation(session.PlayerEntityId));
    }

    private static EditableContentDocument CreateConsoleDocument(string scenarioId, string scenarioName, GridCoord playerStart)
    {
        var document = new EditableContentDocument();
        var roomId = document.AddEntityTemplate(
            $"{scenarioName} Room",
            new EntityTemplate($"{scenarioName} Room", InventoryWidth: 3, InventoryHeight: 2, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        var playerTemplateId = document.AddEntityTemplate(
            $"{scenarioName} Player",
            new EntityTemplate(
                $"{scenarioName} Player",
                InventoryWidth: 1,
                InventoryHeight: 1,
                Bulk: 1,
                Aperture: 5,
                ActionStateDefaults: new ActorActionStateDefaults(Direction.East)),
            new EntityPresentation('@', PresentationColor.Yellow));
        document.UpsertScenario(new ScenarioDefinition(
            scenarioId,
            scenarioName,
            roomId,
            playerTemplateId,
            new EntityId($"{scenarioId}-player"),
            playerStart));

        return document;
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ggg-scenario-catalog-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string FindRepositoryPath(params string[] relativeSegments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GameGameGame.sln")))
            {
                return Path.Combine([directory.FullName, .. relativeSegments]);
            }

            directory = directory.Parent;
        }

        return Path.Combine(relativeSegments);
    }
}
