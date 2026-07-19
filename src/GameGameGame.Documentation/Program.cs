namespace GameGameGame.Documentation;

internal static class Program
{
    private static int Main(string[] args)
    {
        var command = args.Length > 0 ? args[0] : "lint";
        var root = FindRepositoryRoot();
        var graph = DocumentationGraph.Build(DocumentationRepositoryLoader.LoadCompiledDocuments(root));

        switch (command)
        {
            case "lint":
                return RunLint(root, graph);
            case "graph":
                var highlightedProfile = ReadOption(args, "--highlight-profile");
                if (args.Contains("--mmd", StringComparer.OrdinalIgnoreCase) || args.Contains("--mermaid", StringComparer.OrdinalIgnoreCase))
                {
                    var traversal = highlightedProfile is null ? null : TraversalProfileGenerator.Generate(graph, highlightedProfile);
                    Console.Write(MermaidGraphRenderer.Render(graph, traversal));
                }
                else
                {
                    PrintGraph(graph);
                }
                return 0;
            case "graph-mmd":
                var graphMmdHighlightedProfile = ReadOption(args, "--highlight-profile");
                var graphMmdTraversal = graphMmdHighlightedProfile is null ? null : TraversalProfileGenerator.Generate(graph, graphMmdHighlightedProfile);
                Console.Write(MermaidGraphRenderer.Render(graph, graphMmdTraversal));
                return 0;
            case "graph-class-mmd":
                Console.Write(MermaidClassDiagramRenderer.Render(graph));
                return 0;
            case "read-path":
                var role = ReadOption(args, "--role") ?? "core-owner";
                PrintReadPath(graph, role);
                return 0;
            case "traversal":
                var profile = ReadOption(args, "--profile") ?? "content-authoring";
                if (args.Contains("--mmd", StringComparer.OrdinalIgnoreCase) || args.Contains("--mermaid", StringComparer.OrdinalIgnoreCase))
                {
                    Console.Write(TraversalMermaidRenderer.Render(TraversalProfileGenerator.Generate(graph, profile)));
                }
                else
                {
                    PrintTraversal(graph, profile);
                }
                return 0;
            case "traversal-mmd":
                Console.Write(TraversalMermaidRenderer.Render(TraversalProfileGenerator.Generate(graph, ReadOption(args, "--profile") ?? "content-authoring")));
                return 0;
            case "traversal-metrics":
                PrintTraversalMetrics(graph);
                return 0;
            default:
                Console.Error.WriteLine($"Unknown command '{command}'. Expected lint, graph, read-path, traversal, traversal-mmd, or traversal-metrics.");
                return 2;
        }
    }

    private static int RunLint(string root, DocumentationGraph graph)
    {
        var result = DocumentationLinter.Lint(graph, root);
        foreach (var diagnostic in result.Diagnostics)
        {
            Console.WriteLine($"{diagnostic.Code}: {diagnostic.Path}: {diagnostic.Message}");
        }

        Console.WriteLine(result.Success
            ? $"Documentation lint passed for {graph.Documents.Count} compiled docs."
            : $"Documentation lint failed with {result.Diagnostics.Count} diagnostics across {graph.Documents.Count} compiled docs.");

        return result.Success ? 0 : 1;
    }

    private static void PrintGraph(DocumentationGraph graph)
    {
        foreach (var document in graph.Documents.OrderBy(d => d.Metadata.Id, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"{document.Metadata.Id} ({document.Path})");
        }

        Console.WriteLine("Discovery edges:");
        foreach (var edge in graph.GetDiscoveryEdges())
        {
            Console.WriteLine($"{edge.From.Metadata.Id} -> {edge.To.Metadata.Id} ({edge.Label})");
        }
    }

    private static void PrintReadPath(DocumentationGraph graph, string role)
    {
        Console.WriteLine($"Read path for {role}:");
        var index = 1;
        foreach (var document in RoleReadPathGenerator.Generate(graph, role))
        {
            Console.WriteLine($"{index++}. {document.Metadata.Id} - {document.Path}");
        }
    }

    private static void PrintTraversal(DocumentationGraph graph, string profileId)
    {
        var profile = TraversalProfileGenerator.Generate(graph, profileId);
        Console.WriteLine($"Traversal profile {profile.Id}:");
        var index = 1;
        foreach (var document in profile.Documents)
        {
            Console.WriteLine($"{index++}. {document.Metadata.Id} - {document.Path}");
        }
    }

    private static void PrintTraversalMetrics(DocumentationGraph graph)
    {
        Console.WriteLine("Traversal coverage:");
        foreach (var metric in TraversalProfileGenerator.GenerateCoverageMetrics(graph))
        {
            Console.WriteLine($"{metric.DocumentId}: {metric.ProfileCount} profiles - {metric.Path}");
        }
    }

    private static string? ReadOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "docs")) && Directory.Exists(Path.Combine(directory.FullName, "src")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
