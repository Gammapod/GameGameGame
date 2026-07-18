using GameGameGame.Content;
using SadConsole;
using Console = SadConsole.Console;

namespace GameGameGame.SadConsoleApp.Ui.Rendering;

[Obsolete("Legacy/reference-only factory for quarantined SadConsoleShell. New play launches should use GameplayMockConsole/componentized play surfaces.", error: false)]
internal static class LegacySimulationConsoleFactory
{
    public const bool IsLegacyQuarantined = true;

    public static Console CreateForScenario(ScenarioCatalogEntry scenario)
    {
        var startup = new SadConsoleStartup(
            DirectSession: null,
            Catalog: null,
            Error: null,
            DirectContentPath: scenario.ContentPath,
            DirectScenarioId: scenario.ScenarioId,
            LaunchDirectSimulation: true);

        var shell = new SadConsoleShell(startup)
        {
            IsFocused = true,
            FocusedMode = FocusBehavior.Set
        };

        return shell;
    }
}
