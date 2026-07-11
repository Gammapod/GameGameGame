using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Rendering;
using SadConsole;
using SadConsole.Configuration;

var startup = SadConsoleStartup.FromArgs(args);
Settings.WindowTitle = startup.LaunchGallery
    ? "GameGameGame SadConsole Component Gallery"
    : startup.LaunchNewScenarioSelection
        ? "GameGameGame SadConsole New Scenario Selection"
    : "GameGameGame SadConsole Legacy Debug Browser (Deprecated / Reference Only)";

var configuration = Builder.GetBuilder()
    .ConfigureFonts((fonts, _) => fonts.UseBuiltinFontExtended())
    .SetWindowSizeInCells(ComponentGalleryConsole.ScreenWidth, ComponentGalleryConsole.ScreenHeight)
    .SetStartingScreen(_ => startup.LaunchGallery
        ? new ComponentGalleryConsole()
        : startup.LaunchNewScenarioSelection
            ? new ScenarioSelectionConsole(startup)
            : new SadConsoleShell(startup))
    .IsStartingScreenFocused(true);

Game.Create(configuration);
Game.Instance.Run();
Game.Instance.Dispose();
