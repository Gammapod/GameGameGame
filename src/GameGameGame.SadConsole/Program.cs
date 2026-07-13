using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Rendering;
using SadConsole;
using SadConsole.Configuration;

var startup = SadConsoleStartup.FromArgs(args);
Settings.WindowTitle = startup.LaunchGallery
    ? "GameGameGame SadConsole Component Gallery"
    : "GameGameGame SadConsole Editor";

var configuration = Builder.GetBuilder()
    .ConfigureFonts((fonts, _) => fonts.UseBuiltinFontExtended())
    .SetWindowSizeInCells(SadConsoleScreenMetrics.ScreenWidth, SadConsoleScreenMetrics.ScreenHeight)
    .SetStartingScreen(_ => startup.LaunchGallery
        ? new ComponentGalleryConsole()
        : new ScenarioSelectionConsole(startup))
    .IsStartingScreenFocused(true);

Game.Create(configuration);
Game.Instance.Run();
Game.Instance.Dispose();
