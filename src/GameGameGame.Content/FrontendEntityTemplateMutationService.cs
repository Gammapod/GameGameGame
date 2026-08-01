using GameGameGame.Core;

namespace GameGameGame.Content;

internal sealed class FrontendEntityTemplateMutationService(
    ContentEditorSession session,
    Func<FrontendEditorSnapshot> getSnapshot)
{
    public FrontendEditorMutationResult CreateEntityTemplate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return FrontendEditorMutationResult.Failure("Template name is required.", getSnapshot());
        }

        try
        {
            var id = session.Editor.CreateEntityPreset(name.Trim());
            return FrontendEditorMutationResult.Success(
                $"Created template {id.Value}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not create template {name.Trim()}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult DuplicateEntityTemplate(string sourceTemplateId, string name)
    {
        if (string.IsNullOrWhiteSpace(sourceTemplateId))
        {
            return FrontendEditorMutationResult.Failure("Source template id is required.", getSnapshot());
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return FrontendEditorMutationResult.Failure("Template name is required.", getSnapshot());
        }

        if (session.Document.EntityTemplates.ContainsKey(sourceTemplateId) is false)
        {
            return FrontendEditorMutationResult.Failure($"Source template {sourceTemplateId} does not exist.", getSnapshot());
        }

        try
        {
            var id = session.Editor.DuplicateEntityPreset(new EntityTemplateId(sourceTemplateId), name.Trim());
            return FrontendEditorMutationResult.Success(
                $"Duplicated template {sourceTemplateId} as {id.Value}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not duplicate template {sourceTemplateId}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult DeleteEntityTemplate(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return FrontendEditorMutationResult.Failure("Template id is required.", getSnapshot());
        }

        if (session.Document.EntityTemplates.ContainsKey(templateId) is false)
        {
            return FrontendEditorMutationResult.Failure($"Template {templateId} does not exist.", getSnapshot());
        }

        try
        {
            var result = session.Editor.DeleteEntityPreset(new EntityTemplateId(templateId));
            if (!result.IsSuccess)
            {
                return FrontendEditorMutationResult.Failure(
                    result.ErrorMessage ?? $"Could not delete template {templateId}.",
                    getSnapshot());
            }

            return FrontendEditorMutationResult.Success(
                $"Deleted template {templateId}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not delete template {templateId}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult UpdateTemplatePresentation(
        string templateId,
        FrontendEditorTemplatePresentationUpdate update)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return FrontendEditorMutationResult.Failure("Template id is required.", getSnapshot());
        }

        if (string.IsNullOrWhiteSpace(update.Name))
        {
            return FrontendEditorMutationResult.Failure("Template name is required.", getSnapshot());
        }

        var glyphText = update.GlyphText?.Trim();
        if (string.IsNullOrEmpty(glyphText))
        {
            return FrontendEditorMutationResult.Failure("Glyph is required and must contain at least one symbol.", getSnapshot());
        }

        var id = new EntityTemplateId(templateId);
        try
        {
            var current = session.Editor.GetEntityPreset(id);
            var template = current.Template with { Name = update.Name.Trim() };
            var presentation = new EntityPresentation(
                update.PresentationId ?? current.Presentation.PresentationId,
                update.PaletteId ?? current.Presentation.PaletteId,
                glyphText[0],
                update.Color);

            session.Editor.UpdateEntityPreset(id, template, presentation);
            return FrontendEditorMutationResult.Success(
                $"Updated template {templateId}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not update template {templateId}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult UpdateTemplateMetadata(
        string templateId,
        FrontendEditorTemplateMetadataUpdate update)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return FrontendEditorMutationResult.Failure("Template id is required.", getSnapshot());
        }

        if (update.InventoryWidth < 0 || update.InventoryHeight < 0)
        {
            return FrontendEditorMutationResult.Failure("Template inventory dimensions cannot be negative.", getSnapshot());
        }

        if (update.Bulk < 0)
        {
            return FrontendEditorMutationResult.Failure("Template bulk cannot be negative.", getSnapshot());
        }

        if (update.Aperture < 0)
        {
            return FrontendEditorMutationResult.Failure("Template aperture cannot be negative.", getSnapshot());
        }

        var id = new EntityTemplateId(templateId);
        try
        {
            var current = session.Editor.GetEntityPreset(id);
            var template = current.Template with
            {
                InventoryWidth = update.InventoryWidth,
                InventoryHeight = update.InventoryHeight,
                Bulk = update.Bulk,
                Aperture = update.Aperture
            };

            session.Editor.UpdateEntityPreset(id, template, current.Presentation);
            return FrontendEditorMutationResult.Success(
                $"Updated metadata for template {templateId}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not update metadata for template {templateId}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult SetTemplateInitialFacing(string templateId, Direction facing)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return FrontendEditorMutationResult.Failure("Template id is required.", getSnapshot());
        }

        try
        {
            session.Editor.SetInitialFacing(new EntityTemplateId(templateId), facing);
            return FrontendEditorMutationResult.Success(
                $"Set initial facing for template {templateId} to {facing}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not set initial facing for template {templateId}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult ClearTemplateInitialFacing(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return FrontendEditorMutationResult.Failure("Template id is required.", getSnapshot());
        }

        try
        {
            session.Editor.ClearInitialFacing(new EntityTemplateId(templateId));
            return FrontendEditorMutationResult.Success(
                $"Cleared initial facing for template {templateId}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not clear initial facing for template {templateId}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult SetTemplateEnterPolicy(string templateId, EntityEnterPolicy enterPolicy) =>
        UpdateTemplatePolicy(
            templateId,
            template => template with { EnterPolicy = enterPolicy },
            $"Set enter policy for template {templateId} to {enterPolicy}. Preview stale until P rematerializes.",
            $"Could not set enter policy for template {templateId}");

    public FrontendEditorMutationResult ClearTemplateEnterPolicy(string templateId) =>
        UpdateTemplatePolicy(
            templateId,
            template => template with { EnterPolicy = null },
            $"Cleared enter policy for template {templateId}. Preview stale until P rematerializes.",
            $"Could not clear enter policy for template {templateId}");

    public FrontendEditorMutationResult SetTemplateExitPolicy(string templateId, EntityExitPolicy exitPolicy) =>
        UpdateTemplatePolicy(
            templateId,
            template => template with { ExitPolicy = exitPolicy },
            $"Set exit policy for template {templateId} to {exitPolicy}. Preview stale until P rematerializes.",
            $"Could not set exit policy for template {templateId}");

    public FrontendEditorMutationResult ClearTemplateExitPolicy(string templateId) =>
        UpdateTemplatePolicy(
            templateId,
            template => template with { ExitPolicy = null },
            $"Cleared exit policy for template {templateId}. Preview stale until P rematerializes.",
            $"Could not clear exit policy for template {templateId}");

    public FrontendEditorMutationResult SetTemplateTopologyPolicy(string templateId, EntityTopologyPolicy topologyPolicy) =>
        UpdateTemplatePolicy(
            templateId,
            template => template with { TopologyPolicy = topologyPolicy },
            $"Set topology policy for template {templateId} to {topologyPolicy}. Preview stale until P rematerializes.",
            $"Could not set topology policy for template {templateId}");

    private FrontendEditorMutationResult UpdateTemplatePolicy(
        string templateId,
        Func<EntityTemplate, EntityTemplate> update,
        string successMessage,
        string failurePrefix)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return FrontendEditorMutationResult.Failure("Template id is required.", getSnapshot());
        }

        var id = new EntityTemplateId(templateId);
        try
        {
            var current = session.Editor.GetEntityPreset(id);
            session.Editor.UpdateEntityPreset(id, update(current.Template), current.Presentation);
            return FrontendEditorMutationResult.Success(successMessage, getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure($"{failurePrefix}: {ex.Message}", getSnapshot());
        }
    }

    public FrontendEditorMutationResult SetTemplateDefaultActionPlan(string templateId, string actionPlanId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return FrontendEditorMutationResult.Failure("Template id is required.", getSnapshot());
        }

        if (string.IsNullOrWhiteSpace(actionPlanId))
        {
            return FrontendEditorMutationResult.Failure("Action plan id is required.", getSnapshot());
        }

        var template = new EntityTemplateId(templateId);
        var plan = new ActionPlanTemplateId(actionPlanId);
        try
        {
            if (session.Document.ActionPlans.ContainsKey(plan.Value) is false)
            {
                return FrontendEditorMutationResult.Failure(
                    $"Cannot assign missing action plan {actionPlanId} to template {templateId}.",
                    getSnapshot());
            }

            session.Editor.SetDefaultActionPlan(template, plan);
            return FrontendEditorMutationResult.Success(
                $"Assigned default action plan {actionPlanId} to template {templateId}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not assign action plan {actionPlanId} to template {templateId}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult ClearTemplateDefaultActionPlan(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return FrontendEditorMutationResult.Failure("Template id is required.", getSnapshot());
        }

        var template = new EntityTemplateId(templateId);
        try
        {
            session.Editor.ClearDefaultActionPlan(template);
            return FrontendEditorMutationResult.Success(
                $"Cleared default action plan for template {templateId}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not clear default action plan for template {templateId}: {ex.Message}",
                getSnapshot());
        }
    }
}
