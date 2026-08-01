using GameGameGame.SadConsoleApp.Ui.Styling;

namespace GameGameGame.SadConsoleApp.Ui.Components;

internal sealed record InventorySpaceScaleProbeSample(
    string Id,
    string Label,
    InventorySpaceDisplayProfile Profile,
    InventorySpaceViewModel View);

internal sealed record InventorySpaceScaleProbeComponent(
    string Id,
    string Title,
    SadConsoleRect Bounds,
    IReadOnlyList<InventorySpaceScaleProbeSample> Samples,
    UiComponentState State = UiComponentState.Unselected) : IUiComponent
{
    public IReadOnlyList<string> RenderRows(SadConsoleTheme theme)
    {
        var rows = new List<string>
        {
            $"[{State.BorderColor(theme)}] {Title}",
            "Mixed Space Zoom probe; child surfaces use pixel positioning.",
            "Micro4 uses colored-square draw path, not Candii.",
        };

        rows.AddRange(Samples.Select(sample =>
            $"{sample.Label}: {sample.Profile.SpaceZoom} {sample.Profile.CellPixelSize}px gap {sample.Profile.CellGapPixels}px"));
        return rows;
    }
}
