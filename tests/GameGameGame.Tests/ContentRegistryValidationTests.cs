using GameGameGame.Content;
using GameGameGame.Core;

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
    public void PrototypeRegistryValidationReportsMissingFacingForPrimitiveMoveFacing()
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

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanSlot);
        Assert.Equal(new EntityTemplateId("slime"), diagnostic.EntityTemplateId);
        Assert.Equal(new ActionPlanTemplateId("moveFacing"), diagnostic.ActionPlanTemplateId);
        Assert.Equal(new ActionPlanId("moveFacing"), diagnostic.ActionPlanId);
        Assert.Null(diagnostic.StepIndex);
        Assert.Equal(ActionPlanSlot.Facing, diagnostic.ActionPlanSlot);
        Assert.Equal(PlanValueKind.Direction, diagnostic.ExpectedValueKind);
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
    public void PrototypeRegistryValidationReportsMissingTargetForPrimitivePickupTarget()
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

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingPlanSlot);
        Assert.Equal(new EntityTemplateId("slime"), diagnostic.EntityTemplateId);
        Assert.Equal(new ActionPlanTemplateId("pickupTarget"), diagnostic.ActionPlanTemplateId);
        Assert.Equal(new ActionPlanId("pickupTarget"), diagnostic.ActionPlanId);
        Assert.Equal(ActionPlanSlot.Target, diagnostic.ActionPlanSlot);
        Assert.Equal(PlanValueKind.Entity, diagnostic.ExpectedValueKind);
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
                    Weight: 3,
                    CarryingCapacity: 3));

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
                    Weight: 1,
                    CarryingCapacity: 10,
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
                    Weight: 1,
                    CarryingCapacity: 10,
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
                    Weight: 1,
                    CarryingCapacity: 10,
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
                    Weight: 1,
                    CarryingCapacity: 10,
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
                    Weight: 1,
                    CarryingCapacity: 10,
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
                    Weight: 1,
                    CarryingCapacity: 10,
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
                    Weight: 1,
                    CarryingCapacity: 10,
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
