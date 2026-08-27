namespace GameGameGame.Frontend.SadConsole;

internal static class PlayPostSubmitPresentationState
{
    public static bool CloseActionPickersAfterWorldMutation(
        PlaySelectionStack selectionStack,
        PlayActionWorkflowController actionWorkflow,
        MovementPreviewState movementPreview)
    {
        var hadOpenPicker = actionWorkflow.IsActive
            || selectionStack.TopKind != PlaySelectionFrameKind.AdjacentSelection;

        actionWorkflow.Cancel();
        selectionStack.ClearToAdjacentSelection();
        movementPreview.Clear();

        return hadOpenPicker;
    }
}
