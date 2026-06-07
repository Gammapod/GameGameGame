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
