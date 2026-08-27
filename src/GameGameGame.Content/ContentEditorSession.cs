namespace GameGameGame.Content;

public sealed class ContentEditorSession
{
    private ContentEditorSession(EditableContentDocument document, string? filePath)
    {
        Document = document;
        FilePath = filePath;
        Editor = new ContentEditorService(document, MarkDirty);
        _savedYamlBaseline = document.SaveYaml();
    }

    private string _savedYamlBaseline;

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
            var yaml = Document.SaveYaml();
            File.WriteAllText(path, yaml);
            Document.SetSourceYaml(yaml);
            FilePath = path;
            IsDirty = false;
            _savedYamlBaseline = yaml;

            return ContentEditorFileOperationResult.Success();
        }
        catch (Exception ex)
        {
            return ContentEditorFileOperationResult.Failure($"Could not save content file {path}: {ex.Message}");
        }
    }

    public string GetYamlPreview() => Document.SaveYaml();

    public ContentEditorYamlDiff GetYamlDiff()
    {
        var current = GetYamlPreview();
        if (current == _savedYamlBaseline)
        {
            return new ContentEditorYamlDiff([]);
        }

        var baselineLines = SplitLines(_savedYamlBaseline);
        var currentLines = SplitLines(current);
        var lines = baselineLines
            .Where(line => !currentLines.Contains(line))
            .Select(line => $"- {line}")
            .Concat(currentLines
                .Where(line => !baselineLines.Contains(line))
                .Select(line => $"+ {line}"))
            .ToList();

        return new ContentEditorYamlDiff(lines);
    }

    public ContentEditorFileOperationResult Reload()
    {
        if (FilePath is null)
        {
            return ContentEditorFileOperationResult.Failure("Cannot reload content document before choosing a file path.");
        }

        try
        {
            var yaml = File.ReadAllText(FilePath);
            var reloaded = EditableContentDocument.LoadYaml(yaml);
            ReplaceDocumentContents(reloaded);
            Document.SetSourceYaml(yaml);
            _savedYamlBaseline = Document.SaveYaml();
            IsDirty = false;

            return ContentEditorFileOperationResult.Success();
        }
        catch (Exception ex)
        {
            return ContentEditorFileOperationResult.Failure($"Could not reload content file {FilePath}: {ex.Message}");
        }
    }

    private void MarkDirty()
    {
        Document.ClearSourceYaml();
        IsDirty = true;
    }

    private void ReplaceDocumentContents(EditableContentDocument reloaded)
    {
        Document.EntityTemplates.Clear();
        foreach (var (id, template) in reloaded.EntityTemplates)
        {
            Document.EntityTemplates[id] = template;
        }

        Document.Presentations.Clear();
        foreach (var (id, presentation) in reloaded.Presentations)
        {
            Document.Presentations[id] = presentation;
        }

        Document.ActionPlans.Clear();
        foreach (var (id, plan) in reloaded.ActionPlans)
        {
            Document.ActionPlans[id] = plan;
        }

        Document.Scenarios.Clear();
        foreach (var (id, scenario) in reloaded.Scenarios)
        {
            Document.Scenarios[id] = scenario;
        }

        Document.MergedLayers.Clear();
        foreach (var (id, layer) in reloaded.MergedLayers)
        {
            Document.MergedLayers[id] = layer;
        }

        Document.PresentationCatalog = reloaded.PresentationCatalog;
        Document.Palettes = reloaded.Palettes;
    }

    private static IReadOnlyList<string> SplitLines(string yaml) =>
        yaml.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
}

public sealed record ContentEditorYamlDiff(IReadOnlyList<string> Lines)
{
    public bool IsEmpty => Lines.Count == 0;
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
