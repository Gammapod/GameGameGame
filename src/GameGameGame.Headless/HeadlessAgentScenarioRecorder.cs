using GameGameGame.Content;

namespace GameGameGame.Headless;

public sealed class HeadlessAgentScenarioRecorder : IAgentScenarioRecorder
{
    public AgentScenarioRecordingReport Record(EditableContentDocument document, AgentScenarioRecordingRequest request)
    {
        var report = ScenarioRecordingService.Record(
            document,
            new ScenarioRecordingRequest(request.ScenarioId, request.TurnCount, request.OutputDirectory));

        return new AgentScenarioRecordingReport(
            report.ScenarioId,
            report.Name,
            report.ScenarioPlaneId,
            report.PlayerEntityId,
            report.Frames
                .Select(frame => new AgentScenarioRecordingFrame(frame.FrameIndex, frame.TurnNumber, frame.PngPath))
                .ToList(),
            report.GifPath,
            report.ValidationDiagnostics,
            report.RuntimeObservations,
            report.RuntimeFailures,
            report.CapabilityGaps);
    }
}
