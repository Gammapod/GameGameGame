using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Rendering;
using GameGameGame.SadConsoleApp.Ui.Tiles;
using SadConsole;
using SadConsole.Configuration;

var startup = SadConsoleStartup.FromArgs(args);
var display = startup.ActiveDisplaySettings;
Settings.WindowTitle = startup.LaunchGallery
    ? "GameGameGame SadConsole Component Gallery"
    : startup.LaunchPlayMock
        ? "GameGameGame SadConsole Play UX Mock"
        : "GameGameGame SadConsole Editor";

var configuration = Builder.GetBuilder()
    .ConfigureFonts((fonts, _) =>
    {
        fonts.UseCustomFont("assets/Candii.font");
        fonts.SetDefaultFontSize(display.FontSizePreset);
    })
    .SetWindowSizeInPixels(display.WindowWidthPixels, display.WindowHeightPixels)
    .SetStartingScreen(_ => startup.LaunchGallery
        ? new ComponentGalleryConsole(displaySettings: display)
        : startup.LaunchPlayMock
            ? new GameplayMockConsole(startup, displaySettings: display)
            : new ScenarioSelectionConsole(startup, displaySettings: display))
    .IsStartingScreenFocused(true);

Game.Create(configuration);
Game.Instance.Run();
Game.Instance.Dispose();
