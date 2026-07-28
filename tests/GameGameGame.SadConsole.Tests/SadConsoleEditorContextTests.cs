using GameGameGame.SadConsoleApp;
using GameGameGame.Content;
using GameGameGame.Core;
using System.Reflection;

namespace GameGameGame.SadConsole.Tests;

public sealed class SadConsoleEditorContextTests
{
    [Fact]
    public void OpenSelectsRequestedScenarioAndViewLabelsAuthoredContext()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var result = SadConsoleEditorContext.Open(path, "second-smoke");

            Assert.True(result.IsSuccess, result.ErrorMessage);
            var context = result.Context!;
            var view = SadConsoleEditorViewBuilder.Build(context, "opened");

            Assert.Equal("second-smoke", context.SelectedScenario()!.ScenarioId);
            Assert.Contains("Editor mode (authored content", view.Header);
            Assert.Contains(path, view.FileLine);
            Assert.Contains("clean", view.DirtyLine);
            Assert.Contains("scenarios 2", view.CountLine);
            Assert.Contains("Selected authored scenario: Second Smoke (second-smoke)", view.SelectedScenarioLine);
            Assert.Contains(view.ScenarioRows, row => row.StartsWith(">", StringComparison.Ordinal) && row.Contains("second-smoke", StringComparison.Ordinal));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void MoveSelectionClampsToScenarioListAndPreservesSelectedIdentity()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;

            context.MoveSelection(99);
            Assert.Equal("second-smoke", context.SelectedScenario()!.ScenarioId);

            context.MoveSelection(-99);
            Assert.Equal("editor-smoke", context.SelectedScenario()!.ScenarioId);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void BrowserSectionsClampSelectionsAndShowAuthoredSummaries()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;

            context.SelectSection(SadConsoleEditorSection.Templates);
            context.MoveSelection(99);
            var templateView = SadConsoleEditorViewBuilder.Build(context, "templates");

            Assert.Equal(SadConsoleEditorSection.Templates, context.Section);
            Assert.Contains("[Templates]", templateView.SectionLine);
            Assert.Contains("Template browser", templateView.DetailHeader);
            Assert.Contains(templateView.DetailRows, row => row.Contains("Rock (rock)", StringComparison.Ordinal));

            context.SelectSection(SadConsoleEditorSection.ActionPlans);
            context.MoveSelection(99);
            var planView = SadConsoleEditorViewBuilder.Build(context, "plans");

            Assert.Contains("Action plan browser", planView.DetailHeader);
            Assert.Contains(planView.DetailRows, row => row.Contains("moveEast", StringComparison.Ordinal) && row.Contains("Move", StringComparison.Ordinal));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TemplatesSectionShowsSelectedAuthoredTemplatePanelWithReadOnlyTemplateFacts()
    {
        var yaml = EditorFixtureYaml()
            .Replace("templateId: rock", "templateId: missingRock", StringComparison.Ordinal)
            .Replace("templateId: box\n              coord:\n                x: 2", "templateId: box\n              coord:\n                x: 0", StringComparison.Ordinal)
            .Replace("defaultActionPlanId: moveEast", "defaultActionPlanId: missingPlan", StringComparison.Ordinal);
        var path = WriteTempContentFile(yaml);

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");

            var view = SadConsoleEditorViewBuilder.Build(context, "templates");

            Assert.Contains("Template browser", view.DetailHeader);
            Assert.Contains(view.DetailRows, row => row.Contains("Authored entity template panel (presentation editable): # Editor Room (editorRoom)", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("glyph '#'", StringComparison.Ordinal) && row.Contains("color Gray", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("Template metadata: inventory 3x2", StringComparison.Ordinal) && row.Contains("bulk 100", StringComparison.Ordinal) && row.Contains("aperture 100", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("Default action plan id: missingPlan", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("Assigned action plan summary (read-only): missing plan 'missingPlan'", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("Action-state defaults: facing South", StringComparison.Ordinal) && row.Contains("target entity id roomBox", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("Authored starting inventory/carried layout", StringComparison.Ordinal) && row.Contains("missingRock", StringComparison.Ordinal) && row.Contains("Box", StringComparison.Ordinal) && row.Contains("at (0,0)", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("Targeting rules", StringComparison.Ordinal) && row.Contains("slot 1", StringComparison.Ordinal) && row.Contains("Primary target", StringComparison.Ordinal) && row.Contains("Rock (rock)", StringComparison.Ordinal) && row.Contains("range:4", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("Template diagnostics:", StringComparison.Ordinal) && row.Contains("missingPlan", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("Carried diagnostics:", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("Template list (authored", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.StartsWith(">", StringComparison.Ordinal) && row.Contains("Editor Room (editorRoom)", StringComparison.Ordinal));
            Assert.DoesNotContain(view.DetailRows, row => row.StartsWith("Location:", StringComparison.Ordinal));
            Assert.DoesNotContain(view.DetailRows, row => row.Contains("Local activity", StringComparison.Ordinal));
            Assert.DoesNotContain(view.DetailRows, row => row.Contains("Global action log", StringComparison.Ordinal));
            Assert.DoesNotContain(view.DetailRows, row => row.Contains("Player runtime entity", StringComparison.Ordinal));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TemplateEditorUsesSemanticFocusTargetsAndSelectActivatesFocusedField()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");

            Assert.Equal(SadConsoleEditorTemplateFocus.TemplateSelector, context.TemplateFocus);
            var moveName = context.MoveTemplateFocus(1, 0);
            var view = SadConsoleEditorViewBuilder.Build(context, moveName.Message);

            Assert.True(moveName.Succeeded, moveName.Message);
            Assert.Equal(SadConsoleEditorTemplateFocus.Name, context.TemplateFocus);
            Assert.Contains("semantic focus", view.PromptHint, StringComparison.Ordinal);
            Assert.Contains(view.DetailRows, row => row.Contains(">[Name Editor Room]<", StringComparison.Ordinal));

            var activateName = context.ActivateTemplateFocus();

            Assert.True(activateName.Succeeded, activateName.Message);
            Assert.True(context.IsEditingTemplatePresentation);
            Assert.Equal(SadConsoleEditorTemplateEditMode.Name, context.TemplateEditMode);
            context.CancelEdit();

            context.MoveTemplateFocus(0, 1);
            var metadataResult = context.ActivateTemplateFocus();

            Assert.Equal(SadConsoleEditorTemplateFocus.InventoryWidth, context.TemplateFocus);
            Assert.True(metadataResult.Succeeded, metadataResult.Message);
            Assert.Equal(SadConsoleEditorTemplateEditMode.InventoryWidth, context.TemplateEditMode);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TemplateMetadataFocusedFieldEditsThroughEditorServiceAndStalesPreview()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Preview);
            context.RefreshSelectedScenarioPreview();
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");
            context.MoveTemplateFocus(1, 0); // name
            context.MoveTemplateFocus(0, 1); // inventory width

            var begin = context.ActivateTemplateFocus();
            context.BackspaceEditText();
            context.TypeEditText("4");
            var confirm = context.ConfirmEdit();

            var selected = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex];
            Assert.True(begin.Succeeded, begin.Message);
            Assert.True(confirm.Succeeded, confirm.Message);
            Assert.Equal(4, selected.InventoryWidth);
            Assert.Equal(2, selected.InventoryHeight);
            Assert.True(context.Snapshot().IsDirty);
            Assert.Null(context.CachedPreview);
            Assert.Contains("inventory width changed", context.PreviewInvalidationReason, StringComparison.Ordinal);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TemplateInitialFacingPickerSetsAndClearsFacingThroughEditorService()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "rock");
            context.MoveTemplateFocus(1, 0); // name
            context.MoveTemplateFocus(0, 1); // width
            context.MoveTemplateFocus(0, 1); // facing

            var begin = context.ActivateTemplateFocus();
            context.MoveSelection(1); // North
            var set = context.ConfirmTemplateInitialFacingPicker();

            Assert.True(begin.Succeeded, begin.Message);
            Assert.True(set.Succeeded, set.Message);
            Assert.Equal(Direction.North, context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].ActionStateDefaults.Facing);

            begin = context.ActivateTemplateFocus();
            context.MoveSelection(-99); // none
            var clear = context.ConfirmTemplateInitialFacingPicker();

            Assert.True(begin.Succeeded, begin.Message);
            Assert.True(clear.Succeeded, clear.Message);
            Assert.Null(context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].ActionStateDefaults.Facing);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TemplateLifecycleCreateDuplicateAndDeleteUseEditorServices()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            var initialCount = context.Snapshot().EntityTemplates.Count;

            var beginCreate = context.BeginTemplateCreate();
            context.TypeEditText("Temporary Actor");
            var create = context.ConfirmEdit();

            Assert.True(beginCreate.Succeeded, beginCreate.Message);
            Assert.True(create.Succeeded, create.Message);
            Assert.Equal(initialCount + 1, context.Snapshot().EntityTemplates.Count);
            Assert.Equal("Temporary Actor", context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].Name);
            Assert.True(context.Snapshot().IsDirty);

            var createdId = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].TemplateId;
            var beginDuplicate = context.BeginTemplateDuplicate();
            while (context.TemplateEditBuffer.Length > 0)
            {
                context.BackspaceEditText();
            }

            context.TypeEditText("Temporary Actor Copy");
            var duplicate = context.ConfirmEdit();

            Assert.True(beginDuplicate.Succeeded, beginDuplicate.Message);
            Assert.True(duplicate.Succeeded, duplicate.Message);
            Assert.Equal(initialCount + 2, context.Snapshot().EntityTemplates.Count);
            Assert.Equal("Temporary Actor Copy", context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].Name);

            var duplicatedId = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].TemplateId;
            Assert.NotEqual(createdId, duplicatedId);
            var beginDelete = context.BeginTemplateDeleteConfirmation();
            var delete = context.ConfirmEdit();

            Assert.True(beginDelete.Succeeded, beginDelete.Message);
            Assert.True(delete.Succeeded, delete.Message);
            Assert.Equal(initialCount + 1, context.Snapshot().EntityTemplates.Count);
            Assert.DoesNotContain(context.Snapshot().EntityTemplates, template => template.TemplateId == duplicatedId);
            Assert.Contains("template was deleted", context.PreviewInvalidationReason, StringComparison.Ordinal);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TemplateDeleteConfirmationCanBeCancelledWithoutMutation()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");
            var beforeCount = context.Snapshot().EntityTemplates.Count;

            var beginDelete = context.BeginTemplateDeleteConfirmation();
            var cancel = context.CancelEdit();

            Assert.True(beginDelete.Succeeded, beginDelete.Message);
            Assert.True(cancel.Succeeded, cancel.Message);
            Assert.Equal(beforeCount, context.Snapshot().EntityTemplates.Count);
            Assert.Equal("editorRoom", context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].TemplateId);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void DiagnosticsSectionLabelsAuthoredObjectWhenAvailable()
    {
        var path = WriteTempContentFile(EditorFixtureYaml().Replace("defaultActionPlanId: moveEast", "defaultActionPlanId: missingPlan", StringComparison.Ordinal));

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Diagnostics);

            var view = SadConsoleEditorViewBuilder.Build(context, "diagnostics");

            Assert.Contains("Validation diagnostics", view.DetailHeader);
            Assert.Contains(view.DetailRows, row => row.Contains("template:editorPlayer", StringComparison.Ordinal) && row.Contains("missingPlan", StringComparison.Ordinal));
            Assert.Contains(view.DiagnosticRows, row => row.Contains("template:editorPlayer", StringComparison.Ordinal));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void YamlAndDiffSectionSupportsExplicitToggleAndScrollWithoutRefreshingSnapshot()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            var initialSnapshot = context.Snapshot();

            context.SelectSection(SadConsoleEditorSection.YamlAndDiff);
            context.MoveSelection(3);
            var yamlView = SadConsoleEditorViewBuilder.Build(context, "yaml");

            Assert.Contains("YAML preview", yamlView.DetailHeader);
            Assert.True(context.YamlScrollOffset >= 3);
            Assert.Contains(yamlView.DetailRows, row => row.Contains("inventoryWidth", StringComparison.Ordinal) || row.Contains("carriedEntities:", StringComparison.Ordinal));

            context.ToggleTextSurface();
            var diffView = SadConsoleEditorViewBuilder.Build(context, "diff");

            Assert.Contains("Diff surface", diffView.DetailHeader);
            Assert.Contains(diffView.DetailRows, row => row.Contains("No diff lines", StringComparison.Ordinal) || row.StartsWith("   1:", StringComparison.Ordinal));
            Assert.Same(initialSnapshot, context.Snapshot());
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void PreviewSelectedScenarioReturnsDerivedRuntimeSessionWithoutDirtyingContent()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;

            var preview = context.PreviewSelectedScenario();

            Assert.NotNull(preview);
            Assert.True(preview!.IsDerivedRuntimeState);
            Assert.Equal("editor-smoke", preview.ScenarioId);
            Assert.Equal("editor-smoke", preview.Session.ScenarioId);
            Assert.False(context.Snapshot().IsDirty);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void PreviewSectionDoesNotMaterializeDuringViewBuildAndLabelsManualRefresh()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Preview);

            var view = SadConsoleEditorViewBuilder.Build(context, "preview");

            Assert.Null(context.CachedPreview);
            Assert.Contains("[Preview]", view.SectionLine);
            Assert.Contains("manual turn-0 derived runtime", view.DetailHeader);
            Assert.Contains(view.DetailRows, row => row.Contains("not materialized", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("No auto-refresh", StringComparison.Ordinal));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void ExplicitPreviewRefreshCachesDerivedRuntimeFactsAndRefreshesOnDemand()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Preview);

            var first = context.RefreshSelectedScenarioPreview();
            var firstView = SadConsoleEditorViewBuilder.Build(context, "preview");
            var second = context.RefreshSelectedScenarioPreview();

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.NotSame(first, second);
            Assert.True(context.CachedPreview!.IsDerivedRuntimeState);
            Assert.Contains(firstView.DetailRows, row => row.Contains("Derived runtime state: yes - not authored source", StringComparison.Ordinal));
            Assert.Contains(firstView.DetailRows, row => row.Contains("Player runtime entity", StringComparison.Ordinal));
            Assert.Contains(firstView.DetailRows, row => row.Contains("Initial grid summary", StringComparison.Ordinal));
            Assert.False(context.Snapshot().IsDirty);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void PreviewSectionShowsRuntimeEntityLocationTreeWithIndentedNames()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Preview);
            context.RefreshSelectedScenarioPreview();

            var view = SadConsoleEditorViewBuilder.Build(context, "preview");
            var treeHeaderIndex = view.DetailRows.ToList().FindIndex(row => row.Contains("Entity location tree", StringComparison.Ordinal));

            Assert.True(treeHeaderIndex >= 0, "Preview should label the tree as derived runtime containment.");
            Assert.Contains("J jumps selected row to source template", view.DetailRows[treeHeaderIndex]);
            Assert.Equal("> - Editor Room", view.DetailRows[treeHeaderIndex + 1]);
            Assert.Contains("  - Rock", view.DetailRows);
            Assert.Contains("  - Box", view.DetailRows);
            Assert.Contains("    - Pebble", view.DetailRows);
            Assert.Contains("  - Editor Player", view.DetailRows);
            Assert.DoesNotContain(view.DetailRows.Skip(treeHeaderIndex + 1).Take(5), row => row.Contains("roomRock", StringComparison.Ordinal));
            Assert.DoesNotContain(view.DetailRows.Skip(treeHeaderIndex + 1).Take(5), row => row.Contains("editorPlayer", StringComparison.Ordinal));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void PreviewTreeSelectionMovesAndClampsWhenPreviewIsMaterialized()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Preview);
            context.RefreshSelectedScenarioPreview();

            context.MoveSelection(2);
            Assert.Equal(2, context.SelectedPreviewEntityIndex);
            var movedView = SadConsoleEditorViewBuilder.Build(context, "preview");
            context.MoveSelection(99);
            Assert.Equal(4, context.SelectedPreviewEntityIndex);
            var clampedView = SadConsoleEditorViewBuilder.Build(context, "preview");
            context.MoveSelection(-99);

            Assert.Contains(">   - Box", movedView.DetailRows);
            Assert.Contains(">   - Editor Player", clampedView.DetailRows);
            Assert.Equal(0, context.SelectedPreviewEntityIndex);
            Assert.Equal("editor-smoke", context.SelectedScenario()!.ScenarioId);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void PreviewSourceJumpSelectsMatchingAuthoredTemplate()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Preview);
            context.RefreshSelectedScenarioPreview();
            context.MoveSelection(1);

            var result = context.JumpSelectedPreviewEntityToSourceTemplate();
            var view = SadConsoleEditorViewBuilder.Build(context, result.Message);

            Assert.True(result.Succeeded, result.Message);
            Assert.Equal(SadConsoleEditorSection.Templates, context.Section);
            Assert.Contains("Jumped to source template rock", result.Message);
            Assert.Contains(view.DetailRows, row => row.StartsWith(">", StringComparison.Ordinal) && row.Contains("Rock (rock)", StringComparison.Ordinal));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void PreviewSourceJumpWithoutKnownRuntimeSourceShowsHonestMessage()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Preview);

            var result = context.JumpSelectedPreviewEntityToSourceTemplate();

            Assert.False(result.Succeeded);
            Assert.Equal("Source unknown for selected runtime entity", result.Message);
            Assert.Equal(SadConsoleEditorSection.Preview, context.Section);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SelectedScenarioChangeClearsPreviewUntilExplicitRefresh()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Preview);
            context.RefreshSelectedScenarioPreview();

            context.SelectSection(SadConsoleEditorSection.Scenarios);
            context.MoveSelection(1);
            context.SelectSection(SadConsoleEditorSection.Preview);
            var staleClearedView = SadConsoleEditorViewBuilder.Build(context, "preview");

            Assert.Equal("second-smoke", context.SelectedScenario()!.ScenarioId);
            Assert.Null(context.CachedPreview);
            Assert.Contains(staleClearedView.DetailRows, row => row.Contains("second-smoke", StringComparison.Ordinal));
            Assert.Contains(staleClearedView.DetailRows, row => row.Contains("not materialized", StringComparison.Ordinal));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void PreviewRowsSurfaceMaterializationDiagnosticsAndCapabilityGaps()
    {
        var path = WriteTempContentFile(EditorFixtureYaml().Replace("defaultActionPlanId: moveEast", "defaultActionPlanId: missingPlan", StringComparison.Ordinal));

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Preview);
            context.RefreshSelectedScenarioPreview();

            var view = SadConsoleEditorViewBuilder.Build(context, "preview");

            Assert.Contains(view.DetailRows, row => row.Contains("Validation/materialization:", StringComparison.Ordinal) && row.Contains("missingPlan", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("Runtime failures:", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("Capability gaps:", StringComparison.Ordinal));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SnapshotIsCachedAcrossRepeatedEditorViewBuilds()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            var initialSnapshot = context.Snapshot();

            _ = SadConsoleEditorViewBuilder.Build(context, "first draw");
            _ = SadConsoleEditorViewBuilder.Build(context, "second draw");
            context.MoveSelection(1);
            _ = context.SelectedScenario();

            Assert.Same(initialSnapshot, context.Snapshot());
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void RefreshSnapshotRevalidatesCachedSnapshotPreservesSectionAndSelectionAndClearsPreview()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "second-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            context.MoveSelection(2);
            var selectedTemplate = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].TemplateId;
            var initialSnapshot = context.Snapshot();
            context.RefreshSelectedScenarioPreview();

            var result = context.RefreshSnapshot();
            var view = SadConsoleEditorViewBuilder.Build(context, result.Message);

            Assert.NotSame(initialSnapshot, context.Snapshot());
            Assert.Equal(SadConsoleEditorSection.Templates, context.Section);
            Assert.Equal("second-smoke", context.SelectedScenario()!.ScenarioId);
            Assert.Equal(selectedTemplate, context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].TemplateId);
            Assert.Null(context.CachedPreview);
            Assert.True(result.PreviewWasCleared);
            Assert.Contains("Refreshed/revalidated cached authored snapshot", result.Message);
            Assert.Contains("Preview is stale until P rematerializes", result.Message);
            Assert.Contains("action-plan step edits enabled", view.DirtyLine);
            Assert.Contains("S saves", view.PromptHint);
            Assert.Contains("J jumps Preview row to source template", view.PromptHint);
            Assert.Contains("R refreshes/revalidates cached snapshot", view.PromptHint);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void RefreshSnapshotClampsSelectionWhenSelectedAuthoredObjectsDisappear()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            var staleSnapshot = context.Snapshot() with
            {
                Scenarios = context.Snapshot().Scenarios.Concat([
                    new FrontendEditorScenarioSummary("deleted-smoke", "Deleted Smoke", "editorRoom", "editorPlayer", "deletedPlayer", new(0, 0))
                ]).ToList(),
                EntityTemplates = context.Snapshot().EntityTemplates.Concat([
                    new FrontendEditorEntityTemplateSummary(
                        "deletedTemplate",
                        "Deleted Template",
                        '?',
                        PresentationColor.Gray,
                        0,
                        0,
                        0,
                        0,
                        null,
                        new FrontendEditorActionStateDefaultsSummary(null, null),
                        [],
                        [],
                        [])
                ]).ToList(),
                ActionPlans = context.Snapshot().ActionPlans.Concat([
                    new FrontendEditorActionPlanSummary("deletedPlan", "test", [], [])
                ]).ToList()
            };
            ReplaceCachedSnapshot(context, staleSnapshot);
            context.SelectSection(SadConsoleEditorSection.ActionPlans);
            context.TrySelectScenario("deleted-smoke");
            context.SelectSection(SadConsoleEditorSection.Templates);
            context.MoveSelection(99);
            context.SelectSection(SadConsoleEditorSection.ActionPlans);
            context.MoveSelection(99);

            var result = context.RefreshSnapshot();

            Assert.Equal(SadConsoleEditorSection.ActionPlans, context.Section);
            Assert.Equal("second-smoke", context.SelectedScenario()!.ScenarioId);
            Assert.Equal(context.Snapshot().EntityTemplates.Count - 1, context.SelectedTemplateIndex);
            Assert.Equal(context.Snapshot().ActionPlans.Count - 1, context.SelectedActionPlanIndex);
            Assert.False(result.ScenarioSelectionPreserved);
            Assert.False(result.TemplateSelectionPreserved);
            Assert.False(result.ActionPlanSelectionPreserved);
            Assert.Contains("selected scenario 'deleted-smoke' no longer exists; selection clamped", result.Message);
            Assert.Contains("selected template 'deletedTemplate' no longer exists; selection clamped", result.Message);
            Assert.Contains("selected action plan 'deletedPlan' no longer exists; selection clamped", result.Message);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void RefreshedPreviewSurfaceLabelsStalePreviewUntilExplicitRematerialization()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Preview);
            context.RefreshSelectedScenarioPreview();

            context.RefreshSnapshot();
            var staleView = SadConsoleEditorViewBuilder.Build(context, "after refresh");

            Assert.Null(context.CachedPreview);
            Assert.Contains(staleView.DetailRows, row => row.Contains("not materialized/stale", StringComparison.Ordinal));
            Assert.Contains(staleView.DetailRows, row => row.Contains("authored snapshot was refreshed", StringComparison.Ordinal));
            var preview = context.RefreshSelectedScenarioPreview();

            Assert.NotNull(preview);
            Assert.Equal("editor-smoke", context.CachedPreview!.ScenarioId);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SimulationMaterializationDoesNotMarkStaleEditorPreviewCurrent()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Preview);
            context.RefreshSelectedScenarioPreview();
            context.RefreshSnapshot();

            var simulationSession = context.MaterializeSelectedScenarioForSimulation();

            Assert.NotNull(simulationSession);
            Assert.Equal("editor-smoke", simulationSession!.ScenarioId);
            Assert.Null(context.CachedPreview);
            var view = SadConsoleEditorViewBuilder.Build(context, "after simulation launch materialization");
            Assert.Contains(view.DetailRows, row => row.Contains("not materialized/stale", StringComparison.Ordinal));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TemplateNameEditUpdatesSelectedTemplateNameDirtyStateAndStatus()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            var original = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex];

            var begin = context.BeginTemplateNameEdit();
            context.TypeEditText(" Updated");
            var confirm = context.ConfirmEdit();

            var selected = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex];
            Assert.True(begin.Succeeded, begin.Message);
            Assert.True(confirm.Succeeded, confirm.Message);
            Assert.Equal($"{original.Name} Updated", selected.Name);
            Assert.True(context.Snapshot().IsDirty);
            Assert.Contains($"Updated template {original.TemplateId}", confirm.Message);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TemplateGlyphEditWithMultiCharacterInputStoresFirstSymbol()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);

            context.BeginTemplateGlyphEdit();
            context.TypeEditText("xy");
            var result = context.ConfirmEdit();

            Assert.True(result.Succeeded, result.Message);
            Assert.Equal('x', context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].Glyph);
            Assert.True(context.Snapshot().IsDirty);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TemplateColorCycleUpdatesSelectedTemplateColor()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            var originalColor = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].Color;
            var colors = Enum.GetValues<PresentationColor>();
            var expectedColor = colors[(Array.IndexOf(colors, originalColor) + 1 + colors.Length) % colors.Length];

            var result = context.CycleSelectedTemplateColor();

            Assert.True(result.Succeeded, result.Message);
            Assert.Equal(expectedColor, context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].Color);
            Assert.True(context.Snapshot().IsDirty);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void DefaultActionPlanPickerOpensWithNoneAndExistingPlans()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");

            var result = context.BeginTemplateDefaultActionPlanPicker();
            var options = context.TemplateDefaultActionPlanPickerOptions();
            var view = SadConsoleEditorViewBuilder.Build(context, result.Message);

            Assert.True(result.Succeeded, result.Message);
            Assert.True(context.IsPickingTemplateDefaultActionPlan);
            Assert.Equal("none", options[0].Label);
            Assert.Null(options[0].ActionPlanId);
            Assert.Contains(options, option => option.ActionPlanId == "moveEast");
            Assert.Contains(options, option => option.ActionPlanId == "waitPlan");
            Assert.Contains(view.DetailRows, row => row.Contains("Default action plan picker active", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("Default action plan picker options", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("Assigned action plan summary (read-only): moveEast", StringComparison.Ordinal) && row.Contains("steps:Move", StringComparison.Ordinal));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void DefaultActionPlanPickerAppliesPlanUpdatesDirtyStatusAndStalesPreview()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Preview);
            context.RefreshSelectedScenarioPreview();
            Assert.NotNull(context.CachedPreview);
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");

            context.BeginTemplateDefaultActionPlanPicker();
            MovePickerTo(context, "waitPlan");
            var result = context.ConfirmTemplateDefaultActionPlanPicker();

            var selected = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex];
            Assert.True(result.Succeeded, result.Message);
            Assert.Equal("waitPlan", selected.DefaultActionPlanId);
            Assert.True(context.Snapshot().IsDirty);
            Assert.Contains("Assigned default action plan waitPlan", result.Message);
            Assert.False(context.IsPickingTemplateDefaultActionPlan);
            Assert.Null(context.CachedPreview);
            Assert.Contains("default action plan changed", context.PreviewInvalidationReason);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void DefaultActionPlanPickerSelectingNoneClearsDefaultPlan()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");

            context.BeginTemplateDefaultActionPlanPicker();
            MovePickerTo(context, null);
            var result = context.ConfirmTemplateDefaultActionPlanPicker();

            Assert.True(result.Succeeded, result.Message);
            Assert.Null(context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].DefaultActionPlanId);
            Assert.True(context.Snapshot().IsDirty);
            Assert.Contains("Cleared default action plan", result.Message);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void DefaultActionPlanPickerEscCancelDoesNotMutate()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");
            var original = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].DefaultActionPlanId;

            context.BeginTemplateDefaultActionPlanPicker();
            MovePickerTo(context, "waitPlan");
            var result = context.CancelEdit();

            Assert.True(result.Succeeded, result.Message);
            Assert.False(context.IsPickingTemplateDefaultActionPlan);
            Assert.Equal(original, context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].DefaultActionPlanId);
            Assert.False(context.Snapshot().IsDirty);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TargetingRuleEditorOpensAndShowsSlotsOneThroughFour()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");

            var result = context.BeginTemplateTargetingRuleEditor();
            var view = SadConsoleEditorViewBuilder.Build(context, result.Message);

            Assert.True(result.Succeeded, result.Message);
            Assert.True(context.IsEditingTemplateTargetingRule);
            Assert.NotNull(context.TargetingRuleEdit);
            Assert.Equal(1, context.TargetingRuleEdit!.Slot);
            Assert.Contains(view.DetailRows, row => row.Contains("Targeting rule editor active", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("slot 1", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("where Current place", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("slot 2: <empty>", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("slot 3: <empty>", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("slot 4: <empty>", StringComparison.Ordinal));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TargetingRuleLabelEditAppliesValidLabelToPendingState()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");
            context.BeginTemplateTargetingRuleEditor();
            context.MoveSelection(1);

            context.BeginTargetingRuleLabelEdit();
            context.TypeTargetingRuleLabelText("focus1");
            var result = context.ConfirmTargetingRuleLabelEdit();

            Assert.True(result.Succeeded, result.Message);
            Assert.False(context.IsEditingTargetingRuleLabel);
            Assert.Equal("focus1", context.TargetingRuleEdit!.Label);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TargetingRuleTargetSelectionCanSelectSelfCurrentTemplate()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");

            context.BeginTemplateTargetingRuleEditor();
            context.MoveSelection(1);

            Assert.Equal("editorRoom", context.TargetingRuleEdit!.TargetTemplateId);
            Assert.Contains(context.TargetTemplatePickerOptions(), option => option.TemplateId == "editorRoom");
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TargetingRuleRangeClampsBetweenZeroAndTen()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");
            context.BeginTemplateTargetingRuleEditor();
            context.MoveSelection(1);

            context.AdjustTargetingRuleRange(-99);
            Assert.Equal(0, context.TargetingRuleEdit!.Range);

            context.AdjustTargetingRuleRange(99);
            Assert.Equal(10, context.TargetingRuleEdit!.Range);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TargetingRuleSemanticFieldFocusCyclesTargetAndAppliesImmediately()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");
            context.BeginTemplateTargetingRuleEditor(SadConsoleEditorTemplateFocus.TargetingTarget);
            context.MoveSelection(1);
            SetPendingTargetingLabel(context, "focus2");
            context.MoveTargetingRuleField(1);

            Assert.Equal(SadConsoleEditorTargetingRuleField.Target, context.TargetingRuleEdit!.ActiveField);
            Assert.Equal("editorRoom", context.TargetingRuleEdit.TargetTemplateId);

            var result = context.ActivateTargetingRuleField();

            var rule = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].TargetingRules.Single(rule => rule.Slot == 2);
            Assert.True(result.Succeeded, result.Message);
            Assert.NotEqual("editorRoom", rule.TargetTemplateId);
            Assert.Equal(rule.TargetTemplateId, context.TargetingRuleEdit!.TargetTemplateId);
            Assert.True(context.Snapshot().IsDirty);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TargetingRuleSemanticFieldFocusIncrementsRangeAndAppliesImmediately()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");
            context.BeginTemplateTargetingRuleEditor(SadConsoleEditorTemplateFocus.TargetingRange);
            context.MoveSelection(1);
            SetPendingTargetingLabel(context, "focus2");
            context.MoveTargetingRuleField(2);

            Assert.Equal(SadConsoleEditorTargetingRuleField.Range, context.TargetingRuleEdit!.ActiveField);
            var result = context.ActivateTargetingRuleField();

            var rule = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].TargetingRules.Single(rule => rule.Slot == 2);
            Assert.True(result.Succeeded, result.Message);
            Assert.Equal(1, rule.Range);
            Assert.Equal(1, context.TargetingRuleEdit!.Range);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TargetingRuleSemanticFieldFocusCyclesLocalityAndAppliesImmediately()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");
            context.BeginTemplateTargetingRuleEditor();
            context.MoveSelection(1);
            SetPendingTargetingLabel(context, "focus2");
            context.MoveTargetingRuleField(3);

            Assert.Equal(SadConsoleEditorTargetingRuleField.Locality, context.TargetingRuleEdit!.ActiveField);
            var result = context.ActivateTargetingRuleField();

            var rule = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].TargetingRules.Single(rule => rule.Slot == 2);
            Assert.True(result.Succeeded, result.Message);
            Assert.Equal([TargetingLocalityOrigin.OwnInventory], rule.LocalityOrigins);
            Assert.Equal([TargetingLocalityOrigin.OwnInventory], context.TargetingRuleEdit!.LocalityOrigins);
            Assert.Contains("targeting profile rule", result.Message);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TargetingRuleEditorShowsAndCyclesTemplateDefaultLocality()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");
            context.BeginTemplateTargetingRuleEditor();
            var initialView = SadConsoleEditorViewBuilder.Build(context, "targeting");

            var result = context.CycleTemplateTargetingDefaultLocality();
            var template = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex];
            var view = SadConsoleEditorViewBuilder.Build(context, result.Message);

            Assert.Contains(initialView.DetailRows, row => row.Contains("Template targeting default where Current place", StringComparison.Ordinal));
            Assert.True(result.Succeeded, result.Message);
            Assert.Equal([TargetingLocalityOrigin.OwnInventory], template.TargetingProfile!.DefaultLocalityOrigins);
            Assert.Contains(view.DetailRows, row => row.Contains("Template targeting default where Own inventory", StringComparison.Ordinal));
            Assert.Contains("default locality", context.PreviewInvalidationReason, StringComparison.Ordinal);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void ApplyingValidTargetingRuleUpdatesSnapshotDirtyStatusAndStalesPreview()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Preview);
            context.RefreshSelectedScenarioPreview();
            Assert.NotNull(context.CachedPreview);
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");
            context.BeginTemplateTargetingRuleEditor();
            context.MoveSelection(1);
            SetPendingTargetingLabel(context, "focus1");
            context.AdjustTargetingRuleRange(3);

            var result = context.ConfirmTemplateTargetingRuleEditor();

            var selected = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex];
            var rule = selected.TargetingRules.Single(rule => rule.Slot == 2);
            Assert.True(result.Succeeded, result.Message);
            Assert.Equal("focus1", rule.Label);
            Assert.Equal("editorRoom", rule.TargetTemplateId);
            Assert.Equal(3, rule.Range);
            Assert.Equal(FrontendEditorTargetingSource.TargetingProfile, selected.TargetingSource);
            Assert.Equal([TargetingLocalityOrigin.CurrentPlace], rule.LocalityOrigins);
            Assert.Contains("targeting:", context.Snapshot().YamlPreview);
            Assert.True(context.Snapshot().IsDirty);
            Assert.Null(context.CachedPreview);
            Assert.Contains("targeting rules changed", context.PreviewInvalidationReason);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void InvalidDuplicateTargetingRuleLabelReportsServiceStatusAndDoesNotMutate()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");
            context.BeginTemplateTargetingRuleEditor();
            context.MoveSelection(1);
            SetPendingTargetingLabel(context, "focus1");
            var first = context.ConfirmTemplateTargetingRuleEditor();
            Assert.True(first.Succeeded, first.Message);

            context.MoveSelection(1);
            context.BeginTargetingRuleLabelEdit();
            context.TypeTargetingRuleLabelText("focus1");
            var duplicate = context.ConfirmTargetingRuleLabelEdit();

            var selected = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex];
            Assert.False(duplicate.Succeeded);
            Assert.Contains("Duplicate targeting rule label focus1", duplicate.Message);
            Assert.Equal(FrontendEditorTargetingSource.TargetingProfile, selected.TargetingSource);
            Assert.DoesNotContain(selected.TargetingRules, rule => rule.Slot == 3);
            Assert.Contains("targeting:", context.Snapshot().YamlPreview);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void ClearingTargetingRuleSlotRemovesRule()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");
            Assert.Contains(context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].TargetingRules, rule => rule.Slot == 1);

            context.BeginTemplateTargetingRuleEditor();
            var result = context.ClearTemplateTargetingRuleSlot();

            Assert.True(result.Succeeded, result.Message);
            Assert.DoesNotContain(context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].TargetingRules, rule => rule.Slot == 1);
            Assert.True(context.Snapshot().IsDirty);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TargetingRuleEditorEscCancelDoesNotMutatePendingEdit()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");
            var originalRules = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].TargetingRules;

            context.BeginTemplateTargetingRuleEditor();
            context.MoveSelection(1);
            context.AdjustTargetingRuleRange(5);
            var result = context.CancelEdit();

            Assert.True(result.Succeeded, result.Message);
            Assert.False(context.IsEditingTemplateTargetingRule);
            Assert.Equal(originalRules, context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].TargetingRules);
            Assert.False(context.Snapshot().IsDirty);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void InventoryBrushModeExcludesCurrentTemplateFromBrushOptions()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");

            var result = context.BeginTemplateInventoryBrush();
            var options = context.TemplateInventoryBrushOptions();
            var view = SadConsoleEditorViewBuilder.Build(context, result.Message);

            Assert.True(result.Succeeded, result.Message);
            Assert.True(context.IsTemplateInventoryBrushActive);
            Assert.DoesNotContain(options, option => option.TemplateId == "editorRoom");
            Assert.Contains(options, option => option.TemplateId == "rock");
            Assert.Contains(view.DetailRows, row => row.Contains("Inventory brush mode", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("current template excluded", StringComparison.Ordinal));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void InventoryBrushCursorClampsWithinInventoryBounds()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");
            context.BeginTemplateInventoryBrush();

            context.MoveTemplateInventoryBrushCursor(99, 99);
            Assert.Equal(new(2, 1), context.InventoryBrush!.Cursor);

            context.MoveTemplateInventoryBrushCursor(-99, -99);
            Assert.Equal(new(0, 0), context.InventoryBrush!.Cursor);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void InventoryBrushPlacementUpdatesCarriedLayoutDirtyStatusAndStalesPreview()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Preview);
            context.RefreshSelectedScenarioPreview();
            Assert.NotNull(context.CachedPreview);
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");
            context.BeginTemplateInventoryBrush();
            MoveBrushTo(context, "rock");
            context.MoveTemplateInventoryBrushCursor(1, 0);

            var result = context.PlaceTemplateInventoryBrush();

            var selected = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex];
            Assert.True(result.Succeeded, result.Message);
            Assert.True(context.IsTemplateInventoryBrushActive);
            Assert.True(context.Snapshot().IsDirty);
            Assert.Null(context.CachedPreview);
            Assert.Contains("carried inventory changed", context.PreviewInvalidationReason);
            Assert.Contains(selected.CarriedEntities, carried => carried.TemplateId == "rock" && carried.Coord == new GridCoord(1, 0));
            Assert.Contains("Placed template rock", result.Message);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void InventoryBrushOccupiedCellReportsServiceStatusAndDoesNotDuplicate()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");
            context.BeginTemplateInventoryBrush();
            MoveBrushTo(context, "rock");
            var beforeCount = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].CarriedEntities.Count;

            var result = context.PlaceTemplateInventoryBrush();

            var selected = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex];
            Assert.False(result.Succeeded);
            Assert.Contains("occupied", result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(beforeCount, selected.CarriedEntities.Count);
            Assert.Single(selected.CarriedEntities, carried => carried.Coord == new GridCoord(0, 0));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void InventoryBrushNoUsableInventoryReportsClearStatusAndDoesNotPlace()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "rock");

            var begin = context.BeginTemplateInventoryBrush();
            var place = context.PlaceTemplateInventoryBrush();
            var view = SadConsoleEditorViewBuilder.Build(context, place.Message);

            Assert.True(begin.Succeeded, begin.Message);
            Assert.False(place.Succeeded);
            Assert.Contains("no usable inventory", begin.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("no usable inventory", place.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(view.DetailRows, row => row.Contains("No usable inventory", StringComparison.Ordinal));
            Assert.Empty(context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].CarriedEntities);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void InventoryBrushEscCancelExitsModeWithoutMutating()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            SelectTemplate(context, "editorRoom");
            var before = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].CarriedEntities;

            context.BeginTemplateInventoryBrush();
            context.MoveTemplateInventoryBrushCursor(1, 0);
            var result = context.CancelEdit();

            Assert.True(result.Succeeded, result.Message);
            Assert.False(context.IsTemplateInventoryBrushActive);
            Assert.Equal(before, context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].CarriedEntities);
            Assert.False(context.Snapshot().IsDirty);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TemplateEditCancelDoesNotMutate()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            var originalName = context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].Name;

            context.BeginTemplateNameEdit();
            context.TypeEditText(" Mutated");
            var result = context.CancelEdit();

            Assert.True(result.Succeeded, result.Message);
            Assert.Equal(originalName, context.Snapshot().EntityTemplates[context.SelectedTemplateIndex].Name);
            Assert.False(context.Snapshot().IsDirty);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SaveClearsDirtyWhenFilePathExists()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            context.BeginTemplateNameEdit();
            context.TypeEditText(" Saved");
            context.ConfirmEdit();
            Assert.True(context.Snapshot().IsDirty);

            var result = context.Save();

            Assert.True(result.Succeeded, result.Message);
            Assert.False(context.Snapshot().IsDirty);
            Assert.Contains("Saved", result.Message);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void PreviewIsStaleAfterTemplatePresentationMutation()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Preview);
            context.RefreshSelectedScenarioPreview();
            Assert.NotNull(context.CachedPreview);
            context.SelectSection(SadConsoleEditorSection.Templates);

            var result = context.CycleSelectedTemplateColor();

            Assert.True(result.Succeeded, result.Message);
            Assert.Null(context.CachedPreview);
            Assert.Contains("authored template presentation changed", context.PreviewInvalidationReason);
            context.SelectSection(SadConsoleEditorSection.Preview);
            var previewView = SadConsoleEditorViewBuilder.Build(context, result.Message);
            Assert.Contains(previewView.DetailRows, row => row.Contains("not materialized/stale", StringComparison.Ordinal));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void ActionStepEditorOpensWithStableAvailableActionStepsAndCyclesSelectedKind()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.ActionPlans);

            var result = context.BeginActionPlanStepEditor();

            Assert.True(result.Succeeded, result.Message);
            Assert.True(context.IsEditingActionPlanSteps);
            Assert.Contains(context.AvailableActionStepOptions(), step => step.Kind == ActionPlanBehaviorStepKind.MoveFacing);
            Assert.DoesNotContain(context.AvailableActionStepOptions(), step => step.Kind == ActionPlanBehaviorStepKind.AcquireNearestTarget);
            var firstKind = context.AvailableActionStepOptions()[context.ActionStepEdit!.AvailableActionStepIndex].Kind;

            context.CycleActionStepEditorAvailable();

            var cycledKind = context.AvailableActionStepOptions()[context.ActionStepEdit!.AvailableActionStepIndex].Kind;
            Assert.NotEqual(firstKind, cycledKind);
            var view = SadConsoleEditorViewBuilder.Build(context, "editing");
            Assert.Contains(view.DetailRows, row => row.Contains("Selected engine-defined action step", StringComparison.Ordinal));
            Assert.Contains(view.DetailRows, row => row.Contains("Replace possible", StringComparison.Ordinal));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void ActionStepEditorReplaceUpdatesSelectedPlanStepAndStalesPreview()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.PreviewSelectedScenario();
            context.SelectSection(SadConsoleEditorSection.ActionPlans);
            SelectActionPlan(context, "moveEast");
            context.BeginActionPlanStepEditor();
            MoveActionStepOptionTo(context, ActionPlanBehaviorStepKind.Backstep);

            var result = context.ReplaceSelectedActionPlanStep();

            Assert.True(result.Succeeded, result.Message);
            Assert.True(context.Snapshot().IsDirty);
            var plan = context.Snapshot().ActionPlans.Single(plan => plan.ActionPlanId == "moveEast");
            Assert.Equal([ActionPlanBehaviorStepKind.Backstep], plan.ActionSteps.Select(step => step.Kind).ToArray());
            Assert.Equal(["Backstep"], plan.ActionStepNames);
            Assert.Null(context.CachedPreview);
            Assert.Contains("authored action plan steps changed", context.PreviewInvalidationReason);
            Assert.Equal("moveEast", context.Snapshot().ActionPlans[context.SelectedActionPlanIndex].ActionPlanId);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void ActionStepEditorInsertAtBeginningMiddleAndEndUpdatesOrder()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.ActionPlans);
            SelectActionPlan(context, "moveEast");
            context.BeginActionPlanStepEditor();

            MoveActionStepOptionTo(context, ActionPlanBehaviorStepKind.DropFacing);
            var beginning = context.InsertSelectedActionPlanStep();
            Assert.True(beginning.Succeeded, beginning.Message);

            context.MoveSelection(1);
            MoveActionStepOptionTo(context, ActionPlanBehaviorStepKind.Backstep);
            var middle = context.InsertSelectedActionPlanStep();
            Assert.True(middle.Succeeded, middle.Message);

            context.MoveSelection(99);
            MoveActionStepOptionTo(context, ActionPlanBehaviorStepKind.PickupTarget);
            var end = context.InsertSelectedActionPlanStep();
            Assert.True(end.Succeeded, end.Message);

            var plan = context.Snapshot().ActionPlans.Single(plan => plan.ActionPlanId == "moveEast");
            Assert.Equal(
                [ActionPlanBehaviorStepKind.DropFacing, ActionPlanBehaviorStepKind.Backstep, ActionPlanBehaviorStepKind.MoveFacing, ActionPlanBehaviorStepKind.PickupTarget],
                plan.ActionSteps.Select(step => step.Kind).ToArray());
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void ActionStepEditorEscCancelDoesNotMutate()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.ActionPlans);
            SelectActionPlan(context, "moveEast");
            var original = context.Snapshot().ActionPlans.Single(plan => plan.ActionPlanId == "moveEast").ActionSteps.Select(step => step.Kind).ToArray();

            context.BeginActionPlanStepEditor();
            MoveActionStepOptionTo(context, ActionPlanBehaviorStepKind.Backstep);
            context.MoveSelection(1);
            var result = context.CancelEdit();

            Assert.True(result.Succeeded, result.Message);
            Assert.False(context.IsEditingActionPlanSteps);
            Assert.Equal(original, context.Snapshot().ActionPlans.Single(plan => plan.ActionPlanId == "moveEast").ActionSteps.Select(step => step.Kind).ToArray());
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void ActionStepEditorDisablesInvalidReplaceOnEmptyPlanWhileInsertWorks()
    {
        var yaml = EditorFixtureYaml();
        var actionPlansIndex = yaml.IndexOf("actionPlans:", StringComparison.Ordinal);
        Assert.True(actionPlansIndex >= 0, "Editor fixture must define actionPlans.");
        var insertIndex = yaml.IndexOf('\n', actionPlansIndex) + 1;
        var path = WriteTempContentFile(yaml.Insert(insertIndex,
            "  emptyPlan:\n    id: emptyPlan\n    behavior:\n      steps: []\n"));

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.ActionPlans);
            SelectActionPlan(context, "emptyPlan");
            context.BeginActionPlanStepEditor();
            MoveActionStepOptionTo(context, ActionPlanBehaviorStepKind.Backstep);
            var view = SadConsoleEditorViewBuilder.Build(context, "editing");

            var replace = context.ReplaceSelectedActionPlanStep();
            var insert = context.InsertSelectedActionPlanStep();

            Assert.Contains(view.DetailRows, row => row.Contains("Replace possible: no", StringComparison.Ordinal));
            Assert.False(replace.Succeeded);
            Assert.Contains("Cannot replace", replace.Message);
            Assert.True(insert.Succeeded, insert.Message);
            var plan = context.Snapshot().ActionPlans.Single(plan => plan.ActionPlanId == "emptyPlan");
            Assert.Equal([ActionPlanBehaviorStepKind.Backstep], plan.ActionSteps.Select(step => step.Kind).ToArray());
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void CommandMenuOpensClosesAndFooterDescribesDirectionalControls()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;

            var open = context.OpenCommandMenu();
            var openView = SadConsoleEditorViewBuilder.Build(context, open.Message);
            Assert.True(context.IsCommandMenuOpen);
            var cancel = context.CancelCommandMenu();

            Assert.True(open.Succeeded, open.Message);
            Assert.False(context.IsCommandMenuOpen);
            var closedView = SadConsoleEditorViewBuilder.Build(context, cancel.Message);
            Assert.Contains("Enter opens command menu", closedView.PromptHint);
            Assert.Contains("M launches selected scenario", closedView.PromptHint);
            Assert.Contains("Up/Down command", openView.PromptHint);
            Assert.Contains("Enter/Select activates", openView.PromptHint);
            Assert.Contains(openView.DetailRows, row => row.Contains("Command menu", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("cancelled", cancel.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void CommandMenuEntriesVaryByEditorSection()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;

            context.SelectSection(SadConsoleEditorSection.Templates);
            var templateCommands = context.CommandMenuEntries();
            context.SelectSection(SadConsoleEditorSection.ActionPlans);
            var actionPlanCommands = context.CommandMenuEntries();
            context.SelectSection(SadConsoleEditorSection.Preview);
            var previewCommands = context.CommandMenuEntries();

            Assert.Contains(templateCommands, entry => entry.CommandId == SadConsoleEditorCommandId.EditTemplateName);
            Assert.Contains(templateCommands, entry => entry.CommandId == SadConsoleEditorCommandId.InventoryBrushMode);
            Assert.DoesNotContain(templateCommands, entry => entry.CommandId == SadConsoleEditorCommandId.OpenActionStepEditor);
            Assert.Contains(actionPlanCommands, entry => entry.CommandId == SadConsoleEditorCommandId.OpenActionStepEditor);
            Assert.DoesNotContain(actionPlanCommands, entry => entry.CommandId == SadConsoleEditorCommandId.EditTemplateName);
            Assert.Contains(previewCommands, entry => entry.CommandId == SadConsoleEditorCommandId.RematerializePreview);
            Assert.Contains(previewCommands, entry => entry.CommandId == SadConsoleEditorCommandId.JumpPreviewRowToSourceTemplate);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void CommandMenuDirectionalNavigationChangesSelectedCommandAndEscDoesNotInvoke()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            context.OpenCommandMenu();
            var initial = context.CommandMenuSelectedIndex;

            context.MoveCommandMenuSelection(2);
            var moved = context.CommandMenuSelectedIndex;
            var cancel = context.CancelCommandMenu();

            Assert.NotEqual(initial, moved);
            Assert.False(context.IsCommandMenuOpen);
            Assert.False(context.IsEditingTemplatePresentation);
            Assert.False(context.Snapshot().IsDirty);
            Assert.Contains("no command was invoked", cancel.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void CommandMenuEnterInvokesTemplateNameEditAndActionStepEditor()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            context.OpenCommandMenu();
            MoveCommandMenuTo(context, SadConsoleEditorCommandId.EditTemplateName);

            var templateResult = context.ActivateSelectedCommand();
            context.CancelEdit();
            context.SelectSection(SadConsoleEditorSection.ActionPlans);
            context.OpenCommandMenu();
            MoveCommandMenuTo(context, SadConsoleEditorCommandId.OpenActionStepEditor);
            var actionPlanResult = context.ActivateSelectedCommand();

            Assert.True(templateResult.Succeeded, templateResult.Message);
            Assert.Equal(SadConsoleEditorCommandId.EditTemplateName, templateResult.Entry!.CommandId);
            Assert.Contains("Editing template name", templateResult.Message);
            Assert.True(actionPlanResult.Succeeded, actionPlanResult.Message);
            Assert.True(context.IsEditingActionPlanSteps);
            Assert.Equal(SadConsoleEditorCommandId.OpenActionStepEditor, actionPlanResult.Entry!.CommandId);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void CommandMenuEnterInvokesPreviewRematerializeAndSourceJump()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Preview);
            context.OpenCommandMenu();
            MoveCommandMenuTo(context, SadConsoleEditorCommandId.RematerializePreview);
            var previewResult = context.ActivateSelectedCommand();

            Assert.True(previewResult.Succeeded, previewResult.Message);
            Assert.NotNull(context.CachedPreview);
            Assert.Contains("Refreshed turn-0", previewResult.Message);

            context.MoveSelection(1);
            context.OpenCommandMenu();
            MoveCommandMenuTo(context, SadConsoleEditorCommandId.JumpPreviewRowToSourceTemplate);
            var jumpResult = context.ActivateSelectedCommand();

            Assert.True(jumpResult.Succeeded, jumpResult.Message);
            Assert.Equal(SadConsoleEditorSection.Templates, context.Section);
            Assert.Contains("Jumped to source template rock", jumpResult.Message);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void CommandMenuFooterIncludesContextualActions()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var context = SadConsoleEditorContext.Open(path, "editor-smoke").Context!;
            context.SelectSection(SadConsoleEditorSection.Templates);
            var templateView = SadConsoleEditorViewBuilder.Build(context, "templates");
            context.SelectSection(SadConsoleEditorSection.Preview);
            var previewView = SadConsoleEditorViewBuilder.Build(context, "preview");

            Assert.Contains("Enter opens command menu", templateView.PromptHint);
            Assert.Contains("M launches selected scenario", templateView.PromptHint);
            Assert.Contains("name/glyph/color/default plan/targeting/inventory brush", templateView.PromptHint);
            Assert.Contains("Rematerialize Preview", previewView.PromptHint);
            Assert.Contains("Jump row to source", previewView.PromptHint);
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
            defaultActionPlanId: moveEast
            actionStateDefaults:
              facing: South
              target: roomBox
            targetingRules:
            - slot: 1
              label: Primary target
              hint: Pick authored rock
              targetTemplateId: rock
              range: 4
            carriedEntities:
            - entityId: roomRock
              templateId: rock
              coord:
                x: 0
                y: 0
            - entityId: roomBox
              templateId: box
              coord:
                x: 2
                y: 0
          editorPlayer:
            name: Editor Player
            inventoryWidth: 1
            inventoryHeight: 1
            weight: 1
            carryingCapacity: 5
            defaultActionPlanId: moveEast
            actionStateDefaults:
              facing: East
          rock:
            name: Rock
            inventoryWidth: 0
            inventoryHeight: 0
            weight: 1
            carryingCapacity: 0
          box:
            name: Box
            inventoryWidth: 1
            inventoryHeight: 1
            weight: 1
            carryingCapacity: 5
            carriedEntities:
            - entityId: boxPebble
              templateId: pebble
              coord:
                x: 0
                y: 0
          pebble:
            name: Pebble
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
          rock:
            glyph: '*'
            color: Earth
          box:
            glyph: 'b'
            color: Earth
          pebble:
            glyph: '.'
            color: Gray
        actionPlans:
          waitPlan:
            id: waitPlan
            behavior:
              steps:
              - kind: TurnLeft
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
          second-smoke:
            name: Second Smoke
            scenarioRootEntityTemplateId: editorRoom
            playerEntityTemplateId: editorPlayer
            playerEntityId: editorPlayerTwo
            playerStart:
              x: 2
              y: 1
        """;

    private static void MovePickerTo(SadConsoleEditorContext context, string? actionPlanId)
    {
        var options = context.TemplateDefaultActionPlanPickerOptions();
        var target = options.ToList().FindIndex(option => string.Equals(option.ActionPlanId, actionPlanId, StringComparison.Ordinal));
        Assert.True(target >= 0, $"Picker option '{actionPlanId ?? "none"}' was not found.");
        context.MoveSelection(target - context.TemplateDefaultActionPlanPickerIndex);
    }

    private static void SetPendingTargetingLabel(SadConsoleEditorContext context, string label)
    {
        context.BeginTargetingRuleLabelEdit();
        context.TypeTargetingRuleLabelText(label);
        var result = context.ConfirmTargetingRuleLabelEdit();
        Assert.True(result.Succeeded, result.Message);
    }

    private static void MoveBrushTo(SadConsoleEditorContext context, string templateId)
    {
        var options = context.TemplateInventoryBrushOptions();
        var target = options.ToList().FindIndex(option => option.TemplateId == templateId);
        Assert.True(target >= 0, $"Brush option '{templateId}' was not found.");
        context.CycleTemplateInventoryBrush(target - context.InventoryBrush!.BrushTemplateIndex);
    }

    private static void MoveActionStepOptionTo(SadConsoleEditorContext context, ActionPlanBehaviorStepKind kind)
    {
        var options = context.AvailableActionStepOptions();
        var target = options.ToList().FindIndex(option => option.Kind == kind);
        Assert.True(target >= 0, $"Action step option '{kind}' was not found.");
        context.CycleActionStepEditorAvailable(target - context.ActionStepEdit!.AvailableActionStepIndex);
    }

    private static void MoveCommandMenuTo(SadConsoleEditorContext context, SadConsoleEditorCommandId commandId)
    {
        var entries = context.CommandMenuEntries();
        var target = entries.ToList().FindIndex(entry => entry.CommandId == commandId);
        Assert.True(target >= 0, $"Command menu entry '{commandId}' was not found.");
        context.MoveCommandMenuSelection(target - context.CommandMenuSelectedIndex);
    }

    private static void SelectActionPlan(SadConsoleEditorContext context, string actionPlanId)
    {
        var index = context.Snapshot().ActionPlans.ToList().FindIndex(plan => plan.ActionPlanId == actionPlanId);
        Assert.True(index >= 0, $"Action plan '{actionPlanId}' was not found.");
        context.MoveSelection(index - context.SelectedActionPlanIndex);
    }

    private static void SelectTemplate(SadConsoleEditorContext context, string templateId)
    {
        var index = context.Snapshot().EntityTemplates.ToList().FindIndex(template => template.TemplateId == templateId);
        Assert.True(index >= 0, $"Template '{templateId}' was not found.");
        context.MoveSelection(index - context.SelectedTemplateIndex);
    }

    private static string WriteTempContentFile(string yaml)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sadconsole-editor-context-{Guid.NewGuid():N}.yaml");
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

    private static void ReplaceCachedSnapshot(SadConsoleEditorContext context, FrontendEditorSnapshot snapshot)
    {
        var field = typeof(SadConsoleEditorContext).GetField("_snapshot", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(context, snapshot);
    }
}
