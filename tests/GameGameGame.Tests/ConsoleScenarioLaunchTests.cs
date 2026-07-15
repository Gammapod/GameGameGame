using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Console)]
public sealed class ConsoleScenarioLaunchTests
{
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
}
