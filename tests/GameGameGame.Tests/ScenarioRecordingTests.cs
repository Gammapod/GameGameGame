using GameGameGame.Content;
using GameGameGame.Headless;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Headless)]
public sealed class ScenarioRecordingTests : IDisposable
{
    private readonly string tempRoot = Path.Combine(Path.GetTempPath(), $"GameGameGameScenarioRecordingTests-{Guid.NewGuid():N}");

    [Fact]
    public void ScenarioRecordingServiceRecordsPersistedScenarioInitialStateAndFullTurns()
    {
        var document = OpenBetaContent("CanonicalActions", "CanonicalTargetPathMovementShowcase.yaml");
        var outputDirectory = CreateOutputDirectory();

        var report = ScenarioRecordingService.Record(document, new ScenarioRecordingRequest(
            ScenarioId: "beta-canonical-target-path-maze",
            TurnCount: 2,
            OutputDirectory: outputDirectory));

        Assert.Equal("beta-canonical-target-path-maze", report.ScenarioId);
        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal(3, report.Frames.Count);
        Assert.Equal([0, 1, 2], report.Frames.Select(frame => frame.TurnNumber).ToArray());
        Assert.Equal([0, 1, 2], report.Frames.Select(frame => frame.FrameIndex).ToArray());
        Assert.All(report.Frames, frame =>
        {
            Assert.EndsWith(".png", frame.PngPath, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(outputDirectory, frame.PngPath, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(frame.PngPath), $"Expected PNG frame at {frame.PngPath}.");
        });
        Assert.NotNull(report.GifPath);
        Assert.EndsWith(".gif", report.GifPath!, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(outputDirectory, report.GifPath!, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(report.GifPath!), $"Expected GIF recording at {report.GifPath}.");
    }

    [Fact]
    public void ScenarioRecordingServiceReportsAuthoringDiagnosticsWithoutArtifacts()
    {
        var document = OpenBetaContent("CanonicalActions", "CanonicalTargetPathMovementShowcase.yaml");
        var outputDirectory = CreateOutputDirectory();

        var report = ScenarioRecordingService.Record(document, new ScenarioRecordingRequest(
            ScenarioId: "missing-scenario",
            TurnCount: 1,
            OutputDirectory: outputDirectory));

        Assert.Equal("missing-scenario", report.ScenarioId);
        Assert.Contains(report.ValidationDiagnostics, diagnostic => diagnostic.Contains("missing-scenario", StringComparison.Ordinal));
        Assert.Empty(report.RuntimeFailures);
        Assert.Empty(report.Frames);
        Assert.Null(report.GifPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private string CreateOutputDirectory()
    {
        var outputDirectory = Path.Combine(tempRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        return outputDirectory;
    }

    private static EditableContentDocument OpenBetaContent(string group, string fileName)
    {
        var path = FindRepositoryFile(Path.Combine("src", "GameGameGame.Content", "Beta", group, fileName));
        return EditableContentDocument.LoadYaml(File.ReadAllText(path));
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
