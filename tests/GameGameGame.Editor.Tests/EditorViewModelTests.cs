using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.Editor;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Editor)]
public sealed class EditorViewModelTests
{
    [Fact]
    public void EditorViewModelOpensContentFileAndListsEntityPresets()
    {
        var path = WriteTempContentFile(BasicContentYaml);

        try
        {
            var editor = new MainEditorViewModel();

            var result = editor.OpenFile(path);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(path, editor.FilePath);
            Assert.False(editor.IsDirty);
            var preset = Assert.Single(editor.EntityPresets);
            Assert.Equal(new EntityTemplateId("rock"), preset.Id);
            Assert.Equal("Rock", preset.Name);
            Assert.Equal('*', preset.Glyph);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelEditsSelectedPresetAndUpdatesPreviewDiffAndValidation()
    {
        var path = WriteTempContentFile(BasicContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);

            editor.SelectEntityPreset(new EntityTemplateId("rock"));
            editor.SelectedName = "Editor Rock";
            editor.SelectedGlyph = "R";
            editor.SelectedColor = PresentationColor.White;
            editor.SelectedBulk = 5;
            editor.ApplySelectedEntityPresetEdits();

            Assert.True(editor.IsDirty);
            Assert.Empty(editor.ValidationMessages);
            Assert.Contains("Editor Rock", editor.YamlPreview);
            Assert.Contains(editor.YamlDiffLines, line => line.StartsWith("+") && line.Contains("Editor Rock"));
            Assert.Equal("Editor Rock", editor.EntityPresets.Single().Name);
            Assert.Equal('R', editor.EntityPresets.Single().Glyph);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelApplyKeepsSelectedPresetWhenUiClearsSelectionDuringRefresh()
    {
        var path = WriteTempContentFile(BasicContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("rock"));
            editor.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainEditorViewModel.IsDirty))
                {
                    editor.SelectedPreset = null;
                }
            };

            editor.ApplySelectedEntityPresetEdits();

            Assert.NotNull(editor.SelectedPreset);
            Assert.Equal(new EntityTemplateId("rock"), editor.SelectedPreset.Id);
            Assert.Equal("Applied edits to Rock.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSavesAndClearsDirtyStateAndDiff()
    {
        var path = WriteTempContentFile(BasicContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("rock"));
            editor.SelectedName = "Saved Editor Rock";
            editor.ApplySelectedEntityPresetEdits();

            var result = editor.Save();

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.False(editor.IsDirty);
            Assert.Empty(editor.YamlDiffLines);
            Assert.Equal("Saved Editor Rock", YamlContentLoader.LoadRegistryFile(path).EntityTemplates[new EntityTemplateId("rock")].Name);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSaveAsWritesNewPathAndClearsDirtyStateAndDiff()
    {
        var path = WriteTempContentFile(BasicContentYaml);
        var saveAsPath = Path.Combine(Path.GetTempPath(), $"game-editor-viewmodel-save-as-{Guid.NewGuid():N}.yaml");

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("rock"));
            editor.SelectedName = "Saved As Rock";
            editor.ApplySelectedEntityPresetEdits();

            var result = editor.SaveAs(saveAsPath);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(saveAsPath, editor.FilePath);
            Assert.False(editor.IsDirty);
            Assert.Empty(editor.YamlDiffLines);
            Assert.Equal("Saved As Rock", YamlContentLoader.LoadRegistryFile(saveAsPath).EntityTemplates[new EntityTemplateId("rock")].Name);
            Assert.Equal("Saved as.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
            DeleteIfExists(saveAsPath);
        }
    }

    [Fact]
    public void EditorViewModelReloadDiscardsUnsavedEditsAndRefreshesUi()
    {
        var path = WriteTempContentFile(BasicContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("rock"));
            editor.SelectedName = "Unsaved Rock";
            editor.SelectedGlyph = "R";
            editor.ApplySelectedEntityPresetEdits();

            var result = editor.Reload();

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.False(editor.IsDirty);
            Assert.Empty(editor.YamlDiffLines);
            Assert.Equal(new EntityTemplateId("rock"), editor.SelectedPreset?.Id);
            Assert.Equal("Rock", editor.SelectedName);
            Assert.Equal("*", editor.SelectedGlyph);
            Assert.Equal("Reloaded.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelCreatesNewDocumentWithoutPath()
    {
        var editor = new MainEditorViewModel();

        editor.CreateNewDocument();

        Assert.Null(editor.FilePath);
        Assert.False(editor.IsDirty);
        Assert.Empty(editor.EntityPresets);
        Assert.Empty(editor.ValidationMessages);
        Assert.Equal("Created new content document.", editor.StatusMessage);
    }

    [Fact]
    public void EditorViewModelCreatesEntityPresetAndSelectsIt()
    {
        var path = WriteTempContentFile(BasicContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);

            editor.EntityPresetNameInput = "New Bag";
            editor.CreateEntityPreset();

            var created = editor.EntityPresets.Single(item => item.Id == new EntityTemplateId("newBag"));
            Assert.Equal(created, editor.SelectedPreset);
            Assert.Equal("New Bag", editor.SelectedName);
            Assert.Equal("Created New Bag.", editor.StatusMessage);
            Assert.True(editor.IsDirty);
            Assert.Contains("newBag", editor.YamlPreview);
            Assert.Contains(editor.YamlDiffLines, line => line.StartsWith("+") && line.Contains("newBag"));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelDuplicatesSelectedEntityPresetAndSelectsCopy()
    {
        var path = WriteTempContentFile(InventoryContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));

            editor.EntityPresetNameInput = "Bag Copy";
            editor.DuplicateSelectedEntityPreset();

            var duplicate = editor.EntityPresets.Single(item => item.Id == new EntityTemplateId("bagCopy"));
            Assert.Equal(duplicate, editor.SelectedPreset);
            Assert.Equal("Bag Copy", editor.SelectedName);
            Assert.Single(editor.CarriedEntities);
            Assert.Equal("Duplicated Bag as Bag Copy.", editor.StatusMessage);
            Assert.Contains("bagCopy", editor.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelDeletesUnreferencedSelectedEntityPreset()
    {
        var path = WriteTempContentFile(BasicContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("rock"));

            editor.DeleteSelectedEntityPreset();

            Assert.Empty(editor.EntityPresets);
            Assert.Null(editor.SelectedPreset);
            Assert.Equal("Deleted Rock.", editor.StatusMessage);
            Assert.True(editor.IsDirty);
            Assert.DoesNotContain("rock:", editor.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelDoesNotDeleteReferencedSelectedEntityPreset()
    {
        var path = WriteTempContentFile(InventoryContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("rock"));

            editor.DeleteSelectedEntityPreset();

            Assert.Contains(editor.EntityPresets, item => item.Id == new EntityTemplateId("rock"));
            Assert.Equal(new EntityTemplateId("rock"), editor.SelectedPreset?.Id);
            Assert.Contains("Cannot delete entity template rock", editor.StatusMessage);
            Assert.Contains("carriedRock", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelListsActionPlansAndShowsSelectedPresetDefaultPlan()
    {
        var path = WriteTempContentFile(ActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);

            editor.SelectEntityPreset(new EntityTemplateId("slime"));

            var actionPlan = Assert.Single(editor.ActionPlans);
            Assert.Equal(new ActionPlanTemplateId("wander"), actionPlan.Id);
            Assert.Equal(actionPlan, editor.SelectedDefaultActionPlan);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSelectingActionPlanShowsReadableStepSummaries()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);

            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));

            Assert.Collection(
                editor.ActionPlanSteps,
                step =>
                {
                    Assert.Equal(0, step.Index);
                    Assert.Equal("move", step.Label);
                    Assert.Equal("Checks: CanMove(directionVariable=facing)", step.ChecksSummary);
                    Assert.Equal("Success: Move(directionVariable=facing)", step.SuccessSummary);
                    Assert.Equal("Failure: CallPlan(planId=handleBlocker)", step.FailureSummary);
                },
                step =>
                {
                    Assert.Equal(1, step.Index);
                    Assert.Equal("wait", step.Label);
                    Assert.Equal("Checks: none", step.ChecksSummary);
                    Assert.Equal("Success: Wait", step.SuccessSummary);
                    Assert.Equal("Failure: none", step.FailureSummary);
                });
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSelectingBehaviorPlanShowsCanonicalActionSteps()
    {
        var path = WriteTempContentFile(BehaviorChainContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);

            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("ratBehavior"));

            Assert.Equal("Canonical Behavior Chain", editor.SelectedActionPlanShape);
            Assert.Empty(editor.ActionPlanSteps);
            Assert.Collection(
                editor.BehaviorSteps,
                step =>
                {
                    Assert.Equal(0, step.Index);
                    Assert.Equal(ActionPlanBehaviorStepKind.MoveFacing, step.Kind);
                    Assert.Equal("Move Facing", step.DisplayName);
                    Assert.Equal("Requires: Facing:Direction", step.RequiredStateSummary);
                    Assert.Equal("Defaults: Facing:Direction", step.DefaultStateSummary);
                    Assert.Equal("Writes: Target:Entity", step.StateWritesSummary);
                },
                step =>
                {
                    Assert.Equal(1, step.Index);
                    Assert.Equal(ActionPlanBehaviorStepKind.PickupTarget, step.Kind);
                    Assert.Equal("Pickup Target", step.DisplayName);
                    Assert.Equal("Requires: Target:Entity", step.RequiredStateSummary);
                    Assert.Equal("Defaults: Target:Entity", step.DefaultStateSummary);
                    Assert.Equal("Writes: none", step.StateWritesSummary);
                });
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelShowsLegacyCompatibilityOnlyForLegacyLowLevelPlans()
    {
        var legacyPath = WriteTempContentFile(MultiStepActionPlanContentYaml);
        var behaviorPath = WriteTempContentFile(BehaviorChainContentYaml);

        try
        {
            var legacyEditor = new MainEditorViewModel();
            legacyEditor.OpenFile(legacyPath);
            legacyEditor.SelectedActionPlan = legacyEditor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));

            Assert.Equal("Legacy / Advanced Low-Level Steps", legacyEditor.SelectedActionPlanShape);
            Assert.True(legacyEditor.IsLegacyActionPlanCompatibilityVisible);

            var behaviorEditor = new MainEditorViewModel();
            behaviorEditor.OpenFile(behaviorPath);
            behaviorEditor.SelectedActionPlan = behaviorEditor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("ratBehavior"));

            Assert.Equal("Canonical Behavior Chain", behaviorEditor.SelectedActionPlanShape);
            Assert.False(behaviorEditor.IsLegacyActionPlanCompatibilityVisible);
        }
        finally
        {
            DeleteIfExists(legacyPath);
            DeleteIfExists(behaviorPath);
        }
    }

    [Fact]
    public void EditorViewModelAddsReordersAndRemovesBehaviorSteps()
    {
        var path = WriteTempContentFile(ActionPlanContentYamlWithoutAssignedPlan);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));

            editor.AddPickupTargetBehaviorStepToSelectedActionPlan();
            editor.AddMoveFacingBehaviorStepToSelectedActionPlan();
            editor.MoveSelectedBehaviorStepUp();

            Assert.Empty(editor.ActionPlanSteps);
            Assert.Equal(ActionPlanBehaviorStepKind.MoveFacing, editor.BehaviorSteps[0].Kind);
            Assert.Equal(0, editor.SelectedBehaviorStep?.Index);
            Assert.Contains("behavior:", editor.YamlPreview);
            Assert.DoesNotContain("label: wait", editor.YamlPreview);

            editor.RemoveSelectedBehaviorStep();

            var remaining = Assert.Single(editor.BehaviorSteps);
            Assert.Equal(ActionPlanBehaviorStepKind.PickupTarget, remaining.Kind);
            Assert.Equal("Removed Move Facing action step.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSelectingBehaviorStepShowsHint()
    {
        var path = WriteTempContentFile(BehaviorChainContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("ratBehavior"));

            editor.SelectedBehaviorStep = editor.BehaviorSteps[0];

            Assert.Contains("Facing", editor.SelectedBehaviorStepHint);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelReloadsSelectedActionPlanAfterRefresh()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));

            editor.EntityPresetNameInput = "New Rock";
            editor.CreateEntityPreset();

            Assert.Equal(new ActionPlanTemplateId("wander"), editor.SelectedActionPlan?.Id);
            Assert.Equal(2, editor.ActionPlanSteps.Count);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelCreatesActionPlanAndSelectsIt()
    {
        var path = WriteTempContentFile(BasicContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);

            editor.ActionPlanNameInput = "New Plan";
            editor.CreateActionPlan();

            var plan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("newPlan"));
            Assert.Equal(plan, editor.SelectedActionPlan);
            Assert.Equal("Empty / Passive", editor.SelectedActionPlanShape);
            Assert.False(editor.IsLegacyActionPlanCompatibilityVisible);
            Assert.Empty(editor.ActionPlanSteps);
            Assert.Contains("newPlan", editor.YamlPreview);
            Assert.Contains(editor.YamlDiffLines, line => line.StartsWith("+") && line.Contains("newPlan"));
            Assert.Equal("Created action plan New Plan.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelDuplicatesSelectedActionPlanAndSelectsCopy()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));

            editor.ActionPlanNameInput = "Wander Copy";
            editor.DuplicateSelectedActionPlan();

            var duplicate = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wanderCopy"));
            Assert.Equal(duplicate, editor.SelectedActionPlan);
            Assert.Equal(2, editor.ActionPlanSteps.Count);
            Assert.Contains("wanderCopy", editor.YamlPreview);
            Assert.Equal("Duplicated action plan wander as Wander Copy.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelDeletesUnreferencedSelectedActionPlan()
    {
        var path = WriteTempContentFile(ActionPlanContentYamlWithoutAssignedPlan);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));

            editor.DeleteSelectedActionPlan();

            Assert.Empty(editor.ActionPlans);
            Assert.Null(editor.SelectedActionPlan);
            Assert.Empty(editor.ActionPlanSteps);
            Assert.DoesNotContain("wander:", editor.YamlPreview);
            Assert.Equal("Deleted action plan wander.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelDoesNotDeleteReferencedSelectedActionPlan()
    {
        var path = WriteTempContentFile(ActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));

            editor.DeleteSelectedActionPlan();

            Assert.Contains(editor.ActionPlans, item => item.Id == new ActionPlanTemplateId("wander"));
            Assert.Equal(new ActionPlanTemplateId("wander"), editor.SelectedActionPlan?.Id);
            Assert.Contains("Cannot delete action plan wander", editor.StatusMessage);
            Assert.Contains("slime", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSelectingActionPlanStepPopulatesStepLabelInput()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));

            editor.SelectedActionPlanStep = editor.ActionPlanSteps[1];

            Assert.Equal("wait", editor.ActionPlanStepLabelInput);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelUpdatesSelectedActionPlanStepLabel()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[1];
            editor.ActionPlanStepLabelInput = "pause";

            editor.ApplySelectedActionPlanStepLabel();

            Assert.Equal("pause", editor.ActionPlanSteps[1].Label);
            Assert.Equal(1, editor.SelectedActionPlanStep?.Index);
            Assert.Contains("label: pause", editor.YamlPreview);
            Assert.Contains(editor.YamlDiffLines, line => line.StartsWith("+") && line.Contains("label: pause"));
            Assert.Equal("Updated step label to pause.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelAddsWaitStepToSelectedActionPlan()
    {
        var path = WriteTempContentFile(ActionPlanContentYamlWithoutAssignedPlan);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));

            editor.AddWaitStepToSelectedActionPlan();

            Assert.Equal(2, editor.ActionPlanSteps.Count);
            Assert.Equal("wait 2", editor.ActionPlanSteps[1].Label);
            Assert.Equal(1, editor.SelectedActionPlanStep?.Index);
            Assert.Contains("label: wait 2", editor.YamlPreview);
            Assert.Equal("Added wait step.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelMovesSelectedActionPlanStepUpAndDown()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[1];

            editor.MoveSelectedActionPlanStepUp();

            Assert.Equal("wait", editor.ActionPlanSteps[0].Label);
            Assert.Equal(0, editor.SelectedActionPlanStep?.Index);

            editor.MoveSelectedActionPlanStepDown();

            Assert.Equal("wait", editor.ActionPlanSteps[1].Label);
            Assert.Equal(1, editor.SelectedActionPlanStep?.Index);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelRemovesSelectedActionPlanStep()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[0];

            editor.RemoveSelectedActionPlanStep();

            var step = Assert.Single(editor.ActionPlanSteps);
            Assert.Equal("wait", step.Label);
            Assert.Equal(0, editor.SelectedActionPlanStep?.Index);
            Assert.DoesNotContain("label: move", editor.YamlPreview);
            Assert.Equal("Removed step move.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSelectingActionPlanStepPopulatesEffectInputs()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));

            editor.SelectedActionPlanStep = editor.ActionPlanSteps[0];

            Assert.Equal(PlanEffectKind.Move, editor.SelectedSuccessEffectKind);
            Assert.Equal(PlanEffectKind.CallPlan, editor.SelectedFailureEffectKind);
            Assert.Equal(editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("handleBlocker")), editor.SelectedFailureCallPlan);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSetsSelectedStepSuccessEffect()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[1];
            editor.SelectedSuccessEffectKind = PlanEffectKind.Move;

            editor.SetSelectedStepSuccessEffect();

            Assert.Equal("Success: Move", editor.ActionPlanSteps[1].SuccessSummary);
            Assert.Contains("kind: Move", editor.YamlPreview);
            Assert.DoesNotContain("directionVariable: facing", editor.YamlPreview);
            Assert.Equal("Updated success effect for step wait.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSetsAndClearsSelectedStepFailureEffect()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[1];
            editor.SelectedFailureEffectKind = PlanEffectKind.CallPlan;
            editor.SelectedFailureCallPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("handleBlocker"));

            editor.SetSelectedStepFailureEffect();

            Assert.Equal("Failure: CallPlan(planId=handleBlocker)", editor.ActionPlanSteps[1].FailureSummary);
            Assert.Contains("onFailure", editor.YamlPreview);

            editor.ClearSelectedStepFailureEffect();

            Assert.Equal("Failure: none", editor.ActionPlanSteps[1].FailureSummary);
            Assert.DoesNotContain("onFailure", editor.ActionPlanSteps[1].ToString());
            Assert.Equal("Cleared failure effect for step wait.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSelectingPickupEffectPopulatesInputs()
    {
        var path = WriteTempContentFile(PickupActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("pickupPlan"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[0];

            Assert.Equal(PlanEffectKind.Pickup, editor.SelectedSuccessEffectKind);
            Assert.Equal(1, editor.SuccessInventoryCoordX);
            Assert.Equal(2, editor.SuccessInventoryCoordY);
            Assert.True(editor.IsSuccessInventoryCoordVisible);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSetsSelectedStepPickupSuccessEffect()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[1];
            editor.SelectedSuccessEffectKind = PlanEffectKind.Pickup;
            editor.SuccessInventoryCoordX = 1;
            editor.SuccessInventoryCoordY = 2;

            editor.SetSelectedStepSuccessEffect();

            Assert.Equal("Success: Pickup(inventoryCoord=1,2)", editor.ActionPlanSteps[1].SuccessSummary);
            Assert.Contains("kind: Pickup", editor.YamlPreview);
            Assert.DoesNotContain("targetVariable: target", editor.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSelectingReverseDirectionEffectPopulatesInputs()
    {
        var path = WriteTempContentFile(ReverseDirectionActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[0];

            Assert.Equal(PlanEffectKind.ReverseDirection, editor.SelectedSuccessEffectKind);
            Assert.Equal("Success: ReverseDirection(directionVariable=facing, consumesTurn=True, continuePlan=False)", editor.ActionPlanSteps[0].SuccessSummary);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSetsSelectedStepReverseDirectionFailureEffect()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[1];
            editor.SelectedFailureEffectKind = PlanEffectKind.ReverseDirection;

            editor.SetSelectedStepFailureEffect();

            Assert.Equal("Failure: ReverseDirection(consumesTurn=False, continuePlan=False)", editor.ActionPlanSteps[1].FailureSummary);
            Assert.Contains("kind: ReverseDirection", editor.YamlPreview);
            Assert.DoesNotContain("continuePlan: true", editor.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelDisplaysLegacySetVariableEffectWithoutEditableInputs()
    {
        var path = WriteTempContentFile(SetVariableActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("setPlan"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[0];

            Assert.Equal(PlanEffectKind.SetVariable, editor.SelectedSuccessEffectKind);
            Assert.Equal("Success: SetVariable(variableName=facing, value=East, consumesTurn=True, continuePlan=False)", editor.ActionPlanSteps[0].SuccessSummary);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelDoesNotAuthorLegacySetVariableEffect()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[1];
            editor.SelectedSuccessEffectKind = PlanEffectKind.SetVariable;

            editor.SetSelectedStepSuccessEffect();

            Assert.Equal("Success: Wait", editor.ActionPlanSteps[1].SuccessSummary);
            Assert.DoesNotContain("kind: SetVariable", editor.YamlPreview);
            Assert.Equal("SetVariable is legacy-only and cannot be authored from the editor.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelExposesRelevantEffectFieldsForSelectedKinds()
    {
        var editor = new MainEditorViewModel();

        editor.SelectedSuccessEffectKind = PlanEffectKind.Wait;
        editor.SelectedFailureEffectKind = PlanEffectKind.CallPlan;

        Assert.False(editor.IsSuccessCallPlanVisible);
        Assert.True(editor.IsFailureCallPlanVisible);

        editor.SelectedSuccessEffectKind = PlanEffectKind.Move;
        editor.SelectedFailureEffectKind = PlanEffectKind.Wait;

        Assert.False(editor.IsSuccessCallPlanVisible);
        Assert.False(editor.IsFailureCallPlanVisible);

        editor.SelectedSuccessEffectKind = PlanEffectKind.Pickup;

        Assert.True(editor.IsSuccessInventoryCoordVisible);
    }

    [Fact]
    public void EditorViewModelExposesOnlyLiteralInputsForCanonicalPrimitives()
    {
        var editor = new MainEditorViewModel();

        editor.SelectedCheckKind = PlanCheckKind.CanMove;
        editor.SelectedCheckKind = PlanCheckKind.BlockingEntity;
        editor.SelectedCheckKind = PlanCheckKind.CanPickup;
        Assert.True(editor.IsCheckInventoryCoordVisible);

        editor.SelectedSuccessEffectKind = PlanEffectKind.Move;
        editor.SelectedSuccessEffectKind = PlanEffectKind.Pickup;
        Assert.True(editor.IsSuccessInventoryCoordVisible);
    }

    [Fact]
    public void EditorViewModelDoesNotOfferSetVariableForNewCanonicalEffects()
    {
        var editor = new MainEditorViewModel();

        Assert.DoesNotContain(PlanEffectKind.SetVariable, editor.EffectKinds);
        Assert.Contains(PlanEffectKind.Teleport, editor.EffectKinds);
        Assert.Contains(PlanEffectKind.Drop, editor.EffectKinds);
    }

    [Fact]
    public void EditorViewModelAuthorsTeleportAndDropMovementEffects()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[1];

            editor.SelectedSuccessEffectKind = PlanEffectKind.Teleport;
            editor.SelectedSuccessMovementTargetKind = MovementTargetKind.Entity;
            editor.SuccessMovementTargetEntityIdInput = "rock";
            editor.SelectedSuccessMovementDestinationKind = MovementDestinationKind.PlaneCoord;
            editor.SuccessMovementDestinationPlaneIdInput = "world";
            editor.SuccessMovementDestinationCoordX = 4;
            editor.SuccessMovementDestinationCoordY = 2;

            editor.SetSelectedStepSuccessEffect();

            Assert.Equal("Success: Teleport(movementTarget=Entity:rock, movementDestination=PlaneCoord:world(4,2))", editor.ActionPlanSteps[1].SuccessSummary);
            Assert.Contains("kind: Teleport", editor.YamlPreview);
            Assert.Contains("movementTarget", editor.YamlPreview);
            Assert.Contains("entityId: rock", editor.YamlPreview);
            Assert.Contains("planeId: world", editor.YamlPreview);

            editor.SelectedFailureEffectKind = PlanEffectKind.Drop;
            editor.SelectedFailureMovementTargetKind = MovementTargetKind.CarriedInventoryCoord;
            editor.FailureMovementTargetCoordX = 0;
            editor.FailureMovementTargetCoordY = 1;
            editor.SelectedFailureMovementDestinationKind = MovementDestinationKind.AdjacentToSelf;
            editor.FailureMovementDestinationDirection = Direction.West;

            editor.SetSelectedStepFailureEffect();

            Assert.Equal("Failure: Drop(movementTarget=CarriedInventoryCoord:0,1, movementDestination=AdjacentToSelf:West)", editor.ActionPlanSteps[1].FailureSummary);
            Assert.Contains("kind: Drop", editor.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSelectingActionPlanStepPopulatesCheckInputs()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[0];

            var check = Assert.Single(editor.ActionPlanStepChecks);
            Assert.Equal(0, check.Index);
            Assert.Equal(PlanCheckKind.CanMove, check.Kind);

            editor.SelectedActionPlanStepCheck = check;

            Assert.Equal(PlanCheckKind.CanMove, editor.SelectedCheckKind);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelAddsCanMoveCheckToSelectedStep()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[1];

            editor.AddCanMoveCheckToSelectedStep();

            var check = Assert.Single(editor.ActionPlanStepChecks);
            Assert.Equal("CanMove", check.Summary);
            Assert.Equal("Checks: CanMove", editor.ActionPlanSteps[1].ChecksSummary);
            Assert.Contains("kind: CanMove", editor.YamlPreview);
            Assert.DoesNotContain("directionVariable: facing", editor.YamlPreview);
            Assert.Equal("Added CanMove check to step wait.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelUpdatesSelectedCanMoveCheck()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[0];
            editor.SelectedActionPlanStepCheck = editor.ActionPlanStepChecks.Single();

            editor.UpdateSelectedStepCheck();

            var check = Assert.Single(editor.ActionPlanStepChecks);
            Assert.Equal("CanMove", check.Summary);
            Assert.DoesNotContain("directionVariable: turnDirection", editor.YamlPreview);
            Assert.Equal("Updated check 1 for step move.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelRemovesSelectedStepCheck()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[0];
            editor.SelectedActionPlanStepCheck = editor.ActionPlanStepChecks.Single();

            editor.RemoveSelectedStepCheck();

            Assert.Empty(editor.ActionPlanStepChecks);
            Assert.Equal("Checks: none", editor.ActionPlanSteps[0].ChecksSummary);
            Assert.DoesNotContain("kind: CanMove", editor.YamlPreview);
            Assert.Equal("Removed check 1 from step move.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelMovesSelectedStepCheckUpAndDown()
    {
        var path = WriteTempContentFile(MultiCheckActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[0];
            editor.SelectedActionPlanStepCheck = editor.ActionPlanStepChecks[1];

            editor.MoveSelectedStepCheckUp();

            Assert.Equal("CanMove(directionVariable=turnDirection)", editor.ActionPlanStepChecks[0].Summary);
            Assert.Equal(0, editor.SelectedActionPlanStepCheck?.Index);
            Assert.Equal("Moved check up.", editor.StatusMessage);

            editor.MoveSelectedStepCheckDown();

            Assert.Equal("CanMove(directionVariable=turnDirection)", editor.ActionPlanStepChecks[1].Summary);
            Assert.Equal(1, editor.SelectedActionPlanStepCheck?.Index);
            Assert.Contains("directionVariable: turnDirection", editor.YamlPreview);
            Assert.Equal("Moved check down.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSelectingBlockingEntityCheckPopulatesInputs()
    {
        var path = WriteTempContentFile(BlockingEntityCheckActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[0];
            editor.SelectedActionPlanStepCheck = editor.ActionPlanStepChecks.Single();

            Assert.Equal(PlanCheckKind.BlockingEntity, editor.SelectedCheckKind);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelAddsAndUpdatesBlockingEntityCheck()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[1];
            editor.SelectedCheckKind = PlanCheckKind.BlockingEntity;

            editor.AddSelectedCheckToSelectedStep();

            var check = Assert.Single(editor.ActionPlanStepChecks);
            Assert.Equal("BlockingEntity", check.Summary);

            editor.UpdateSelectedStepCheck();

            Assert.Equal("BlockingEntity", editor.ActionPlanStepChecks.Single().Summary);
            Assert.DoesNotContain("targetVariable: target", editor.YamlPreview);
            Assert.Equal("Updated check 1 for step wait.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelSelectingCanPickupCheckPopulatesInputs()
    {
        var path = WriteTempContentFile(PickupActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("pickupPlan"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[0];
            editor.SelectedActionPlanStepCheck = editor.ActionPlanStepChecks.Single();

            Assert.Equal(PlanCheckKind.CanPickup, editor.SelectedCheckKind);
            Assert.Equal(1, editor.CheckInventoryCoordX);
            Assert.Equal(2, editor.CheckInventoryCoordY);
            Assert.True(editor.IsCheckInventoryCoordVisible);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelAddsAndUpdatesCanPickupCheck()
    {
        var path = WriteTempContentFile(MultiStepActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[1];
            editor.SelectedCheckKind = PlanCheckKind.CanPickup;
            editor.CheckInventoryCoordX = 1;
            editor.CheckInventoryCoordY = 2;

            editor.AddSelectedCheckToSelectedStep();

            var check = Assert.Single(editor.ActionPlanStepChecks);
            Assert.Equal("CanPickup(inventoryCoord=1,2)", check.Summary);

            editor.CheckInventoryCoordX = 0;
            editor.CheckInventoryCoordY = 1;
            editor.UpdateSelectedStepCheck();

            Assert.Equal("CanPickup(inventoryCoord=0,1)", editor.ActionPlanStepChecks.Single().Summary);
            Assert.Contains("kind: CanPickup", editor.YamlPreview);
            Assert.Contains("x: 0", editor.YamlPreview);
            Assert.Contains("y: 1", editor.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelFiltersDiagnosticsForSelectedPresetActionPlanAndStep()
    {
        var path = WriteTempContentFile(ActionPlanContentYamlMissingEntityVariable);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("slime"));
            editor.SelectedActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("pickupPlan"));
            editor.SelectedActionPlanStep = editor.ActionPlanSteps[0];

            Assert.Contains(editor.SelectedPresetDiagnostics, message => message.Contains("target") && message.Contains("missing required variable"));
            Assert.Contains(editor.SelectedActionPlanDiagnostics, message => message.Contains("pickupPlan") && message.Contains("target"));
            Assert.Contains(editor.SelectedActionPlanStepDiagnostics, message => message.Contains("step pickup target") && message.Contains("target"));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelAssignsDefaultActionPlanAndRefreshesPreviewDiffAndValidation()
    {
        var path = WriteTempContentFile(ActionPlanContentYamlWithoutAssignedPlan);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("slime"));
            editor.SelectedDefaultActionPlan = editor.ActionPlans.Single(item => item.Id == new ActionPlanTemplateId("wander"));

            editor.AssignSelectedDefaultActionPlan();

            Assert.Equal(new ActionPlanTemplateId("wander"), editor.SelectedDefaultActionPlan?.Id);
            Assert.Contains("defaultActionPlanId: wander", editor.YamlPreview);
            Assert.Contains(editor.YamlDiffLines, line => line.StartsWith("+") && line.Contains("defaultActionPlanId: wander"));
            Assert.Equal("Assigned wander to Slime.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelClearsDefaultActionPlanAndRefreshesPreviewDiffAndValidation()
    {
        var path = WriteTempContentFile(ActionPlanContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("slime"));

            editor.ClearSelectedDefaultActionPlan();

            Assert.Null(editor.SelectedDefaultActionPlan);
            Assert.DoesNotContain("defaultActionPlanId", editor.YamlPreview);
            Assert.Contains(editor.YamlDiffLines, line => line.StartsWith("-") && line.Contains("defaultActionPlanId: wander"));
            Assert.Equal("Cleared default action plan for Slime.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelEditsInitialFacingAsActorState()
    {
        var path = WriteTempContentFile(ActionPlanContentYamlWithoutAssignedPlan);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("slime"));

            Assert.False(editor.HasInitialFacing);

            editor.SelectedInitialFacing = Direction.East;
            editor.SetInitialFacing();

            Assert.True(editor.HasInitialFacing);
            Assert.Equal(Direction.East, editor.SelectedInitialFacing);
            Assert.Contains("actionStateDefaults", editor.YamlPreview);
            Assert.Contains("facing: East", editor.YamlPreview);
            Assert.DoesNotContain("defaultPlanVariables", editor.YamlPreview);
            Assert.Equal("Set initial facing to East.", editor.StatusMessage);

            editor.ClearInitialFacing();

            Assert.False(editor.HasInitialFacing);
            Assert.DoesNotContain("actionStateDefaults", editor.YamlPreview);
            Assert.Equal("Cleared initial facing.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelListsCarriedEntitiesForSelectedPreset()
    {
        var path = WriteTempContentFile(InventoryContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);

            editor.SelectEntityPreset(new EntityTemplateId("bag"));

            var carried = Assert.Single(editor.CarriedEntities);
            Assert.Equal(new EntityId("carriedRock"), carried.EntityId);
            Assert.Equal(new EntityTemplateId("rock"), carried.TemplateId);
            Assert.Equal(new GridCoord(0, 0), carried.Coord);
            Assert.Equal("Rock", carried.TemplateName);
            Assert.Equal('*', carried.Glyph);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelBuildsInventoryGridCellsForSelectedPreset()
    {
        var path = WriteTempContentFile(InventoryContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));

            Assert.Collection(
                editor.InventoryGridCells,
                cell =>
                {
                    Assert.Equal(new GridCoord(0, 0), cell.Coord);
                    Assert.True(cell.IsOccupied);
                    Assert.Equal(new EntityId("carriedRock"), cell.CarriedEntityId);
                    Assert.Equal("* Rock", cell.DisplayText);
                },
                cell =>
                {
                    Assert.Equal(new GridCoord(1, 0), cell.Coord);
                    Assert.False(cell.IsOccupied);
                    Assert.Equal(".", cell.DisplayText);
                });
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelClickingOccupiedGridCellSelectsCarriedEntity()
    {
        var path = WriteTempContentFile(InventoryContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));

            editor.ClickInventoryGridCell(editor.InventoryGridCells.Single(cell => cell.Coord == new GridCoord(0, 0)));

            Assert.NotNull(editor.SelectedCarriedEntity);
            Assert.Equal(new EntityId("carriedRock"), editor.SelectedCarriedEntity.EntityId);
            Assert.Equal("Selected Rock at 0,0.", editor.StatusMessage);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelClickingEmptyGridCellPlacesSelectedTemplateThere()
    {
        var path = WriteTempContentFile(InventoryContentYamlWithoutCarriedEntity);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));
            editor.SelectedTemplateToPlace = editor.EntityPresets.Single(item => item.Id == new EntityTemplateId("rock"));

            editor.ClickInventoryGridCell(editor.InventoryGridCells.Single(cell => cell.Coord == new GridCoord(1, 0)));

            var carried = Assert.Single(editor.CarriedEntities);
            Assert.Equal(new GridCoord(1, 0), carried.Coord);
            Assert.Equal(carried.EntityId, editor.SelectedCarriedEntity?.EntityId);
            Assert.Contains("x: 1", editor.YamlPreview);
            Assert.Contains(editor.YamlDiffLines, line => line.StartsWith("+") && line.Contains("bagRock"));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelGridPlacementKeepsTemplateWhenUiClearsSelectionDuringRefresh()
    {
        var path = WriteTempContentFile(InventoryContentYamlWithoutCarriedEntity);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));
            editor.SelectedTemplateToPlace = editor.EntityPresets.Single(item => item.Id == new EntityTemplateId("rock"));
            editor.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainEditorViewModel.IsDirty))
                {
                    editor.SelectedTemplateToPlace = null;
                }
            };

            editor.ClickInventoryGridCell(editor.InventoryGridCells.Single(cell => cell.Coord == new GridCoord(1, 0)));

            Assert.Equal("Placed Rock at 1,0.", editor.StatusMessage);
            var carried = Assert.Single(editor.CarriedEntities);
            Assert.Equal(new GridCoord(1, 0), carried.Coord);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelClickingEmptyGridCellMovesSelectedCarriedEntityThere()
    {
        var path = WriteTempContentFile(InventoryContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));
            editor.ClickInventoryGridCell(editor.InventoryGridCells.Single(cell => cell.Coord == new GridCoord(0, 0)));

            editor.ClickInventoryGridCell(editor.InventoryGridCells.Single(cell => cell.Coord == new GridCoord(1, 0)));

            var carried = Assert.Single(editor.CarriedEntities);
            Assert.Equal(new GridCoord(1, 0), carried.Coord);
            Assert.Equal(carried.EntityId, editor.SelectedCarriedEntity?.EntityId);
            Assert.Contains("Moved Rock to 1,0.", editor.StatusMessage);
            Assert.Contains("x: 1", editor.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelPlacesCarriedEntityAndRefreshesPreviewDiffAndValidation()
    {
        var path = WriteTempContentFile(InventoryContentYamlWithoutCarriedEntity);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));

            editor.SelectedTemplateToPlace = editor.EntityPresets.Single(item => item.Id == new EntityTemplateId("rock"));
            editor.PlaceSelectedTemplateInInventory();

            var carried = Assert.Single(editor.CarriedEntities);
            Assert.Equal(new EntityId("bagRock"), carried.EntityId);
            Assert.Equal(new GridCoord(0, 0), carried.Coord);
            Assert.Empty(editor.ValidationMessages);
            Assert.Contains("bagRock", editor.YamlPreview);
            Assert.Contains(editor.YamlDiffLines, line => line.StartsWith("+") && line.Contains("bagRock"));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelFirstOpenPlacementKeepsTemplateWhenUiClearsSelectionDuringRefresh()
    {
        var path = WriteTempContentFile(InventoryContentYamlWithoutCarriedEntity);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));
            editor.SelectedTemplateToPlace = editor.EntityPresets.Single(item => item.Id == new EntityTemplateId("rock"));
            editor.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainEditorViewModel.IsDirty))
                {
                    editor.SelectedTemplateToPlace = null;
                }
            };

            editor.PlaceSelectedTemplateInInventory();

            Assert.Equal("Placed Rock.", editor.StatusMessage);
            Assert.Single(editor.CarriedEntities);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelReplacesSelectedCarriedEntityTemplate()
    {
        var path = WriteTempContentFile(InventoryContentYamlWithGem);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));
            editor.SelectedCarriedEntity = editor.CarriedEntities.Single();
            editor.SelectedReplacementTemplate = editor.EntityPresets.Single(item => item.Id == new EntityTemplateId("gem"));

            editor.ReplaceSelectedCarriedEntityTemplate();

            var carried = Assert.Single(editor.CarriedEntities);
            Assert.Equal(new EntityTemplateId("gem"), carried.TemplateId);
            Assert.Equal("Gem", carried.TemplateName);
            Assert.Contains("templateId: gem", editor.YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void EditorViewModelRemovesSelectedCarriedEntity()
    {
        var path = WriteTempContentFile(InventoryContentYaml);

        try
        {
            var editor = new MainEditorViewModel();
            editor.OpenFile(path);
            editor.SelectEntityPreset(new EntityTemplateId("bag"));
            editor.SelectedCarriedEntity = editor.CarriedEntities.Single();

            editor.RemoveSelectedCarriedEntity();

            Assert.Empty(editor.CarriedEntities);
            Assert.DoesNotContain("carriedRock", editor.YamlPreview);
            Assert.Contains(editor.YamlDiffLines, line => line.StartsWith("-") && line.Contains("carriedRock"));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    private const string BasicContentYaml =
        """
        entityTemplates:
          rock:
            name: Rock
            inventoryWidth: 0
            inventoryHeight: 0
            weight: 3
            carryingCapacity: 3
        presentations:
          rock:
            glyph: '*'
            color: Earth
        actionPlans: {}
        """;

    private const string InventoryContentYaml =
        """
        entityTemplates:
          bag:
            name: Bag
            inventoryWidth: 2
            inventoryHeight: 1
            weight: 1
            carryingCapacity: 10
            carriedEntities:
              - entityId: carriedRock
                templateId: rock
                coord:
                  x: 0
                  y: 0
          rock:
            name: Rock
            inventoryWidth: 0
            inventoryHeight: 0
            weight: 3
            carryingCapacity: 3
        presentations:
          bag:
            glyph: b
            color: Gray
          rock:
            glyph: '*'
            color: Earth
        actionPlans: {}
        """;

    private const string InventoryContentYamlWithoutCarriedEntity =
        """
        entityTemplates:
          bag:
            name: Bag
            inventoryWidth: 2
            inventoryHeight: 1
            weight: 1
            carryingCapacity: 10
          rock:
            name: Rock
            inventoryWidth: 0
            inventoryHeight: 0
            weight: 3
            carryingCapacity: 3
        presentations:
          bag:
            glyph: b
            color: Gray
          rock:
            glyph: '*'
            color: Earth
        actionPlans: {}
        """;

    private const string InventoryContentYamlWithGem =
        """
        entityTemplates:
          bag:
            name: Bag
            inventoryWidth: 2
            inventoryHeight: 1
            weight: 1
            carryingCapacity: 10
            carriedEntities:
              - entityId: carriedRock
                templateId: rock
                coord:
                  x: 0
                  y: 0
          rock:
            name: Rock
            inventoryWidth: 0
            inventoryHeight: 0
            weight: 3
            carryingCapacity: 3
          gem:
            name: Gem
            inventoryWidth: 0
            inventoryHeight: 0
            weight: 1
            carryingCapacity: 0
        presentations:
          bag:
            glyph: b
            color: Gray
          rock:
            glyph: '*'
            color: Earth
          gem:
            glyph: g
            color: Green
        actionPlans: {}
        """;

    private const string ActionPlanContentYaml =
        """
        entityTemplates:
          slime:
            name: Slime
            inventoryWidth: 1
            inventoryHeight: 1
            weight: 3
            carryingCapacity: 20
            defaultActionPlanId: wander
        presentations:
          slime:
            glyph: s
            color: Green
        actionPlans:
          wander:
            id: wander
            steps:
              - label: wait
                checks: []
                onSuccess:
                  kind: Wait
        """;

    private const string ActionPlanContentYamlWithoutAssignedPlan =
        """
        entityTemplates:
          slime:
            name: Slime
            inventoryWidth: 1
            inventoryHeight: 1
            weight: 3
            carryingCapacity: 20
        presentations:
          slime:
            glyph: s
            color: Green
        actionPlans:
          wander:
            id: wander
            steps:
              - label: wait
                checks: []
                onSuccess:
                  kind: Wait
        """;

    private const string ActionPlanContentYamlWithDefaultVariable =
        """
        entityTemplates:
          slime:
            name: Slime
            inventoryWidth: 1
            inventoryHeight: 1
            weight: 3
            carryingCapacity: 20
            defaultActionPlanId: wander
            defaultPlanVariables:
              facing:
                kind: Direction
                directionValue: West
        presentations:
          slime:
            glyph: s
            color: Green
        actionPlans:
          wander:
            id: wander
            steps:
              - label: wait
                checks: []
                onSuccess:
                  kind: Wait
        """;

    private const string ActionPlanContentYamlMissingDefaultVariable =
        """
        entityTemplates:
          slime:
            name: Slime
            inventoryWidth: 1
            inventoryHeight: 1
            weight: 3
            carryingCapacity: 20
            defaultActionPlanId: wander
        presentations:
          slime:
            glyph: s
            color: Green
        actionPlans:
          wander:
            id: wander
            steps:
              - label: move
                checks:
                  - kind: CanMove
                    directionVariable: facing
                onSuccess:
                  kind: Move
                  directionVariable: facing
        """;

    private const string ActionPlanContentYamlWithEntityDefaultVariable =
        """
        entityTemplates:
          slime:
            name: Slime
            inventoryWidth: 1
            inventoryHeight: 1
            weight: 3
            carryingCapacity: 20
            defaultPlanVariables:
              target:
                kind: Entity
                entityValue: blocker
        presentations:
          slime:
            glyph: s
            color: Green
        actionPlans: {}
        """;

    private const string ActionPlanContentYamlMissingEntityVariable =
        """
        entityTemplates:
          slime:
            name: Slime
            inventoryWidth: 1
            inventoryHeight: 1
            weight: 3
            carryingCapacity: 20
            defaultActionPlanId: pickupPlan
        presentations:
          slime:
            glyph: s
            color: Green
        actionPlans:
          pickupPlan:
            id: pickupPlan
            steps:
              - label: pickup target
                checks:
                  - kind: CanPickup
                    targetVariable: target
                    inventoryCoord:
                      x: 0
                      y: 0
                onSuccess:
                  kind: Pickup
                  targetVariable: target
                  inventoryCoord:
                    x: 0
                    y: 0
        """;

    private const string BehaviorChainContentYaml =
        """
        entityTemplates:
          slime:
            name: Slime
            inventoryWidth: 1
            inventoryHeight: 1
            weight: 3
            carryingCapacity: 20
            defaultActionPlanId: ratBehavior
            actionStateDefaults:
              facing: West
        presentations:
          slime:
            glyph: s
            color: Green
        actionPlans:
          ratBehavior:
            id: ratBehavior
            behavior:
              steps:
                - kind: MoveFacing
                - kind: PickupTarget
        """;

    private const string BehaviorChainMissingFacingYaml =
        """
        entityTemplates:
          slime:
            name: Slime
            inventoryWidth: 1
            inventoryHeight: 1
            weight: 3
            carryingCapacity: 20
            defaultActionPlanId: ratBehavior
        presentations:
          slime:
            glyph: s
            color: Green
        actionPlans:
          ratBehavior:
            id: ratBehavior
            behavior:
              steps:
                - kind: MoveFacing
                - kind: PickupTarget
        """;

    private const string MultiStepActionPlanContentYaml =
        """
        entityTemplates:
          slime:
            name: Slime
            inventoryWidth: 1
            inventoryHeight: 1
            weight: 3
            carryingCapacity: 20
        presentations:
          slime:
            glyph: s
            color: Green
        actionPlans:
          wander:
            id: wander
            steps:
              - label: move
                checks:
                  - kind: CanMove
                    directionVariable: facing
                onSuccess:
                  kind: Move
                  directionVariable: facing
                onFailure:
                  kind: CallPlan
                  planId: handleBlocker
              - label: wait
                checks: []
                onSuccess:
                  kind: Wait
          handleBlocker:
            id: handleBlocker
            steps:
              - label: wait
                checks: []
                onSuccess:
                  kind: Wait
        """;

    private const string MultiCheckActionPlanContentYaml =
        """
        entityTemplates:
          slime:
            name: Slime
            inventoryWidth: 1
            inventoryHeight: 1
            weight: 3
            carryingCapacity: 20
        presentations:
          slime:
            glyph: s
            color: Green
        actionPlans:
          wander:
            id: wander
            steps:
              - label: move
                checks:
                  - kind: CanMove
                    directionVariable: facing
                  - kind: CanMove
                    directionVariable: turnDirection
                onSuccess:
                  kind: Wait
        """;

    private const string BlockingEntityCheckActionPlanContentYaml =
        """
        entityTemplates:
          slime:
            name: Slime
            inventoryWidth: 1
            inventoryHeight: 1
            weight: 3
            carryingCapacity: 20
        presentations:
          slime:
            glyph: s
            color: Green
        actionPlans:
          wander:
            id: wander
            steps:
              - label: find blocker
                checks:
                  - kind: BlockingEntity
                    directionVariable: facing
                    targetVariable: target
                onSuccess:
                  kind: Wait
        """;

    private const string PickupActionPlanContentYaml =
        """
        entityTemplates:
          slime:
            name: Slime
            inventoryWidth: 2
            inventoryHeight: 3
            weight: 3
            carryingCapacity: 20
        presentations:
          slime:
            glyph: s
            color: Green
        actionPlans:
          pickupPlan:
            id: pickupPlan
            steps:
              - label: pickup target
                checks:
                  - kind: CanPickup
                    targetVariable: target
                    inventoryCoord:
                      x: 1
                      y: 2
                onSuccess:
                  kind: Pickup
                  targetVariable: target
                  inventoryCoord:
                    x: 1
                    y: 2
        """;

    private const string ReverseDirectionActionPlanContentYaml =
        """
        entityTemplates:
          slime:
            name: Slime
            inventoryWidth: 1
            inventoryHeight: 1
            weight: 3
            carryingCapacity: 20
        presentations:
          slime:
            glyph: s
            color: Green
        actionPlans:
          wander:
            id: wander
            steps:
              - label: reverse
                checks: []
                onSuccess:
                  kind: ReverseDirection
                  directionVariable: facing
                  consumesTurn: true
                  continuePlan: false
        """;

    private const string SetVariableActionPlanContentYaml =
        """
        entityTemplates:
          slime:
            name: Slime
            inventoryWidth: 1
            inventoryHeight: 1
            weight: 3
            carryingCapacity: 20
        presentations:
          slime:
            glyph: s
            color: Green
        actionPlans:
          setPlan:
            id: setPlan
            steps:
              - label: set facing
                checks: []
                onSuccess:
                  kind: SetVariable
                  variableName: facing
                  value:
                    kind: Direction
                    directionValue: East
                  consumesTurn: true
                  continuePlan: false
        """;

    private static string WriteTempContentFile(string yaml)
    {
        var path = Path.Combine(Path.GetTempPath(), $"game-editor-viewmodel-{Guid.NewGuid():N}.yaml");
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
