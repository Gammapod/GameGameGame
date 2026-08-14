using System.Runtime.CompilerServices;
using GameGameGame.Content;

namespace GameGameGame.Frontend.SadConsole.Tests;

internal static class TestRepository
{
    public static string Root([CallerFilePath] string sourcePath = "")
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

    public static WorkspaceScenarioCatalogResult BuildDefaultCatalog([CallerFilePath] string sourcePath = "") =>
        WorkspaceScenarioCatalogService.BuildCatalog(new WorkspaceScenarioCatalogOptions(RepositoryRoot: Root(sourcePath)));

    public static WorkspaceScenarioCatalogResult BuildDebugRoomCatalog([CallerFilePath] string sourcePath = "") =>
        WorkspaceScenarioCatalogService.BuildCatalog(new WorkspaceScenarioCatalogOptions(
            IncludeSingleFileCompatibilityScenarios: false,
            RepositoryRoot: Root(sourcePath)));
}
