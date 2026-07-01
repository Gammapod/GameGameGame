using GameGameGame.SadConsoleApp;
using SadConsole;
using SadConsole.Configuration;

Settings.WindowTitle = "GameGameGame SadConsole Debug Browser";

var startup = SadConsoleStartup.FromArgs(args);
var configuration = Builder.GetBuilder()
    .ConfigureFonts((fonts, _) => fonts.UseBuiltinFontExtended())
    .SetWindowSizeInCells(SadConsoleShell.ScreenWidth, SadConsoleShell.ScreenHeight)
    .SetStartingScreen(_ => new SadConsoleShell(startup))
    .IsStartingScreenFocused(true);

Game.Create(configuration);
Game.Instance.Run();
Game.Instance.Dispose();
