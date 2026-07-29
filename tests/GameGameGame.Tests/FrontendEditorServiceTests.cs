using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class FrontendEditorServiceTests
{
    [Fact]
    public void OpenFileBuildsReadOnlyEditorSnapshotFromContentEditorSession()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var open = FrontendEditorService.OpenFile(path);

            Assert.True(open.IsSuccess, open.ErrorMessage);
            var service = open.Service!;
            var snapshot = service.GetSnapshot();

            Assert.Equal(path, snapshot.FilePath);
            Assert.False(snapshot.IsDirty);
            Assert.Single(snapshot.Scenarios);
            Assert.Equal("editor-smoke", snapshot.Scenarios[0].ScenarioId);
            Assert.Equal("Editor Smoke", snapshot.Scenarios[0].Name);
            Assert.Equal("editorRoom", snapshot.Scenarios[0].ScenarioRootEntityTemplateId);
            Assert.Equal("editorPlayer", snapshot.Scenarios[0].PlayerEntityTemplateId);
            Assert.Equal(new GridCoord(1, 1), snapshot.Scenarios[0].PlayerStart);

            var room = Assert.Single(snapshot.EntityTemplates, template => template.TemplateId == "editorRoom");
            Assert.Equal("Editor Room", room.Name);
            Assert.Equal('#', room.Glyph);
            Assert.Equal(PresentationColor.Gray, room.Color);
            Assert.Equal(2, room.CarriedEntities.Count);
            var northWall = Assert.Single(room.CarriedEntities, carried => carried.EntityId == "northWall");
            Assert.Equal("wall", northWall.TemplateId);
            Assert.Equal("Wall", northWall.TemplateName);
            Assert.Equal('#', northWall.Glyph);
            Assert.Equal(PresentationColor.Earth, northWall.Color);
            Assert.Equal(new GridCoord(0, 0), northWall.Coord);

            var player = Assert.Single(snapshot.EntityTemplates, template => template.TemplateId == "editorPlayer");
            Assert.Equal("moveEast", player.DefaultActionPlanId);
            Assert.Equal(Direction.East, player.ActionStateDefaults.Facing);
            Assert.Null(player.ActionStateDefaults.TargetEntityId);
            var targetingRule = Assert.Single(player.TargetingRules);
            Assert.Equal(1, targetingRule.Slot);
            Assert.Equal("nearbywall", targetingRule.Label);
            Assert.Equal("Obstacle", targetingRule.Hint);
            Assert.Equal("wall", targetingRule.TargetTemplateId);
            Assert.Equal("Wall", targetingRule.TargetTemplateName);
            Assert.Equal(5, targetingRule.Range);

            var plan = Assert.Single(snapshot.ActionPlans);
            Assert.Equal("moveEast", plan.ActionPlanId);
            Assert.Equal("Canonical Behavior Chain", plan.Shape);
            Assert.Equal(["Move Facing"], plan.ActionStepNames);
            Assert.Equal([ActionPlanBehaviorStepKind.MoveFacing], plan.ActionSteps.Select(step => step.Kind).ToArray());
            Assert.Contains(snapshot.AvailableActionSteps, step => step.Kind == ActionPlanBehaviorStepKind.MoveFacing && step.DisplayName == "Move Facing");
            Assert.DoesNotContain(snapshot.AvailableActionSteps, step => step.Kind == ActionPlanBehaviorStepKind.AcquireNearestTarget);

            Assert.DoesNotContain(snapshot.ValidationDiagnostics, diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error);
            Assert.Contains("editor-smoke", snapshot.YamlPreview);
            Assert.True(snapshot.YamlDiffLines.Count == 0);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SnapshotGroupsTemplateAndCarriedEntityDiagnosticsForEntityTemplatePanels()
    {
        var path = WriteTempContentFile(
            """
            entityTemplates:
              invalidRoom:
                name: Invalid Room
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 10
                carryingCapacity: 10
                carriedEntities:
                - entityId: missingRock
                  templateId: missingTemplate
                  coord:
                    x: 2
                    y: 0
            presentations:
              invalidRoom:
                glyph: '#'
                color: Gray
            actionPlans: {}
            """);

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var invalidRoom = Assert.Single(service.GetSnapshot().EntityTemplates);

            Assert.Contains(invalidRoom.Diagnostics, diagnostic => diagnostic.EntityTemplateId == "invalidRoom");
            var carried = Assert.Single(invalidRoom.CarriedEntities);
            Assert.Equal("missingRock", carried.EntityId);
            Assert.Equal("missingTemplate", carried.TemplateId);
            Assert.Null(carried.TemplateName);
            Assert.Contains(carried.Diagnostics, diagnostic => diagnostic.CarriedEntityId == "missingRock");
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void PreviewScenarioMaterializesTurnZeroRuntimeStateWithoutMutatingDocument()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var preview = service.PreviewScenario("editor-smoke");

            Assert.True(preview.CanPlay, string.Join(" | ", preview.ValidationDiagnostics.Concat(preview.RuntimeFailures)));
            Assert.True(preview.IsDerivedRuntimeState);
            Assert.Equal("editor-smoke", preview.ScenarioId);
            Assert.Equal("Editor Smoke", preview.Name);
            Assert.Equal(new EntityId("editorPlayer"), preview.Session.PlayerEntityId);
            Assert.Equal(new EntityId("scenarioRoot"), preview.Session.ActiveContainerEntityId);
            Assert.Equal("editor-smoke", service.GetSnapshot().Scenarios.Single().ScenarioId);
            Assert.False(service.GetSnapshot().IsDirty);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SnapshotReflectsServiceBackedEditsThroughSharedSession()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;
            var session = service.Session;

            session.Editor.UpdateEntityPreset(
                new EntityTemplateId("wall"),
                session.Editor.GetEntityPreset(new EntityTemplateId("wall")).Template with { Name = "Renamed Wall" },
                new EntityPresentation('W', PresentationColor.White));

            var snapshot = service.GetSnapshot();
            var wall = Assert.Single(snapshot.EntityTemplates, template => template.TemplateId == "wall");
            Assert.Equal("Renamed Wall", wall.Name);
            Assert.Equal('W', wall.Glyph);
            Assert.True(snapshot.IsDirty);
            Assert.Contains(snapshot.YamlDiffLines, line => line.Contains("Renamed Wall", StringComparison.Ordinal));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void UpdateTemplatePresentationEditsNameGlyphAndColorThroughEditorServices()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.UpdateTemplatePresentation(
                "wall",
                new FrontendEditorTemplatePresentationUpdate(
                    Name: "Stone Wall",
                    GlyphText: "WX",
                    Color: PresentationColor.White));

            Assert.True(result.IsSuccess, result.StatusMessage);
            Assert.True(result.Snapshot.IsDirty);
            Assert.Contains("Preview stale", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
            var wall = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == "wall");
            Assert.Equal("Stone Wall", wall.Name);
            Assert.Equal('W', wall.Glyph);
            Assert.Equal(PresentationColor.White, wall.Color);
            Assert.Contains("Stone Wall", result.Snapshot.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateTemplatePresentationRejectsBlankGlyph(string? glyphText)
    {
        var service = FrontendEditorService.CreateNew();
        var id = service.Session.Editor.CreateEntityPreset("Glyph Test");

        var result = service.UpdateTemplatePresentation(
            id.Value,
            new FrontendEditorTemplatePresentationUpdate("Glyph Test", glyphText, PresentationColor.Gray));

        Assert.False(result.IsSuccess);
        Assert.Contains("glyph", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SaveWritesCurrentEditorSessionAndReturnsCleanSnapshot()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;
            service.UpdateTemplatePresentation(
                "wall",
                new FrontendEditorTemplatePresentationUpdate("Saved Wall", "W", PresentationColor.White));

            var save = service.Save();

            Assert.True(save.IsSuccess, save.StatusMessage);
            Assert.False(save.Snapshot.IsDirty);
            var reloaded = FrontendEditorService.OpenFile(path).Service!.GetSnapshot();
            var wall = Assert.Single(reloaded.EntityTemplates, template => template.TemplateId == "wall");
            Assert.Equal("Saved Wall", wall.Name);
            Assert.Equal('W', wall.Glyph);
            Assert.Equal(PresentationColor.White, wall.Color);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SaveWithoutFilePathReturnsFailureSnapshot()
    {
        var service = FrontendEditorService.CreateNew();

        var save = service.Save();

        Assert.False(save.IsSuccess);
        Assert.Contains("Save As", save.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.False(save.Snapshot.IsDirty);
    }

    [Fact]
    public void SetTemplateDefaultActionPlanAssignsExistingPlanThroughEditorServices()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.SetTemplateDefaultActionPlan("wall", "moveEast");

            Assert.True(result.IsSuccess, result.StatusMessage);
            Assert.True(result.Snapshot.IsDirty);
            Assert.Contains("Preview stale", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
            var wall = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == "wall");
            Assert.Equal("moveEast", wall.DefaultActionPlanId);
            Assert.Equal(Direction.West, wall.ActionStateDefaults.Facing);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SetTemplateDefaultActionPlanRejectsMissingPlan()
    {
        var service = FrontendEditorService.CreateNew();
        var id = service.Session.Editor.CreateEntityPreset("Plan Test");

        var result = service.SetTemplateDefaultActionPlan(id.Value, "missingPlan");

        Assert.False(result.IsSuccess);
        Assert.Contains("missingPlan", result.StatusMessage);
        var template = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == id.Value);
        Assert.Null(template.DefaultActionPlanId);
    }

    [Fact]
    public void ClearTemplateDefaultActionPlanClearsPlanThroughEditorServices()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.ClearTemplateDefaultActionPlan("editorPlayer");

            Assert.True(result.IsSuccess, result.StatusMessage);
            Assert.True(result.Snapshot.IsDirty);
            var player = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == "editorPlayer");
            Assert.Null(player.DefaultActionPlanId);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SetTemplateTargetingRuleWritesValidatedRuleThroughEditorServices()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.SetTemplateTargetingRule(
                "wall",
                new FrontendEditorTargetingRuleUpdate(1, "fears", "editorPlayer", 7));

            Assert.True(result.IsSuccess, result.StatusMessage);
            Assert.True(result.Snapshot.IsDirty);
            Assert.Contains("Preview stale", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
            var wall = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == "wall");
            var rule = Assert.Single(wall.TargetingRules);
            Assert.Equal(1, rule.Slot);
            Assert.Equal("fears", rule.Label);
            Assert.Equal("editorPlayer", rule.TargetTemplateId);
            Assert.Equal("Editor Player", rule.TargetTemplateName);
            Assert.Equal(7, rule.Range);
            Assert.Empty(rule.TargetCapabilities);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SetTemplateTargetingRuleCanWriteCapabilityAdjectives()
    {
        var path = WriteTempContentFile(
            """
            entityTemplates:
              thief:
                name: Thief
                inventoryWidth: 1
                inventoryHeight: 1
                bulk: 1
                aperture: 2
                defaultActionPlanId: thiefPlan
              gold:
                name: Gold
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
            presentations:
              thief:
                glyph: t
                color: Gray
              gold:
                glyph: '$'
                color: Yellow
            actionPlans:
              thiefPlan:
                id: thiefPlan
                behavior:
                  steps:
                    - kind: PickupTarget
                      targetLabel: loves
            """);

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.SetTemplateTargetingRule(
                "thief",
                new FrontendEditorTargetingRuleUpdate(
                    1,
                    "loves",
                    "gold",
                    5,
                    [ActionPlanBehaviorStepKind.PickupTarget]));

            Assert.True(result.IsSuccess, result.StatusMessage);
            var thief = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == "thief");
            var rule = Assert.Single(thief.TargetingRules);
            Assert.Equal("gold", rule.TargetTemplateId);
            Assert.Equal([ActionPlanBehaviorStepKind.PickupTarget], rule.TargetCapabilities);
            Assert.Contains("targetCapabilities:", result.Snapshot.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SnapshotProjectsTargetingProfileRangeAndRuleLocality()
    {
        var path = WriteTempContentFile(
            """
            entityTemplates:
              goblin:
                name: Goblin
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
                targeting:
                  range: 8
                  defaultLocality:
                    origins:
                      - CurrentPlace
                  rules:
                    - slot: 1
                      label: hates
                      targetTemplateId: slime
                    - slot: 2
                      label: wants
                      targetTemplateId: gold
                      locality:
                        origins:
                          - CurrentPlace
                          - PeerInventories
              slime:
                name: Slime
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
              gold:
                name: Gold
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 0
            presentations:
              goblin:
                glyph: g
                color: Gray
              slime:
                glyph: s
                color: Green
              gold:
                glyph: '$'
                color: Yellow
            actionPlans: {}
            """);

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var goblin = Assert.Single(service.GetSnapshot().EntityTemplates, template => template.TemplateId == "goblin");

            Assert.NotNull(goblin.TargetingProfile);
            Assert.Equal(FrontendEditorTargetingSource.TargetingProfile, goblin.TargetingSource);
            Assert.Equal(8, goblin.TargetingProfile!.Range);
            Assert.Equal([TargetingLocalityOrigin.CurrentPlace], goblin.TargetingProfile.DefaultLocalityOrigins);
            Assert.Equal(["hates", "wants"], goblin.TargetingRules.Select(rule => rule.Label).ToArray());
            Assert.Equal([TargetingLocalityOrigin.CurrentPlace], goblin.TargetingRules[0].EffectiveLocalityOrigins);
            Assert.Null(goblin.TargetingRules[0].LocalityOrigins);
            Assert.Equal([TargetingLocalityOrigin.CurrentPlace, TargetingLocalityOrigin.PeerInventories], goblin.TargetingRules[1].LocalityOrigins);
            Assert.Equal([TargetingLocalityOrigin.CurrentPlace, TargetingLocalityOrigin.PeerInventories], goblin.TargetingRules[1].EffectiveLocalityOrigins);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SetTemplateTargetingProfileRuleWritesCanonicalProfileShape()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.SetTemplateTargetingProfileRule(
                "wall",
                new FrontendEditorTargetingProfileRuleUpdate(
                    Range: 6,
                    Slot: 1,
                    Label: "fears",
                    TargetTemplateId: "editorPlayer",
                    LocalityOrigins: [TargetingLocalityOrigin.CurrentPlace, TargetingLocalityOrigin.PeerInventories]));

            Assert.True(result.IsSuccess, result.StatusMessage);
            var wall = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == "wall");
            Assert.Equal(FrontendEditorTargetingSource.TargetingProfile, wall.TargetingSource);
            Assert.Equal(6, wall.TargetingProfile!.Range);
            var rule = Assert.Single(wall.TargetingRules);
            Assert.Equal("fears", rule.Label);
            Assert.Equal([TargetingLocalityOrigin.CurrentPlace, TargetingLocalityOrigin.PeerInventories], rule.LocalityOrigins);
            Assert.Contains("targeting:", result.Snapshot.YamlPreview);
            Assert.Contains("range: 6", result.Snapshot.YamlPreview);
            Assert.Contains("PeerInventories", result.Snapshot.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SetTemplateTargetingProfileRuleRejectsDuplicateLabelOnSameTemplateProfile()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;
            var first = service.SetTemplateTargetingProfileRule(
                "wall",
                new FrontendEditorTargetingProfileRuleUpdate(
                    Range: 6,
                    Slot: 1,
                    Label: "fears",
                    TargetTemplateId: "editorPlayer",
                    LocalityOrigins: [TargetingLocalityOrigin.CurrentPlace]));
            Assert.True(first.IsSuccess, first.StatusMessage);

            var duplicate = service.SetTemplateTargetingProfileRule(
                "wall",
                new FrontendEditorTargetingProfileRuleUpdate(
                    Range: 6,
                    Slot: 2,
                    Label: "fears",
                    TargetTemplateId: "editorPlayer",
                    LocalityOrigins: [TargetingLocalityOrigin.CurrentPlace]));

            Assert.False(duplicate.IsSuccess);
            Assert.Contains("duplicate", duplicate.StatusMessage, StringComparison.OrdinalIgnoreCase);
            var wall = Assert.Single(duplicate.Snapshot.EntityTemplates, template => template.TemplateId == "wall");
            Assert.Single(wall.TargetingRules);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SetTemplateTargetingDefaultLocalityUpdatesProfileAndNewRuleDefaults()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var locality = service.SetTemplateTargetingDefaultLocality(
                "wall",
                [TargetingLocalityOrigin.CurrentPlace, TargetingLocalityOrigin.PeerInventories]);
            Assert.True(locality.IsSuccess, locality.StatusMessage);

            var result = service.SetTemplateTargetingProfileRule(
                "wall",
                new FrontendEditorTargetingProfileRuleUpdate(5, 1, "fears", "editorPlayer"));

            Assert.True(result.IsSuccess, result.StatusMessage);
            var wall = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == "wall");
            Assert.Equal([TargetingLocalityOrigin.CurrentPlace, TargetingLocalityOrigin.PeerInventories], wall.TargetingProfile!.DefaultLocalityOrigins);
            var rule = Assert.Single(wall.TargetingRules);
            Assert.Null(rule.LocalityOrigins);
            Assert.Equal([TargetingLocalityOrigin.CurrentPlace, TargetingLocalityOrigin.PeerInventories], rule.EffectiveLocalityOrigins);
            Assert.Contains("defaultLocality:", result.Snapshot.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SnapshotProjectsDefaultActionPlanTargetLabelRequirementsAndOrphanedRules()
    {
        var path = WriteTempContentFile(
            """
            entityTemplates:
              actor:
                name: Actor
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
                defaultActionPlanId: feelings
                targetingRules:
                - slot: 1
                  label: loves
                  targetTemplateId: friend
                  range: 4
                - slot: 2
                  label: unused
                  targetTemplateId: enemy
                  range: 5
              noPlan:
                name: No Plan
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
              friend:
                name: Friend
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
              enemy:
                name: Enemy
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
            presentations:
              actor:
                glyph: '@'
                color: Yellow
              noPlan:
                glyph: '?'
                color: Gray
              friend:
                glyph: 'f'
                color: Gray
              enemy:
                glyph: 'e'
                color: Earth
            actionPlans:
              feelings:
                id: feelings
                behavior:
                  steps:
                  - kind: SeekTarget
                    targetLabel: loves
                  - kind: FleeTarget
                    targetLabel: fears
                  - kind: GiveTarget
                    targetLabel: loves
            """);

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var actor = Assert.Single(service.GetSnapshot().EntityTemplates, template => template.TemplateId == "actor");

            Assert.Equal(["loves", "fears"], actor.TargetingRequirements.Select(requirement => requirement.Label).ToArray());
            var loves = actor.TargetingRequirements[0];
            Assert.True(loves.IsConfigured);
            Assert.Equal([0, 2], loves.StepIndexes.ToArray());
            Assert.Equal([ActionPlanBehaviorStepKind.SeekTarget, ActionPlanBehaviorStepKind.GiveTarget], loves.StepKinds.ToArray());
            Assert.NotNull(loves.Rule);
            Assert.Equal("friend", loves.Rule!.TargetTemplateId);

            var fears = actor.TargetingRequirements[1];
            Assert.False(fears.IsConfigured);
            Assert.Null(fears.Rule);

            var orphan = Assert.Single(actor.OrphanedTargetingRules);
            Assert.Equal("unused", orphan.Label);

            var noPlan = Assert.Single(service.GetSnapshot().EntityTemplates, template => template.TemplateId == "noPlan");
            Assert.Empty(noPlan.TargetingRequirements);
            Assert.Empty(noPlan.OrphanedTargetingRules);

            var plan = Assert.Single(service.GetSnapshot().ActionPlans, plan => plan.ActionPlanId == "feelings");
            Assert.Equal(["loves", "fears"], plan.TargetLabelRequirements.Select(requirement => requirement.Label).ToArray());
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Theory]
    [InlineData(0, "fears", "editorPlayer", 5, "slot")]
    [InlineData(5, "fears", "editorPlayer", 5, "slot")]
    [InlineData(1, "", "editorPlayer", 5, "label")]
    [InlineData(1, "has space", "editorPlayer", 5, "lowercase alphanumeric")]
    [InlineData(1, "Fear", "editorPlayer", 5, "lowercase alphanumeric")]
    [InlineData(1, "fears", "missingTemplate", 5, "missingTemplate")]
    [InlineData(1, "fears", "editorPlayer", -1, "range")]
    [InlineData(1, "fears", "editorPlayer", 11, "range")]
    public void SetTemplateTargetingRuleRejectsInvalidFrontendRules(
        int slot,
        string label,
        string targetTemplateId,
        int range,
        string expectedMessagePart)
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.SetTemplateTargetingRule(
                "wall",
                new FrontendEditorTargetingRuleUpdate(slot, label, targetTemplateId, range));

            Assert.False(result.IsSuccess);
            Assert.Contains(expectedMessagePart, result.StatusMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SetTemplateTargetingRuleRejectsDuplicateLabelOnSameTemplate()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.SetTemplateTargetingRule(
                "editorPlayer",
                new FrontendEditorTargetingRuleUpdate(2, "nearbywall", "wall", 5));

            Assert.False(result.IsSuccess);
            Assert.Contains("duplicate", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void ClearTemplateTargetingRuleRemovesExistingSlot()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.ClearTemplateTargetingRule("editorPlayer", 1);

            Assert.True(result.IsSuccess, result.StatusMessage);
            var player = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == "editorPlayer");
            Assert.Empty(player.TargetingRules);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void PlaceTemplateInInventoryAddsGeneratedCarriedEntityThroughEditorServices()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.PlaceTemplateInInventory("editorRoom", "rock", new GridCoord(1, 0));

            Assert.True(result.IsSuccess, result.StatusMessage);
            Assert.True(result.Snapshot.IsDirty);
            Assert.Contains("Preview stale", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
            var room = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == "editorRoom");
            var placed = Assert.Single(room.CarriedEntities, carried => carried.Coord == new GridCoord(1, 0));
            Assert.StartsWith("editorRoomRock", placed.EntityId, StringComparison.Ordinal);
            Assert.Equal("rock", placed.TemplateId);
            Assert.Equal("Rock", placed.TemplateName);
            Assert.Equal('*', placed.Glyph);
            Assert.Equal(PresentationColor.Earth, placed.Color);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void PlaceTemplateInInventoryRejectsDirectSelfTemplatePlacement()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.PlaceTemplateInInventory("editorRoom", "editorRoom", new GridCoord(1, 0));

            Assert.False(result.IsSuccess);
            Assert.Contains("itself", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
            var room = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == "editorRoom");
            Assert.DoesNotContain(room.CarriedEntities, carried => carried.TemplateId == "editorRoom");
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Theory]
    [InlineData("editorRoom", "rock", 0, 0, "occupied")]
    [InlineData("editorRoom", "rock", 99, 0, "outside")]
    [InlineData("wall", "rock", 0, 0, "no usable inventory")]
    [InlineData("editorRoom", "missingTemplate", 1, 0, "missingTemplate")]
    public void PlaceTemplateInInventoryReportsInvalidBrushPlacement(
        string parentTemplateId,
        string brushTemplateId,
        int x,
        int y,
        string expectedMessagePart)
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.PlaceTemplateInInventory(parentTemplateId, brushTemplateId, new GridCoord(x, y));

            Assert.False(result.IsSuccess);
            Assert.Contains(expectedMessagePart, result.StatusMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void ReplaceActionPlanStepReplacesExistingCanonicalStepThroughEditorServices()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.ReplaceActionPlanStep("moveEast", 0, ActionPlanBehaviorStepKind.Backstep);

            Assert.True(result.IsSuccess, result.StatusMessage);
            Assert.True(result.Snapshot.IsDirty);
            Assert.Contains("Preview stale", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
            var plan = Assert.Single(result.Snapshot.ActionPlans, plan => plan.ActionPlanId == "moveEast");
            Assert.Equal([ActionPlanBehaviorStepKind.Backstep], plan.ActionSteps.Select(step => step.Kind).ToArray());
            Assert.Equal(["Backstep"], plan.ActionStepNames);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void InsertActionPlanStepInsertsCanonicalStepAtRequestedIndex()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.InsertActionPlanStep("moveEast", 0, ActionPlanBehaviorStepKind.PickupTarget);

            Assert.True(result.IsSuccess, result.StatusMessage);
            var plan = Assert.Single(result.Snapshot.ActionPlans, plan => plan.ActionPlanId == "moveEast");
            Assert.Equal(
                [ActionPlanBehaviorStepKind.PickupTarget, ActionPlanBehaviorStepKind.MoveFacing],
                plan.ActionSteps.Select(step => step.Kind).ToArray());
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void InsertActionPlanStepAllowsAppendAtEnd()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.InsertActionPlanStep("moveEast", 1, ActionPlanBehaviorStepKind.DropFacing);

            Assert.True(result.IsSuccess, result.StatusMessage);
            var plan = Assert.Single(result.Snapshot.ActionPlans, plan => plan.ActionPlanId == "moveEast");
            Assert.Equal(
                [ActionPlanBehaviorStepKind.MoveFacing, ActionPlanBehaviorStepKind.DropFacing],
                plan.ActionSteps.Select(step => step.Kind).ToArray());
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void ActionPlanStepMutationsDefaultRequiredMoveAndTransferOptions()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var move = service.ReplaceActionPlanStep("moveEast", 0, ActionPlanBehaviorStepKind.Move);
            var transfer = service.InsertActionPlanStep("moveEast", 1, ActionPlanBehaviorStepKind.Transfer);

            Assert.True(move.IsSuccess, move.StatusMessage);
            Assert.True(transfer.IsSuccess, transfer.StatusMessage);
            Assert.DoesNotContain(transfer.Snapshot.ValidationDiagnostics, diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error);
            Assert.Contains("kind: Move", transfer.Snapshot.YamlPreview);
            Assert.Contains("directionMode: Forward", transfer.Snapshot.YamlPreview);
            Assert.Contains("kind: Transfer", transfer.Snapshot.YamlPreview);
            Assert.Contains("transferDirection: TargetToActor", transfer.Snapshot.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void RemoveActionPlanStepRemovesCanonicalStepThroughEditorServices()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;
            Assert.True(service.InsertActionPlanStep("moveEast", 1, ActionPlanBehaviorStepKind.PickupTarget).IsSuccess);

            var result = service.RemoveActionPlanStep("moveEast", 0);

            Assert.True(result.IsSuccess, result.StatusMessage);
            Assert.True(result.Snapshot.IsDirty);
            Assert.Contains("Preview stale", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
            var plan = Assert.Single(result.Snapshot.ActionPlans, plan => plan.ActionPlanId == "moveEast");
            Assert.Equal([ActionPlanBehaviorStepKind.PickupTarget], plan.ActionSteps.Select(step => step.Kind).ToArray());
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void MoveActionPlanStepReordersCanonicalStepsThroughEditorServices()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;
            Assert.True(service.InsertActionPlanStep("moveEast", 1, ActionPlanBehaviorStepKind.PickupTarget).IsSuccess);

            var result = service.MoveActionPlanStep("moveEast", 1, 0);

            Assert.True(result.IsSuccess, result.StatusMessage);
            Assert.True(result.Snapshot.IsDirty);
            Assert.Contains("Preview stale", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
            var plan = Assert.Single(result.Snapshot.ActionPlans, plan => plan.ActionPlanId == "moveEast");
            Assert.Equal(
                [ActionPlanBehaviorStepKind.PickupTarget, ActionPlanBehaviorStepKind.MoveFacing],
                plan.ActionSteps.Select(step => step.Kind).ToArray());
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SnapshotIncludesActionPlanStepTargetReferencesAndTargetConsumptionMetadata()
    {
        var path = WriteTempContentFile(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              targetPlan:
                id: targetPlan
                behavior:
                  steps:
                  - kind: MoveFacing
                  - kind: SeekTarget
                    targetLabel: loves
                  - kind: FleeTarget
                    targetSlot: 2
            """);

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var plan = Assert.Single(service.GetSnapshot().ActionPlans, plan => plan.ActionPlanId == "targetPlan");

            Assert.Collection(
                plan.ActionSteps,
                step =>
                {
                    Assert.Equal(0, step.Index);
                    Assert.Equal(ActionPlanBehaviorStepKind.MoveFacing, step.Kind);
                    Assert.Null(step.TargetLabel);
                    Assert.Null(step.TargetSlot);
                    Assert.False(step.ConsumesTargetReference);
                },
                step =>
                {
                    Assert.Equal(1, step.Index);
                    Assert.Equal(ActionPlanBehaviorStepKind.SeekTarget, step.Kind);
                    Assert.Equal("loves", step.TargetLabel);
                    Assert.Null(step.TargetSlot);
                    Assert.True(step.ConsumesTargetReference);
                },
                step =>
                {
                    Assert.Equal(2, step.Index);
                    Assert.Equal(ActionPlanBehaviorStepKind.FleeTarget, step.Kind);
                    Assert.Null(step.TargetLabel);
                    Assert.Equal(2, step.TargetSlot);
                    Assert.True(step.ConsumesTargetReference);
                });
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void FrontendEditorSnapshotIncludesBehaviorStepCosts()
    {
        var path = WriteTempContentFile(
            """
            entityTemplates:
              scrap:
                name: Scrap
                inventoryWidth: 0
                inventoryHeight: 0
                bulk: 1
                aperture: 1
            presentations:
              scrap:
                glyph: s
                color: Gray
            actionPlans:
              costlyMove:
                id: costlyMove
                behavior:
                  steps:
                  - kind: MoveFacing
                    costs:
                    - templateId: scrap
                      quantity: 3
            """);

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var step = Assert.Single(Assert.Single(service.GetSnapshot().ActionPlans).ActionSteps);

            Assert.Equal("Cost: 3× Scrap", step.CostSummary);
            var cost = Assert.Single(step.Costs);
            Assert.Equal("scrap", cost.TemplateId);
            Assert.Equal(3, cost.Quantity);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void FrontendEditorServiceSetsActionPlanStepCosts()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.SetActionPlanStepCosts("moveEast", 0, [new ActionStepCostDescriptor("wall", 1)]);

            Assert.True(result.IsSuccess, result.StatusMessage);
            var step = Assert.Single(Assert.Single(result.Snapshot.ActionPlans, plan => plan.ActionPlanId == "moveEast").ActionSteps);
            Assert.Equal("Cost: 1× Wall", step.CostSummary);
            Assert.Contains("costs:", result.Snapshot.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SetActionPlanStepTargetLabelUpdatesAndClearsTargetLabelRequirements()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;
            Assert.True(service.ReplaceActionPlanStep("moveEast", 0, ActionPlanBehaviorStepKind.SeekTarget).IsSuccess);

            var set = service.SetActionPlanStepTargetLabel("moveEast", 0, "loves");

            Assert.True(set.IsSuccess, set.StatusMessage);
            var setPlan = Assert.Single(set.Snapshot.ActionPlans, plan => plan.ActionPlanId == "moveEast");
            var setStep = Assert.Single(setPlan.ActionSteps);
            Assert.Equal("loves", setStep.TargetLabel);
            Assert.Null(setStep.TargetSlot);
            var requirement = Assert.Single(setPlan.TargetLabelRequirements);
            Assert.Equal("loves", requirement.Label);
            Assert.Equal([0], requirement.StepIndexes.ToArray());

            var clear = service.SetActionPlanStepTargetLabel("moveEast", 0, null);

            Assert.True(clear.IsSuccess, clear.StatusMessage);
            var clearPlan = Assert.Single(clear.Snapshot.ActionPlans, plan => plan.ActionPlanId == "moveEast");
            Assert.Null(Assert.Single(clearPlan.ActionSteps).TargetLabel);
            Assert.Empty(clearPlan.TargetLabelRequirements);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Theory]
    [InlineData(9, "index")]
    [InlineData(0, "label")]
    public void SetActionPlanStepTargetLabelRejectsInvalidIndexesAndLabels(int stepIndex, string expectedMessagePart)
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;
            var label = stepIndex == 0 ? "Has Space" : "loves";

            var result = service.SetActionPlanStepTargetLabel("moveEast", stepIndex, label);

            Assert.False(result.IsSuccess);
            Assert.Contains(expectedMessagePart, result.StatusMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void ActionPlanStepEditsRejectLegacyStepsAndInvalidIndexes()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var legacy = service.ReplaceActionPlanStep("moveEast", 0, ActionPlanBehaviorStepKind.AcquireNearestTarget);
            var missing = service.ReplaceActionPlanStep("moveEast", 9, ActionPlanBehaviorStepKind.Backstep);
            var badInsert = service.InsertActionPlanStep("moveEast", -1, ActionPlanBehaviorStepKind.Backstep);
            var badRemove = service.RemoveActionPlanStep("moveEast", 9);
            var badMove = service.MoveActionPlanStep("moveEast", 0, 9);
            Assert.True(service.CreatePassiveActionPlan("Passive Plan").IsSuccess);
            var passiveRemove = service.RemoveActionPlanStep("passivePlan", 0);
            var passiveMove = service.MoveActionPlanStep("passivePlan", 0, 0);

            Assert.False(legacy.IsSuccess);
            Assert.Contains("not available", legacy.StatusMessage, StringComparison.OrdinalIgnoreCase);
            Assert.False(missing.IsSuccess);
            Assert.Contains("index", missing.StatusMessage, StringComparison.OrdinalIgnoreCase);
            Assert.False(badInsert.IsSuccess);
            Assert.Contains("index", badInsert.StatusMessage, StringComparison.OrdinalIgnoreCase);
            Assert.False(badRemove.IsSuccess);
            Assert.Contains("index", badRemove.StatusMessage, StringComparison.OrdinalIgnoreCase);
            Assert.False(badMove.IsSuccess);
            Assert.Contains("index", badMove.StatusMessage, StringComparison.OrdinalIgnoreCase);
            Assert.False(passiveRemove.IsSuccess);
            Assert.Contains("only canonical behavior chains", passiveRemove.StatusMessage, StringComparison.OrdinalIgnoreCase);
            Assert.False(passiveMove.IsSuccess);
            Assert.Contains("only canonical behavior chains", passiveMove.StatusMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void UpdateTemplateMetadataEditsInventoryDimensionsBulkAndAperture()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.UpdateTemplateMetadata(
                "wall",
                new FrontendEditorTemplateMetadataUpdate(InventoryWidth: 2, InventoryHeight: 3, Bulk: 4, Aperture: 5));

            Assert.True(result.IsSuccess, result.StatusMessage);
            Assert.True(result.Snapshot.IsDirty);
            Assert.Contains("Preview stale", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
            var wall = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == "wall");
            Assert.Equal(2, wall.InventoryWidth);
            Assert.Equal(3, wall.InventoryHeight);
            Assert.Equal(4, wall.Bulk);
            Assert.Equal(5, wall.Aperture);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Theory]
    [InlineData(-1, 1, 1, 1, "inventory")]
    [InlineData(1, -1, 1, 1, "inventory")]
    [InlineData(1, 1, -1, 1, "bulk")]
    [InlineData(1, 1, 1, -1, "aperture")]
    public void UpdateTemplateMetadataRejectsNegativeValues(
        int width,
        int height,
        int bulk,
        int aperture,
        string expectedMessagePart)
    {
        var service = FrontendEditorService.CreateNew();
        var id = service.Session.Editor.CreateEntityPreset("Metadata Test");

        var result = service.UpdateTemplateMetadata(
            id.Value,
            new FrontendEditorTemplateMetadataUpdate(width, height, bulk, aperture));

        Assert.False(result.IsSuccess);
        Assert.Contains(expectedMessagePart, result.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SetAndClearTemplateInitialFacingMutatesActionStateDefaults()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var set = service.SetTemplateInitialFacing("wall", Direction.North);

            Assert.True(set.IsSuccess, set.StatusMessage);
            var wall = Assert.Single(set.Snapshot.EntityTemplates, template => template.TemplateId == "wall");
            Assert.Equal(Direction.North, wall.ActionStateDefaults.Facing);

            var clear = service.ClearTemplateInitialFacing("wall");

            Assert.True(clear.IsSuccess, clear.StatusMessage);
            wall = Assert.Single(clear.Snapshot.EntityTemplates, template => template.TemplateId == "wall");
            Assert.Null(wall.ActionStateDefaults.Facing);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SetAndClearTemplateInventoryBoundaryPoliciesMutatesTemplatePolicies()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var setEnter = service.SetTemplateEnterPolicy("wall", EntityEnterPolicy.FarthestFromOccupied);
            var setExit = service.SetTemplateExitPolicy("wall", EntityExitPolicy.EdgeAlignedWithExitDirection);
            var setTopology = service.SetTemplateTopologyPolicy("wall", EntityTopologyPolicy.ConnectsInward);

            Assert.True(setEnter.IsSuccess, setEnter.StatusMessage);
            Assert.True(setExit.IsSuccess, setExit.StatusMessage);
            Assert.True(setTopology.IsSuccess, setTopology.StatusMessage);
            var wall = Assert.Single(setTopology.Snapshot.EntityTemplates, template => template.TemplateId == "wall");
            Assert.Equal(EntityEnterPolicy.FarthestFromOccupied, wall.EnterPolicy);
            Assert.Equal(EntityEnterPolicy.FarthestFromOccupied, wall.EffectiveEnterPolicy);
            Assert.Equal(EntityExitPolicy.EdgeAlignedWithExitDirection, wall.ExitPolicy);
            Assert.Equal(EntityExitPolicy.EdgeAlignedWithExitDirection, wall.EffectiveExitPolicy);
            Assert.Equal(EntityTopologyPolicy.ConnectsInward, wall.TopologyPolicy);

            var clearEnter = service.ClearTemplateEnterPolicy("wall");
            var clearExit = service.ClearTemplateExitPolicy("wall");
            var clearTopology = service.SetTemplateTopologyPolicy("wall", EntityTopologyPolicy.None);

            Assert.True(clearEnter.IsSuccess, clearEnter.StatusMessage);
            Assert.True(clearExit.IsSuccess, clearExit.StatusMessage);
            Assert.True(clearTopology.IsSuccess, clearTopology.StatusMessage);
            wall = Assert.Single(clearTopology.Snapshot.EntityTemplates, template => template.TemplateId == "wall");
            Assert.Null(wall.EnterPolicy);
            Assert.Equal(EntityEnterPolicy.FirstUnoccupiedRowMajor, wall.EffectiveEnterPolicy);
            Assert.Null(wall.ExitPolicy);
            Assert.Equal(EntityExitPolicy.AnyCell, wall.EffectiveExitPolicy);
            Assert.Equal(EntityTopologyPolicy.None, wall.TopologyPolicy);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void RemoveCarriedEntityRemovesAuthoredInventoryEntry()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.RemoveCarriedEntity("editorRoom", "northWall");

            Assert.True(result.IsSuccess, result.StatusMessage);
            var room = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == "editorRoom");
            Assert.DoesNotContain(room.CarriedEntities, carried => carried.EntityId == "northWall");
            Assert.Contains(room.CarriedEntities, carried => carried.EntityId == "floorRock");
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void MoveCarriedEntityMovesAuthoredInventoryEntry()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.MoveCarriedEntity("editorRoom", "northWall", new GridCoord(1, 0));

            Assert.True(result.IsSuccess, result.StatusMessage);
            var room = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == "editorRoom");
            Assert.Contains(room.CarriedEntities, carried => carried.EntityId == "northWall" && carried.Coord == new GridCoord(1, 0));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void ReplaceCarriedEntityTemplateChangesBrushReferenceWithoutMoving()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.ReplaceCarriedEntityTemplate("editorRoom", "northWall", "rock");

            Assert.True(result.IsSuccess, result.StatusMessage);
            var room = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == "editorRoom");
            var carried = Assert.Single(room.CarriedEntities, carried => carried.EntityId == "northWall");
            Assert.Equal("rock", carried.TemplateId);
            Assert.Equal("Rock", carried.TemplateName);
            Assert.Equal(new GridCoord(0, 0), carried.Coord);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SetCarriedEntityControllerUpdatesAuthoredInventoryEntry()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var set = service.SetCarriedEntityController("editorRoom", "northWall", EntityController.Player);
            var clear = service.SetCarriedEntityController("editorRoom", "northWall", null);

            Assert.True(set.IsSuccess, set.StatusMessage);
            var setRoom = Assert.Single(set.Snapshot.EntityTemplates, template => template.TemplateId == "editorRoom");
            Assert.Equal(EntityController.Player, Assert.Single(setRoom.CarriedEntities, carried => carried.EntityId == "northWall").Controller);
            Assert.True(clear.IsSuccess, clear.StatusMessage);
            var clearedRoom = Assert.Single(clear.Snapshot.EntityTemplates, template => template.TemplateId == "editorRoom");
            Assert.Null(Assert.Single(clearedRoom.CarriedEntities, carried => carried.EntityId == "northWall").Controller);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void OverwriteCarriedEntityAtCoordinateRemovesOccupantAndPlacesBrush()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.OverwriteTemplateInInventory("editorRoom", "rock", new GridCoord(0, 0));

            Assert.True(result.IsSuccess, result.StatusMessage);
            var room = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == "editorRoom");
            Assert.DoesNotContain(room.CarriedEntities, carried => carried.EntityId == "northWall");
            Assert.Single(room.CarriedEntities, carried => carried.Coord == new GridCoord(0, 0));
            Assert.Contains(room.CarriedEntities, carried => carried.Coord == new GridCoord(0, 0) && carried.TemplateId == "rock");
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void CarriedEntityOperationsReportInvalidRequests()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var moveOccupied = service.MoveCarriedEntity("editorRoom", "northWall", new GridCoord(2, 1));
            var replaceSelf = service.ReplaceCarriedEntityTemplate("editorRoom", "northWall", "editorRoom");
            var removeMissing = service.RemoveCarriedEntity("editorRoom", "missingEntity");

            Assert.False(moveOccupied.IsSuccess);
            Assert.Contains("occupied", moveOccupied.StatusMessage, StringComparison.OrdinalIgnoreCase);
            Assert.False(replaceSelf.IsSuccess);
            Assert.Contains("itself", replaceSelf.StatusMessage, StringComparison.OrdinalIgnoreCase);
            Assert.False(removeMissing.IsSuccess);
            Assert.Contains("missingEntity", removeMissing.StatusMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void CreateEntityTemplateAddsDefaultTemplateAndPresentation()
    {
        var service = FrontendEditorService.CreateNew();

        var result = service.CreateEntityTemplate("New Actor");

        Assert.True(result.IsSuccess, result.StatusMessage);
        Assert.True(result.Snapshot.IsDirty);
        Assert.Contains("Preview stale", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
        var template = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == "newActor");
        Assert.Equal("New Actor", template.Name);
        Assert.Equal('?', template.Glyph);
        Assert.Equal(PresentationColor.Gray, template.Color);
        Assert.Equal(0, template.InventoryWidth);
        Assert.Equal(0, template.InventoryHeight);
        Assert.Equal(0, template.Bulk);
        Assert.Equal(0, template.Aperture);
    }

    [Fact]
    public void CreateEntityTemplateRejectsBlankName()
    {
        var service = FrontendEditorService.CreateNew();

        var result = service.CreateEntityTemplate("   ");

        Assert.False(result.IsSuccess);
        Assert.Contains("name", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Snapshot.EntityTemplates);
    }

    [Fact]
    public void DuplicateEntityTemplateCopiesPresentationMetadataAndCarriedLayout()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.DuplicateEntityTemplate("editorRoom", "Copied Room");

            Assert.True(result.IsSuccess, result.StatusMessage);
            Assert.True(result.Snapshot.IsDirty);
            var duplicate = Assert.Single(result.Snapshot.EntityTemplates, template => template.TemplateId == "copiedRoom");
            Assert.Equal("Copied Room", duplicate.Name);
            Assert.Equal('#', duplicate.Glyph);
            Assert.Equal(PresentationColor.Gray, duplicate.Color);
            Assert.Equal(3, duplicate.InventoryWidth);
            Assert.Equal(2, duplicate.InventoryHeight);
            Assert.Equal(2, duplicate.CarriedEntities.Count);
            Assert.Contains(duplicate.CarriedEntities, carried => carried.EntityId == "copiedRoomNorthWall" && carried.TemplateId == "wall" && carried.Coord == new GridCoord(0, 0));
            Assert.Contains(duplicate.CarriedEntities, carried => carried.EntityId == "copiedRoomFloorRock" && carried.TemplateId == "rock" && carried.Coord == new GridCoord(2, 1));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void DuplicateEntityTemplateRejectsMissingSourceAndBlankName()
    {
        var service = FrontendEditorService.CreateNew();
        service.CreateEntityTemplate("Source");

        var missing = service.DuplicateEntityTemplate("missing", "Copy");
        var blank = service.DuplicateEntityTemplate("source", " ");

        Assert.False(missing.IsSuccess);
        Assert.Contains("missing", missing.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.False(blank.IsSuccess);
        Assert.Contains("name", blank.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeleteEntityTemplateRemovesUnreferencedTemplateAndPresentation()
    {
        var service = FrontendEditorService.CreateNew();
        service.CreateEntityTemplate("Temporary Template");

        var result = service.DeleteEntityTemplate("temporaryTemplate");

        Assert.True(result.IsSuccess, result.StatusMessage);
        Assert.True(result.Snapshot.IsDirty);
        Assert.DoesNotContain(result.Snapshot.EntityTemplates, template => template.TemplateId == "temporaryTemplate");
        Assert.DoesNotContain("temporaryTemplate", result.Snapshot.YamlPreview, StringComparison.Ordinal);
    }

    [Fact]
    public void DeleteEntityTemplateRejectsReferencedOrMissingTemplates()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var referenced = service.DeleteEntityTemplate("wall");
            var missing = service.DeleteEntityTemplate("missingTemplate");

            Assert.False(referenced.IsSuccess);
            Assert.Contains("referenced", referenced.StatusMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(referenced.Snapshot.EntityTemplates, template => template.TemplateId == "wall");
            Assert.False(missing.IsSuccess);
            Assert.Contains("missingTemplate", missing.StatusMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void CreateActionPlanAddsEditableWaitPlan()
    {
        var service = FrontendEditorService.CreateNew();

        var result = service.CreateActionPlan("New Plan");

        Assert.True(result.IsSuccess, result.StatusMessage);
        Assert.True(result.Snapshot.IsDirty);
        Assert.Contains("Preview stale", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
        var plan = Assert.Single(result.Snapshot.ActionPlans, plan => plan.ActionPlanId == "newPlan");
        Assert.Equal("Legacy / Advanced Low-Level Steps", plan.Shape);
        Assert.Equal(["wait"], plan.ActionStepNames);
    }

    [Fact]
    public void InsertActionPlanStepConvertsNewPlaceholderPlanToCanonicalBehavior()
    {
        var service = FrontendEditorService.CreateNew();
        var create = service.CreateActionPlan("Player Debug");
        Assert.True(create.IsSuccess, create.StatusMessage);

        var result = service.InsertActionPlanStep("playerDebug", 0, ActionPlanBehaviorStepKind.Move);

        Assert.True(result.IsSuccess, result.StatusMessage);
        var plan = Assert.Single(result.Snapshot.ActionPlans, plan => plan.ActionPlanId == "playerDebug");
        Assert.Equal("Canonical Behavior Chain", plan.Shape);
        var step = Assert.Single(plan.ActionSteps);
        Assert.Equal(ActionPlanBehaviorStepKind.Move, step.Kind);
        Assert.Equal(["Move"], plan.ActionStepNames);
    }

    [Fact]
    public void CreatePassiveActionPlanAddsEmptyPassivePlan()
    {
        var service = FrontendEditorService.CreateNew();

        var result = service.CreatePassiveActionPlan("Passive Plan");

        Assert.True(result.IsSuccess, result.StatusMessage);
        Assert.True(result.Snapshot.IsDirty);
        var plan = Assert.Single(result.Snapshot.ActionPlans, plan => plan.ActionPlanId == "passivePlan");
        Assert.Equal("Empty / Passive", plan.Shape);
        Assert.Empty(plan.ActionStepNames);
        Assert.Empty(plan.ActionSteps);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateActionPlanRejectsBlankName(bool passive)
    {
        var service = FrontendEditorService.CreateNew();

        var result = passive
            ? service.CreatePassiveActionPlan(" ")
            : service.CreateActionPlan(" ");

        Assert.False(result.IsSuccess);
        Assert.Contains("name", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Snapshot.ActionPlans);
    }

    [Fact]
    public void DuplicateActionPlanCopiesCanonicalBehaviorWithNewId()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var result = service.DuplicateActionPlan("moveEast", "Move East Copy");

            Assert.True(result.IsSuccess, result.StatusMessage);
            Assert.True(result.Snapshot.IsDirty);
            var plan = Assert.Single(result.Snapshot.ActionPlans, plan => plan.ActionPlanId == "moveEastCopy");
            Assert.Equal("Canonical Behavior Chain", plan.Shape);
            var step = Assert.Single(plan.ActionSteps);
            Assert.Equal(0, step.Index);
            Assert.Equal(ActionPlanBehaviorStepKind.MoveFacing, step.Kind);
            Assert.Equal("Move Facing", step.DisplayName);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void DuplicateActionPlanRejectsMissingSourceAndBlankName()
    {
        var service = FrontendEditorService.CreateNew();
        service.CreateActionPlan("Source Plan");

        var missing = service.DuplicateActionPlan("missingPlan", "Copy Plan");
        var blank = service.DuplicateActionPlan("sourcePlan", " ");

        Assert.False(missing.IsSuccess);
        Assert.Contains("missingPlan", missing.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.False(blank.IsSuccess);
        Assert.Contains("name", blank.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeleteActionPlanRemovesUnreferencedPlan()
    {
        var service = FrontendEditorService.CreateNew();
        service.CreateActionPlan("Temporary Plan");

        var result = service.DeleteActionPlan("temporaryPlan");

        Assert.True(result.IsSuccess, result.StatusMessage);
        Assert.True(result.Snapshot.IsDirty);
        Assert.DoesNotContain(result.Snapshot.ActionPlans, plan => plan.ActionPlanId == "temporaryPlan");
        Assert.DoesNotContain("temporaryPlan", result.Snapshot.YamlPreview, StringComparison.Ordinal);
    }

    [Fact]
    public void DeleteActionPlanRejectsReferencedOrMissingPlans()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var referenced = service.DeleteActionPlan("moveEast");
            var missing = service.DeleteActionPlan("missingPlan");

            Assert.False(referenced.IsSuccess);
            Assert.Contains("referenced", referenced.StatusMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(referenced.Snapshot.ActionPlans, plan => plan.ActionPlanId == "moveEast");
            Assert.False(missing.IsSuccess);
            Assert.Contains("missingPlan", missing.StatusMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    private static string EditorFixtureYaml() =>
        """
        entityTemplates:
          editorRoom:
            name: Editor Room
            inventoryWidth: 3
            inventoryHeight: 2
            weight: 100
            carryingCapacity: 100
            carriedEntities:
            - entityId: northWall
              templateId: wall
              coord:
                x: 0
                y: 0
            - entityId: floorRock
              templateId: rock
              coord:
                x: 2
                y: 1
          editorPlayer:
            name: Editor Player
            inventoryWidth: 1
            inventoryHeight: 1
            weight: 1
            carryingCapacity: 5
            defaultActionPlanId: moveEast
            actionStateDefaults:
              facing: East
            targetingRules:
            - slot: 1
              label: nearbywall
              hint: Obstacle
              targetTemplateId: wall
              range: 5
          wall:
            name: Wall
            inventoryWidth: 0
            inventoryHeight: 0
            weight: 10
            carryingCapacity: 0
          rock:
            name: Rock
            inventoryWidth: 0
            inventoryHeight: 0
            weight: 1
            carryingCapacity: 0
        presentations:
          editorRoom:
            glyph: '#'
            color: Gray
          editorPlayer:
            glyph: '@'
            color: Yellow
          wall:
            glyph: '#'
            color: Earth
          rock:
            glyph: '*'
            color: Earth
        actionPlans:
          moveEast:
            id: moveEast
            behavior:
              steps:
              - kind: MoveFacing
        scenarios:
          editor-smoke:
            name: Editor Smoke
            scenarioRootEntityTemplateId: editorRoom
            playerEntityTemplateId: editorPlayer
            playerEntityId: editorPlayer
            playerStart:
              x: 1
              y: 1
        """;

    private static string WriteTempContentFile(string yaml)
    {
        var path = Path.Combine(Path.GetTempPath(), $"frontend-editor-service-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(path, yaml);
        return path;
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
