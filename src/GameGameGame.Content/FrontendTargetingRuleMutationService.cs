using GameGameGame.Core;

namespace GameGameGame.Content;

internal sealed class FrontendTargetingRuleMutationService(
    ContentEditorSession session,
    Func<FrontendEditorSnapshot> getSnapshot)
{
    public FrontendEditorMutationResult SetTemplateTargetingRule(
        string templateId,
        FrontendEditorTargetingRuleUpdate update)
    {
        var validationError = ValidateTargetingRuleUpdate(templateId, update);
        if (validationError is not null)
        {
            return FrontendEditorMutationResult.Failure(validationError, getSnapshot());
        }

        var template = new EntityTemplateId(templateId);
        try
        {
            session.Editor.SetTargetingRule(
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
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not update targeting rule slot {update.Slot} on template {templateId}: {ex.Message}",
                getSnapshot());
        }
    }

    public FrontendEditorMutationResult ClearTemplateTargetingRule(string templateId, int slot)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return FrontendEditorMutationResult.Failure("Template id is required.", getSnapshot());
        }

        if (slot is < 1 or > 4)
        {
            return FrontendEditorMutationResult.Failure("Targeting rule slot must be between 1 and 4.", getSnapshot());
        }

        try
        {
            session.Editor.RemoveTargetingRule(new EntityTemplateId(templateId), slot);
            return FrontendEditorMutationResult.Success(
                $"Cleared targeting rule slot {slot} on template {templateId}. Preview stale until P rematerializes.",
                getSnapshot());
        }
        catch (Exception ex)
        {
            return FrontendEditorMutationResult.Failure(
                $"Could not clear targeting rule slot {slot} on template {templateId}: {ex.Message}",
                getSnapshot());
        }
    }

    private string? ValidateTargetingRuleUpdate(string templateId, FrontendEditorTargetingRuleUpdate update)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return "Template id is required.";
        }

        if (session.Document.EntityTemplates.ContainsKey(templateId) is false)
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
            && session.Document.EntityTemplates.ContainsKey(update.TargetTemplateId) is false)
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

        var duplicate = session.Editor.ListTargetingRules(new EntityTemplateId(templateId))
            .Any(rule => rule.Slot != update.Slot && string.Equals(rule.Label, update.Label, StringComparison.Ordinal));
        if (duplicate)
        {
            return $"Duplicate targeting rule label {update.Label} on template {templateId}.";
        }

        return null;
    }
}
