using GameGameGame.Content;
using SadConsole;
using Console = SadConsole.Console;

namespace GameGameGame.SadConsoleApp.Ui.Rendering;

internal static class LegacySimulationConsoleFactory
{
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
