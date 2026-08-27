using GameGameGame.Content;
using GameGameGame.Core;
using System.Runtime.CompilerServices;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class ContentRegistryValidationTests
{
    [Fact]
    public void PrototypeRegistryValidationPassesForBuiltInContent()
    {
        var registry = PrototypeContent.CreateRegistry();

        var result = registry.Validate();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void CanonicalDebugStandardPlayerLoadoutIncludesPush()
    {
        var path = FindRepositoryFile(Path.Combine("src", "GameGameGame.Content", "Beta", "Debug", "CanonicalDebugRooms.yaml"));
        var document = EditableContentDocument.LoadYaml(File.ReadAllText(path));

        var step = document
            .ToRegistry()
            .GetActionPlanDescriptor(new ActionPlanTemplateId("Player_Debug_Action_Plan"))
            .Behavior!
            .Steps
            .Single(step => step.Kind == ActionPlanBehaviorStepKind.Push);

        Assert.Equal(1, step.TargetSlot);
        Assert.Equal(ActionPlanMoveDirectionMode.Forward, step.DirectionMode);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsMissingTemplateDefaultPlan()
    {
        var missingPlanId = new ActionPlanTemplateId("missingPlan");
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                PrototypeContent.RockTemplateId,
                PrototypeContent.CreateRockTemplate() with { DefaultActionPlanId = missingPlanId });

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("missingPlan") && error.Contains("Rock"));
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingActionPlanReference);
        Assert.Equal(new EntityTemplateId("rock"), diagnostic.EntityTemplateId);
        Assert.Equal(missingPlanId, diagnostic.ActionPlanTemplateId);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsInvalidTargetingRules()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                PrototypeContent.RockTemplateId,
                PrototypeContent.CreateRockTemplate() with
                {
                    TargetingRules =
                    [
                        new EntityTargetingRule(0, new EntityTemplateId("missing"), -1, "Invalid")
                    ]
                });

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidTargetingRule && diagnostic.Message.Contains("slot"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidTargetingRule && diagnostic.Message.Contains("range"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingTargetTemplateReference && diagnostic.Message.Contains("missing"));
    }

    [Fact]
    public void PrototypeRegistryValidationReportsInvalidTargetingCapabilityAdjectives()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                PrototypeContent.RockTemplateId,
                PrototypeContent.CreateRockTemplate() with
                {
                    TargetingRules =
                    [
                        new EntityTargetingRule(1, null, 3, Label: "loves", TargetCapabilities: [ActionPlanBehaviorStepKind.SeekTarget]),
                        new EntityTargetingRule(2, null, 3, Label: "wants", TargetCapabilities: [ActionPlanBehaviorStepKind.PickupTarget])
                    ]
                });

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidTargetingRule && diagnostic.Message.Contains("unsupported target capability SeekTarget"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidTargetingRule && diagnostic.Message.Contains("PickupTarget") && diagnostic.Message.Contains("default action plan"));
    }

    [Fact]
    public void PrototypeRegistryValidationReportsInvalidPreferredTargetingProfileRules()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                PrototypeContent.RockTemplateId,
                PrototypeContent.CreateRockTemplate() with
                {
                    Targeting = new EntityTargetingProfile(
                        3,
                        Rules:
                        [
                            new EntityTargetingRule(0, new EntityTemplateId("missing"), Label: "bad"),
                            new EntityTargetingRule(1, null, Label: "wants", TargetCapabilities: [ActionPlanBehaviorStepKind.PickupTarget])
                        ])
                });

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidTargetingRule && diagnostic.Message.Contains("slot"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingTargetTemplateReference && diagnostic.Message.Contains("missing"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidTargetingRule && diagnostic.Message.Contains("PickupTarget") && diagnostic.Message.Contains("default action plan"));
    }

    [Fact]
    public void PrototypeRegistryValidationReportsInvalidEmptyTargetingProfileRange()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                PrototypeContent.RockTemplateId,
                PrototypeContent.CreateRockTemplate() with
                {
                    Targeting = new EntityTargetingProfile(-1, DefaultLocality: new TargetingLocalityQuery())
                });

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidTargetingRule && diagnostic.Message.Contains("targeting profile range"));
    }

    [Fact]
    public void PreferredTargetingProfileRulesSatisfyTargetActionStateContract()
    {
        var planId = new ActionPlanTemplateId("profileTargetPlan");
        var targetTemplateId = new EntityTemplateId("target");
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                planId,
                new ActionPlanDescriptor(
                    new ActionPlanId("profileTargetPlan"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(
                            ActionPlanBehaviorStepKind.TargetPathMove,
                            TargetLabel: "danger",
                            PathMode: ActionPlanTargetPathMode.SeekAdjacency)
                    ])))
            .WithEntityTemplate(
                targetTemplateId,
                PrototypeContent.CreateRockTemplate() with { Name = "Target" })
            .WithEntityTemplate(
                PrototypeContent.RockTemplateId,
                PrototypeContent.CreateRockTemplate() with
                {
                    DefaultActionPlanId = planId,
                    Targeting = new EntityTargetingProfile(
                        4,
                        Rules: [new EntityTargetingRule(1, targetTemplateId, Label: "danger")])
                });

        var result = registry.Validate();

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanSlot && diagnostic.ActionPlanSlot == ActionPlanSlot.Target);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidTargetingRule);
    }

    [Fact]
    public void PreferredTargetingProfileCapabilityCanBeConsumedByCanonicalTransferCounterparty()
    {
        var planId = new ActionPlanTemplateId("giveToDeposit");
        var itemTemplateId = new EntityTemplateId("item");
        var depositTemplateId = new EntityTemplateId("deposit");
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                planId,
                new ActionPlanDescriptor(
                    new ActionPlanId("giveToDeposit"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(
                            ActionPlanBehaviorStepKind.Transfer,
                            TargetLabel: "item",
                            CounterpartyTargetLabel: "deposit",
                            TransferDirection: TransferDirection.ActorToTarget)
                    ])))
            .WithEntityTemplate(
                itemTemplateId,
                PrototypeContent.CreateRockTemplate() with { Name = "Item" })
            .WithEntityTemplate(
                depositTemplateId,
                PrototypeContent.CreateRockTemplate() with { Name = "Deposit" })
            .WithEntityTemplate(
                PrototypeContent.RockTemplateId,
                PrototypeContent.CreateRockTemplate() with
                {
                    DefaultActionPlanId = planId,
                    Targeting = new EntityTargetingProfile(
                        4,
                        Rules:
                        [
                            new EntityTargetingRule(1, itemTemplateId, Label: "item"),
                            new EntityTargetingRule(2, depositTemplateId, Label: "deposit", TargetCapabilities: [ActionPlanBehaviorStepKind.GiveTarget])
                        ])
                });

        var result = registry.Validate();

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidTargetingRule && diagnostic.Message.Contains("GiveTarget"));
    }

    [Fact]
    public void PrototypeRegistryValidationReportsInvalidBehaviorTargetSlot()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("invalidTargetSlot"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.SeekTarget, TargetSlot: 0)
                    ])));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidActionStepTargetSlot);
        Assert.Equal(PrototypeContent.WanderingActionPlanTemplateId, diagnostic.ActionPlanTemplateId);
        Assert.Equal(new ActionPlanId("invalidTargetSlot"), diagnostic.ActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsBehaviorTargetWithSlotAndLabel()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("invalidTargetReference"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.SeekTarget, TargetSlot: 1, TargetLabel: "fears")
                    ])));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidActionStepTargetReference);
    }

    [Fact]
    public void PrototypeRegistryValidationRejectsLegacyTargetingAndCoordinateMovementActionSteps()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("legacyCoordinateMovement"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.AcquireNearestTarget)
                    ])));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.UnsupportedLegacyActionStep);
        Assert.Equal(PrototypeContent.WanderingActionPlanTemplateId, diagnostic.ActionPlanTemplateId);
        Assert.Equal(new ActionPlanId("legacyCoordinateMovement"), diagnostic.ActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
        Assert.Contains("graph-first targeting", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsCanonicalMoveMissingDirectionMode()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("canonicalMoveMissingDirectionMode"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Move)
                    ])));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidActionStepField);
        Assert.Equal(new ActionPlanId("canonicalMoveMissingDirectionMode"), diagnostic.ActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
        Assert.Contains("directionMode", diagnostic.Message);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsMalformedCanonicalTransferFields()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("canonicalTransferMalformed"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Transfer)
                    ])));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidActionStepField && diagnostic.Message.Contains("directionMode"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidActionStepField && diagnostic.Message.Contains("transferDirection"));
    }

    private static string FindRepositoryFile(string relativePath, [CallerFilePath] string sourceFilePath = "")
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

    [Fact]
    public void PrototypeRegistryValidationReportsCanonicalPushMissingDirectionMode()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("canonicalPushMissingDirectionMode"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Push)
                    ])));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidActionStepField);
        Assert.Equal(new ActionPlanId("canonicalPushMissingDirectionMode"), diagnostic.ActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
        Assert.Contains("directionMode", diagnostic.Message);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsUnknownPresentationAndPaletteIds()
    {
        var templateId = new EntityTemplateId("rock");
        var registry = PrototypeContent.CreateRegistry()
            .WithPresentation(
                templateId,
                new EntityPresentation(
                    new PresentationId("creature.unknown"),
                    new PaletteId("palette.unknown"),
                    '?',
                    PresentationColor.Gray));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == ContentDiagnosticCode.UnknownPresentationId
            && diagnostic.EntityTemplateId == templateId
            && diagnostic.Message.Contains("creature.unknown"));
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == ContentDiagnosticCode.UnknownPaletteId
            && diagnostic.EntityTemplateId == templateId
            && diagnostic.Message.Contains("palette.unknown"));
    }

    [Fact]
    public void PrototypeRegistryValidationReportsInvalidTargetPathMoveFields()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("targetPathMalformed"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.TargetPathMove),
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.TargetPathMove, PathMode: ActionPlanTargetPathMode.MaintainDistance),
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.TargetPathMove, PathMode: ActionPlanTargetPathMode.Orbit, DesiredDistance: 2),
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.TargetPathMove, PathMode: ActionPlanTargetPathMode.SeekAdjacency, DesiredDistance: 1),
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.TargetPathMove, PathMode: ActionPlanTargetPathMode.FleeAdjacency, OrbitDirection: ActionPlanOrbitDirection.Clockwise),
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.TargetPathMove, PathMode: ActionPlanTargetPathMode.MaintainDistance, DesiredDistance: -1)
                    ])));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidActionStepField && diagnostic.StepIndex == 0 && diagnostic.Message.Contains("pathMode"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidActionStepField && diagnostic.StepIndex == 1 && diagnostic.Message.Contains("desiredDistance"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidActionStepField && diagnostic.StepIndex == 2 && diagnostic.Message.Contains("orbitDirection"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidActionStepField && diagnostic.StepIndex == 3 && diagnostic.Message.Contains("desiredDistance") && diagnostic.Message.Contains("not support"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidActionStepField && diagnostic.StepIndex == 4 && diagnostic.Message.Contains("orbitDirection") && diagnostic.Message.Contains("only"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidActionStepField && diagnostic.StepIndex == 5 && diagnostic.Message.Contains("non-negative"));
    }

    [Fact]
    public void PrototypeRegistryValidationReportsMissingCreateEntityTemplateReference()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("createMissingTemplate"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.CreateEntity, TemplateId: "missingRat")
                    ])));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingTargetTemplateReference);
        Assert.Equal(new ActionPlanId("createMissingTemplate"), diagnostic.ActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
        Assert.Contains("missingRat", diagnostic.Message);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsMissingPolymorphTargetTemplateReference()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("polymorphMissingTemplate"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PolymorphTarget, TargetSelf: true, TemplateId: "missingButterfly")
                    ])));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingTargetTemplateReference);
        Assert.Equal(new ActionPlanId("polymorphMissingTemplate"), diagnostic.ActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
        Assert.Contains("missingButterfly", diagnostic.Message);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsCreateEntityFacingPlacementMissingDirectionMode()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("createFacingMissingDirection"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(
                            ActionPlanBehaviorStepKind.CreateEntity,
                            TemplateId: PrototypeContent.RockTemplateId.Value,
                            CreatePlacement: CreateEntityPlacement.Facing)
                    ])));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidActionStepField && diagnostic.Message.Contains("directionMode"));
    }

    [Fact]
    public void PrototypeRegistryValidationReportsUnknownCostTemplate()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("unknownCostTemplate"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing)
                        {
                            Costs = [new ActionStepCostDescriptor("missingScrap", 1)]
                        }
                    ])));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingTargetTemplateReference);
        Assert.Equal(new ActionPlanId("unknownCostTemplate"), diagnostic.ActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
        Assert.Contains("missingScrap", diagnostic.Message);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsNonPositiveCostQuantity()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("nonPositiveCostQuantity"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing)
                        {
                            Costs = [new ActionStepCostDescriptor(PrototypeContent.RockTemplateId.Value, 0)]
                        }
                    ])));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidActionStepField);
        Assert.Equal(new ActionPlanId("nonPositiveCostQuantity"), diagnostic.ActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
        Assert.Contains("quantity", diagnostic.Message);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsDuplicateCostTemplateEntries()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("duplicateCostTemplate"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing)
                        {
                            Costs = [
                                new ActionStepCostDescriptor(PrototypeContent.RockTemplateId.Value, 1),
                                new ActionStepCostDescriptor(PrototypeContent.RockTemplateId.Value, 2)
                            ]
                        }
                    ])));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidActionStepField);
        Assert.Equal(new ActionPlanId("duplicateCostTemplate"), diagnostic.ActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
        Assert.Contains("duplicate cost", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsMissingApplyPrePlanReference()
    {
        var missingPlanId = new ActionPlanId("missingFear");
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("applyMissingPrePlan"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.ApplyPrePlan, PlanId: missingPlanId)
                    ])));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingActionPlanReference);
        Assert.Equal(PrototypeContent.WanderingActionPlanTemplateId, diagnostic.ActionPlanTemplateId);
        Assert.Equal(new ActionPlanId("applyMissingPrePlan"), diagnostic.ActionPlanId);
        Assert.Equal(missingPlanId, diagnostic.ReferencedActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
    }

    [Theory]
    [InlineData(ActionPlanBehaviorStepKind.ApplyMainPlan)]
    [InlineData(ActionPlanBehaviorStepKind.ApplyPostPlan)]
    public void PrototypeRegistryValidationReportsMissingApplyPlanReference(ActionPlanBehaviorStepKind stepKind)
    {
        var missingPlanId = new ActionPlanId("missingOverride");
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("applyMissingOverride"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(stepKind, PlanId: missingPlanId)
                    ])));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingActionPlanReference);
        Assert.Equal(missingPlanId, diagnostic.ReferencedActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsMissingApplyPrePlanPlanId()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("applyMissingPrePlanId"),
                    [],
                    Behavior: new ActionPlanBehaviorDescriptor([
                        new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.ApplyPrePlan)
                    ])));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingActionPlanReference);
        Assert.Equal(new ActionPlanId("applyMissingPrePlanId"), diagnostic.ActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsMissingCalledPlan()
    {
        var missingPlanId = new ActionPlanId("missingNestedPlan");
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("invalidWandering"),
                    [
                        new ActionPlanStepDescriptor(
                            "call missing",
                            [],
                            PlanEffectDescriptor.CallPlan(missingPlanId),
                            OnFailure: null)
                    ]));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("missingNestedPlan") && error.Contains("invalidWandering"));
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingCalledPlan);
        Assert.Equal(PrototypeContent.WanderingActionPlanTemplateId, diagnostic.ActionPlanTemplateId);
        Assert.Equal(new ActionPlanId("invalidWandering"), diagnostic.ActionPlanId);
        Assert.Equal(missingPlanId, diagnostic.ReferencedActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsMissingPrimitiveFallbackPlan()
    {
        var missingPlanId = new ActionPlanId("missingFallback");
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("primitiveMove"),
                    [],
                    new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing, missingPlanId)));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingCalledPlan);
        Assert.Equal(PrototypeContent.WanderingActionPlanTemplateId, diagnostic.ActionPlanTemplateId);
        Assert.Equal(new ActionPlanId("primitiveMove"), diagnostic.ActionPlanId);
        Assert.Equal(missingPlanId, diagnostic.ReferencedActionPlanId);
        Assert.Null(diagnostic.StepIndex);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsInvalidMovementTargetDescriptor()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("invalidMovement"),
                    [
                        new ActionPlanStepDescriptor(
                            "teleport missing entity",
                            [],
                            PlanEffectDescriptor.Teleport(
                                new MovementTargetDescriptor(MovementTargetKind.Entity),
                                MovementDestinationDescriptor.Plane(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0)))),
                            OnFailure: null)
                    ]));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidMovementDescriptor);
        Assert.Equal(new ActionPlanId("invalidMovement"), diagnostic.ActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
        Assert.Contains("entityId", diagnostic.Message);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsInvalidMovementDestinationDescriptor()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithActionPlanDescriptor(
                PrototypeContent.WanderingActionPlanTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("invalidMovement"),
                    [
                        new ActionPlanStepDescriptor(
                            "drop missing direction",
                            [],
                            PlanEffectDescriptor.Drop(
                                MovementTargetDescriptor.CarriedInventoryCoord(new GridCoord(0, 0)),
                                new MovementDestinationDescriptor(MovementDestinationKind.AdjacentToSelf)),
                            OnFailure: null)
                    ]));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidMovementDescriptor);
        Assert.Equal(new ActionPlanId("invalidMovement"), diagnostic.ActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
        Assert.Contains("direction", diagnostic.Message);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsMissingCanonicalFacingSlot()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: moveFacing
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              moveFacing:
                id: moveFacing
                steps:
                  - label: move facing
                    checks:
                      - kind: CanMove
                    onSuccess:
                      kind: Move
            """);

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanSlot);
        Assert.Equal(new EntityTemplateId("slime"), diagnostic.EntityTemplateId);
        Assert.Equal(new ActionPlanTemplateId("moveFacing"), diagnostic.ActionPlanTemplateId);
        Assert.Equal(new ActionPlanId("moveFacing"), diagnostic.ActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
        Assert.Equal(ActionPlanSlot.Facing, diagnostic.ActionPlanSlot);
        Assert.Equal(PlanValueKind.Direction, diagnostic.ExpectedValueKind);
    }

    [Fact]
    public void PrototypeRegistryValidationAcceptsCanonicalFacingDefault()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: moveFacing
                actionStateDefaults:
                  facing: West
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              moveFacing:
                id: moveFacing
                steps:
                  - label: move facing
                    checks:
                      - kind: CanMove
                    onSuccess:
                      kind: Move
            """);

        var result = registry.Validate();

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanSlot);
    }

    [Fact]
    public void PrototypeRegistryValidationAcceptsDefaultableFacingForPrimitiveMoveFacing()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: moveFacing
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              moveFacing:
                id: moveFacing
                primitive:
                  kind: MoveFacing
            """);

        var result = registry.Validate();

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanSlot);
    }

    [Fact]
    public void PrototypeRegistryValidationAcceptsPrimitiveMoveFacingWithFacingDefault()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: moveFacing
                actionStateDefaults:
                  facing: West
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              moveFacing:
                id: moveFacing
                primitive:
                  kind: MoveFacing
            """);

        var result = registry.Validate();

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanSlot);
    }

    [Fact]
    public void PrototypeRegistryValidationAcceptsDefaultableTargetForPrimitivePickupTarget()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: pickupTarget
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              pickupTarget:
                id: pickupTarget
                primitive:
                  kind: PickupTarget
            """);

        var result = registry.Validate();

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanSlot);
    }

    [Fact]
    public void PrototypeRegistryValidationAcceptsPrimitivePickupTargetAfterMoveFacingFallbackWritesTarget()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: moveFacing
                actionStateDefaults:
                  facing: North
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              moveFacing:
                id: moveFacing
                primitive:
                  kind: MoveFacing
                  fallbackPlanId: pickupTarget
              pickupTarget:
                id: pickupTarget
                primitive:
                  kind: PickupTarget
            """);

        var result = registry.Validate();

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanSlot);
    }

    [Fact]
    public void PrototypeRegistryValidationAcceptsBehaviorChainPickupTargetAfterMoveFacingWritesTarget()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: behaviorChain
                actionStateDefaults:
                  facing: North
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              behaviorChain:
                id: behaviorChain
                behavior:
                  steps:
                    - kind: MoveFacing
                    - kind: PickupTarget
                    - kind: TurnLeft
                    - kind: TurnRight
                    - kind: ReverseFacing
            """);

        var result = registry.Validate();

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanSlot);
    }

    [Fact]
    public void PrototypeRegistryValidationAcceptsDefaultableStateForBehaviorChain()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: behaviorChain
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              behaviorChain:
                id: behaviorChain
                behavior:
                  steps:
                    - kind: MoveFacing
                    - kind: PickupTarget
            """);

        var result = registry.Validate();

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanSlot);
    }

    [Fact]
    public void PrototypeRegistryValidationAcceptsDefaultableFacingForTurnActionSteps()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: turnBehavior
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              turnBehavior:
                id: turnBehavior
                behavior:
                  steps:
                    - kind: TurnLeft
                    - kind: TurnRight
                    - kind: ReverseFacing
            """);

        var result = registry.Validate();

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanSlot);
    }

    [Fact]
    public void PrototypeRegistryValidationAcceptsDefaultableFacingForBackstep()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: backstepBehavior
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              backstepBehavior:
                id: backstepBehavior
                behavior:
                  steps:
                    - kind: Backstep
            """);

        var result = registry.Validate();

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanSlot);
    }

    [Fact]
    public void PrototypeRegistryValidationAcceptsPickupTargetAsFirstBehaviorStep()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: behaviorChain
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              behaviorChain:
                id: behaviorChain
                behavior:
                  steps:
                    - kind: PickupTarget
            """);

        var result = registry.Validate();

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanSlot);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsMixedActionPlanShapes()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              mixed:
                id: mixed
                behavior:
                  steps:
                    - kind: MoveFacing
                steps:
                  - label: wait
                    checks: []
                    onSuccess:
                      kind: Wait
            """);

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidActionPlanShape);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsEmptyBehaviorChain()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              emptyBehavior:
                id: emptyBehavior
                behavior:
                  steps: []
            """);

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == ContentDiagnosticCode.InvalidActionPlanShape
            && diagnostic.Message.Contains("empty behavior chain"));
    }

    [Fact]
    public void PrototypeRegistryValidationAcceptsCanonicalTargetWrittenBeforePickup()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: handleBlocker
                actionStateDefaults:
                  facing: South
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              handleBlocker:
                id: handleBlocker
                steps:
                  - label: bind target
                    checks:
                      - kind: BlockingEntity
                    onSuccess:
                      kind: Pickup
                      inventoryCoord:
                        x: 0
                        y: 0
            """);

        var result = registry.Validate();

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanSlot);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsCanonicalTargetReadBeforeWrite()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: pickupTarget
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              pickupTarget:
                id: pickupTarget
                steps:
                  - label: pickup target
                    checks:
                      - kind: CanPickup
                        inventoryCoord:
                          x: 0
                          y: 0
                    onSuccess:
                      kind: Pickup
                      inventoryCoord:
                        x: 0
                        y: 0
            """);

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanSlot);
        Assert.Equal(ActionPlanSlot.Target, diagnostic.ActionPlanSlot);
        Assert.Equal(PlanValueKind.Entity, diagnostic.ExpectedValueKind);
        Assert.Equal(0, diagnostic.StepIndex);
    }

    [Fact]
    public void PrototypeRegistryValidationIncludesCalledPlanCanonicalSlotRequirements()
    {
        var registry = YamlContentLoader.LoadRegistry(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: parent
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              parent:
                id: parent
                steps:
                  - label: call child
                    checks: []
                    onSuccess:
                      kind: CallPlan
                      planId: child
              child:
                id: child
                steps:
                  - label: move facing
                    checks:
                      - kind: CanMove
                    onSuccess:
                      kind: Move
            """);

        var result = registry.Validate();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanSlot);
        Assert.Equal(new ActionPlanId("child"), diagnostic.ActionPlanId);
        Assert.Equal(ActionPlanSlot.Facing, diagnostic.ActionPlanSlot);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsMissingPresentationAsStructuredDiagnostic()
    {
        var templateId = new EntityTemplateId("invisibleRock");
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                templateId,
                new EntityTemplate(
                    "Invisible Rock",
                    InventoryWidth: 0,
                    InventoryHeight: 0,
                    Bulk: 3,
                    Aperture: 3));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("invisibleRock") && error.Contains("no presentation"));
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPresentation);
        Assert.Equal(templateId, diagnostic.EntityTemplateId);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsCarriedEntityOutsideInventoryBounds()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                new EntityTemplateId("badBag"),
                new EntityTemplate(
                    "Bad Bag",
                    InventoryWidth: 1,
                    InventoryHeight: 1,
                    Bulk: 1,
                    Aperture: 10,
                    CarriedEntities:
                    [
                        new CarriedEntityTemplate(new EntityId("outsideRock"), PrototypeContent.RockTemplateId, new GridCoord(1, 0))
                    ]));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("badBag") && error.Contains("outsideRock") && error.Contains("outside inventory bounds"));
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InventoryOutOfBounds);
        Assert.Equal(ContentDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(new EntityTemplateId("badBag"), diagnostic.EntityTemplateId);
        Assert.Equal(new EntityId("outsideRock"), diagnostic.CarriedEntityId);
        Assert.Equal(new GridCoord(1, 0), diagnostic.Coord);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsOverlappingCarriedEntities()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                new EntityTemplateId("crowdedBag"),
                new EntityTemplate(
                    "Crowded Bag",
                    InventoryWidth: 2,
                    InventoryHeight: 1,
                    Bulk: 1,
                    Aperture: 10,
                    CarriedEntities:
                    [
                        new CarriedEntityTemplate(new EntityId("firstRock"), PrototypeContent.RockTemplateId, new GridCoord(0, 0)),
                        new CarriedEntityTemplate(new EntityId("secondRock"), PrototypeContent.RockTemplateId, new GridCoord(0, 0))
                    ]));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("crowdedBag") && error.Contains("firstRock") && error.Contains("secondRock") && error.Contains("overlap"));
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InventoryOverlap);
        Assert.Equal(new EntityTemplateId("crowdedBag"), diagnostic.EntityTemplateId);
        Assert.Equal(new EntityId("secondRock"), diagnostic.CarriedEntityId);
        Assert.Equal(new EntityId("firstRock"), diagnostic.RelatedEntityId);
        Assert.Equal(new GridCoord(0, 0), diagnostic.Coord);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsDuplicateCarriedEntityIds()
    {
        var duplicateId = new EntityId("duplicateRock");
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                new EntityTemplateId("duplicateBag"),
                new EntityTemplate(
                    "Duplicate Bag",
                    InventoryWidth: 2,
                    InventoryHeight: 1,
                    Bulk: 1,
                    Aperture: 10,
                    CarriedEntities:
                    [
                        new CarriedEntityTemplate(duplicateId, PrototypeContent.RockTemplateId, new GridCoord(0, 0)),
                        new CarriedEntityTemplate(duplicateId, PrototypeContent.RockTemplateId, new GridCoord(1, 0))
                    ]));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("duplicateBag") && error.Contains("duplicateRock") && error.Contains("duplicate carried entity ID"));
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.DuplicateCarriedEntityId);
        Assert.Equal(new EntityTemplateId("duplicateBag"), diagnostic.EntityTemplateId);
        Assert.Equal(new EntityId("duplicateRock"), diagnostic.CarriedEntityId);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsCarriedEntitiesOnTemplateWithoutUsableInventory()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                new EntityTemplateId("pocketlessBag"),
                new EntityTemplate(
                    "Pocketless Bag",
                    InventoryWidth: 0,
                    InventoryHeight: 0,
                    Bulk: 1,
                    Aperture: 10,
                    CarriedEntities:
                    [
                        new CarriedEntityTemplate(new EntityId("trappedRock"), PrototypeContent.RockTemplateId, new GridCoord(0, 0))
                    ]));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("pocketlessBag") && error.Contains("trappedRock") && error.Contains("no usable inventory"));
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.CarriedEntityWithoutUsableInventory);
        Assert.Equal(new EntityTemplateId("pocketlessBag"), diagnostic.EntityTemplateId);
        Assert.Equal(new EntityId("trappedRock"), diagnostic.CarriedEntityId);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsMissingCarriedTemplateAsStructuredDiagnostic()
    {
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                new EntityTemplateId("badBag"),
                new EntityTemplate(
                    "Bad Bag",
                    InventoryWidth: 1,
                    InventoryHeight: 1,
                    Bulk: 1,
                    Aperture: 10,
                    CarriedEntities:
                    [
                        new CarriedEntityTemplate(new EntityId("ghostRock"), new EntityTemplateId("missingRock"), new GridCoord(0, 0))
                    ]));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("badBag") && error.Contains("ghostRock") && error.Contains("missingRock"));
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingCarriedEntityTemplateReference);
        Assert.Equal(new EntityTemplateId("badBag"), diagnostic.EntityTemplateId);
        Assert.Equal(new EntityId("ghostRock"), diagnostic.CarriedEntityId);
        Assert.Equal(new EntityTemplateId("missingRock"), diagnostic.ReferencedEntityTemplateId);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsMissingRequiredPlanVariable()
    {
        var planTemplateId = new ActionPlanTemplateId("needsDirection");
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                PrototypeContent.RockTemplateId,
                PrototypeContent.CreateRockTemplate() with { DefaultActionPlanId = planTemplateId })
            .WithActionPlanDescriptor(
                planTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("needsDirection"),
                    [
                        new ActionPlanStepDescriptor(
                            "move missing variable",
                            [PlanCheckDescriptor.CanMove("facing")],
                            PlanEffectDescriptor.Move("facing"),
                            OnFailure: null)
                    ]));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Rock") && error.Contains("facing") && error.Contains("missing required variable"));
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanVariable);
        Assert.Equal(PrototypeContent.RockTemplateId, diagnostic.EntityTemplateId);
        Assert.Equal(planTemplateId, diagnostic.ActionPlanTemplateId);
        Assert.Equal(new ActionPlanId("needsDirection"), diagnostic.ActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
        Assert.Equal("facing", diagnostic.VariableName);
        Assert.Equal(PlanValueKind.Direction, diagnostic.ExpectedValueKind);
    }

    [Fact]
    public void PrototypeRegistryValidationReportsPlanVariableTypeMismatch()
    {
        var planTemplateId = new ActionPlanTemplateId("wrongDirectionType");
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                PrototypeContent.RockTemplateId,
                PrototypeContent.CreateRockTemplate() with
                {
                    DefaultActionPlanId = planTemplateId,
                    DefaultPlanVariables = new Dictionary<string, PlanValueDescriptor>
                    {
                        ["facing"] = PlanValueDescriptor.Int(1)
                    }
                })
            .WithActionPlanDescriptor(
                planTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("wrongDirectionType"),
                    [
                        new ActionPlanStepDescriptor(
                            "move wrong variable type",
                            [PlanCheckDescriptor.CanMove("facing")],
                            PlanEffectDescriptor.Move("facing"),
                            OnFailure: null)
                    ]));

        var result = registry.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Rock") && error.Contains("facing") && error.Contains("expected Direction") && error.Contains("found Int"));
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.PlanVariableTypeMismatch);
        Assert.Equal(PrototypeContent.RockTemplateId, diagnostic.EntityTemplateId);
        Assert.Equal(planTemplateId, diagnostic.ActionPlanTemplateId);
        Assert.Equal(new ActionPlanId("wrongDirectionType"), diagnostic.ActionPlanId);
        Assert.Equal(0, diagnostic.StepIndex);
        Assert.Equal("facing", diagnostic.VariableName);
        Assert.Equal(PlanValueKind.Direction, diagnostic.ExpectedValueKind);
        Assert.Equal(PlanValueKind.Int, diagnostic.ActualValueKind);
    }

    [Fact]
    public void ContentValidationResultCanFilterDiagnosticsByEntityTemplate()
    {
        var badBagId = new EntityTemplateId("badBag");
        var otherBagId = new EntityTemplateId("otherBag");
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                badBagId,
                new EntityTemplate(
                    "Bad Bag",
                    InventoryWidth: 1,
                    InventoryHeight: 1,
                    Bulk: 1,
                    Aperture: 10,
                    CarriedEntities:
                    [
                        new CarriedEntityTemplate(new EntityId("outsideRock"), PrototypeContent.RockTemplateId, new GridCoord(2, 0))
                    ]))
            .WithEntityTemplate(
                otherBagId,
                new EntityTemplate(
                    "Other Bag",
                    InventoryWidth: 0,
                    InventoryHeight: 0,
                    Bulk: 1,
                    Aperture: 10,
                    CarriedEntities:
                    [
                        new CarriedEntityTemplate(new EntityId("trappedRock"), PrototypeContent.RockTemplateId, new GridCoord(0, 0))
                    ]));

        var result = registry.Validate();

        var badBagDiagnostics = result.ForEntityTemplate(badBagId);
        Assert.Contains(badBagDiagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InventoryOutOfBounds);
        Assert.DoesNotContain(badBagDiagnostics, diagnostic => diagnostic.EntityTemplateId == otherBagId);
    }

    [Fact]
    public void ContentValidationResultCanFilterDiagnosticsByActionPlanAndStep()
    {
        var planTemplateId = new ActionPlanTemplateId("needsDirection");
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                PrototypeContent.RockTemplateId,
                PrototypeContent.CreateRockTemplate() with { DefaultActionPlanId = planTemplateId })
            .WithActionPlanDescriptor(
                planTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("needsDirection"),
                    [
                        new ActionPlanStepDescriptor(
                            "wait first",
                            [],
                            PlanEffectDescriptor.Wait(),
                            OnFailure: null),
                        new ActionPlanStepDescriptor(
                            "move missing variable",
                            [PlanCheckDescriptor.CanMove("facing")],
                            PlanEffectDescriptor.Move("facing"),
                            OnFailure: null)
                    ]));

        var result = registry.Validate();

        var planDiagnostics = result.ForActionPlan(planTemplateId);
        var stepDiagnostics = result.ForActionPlanStep(planTemplateId, stepIndex: 1);
        Assert.Contains(planDiagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanVariable);
        var diagnostic = Assert.Single(stepDiagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanVariable);
        Assert.Equal(1, diagnostic.StepIndex);
    }

    [Fact]
    public void ContentValidationResultCanFilterDiagnosticsByCarriedEntity()
    {
        var bagId = new EntityTemplateId("badBag");
        var carriedId = new EntityId("outsideRock");
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                bagId,
                new EntityTemplate(
                    "Bad Bag",
                    InventoryWidth: 1,
                    InventoryHeight: 1,
                    Bulk: 1,
                    Aperture: 10,
                    CarriedEntities:
                    [
                        new CarriedEntityTemplate(carriedId, PrototypeContent.RockTemplateId, new GridCoord(2, 0))
                    ]));

        var result = registry.Validate();

        var diagnostics = result.ForCarriedEntity(bagId, carriedId);
        var diagnostic = Assert.Single(diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InventoryOutOfBounds);
        Assert.Equal(new GridCoord(2, 0), diagnostic.Coord);
    }

    [Fact]
    public void PrototypeRegistryValidationAcceptsVariablesWrittenByChecksBeforeLaterReads()
    {
        var planTemplateId = new ActionPlanTemplateId("writeThenRead");
        var registry = PrototypeContent.CreateRegistry()
            .WithEntityTemplate(
                PrototypeContent.RockTemplateId,
                PrototypeContent.CreateRockTemplate() with
                {
                    DefaultActionPlanId = planTemplateId,
                    DefaultPlanVariables = new Dictionary<string, PlanValueDescriptor>
                    {
                        ["facing"] = PlanValueDescriptor.Direction(Direction.West)
                    }
                })
            .WithActionPlanDescriptor(
                planTemplateId,
                new ActionPlanDescriptor(
                    new ActionPlanId("writeThenRead"),
                    [
                        new ActionPlanStepDescriptor(
                            "find target",
                            [PlanCheckDescriptor.BlockingEntity("facing", "target")],
                            PlanEffectDescriptor.Pickup("target", new GridCoord(0, 0)),
                            OnFailure: null)
                    ]));

        var result = registry.Validate();

        Assert.DoesNotContain(result.Errors, error => error.Contains("target") && error.Contains("missing required variable"));
    }
}
