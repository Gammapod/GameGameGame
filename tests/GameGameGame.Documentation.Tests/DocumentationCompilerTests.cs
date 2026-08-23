using GameGameGame.Documentation;

namespace GameGameGame.Documentation.Tests;

public sealed class DocumentationCompilerTests
{
    [Fact]
    public void MarkdownFrontmatterParserReadsScalarListEmptyListAndEmptyValueFields()
    {
        const string markdown = """
---
id: source.example
title: Example Doc
purpose: Short orientation summary.
kind: source-of-truth
status: active
owners: [core-owner]
audience:
  - core-owner
  - frontend-owner
related: []
superseded_by:
truth_rank: 10
truth_domains:
  - runtime-behavior
  - test-trace
---
# Example

Body text.
""";

        var document = MarkdownFrontmatterParser.Parse("docs/Source of Truth/example.md", markdown);

        Assert.Equal("source.example", document.Metadata.Id);
        Assert.Equal("Example Doc", document.Metadata.Title);
        Assert.Equal("Short orientation summary.", document.Metadata.Purpose);
        Assert.Equal("source-of-truth", document.Metadata.Kind);
        Assert.Equal("active", document.Metadata.Status);
        Assert.Equal(["core-owner"], document.Metadata.Owners);
        Assert.Equal(["core-owner", "frontend-owner"], document.Metadata.Audience);
        Assert.Empty(document.Metadata.Related);
        Assert.Null(document.Metadata.SupersededBy);
        Assert.Equal(10, document.Metadata.TruthRank);
        Assert.Equal(["runtime-behavior", "test-trace"], document.Metadata.TruthDomains);
        Assert.Contains("Body text.", document.Body);
    }

    [Fact]
    public void ReadPathBriefingIncludesPurposeAndReadWhenForAgentOrientation()
    {
        var graph = DocumentationGraph.Build([
            SourceDocWithPurpose("docs/Source of Truth/planning-index.md", "source.planning-index", "navigation", "Planning navigation.", ["starting a session"]),
            SourceDocWithPurpose("docs/Source of Truth/invariants.md", "source.invariants", "invariant-trace", "Stable behavior contracts.", ["changing behavior"])
        ]);

        var briefing = RoleReadPathBriefingRenderer.Render(graph, "core-owner");

        Assert.Contains("Read path briefing for core-owner:", briefing);
        Assert.Contains("1. source.planning-index - docs/Source of Truth/planning-index.md", briefing);
        Assert.Contains("Purpose: Planning navigation.", briefing);
        Assert.Contains("Read when: starting a session", briefing);
        Assert.Contains("Purpose: Stable behavior contracts.", briefing);
    }

    [Fact]
    public void DocumentationCompilerLoadsMultipleDocumentsIntoGraphKeyedByDocumentId()
    {
        var documents = new[]
        {
            ValidSourceDoc("docs/Source of Truth/a.md", "source.a", "navigation"),
            ValidSourceDoc("docs/Source of Truth/b.md", "source.b", "testing", related: ["source.a"])
        };

        var graph = DocumentationGraph.Build(documents);

        Assert.True(graph.TryGetDocument("source.a", out var a));
        Assert.True(graph.TryGetDocument("source.b", out var b));
        Assert.Equal("docs/Source of Truth/a.md", a.Path);
        Assert.Equal(["source.a"], b.Metadata.Related);
    }

    [Fact]
    public void LinterReportsMissingRequiredMetadata()
    {
        var document = MarkdownFrontmatterParser.Parse("docs/missing.md", """
---
id: source.missing
kind: source-of-truth
status: active
owners: [core-owner]
audience: [core-owner]
lane: testing
read_when: [testing docs]
---
# Missing Title
""");

        var diagnostics = DocumentationLinter.Lint(DocumentationGraph.Build([document]), TestRepositoryRoot()).Diagnostics;

        Assert.Contains(diagnostics, d => d.Code == DocumentationDiagnosticCodes.MissingRequiredMetadata && d.Message.Contains("title"));
    }

    [Fact]
    public void LinterReportsDuplicateDocumentIds()
    {
        var first = ValidSourceDoc("docs/Source of Truth/a.md", "source.duplicate", "testing");
        var second = ValidSourceDoc("docs/Source of Truth/b.md", "source.duplicate", "testing");

        var diagnostics = DocumentationLinter.Lint(DocumentationGraph.Build([first, second]), TestRepositoryRoot()).Diagnostics;

        Assert.Contains(diagnostics, d => d.Code == DocumentationDiagnosticCodes.DuplicateDocumentId);
    }

    [Fact]
    public void LinterReportsUnresolvedRelatedReferences()
    {
        var document = ValidSourceDoc("docs/Source of Truth/a.md", "source.a", "testing", related: ["source.missing"]);

        var diagnostics = DocumentationLinter.Lint(DocumentationGraph.Build([document]), TestRepositoryRoot()).Diagnostics;

        Assert.Contains(diagnostics, d => d.Code == DocumentationDiagnosticCodes.UnresolvedDocumentReference && d.Message.Contains("source.missing"));
    }

    [Fact]
    public void LinterReportsSourceOfTruthDocumentMissingLaneOrReadWhen()
    {
        var document = MarkdownFrontmatterParser.Parse("docs/Source of Truth/a.md", """
---
id: source.a
title: Source A
kind: source-of-truth
status: active
owners: [core-owner]
audience: [core-owner]
---
# Source A
""");

        var diagnostics = DocumentationLinter.Lint(DocumentationGraph.Build([document]), TestRepositoryRoot()).Diagnostics;

        Assert.Contains(diagnostics, d => d.Code == DocumentationDiagnosticCodes.MissingSourceOfTruthMetadata && d.Message.Contains("lane"));
        Assert.Contains(diagnostics, d => d.Code == DocumentationDiagnosticCodes.MissingSourceOfTruthMetadata && d.Message.Contains("read_when"));
    }

    [Fact]
    public void LinterReportsUnknownOwnerAndAudienceRoles()
    {
        var document = MarkdownFrontmatterParser.Parse("docs/Source of Truth/a.md", """
---
id: source.a
title: Source A
kind: source-of-truth
status: active
owners: [unknown-owner]
audience: [core-owner, unknown-reader]
lane: testing
read_when: [testing docs]
---
# Source A
""");

        var diagnostics = DocumentationLinter.Lint(DocumentationGraph.Build([document]), TestRepositoryRoot()).Diagnostics;

        Assert.Contains(diagnostics, d => d.Code == DocumentationDiagnosticCodes.UnknownRole && d.Message.Contains("unknown-owner"));
        Assert.Contains(diagnostics, d => d.Code == DocumentationDiagnosticCodes.UnknownRole && d.Message.Contains("unknown-reader"));
    }

    [Fact]
    public void LinterAcceptsSmallValidSourceOfTruthFixtureSet()
    {
        var documents = new[]
        {
            ValidSourceDoc("docs/Source of Truth/planning-index.md", "source.planning-index", "navigation", owners: ["core-owner", "content-editor", "frontend-owner"]),
            ValidSourceDoc("docs/Source of Truth/invariants.md", "source.invariants", "invariant-trace", related: ["source.planning-index"]),
            ValidSourceDoc("docs/Source of Truth/Content-Authoring-Manual.md", "source.content-authoring-manual", "content-authoring", owners: ["content-editor"], audience: ["content-editor"], related: ["source.planning-index"])
        };

        var diagnostics = DocumentationLinter.Lint(DocumentationGraph.Build(documents), TestRepositoryRoot()).Diagnostics;

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void ContentEditorReadPathStartsWithNavigationAndContentAuthoringAndExcludesInvariantTraceByDefault()
    {
        var graph = DocumentationGraph.Build([
            ValidSourceDoc("docs/Source of Truth/planning-index.md", "source.planning-index", "navigation"),
            ValidSourceDoc("docs/Source of Truth/Content-Authoring-Manual.md", "source.content-authoring-manual", "content-authoring", owners: ["content-editor"], audience: ["content-editor"]),
            ValidSourceDoc("docs/Source of Truth/invariants.md", "source.invariants", "invariant-trace")
        ]);

        var path = RoleReadPathGenerator.Generate(graph, "content-editor");

        Assert.Equal(["source.planning-index", "source.content-authoring-manual"], path.Select(d => d.Metadata.Id));
    }

    [Fact]
    public void FrontendOwnerReadPathIncludesFrontendInvariantsStandardsAndDecisionsInOrder()
    {
        var graph = DocumentationGraph.Build([
            ValidSourceDoc("docs/Source of Truth/planning-index.md", "source.planning-index", "navigation"),
            ValidSourceDoc("docs/Source of Truth/Frontend-UX-Decisions.md", "source.frontend-ux-decisions", "frontend-ux-decisions", owners: ["frontend-owner"], audience: ["frontend-owner"]),
            ValidSourceDoc("docs/Source of Truth/Frontend-UX-Invariants.md", "source.frontend-ux-invariants", "frontend-ux-invariants", owners: ["frontend-owner"], audience: ["frontend-owner"]),
            ValidSourceDoc("docs/Source of Truth/Frontend-UX-Standards.md", "source.frontend-ux-standards", "frontend-ux-standards", owners: ["frontend-owner"], audience: ["frontend-owner"])
        ]);

        var path = RoleReadPathGenerator.Generate(graph, "frontend-owner");

        Assert.Equal([
            "source.planning-index",
            "source.frontend-ux-invariants",
            "source.frontend-ux-standards",
            "source.frontend-ux-decisions"
        ], path.Select(d => d.Metadata.Id));
    }

    [Fact]
    public void CoreOwnerReadPathIncludesInvariantsTestingCapabilityMatrixAndVerticalSliceNavigation()
    {
        var graph = DocumentationGraph.Build([
            ValidSourceDoc("docs/Source of Truth/planning-index.md", "source.planning-index", "navigation"),
            ValidSourceDoc("docs/Source of Truth/vertical-slice-map.md", "source.vertical-slice-map", "vertical-slice"),
            ValidSourceDoc("docs/Source of Truth/Engine-Editor-Capabilities.md", "source.engine-editor-capabilities", "capability-matrix"),
            ValidSourceDoc("docs/Source of Truth/testing-charter.md", "source.testing-charter", "testing"),
            ValidSourceDoc("docs/Source of Truth/invariants.md", "source.invariants", "invariant-trace")
        ]);

        var path = RoleReadPathGenerator.Generate(graph, "core-owner");

        Assert.Equal([
            "source.planning-index",
            "source.invariants",
            "source.testing-charter",
            "source.engine-editor-capabilities",
            "source.vertical-slice-map"
        ], path.Select(d => d.Metadata.Id));
    }

    [Fact]
    public void LinkPathValidationReportsMissingMarkdownAndCodeReferences()
    {
        var root = CreateTempRepositoryRoot();
        var document = MarkdownFrontmatterParser.Parse("docs/Source of Truth/a.md", """
---
id: source.a
title: Source A
kind: source-of-truth
status: active
owners: [core-owner]
audience: [core-owner]
lane: testing
read_when: [testing docs]
code_refs:
  - src/Missing.cs
---
# Source A

[Missing](../Missing.md)
""");

        var diagnostics = DocumentationLinter.Lint(DocumentationGraph.Build([document]), root).Diagnostics;

        Assert.Contains(diagnostics, d => d.Code == DocumentationDiagnosticCodes.UnresolvedMarkdownLink);
        Assert.Contains(diagnostics, d => d.Code == DocumentationDiagnosticCodes.UnresolvedPathReference && d.Message.Contains("src/Missing.cs"));
    }

    [Fact]
    public void MermaidGraphRendererOutputsMmdGraphWithDocumentNodesAndRelatedEdges()
    {
        var graph = DocumentationGraph.Build([
            ValidSourceDoc("docs/Source of Truth/planning-index.md", "source.planning-index", "navigation"),
            ValidSourceDoc("docs/Source of Truth/Content-Authoring-Manual.md", "source.content-authoring-manual", "content-authoring", related: ["source.planning-index"])
        ]);

        var mermaid = MermaidGraphRenderer.Render(graph);

        Assert.Contains("flowchart TD", mermaid);
        Assert.Contains("source_content_authoring_manual[\"Content Authoring Manual", mermaid);
        Assert.Contains("source_planning_index[\"Planning Index", mermaid);
        Assert.Contains("source_content_authoring_manual -->|behavior/test trace| source_planning_index", mermaid);
    }

    [Fact]
    public void DocumentationGraphJsonRendererOutputsDocumentsAndDiscoveryEdges()
    {
        var graph = DocumentationGraph.Build([
            SourceDocWithPurpose("docs/Source of Truth/planning-index.md", "source.planning-index", "navigation", "Planning navigation.", ["starting a session"]),
            SourceDocWithPurpose("docs/Source of Truth/invariants.md", "source.invariants", "invariant-trace", "Stable behavior contracts.", ["changing behavior"], related: ["source.planning-index"])
        ]);

        var json = DocumentationGraphJsonRenderer.Render(graph);

        Assert.Contains("\"documents\"", json);
        Assert.Contains("\"id\": \"source.invariants\"", json);
        Assert.Contains("\"purpose\": \"Stable behavior contracts.\"", json);
        Assert.Contains("\"edges\"", json);
        Assert.Contains("\"from\": \"source.invariants\"", json);
        Assert.Contains("\"to\": \"source.planning-index\"", json);
    }

    [Fact]
    public void PlanningFreshnessAnalyzerFlagsNowItemsWithoutLastUpdatedMarker()
    {
        var board = MarkdownFrontmatterParser.Parse("docs/Plans/Core-Rolling-Board.md", """
---
id: plan.core-rolling-board
title: Core Rolling Board
kind: rolling-board
status: active
owners: [core-owner]
audience: [core-owner]
lane: core-rolling-board
related: [source.planning-index]
---
# Core Rolling Board

## Now

### Improve documentation mapping

**User story:** As a maintainer, I can orient quickly.

## Next

### Later item

**Last updated:** 2026-08-23
""");

        var diagnostics = PlanningFreshnessAnalyzer.Analyze(DocumentationGraph.Build([board]));

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DocumentationDiagnosticCodes.PlanningItemNeedsUpdateMarker, diagnostic.Code);
        Assert.Contains("Improve documentation mapping", diagnostic.Message);
    }

    [Fact]
    public void LinterReportsInvalidTruthRankAndUnknownTruthDomain()
    {
        var document = MarkdownFrontmatterParser.Parse("docs/Source of Truth/a.md", """
---
id: source.a
title: Source A
kind: source-of-truth
status: active
owners: [core-owner]
audience: [core-owner]
lane: testing
read_when: [testing docs]
truth_rank: not-a-number
truth_domains: [runtime-behavior, unknown-domain]
---
# Source A
""");

        var diagnostics = DocumentationLinter.Lint(DocumentationGraph.Build([document]), TestRepositoryRoot()).Diagnostics;

        Assert.Contains(diagnostics, d => d.Code == DocumentationDiagnosticCodes.InvalidTruthRank);
        Assert.Contains(diagnostics, d => d.Code == DocumentationDiagnosticCodes.UnknownTruthDomain && d.Message.Contains("unknown-domain"));
    }

    [Fact]
    public void TraversalProfileGeneratorReturnsTaskPathAndCoverageMetrics()
    {
        var graph = DocumentationGraph.Build([
            ValidSourceDoc("docs/Source of Truth/planning-index.md", "source.planning-index", "navigation"),
            ValidSourceDoc("docs/Source of Truth/invariants.md", "source.invariants", "invariant-trace"),
            ValidSourceDoc("docs/Source of Truth/testing-charter.md", "source.testing-charter", "testing"),
            ValidSourceDoc("docs/Source of Truth/Engine-Editor-Capabilities.md", "source.engine-editor-capabilities", "capability-matrix"),
            ValidSourceDoc("docs/Source of Truth/vertical-slice-map.md", "source.vertical-slice-map", "vertical-slice"),
            ValidSourceDoc("docs/Source of Truth/Content-Authoring-Manual.md", "source.content-authoring-manual", "content-authoring", owners: ["content-editor"], audience: ["content-editor"])
        ]);

        var traversal = TraversalProfileGenerator.Generate(graph, "core-stable-behavior-change");
        var metrics = TraversalProfileGenerator.GenerateCoverageMetrics(graph);

        Assert.Equal([
            "source.planning-index",
            "source.invariants",
            "source.testing-charter",
            "source.engine-editor-capabilities",
            "source.vertical-slice-map"
        ], traversal.Documents.Select(d => d.Metadata.Id));
        Assert.Contains(metrics, metric => metric.DocumentId == "source.planning-index" && metric.ProfileCount >= 1);
    }

    [Fact]
    public void TraversalMermaidRendererOutputsTaskPathGraph()
    {
        var graph = DocumentationGraph.Build([
            ValidSourceDoc("docs/Source of Truth/planning-index.md", "source.planning-index", "navigation"),
            ValidSourceDoc("docs/Source of Truth/Content-Authoring-Manual.md", "source.content-authoring-manual", "content-authoring", owners: ["content-editor"], audience: ["content-editor"])
        ]);

        var traversal = TraversalProfileGenerator.Generate(graph, "content-authoring");
        var mermaid = TraversalMermaidRenderer.Render(traversal);

        Assert.Contains("flowchart LR", mermaid);
        Assert.Contains("task_content_authoring((content-authoring))", mermaid);
        Assert.Contains("task_content_authoring --> source_planning_index", mermaid);
        Assert.Contains("source_planning_index --> source_content_authoring_manual", mermaid);
    }

    [Fact]
    public void MermaidClassDiagramRendererOutputsMetadataRichDocumentNodes()
    {
        var graph = DocumentationGraph.Build([
            ValidSourceDoc("docs/Source of Truth/invariants.md", "source.invariants", "invariant-trace")
        ]);

        var mermaid = MermaidClassDiagramRenderer.Render(graph);

        Assert.Contains("classDiagram", mermaid);
        Assert.Contains("class source_invariants", mermaid);
        Assert.Contains("kind source-of-truth", mermaid);
        Assert.Contains("lane invariant-trace", mermaid);
        Assert.Contains("owners core-owner", mermaid);
        Assert.Contains("truth_rank 30", mermaid);
        Assert.Contains("domains stable-contract", mermaid);
    }

    [Fact]
    public void MermaidGraphRendererCanHighlightTraversalProfileNodes()
    {
        var graph = DocumentationGraph.Build([
            ValidSourceDoc("docs/Source of Truth/planning-index.md", "source.planning-index", "navigation"),
            ValidSourceDoc("docs/Source of Truth/Content-Authoring-Manual.md", "source.content-authoring-manual", "content-authoring", owners: ["content-editor"], audience: ["content-editor"]),
            ValidSourceDoc("docs/Source of Truth/invariants.md", "source.invariants", "invariant-trace")
        ]);
        var traversal = TraversalProfileGenerator.Generate(graph, "content-authoring");

        var mermaid = MermaidGraphRenderer.Render(graph, traversal);

        Assert.Contains("classDef traversal", mermaid);
        Assert.Contains("class source_planning_index,source_content_authoring_manual traversal", mermaid);
        Assert.DoesNotContain("class source_invariants traversal", mermaid);
    }

    [Fact]
    public void DocumentationGraphDiscoveryEdgesPointFromHigherTruthToLowerTruthAndDeduplicateReciprocalLinks()
    {
        var highTruth = SourceDocWithTruthRank("docs/Source of Truth/invariants.md", "source.invariants", "invariant-trace", 10, related: ["plan.archived-reference"]);
        var lowTruth = SourceDocWithTruthRank("docs/Plans/archived-reference.md", "plan.archived-reference", "planning", 90, kind: "archived", status: "archived", related: ["source.invariants"]);

        var graph = DocumentationGraph.Build([highTruth, lowTruth]);

        var edge = Assert.Single(graph.GetDiscoveryEdges());
        Assert.Equal("source.invariants", edge.From.Metadata.Id);
        Assert.Equal("plan.archived-reference", edge.To.Metadata.Id);
    }

    [Fact]
    public void MermaidRenderersUseNormalizedDiscoveryEdges()
    {
        var highTruth = SourceDocWithTruthRank("docs/Source of Truth/invariants.md", "source.invariants", "invariant-trace", 10);
        var lowTruth = SourceDocWithTruthRank("docs/Plans/archive.md", "plan.archive", "planning", 90, kind: "archived", status: "archived", related: ["source.invariants"]);
        var graph = DocumentationGraph.Build([highTruth, lowTruth]);

        var flowchart = MermaidGraphRenderer.Render(graph);
        var classDiagram = MermaidClassDiagramRenderer.Render(graph);

        Assert.Contains("source_invariants -->|historical context| plan_archive", flowchart);
        Assert.DoesNotContain("plan_archive -->|", flowchart);
        Assert.Contains("source_invariants --> plan_archive : historical context", classDiagram);
        Assert.DoesNotContain("plan_archive --> source_invariants : related", classDiagram);
    }

    [Theory]
    [InlineData("capability-support", "capability support")]
    [InlineData("authorability", "authoring guidance")]
    [InlineData("gap-workflow", "gap workflow")]
    [InlineData("action-logic", "action outcome logic")]
    [InlineData("frontend-boundary", "frontend boundary")]
    [InlineData("frontend-presentation", "frontend presentation")]
    [InlineData("frontend-rationale", "decision rationale")]
    [InlineData("implementation-navigation", "implementation path")]
    [InlineData("planning-priority", "planning context")]
    [InlineData("stable-contract", "behavior/test trace")]
    public void DiscoveryEdgeLabelIsDerivedFromTargetTruthDomain(string targetDomain, string expectedLabel)
    {
        var source = DocWithTruthMetadata("docs/source.md", "source.high", truthRank: 10, truthDomains: ["stable-contract"], related: ["source.target"]);
        var target = DocWithTruthMetadata("docs/target.md", "source.target", truthRank: 30, truthDomains: [targetDomain]);

        var edge = Assert.Single(DocumentationGraph.Build([source, target]).GetDiscoveryEdges());

        Assert.Equal(expectedLabel, edge.Label);
    }

    [Fact]
    public void MermaidFlowchartEdgesUseDerivedLabels()
    {
        var source = DocWithTruthMetadata("docs/source.md", "source.high", truthRank: 10, truthDomains: ["stable-contract"], related: ["source.authoring"]);
        var target = DocWithTruthMetadata("docs/authoring.md", "source.authoring", truthRank: 30, truthDomains: ["authorability"]);

        var mermaid = MermaidGraphRenderer.Render(DocumentationGraph.Build([source, target]));

        Assert.Contains("source_high -->|authoring guidance| source_authoring", mermaid);
    }

    private static MarkdownDocument ValidSourceDoc(
        string path,
        string id,
        string lane,
        string[]? owners = null,
        string[]? audience = null,
        string[]? related = null)
    {
        var relatedText = related is { Length: > 0 }
            ? $"related: [{string.Join(", ", related)}]"
            : "related: []";

        var title = id switch
        {
            "source.planning-index" => "Planning Index",
            "source.content-authoring-manual" => "Content Authoring Manual",
            _ => id
        };

        return MarkdownFrontmatterParser.Parse(path, $$"""
---
id: {{id}}
title: {{title}}
kind: source-of-truth
status: active
owners: [{{string.Join(", ", owners ?? ["core-owner"])}}]
audience: [{{string.Join(", ", audience ?? ["core-owner"])}}]
lane: {{lane}}
read_when: [testing docs]
truth_rank: 30
truth_domains: [stable-contract]
{{relatedText}}
---
# {{id}}
""");
    }

    private static MarkdownDocument SourceDocWithPurpose(
        string path,
        string id,
        string lane,
        string purpose,
        string[] readWhen,
        string[]? related = null)
    {
        var relatedText = related is { Length: > 0 }
            ? $"related: [{string.Join(", ", related)}]"
            : "related: []";

        return MarkdownFrontmatterParser.Parse(path, $$"""
---
id: {{id}}
title: {{id}}
kind: source-of-truth
status: active
owners: [core-owner]
audience: [core-owner]
lane: {{lane}}
purpose: {{purpose}}
read_when: [{{string.Join(", ", readWhen)}}]
truth_rank: 30
truth_domains: [stable-contract]
{{relatedText}}
---
# {{id}}
""");
    }

    private static string TestRepositoryRoot() => AppContext.BaseDirectory;

    private static MarkdownDocument SourceDocWithTruthRank(
        string path,
        string id,
        string lane,
        int truthRank,
        string kind = "source-of-truth",
        string status = "active",
        string[]? related = null)
    {
        var relatedText = related is { Length: > 0 }
            ? $"related: [{string.Join(", ", related)}]"
            : "related: []";

        return MarkdownFrontmatterParser.Parse(path, $$"""
---
id: {{id}}
title: {{id}}
kind: {{kind}}
status: {{status}}
owners: [core-owner]
audience: [core-owner]
lane: {{lane}}
read_when: [testing docs]
truth_rank: {{truthRank}}
truth_domains: [stable-contract]
{{relatedText}}
---
# {{id}}
""");
    }

    private static MarkdownDocument DocWithTruthMetadata(
        string path,
        string id,
        int truthRank,
        string[] truthDomains,
        string[]? related = null,
        string kind = "source-of-truth",
        string status = "active")
    {
        var relatedText = related is { Length: > 0 }
            ? $"related: [{string.Join(", ", related)}]"
            : "related: []";

        return MarkdownFrontmatterParser.Parse(path, $$"""
---
id: {{id}}
title: {{id}}
kind: {{kind}}
status: {{status}}
owners: [core-owner]
audience: [core-owner]
lane: testing
read_when: [testing docs]
truth_rank: {{truthRank}}
truth_domains: [{{string.Join(", ", truthDomains)}}]
{{relatedText}}
---
# {{id}}
""");
    }

    private static string CreateTempRepositoryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ggg-doc-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "docs", "Source of Truth"));
        Directory.CreateDirectory(Path.Combine(root, "src"));
        return root;
    }
}
