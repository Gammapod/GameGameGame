using System.Runtime.CompilerServices;

namespace GameGameGame.Documentation.Tests;

public sealed class DefaultFrontendWorkflowTests
{
    [Fact]
    public void DefaultSolutionExcludesLegacySadConsoleProjectsAndIncludesNewFrontendProjects()
    {
        var solution = File.ReadAllText(Path.Combine(RepositoryRoot(), "GameGameGame.sln"));

        Assert.Contains("src\\GameGameGame.Frontend.SadConsole\\GameGameGame.Frontend.SadConsole.csproj", solution, StringComparison.Ordinal);
        Assert.Contains("tests\\GameGameGame.Frontend.SadConsole.Tests\\GameGameGame.Frontend.SadConsole.Tests.csproj", solution, StringComparison.Ordinal);
        Assert.DoesNotContain("src\\GameGameGame.SadConsole\\GameGameGame.SadConsole.csproj", solution, StringComparison.Ordinal);
        Assert.DoesNotContain("tests\\GameGameGame.SadConsole.Tests\\GameGameGame.SadConsole.Tests.csproj", solution, StringComparison.Ordinal);
    }

    [Fact]
    public void FeedbackPackagePublishesNewFrontendAndLaunchInstructionsNameItsExecutable()
    {
        var script = File.ReadAllText(Path.Combine(RepositoryRoot(), "tools", "package-feedback-build.ps1"));

        Assert.Contains("src/GameGameGame.Frontend.SadConsole/GameGameGame.Frontend.SadConsole.csproj", script, StringComparison.Ordinal);
        Assert.Contains("Launch GameGameGame.Frontend.SadConsole.exe", script, StringComparison.Ordinal);
        Assert.DoesNotContain("src/GameGameGame.SadConsole/GameGameGame.SadConsole.csproj", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Launch GameGameGame.exe", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadmeDocumentsNewFrontendAsNormalRunTargetWithoutLegacyReferences()
    {
        var readme = File.ReadAllText(Path.Combine(RepositoryRoot(), "README.md"));

        Assert.Contains("src/GameGameGame.Frontend.SadConsole", readme, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project src/GameGameGame.Frontend.SadConsole/GameGameGame.Frontend.SadConsole.csproj", readme, StringComparison.Ordinal);
        Assert.DoesNotContain("src/GameGameGame.SadConsole", readme, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet run --project src/GameGameGame.SadConsole/GameGameGame.SadConsole.csproj", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void SourceOfTruthDocsDoNotClaimCoverageFromRemovedLegacySadConsoleTests()
    {
        var sourceOfTruth = Path.Combine(RepositoryRoot(), "docs", "Source of Truth");
        var documents = Directory.EnumerateFiles(sourceOfTruth, "*.md");
        var combined = string.Join(Environment.NewLine, documents.Select(File.ReadAllText));

        Assert.DoesNotContain("SadConsoleEditorContextTests", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("SadConsoleScenarioSelectionScreenTests", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("SadConsoleUiComponentLibraryTests", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("SadConsoleComponentGalleryTests", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("tests/GameGameGame.SadConsole.Tests", combined, StringComparison.Ordinal);
    }

    private static string RepositoryRoot([CallerFilePath] string sourcePath = "")
    {
        var directory = Path.GetDirectoryName(sourcePath)
            ?? throw new InvalidOperationException("Test source path did not include a directory.");

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "GameGameGame.sln")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException($"Could not find repository root from {sourcePath}.");
    }
}
