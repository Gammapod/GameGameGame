using GameGameGame.SadConsoleApp.Ui.Components;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed record ConsumerPlayModeRenderFrame(
    SadConsoleRect DrawableBounds,
    bool DebugVisible,
    IReadOnlyList<IUiComponent> MainComponents,
    IReadOnlyList<IUiComponent> DebugComponents,
    IUiComponent? DiagnosticsChromeComponent,
    IUiComponent? PromptOverlay,
    IReadOnlyList<string> DebugRows)
{
    public bool PromptOverlayActive => PromptOverlay is not null;

    public IReadOnlyList<ConnectorLineComponent> MainConnectors =>
        MainComponents.OfType<ConnectorLineComponent>().ToList();

    public IReadOnlyList<ConnectorLineComponent> DebugConnectors =>
        DebugComponents.OfType<ConnectorLineComponent>().ToList();

    public IEnumerable<IUiComponent> MainDrawableComponents =>
        MainComponents.Where(component => component is not ConnectorLineComponent);

    public IEnumerable<IUiComponent> DebugDrawableComponents =>
        DebugComponents.Where(component => component is not ConnectorLineComponent);

    public IReadOnlyList<ConsumerPlayModeCaptureRegion> CaptureRegions()
    {
        var regions = new List<ConsumerPlayModeCaptureRegion>();
        AddRegions(regions, MainComponents);
        if (DiagnosticsChromeComponent is not null)
        {
            regions.Add(new ConsumerPlayModeCaptureRegion(DiagnosticsChromeComponent.Id, DiagnosticsChromeComponent.Title, DiagnosticsChromeComponent.Bounds));
        }

        if (PromptOverlay is not null)
        {
            regions.Add(new ConsumerPlayModeCaptureRegion(PromptOverlay.Id, PromptOverlay.Title, PromptOverlay.Bounds, IsOverlay: true));
        }

        return regions;
    }

    private static void AddRegions(List<ConsumerPlayModeCaptureRegion> regions, IReadOnlyList<IUiComponent> components)
    {
        foreach (var component in components.Where(component => component is not ConnectorLineComponent))
        {
            regions.Add(new ConsumerPlayModeCaptureRegion(component.Id, component.Title, component.Bounds));
        }
    }
}

internal sealed record ConsumerPlayModeCaptureRegion(
    string Id,
    string Title,
    SadConsoleRect Bounds,
    bool IsOverlay = false);
