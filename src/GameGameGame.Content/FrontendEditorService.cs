using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed class FrontendEditorService(ContentEditorSession session)
{
    public ContentEditorSession Session { get; } = session;

    public static FrontendEditorOpenResult OpenFile(string path)
    {
        var result = ContentEditorSession.OpenFile(path);
        return result.IsSuccess
            ? FrontendEditorOpenResult.Success(new FrontendEditorService(result.Session!))
            : FrontendEditorOpenResult.Failure(result.ErrorMessage ?? $"Could not open content file {path}.");
    }

    public static FrontendEditorService CreateNew() => new(ContentEditorSession.CreateNew());

    public FrontendEditorMutationResult CreateEntityTemplate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return FrontendEditorMutationResult.Failure("Template name is required.", GetSnapshot());
        }

        try
        {
            var id = Session.Editor.CreateEntityPreset(name.Trim());
            return FrontendEditorMutationResult.Success(
                $"Created template {id.Value}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not create template {name.Trim()}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult DuplicateEntityTemplate(string sourceTemplateId, string name)
    {
        if (string.IsNullOrWhiteSpace(sourceTemplateId))
        {
            return FrontendEditorMutationResult.Failure("Source template id is required.", GetSnapshot());
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return FrontendEditorMutationResult.Failure("Template name is required.", GetSnapshot());
        }

        if (Session.Document.EntityTemplates.ContainsKey(sourceTemplateId) is false)
        {
            return FrontendEditorMutationResult.Failure($"Source template {sourceTemplateId} does not exist.", GetSnapshot());
        }

        try
        {
            var id = Session.Editor.DuplicateEntityPreset(new EntityTemplateId(sourceTemplateId), name.Trim());
            return FrontendEditorMutationResult.Success(
                $"Duplicated template {sourceTemplateId} as {id.Value}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not duplicate template {sourceTemplateId}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult DeleteEntityTemplate(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return FrontendEditorMutationResult.Failure("Template id is required.", GetSnapshot());
        }

        if (Session.Document.EntityTemplates.ContainsKey(templateId) is false)
        {
            return FrontendEditorMutationResult.Failure($"Template {templateId} does not exist.", GetSnapshot());
        }

        try
        {
            var result = Session.Editor.DeleteEntityPreset(new EntityTemplateId(templateId));
            if (!result.IsSuccess)
            {
                return FrontendEditorMutationResult.Failure(
                    result.ErrorMessage ?? $"Could not delete template {templateId}.",
                    GetSnapshot());
            }

            return FrontendEditorMutationResult.Success(
                $"Deleted template {templateId}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not delete template {templateId}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult CreateActionPlan(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return FrontendEditorMutationResult.Failure("Action plan name is required.", GetSnapshot());
        }

        try
        {
            var id = Session.Editor.CreateActionPlan(name.Trim());
            return FrontendEditorMutationResult.Success(
                $"Created action plan {id.Value}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not create action plan {name.Trim()}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult CreatePassiveActionPlan(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return FrontendEditorMutationResult.Failure("Action plan name is required.", GetSnapshot());
        }

        try
        {
            var id = Session.Editor.CreatePassiveActionPlan(name.Trim());
            return FrontendEditorMutationResult.Success(
                $"Created passive action plan {id.Value}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not create passive action plan {name.Trim()}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult DuplicateActionPlan(string sourceActionPlanId, string name)
    {
        if (string.IsNullOrWhiteSpace(sourceActionPlanId))
        {
            return FrontendEditorMutationResult.Failure("Source action plan id is required.", GetSnapshot());
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return FrontendEditorMutationResult.Failure("Action plan name is required.", GetSnapshot());
        }

        if (Session.Document.ActionPlans.ContainsKey(sourceActionPlanId) is false)
        {
            return FrontendEditorMutationResult.Failure($"Source action plan {sourceActionPlanId} does not exist.", GetSnapshot());
        }

        try
        {
            var id = Session.Editor.DuplicateActionPlan(new ActionPlanTemplateId(sourceActionPlanId), name.Trim());
            return FrontendEditorMutationResult.Success(
                $"Duplicated action plan {sourceActionPlanId} as {id.Value}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not duplicate action plan {sourceActionPlanId}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult DeleteActionPlan(string actionPlanId)
    {
        if (string.IsNullOrWhiteSpace(actionPlanId))
        {
            return FrontendEditorMutationResult.Failure("Action plan id is required.", GetSnapshot());
        }

        if (Session.Document.ActionPlans.ContainsKey(actionPlanId) is false)
        {
            return FrontendEditorMutationResult.Failure($"Action plan {actionPlanId} does not exist.", GetSnapshot());
        }

        try
        {
            var result = Session.Editor.DeleteActionPlan(new ActionPlanTemplateId(actionPlanId));
            if (!result.IsSuccess)
            {
                return FrontendEditorMutationResult.Failure(
                    result.ErrorMessage ?? $"Could not delete action plan {actionPlanId}.",
                    GetSnapshot());
            }

            return FrontendEditorMutationResult.Success(
                $"Deleted action plan {actionPlanId}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not delete action plan {actionPlanId}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult UpdateTemplatePresentation(
        string templateId,
        FrontendEditorTemplatePresentationUpdate update)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return FrontendEditorMutationResult.Failure("Template id is required.", GetSnapshot());
        }

        if (string.IsNullOrWhiteSpace(update.Name))
        {
            return FrontendEditorMutationResult.Failure("Template name is required.", GetSnapshot());
        }

        var glyphText = update.GlyphText?.Trim();
        if (string.IsNullOrEmpty(glyphText))
        {
            return FrontendEditorMutationResult.Failure("Glyph is required and must contain at least one symbol.", GetSnapshot());
        }

        var id = new EntityTemplateId(templateId);
        try
        {
            var current = Session.Editor.GetEntityPreset(id);
            var template = current.Template with { Name = update.Name.Trim() };
            var presentation = new EntityPresentation(glyphText[0], update.Color);

            Session.Editor.UpdateEntityPreset(id, template, presentation);
            return FrontendEditorMutationResult.Success(
                $"Updated template {templateId}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not update template {templateId}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult UpdateTemplateMetadata(
        string templateId,
        FrontendEditorTemplateMetadataUpdate update)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return FrontendEditorMutationResult.Failure("Template id is required.", GetSnapshot());
        }

        if (update.InventoryWidth < 0 || update.InventoryHeight < 0)
        {
            return FrontendEditorMutationResult.Failure("Template inventory dimensions cannot be negative.", GetSnapshot());
        }

        if (update.Bulk < 0)
        {
            return FrontendEditorMutationResult.Failure("Template bulk cannot be negative.", GetSnapshot());
        }

        if (update.Aperture < 0)
        {
            return FrontendEditorMutationResult.Failure("Template aperture cannot be negative.", GetSnapshot());
        }

        var id = new EntityTemplateId(templateId);
        try
        {
            var current = Session.Editor.GetEntityPreset(id);
            var template = current.Template with
            {
                InventoryWidth = update.InventoryWidth,
                InventoryHeight = update.InventoryHeight,
                Bulk = update.Bulk,
                Aperture = update.Aperture
            };

            Session.Editor.UpdateEntityPreset(id, template, current.Presentation);
            return FrontendEditorMutationResult.Success(
                $"Updated metadata for template {templateId}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not update metadata for template {templateId}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult SetTemplateInitialFacing(string templateId, Direction facing)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return FrontendEditorMutationResult.Failure("Template id is required.", GetSnapshot());
        }

        try
        {
            Session.Editor.SetInitialFacing(new EntityTemplateId(templateId), facing);
            return FrontendEditorMutationResult.Success(
                $"Set initial facing for template {templateId} to {facing}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not set initial facing for template {templateId}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult ClearTemplateInitialFacing(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return FrontendEditorMutationResult.Failure("Template id is required.", GetSnapshot());
        }

        try
        {
            Session.Editor.ClearInitialFacing(new EntityTemplateId(templateId));
            return FrontendEditorMutationResult.Success(
                $"Cleared initial facing for template {templateId}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not clear initial facing for template {templateId}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult Save()
    {
        if (Session.FilePath is null)
        {
            return FrontendEditorMutationResult.Failure(
                "Cannot save yet because this editor context has no file path. Save As is not implemented in SadConsole Editor MVP.",
                GetSnapshot());
        }

        var result = Session.Save();
        return result.IsSuccess
            ? FrontendEditorMutationResult.Success($"Saved {Session.FilePath}.", GetSnapshot())
            : FrontendEditorMutationResult.Failure(result.ErrorMessage ?? "Save failed.", GetSnapshot());
    }

    public FrontendEditorMutationResult SetTemplateDefaultActionPlan(string templateId, string actionPlanId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return FrontendEditorMutationResult.Failure("Template id is required.", GetSnapshot());
        }

        if (string.IsNullOrWhiteSpace(actionPlanId))
        {
            return FrontendEditorMutationResult.Failure("Action plan id is required.", GetSnapshot());
        }

        var template = new EntityTemplateId(templateId);
        var plan = new ActionPlanTemplateId(actionPlanId);
        try
        {
            if (Session.Document.ActionPlans.ContainsKey(plan.Value) is false)
            {
                return FrontendEditorMutationResult.Failure(
                    $"Cannot assign missing action plan {actionPlanId} to template {templateId}.",
                    GetSnapshot());
            }

            Session.Editor.SetDefaultActionPlan(template, plan);
            return FrontendEditorMutationResult.Success(
                $"Assigned default action plan {actionPlanId} to template {templateId}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not assign action plan {actionPlanId} to template {templateId}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult ClearTemplateDefaultActionPlan(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return FrontendEditorMutationResult.Failure("Template id is required.", GetSnapshot());
        }

        var template = new EntityTemplateId(templateId);
        try
        {
            Session.Editor.ClearDefaultActionPlan(template);
            return FrontendEditorMutationResult.Success(
                $"Cleared default action plan for template {templateId}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not clear default action plan for template {templateId}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult SetTemplateTargetingRule(
        string templateId,
        FrontendEditorTargetingRuleUpdate update)
    {
        var validationError = ValidateTargetingRuleUpdate(templateId, update);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, GetSnapshot());
        }

        var template = new EntityTemplateId(templateId);
        try
        {
            Session.Editor.SetTargetingRule(
                template,
                new EntityTargetingRule(
                    update.Slot,
                    string.IsNullOrWhiteSpace(update.TargetTemplateId) ? null : new EntityTemplateId(update.TargetTemplateId),
                    update.Range,
                    Hint: null,
                    Label: update.Label,
                    TargetCapabilities: update.TargetCapabilities));
            return FrontendEditorMutationResult.Success(
                $"Updated targeting rule slot {update.Slot} on template {templateId}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not update targeting rule slot {update.Slot} on template {templateId}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult ClearTemplateTargetingRule(string templateId, int slot)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return FrontendEditorMutationResult.Failure("Template id is required.", GetSnapshot());
        }

        if (slot is < 1 or > 4)
        {
            return FrontendEditorMutationResult.Failure("Targeting rule slot must be between 1 and 4.", GetSnapshot());
        }

        try
        {
            Session.Editor.RemoveTargetingRule(new EntityTemplateId(templateId), slot);
            return FrontendEditorMutationResult.Success(
                $"Cleared targeting rule slot {slot} on template {templateId}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not clear targeting rule slot {slot} on template {templateId}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult PlaceTemplateInInventory(
        string parentTemplateId,
        string brushTemplateId,
        GridCoord coord)
    {
        if (string.IsNullOrWhiteSpace(parentTemplateId))
        {
            return FrontendEditorMutationResult.Failure("Parent template id is required.", GetSnapshot());
        }

        if (string.IsNullOrWhiteSpace(brushTemplateId))
        {
            return FrontendEditorMutationResult.Failure("Brush template id is required.", GetSnapshot());
        }

        if (string.Equals(parentTemplateId, brushTemplateId, StringComparison.Ordinal))
        {
            return FrontendEditorMutationResult.Failure(
                $"Cannot place template {brushTemplateId} inside itself. Direct self-template placement is disabled; validation still catches deeper cycles.",
                GetSnapshot());
        }

        if (Session.Document.EntityTemplates.ContainsKey(parentTemplateId) is false)
        {
            return FrontendEditorMutationResult.Failure($"Parent template {parentTemplateId} does not exist.", GetSnapshot());
        }

        if (Session.Document.EntityTemplates.ContainsKey(brushTemplateId) is false)
        {
            return FrontendEditorMutationResult.Failure($"Brush template {brushTemplateId} does not exist.", GetSnapshot());
        }

        var parent = new EntityTemplateId(parentTemplateId);
        var brush = new EntityTemplateId(brushTemplateId);
        try
        {
            var placement = Session.Editor.ValidateCarriedEntityPlacement(parent, coord);
            if (!placement.IsSuccess)
            {
                return FrontendEditorMutationResult.Failure(
                    placement.ErrorMessage ?? $"Cannot place template {brushTemplateId} at {coord.X},{coord.Y}.",
                    GetSnapshot());
            }

            var entityId = Session.Editor.PlaceCarriedEntity(parent, brush, coord);
            return FrontendEditorMutationResult.Success(
                $"Placed template {brushTemplateId} as {entityId.Value} in {parentTemplateId} at {coord.X},{coord.Y}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not place template {brushTemplateId} in {parentTemplateId}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult RemoveCarriedEntity(string parentTemplateId, string entityId)
    {
        var validationError = ValidateCarriedEntityMutation(parentTemplateId, entityId);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, GetSnapshot());
        }

        try
        {
            Session.Editor.RemoveCarriedEntity(new EntityTemplateId(parentTemplateId), new EntityId(entityId));
            return FrontendEditorMutationResult.Success(
                $"Removed carried entity {entityId} from template {parentTemplateId}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not remove carried entity {entityId} from template {parentTemplateId}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult MoveCarriedEntity(string parentTemplateId, string entityId, GridCoord coord)
    {
        var validationError = ValidateCarriedEntityMutation(parentTemplateId, entityId);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, GetSnapshot());
        }

        try
        {
            var parent = new EntityTemplateId(parentTemplateId);
            var carried = new EntityId(entityId);
            var placement = Session.Editor.ValidateCarriedEntityPlacement(parent, coord, carried);
            if (!placement.IsSuccess)
            {
                return FrontendEditorMutationResult.Failure(
                    placement.ErrorMessage ?? $"Cannot move carried entity {entityId} to {coord.X},{coord.Y}.",
                    GetSnapshot());
            }

            Session.Editor.MoveCarriedEntity(parent, carried, coord);
            return FrontendEditorMutationResult.Success(
                $"Moved carried entity {entityId} in template {parentTemplateId} to {coord.X},{coord.Y}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not move carried entity {entityId} in template {parentTemplateId}: {ex.Message}",
                GetSnapshot());
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
            return FrontendEditorMutationResult.Failure(validationError, GetSnapshot());
        }

        try
        {
            Session.Editor.ReplaceCarriedEntityTemplate(
                new EntityTemplateId(parentTemplateId),
                new EntityId(entityId),
                new EntityTemplateId(brushTemplateId));
            return FrontendEditorMutationResult.Success(
                $"Replaced carried entity {entityId} in template {parentTemplateId} with template {brushTemplateId}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not replace carried entity {entityId} in template {parentTemplateId}: {ex.Message}",
                GetSnapshot());
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
            return FrontendEditorMutationResult.Failure(validationError, GetSnapshot());
        }

        try
        {
            var parent = new EntityTemplateId(parentTemplateId);
            var placement = Session.Editor.ValidateCarriedEntityPlacement(parent, coord);
            if (!placement.IsSuccess && placement.ErrorMessage?.Contains("occupied", StringComparison.OrdinalIgnoreCase) is not true)
            {
                return FrontendEditorMutationResult.Failure(
                    placement.ErrorMessage ?? $"Cannot overwrite cell {coord.X},{coord.Y} in template {parentTemplateId}.",
                    GetSnapshot());
            }

            var occupant = Session.Editor.ListCarriedEntities(parent)
                .FirstOrDefault(carried => carried.Coord == coord);
            if (occupant is not null)
            {
                Session.Editor.RemoveCarriedEntity(parent, occupant.EntityId);
            }

            var entityId = Session.Editor.PlaceCarriedEntity(parent, new EntityTemplateId(brushTemplateId), coord);
            return FrontendEditorMutationResult.Success(
                $"Overwrote {parentTemplateId} cell {coord.X},{coord.Y} with template {brushTemplateId} as {entityId.Value}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not overwrite inventory cell {coord.X},{coord.Y} in template {parentTemplateId}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult ReplaceActionPlanStep(
        string actionPlanId,
        int stepIndex,
        ActionPlanBehaviorStepKind kind)
    {
        var validationError = ValidateActionPlanStepMutation(actionPlanId, kind);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, GetSnapshot());
        }

        try
        {
            var planId = new ActionPlanTemplateId(actionPlanId);
            var steps = GetEditableBehaviorSteps(planId);
            if (stepIndex < 0 || stepIndex >= steps.Count)
            {
                return FrontendEditorMutationResult.Failure(
                    $"Action plan {actionPlanId} step index {stepIndex} is outside editable step range 0..{Math.Max(steps.Count - 1, 0)}.",
                    GetSnapshot());
            }

            steps[stepIndex] = new ActionPlanBehaviorStepDescriptor(kind);
            Session.Editor.SetActionPlanBehavior(planId, steps);
            return FrontendEditorMutationResult.Success(
                $"Replaced action plan {actionPlanId} step {stepIndex} with {ActionStepCatalog.Get(kind).DisplayName}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not replace action plan {actionPlanId} step {stepIndex}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult InsertActionPlanStep(
        string actionPlanId,
        int insertIndex,
        ActionPlanBehaviorStepKind kind)
    {
        var validationError = ValidateActionPlanStepMutation(actionPlanId, kind);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, GetSnapshot());
        }

        try
        {
            var planId = new ActionPlanTemplateId(actionPlanId);
            var steps = GetEditableBehaviorSteps(planId, allowEmptyPassive: true);
            if (insertIndex < 0 || insertIndex > steps.Count)
            {
                return FrontendEditorMutationResult.Failure(
                    $"Action plan {actionPlanId} insert index {insertIndex} is outside editable insert range 0..{steps.Count}.",
                    GetSnapshot());
            }

            steps.Insert(insertIndex, new ActionPlanBehaviorStepDescriptor(kind));
            Session.Editor.SetActionPlanBehavior(planId, steps);
            return FrontendEditorMutationResult.Success(
                $"Inserted {ActionStepCatalog.Get(kind).DisplayName} into action plan {actionPlanId} at {insertIndex}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not insert action step into action plan {actionPlanId}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult RemoveActionPlanStep(string actionPlanId, int stepIndex)
    {
        var validationError = ValidateActionPlanMutation(actionPlanId);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, GetSnapshot());
        }

        try
        {
            var planId = new ActionPlanTemplateId(actionPlanId);
            var steps = GetEditableBehaviorSteps(planId);
            if (stepIndex < 0 || stepIndex >= steps.Count)
            {
                return FrontendEditorMutationResult.Failure(
                    $"Action plan {actionPlanId} step index {stepIndex} is outside editable step range 0..{Math.Max(steps.Count - 1, 0)}.",
                    GetSnapshot());
            }

            var removed = steps[stepIndex];
            Session.Editor.RemoveActionPlanBehaviorStep(planId, stepIndex);
            return FrontendEditorMutationResult.Success(
                $"Removed {ActionStepCatalog.Get(removed.Kind).DisplayName} from action plan {actionPlanId} at {stepIndex}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not remove action plan {actionPlanId} step {stepIndex}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult MoveActionPlanStep(string actionPlanId, int fromIndex, int toIndex)
    {
        var validationError = ValidateActionPlanMutation(actionPlanId);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, GetSnapshot());
        }

        try
        {
            var planId = new ActionPlanTemplateId(actionPlanId);
            var steps = GetEditableBehaviorSteps(planId);
            if (fromIndex < 0 || fromIndex >= steps.Count)
            {
                return FrontendEditorMutationResult.Failure(
                    $"Action plan {actionPlanId} from index {fromIndex} is outside editable step range 0..{Math.Max(steps.Count - 1, 0)}.",
                    GetSnapshot());
            }

            if (toIndex < 0 || toIndex >= steps.Count)
            {
                return FrontendEditorMutationResult.Failure(
                    $"Action plan {actionPlanId} to index {toIndex} is outside editable step range 0..{Math.Max(steps.Count - 1, 0)}.",
                    GetSnapshot());
            }

            Session.Editor.MoveActionPlanBehaviorStep(planId, fromIndex, toIndex);
            return FrontendEditorMutationResult.Success(
                $"Moved action plan {actionPlanId} step from {fromIndex} to {toIndex}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not move action plan {actionPlanId} step from {fromIndex} to {toIndex}: {ex.Message}",
                GetSnapshot());
        }
    }

    public FrontendEditorMutationResult SetActionPlanStepTargetLabel(
        string actionPlanId,
        int stepIndex,
        string? targetLabel)
    {
        var validationError = ValidateActionPlanMutation(actionPlanId);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, GetSnapshot());
        }

        try
        {
            var planId = new ActionPlanTemplateId(actionPlanId);
            var steps = GetEditableBehaviorSteps(planId);
            if (stepIndex < 0 || stepIndex >= steps.Count)
            {
                return FrontendEditorMutationResult.Failure(
                    $"Action plan {actionPlanId} step index {stepIndex} is outside editable step range 0..{Math.Max(steps.Count - 1, 0)}.",
                    GetSnapshot());
            }

            var normalizedLabel = targetLabel?.Trim();
            var labelValidationError = ValidateActionPlanStepTargetLabel(normalizedLabel);
            if (labelValidationError is not null)
            {
                return FrontendEditorMutationResult.Failure(labelValidationError, GetSnapshot());
            }

            Session.Editor.SetActionPlanBehaviorStepTargetLabel(planId, stepIndex, normalizedLabel);
            var displayLabel = normalizedLabel is null ? "cleared" : $"set to {normalizedLabel}";
            return FrontendEditorMutationResult.Success(
                $"Action plan {actionPlanId} step {stepIndex} target label {displayLabel}. Preview stale until P rematerializes.",
                GetSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not set action plan {actionPlanId} step {stepIndex} target label: {ex.Message}",
                GetSnapshot());
        }
    }

    private string? ValidateActionPlanStepMutation(string actionPlanId, ActionPlanBehaviorStepKind kind)
    {
        var validationError = ValidateActionPlanMutation(actionPlanId);
        if (validationError is not null)
        {
            return validationError;
        }

        _ = ActionStepCatalog.Get(kind);
        if (ActionStepCatalog.IsStableAuthoringStep(kind) is false)
        {
            return $"Action step {kind} is not available for canonical authoring.";
        }

        return null;
    }

    private string? ValidateActionPlanMutation(string actionPlanId)
    {
        if (string.IsNullOrWhiteSpace(actionPlanId))
        {
            return "Action plan id is required.";
        }

        if (Session.Document.ActionPlans.ContainsKey(actionPlanId) is false)
        {
            return $"Action plan {actionPlanId} does not exist.";
        }

        return null;
    }

    private static string? ValidateActionPlanStepTargetLabel(string? targetLabel)
    {
        if (targetLabel is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(targetLabel))
        {
            return "Action step target label must not be blank.";
        }

        if (targetLabel.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9') is false)
        {
            return "Action step target label must be lowercase alphanumeric with no spaces.";
        }

        return null;
    }

    private string? ValidateCarriedEntityMutation(string parentTemplateId, string entityId)
    {
        if (string.IsNullOrWhiteSpace(parentTemplateId))
        {
            return "Parent template id is required.";
        }

        if (Session.Document.EntityTemplates.ContainsKey(parentTemplateId) is false)
        {
            return $"Parent template {parentTemplateId} does not exist.";
        }

        if (string.IsNullOrWhiteSpace(entityId))
        {
            return "Carried entity id is required.";
        }

        var exists = Session.Editor.ListCarriedEntities(new EntityTemplateId(parentTemplateId))
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

        if (Session.Document.EntityTemplates.ContainsKey(parentTemplateId) is false)
        {
            return $"Parent template {parentTemplateId} does not exist.";
        }

        if (Session.Document.EntityTemplates.ContainsKey(brushTemplateId) is false)
        {
            return $"Brush template {brushTemplateId} does not exist.";
        }

        return null;
    }

    private List<ActionPlanBehaviorStepDescriptor> GetEditableBehaviorSteps(
        ActionPlanTemplateId planId,
        bool allowEmptyPassive = false)
    {
        var descriptor = Session.Editor.ListActionPlans()
            .Single(plan => plan.TemplateId == planId)
            .Descriptor;
        var shape = ActionPlanShapeClassifier.Classify(descriptor);

        if (descriptor.Behavior is { } behavior)
        {
            return behavior.Steps.ToList();
        }

        if (allowEmptyPassive && shape == ActionPlanShape.EmptyPassive)
        {
            return [];
        }

        throw new InvalidOperationException($"Action plan {planId} is {ContentEditorService.FormatActionPlanShape(shape)}; only canonical behavior chains are editable in this slice.");
    }

    private string? ValidateTargetingRuleUpdate(string templateId, FrontendEditorTargetingRuleUpdate update)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return "Template id is required.";
        }

        if (Session.Document.EntityTemplates.ContainsKey(templateId) is false)
        {
            return $"Template {templateId} does not exist.";
        }

        if (update.Slot is < 1 or > 4)
        {
            return "Targeting rule slot must be between 1 and 4.";
        }

        if (string.IsNullOrWhiteSpace(update.Label))
        {
            return "Targeting rule label is required before choosing a target template.";
        }

        if (update.Label.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9') is false)
        {
            return "Targeting rule label must be lowercase alphanumeric with no spaces.";
        }

        if (string.IsNullOrWhiteSpace(update.TargetTemplateId) && update.TargetCapabilities.Count == 0)
        {
            return "Targeting rule requires a target template, at least one target capability, or both.";
        }

        if (!string.IsNullOrWhiteSpace(update.TargetTemplateId)
            && Session.Document.EntityTemplates.ContainsKey(update.TargetTemplateId) is false)
        {
            return $"Target template {update.TargetTemplateId} does not exist.";
        }

        foreach (var capability in update.TargetCapabilities)
        {
            if (!EntityInteractionAffordanceService.IsSupportedTargetCapability(capability))
            {
                return $"Target capability {capability} is not supported for targeting rules.";
            }
        }

        if (update.Range is < 0 or > 10)
        {
            return "Targeting rule range must be between 0 and 10.";
        }

        var duplicate = Session.Editor.ListTargetingRules(new EntityTemplateId(templateId))
            .Any(rule => rule.Slot != update.Slot && string.Equals(rule.Label, update.Label, StringComparison.Ordinal));
        if (duplicate)
        {
            return $"Duplicate targeting rule label {update.Label} on template {templateId}.";
        }

        return null;
    }

    public FrontendEditorSnapshot GetSnapshot()
    {
        var validation = Session.Editor.Validate();
        var canonicalValidation = Session.Document.ValidateCanonicalAuthoring();
        var diagnostics = validation.Diagnostics
            .Concat(canonicalValidation.Diagnostics)
            .Select(FrontendEditorDiagnostic.From)
            .ToList();

        return new FrontendEditorSnapshot(
            Session.FilePath,
            Session.IsDirty,
            ListScenarios(),
            ListEntityTemplates(diagnostics),
            ListActionPlans(),
            ListAvailableActionSteps(),
            diagnostics,
            Session.GetYamlPreview(),
            Session.GetYamlDiff().Lines);
    }

    public FrontendEditorScenarioPreview PreviewScenario(string scenarioId)
    {
        var session = PlayableScenarioLauncher.CreateFromDocument(Session.Document, scenarioId);

        return new FrontendEditorScenarioPreview(
            session.ScenarioId,
            session.Name,
            IsDerivedRuntimeState: true,
            session.CanPlay,
            session,
            session.ValidationDiagnostics,
            session.RuntimeFailures,
            session.CapabilityGaps);
    }

    private IReadOnlyList<FrontendEditorScenarioSummary> ListScenarios() =>
        Session.Document.Scenarios
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry =>
            {
                var scenario = entry.Value.ToDefinition(entry.Key);
                return new FrontendEditorScenarioSummary(
                    scenario.ScenarioId,
                    scenario.Name,
                    scenario.ScenarioRootEntityTemplateId.Value,
                    scenario.PlayerEntityTemplateId.Value,
                    scenario.PlayerEntityId.Value,
                    scenario.PlayerStart);
            })
            .ToList();

    private IReadOnlyList<FrontendEditorEntityTemplateSummary> ListEntityTemplates(IReadOnlyList<FrontendEditorDiagnostic> diagnostics) =>
        Session.Editor.ListEntityPresets()
            .Select(model =>
            {
                var targetingRules = (model.Template.TargetingRules ?? [])
                    .OrderBy(rule => rule.Slot)
                    .ThenBy(rule => rule.Label ?? string.Empty, StringComparer.Ordinal)
                    .Select(rule => new FrontendEditorTargetingRuleSummary(
                        rule.Slot,
                        rule.Label,
                        rule.Hint,
                        rule.TargetTemplateId?.Value,
                        rule.TargetTemplateId is { } targetTemplateId ? TryGetTemplateName(targetTemplateId.Value) : null,
                        rule.Range,
                        rule.TargetCapabilities))
                    .ToList();
                var targetRequirements = GetTargetingRequirements(model.Template.DefaultActionPlanId, targetingRules);
                var requirementLabels = targetRequirements
                    .Select(requirement => requirement.Label)
                    .ToHashSet(StringComparer.Ordinal);
                var orphanedRules = model.Template.DefaultActionPlanId is null
                    ? []
                    : targetingRules
                        .Where(rule => rule.Label is null || !requirementLabels.Contains(rule.Label))
                        .ToList();

                return new FrontendEditorEntityTemplateSummary(
                model.Id.Value,
                model.Template.Name,
                model.Presentation.Glyph,
                model.Presentation.Color,
                model.Template.InventoryWidth,
                model.Template.InventoryHeight,
                model.Template.Bulk,
                model.Template.Aperture,
                model.Template.DefaultActionPlanId?.Value,
                new FrontendEditorActionStateDefaultsSummary(
                    model.Template.ActionStateDefaults?.Facing,
                    model.Template.ActionStateDefaults?.Target?.Value),
                targetingRules,
                (model.Template.CarriedEntities ?? [])
                    .OrderBy(carried => carried.Coord.Y)
                    .ThenBy(carried => carried.Coord.X)
                    .ThenBy(carried => carried.EntityId.Value, StringComparer.Ordinal)
                    .Select(carried => new FrontendEditorCarriedEntitySummary(
                        carried.EntityId.Value,
                        carried.TemplateId?.Value,
                        carried.TemplateId is null ? null : TryGetTemplateName(carried.TemplateId.Value.Value),
                        carried.TemplateId is null ? null : TryGetGlyph(carried.TemplateId.Value.Value),
                        carried.TemplateId is null ? null : TryGetColor(carried.TemplateId.Value.Value),
                        carried.Coord,
                        diagnostics
                            .Where(diagnostic => diagnostic.EntityTemplateId == model.Id.Value
                                && diagnostic.CarriedEntityId == carried.EntityId.Value)
                            .ToList()))
                    .ToList(),
                diagnostics
                    .Where(diagnostic => diagnostic.EntityTemplateId == model.Id.Value)
                    .ToList())
                {
                    TargetingRequirements = targetRequirements,
                    OrphanedTargetingRules = orphanedRules
                };
            })
            .ToList();

    private IReadOnlyList<FrontendEditorTargetingRequirementSummary> GetTargetingRequirements(
        ActionPlanTemplateId? defaultActionPlanId,
        IReadOnlyList<FrontendEditorTargetingRuleSummary> targetingRules)
    {
        if (defaultActionPlanId is null
            || !Session.Document.ActionPlans.TryGetValue(defaultActionPlanId.Value.Value, out var plan))
        {
            return [];
        }

        var rulesByLabel = targetingRules
            .Where(rule => rule.Label is not null)
            .GroupBy(rule => rule.Label!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.OrderBy(rule => rule.Slot).First(), StringComparer.Ordinal);

        return ActionPlanTargetLabelRequirementProjection.Project(plan.ToDescriptor(defaultActionPlanId.Value.Value))
            .Select(requirement =>
            {
                rulesByLabel.TryGetValue(requirement.Label, out var rule);
                return new FrontendEditorTargetingRequirementSummary(
                    requirement.Label,
                    requirement.StepIndexes,
                    requirement.StepKinds,
                    rule is not null,
                    rule);
            })
            .ToList();
    }

    private string? TryGetTemplateName(string templateId) =>
        Session.Document.EntityTemplates.TryGetValue(templateId, out var template)
            ? template.Name ?? templateId
            : null;

    private char? TryGetGlyph(string templateId) =>
        Session.Document.Presentations.TryGetValue(templateId, out var presentation)
            && !string.IsNullOrEmpty(presentation.Glyph)
                ? presentation.Glyph[0]
                : null;

    private PresentationColor? TryGetColor(string templateId) =>
        Session.Document.Presentations.TryGetValue(templateId, out var presentation)
            ? presentation.Color
            : null;

    private IReadOnlyList<FrontendEditorActionPlanSummary> ListActionPlans() =>
        Session.Editor.ListActionPlans()
            .Select(model => new FrontendEditorActionPlanSummary(
                model.TemplateId.Value,
                ContentEditorService.FormatActionPlanShape(ActionPlanShapeClassifier.Classify(model.Descriptor)),
                GetActionSteps(model.Descriptor),
                GetActionStepNames(model.Descriptor))
            {
                TargetLabelRequirements = ActionPlanTargetLabelRequirementProjection.Project(model.Descriptor)
                    .Select(requirement => new FrontendEditorActionPlanTargetLabelRequirementSummary(
                        requirement.Label,
                        requirement.StepIndexes,
                        requirement.StepKinds))
                    .ToList()
            })
            .ToList();

    private IReadOnlyList<FrontendEditorAvailableActionStepSummary> ListAvailableActionSteps() =>
        Session.Editor.ListActionSteps()
            .Select(step => new FrontendEditorAvailableActionStepSummary(
                step.Kind,
                step.DisplayName,
                step.Description))
            .ToList();

    private static IReadOnlyList<FrontendEditorActionPlanStepSummary> GetActionSteps(ActionPlanDescriptor descriptor)
    {
        if (descriptor.Behavior?.Steps.Count > 0)
        {
            return descriptor.Behavior.Steps
                .Select((step, index) =>
                {
                    var metadata = ActionStepCatalog.Get(step.Kind);
                    var consumesTargetReference = metadata.RequiredState
                        .Any(state => state.Slot == ActionPlanSlot.Target);
                    return new FrontendEditorActionPlanStepSummary(
                        index,
                        step.Kind,
                        metadata.DisplayName,
                        step.TargetLabel,
                        step.TargetSlot,
                        consumesTargetReference);
                })
                .ToList();
        }

        return [];
    }

    private static IReadOnlyList<string> GetActionStepNames(ActionPlanDescriptor descriptor)
    {
        if (descriptor.Behavior?.Steps.Count > 0)
        {
            return descriptor.Behavior.Steps
                .Select(step => ActionStepCatalog.Get(step.Kind).DisplayName)
                .ToList();
        }

        if (descriptor.Primitive is { } primitive)
        {
            return [primitive.Kind.ToString()];
        }

        if (descriptor.Steps.Count > 0)
        {
            return descriptor.Steps.Select(step => step.Label).ToList();
        }

        return [];
    }
}

public sealed record FrontendEditorOpenResult(FrontendEditorService? Service, string? ErrorMessage)
{
    public bool IsSuccess => Service is not null;

    public static FrontendEditorOpenResult Success(FrontendEditorService service) => new(service, ErrorMessage: null);

    public static FrontendEditorOpenResult Failure(string errorMessage) => new(Service: null, errorMessage);
}

public sealed record FrontendEditorTemplatePresentationUpdate(
    string Name,
    string? GlyphText,
    PresentationColor Color);

public sealed record FrontendEditorTemplateMetadataUpdate(
    int InventoryWidth,
    int InventoryHeight,
    int Bulk,
    int Aperture);

public sealed record FrontendEditorTargetingRuleUpdate(
    int Slot,
    string Label,
    string? TargetTemplateId,
    int Range,
    IReadOnlyList<ActionPlanBehaviorStepKind>? TargetCapabilities = null)
{
    public IReadOnlyList<ActionPlanBehaviorStepKind> TargetCapabilities { get; } = TargetCapabilities ?? [];
}

public sealed record FrontendEditorMutationResult(
    bool IsSuccess,
    string StatusMessage,
    FrontendEditorSnapshot Snapshot)
{
    public static FrontendEditorMutationResult Success(string statusMessage, FrontendEditorSnapshot snapshot) =>
        new(IsSuccess: true, statusMessage, snapshot);

    public static FrontendEditorMutationResult Failure(string statusMessage, FrontendEditorSnapshot snapshot) =>
        new(IsSuccess: false, statusMessage, snapshot);
}

public sealed record FrontendEditorSnapshot(
    string? FilePath,
    bool IsDirty,
    IReadOnlyList<FrontendEditorScenarioSummary> Scenarios,
    IReadOnlyList<FrontendEditorEntityTemplateSummary> EntityTemplates,
    IReadOnlyList<FrontendEditorActionPlanSummary> ActionPlans,
    IReadOnlyList<FrontendEditorAvailableActionStepSummary> AvailableActionSteps,
    IReadOnlyList<FrontendEditorDiagnostic> ValidationDiagnostics,
    string YamlPreview,
    IReadOnlyList<string> YamlDiffLines);

public sealed record FrontendEditorScenarioSummary(
    string ScenarioId,
    string Name,
    string ScenarioRootEntityTemplateId,
    string PlayerEntityTemplateId,
    string PlayerEntityId,
    GridCoord PlayerStart);

public sealed record FrontendEditorEntityTemplateSummary(
    string TemplateId,
    string Name,
    char Glyph,
    PresentationColor Color,
    int InventoryWidth,
    int InventoryHeight,
    int Bulk,
    int Aperture,
    string? DefaultActionPlanId,
    FrontendEditorActionStateDefaultsSummary ActionStateDefaults,
    IReadOnlyList<FrontendEditorTargetingRuleSummary> TargetingRules,
    IReadOnlyList<FrontendEditorCarriedEntitySummary> CarriedEntities,
    IReadOnlyList<FrontendEditorDiagnostic> Diagnostics)
{
    public IReadOnlyList<FrontendEditorTargetingRequirementSummary> TargetingRequirements { get; init; } = [];

    public IReadOnlyList<FrontendEditorTargetingRuleSummary> OrphanedTargetingRules { get; init; } = [];
}

public sealed record FrontendEditorActionStateDefaultsSummary(
    Direction? Facing,
    string? TargetEntityId);

public sealed record FrontendEditorTargetingRuleSummary(
    int Slot,
    string? Label,
    string? Hint,
    string? TargetTemplateId,
    string? TargetTemplateName,
    int Range,
    IReadOnlyList<ActionPlanBehaviorStepKind>? TargetCapabilities = null)
{
    public IReadOnlyList<ActionPlanBehaviorStepKind> TargetCapabilities { get; } = TargetCapabilities ?? [];
}

public sealed record FrontendEditorTargetingRequirementSummary(
    string Label,
    IReadOnlyList<int> StepIndexes,
    IReadOnlyList<ActionPlanBehaviorStepKind> StepKinds,
    bool IsConfigured,
    FrontendEditorTargetingRuleSummary? Rule);

public sealed record FrontendEditorCarriedEntitySummary(
    string EntityId,
    string? TemplateId,
    string? TemplateName,
    char? Glyph,
    PresentationColor? Color,
    GridCoord Coord,
    IReadOnlyList<FrontendEditorDiagnostic> Diagnostics);

public sealed record FrontendEditorActionPlanSummary(
    string ActionPlanId,
    string Shape,
    IReadOnlyList<FrontendEditorActionPlanStepSummary> ActionSteps,
    IReadOnlyList<string> ActionStepNames)
{
    public IReadOnlyList<FrontendEditorActionPlanTargetLabelRequirementSummary> TargetLabelRequirements { get; init; } = [];
}

public sealed record FrontendEditorActionPlanTargetLabelRequirementSummary(
    string Label,
    IReadOnlyList<int> StepIndexes,
    IReadOnlyList<ActionPlanBehaviorStepKind> StepKinds);

public sealed record FrontendEditorActionPlanStepSummary(
    int Index,
    ActionPlanBehaviorStepKind Kind,
    string DisplayName,
    string? TargetLabel = null,
    int? TargetSlot = null,
    bool ConsumesTargetReference = false);

public sealed record FrontendEditorAvailableActionStepSummary(
    ActionPlanBehaviorStepKind Kind,
    string DisplayName,
    string Hint);

public sealed record FrontendEditorDiagnostic(
    ContentDiagnosticSeverity Severity,
    ContentDiagnosticCode Code,
    string Message,
    string? EntityTemplateId,
    string? ActionPlanId,
    int? StepIndex,
    string? CarriedEntityId,
    GridCoord? Coord)
{
    public static FrontendEditorDiagnostic From(ContentDiagnostic diagnostic) =>
        new(
            diagnostic.Severity,
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.EntityTemplateId?.Value,
            diagnostic.ActionPlanTemplateId?.Value,
            diagnostic.StepIndex,
            diagnostic.CarriedEntityId?.Value,
            diagnostic.Coord);
}

public sealed record FrontendEditorScenarioPreview(
    string ScenarioId,
    string Name,
    bool IsDerivedRuntimeState,
    bool CanPlay,
    PlayableScenarioSession Session,
    IReadOnlyList<string> ValidationDiagnostics,
    IReadOnlyList<string> RuntimeFailures,
    IReadOnlyList<string> CapabilityGaps);
