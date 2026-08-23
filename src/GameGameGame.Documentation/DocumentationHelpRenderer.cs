namespace GameGameGame.Documentation;

public static class DocumentationHelpRenderer
{
    public static string Render() => """
GameGameGame Documentation Tool

Commands:
  help | --help | -h
      Show this help text.

  lint
      Validate compiled documentation metadata, links, path rules, and graph references.

  read-path --role <role> [--topic <topic>] [--format briefing]
      Print the role-oriented discovery path. By default includes IDs, file paths, purposes, and summaries.
      Use --topic to filter within the role path using metadata/body relevance scoring.
      Use --format briefing for read_when/skip_when orientation details.

  graph [--format json|mermaid|mmd] [--highlight-profile <profile>]
      Print compiled document IDs and discovery edges, or export the graph as JSON/Mermaid.

  graph-mmd [--highlight-profile <profile>]
      Export the full documentation graph as Mermaid flowchart text.

  graph-class-mmd
      Export metadata-rich documentation graph as a Mermaid class diagram.

  traversal --profile <profile> [--mmd|--mermaid]
      Print a task traversal profile, optionally as Mermaid.

  traversal-mmd --profile <profile>
      Export a task traversal profile as Mermaid.

  traversal-metrics
      Print traversal profile coverage counts per document.

  check-planning
      Run advisory planning freshness checks for active planning boards.

Roles:
  core-owner
  content-editor
  frontend-owner

Common profiles:
  core-stable-behavior-change
  content-authoring

Examples:
  dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- read-path --role core-owner
  dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- read-path --role core-owner --topic topology
  dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- graph --format json
  dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- check-planning
""";
}
