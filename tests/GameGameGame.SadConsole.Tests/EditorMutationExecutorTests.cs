using GameGameGame.Content;
using GameGameGame.SadConsoleApp.Ui.Screens;

namespace GameGameGame.SadConsole.Tests;

public sealed class EditorMutationExecutorTests
{
    [Fact]
    public void ExecuteReportsServiceRequiredMessageWhenScreenIsNotServiceBacked()
    {
        var replaced = false;
        var executor = new EditorMutationExecutor(null, _ => replaced = true);

        var result = executor.Execute(
            "Mutation requires a service-backed editor screen.",
            _ => throw new InvalidOperationException("should not run"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Mutation requires a service-backed editor screen.", result.StatusMessage);
        Assert.False(replaced);
    }

    [Fact]
    public void ExecuteConvertsUnexpectedMutationExceptionToStatusMessage()
    {
        var replaced = false;
        var executor = new EditorMutationExecutor(FrontendEditorService.CreateNew(), _ => replaced = true);

        var result = executor.Execute(
            "Mutation requires a service-backed editor screen.",
            _ => throw new InvalidOperationException("boom"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Editor mutation failed: boom", result.StatusMessage);
        Assert.False(replaced);
    }

    [Fact]
    public void ExecuteReplacesSnapshotAfterSuccessfulMutation()
    {
        FrontendEditorSnapshot? replaced = null;
        var service = FrontendEditorService.CreateNew();
        var executor = new EditorMutationExecutor(service, snapshot => replaced = snapshot);

        var result = executor.Execute(
            "Mutation requires a service-backed editor screen.",
            editor => editor.CreateEntityTemplate("Test Template"));

        Assert.True(result.IsSuccess, result.StatusMessage);
        Assert.NotNull(replaced);
        Assert.Contains(replaced!.EntityTemplates, template => template.Name == "Test Template");
    }
}
