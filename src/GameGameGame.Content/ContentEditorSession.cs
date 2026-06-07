namespace GameGameGame.Content;

public sealed class ContentEditorSession
{
    private ContentEditorSession(EditableContentDocument document, string? filePath)
    {
        Document = document;
        FilePath = filePath;
        Editor = new ContentEditorService(document, MarkDirty);
    }

    public EditableContentDocument Document { get; }

    public ContentEditorService Editor { get; }

    public string? FilePath { get; private set; }

    public bool IsDirty { get; private set; }

    public static ContentEditorSession CreateNew() => new(new EditableContentDocument(), filePath: null);

    public static ContentEditorSessionOpenResult OpenFile(string path)
    {
        try
        {
            var document = EditableContentDocument.LoadYaml(File.ReadAllText(path));

            return ContentEditorSessionOpenResult.Success(new ContentEditorSession(document, path));
        }
        catch (Exception ex)
        {
            return ContentEditorSessionOpenResult.Failure($"Could not open content file {path}: {ex.Message}");
        }
    }

    public ContentEditorFileOperationResult Save()
    {
        if (FilePath is null)
        {
            return ContentEditorFileOperationResult.Failure("Cannot save content document before choosing a file path.");
        }

        return SaveAs(FilePath);
    }

    public ContentEditorFileOperationResult SaveAs(string path)
    {
        try
        {
            File.WriteAllText(path, Document.SaveYaml());
            FilePath = path;
            IsDirty = false;

            return ContentEditorFileOperationResult.Success();
        }
        catch (Exception ex)
        {
            return ContentEditorFileOperationResult.Failure($"Could not save content file {path}: {ex.Message}");
        }
    }

    private void MarkDirty() => IsDirty = true;
}

public sealed record ContentEditorSessionOpenResult(ContentEditorSession? Session, string? ErrorMessage)
{
    public bool IsSuccess => Session is not null;

    public static ContentEditorSessionOpenResult Success(ContentEditorSession session) => new(session, ErrorMessage: null);

    public static ContentEditorSessionOpenResult Failure(string errorMessage) => new(Session: null, errorMessage);
}

public sealed record ContentEditorFileOperationResult(string? ErrorMessage)
{
    public bool IsSuccess => ErrorMessage is null;

    public static ContentEditorFileOperationResult Success() => new(ErrorMessage: null);

    public static ContentEditorFileOperationResult Failure(string errorMessage) => new(errorMessage);
}
