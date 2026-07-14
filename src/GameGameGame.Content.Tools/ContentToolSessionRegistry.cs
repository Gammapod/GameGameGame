using GameGameGame.Content;

namespace GameGameGame.Content.Tools;

public sealed class ContentToolSessionRegistry
{
    private readonly Dictionary<string, AgentContentEditorApi> sessions = [];

    public ContentToolSessionOpened CreateNew()
    {
        var sessionId = NewSessionId();
        var api = AgentContentEditorApi.CreateNew();
        sessions[sessionId] = api;
        return new ContentToolSessionOpened(sessionId, api.Session.FilePath, api.Session.IsDirty);
    }

    public AgentApiResult<ContentToolSessionOpened> OpenFile(string path)
    {
        var result = AgentContentEditorApi.OpenFile(path);
        if (!result.IsSuccess)
        {
            return AgentApiResult<ContentToolSessionOpened>.Failure(result.Error!);
        }

        var sessionId = NewSessionId();
        sessions[sessionId] = result.Value!;
        return AgentApiResult<ContentToolSessionOpened>.Success(new ContentToolSessionOpened(sessionId, result.Value!.Session.FilePath, result.Value.Session.IsDirty));
    }

    public AgentApiResult<AgentContentEditorApi> Get(string sessionId) =>
        sessions.TryGetValue(sessionId, out var api)
            ? AgentApiResult<AgentContentEditorApi>.Success(api)
            : AgentApiResult<AgentContentEditorApi>.Failure(new AgentApiError(
                "InvalidSession",
                $"No active content editor session exists for session ID '{sessionId}'. Open or create a session first.",
                Recoverable: true,
                SuggestedActions: ["Call ggg_content_open_file or ggg_content_create_new, then retry with the returned sessionId."]));

    public bool Close(string sessionId) => sessions.Remove(sessionId);

    private static string NewSessionId() => $"content-session-{Guid.NewGuid():N}";
}
