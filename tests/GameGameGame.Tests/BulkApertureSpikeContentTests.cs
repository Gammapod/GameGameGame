using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.Headless;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class BulkApertureSpikeContentTests
{
    [Fact]
    public void BulkApertureSpikeContentValidatesAndRunsPickupComparison()
    {
        var document = EditableContentDocument.LoadYaml(File.ReadAllText(FindRepositoryFile(Path.Combine("src", "GameGameGame.Content", "BulkApertureSpike.yaml"))));

        var validation = document.ToRegistry().Validate();
        var canonicalValidation = document.ValidateCanonicalAuthoring();
        var run = ScenarioRunService.Run(document, new PersistedScenarioRunRequest("aperture-spike-pickup", TurnCount: 1));

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        Assert.True(canonicalValidation.IsValid, string.Join(Environment.NewLine, canonicalValidation.Errors));
        Assert.Empty(run.RuntimeFailures);
        Assert.Equal([new EntityId("smallCollector"), new EntityId("narrowCollector")], run.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(run.Turns[0].TraceLines, line => line.StartsWith("1. MoveFacing: Failure", StringComparison.Ordinal));
        Assert.Contains(run.Turns[0].TraceLines, line => line.StartsWith("2. PickupTarget: Success", StringComparison.Ordinal));
        Assert.Contains(run.Turns[1].TraceLines, line => line.StartsWith("2. PickupTarget: Failure; reason=ApertureBlocked", StringComparison.Ordinal));
        Assert.Contains("Small Collector inventory:", run.InventorySummaryLines);
        Assert.Contains("  - Small Gem smallGem at (0,0)", run.InventorySummaryLines);
        Assert.Contains("Large Stone: scenarioRoot(5,1), facing none, target none", run.FinalStateLines);
    }

    private static string FindRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return Path.GetFullPath(relativePath);
    }
}
