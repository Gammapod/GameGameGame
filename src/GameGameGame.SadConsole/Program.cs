using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Rendering;
using SadConsole;
using SadConsole.Configuration;

var startup = SadConsoleStartup.FromArgs(args);
Settings.WindowTitle = startup.LaunchGallery
    ? "GameGameGame SadConsole Component Gallery"
    : startup.LaunchLegacyBetaEditor
        ? "GameGameGame SadConsole Legacy Beta Editor (Deprecated / Reference Only)"
        : "GameGameGame SadConsole Editor";

var configuration = Builder.GetBuilder()
    .ConfigureFonts((fonts, _) => fonts.UseBuiltinFontExtended())
    .SetWindowSizeInCells(ComponentGalleryConsole.ScreenWidth, ComponentGalleryConsole.ScreenHeight)
    .SetStartingScreen(_ => startup.LaunchGallery
        ? new ComponentGalleryConsole()
        : startup.LaunchLegacyBetaEditor
            ? new SadConsoleShell(startup)
            : new ScenarioSelectionConsole(startup))
    .IsStartingScreenFocused(true);

Game.Create(configuration);
Game.Instance.Run();
Game.Instance.Dispose();
