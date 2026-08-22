namespace GameGameGame.Frontend.SadConsole;

internal static class PlayActionHighlightResolver
{
    public static CellHighlightKind ForInspectionAction(EntityInspectionActionRow? row)
    {
        if (row is null)
        {
            return CellHighlightKind.EntityTarget;
        }

        if (!row.Selectable)
        {
            return CellHighlightKind.NoAction;
        }

        return row.Candidate?.Kind switch
        {
            GameGameGame.Core.ActionChoiceKind.Pickup => CellHighlightKind.Pickup,
            GameGameGame.Core.ActionChoiceKind.Enter => CellHighlightKind.Enter,
            GameGameGame.Core.ActionChoiceKind.Transfer => CellHighlightKind.Transfer,
            _ => CellHighlightKind.EntityTarget
        };
    }
}
