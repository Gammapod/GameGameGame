using GameGameGame.Content;
using GameGameGame.Frontend.SadConsole;
using SadConsole;
using SadConsole.Configuration;

var settings = FrontendSadConsoleSettingsStore.LoadOrDefault();
var display = SadConsoleDisplaySettings.FromSettings(settings);
var catalog = WorkspaceScenarioCatalogService.BuildDefaultCatalog();

global::SadConsole.Settings.WindowTitle = "GameGameGame Frontend.SadConsole";

var shell = FrontendDisplayShell.Resolve(
    display.StartupWindowWidthPixels,
    display.StartupWindowHeightPixels,
    display);

var configuration = Builder.GetBuilder()
    .ConfigureFonts((fonts, _) =>
    {
        fonts.UseCustomFont("assets/Candii.font");
        fonts.SetDefaultFontSize(display.FontSizePreset);
    })
    .SetWindowSizeInPixels(display.StartupWindowWidthPixels, display.StartupWindowHeightPixels)
    .SetStartingScreen(_ => new ScenarioBrowserConsole(catalog, shell, display, settings.WindowMode))
    .IsStartingScreenFocused(true);

global::SadConsole.Game.Create(configuration);

global::SadConsole.Game.Instance.Run();
global::SadConsole.Game.Instance.Dispose();
