using GameGameGame.Core;

namespace GameGameGame.Content;

public enum ContentWorkspaceMutationPolicy
{
    RespectDocumentProtection,
    AllowProtectedDocumentMutation
}

public enum ContentWorkspaceSavePolicy
{
    SkipProtectedDocuments,
    IncludeProtectedDocuments
}

public sealed record ContentWorkspaceEditResult(
    bool IsSuccess,
    string? ErrorMessage = null,
    bool MutatedProtectedDocument = false)
{
    public static ContentWorkspaceEditResult Success(bool mutatedProtectedDocument = false) =>
        new(true, ErrorMessage: null, mutatedProtectedDocument);

    public static ContentWorkspaceEditResult Failure(string errorMessage) =>
        new(false, errorMessage);
}

public sealed record ContentWorkspaceSaveResult(
    IReadOnlyList<string> SavedDocumentIds,
    IReadOnlyList<string> SkippedProtectedDocumentIds,
    IReadOnlyList<string> Errors)
{
    public bool IsSuccess => Errors.Count == 0;
}

public sealed class ContentWorkspaceEditor(ContentWorkspace workspace)
{
    public ContentWorkspaceEditResult CreateEntityPreset(
        string name,
        string? targetDocumentId = null,
        ContentWorkspaceMutationPolicy policy = ContentWorkspaceMutationPolicy.RespectDocumentProtection)
    {
        var candidates = workspace.Documents
            .Where(document => targetDocumentId is null || document.DocumentId == targetDocumentId)
            .ToList();
        if (candidates.Count != 1)
        {
            return ContentWorkspaceEditResult.Failure(targetDocumentId is null
                ? "Creating a workspace entity template requires a target document when multiple documents are loaded."
                : $"Target workspace document {targetDocumentId} was not found or is ambiguous.");
        }

        var owner = candidates[0];
        if (owner.IsReadOnly && policy == ContentWorkspaceMutationPolicy.RespectDocumentProtection)
        {
            return ContentWorkspaceEditResult.Failure($"Target document {Describe(owner)} is protected and cannot be mutated by the current policy.");
        }

        new ContentEditorService(owner.Document, () => MarkDirty(owner)).CreateEntityPreset(name);
        if (owner.IsReadOnly)
        {
            owner.HasProtectedMutation = true;
        }

        return ContentWorkspaceEditResult.Success(owner.IsReadOnly);
    }

    public ContentWorkspaceEditResult UpdateEntityPreset(
        EntityTemplateId id,
        EntityTemplate template,
        EntityPresentation presentation,
        ContentWorkspaceMutationPolicy policy = ContentWorkspaceMutationPolicy.RespectDocumentProtection)
    {
        var owner = FindUniqueOwner(ContentSymbolKind.EntityTemplate, id.Value);
        if (owner is null)
        {
            return ContentWorkspaceEditResult.Failure($"Entity template {id} was not found or is ambiguous in the workspace.");
        }

        if (owner.IsReadOnly && policy == ContentWorkspaceMutationPolicy.RespectDocumentProtection)
        {
            return ContentWorkspaceEditResult.Failure($"Entity template {id} belongs to protected document {Describe(owner)} and cannot be mutated by the current policy.");
        }

        new ContentEditorService(owner.Document, () => MarkDirty(owner)).UpdateEntityPreset(id, template, presentation);
        if (owner.IsReadOnly)
        {
            owner.HasProtectedMutation = true;
        }

        return ContentWorkspaceEditResult.Success(owner.IsReadOnly);
    }

    public ContentWorkspaceSaveResult Save(ContentWorkspaceSavePolicy policy = ContentWorkspaceSavePolicy.SkipProtectedDocuments)
    {
        var saved = new List<string>();
        var skipped = new List<string>();
        var errors = new List<string>();

        foreach (var document in workspace.Documents.Where(document => document.IsDirty))
        {
            if (document.IsReadOnly && policy == ContentWorkspaceSavePolicy.SkipProtectedDocuments)
            {
                skipped.Add(DocumentLabel(document));
                continue;
            }

            if (document.SourcePath is null)
            {
                errors.Add($"Workspace document {DocumentLabel(document)} has no source path and cannot be saved in-place.");
                continue;
            }

            try
            {
                File.WriteAllText(document.SourcePath, document.Document.SaveYaml());
                document.IsDirty = false;
                document.HasProtectedMutation = false;
                saved.Add(DocumentLabel(document));
            }
            catch (Exception ex)
            {
                errors.Add($"Could not save workspace document {DocumentLabel(document)}: {ex.Message}");
            }
        }

        return new ContentWorkspaceSaveResult(saved, skipped, errors);
    }

    private ContentWorkspaceDocument? FindUniqueOwner(ContentSymbolKind kind, string id)
    {
        var symbols = ContentCompiler.Compile(workspace).Symbols
            .Where(symbol => symbol.Kind == kind && symbol.Id == id)
            .ToList();
        if (symbols.Count != 1)
        {
            return null;
        }

        var symbol = symbols[0];
        return workspace.Documents.SingleOrDefault(document => document.DocumentId == symbol.DocumentId && document.SourcePath == symbol.SourcePath);
    }

    private static void MarkDirty(ContentWorkspaceDocument document)
    {
        document.IsDirty = true;
    }

    private static string Describe(ContentWorkspaceDocument document) =>
        $"{DocumentLabel(document)} ({document.SourcePath ?? "no source path"})";

    private static string DocumentLabel(ContentWorkspaceDocument document) =>
        document.DocumentId ?? document.SourcePath ?? "unknown-document";
}
