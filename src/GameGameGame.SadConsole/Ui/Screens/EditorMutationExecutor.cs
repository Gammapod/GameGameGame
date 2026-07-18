using GameGameGame.Content;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed class EditorMutationExecutor
{
    private readonly FrontendEditorService? _service;
    private readonly Action<FrontendEditorSnapshot> _replaceSnapshot;

    public EditorMutationExecutor(FrontendEditorService? service, Action<FrontendEditorSnapshot> replaceSnapshot)
    {
        _service = service;
        _replaceSnapshot = replaceSnapshot;
    }

    public EditorMutationExecutionResult Execute(
        string serviceRequiredMessage,
        Func<FrontendEditorService, FrontendEditorMutationResult> mutation)
    {
        if (_service is null)
        {
            return EditorMutationExecutionResult.Failure(serviceRequiredMessage);
        }

        try
        {
            var result = mutation(_service);
            _replaceSnapshot(result.Snapshot);
            return new EditorMutationExecutionResult(result.IsSuccess, result.StatusMessage);
        }
        catch (Exception ex)
        {
            return EditorMutationExecutionResult.Failure($"Editor mutation failed: {ex.Message}");
        }
    }
}

internal sealed record EditorMutationExecutionResult(bool IsSuccess, string StatusMessage)
{
    public static EditorMutationExecutionResult Failure(string statusMessage) => new(false, statusMessage);
}
