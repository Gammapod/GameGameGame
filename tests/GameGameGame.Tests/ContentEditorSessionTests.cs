using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

public sealed class ContentEditorSessionTests
{
    [Fact]
    public void OpenFileCreatesCleanSessionWithDocumentAndPath()
    {
        var path = WriteTempContentFile(
            """
            entityTemplates:
              rock:
                name: Rock
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 3
            presentations:
              rock:
                glyph: '*'
                color: Earth
            actionPlans: {}
            """);

        try
        {
            var result = ContentEditorSession.OpenFile(path);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            var session = result.Session!;
            Assert.Equal(path, session.FilePath);
            Assert.False(session.IsDirty);
            Assert.Equal("Rock", session.Document.ToRegistry().EntityTemplates[new EntityTemplateId("rock")].Name);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SaveWritesReloadableYamlAndClearsDirtyState()
    {
        var path = WriteTempContentFile(
            """
            entityTemplates:
              rock:
                name: Rock
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 3
            presentations:
              rock:
                glyph: '*'
                color: Earth
            actionPlans: {}
            """);

        try
        {
            var session = ContentEditorSession.OpenFile(path).Session!;
            var id = new EntityTemplateId("rock");

            session.Editor.UpdateEntityPreset(
                id,
                session.Editor.GetEntityPreset(id).Template with { Name = "Heavy Rock", Weight = 5 },
                new EntityPresentation('R', PresentationColor.Gray));
            Assert.True(session.IsDirty);

            var saveResult = session.Save();

            Assert.True(saveResult.IsSuccess, saveResult.ErrorMessage);
            Assert.False(session.IsDirty);
            var reloaded = YamlContentLoader.LoadRegistryFile(path);
            Assert.Equal("Heavy Rock", reloaded.EntityTemplates[id].Name);
            Assert.Equal(5, reloaded.EntityTemplates[id].Weight);
            Assert.Equal('R', reloaded.Presentations[id].Glyph);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SaveAsChangesFilePathWritesYamlAndClearsDirtyState()
    {
        var sourcePath = WriteTempContentFile(
            """
            entityTemplates:
              rock:
                name: Rock
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 3
            presentations:
              rock:
                glyph: '*'
                color: Earth
            actionPlans: {}
            """);
        var saveAsPath = Path.Combine(Path.GetTempPath(), $"game-content-session-{Guid.NewGuid():N}.yaml");

        try
        {
            var session = ContentEditorSession.OpenFile(sourcePath).Session!;
            var id = new EntityTemplateId("rock");

            session.Editor.UpdateEntityPreset(
                id,
                session.Editor.GetEntityPreset(id).Template with { Name = "Saved As Rock" },
                new EntityPresentation('R', PresentationColor.White));

            var result = session.SaveAs(saveAsPath);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(saveAsPath, session.FilePath);
            Assert.False(session.IsDirty);
            Assert.Equal("Saved As Rock", YamlContentLoader.LoadRegistryFile(saveAsPath).EntityTemplates[id].Name);
        }
        finally
        {
            DeleteIfExists(sourcePath);
            DeleteIfExists(saveAsPath);
        }
    }

    [Fact]
    public void CreateNewStartsWithoutPathAndSaveRequiresSaveAsPath()
    {
        var session = ContentEditorSession.CreateNew();

        var result = session.Save();

        Assert.False(result.IsSuccess);
        Assert.Null(session.FilePath);
        Assert.False(session.IsDirty);
        Assert.Contains("path", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InvalidFileLoadReturnsFailureInsteadOfThrowing()
    {
        var path = WriteTempContentFile("entityTemplates: [this is not valid yaml");

        try
        {
            var result = ContentEditorSession.OpenFile(path);

            Assert.False(result.IsSuccess);
            Assert.Null(result.Session);
            Assert.Contains(path, result.ErrorMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    private static string WriteTempContentFile(string yaml)
    {
        var path = Path.Combine(Path.GetTempPath(), $"game-content-session-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(path, yaml);

        return path;
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
