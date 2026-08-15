using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class ContentWorkspaceCompilerTests
{
    [Fact]
    public void DebugRoomWorkspaceValidatesInitialCanonicalComposition()
    {
        var workspace = LoadInitialDebugRoomWorkspace();

        var result = ContentCompiler.Compile(workspace);

        Assert.True(result.Validation.IsValid, string.Join(Environment.NewLine, result.Validation.Errors));
        Assert.NotNull(result.Registry);
        Assert.Contains(result.Symbols, symbol => symbol.Kind == ContentSymbolKind.EntityTemplate && symbol.Id == "debugPlayer" && symbol.DocumentId == "canonical.creatures.debug-player");
        Assert.Contains(result.Symbols, symbol => symbol.Kind == ContentSymbolKind.ActionPlan && symbol.Id == "debugPlayerActionPlan" && symbol.DocumentId == "canonical.creatures.debug-player");
        Assert.Contains(result.Symbols, symbol => symbol.Kind == ContentSymbolKind.EntityTemplate && symbol.Id == "scrap" && symbol.DocumentId == "canonical.substrates.scrap");
        Assert.Contains(result.Symbols, symbol => symbol.Kind == ContentSymbolKind.EntityTemplate && symbol.Id == "chest" && symbol.DocumentId == "canonical.objects.chest");
        Assert.Contains(result.Symbols, symbol => symbol.Kind == ContentSymbolKind.EntityTemplate && symbol.Id == "bag" && symbol.DocumentId == "canonical.objects.bag");
        Assert.Contains(result.Symbols, symbol => symbol.Kind == ContentSymbolKind.EntityTemplate && symbol.Id == "pushBlock" && symbol.DocumentId == "canonical.objects.push-block");
        Assert.Contains(result.Symbols, symbol => symbol.Kind == ContentSymbolKind.EntityTemplate && symbol.Id == "debugRoomRoot" && symbol.DocumentId == "canonical.spaces.debug-room-root");
        Assert.Contains(result.Symbols, symbol => symbol.Kind == ContentSymbolKind.Scenario && symbol.Id == "debug-room" && symbol.DocumentId == "debug.debug-room");
        Assert.All(result.WorkspaceDocuments.Where(document => document.SourceKind == ContentWorkspaceSourceKind.Canonical), document => Assert.True(document.IsReadOnly));
    }

    [Fact]
    public void DebugRoomWorkspaceScenarioMaterializesAndRunsInitialRoom()
    {
        var workspace = LoadInitialDebugRoomWorkspace();

        var materialization = ScenarioMaterializer.Materialize(workspace, "debug-room");
        var run = ScenarioRunService.Run(workspace, new PersistedScenarioRunRequest("debug-room", TurnCount: 0));

        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Equal(new EntityTemplateId("debugRoomRoot"), materialization.ScenarioRootEntityTemplateId);
        Assert.Null(materialization.PlayerEntityId);
        Assert.Null(materialization.PlayerLocation);
        Assert.Equal([new EntityId("debugPlayer")], materialization.PlayerControls["player-1"]);
        Assert.Equal(new PlaneCoord(new PlaneId("debugStartRoom"), new GridCoord(4, 3)), materialization.World.GetEntityLocation(new EntityId("debugPlayer")));
        for (var index = 1; index <= 5; index++)
        {
            Assert.True(materialization.World.Entities.ContainsKey(new EntityId($"debugScrap{index}")));
        }

        Assert.Equal(new PlaneCoord(new PlaneId("debugStartRoom"), new GridCoord(1, 1)), materialization.World.GetEntityLocation(new EntityId("debugChest")));
        Assert.Equal(new PlaneCoord(new PlaneId("debugStartRoom"), new GridCoord(7, 1)), materialization.World.GetEntityLocation(new EntityId("debugBag")));
        Assert.Equal(new PlaneCoord(new PlaneId("debugStartRoom"), new GridCoord(4, 4)), materialization.World.GetEntityLocation(new EntityId("debugPushBlock")));
        var chest = materialization.Registry.EntityTemplates[new EntityTemplateId("chest")];
        var bag = materialization.Registry.EntityTemplates[new EntityTemplateId("bag")];
        var pushBlock = materialization.Registry.EntityTemplates[new EntityTemplateId("pushBlock")];
        Assert.True(chest.Bulk > materialization.Registry.EntityTemplates[new EntityTemplateId("debugPlayer")].Aperture);
        Assert.True(bag.Bulk <= materialization.Registry.EntityTemplates[new EntityTemplateId("debugPlayer")].Aperture);
        Assert.True(materialization.Registry.EntityTemplates[new EntityTemplateId("debugPlayer")].Bulk > bag.Aperture);
        Assert.True(pushBlock.Bulk <= materialization.Registry.EntityTemplates[new EntityTemplateId("debugPlayer")].Aperture);
        Assert.False(materialization.Registry.EntityTemplates[new EntityTemplateId("debugPlayer")].Bulk > pushBlock.Aperture);

        Assert.Empty(run.ValidationDiagnostics);
        Assert.Contains("Run mode: Workspace persisted scenario simulation", run.SetupLines);
        Assert.Contains(run.FinalStateLines, line => line.Contains("Debug Start Room: scenarioRoot(1,1)", StringComparison.Ordinal));
    }

    [Fact]
    public void ContentWorkspaceCompilerCompilesSingleDocumentThroughCompatibilityAdapter()
    {
        var document = LoadMinimalValidDocument();
        var workspace = new ContentWorkspace([
            new ContentWorkspaceDocument(
                document,
                DocumentId: "workspace-doc",
                SourcePath: "content/workspace.yaml")
        ]);

        var workspaceResult = ContentCompiler.Compile(workspace);
        var adapterResult = ContentCompiler.Compile(
            document,
            new ContentCompileOptions("workspace-doc", "content/workspace.yaml"));

        Assert.NotNull(workspaceResult.Registry);
        Assert.True(workspaceResult.Validation.IsValid, string.Join(Environment.NewLine, workspaceResult.Validation.Errors));
        Assert.Equal(adapterResult.Validation.Diagnostics, workspaceResult.Validation.Diagnostics);
        Assert.Equal(adapterResult.Symbols, workspaceResult.Symbols);
        Assert.Equal(adapterResult.References, workspaceResult.References);
        Assert.True(workspaceResult.Registry.EntityTemplates.ContainsKey(new EntityTemplateId("room")));
    }

    [Fact]
    public void ContentWorkspaceCompilerCarriesDocumentIdentityAndSourcePath()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              actor:
                name: Actor
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 1
                carryingCapacity: 1
                defaultActionPlanId: missingPlan
            presentations:
              actor: { glyph: a, color: Green }
            actionPlans: {}
            """);
        var workspace = new ContentWorkspace([
            new ContentWorkspaceDocument(
                document,
                DocumentId: "canonical.creatures",
                SourcePath: "Content/Canonical/creatures.yaml",
                SourceKind: ContentWorkspaceSourceKind.Canonical,
                IsReadOnly: true)
        ]);

        var result = ContentCompiler.Compile(workspace);

        var workspaceDocument = Assert.Single(result.WorkspaceDocuments);
        Assert.Equal("canonical.creatures", workspaceDocument.DocumentId);
        Assert.Equal("Content/Canonical/creatures.yaml", workspaceDocument.SourcePath);
        Assert.Equal(ContentWorkspaceSourceKind.Canonical, workspaceDocument.SourceKind);
        Assert.True(workspaceDocument.IsReadOnly);

        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingActionPlanReference);
        Assert.Equal("canonical.creatures", diagnostic.DocumentId);
        Assert.Equal("Content/Canonical/creatures.yaml", diagnostic.SourcePath);

        Assert.Contains(result.Symbols, symbol =>
            symbol.Kind == ContentSymbolKind.EntityTemplate
            && symbol.Id == "actor"
            && symbol.DocumentId == "canonical.creatures"
            && symbol.SourcePath == "Content/Canonical/creatures.yaml");
    }

    [Fact]
    public void ContentWorkspaceCompilerAllowsTransientDocumentWithoutSourcePath()
    {
        var document = LoadMinimalValidDocument();
        var workspace = new ContentWorkspace([
            new ContentWorkspaceDocument(
                document,
                DocumentId: "transient-debug-room",
                SourceKind: ContentWorkspaceSourceKind.Test)
        ]);

        var result = ContentCompiler.Compile(workspace);

        Assert.True(result.Validation.IsValid, string.Join(Environment.NewLine, result.Validation.Errors));
        var workspaceDocument = Assert.Single(result.WorkspaceDocuments);
        Assert.Equal("transient-debug-room", workspaceDocument.DocumentId);
        Assert.Null(workspaceDocument.SourcePath);
        Assert.Equal(ContentWorkspaceSourceKind.Test, workspaceDocument.SourceKind);
        Assert.All(result.Symbols, symbol =>
        {
            Assert.Equal("transient-debug-room", symbol.DocumentId);
            Assert.Null(symbol.SourcePath);
        });
    }

    [Fact]
    public void ContentWorkspaceCompilerBuildsSymbolsAcrossDocuments()
    {
        var creatures = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
            presentations:
              slime: { glyph: s, color: Green }
            actionPlans: {}
            """);
        var plans = EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              slimeDefault:
                id: slimeDefault
                behavior:
                  steps:
                    - kind: MoveFacing
            """);
        var workspace = new ContentWorkspace([
            new ContentWorkspaceDocument(creatures, "canonical.creatures", "Content/Canonical/creatures.yaml", ContentWorkspaceSourceKind.Canonical, IsReadOnly: true),
            new ContentWorkspaceDocument(plans, "canonical.creature-plans", "Content/Canonical/creature-plans.yaml", ContentWorkspaceSourceKind.Canonical, IsReadOnly: true)
        ]);

        var result = ContentCompiler.Compile(workspace);

        Assert.True(result.Validation.IsValid, string.Join(Environment.NewLine, result.Validation.Errors));
        Assert.NotNull(result.Registry);
        Assert.Equal(2, result.WorkspaceDocuments.Count);
        Assert.Contains(result.Symbols, symbol =>
            symbol.Kind == ContentSymbolKind.EntityTemplate
            && symbol.Id == "slime"
            && symbol.DocumentId == "canonical.creatures"
            && symbol.SourcePath == "Content/Canonical/creatures.yaml");
        Assert.Contains(result.Symbols, symbol =>
            symbol.Kind == ContentSymbolKind.ActionPlan
            && symbol.Id == "slimeDefault"
            && symbol.DocumentId == "canonical.creature-plans"
            && symbol.SourcePath == "Content/Canonical/creature-plans.yaml");
    }

    [Fact]
    public void ContentWorkspaceCompilerReportsDuplicateSameKindSymbolsAcrossDocuments()
    {
        var canonical = LoadSingleTemplateDocument("slime", "Slime");
        var user = LoadSingleTemplateDocument("slime", "User Slime");
        var workspace = new ContentWorkspace([
            new ContentWorkspaceDocument(canonical, "canonical.creatures", "Content/Canonical/creatures.yaml", ContentWorkspaceSourceKind.Canonical, IsReadOnly: true),
            new ContentWorkspaceDocument(user, "user.creatures", "Content/User/creatures.yaml", ContentWorkspaceSourceKind.User)
        ]);

        var result = ContentCompiler.Compile(workspace);

        Assert.False(result.Validation.IsValid);
        var diagnostics = result.Diagnostics
            .Where(diagnostic =>
                diagnostic.Code == ContentDiagnosticCode.DuplicateSymbolDeclaration
                && diagnostic.SymbolKind == nameof(ContentSymbolKind.EntityTemplate))
            .ToList();
        Assert.Equal(2, diagnostics.Count);
        Assert.All(diagnostics, diagnostic =>
        {
            Assert.Equal(nameof(ContentSymbolKind.EntityTemplate), diagnostic.SymbolKind);
            Assert.Equal("slime", diagnostic.SymbolId);
        });
        Assert.Contains(diagnostics, diagnostic => diagnostic.DocumentId == "canonical.creatures" && diagnostic.SourcePath == "Content/Canonical/creatures.yaml");
        Assert.Contains(diagnostics, diagnostic => diagnostic.DocumentId == "user.creatures" && diagnostic.SourcePath == "Content/User/creatures.yaml");
    }

    [Fact]
    public void ContentWorkspaceCompilerAllowsSameIdAcrossDifferentSymbolKinds()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              shared:
                name: Shared Entity
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
            presentations:
              shared: { glyph: s, color: Green }
            actionPlans:
              shared:
                id: shared
                behavior:
                  steps:
                    - kind: MoveFacing
            """);
        var workspace = new ContentWorkspace([
            new ContentWorkspaceDocument(document, "same-id", "Content/Test/same-id.yaml", ContentWorkspaceSourceKind.Test)
        ]);

        var result = ContentCompiler.Compile(workspace);

        Assert.True(result.Validation.IsValid, string.Join(Environment.NewLine, result.Validation.Errors));
        Assert.Contains(result.Symbols, symbol => symbol.Kind == ContentSymbolKind.EntityTemplate && symbol.Id == "shared");
        Assert.Contains(result.Symbols, symbol => symbol.Kind == ContentSymbolKind.ActionPlan && symbol.Id == "shared");
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.DuplicateSymbolDeclaration);
    }

    [Fact]
    public void ContentWorkspaceCompilerResolvesTemplateDefaultActionPlanAcrossDocuments()
    {
        var templates = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
                defaultActionPlanId: slimeDefault
            presentations:
              slime: { glyph: s, color: Green }
            actionPlans: {}
            """);
        var plans = EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              slimeDefault:
                id: slimeDefault
                behavior:
                  steps:
                    - kind: MoveFacing
            """);

        var result = ContentCompiler.Compile(new ContentWorkspace([
            new ContentWorkspaceDocument(templates, "canonical.creatures", "creatures.yaml"),
            new ContentWorkspaceDocument(plans, "canonical.plans", "plans.yaml")
        ]));

        Assert.True(result.Validation.IsValid, string.Join(Environment.NewLine, result.Validation.Errors));
        Assert.Contains(result.References, reference =>
            reference.Kind == ContentReferenceKind.DefaultActionPlan
            && reference.SourceId == "slime"
            && reference.TargetId == "slimeDefault"
            && reference.Resolution == ContentReferenceResolution.Resolved);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingActionPlanReference);
    }

    [Fact]
    public void ContentWorkspaceCompilerResolvesScenarioRootAndPlayerTemplatesAcrossDocuments()
    {
        var scenario = EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans: {}
            scenarios:
              debugRoom:
                name: Debug Room
                scenarioRootEntityTemplateId: debugRoot
                playerEntityTemplateId: player
                playerEntityId: player1
                playerStart: { x: 0, y: 0 }
            """);
        var templates = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              debugRoot:
                name: Debug Root
                inventoryWidth: 2
                inventoryHeight: 2
                weight: 100
                carryingCapacity: 100
              player:
                name: Player
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
            presentations:
              debugRoot: { glyph: '#', color: Gray }
              player: { glyph: '@', color: White }
            actionPlans: {}
            """);

        var result = ContentCompiler.Compile(new ContentWorkspace([
            new ContentWorkspaceDocument(scenario, "debug.scenario", "debug-room.yaml"),
            new ContentWorkspaceDocument(templates, "debug.templates", "templates.yaml")
        ]));

        Assert.True(result.Validation.IsValid, string.Join(Environment.NewLine, result.Validation.Errors));
        Assert.Contains(result.References, reference => reference.Kind == ContentReferenceKind.ScenarioRootTemplate && reference.TargetId == "debugRoot" && reference.Resolution == ContentReferenceResolution.Resolved);
        Assert.Contains(result.References, reference => reference.Kind == ContentReferenceKind.ScenarioPlayerTemplate && reference.TargetId == "player" && reference.Resolution == ContentReferenceResolution.Resolved);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidScenarioDefinition);
    }

    [Fact]
    public void ContentWorkspaceCompilerResolvesPresentationAndPaletteAcrossDocuments()
    {
        var templates = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
            presentations:
              slime:
                presentationId: creature.slime
                paletteId: palette.slime
                glyph: s
                color: Green
            actionPlans: {}
            """);
        var presentationCatalog = EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            presentationCatalog:
              creature.slime:
                name: Slime
            palettes:
              palette.slime:
                name: Slime Palette
            actionPlans: {}
            """);

        var result = ContentCompiler.Compile(new ContentWorkspace([
            new ContentWorkspaceDocument(templates, "canonical.creatures", "creatures.yaml"),
            new ContentWorkspaceDocument(presentationCatalog, "canonical.presentation", "presentation.yaml")
        ]));

        Assert.True(result.Validation.IsValid, string.Join(Environment.NewLine, result.Validation.Errors));
        Assert.Contains(result.References, reference => reference.Kind == ContentReferenceKind.PresentationId && reference.TargetId == "creature.slime" && reference.Resolution == ContentReferenceResolution.Resolved);
        Assert.Contains(result.References, reference => reference.Kind == ContentReferenceKind.PaletteId && reference.TargetId == "palette.slime" && reference.Resolution == ContentReferenceResolution.Resolved);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code is ContentDiagnosticCode.UnknownPresentationId or ContentDiagnosticCode.UnknownPaletteId);
    }

    [Fact]
    public void ContentWorkspaceCompilerReportsAmbiguousReferencesAcrossDocuments()
    {
        var template = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
                defaultActionPlanId: sharedPlan
            presentations:
              slime: { glyph: s, color: Green }
            actionPlans: {}
            """);
        var planA = LoadSingleActionPlanDocument("sharedPlan");
        var planB = LoadSingleActionPlanDocument("sharedPlan");

        var result = ContentCompiler.Compile(new ContentWorkspace([
            new ContentWorkspaceDocument(template, "canonical.creatures", "creatures.yaml"),
            new ContentWorkspaceDocument(planA, "canonical.plan-a", "plan-a.yaml"),
            new ContentWorkspaceDocument(planB, "user.plan-b", "plan-b.yaml")
        ]));

        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.References, reference =>
            reference.Kind == ContentReferenceKind.DefaultActionPlan
            && reference.TargetId == "sharedPlan"
            && reference.Resolution == ContentReferenceResolution.Ambiguous);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == ContentDiagnosticCode.AmbiguousSymbolReference
            && diagnostic.DocumentId == "canonical.creatures"
            && diagnostic.SymbolKind == nameof(ContentSymbolKind.ActionPlan)
            && diagnostic.SymbolId == "sharedPlan");
    }

    [Fact]
    public void ScenarioMaterializerMaterializesPersistedScenarioFromWorkspaceDependencies()
    {
        var workspace = CreatePlayableWorkspace();

        var materialization = ScenarioMaterializer.Materialize(workspace, "debugRoom");

        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Equal("debugRoom", materialization.ScenarioId);
        Assert.Equal(new EntityTemplateId("debugRoot"), materialization.ScenarioRootEntityTemplateId);
        Assert.Equal(new EntityTemplateId("player"), materialization.PlayerEntityTemplateId);
        Assert.Equal(new EntityId("debugPlayer"), materialization.PlayerEntityId);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(0, 0)), materialization.PlayerLocation);
        Assert.True(materialization.Registry.EntityTemplates.ContainsKey(new EntityTemplateId("debugRoot")));
        Assert.True(materialization.Registry.EntityTemplates.ContainsKey(new EntityTemplateId("player")));
    }

    [Fact]
    public void ScenarioMaterializerReportsWorkspaceMissingReferencesWithSourceDocuments()
    {
        var scenario = EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans: {}
            scenarios:
              missingRoom:
                name: Missing Room
                scenarioRootEntityTemplateId: missingRoot
            """);
        var workspace = new ContentWorkspace([
            new ContentWorkspaceDocument(scenario, "debug.scenario", "debug-room.yaml")
        ]);

        var materialization = ScenarioMaterializer.Materialize(workspace, "missingRoom");

        Assert.False(materialization.CanPlay);
        Assert.Contains(materialization.ValidationDiagnostics, diagnostic =>
            diagnostic.Contains("debug.scenario", StringComparison.Ordinal)
            && diagnostic.Contains("debug-room.yaml", StringComparison.Ordinal)
            && diagnostic.Contains("missingRoot", StringComparison.Ordinal));
    }

    [Fact]
    public void PlayableScenarioLauncherBuildsSessionFromWorkspaceScenario()
    {
        var workspace = CreatePlayableWorkspace();

        var session = PlayableScenarioLauncher.CreateFromWorkspace(workspace, "debugRoom");

        Assert.True(session.CanPlay, string.Join(Environment.NewLine, session.ValidationDiagnostics));
        Assert.Equal("debugRoom", session.ScenarioId);
        Assert.Equal("Debug Room", session.Name);
        Assert.Equal(new EntityId("debugPlayer"), session.PlayerEntityId);
        Assert.Equal(new PlaneId("scenarioRoot"), session.ActivePlaneId);
        Assert.Equal([new EntityId("debugPlayer")], session.PlayerControls["player-1"]);
    }

    [Fact]
    public void ContentWorkspaceEditorRejectsProtectedDocumentMutationByDefault()
    {
        var protectedDocument = LoadSingleTemplateDocument("slime", "Slime");
        var workspaceDocument = new ContentWorkspaceDocument(
            protectedDocument,
            "canonical.creatures",
            "creatures.yaml",
            ContentWorkspaceSourceKind.Canonical,
            IsReadOnly: true);
        var editor = new ContentWorkspaceEditor(new ContentWorkspace([workspaceDocument]));

        var result = editor.UpdateEntityPreset(
            new EntityTemplateId("slime"),
            new EntityTemplate("Edited Slime", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 0),
            new EntityPresentation('S', PresentationColor.Green));

        Assert.False(result.IsSuccess);
        Assert.False(workspaceDocument.IsDirty);
        Assert.False(workspaceDocument.HasProtectedMutation);
        Assert.Equal("Slime", protectedDocument.EntityTemplates["slime"].Name);
    }

    [Fact]
    public void ContentWorkspaceEditorAllowsProtectedDocumentMutationWhenCurationPolicyIsExplicit()
    {
        var protectedDocument = LoadSingleTemplateDocument("slime", "Slime");
        var workspaceDocument = new ContentWorkspaceDocument(
            protectedDocument,
            "canonical.creatures",
            "creatures.yaml",
            ContentWorkspaceSourceKind.Canonical,
            IsReadOnly: true);
        var editor = new ContentWorkspaceEditor(new ContentWorkspace([workspaceDocument]));

        var result = editor.UpdateEntityPreset(
            new EntityTemplateId("slime"),
            new EntityTemplate("Curated Slime", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 0),
            new EntityPresentation('S', PresentationColor.Green),
            ContentWorkspaceMutationPolicy.AllowProtectedDocumentMutation);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(result.MutatedProtectedDocument);
        Assert.True(workspaceDocument.IsDirty);
        Assert.True(workspaceDocument.HasProtectedMutation);
        Assert.Equal("Curated Slime", protectedDocument.EntityTemplates["slime"].Name);
    }

    [Fact]
    public void ContentWorkspaceSaveSkipsProtectedDocumentsByDefault()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(directory.FullName, "creatures.yaml");
            var protectedDocument = LoadSingleTemplateDocument("slime", "Slime");
            File.WriteAllText(path, protectedDocument.SaveYaml());
            var workspaceDocument = new ContentWorkspaceDocument(
                protectedDocument,
                "canonical.creatures",
                path,
                ContentWorkspaceSourceKind.Canonical,
                IsReadOnly: true);
            var editor = new ContentWorkspaceEditor(new ContentWorkspace([workspaceDocument]));
            editor.UpdateEntityPreset(
                new EntityTemplateId("slime"),
                new EntityTemplate("Curated Slime", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 0),
                new EntityPresentation('S', PresentationColor.Green),
                ContentWorkspaceMutationPolicy.AllowProtectedDocumentMutation);

            var save = editor.Save();

            Assert.True(save.IsSuccess, string.Join(Environment.NewLine, save.Errors));
            Assert.Empty(save.SavedDocumentIds);
            Assert.Contains("canonical.creatures", save.SkippedProtectedDocumentIds);
            Assert.True(workspaceDocument.IsDirty);
            Assert.DoesNotContain("Curated Slime", File.ReadAllText(path), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory.FullName, recursive: true);
        }
    }

    [Fact]
    public void ContentWorkspaceSaveRequiresExplicitIntentForProtectedDirtyDocuments()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(directory.FullName, "creatures.yaml");
            var protectedDocument = LoadSingleTemplateDocument("slime", "Slime");
            File.WriteAllText(path, protectedDocument.SaveYaml());
            var workspaceDocument = new ContentWorkspaceDocument(
                protectedDocument,
                "canonical.creatures",
                path,
                ContentWorkspaceSourceKind.Canonical,
                IsReadOnly: true);
            var editor = new ContentWorkspaceEditor(new ContentWorkspace([workspaceDocument]));
            editor.UpdateEntityPreset(
                new EntityTemplateId("slime"),
                new EntityTemplate("Curated Slime", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 0),
                new EntityPresentation('S', PresentationColor.Green),
                ContentWorkspaceMutationPolicy.AllowProtectedDocumentMutation);

            var save = editor.Save(ContentWorkspaceSavePolicy.IncludeProtectedDocuments);

            Assert.True(save.IsSuccess, string.Join(Environment.NewLine, save.Errors));
            Assert.Contains("canonical.creatures", save.SavedDocumentIds);
            Assert.False(workspaceDocument.IsDirty);
            Assert.False(workspaceDocument.HasProtectedMutation);
            Assert.Contains("Curated Slime", File.ReadAllText(path), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory.FullName, recursive: true);
        }
    }

    [Fact]
    public void ContentWorkspaceEditorRequiresTargetDocumentForNewSymbolsWhenAmbiguous()
    {
        var first = new ContentWorkspaceDocument(
            new EditableContentDocument(),
            "user.one",
            "one.yaml",
            ContentWorkspaceSourceKind.User);
        var second = new ContentWorkspaceDocument(
            new EditableContentDocument(),
            "user.two",
            "two.yaml",
            ContentWorkspaceSourceKind.User);
        var editor = new ContentWorkspaceEditor(new ContentWorkspace([first, second]));

        var ambiguous = editor.CreateEntityPreset("New Creature");
        var targeted = editor.CreateEntityPreset("New Creature", targetDocumentId: "user.two");

        Assert.False(ambiguous.IsSuccess);
        Assert.True(targeted.IsSuccess, targeted.ErrorMessage);
        Assert.False(first.IsDirty);
        Assert.True(second.IsDirty);
        Assert.Empty(first.Document.EntityTemplates);
        Assert.Single(second.Document.EntityTemplates);
    }

    private static EditableContentDocument LoadMinimalValidDocument() =>
        EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 100
                carryingCapacity: 100
                defaultActionPlanId: wait
            presentations:
              room:
                glyph: '#'
                color: Gray
            actionPlans:
              wait:
                id: wait
                behavior:
                  steps:
                    - kind: MoveFacing
            """);

    private static EditableContentDocument LoadSingleTemplateDocument(string id, string name) =>
        EditableContentDocument.LoadYaml(
            $$"""
            entityTemplates:
              {{id}}:
                name: {{name}}
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
            presentations:
              {{id}}: { glyph: s, color: Green }
            actionPlans: {}
            """);

    private static EditableContentDocument LoadSingleActionPlanDocument(string id) =>
        EditableContentDocument.LoadYaml(
            $$"""
            entityTemplates: {}
            presentations: {}
            actionPlans:
              {{id}}:
                id: {{id}}
                behavior:
                  steps:
                    - kind: MoveFacing
            """);

    private static ContentWorkspace CreatePlayableWorkspace()
    {
        var scenario = EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans: {}
            scenarios:
              debugRoom:
                name: Debug Room
                scenarioRootEntityTemplateId: debugRoot
                playerEntityTemplateId: player
                playerEntityId: debugPlayer
                playerStart: { x: 0, y: 0 }
            """);
        var templates = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              debugRoot:
                name: Debug Root
                inventoryWidth: 2
                inventoryHeight: 2
                weight: 100
                carryingCapacity: 100
              player:
                name: Player
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
            presentations:
              debugRoot: { glyph: '#', color: Gray }
              player: { glyph: '@', color: White }
            actionPlans: {}
            """);

        return new ContentWorkspace([
            new ContentWorkspaceDocument(scenario, "debug.scenario", "debug-room.yaml"),
            new ContentWorkspaceDocument(templates, "debug.templates", "templates.yaml")
        ]);
    }

    private static ContentWorkspace LoadInitialDebugRoomWorkspace()
    {
        var debugPlayerPath = FindRepositoryFile(Path.Combine("src", "GameGameGame.Content", "Canonical", "Creatures", "DebugPlayer.yaml"));
        var chestPath = FindRepositoryFile(Path.Combine("src", "GameGameGame.Content", "Canonical", "Objects", "Chest.yaml"));
        var bagPath = FindRepositoryFile(Path.Combine("src", "GameGameGame.Content", "Canonical", "Objects", "Bag.yaml"));
        var pushBlockPath = FindRepositoryFile(Path.Combine("src", "GameGameGame.Content", "Canonical", "Objects", "PushBlock.yaml"));
        var scrapPath = FindRepositoryFile(Path.Combine("src", "GameGameGame.Content", "Canonical", "Substrates", "Scrap.yaml"));
        var debugRoomRootPath = FindRepositoryFile(Path.Combine("src", "GameGameGame.Content", "Canonical", "Spaces", "DebugRoomRoot.yaml"));
        var scenarioPath = FindRepositoryFile(Path.Combine("src", "GameGameGame.Content", "Debug", "DebugRoom.yaml"));

        return new ContentWorkspace([
            new ContentWorkspaceDocument(
                EditableContentDocument.LoadYaml(File.ReadAllText(debugPlayerPath)),
                "canonical.creatures.debug-player",
                debugPlayerPath,
                ContentWorkspaceSourceKind.Canonical,
                IsReadOnly: true),
            new ContentWorkspaceDocument(
                EditableContentDocument.LoadYaml(File.ReadAllText(chestPath)),
                "canonical.objects.chest",
                chestPath,
                ContentWorkspaceSourceKind.Canonical,
                IsReadOnly: true),
            new ContentWorkspaceDocument(
                EditableContentDocument.LoadYaml(File.ReadAllText(bagPath)),
                "canonical.objects.bag",
                bagPath,
                ContentWorkspaceSourceKind.Canonical,
                IsReadOnly: true),
            new ContentWorkspaceDocument(
                EditableContentDocument.LoadYaml(File.ReadAllText(pushBlockPath)),
                "canonical.objects.push-block",
                pushBlockPath,
                ContentWorkspaceSourceKind.Canonical,
                IsReadOnly: true),
            new ContentWorkspaceDocument(
                EditableContentDocument.LoadYaml(File.ReadAllText(scrapPath)),
                "canonical.substrates.scrap",
                scrapPath,
                ContentWorkspaceSourceKind.Canonical,
                IsReadOnly: true),
            new ContentWorkspaceDocument(
                EditableContentDocument.LoadYaml(File.ReadAllText(debugRoomRootPath)),
                "canonical.spaces.debug-room-root",
                debugRoomRootPath,
                ContentWorkspaceSourceKind.Canonical,
                IsReadOnly: true),
            new ContentWorkspaceDocument(
                EditableContentDocument.LoadYaml(File.ReadAllText(scenarioPath)),
                "debug.debug-room",
                scenarioPath,
                ContentWorkspaceSourceKind.User)
        ]);
    }

    private static string FindRepositoryFile(string relativePath, [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "")
    {
        var directory = Path.GetDirectoryName(sourceFilePath)!;
        while (directory is not null)
        {
            var candidate = Path.Combine(directory, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new FileNotFoundException($"Could not find {relativePath} starting from {sourceFilePath}.");
    }
}
