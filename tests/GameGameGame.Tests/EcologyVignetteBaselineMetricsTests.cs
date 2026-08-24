using GameGameGame.Content;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class EcologyVignetteBaselineMetricsTests
{
    [Theory]
    [MemberData(nameof(BaselineCases))]
    public void EcologyVignettesHaveKnownAutonomousHeadlessPopulationBaseline(
        string scenarioId,
        int turnCount,
        IReadOnlyDictionary<string, int> expectedCounts,
        int expectedRuntimeObservations)
    {
        var document = LoadEcologyDocumentForAutonomousAnalysis();

        var report = ScenarioRunService.Run(document, new PersistedScenarioRunRequest(scenarioId, turnCount));
        var actualCounts = CountNamedEntities(report);

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Empty(report.CapabilityGaps);
        Assert.Equal(expectedRuntimeObservations, report.RuntimeObservations.Count);
        Assert.Equal(expectedCounts, actualCounts);
    }

    public static IEnumerable<object[]> BaselineCases()
    {
        yield return ["ecology-glowcap-grubarium", 10, Counts(("Ecology Observer", 1), ("Glowcap Fungus", 3), ("Glowcap Spore", 18), ("Cave Grub", 2), ("Duskwing Bat", 1)), 41];
        yield return ["ecology-glowcap-grubarium", 25, Counts(("Ecology Observer", 1), ("Glowcap Fungus", 3), ("Glowcap Spore", 21), ("Duskwing Bat", 1)), 112];
        yield return ["ecology-glowcap-grubarium", 50, Counts(("Ecology Observer", 1), ("Glowcap Fungus", 3), ("Glowcap Spore", 21), ("Duskwing Bat", 1)), 237];

        yield return ["ecology-mana-crystal-automata", 10, Counts(("Ecology Observer", 1), ("Mana Crystal", 2), ("Mana Spark", 14), ("Tiny Construct", 2), ("Mana Leech", 2)), 35];
        yield return ["ecology-mana-crystal-automata", 25, Counts(("Ecology Observer", 1), ("Mana Crystal", 2), ("Mana Spark", 14), ("Tiny Construct", 2), ("Mana Leech", 2)), 125];
        yield return ["ecology-mana-crystal-automata", 50, Counts(("Ecology Observer", 1), ("Mana Crystal", 2), ("Mana Spark", 14), ("Tiny Construct", 2), ("Mana Leech", 2)), 275];

        yield return ["ecology-goblin-coin-table", 10, Counts(("Ecology Observer", 1), ("Coin Fountain", 2), ("Coin", 13), ("Coin Goblin", 2), ("Goblin Thief", 1)), 17];
        yield return ["ecology-goblin-coin-table", 25, Counts(("Ecology Observer", 1), ("Coin Fountain", 2), ("Coin", 13), ("Coin Goblin", 2), ("Goblin Thief", 1)), 64];
        yield return ["ecology-goblin-coin-table", 50, Counts(("Ecology Observer", 1), ("Coin Fountain", 2), ("Coin", 13), ("Coin Goblin", 2), ("Goblin Thief", 1)), 139];
    }

    private static EditableContentDocument LoadEcologyDocumentForAutonomousAnalysis()
    {
        var contentPath = Path.Combine(FindRepositoryRoot(), "src", "GameGameGame.Content", "Beta", "Ecology", "EcologyVignettes.yaml");
        var document = EditableContentDocument.LoadYaml(File.ReadAllText(contentPath));

        // The authored gallery rooms include an observer with Player control for manual/SadConsole play.
        // Autonomous population metrics clear that controller in-memory so headless runs do not stop at PlayerChoice on turn 1.
        foreach (var roomId in new[] { "glowcapGrubariumRoom", "manaAutomataRoom", "goblinCoinTableRoom" })
        {
            foreach (var carried in document.EntityTemplates[roomId].CarriedEntities ?? [])
            {
                carried.Controller = null;
            }
        }

        return document;
    }

    private static IReadOnlyDictionary<string, int> CountNamedEntities(GameGameGame.Content.ScenarioRunReport report)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var knownNames = BaselineCases()
            .SelectMany(testCase => ((IReadOnlyDictionary<string, int>)testCase[2]).Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(name => name.Length)
            .ToList();

        foreach (var line in report.FinalStateLines)
        {
            AddKnownName(line, knownNames, counts);
        }

        foreach (var line in report.InventorySummaryLines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                AddKnownName(trimmed[2..], knownNames, counts);
            }
        }

        return counts;
    }

    private static void AddKnownName(string line, IReadOnlyList<string> knownNames, Dictionary<string, int> counts)
    {
        var name = knownNames.FirstOrDefault(name =>
            line.StartsWith(name + ":", StringComparison.Ordinal)
            || line.StartsWith(name + " ", StringComparison.Ordinal));
        if (name is null)
        {
            return;
        }

        counts[name] = counts.GetValueOrDefault(name) + 1;
    }

    private static IReadOnlyDictionary<string, int> Counts(params (string Name, int Count)[] entries) =>
        entries.ToDictionary(entry => entry.Name, entry => entry.Count, StringComparer.Ordinal);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "src", "GameGameGame.Content")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test working directory.");
    }
}
