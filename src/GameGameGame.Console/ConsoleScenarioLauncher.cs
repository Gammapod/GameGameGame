using GameGameGame.Content;

namespace GameGameGame.ConsoleApp;

public static class ConsoleScenarioLauncher
{
    public static PlayableScenarioSession CreatePrototype() =>
        PlayableScenarioLauncher.CreatePrototype();

    public static PlayableScenarioSession CreateFromFile(string path, string scenarioId) =>
        PlayableScenarioLauncher.CreateFromFile(path, scenarioId);

    public static PlayableScenarioSession CreateFromCatalogEntry(ScenarioCatalogEntry entry) =>
        PlayableScenarioLauncher.CreateFromCatalogEntry(entry);

    public static PlayableScenarioSession CreateFromDocument(EditableContentDocument document, string scenarioId) =>
        PlayableScenarioLauncher.CreateFromDocument(document, scenarioId);
}
