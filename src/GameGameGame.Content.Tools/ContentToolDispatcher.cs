using System.Text.Json;
using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Content.Tools;

public sealed class ContentToolDispatcher(ContentToolSessionRegistry sessions)
{
    private static readonly JsonSerializerOptions JsonOptions = ContentToolJson.Options;

    public ContentToolResponse Invoke(string toolName, JsonElement arguments) =>
        Invoke(toolName, Deserialize(toolName, arguments));

    public ContentToolResponse Invoke(string toolName, object request)
    {
        try
        {
            return toolName switch
            {
                ContentToolNames.CreateNew => CreateNew(),
                ContentToolNames.OpenFile => OpenFile((ContentToolOpenFileRequest)request),
                ContentToolNames.Snapshot => WithSession((ContentToolSessionRequest)request, api => ContentToolResponse.Success(api.GetDocumentSnapshot(), BuildSummary(api))),
                ContentToolNames.Validate => WithSession((ContentToolSessionRequest)request, api => FromAgentResult(api, api.Validate())),
                ContentToolNames.ValidateCanonicalAuthoring => WithSession((ContentToolSessionRequest)request, api => FromAgentResult(api, api.ValidateCanonicalAuthoring())),
                ContentToolNames.Save => WithSession((ContentToolSessionRequest)request, api => FromAgentResult(api, api.Save())),
                ContentToolNames.SaveAs => WithSession((ContentToolSaveAsRequest)request, api => FromAgentResult(api, api.SaveAs(((ContentToolSaveAsRequest)request).Path))),
                ContentToolNames.Close => Close((ContentToolSessionRequest)request),
                ContentToolNames.ListEntityTemplates => WithSession((ContentToolSessionRequest)request, api => ContentToolResponse.Success(ListEntityTemplates(api), BuildSummary(api))),
                ContentToolNames.GetEntityTemplate => WithSession((ContentToolEntityTemplateRequest)request, api => ContentToolResponse.Success(ListEntityTemplates(api).Single(template => template.EntityTemplateId == ((ContentToolEntityTemplateRequest)request).EntityTemplateId), BuildSummary(api))),
                ContentToolNames.CreateEntityTemplate => WithSession((ContentToolCreateEntityTemplateRequest)request, api => FromAgentResult(api, api.CreateEntityTemplate(((ContentToolCreateEntityTemplateRequest)request).Name), id => new ContentToolCreatedEntityTemplate(id))),
                ContentToolNames.UpdateEntityTemplate => WithSession((ContentToolUpdateEntityTemplateRequest)request, api => FromAgentResult(api, api.UpdateEntityTemplate(((ContentToolUpdateEntityTemplateRequest)request).EntityTemplateId, ((ContentToolUpdateEntityTemplateRequest)request).Update))),
                ContentToolNames.PlaceCarriedEntity => WithSession((ContentToolPlaceCarriedEntityRequest)request, api => FromAgentResult(api, api.PlaceCarriedEntity(((ContentToolPlaceCarriedEntityRequest)request).ParentTemplateId, ((ContentToolPlaceCarriedEntityRequest)request).CarriedTemplateId, ((ContentToolPlaceCarriedEntityRequest)request).Coord), id => new ContentToolPlacedCarriedEntity(id))),
                ContentToolNames.ListCarriedEntities => WithSession((ContentToolEntityTemplateRequest)request, api => ContentToolResponse.Success(api.Session.Editor.ListCarriedEntities(((ContentToolEntityTemplateRequest)request).EntityTemplateId), BuildSummary(api))),
                ContentToolNames.ListActionPlans => WithSession((ContentToolSessionRequest)request, api => ContentToolResponse.Success(ListActionPlans(api), BuildSummary(api))),
                ContentToolNames.GetActionPlan => WithSession((ContentToolActionPlanRequest)request, api => ContentToolResponse.Success(ListActionPlans(api).Single(plan => plan.ActionPlanTemplateId == ((ContentToolActionPlanRequest)request).ActionPlanTemplateId), BuildSummary(api))),
                ContentToolNames.CreateActionPlan => WithSession((ContentToolCreateActionPlanRequest)request, api => FromAgentResult(api, api.CreateActionPlan(((ContentToolCreateActionPlanRequest)request).Name), id => new ContentToolCreatedActionPlan(id))),
                ContentToolNames.SetActionPlanBehavior => WithSession((ContentToolSetActionPlanBehaviorRequest)request, api => FromAgentResult(api, api.SetActionPlanBehavior(((ContentToolSetActionPlanBehaviorRequest)request).ActionPlanTemplateId, ((ContentToolSetActionPlanBehaviorRequest)request).Steps))),
                ContentToolNames.AddActionPlanBehaviorStep => WithSession((ContentToolAddActionPlanBehaviorStepRequest)request, api => FromAgentResult(api, api.AddActionPlanBehaviorStep(((ContentToolAddActionPlanBehaviorStepRequest)request).ActionPlanTemplateId, ((ContentToolAddActionPlanBehaviorStepRequest)request).Kind))),
                ContentToolNames.MoveActionPlanBehaviorStep => WithSession((ContentToolMoveActionPlanBehaviorStepRequest)request, api => FromAgentResult(api, api.MoveActionPlanBehaviorStep(((ContentToolMoveActionPlanBehaviorStepRequest)request).ActionPlanTemplateId, ((ContentToolMoveActionPlanBehaviorStepRequest)request).FromIndex, ((ContentToolMoveActionPlanBehaviorStepRequest)request).ToIndex))),
                ContentToolNames.RemoveActionPlanBehaviorStep => WithSession((ContentToolRemoveActionPlanBehaviorStepRequest)request, api => FromAgentResult(api, api.RemoveActionPlanBehaviorStep(((ContentToolRemoveActionPlanBehaviorStepRequest)request).ActionPlanTemplateId, ((ContentToolRemoveActionPlanBehaviorStepRequest)request).StepIndex))),
                ContentToolNames.SetBehaviorStepTargetLabel => WithSession((ContentToolSetBehaviorStepTargetLabelRequest)request, api => FromAgentResult(api, api.SetActionPlanBehaviorStepTargetLabel(((ContentToolSetBehaviorStepTargetLabelRequest)request).ActionPlanTemplateId, ((ContentToolSetBehaviorStepTargetLabelRequest)request).StepIndex, ((ContentToolSetBehaviorStepTargetLabelRequest)request).TargetLabel))),
                ContentToolNames.SetBehaviorStepTargetSlot => WithSession((ContentToolSetBehaviorStepTargetSlotRequest)request, api => FromAgentResult(api, api.SetActionPlanBehaviorStepTargetSlot(((ContentToolSetBehaviorStepTargetSlotRequest)request).ActionPlanTemplateId, ((ContentToolSetBehaviorStepTargetSlotRequest)request).StepIndex, ((ContentToolSetBehaviorStepTargetSlotRequest)request).TargetSlot))),
                ContentToolNames.SetBehaviorStepCounterpartyTargetLabel => WithSession((ContentToolSetBehaviorStepCounterpartyTargetLabelRequest)request, api => FromAgentResult(api, api.SetActionPlanBehaviorStepCounterpartyTargetLabel(((ContentToolSetBehaviorStepCounterpartyTargetLabelRequest)request).ActionPlanTemplateId, ((ContentToolSetBehaviorStepCounterpartyTargetLabelRequest)request).StepIndex, ((ContentToolSetBehaviorStepCounterpartyTargetLabelRequest)request).TargetLabel))),
                ContentToolNames.SetBehaviorStepCounterpartyTargetSlot => WithSession((ContentToolSetBehaviorStepCounterpartyTargetSlotRequest)request, api => FromAgentResult(api, api.SetActionPlanBehaviorStepCounterpartyTargetSlot(((ContentToolSetBehaviorStepCounterpartyTargetSlotRequest)request).ActionPlanTemplateId, ((ContentToolSetBehaviorStepCounterpartyTargetSlotRequest)request).StepIndex, ((ContentToolSetBehaviorStepCounterpartyTargetSlotRequest)request).TargetSlot))),
                ContentToolNames.SetBehaviorStepPlanId => WithSession((ContentToolSetBehaviorStepPlanIdRequest)request, api => FromAgentResult(api, api.SetActionPlanBehaviorStepPlanId(((ContentToolSetBehaviorStepPlanIdRequest)request).ActionPlanTemplateId, ((ContentToolSetBehaviorStepPlanIdRequest)request).StepIndex, ((ContentToolSetBehaviorStepPlanIdRequest)request).PlanId))),
                ContentToolNames.SetBehaviorStepDirectionMode => WithSession((ContentToolSetBehaviorStepDirectionModeRequest)request, api => FromAgentResult(api, api.SetActionPlanBehaviorStepDirectionMode(((ContentToolSetBehaviorStepDirectionModeRequest)request).ActionPlanTemplateId, ((ContentToolSetBehaviorStepDirectionModeRequest)request).StepIndex, ((ContentToolSetBehaviorStepDirectionModeRequest)request).DirectionMode))),
                ContentToolNames.SetBehaviorStepCosts => WithSession((ContentToolSetBehaviorStepCostsRequest)request, api => FromAgentResult(api, api.SetActionPlanBehaviorStepCosts(((ContentToolSetBehaviorStepCostsRequest)request).ActionPlanTemplateId, ((ContentToolSetBehaviorStepCostsRequest)request).StepIndex, ((ContentToolSetBehaviorStepCostsRequest)request).Costs))),
                ContentToolNames.SetBehaviorStepTargetPathMode => WithSession((ContentToolSetBehaviorStepTargetPathModeRequest)request, api => FromAgentResult(api, api.SetActionPlanBehaviorStepTargetPathMode(((ContentToolSetBehaviorStepTargetPathModeRequest)request).ActionPlanTemplateId, ((ContentToolSetBehaviorStepTargetPathModeRequest)request).StepIndex, ((ContentToolSetBehaviorStepTargetPathModeRequest)request).PathMode))),
                ContentToolNames.SetBehaviorStepDesiredDistance => WithSession((ContentToolSetBehaviorStepDesiredDistanceRequest)request, api => FromAgentResult(api, api.SetActionPlanBehaviorStepDesiredDistance(((ContentToolSetBehaviorStepDesiredDistanceRequest)request).ActionPlanTemplateId, ((ContentToolSetBehaviorStepDesiredDistanceRequest)request).StepIndex, ((ContentToolSetBehaviorStepDesiredDistanceRequest)request).DesiredDistance))),
                ContentToolNames.SetBehaviorStepOrbitDirection => WithSession((ContentToolSetBehaviorStepOrbitDirectionRequest)request, api => FromAgentResult(api, api.SetActionPlanBehaviorStepOrbitDirection(((ContentToolSetBehaviorStepOrbitDirectionRequest)request).ActionPlanTemplateId, ((ContentToolSetBehaviorStepOrbitDirectionRequest)request).StepIndex, ((ContentToolSetBehaviorStepOrbitDirectionRequest)request).OrbitDirection))),
                ContentToolNames.ListActionSteps => WithSession((ContentToolSessionRequest)request, api => FromAgentResult(api, api.ListActionSteps())),
                ContentToolNames.PreviewActionPlan => WithSession((ContentToolPreviewActionPlanRequest)request, api => FromAgentResult(api, api.PreviewActionPlan(((ContentToolPreviewActionPlanRequest)request).ActionPlanTemplateId, ((ContentToolPreviewActionPlanRequest)request).EntityTemplateId))),
                ContentToolNames.ListScenarios => WithSession((ContentToolSessionRequest)request, api => ContentToolResponse.Success(ListScenarios(api), BuildSummary(api))),
                ContentToolNames.GetScenario => WithSession((ContentToolScenarioRequest)request, api => ContentToolResponse.Success(ListScenarios(api).Single(scenario => scenario.ScenarioId == ((ContentToolScenarioRequest)request).ScenarioId), BuildSummary(api))),
                ContentToolNames.UpsertScenario => WithSession((ContentToolUpsertScenarioRequest)request, api => FromAgentResult(api, api.UpsertScenario(((ContentToolUpsertScenarioRequest)request).Scenario))),
                ContentToolNames.MaterializeScenario => WithSession((ContentToolScenarioRequest)request, api => FromAgentResult(api, api.MaterializeScenario(((ContentToolScenarioRequest)request).ScenarioId))),
                ContentToolNames.RunScenarioById => WithSession((ContentToolRunScenarioByIdRequest)request, api => FromAgentResult(api, api.RunScenarioById(((ContentToolRunScenarioByIdRequest)request).ScenarioId, ((ContentToolRunScenarioByIdRequest)request).TurnCount, ((ContentToolRunScenarioByIdRequest)request).Options))),
                ContentToolNames.RunScenarioPlayerLogById => WithSession((ContentToolRunScenarioPlayerLogByIdRequest)request, api => FromAgentResult(api, api.RunScenarioPlayerLogById(((ContentToolRunScenarioPlayerLogByIdRequest)request).ScenarioId, ((ContentToolRunScenarioPlayerLogByIdRequest)request).TurnCount, ((ContentToolRunScenarioPlayerLogByIdRequest)request).ObserverEntityId))),
                ContentToolNames.PreviewAndRunScenarioById => WithSession((ContentToolRunScenarioByIdRequest)request, api => FromAgentResult(api, api.PreviewAndRunScenarioById(((ContentToolRunScenarioByIdRequest)request).ScenarioId, ((ContentToolRunScenarioByIdRequest)request).TurnCount, ((ContentToolRunScenarioByIdRequest)request).Options))),
                ContentToolNames.OpenScenarioManifest => ContentToolResponse.Success(ScenarioCatalog.LoadManifest(((ContentToolScenarioManifestRequest)request).Path)),
                ContentToolNames.ScanScenarioManifestCandidates => ContentToolResponse.Success(ScenarioCatalog.ScanCandidates(((ContentToolScenarioManifestScanRequest)request).FolderPath)),
                ContentToolNames.ValidateScenarioManifest => ValidateScenarioManifest((ContentToolScenarioManifestValidateRequest)request),
                ContentToolNames.GetAuthoringGuide => ContentToolResponse.Success(AuthoringGuide()),
                ContentToolNames.DescribeSchema => ContentToolResponse.Success(DescribeSchema(ReadConcept(request))),
                ContentToolNames.ListWorkflows => ContentToolResponse.Success(WorkflowRecipes()),
                ContentToolNames.ListExamples => ContentToolResponse.Success(ExampleReferences()),
                _ => ContentToolResponse.Failure(new AgentApiError("UnknownTool", $"Unknown content tool '{toolName}'.", Recoverable: true))
            };
        }
        catch (Exception ex)
        {
            return ContentToolResponse.Failure(AgentApiError.FromException("ToolDispatchFailed", ex));
        }
    }

    private ContentToolResponse CreateNew()
    {
        var opened = sessions.CreateNew();
        return ContentToolResponse.Success(opened);
    }

    private ContentToolResponse OpenFile(ContentToolOpenFileRequest request)
    {
        var result = sessions.OpenFile(request.Path);
        return result.IsSuccess ? ContentToolResponse.Success(result.Value) : ContentToolResponse.Failure(result.Error!);
    }

    private ContentToolResponse Close(ContentToolSessionRequest request) =>
        ContentToolResponse.Success(new { closed = sessions.Close(request.SessionId) });

    private static ContentToolResponse ValidateScenarioManifest(ContentToolScenarioManifestValidateRequest request)
    {
        var validation = ScenarioCatalog.ValidateManifest(request.Path, request.FolderPath);
        return ContentToolResponse.Success(new ContentToolScenarioManifestValidationSummary(validation.IsValid, validation.Diagnostics));
    }

    private static ContentToolAuthoringGuide AuthoringGuide() => new(
        StartHere: "Open/create a content session, list and inspect existing content, make semantic edits, validate and canonical-validate, review snapshot diff, then save deliberately.",
        AuthoritativeDocs:
        [
            "docs/Source of Truth/Content-Authoring-Manual.md",
            "docs/Source of Truth/Engine-Editor-Capabilities.md",
            "docs/Source of Truth/Capability-Gap-Log.md"
        ],
        CurrentAuthoringSurface:
        [
            "Documents: create/open/snapshot/validate/save sessions.",
            "Entity templates: create, inspect, update core inventory/presentation/policy fields, and place carried entities.",
            "Action plans: create, inspect, author behavior-chain steps, configure exposed step fields, preview.",
            "Scenarios: list/get/upsert/materialize/run persisted scenarios, including autonomous headless debug options and printable debug report lines, plus player narrative projections.",
            "Scenario manifests: open curated manifests, scan candidates, validate curated coverage."
        ],
        FirstCalls:
        [
            "ggg_content_open_file or ggg_content_create_new",
            "ggg_content_snapshot",
            "ggg_content_list_entity_templates / ggg_content_list_action_plans / ggg_content_list_scenarios",
            "ggg_content_list_action_steps",
            "ggg_content_validate and ggg_content_validate_canonical_authoring"
        ],
        SafetyRules:
        [
            "Prefer ggg_content_* semantic tools over ad-hoc YAML edits when they cover the task.",
            "Discovery/list/preview tools must not mutate content; save only with explicit save/save_as.",
            "Use the snapshot diff and validation summaries before saving.",
            "If a desired engine behavior cannot be authored through the listed surface, record a capability gap rather than inventing schema."
        ]);

    private static IReadOnlyList<ContentToolWorkflowRecipe> WorkflowRecipes() =>
    [
        new("open-review", "Open and review a content file", "Start any edit or audit of an existing YAML document.",
            ["Open the file.", "Request a snapshot.", "Validate normal and canonical authoring rules.", "List entity templates, action plans, scenarios, and action-step catalog."],
            [ContentToolNames.OpenFile, ContentToolNames.Snapshot, ContentToolNames.Validate, ContentToolNames.ValidateCanonicalAuthoring, ContentToolNames.ListEntityTemplates, ContentToolNames.ListActionPlans, ContentToolNames.ListScenarios, ContentToolNames.ListActionSteps]),
        new("behavior-plan", "Create or edit a behavior-chain action plan", "Author normal action-plan behavior without legacy low-level YAML editing.",
            ["Create or inspect an action plan.", "List action steps to choose supported kinds.", "Set/add/reorder behavior steps.", "Use field-specific setters for target labels, direction/path options, plan refs, and costs.", "Preview the action plan."],
            [ContentToolNames.CreateActionPlan, ContentToolNames.ListActionSteps, ContentToolNames.SetActionPlanBehavior, ContentToolNames.AddActionPlanBehaviorStep, ContentToolNames.PreviewActionPlan]),
        new("scenario-run", "Create or inspect a persisted scenario", "Check that authored content materializes and runs through shared Content/Core services.",
            ["List or upsert scenarios.", "Materialize the scenario to catch authoring diagnostics.", "Run for a small turn count and inspect debugReportLines.", "For autonomous experiments, pass options.ignorePlayerChoiceControl=true; use traceActorFilter with includeAllTraces=false to focus traces.", "Use player-log projection when compact player-facing rows are desired."],
            [ContentToolNames.ListScenarios, ContentToolNames.UpsertScenario, ContentToolNames.MaterializeScenario, ContentToolNames.RunScenarioById, ContentToolNames.RunScenarioPlayerLogById]),
        new("manifest-maintenance", "Maintain curated scenario manifests", "Reconcile scenario-browser manifest entries with content files.",
            ["Scan a folder for candidates.", "Open the curated manifest.", "Validate manifest coverage and metadata.", "Treat the manifest as authoritative; scan output is reconciliation only."],
            [ContentToolNames.ScanScenarioManifestCandidates, ContentToolNames.OpenScenarioManifest, ContentToolNames.ValidateScenarioManifest]),
        new("safe-save-loop", "Safe save loop", "Finish any deliberate content edit.",
            ["Validate.", "Canonical-validate.", "Review snapshot diff and dirty state.", "Save or save_as explicitly.", "Close when done."],
            [ContentToolNames.Validate, ContentToolNames.ValidateCanonicalAuthoring, ContentToolNames.Snapshot, ContentToolNames.Save, ContentToolNames.Close])
    ];

    private static IReadOnlyList<ContentToolExampleReference> ExampleReferences() =>
    [
        new("Built-in content root and reusable canonical definitions", "src/GameGameGame.Content", []),
        new("Curated Beta scenario manifest", "src/GameGameGame.Content/Beta/Manifest.yaml", []),
        new("Targeting scenario examples", "src/GameGameGame.Content/Beta/Targeting", []),
        new("Transfer scenario examples", "src/GameGameGame.Content/Beta/Transfer", []),
        new("Topology scenario examples", "src/GameGameGame.Content/Beta/Topology", []),
        new("Content/editor regression fixtures and inline examples", "tests/GameGameGame.Tests", [])
    ];

    private static ContentToolSchemaDescription DescribeSchema(string concept) => concept switch
    {
        "entityTemplateUpdate" => new(concept, "Partial update object for ggg_content_update_entity_template.",
            [
                new("name", "string", false, "Template display name."),
                new("inventoryWidth", "integer", false, "Authored inventory width; 0 means no usable inventory."),
                new("inventoryHeight", "integer", false, "Authored inventory height; 0 means no usable inventory."),
                new("bulk", "integer", false, "Bulk for inventory/aperture rules."),
                new("aperture", "integer", false, "Aperture for inventory transitions."),
                new("material", "string|null", false, "Presentation-only material for inventory backdrop presentation. Valid explicit values are metal, wood, stone; omitted/cleared is undefined and uses gridDotted debug/fallback presentation.", ["metal", "wood", "stone"], NullableClears: true),
                new("enterPolicy", "string|null", false, "Authored enter-placement policy.", Enum.GetNames<EntityEnterPolicy>(), NullableClears: true),
                new("exitPolicy", "string|null", false, "Authored exit-source policy.", Enum.GetNames<EntityExitPolicy>(), NullableClears: true),
                new("topologyPolicy", "string", false, "Directed inventory-boundary topology policy.", Enum.GetNames<EntityTopologyPolicy>()),
                new("clearEnterPolicy", "boolean", false, "Set true to clear enterPolicy."),
                new("clearExitPolicy", "boolean", false, "Set true to clear exitPolicy."),
                new("clearMaterial", "boolean", false, "Set true to clear authored material."),
                new("glyph", "one-character string", false, "Legacy/editor glyph."),
                new("color", "string", false, "Presentation color.", Enum.GetNames<PresentationColor>())
            ],
            ["{ \"name\": \"Player\", \"inventoryWidth\": 0, \"inventoryHeight\": 0, \"bulk\": 1, \"aperture\": 5, \"material\": \"wood\", \"glyph\": \"@\", \"color\": \"Yellow\" }"]),
        "scenario" => new(concept, "Persisted scenario object for ggg_content_upsert_scenario.",
            [
                new("scenarioId", "string", true, "Stable scenario ID."),
                new("name", "string", true, "Human-readable scenario name."),
                new("scenarioRootEntityTemplateId", "string", true, "Root/container template to materialize."),
                new("playerEntityTemplateId", "string|null", false, "Compatibility player template; can be null when using placed Player controllers.", NullableClears: true),
                new("playerEntityId", "string|null", false, "Compatibility inserted player entity ID.", NullableClears: true),
                new("playerStart", "coord|null", false, "Compatibility player start coordinate { x, y }.", NullableClears: true),
                new("playerControls", "object", false, "Optional input/control binding map from player/input IDs to entity IDs.")
            ],
            ["{ \"scenarioId\": \"demo\", \"name\": \"Demo\", \"scenarioRootEntityTemplateId\": \"room\", \"playerEntityTemplateId\": \"player\", \"playerEntityId\": \"player1\", \"playerStart\": { \"x\": 0, \"y\": 0 } }"]),
        "coord" => new(concept, "Grid coordinate object.", [new("x", "integer", true, "Horizontal coordinate."), new("y", "integer", true, "Vertical coordinate.")], ["{ \"x\": 1, \"y\": 2 }"]),
        "behaviorStep" => new(concept, "Behavior-chain Action Step kind plus optional fields set by field-specific tools.",
            [new("kind", "string", true, "Action Step kind.", Enum.GetNames<ActionPlanBehaviorStepKind>()), new("targetLabel", "string|null", false, "Stable labeled target reference.", NullableClears: true), new("targetSlot", "integer|null", false, "Compatibility numeric target slot.", NullableClears: true), new("counterpartyTargetLabel", "string|null", false, "Transfer-only stable labeled counterparty target reference.", NullableClears: true), new("counterpartyTargetSlot", "integer|null", false, "Transfer-only compatibility numeric counterparty target slot.", NullableClears: true), new("costs", "cost[]", false, "Optional required costs consumed by the step.")],
            ["{ \"kind\": \"Move\" }", "then call ggg_content_set_behavior_step_direction_mode for Move directionMode"]),
        "cost" => new(concept, "Action Step cost descriptor.", [new("templateId", "string", true, "Entity template ID used as the cost item."), new("quantity", "integer", true, "Required quantity.")], ["{ \"templateId\": \"scrap\", \"quantity\": 3 }"]),
        _ => new(concept, "Unknown schema concept. Use one of: entityTemplateUpdate, scenario, coord, behaviorStep, cost.", [], [])
    };

    private static string ReadConcept(object request)
    {
        if (request is ContentToolDescribeSchemaRequest typed) return typed.Concept;
        if (request is JsonElement json && json.TryGetProperty("concept", out var concept)) return concept.GetString() ?? string.Empty;
        var property = request.GetType().GetProperty("concept") ?? request.GetType().GetProperty("Concept");
        return property?.GetValue(request)?.ToString() ?? string.Empty;
    }

    private ContentToolResponse WithSession(IContentToolSessionRequest request, Func<AgentContentEditorApi, ContentToolResponse> operation) =>
        WithSession(request.SessionId, operation);

    private ContentToolResponse WithSession(string sessionId, Func<AgentContentEditorApi, ContentToolResponse> operation)
    {
        var result = sessions.Get(sessionId);
        return result.IsSuccess ? operation(result.Value!) : ContentToolResponse.Failure(result.Error!);
    }

    private ContentToolResponse FromAgentResult(AgentContentEditorApi api, AgentApiResult result) =>
        result.IsSuccess ? ContentToolResponse.Success(summary: BuildSummary(api)) : ContentToolResponse.Failure(result.Error!, BuildSummary(api));

    private ContentToolResponse FromAgentResult<T>(AgentContentEditorApi api, AgentApiResult<T> result) =>
        result.IsSuccess ? ContentToolResponse.Success(result.Value, BuildSummary(api)) : ContentToolResponse.Failure(result.Error!, BuildSummary(api));

    private ContentToolResponse FromAgentResult<T, TMapped>(AgentContentEditorApi api, AgentApiResult<T> result, Func<T, TMapped> map) =>
        result.IsSuccess ? ContentToolResponse.Success(map(result.Value!), BuildSummary(api)) : ContentToolResponse.Failure(result.Error!, BuildSummary(api));

    private static ContentToolMutationSummary BuildSummary(AgentContentEditorApi api)
    {
        var snapshot = api.GetDocumentSnapshot();
        return new ContentToolMutationSummary(
            snapshot.FilePath,
            snapshot.IsDirty,
            snapshot.Validation.IsValid,
            snapshot.CanonicalValidation.IsValid,
            snapshot.Validation.Diagnostics.Select(diagnostic => diagnostic.Message).ToList(),
            snapshot.CanonicalValidation.Diagnostics.Select(diagnostic => diagnostic.Message).ToList(),
            snapshot.YamlDiffLines.Take(80).ToList());
    }

    private static IReadOnlyList<ContentToolEntityTemplateSummary> ListEntityTemplates(AgentContentEditorApi api)
    {
        var validation = api.Session.Editor.Validate();
        return api.Session.Editor.ListEntityPresets()
            .Select(preset => new ContentToolEntityTemplateSummary(
                preset.Id,
                preset.Template.Name,
                preset.Template.InventoryWidth,
                preset.Template.InventoryHeight,
                preset.Template.Bulk,
                preset.Template.Aperture,
                preset.Presentation.PresentationId,
                preset.Presentation.PaletteId,
                preset.Presentation.Glyph,
                preset.Presentation.Color,
                preset.Template.DefaultActionPlanId,
                validation.ForEntityTemplate(preset.Id)))
            .ToList();
    }

    private static IReadOnlyList<ContentToolActionPlanSummary> ListActionPlans(AgentContentEditorApi api)
    {
        var validation = api.Session.Editor.Validate();
        return api.Session.Editor.ListActionPlans()
            .Select(plan => new ContentToolActionPlanSummary(
                plan.TemplateId,
                plan.Descriptor.Id,
                plan.Descriptor.Id.Value,
                ContentEditorService.FormatActionPlanShape(ActionPlanShapeClassifier.Classify(plan.Descriptor)),
                plan.Descriptor.Behavior?.Steps ?? [],
                validation.ForActionPlan(plan.TemplateId)))
            .ToList();
    }

    private static IReadOnlyList<ContentToolScenarioSummary> ListScenarios(AgentContentEditorApi api) =>
        api.Session.Document.Scenarios
            .OrderBy(entry => entry.Key)
            .Select(entry => api.Session.Editor.GetScenario(entry.Key))
            .Select(scenario => new ContentToolScenarioSummary(
                scenario.ScenarioId,
                scenario.Name,
                scenario.ScenarioRootEntityTemplateId,
                scenario.PlayerEntityTemplateId,
                scenario.PlayerEntityId,
                scenario.PlayerStart,
                scenario.PlayerControls))
            .ToList();

    private static object Deserialize(string toolName, JsonElement arguments) => toolName switch
    {
        ContentToolNames.CreateNew => new ContentToolCreateNewRequest(),
        ContentToolNames.OpenFile => arguments.Deserialize<ContentToolOpenFileRequest>(JsonOptions)!,
        ContentToolNames.OpenScenarioManifest => arguments.Deserialize<ContentToolScenarioManifestRequest>(JsonOptions)!,
        ContentToolNames.ScanScenarioManifestCandidates => arguments.Deserialize<ContentToolScenarioManifestScanRequest>(JsonOptions)!,
        ContentToolNames.ValidateScenarioManifest => arguments.Deserialize<ContentToolScenarioManifestValidateRequest>(JsonOptions)!,
        ContentToolNames.DescribeSchema => arguments.Deserialize<ContentToolDescribeSchemaRequest>(JsonOptions)!,
        ContentToolNames.GetAuthoringGuide or ContentToolNames.ListWorkflows or ContentToolNames.ListExamples => new { },
        ContentToolNames.Snapshot or ContentToolNames.Validate or ContentToolNames.ValidateCanonicalAuthoring or ContentToolNames.Save or ContentToolNames.Close or ContentToolNames.ListEntityTemplates or ContentToolNames.ListActionPlans or ContentToolNames.ListActionSteps or ContentToolNames.ListScenarios => arguments.Deserialize<ContentToolSessionRequest>(JsonOptions)!,
        ContentToolNames.SaveAs => arguments.Deserialize<ContentToolSaveAsRequest>(JsonOptions)!,
        ContentToolNames.GetEntityTemplate or ContentToolNames.ListCarriedEntities => arguments.Deserialize<ContentToolEntityTemplateRequest>(JsonOptions)!,
        ContentToolNames.CreateEntityTemplate => arguments.Deserialize<ContentToolCreateEntityTemplateRequest>(JsonOptions)!,
        ContentToolNames.UpdateEntityTemplate => arguments.Deserialize<ContentToolUpdateEntityTemplateRequest>(JsonOptions)!,
        ContentToolNames.PlaceCarriedEntity => arguments.Deserialize<ContentToolPlaceCarriedEntityRequest>(JsonOptions)!,
        ContentToolNames.GetActionPlan => arguments.Deserialize<ContentToolActionPlanRequest>(JsonOptions)!,
        ContentToolNames.CreateActionPlan => arguments.Deserialize<ContentToolCreateActionPlanRequest>(JsonOptions)!,
        ContentToolNames.SetActionPlanBehavior => arguments.Deserialize<ContentToolSetActionPlanBehaviorRequest>(JsonOptions)!,
        ContentToolNames.AddActionPlanBehaviorStep => arguments.Deserialize<ContentToolAddActionPlanBehaviorStepRequest>(JsonOptions)!,
        ContentToolNames.MoveActionPlanBehaviorStep => arguments.Deserialize<ContentToolMoveActionPlanBehaviorStepRequest>(JsonOptions)!,
        ContentToolNames.RemoveActionPlanBehaviorStep => arguments.Deserialize<ContentToolRemoveActionPlanBehaviorStepRequest>(JsonOptions)!,
        ContentToolNames.SetBehaviorStepTargetLabel => arguments.Deserialize<ContentToolSetBehaviorStepTargetLabelRequest>(JsonOptions)!,
        ContentToolNames.SetBehaviorStepTargetSlot => arguments.Deserialize<ContentToolSetBehaviorStepTargetSlotRequest>(JsonOptions)!,
        ContentToolNames.SetBehaviorStepCounterpartyTargetLabel => arguments.Deserialize<ContentToolSetBehaviorStepCounterpartyTargetLabelRequest>(JsonOptions)!,
        ContentToolNames.SetBehaviorStepCounterpartyTargetSlot => arguments.Deserialize<ContentToolSetBehaviorStepCounterpartyTargetSlotRequest>(JsonOptions)!,
        ContentToolNames.SetBehaviorStepPlanId => arguments.Deserialize<ContentToolSetBehaviorStepPlanIdRequest>(JsonOptions)!,
        ContentToolNames.SetBehaviorStepDirectionMode => arguments.Deserialize<ContentToolSetBehaviorStepDirectionModeRequest>(JsonOptions)!,
        ContentToolNames.SetBehaviorStepCosts => arguments.Deserialize<ContentToolSetBehaviorStepCostsRequest>(JsonOptions)!,
        ContentToolNames.SetBehaviorStepTargetPathMode => arguments.Deserialize<ContentToolSetBehaviorStepTargetPathModeRequest>(JsonOptions)!,
        ContentToolNames.SetBehaviorStepDesiredDistance => arguments.Deserialize<ContentToolSetBehaviorStepDesiredDistanceRequest>(JsonOptions)!,
        ContentToolNames.SetBehaviorStepOrbitDirection => arguments.Deserialize<ContentToolSetBehaviorStepOrbitDirectionRequest>(JsonOptions)!,
        ContentToolNames.PreviewActionPlan => arguments.Deserialize<ContentToolPreviewActionPlanRequest>(JsonOptions)!,
        ContentToolNames.GetScenario or ContentToolNames.MaterializeScenario => arguments.Deserialize<ContentToolScenarioRequest>(JsonOptions)!,
        ContentToolNames.UpsertScenario => arguments.Deserialize<ContentToolUpsertScenarioRequest>(JsonOptions)!,
        ContentToolNames.RunScenarioById or ContentToolNames.PreviewAndRunScenarioById => arguments.Deserialize<ContentToolRunScenarioByIdRequest>(JsonOptions)!,
        ContentToolNames.RunScenarioPlayerLogById => arguments.Deserialize<ContentToolRunScenarioPlayerLogByIdRequest>(JsonOptions)!,
        _ => new { }
    };
}
