using GameGameGame.Core;

namespace GameGameGame.Content;

internal sealed class FrontendCarriedLayoutMutationService(
    ContentEditorSession session,
    Func<FrontendEditorSnapshot> getSnapshot)
{
    public FrontendEditorMutationResult PlaceTemplateInInventory(
        string parentTemplateId,
        string brushTemplateId,
        GridCoord coord)
    {
        var validationError = ValidateBrushTemplate(parentTemplateId, brushTemplateId);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, getSnapshot());
        }

        var parent = new EntityTemplateId(parentTemplateId);
        var brush = new EntityTemplateId(brushTemplateId);
        try
        {
            var placement = session.Editor.ValidateCarriedEntityPlacement(parent, coord);
            if (!placement.IsSuccess)
            {
                return FrontendEditorMutationResult.Failure(
                    placement.ErrorMessage ?? $"Cannot place template {brushTemplateId} at {coord.X},{coord.Y}.",
                    getSnapshot());
            }

            var entityId = session.Editor.PlaceCarriedEntity(parent, brush, coord);
            return FrontendEditorMutationResult.Success(
                $"Placed template {brushTemplateId} as {entityId.Value} in {parentTemplateId} at {coord.X},{coord.Y}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not place template {brushTemplateId} in {parentTemplateId}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult RemoveCarriedEntity(string parentTemplateId, string entityId)
    {
        var validationError = ValidateCarriedEntityMutation(parentTemplateId, entityId);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, getSnapshot());
        }

        try
        {
            session.Editor.RemoveCarriedEntity(new EntityTemplateId(parentTemplateId), new EntityId(entityId));
            return FrontendEditorMutationResult.Success(
                $"Removed carried entity {entityId} from template {parentTemplateId}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not remove carried entity {entityId} from template {parentTemplateId}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult MoveCarriedEntity(string parentTemplateId, string entityId, GridCoord coord)
    {
        var validationError = ValidateCarriedEntityMutation(parentTemplateId, entityId);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, getSnapshot());
        }

        try
        {
            var parent = new EntityTemplateId(parentTemplateId);
            var carried = new EntityId(entityId);
            var placement = session.Editor.ValidateCarriedEntityPlacement(parent, coord, carried);
            if (!placement.IsSuccess)
            {
                return FrontendEditorMutationResult.Failure(
                    placement.ErrorMessage ?? $"Cannot move carried entity {entityId} to {coord.X},{coord.Y}.",
                    getSnapshot());
            }

            session.Editor.MoveCarriedEntity(parent, carried, coord);
            return FrontendEditorMutationResult.Success(
                $"Moved carried entity {entityId} in template {parentTemplateId} to {coord.X},{coord.Y}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not move carried entity {entityId} in template {parentTemplateId}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult ReplaceCarriedEntityTemplate(
        string parentTemplateId,
        string entityId,
        string brushTemplateId)
    {
        var validationError = ValidateCarriedEntityMutation(parentTemplateId, entityId)
            ?? ValidateBrushTemplate(parentTemplateId, brushTemplateId);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, getSnapshot());
        }

        try
        {
            session.Editor.ReplaceCarriedEntityTemplate(
                new EntityTemplateId(parentTemplateId),
                new EntityId(entityId),
                new EntityTemplateId(brushTemplateId));
            return FrontendEditorMutationResult.Success(
                $"Replaced carried entity {entityId} in template {parentTemplateId} with template {brushTemplateId}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not replace carried entity {entityId} in template {parentTemplateId}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult SetCarriedEntityController(
        string parentTemplateId,
        string entityId,
        EntityController? controller)
    {
        var validationError = ValidateCarriedEntityMutation(parentTemplateId, entityId);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, getSnapshot());
        }

        try
        {
            session.Editor.SetCarriedEntityController(
                new EntityTemplateId(parentTemplateId),
                new EntityId(entityId),
                controller);
            var label = controller?.ToString() ?? "default Computer";
            return FrontendEditorMutationResult.Success(
                $"Set carried entity {entityId} in template {parentTemplateId} controller to {label}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not set carried entity {entityId} controller in template {parentTemplateId}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult OverwriteTemplateInInventory(
        string parentTemplateId,
        string brushTemplateId,
        GridCoord coord)
    {
        var validationError = ValidateBrushTemplate(parentTemplateId, brushTemplateId);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, getSnapshot());
        }

        try
        {
            var parent = new EntityTemplateId(parentTemplateId);
            var placement = session.Editor.ValidateCarriedEntityPlacement(parent, coord);
            if (!placement.IsSuccess && placement.ErrorMessage?.Contains("occupied", StringComparison.OrdinalIgnoreCase) is not true)
            {
                return FrontendEditorMutationResult.Failure(
                    placement.ErrorMessage ?? $"Cannot overwrite cell {coord.X},{coord.Y} in template {parentTemplateId}.",
                    getSnapshot());
            }

            var occupant = session.Editor.ListCarriedEntities(parent)
                .FirstOrDefault(carried => carried.Coord == coord);
            if (occupant is not null)
            {
                session.Editor.RemoveCarriedEntity(parent, occupant.EntityId);
            }

            var entityId = session.Editor.PlaceCarriedEntity(parent, new EntityTemplateId(brushTemplateId), coord);
            return FrontendEditorMutationResult.Success(
                $"Overwrote {parentTemplateId} cell {coord.X},{coord.Y} with template {brushTemplateId} as {entityId.Value}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not overwrite inventory cell {coord.X},{coord.Y} in template {parentTemplateId}: {ex.Message}",
                getSnapshot());
        }
    }

    private string? ValidateCarriedEntityMutation(string parentTemplateId, string entityId)
    {
        if (string.IsNullOrWhiteSpace(parentTemplateId))
        {
            return "Parent template id is required.";
        }

        if (session.Document.EntityTemplates.ContainsKey(parentTemplateId) is false)
        {
            return $"Parent template {parentTemplateId} does not exist.";
        }

        if (string.IsNullOrWhiteSpace(entityId))
        {
            return "Carried entity id is required.";
        }

        var exists = session.Editor.ListCarriedEntities(new EntityTemplateId(parentTemplateId))
            .Any(carried => carried.EntityId.Value == entityId);
        return exists ? null : $"Entity template {parentTemplateId} does not carry entity {entityId}.";
    }

    private string? ValidateBrushTemplate(string parentTemplateId, string brushTemplateId)
    {
        if (string.IsNullOrWhiteSpace(parentTemplateId))
        {
            return "Parent template id is required.";
        }

        if (string.IsNullOrWhiteSpace(brushTemplateId))
        {
            return "Brush template id is required.";
        }

        if (string.Equals(parentTemplateId, brushTemplateId, StringComparison.Ordinal))
        {
            return $"Cannot place template {brushTemplateId} inside itself. Direct self-template placement is disabled; validation still catches deeper cycles.";
        }

        if (session.Document.EntityTemplates.ContainsKey(parentTemplateId) is false)
        {
            return $"Parent template {parentTemplateId} does not exist.";
        }

        if (session.Document.EntityTemplates.ContainsKey(brushTemplateId) is false)
        {
            return $"Brush template {brushTemplateId} does not exist.";
        }

        return null;
    }
}
