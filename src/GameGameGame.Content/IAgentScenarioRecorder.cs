namespace GameGameGame.Content;

public interface IAgentScenarioRecorder
{
    AgentScenarioRecordingReport Record(EditableContentDocument document, AgentScenarioRecordingRequest request);
}
