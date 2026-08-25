using System.Text.Json.Nodes;
using GameGameGame.Core;

namespace GameGameGame.Content.Tools;

public static class ContentToolCatalog
{
    public static readonly IReadOnlyList<string> Names =
    [
        ContentToolNames.CreateNew,
        ContentToolNames.OpenFile,
        ContentToolNames.Snapshot,
        ContentToolNames.Validate,
        ContentToolNames.ValidateCanonicalAuthoring,
        ContentToolNames.Save,
        ContentToolNames.SaveAs,
        ContentToolNames.Close,
        ContentToolNames.ListEntityTemplates,
        ContentToolNames.GetEntityTemplate,
        ContentToolNames.CreateEntityTemplate,
        ContentToolNames.UpdateEntityTemplate,
        ContentToolNames.PlaceCarriedEntity,
        ContentToolNames.ListCarriedEntities,
        ContentToolNames.ListActionPlans,
        ContentToolNames.GetActionPlan,
        ContentToolNames.CreateActionPlan,
        ContentToolNames.SetActionPlanBehavior,
        ContentToolNames.AddActionPlanBehaviorStep,
        ContentToolNames.MoveActionPlanBehaviorStep,
        ContentToolNames.RemoveActionPlanBehaviorStep,
        ContentToolNames.SetBehaviorStepTargetLabel,
        ContentToolNames.SetBehaviorStepTargetSlot,
        ContentToolNames.SetBehaviorStepCounterpartyTargetLabel,
        ContentToolNames.SetBehaviorStepCounterpartyTargetSlot,
        ContentToolNames.SetBehaviorStepPlanId,
        ContentToolNames.SetBehaviorStepDirectionMode,
        ContentToolNames.SetBehaviorStepCosts,
        ContentToolNames.SetBehaviorStepTargetPathMode,
        ContentToolNames.SetBehaviorStepDesiredDistance,
        ContentToolNames.SetBehaviorStepOrbitDirection,
        ContentToolNames.ListActionSteps,
        ContentToolNames.PreviewActionPlan,
        ContentToolNames.ListScenarios,
        ContentToolNames.GetScenario,
        ContentToolNames.UpsertScenario,
        ContentToolNames.MaterializeScenario,
        ContentToolNames.RunScenarioById,
        ContentToolNames.RunScenarioPlayerLogById,
        ContentToolNames.PreviewAndRunScenarioById,
        ContentToolNames.OpenScenarioManifest,
        ContentToolNames.ScanScenarioManifestCandidates,
        ContentToolNames.ValidateScenarioManifest,
        ContentToolNames.GetAuthoringGuide,
        ContentToolNames.DescribeSchema,
        ContentToolNames.ListWorkflows,
        ContentToolNames.ListExamples
    ];

    public static string Describe(string name) => name switch
    {
        ContentToolNames.CreateNew => "Create a new in-memory GameGameGame content editor session.",
        ContentToolNames.OpenFile => "Open a YAML content file and return a sessionId for later tool calls.",
        ContentToolNames.Snapshot => "Return YAML preview, compact diff, dirty state, and validation summaries for a session.",
        ContentToolNames.Validate => "Validate the current content document.",
        ContentToolNames.ValidateCanonicalAuthoring => "Validate canonical authoring rules for the current content document.",
        ContentToolNames.Save => "Save the currently opened file-backed content session.",
        ContentToolNames.SaveAs => "Save the session to a chosen YAML file path.",
        ContentToolNames.Close => "Close a content editor tool session.",
        ContentToolNames.ListEntityTemplates => "List entity templates with presentation and validation diagnostics.",
        ContentToolNames.GetEntityTemplate => "Get one entity template summary.",
        ContentToolNames.CreateEntityTemplate => "Create an entity template through AgentContentEditorApi.",
        ContentToolNames.UpdateEntityTemplate => "Update entity template/presentation fields through AgentContentEditorApi.",
        ContentToolNames.PlaceCarriedEntity => "Place a carried entity in an authored inventory layout.",
        ContentToolNames.ListCarriedEntities => "List carried entities for an entity template inventory layout.",
        ContentToolNames.ListActionPlans => "List action plans with canonical behavior summaries and diagnostics.",
        ContentToolNames.GetActionPlan => "Get one action plan summary.",
        ContentToolNames.CreateActionPlan => "Create an action plan through AgentContentEditorApi.",
        ContentToolNames.SetActionPlanBehavior => "Replace an action plan's canonical behavior-chain steps.",
        ContentToolNames.AddActionPlanBehaviorStep => "Append a canonical behavior-chain Action Step.",
        ContentToolNames.MoveActionPlanBehaviorStep => "Move a canonical behavior-chain Action Step.",
        ContentToolNames.RemoveActionPlanBehaviorStep => "Remove a canonical behavior-chain Action Step.",
        ContentToolNames.SetBehaviorStepTargetLabel => "Set or clear a canonical Action Step target label.",
        ContentToolNames.SetBehaviorStepTargetSlot => "Set or clear a compatibility numeric Action Step target slot.",
        ContentToolNames.SetBehaviorStepCounterpartyTargetLabel => "Set or clear a Transfer Action Step counterparty target label.",
        ContentToolNames.SetBehaviorStepCounterpartyTargetSlot => "Set or clear a Transfer Action Step counterparty target slot.",
        ContentToolNames.SetBehaviorStepPlanId => "Set or clear a referenced Action Plan ID on an apply-plan Action Step.",
        ContentToolNames.SetBehaviorStepDirectionMode => "Set or clear a canonical Move/Transfer Action Step directionMode.",
        ContentToolNames.SetBehaviorStepCosts => "Set or clear optional behavior Action Step costs.",
        ContentToolNames.SetBehaviorStepTargetPathMode => "Set or clear a TargetPathMove pathMode.",
        ContentToolNames.SetBehaviorStepDesiredDistance => "Set or clear a TargetPathMove desiredDistance.",
        ContentToolNames.SetBehaviorStepOrbitDirection => "Set or clear a TargetPathMove orbitDirection.",
        ContentToolNames.ListActionSteps => "List stable canonical Action Step descriptors.",
        ContentToolNames.PreviewActionPlan => "Preview an action plan through ContentEditorService.",
        ContentToolNames.ListScenarios => "List persisted scenario definitions.",
        ContentToolNames.GetScenario => "Get one persisted scenario definition summary.",
        ContentToolNames.UpsertScenario => "Create or update a persisted scenario definition.",
        ContentToolNames.MaterializeScenario => "Materialize a persisted scenario by ID and report diagnostics.",
        ContentToolNames.RunScenarioById => "Run a persisted scenario by ID and return setup, validation/runtime diagnostics, turn traces, final state, inventory summary, and a printable debug report.",
        ContentToolNames.RunScenarioPlayerLogById => "Run a persisted scenario by ID and return compact player narrative projection message IDs/args without debug traces/final state/inventory summaries.",
        ContentToolNames.PreviewAndRunScenarioById => "Return validation, previews, materialization, and scenario run/debug report for one scenario.",
        ContentToolNames.OpenScenarioManifest => "Open a curated scenario manifest/catalog artifact with sections and entry metadata.",
        ContentToolNames.ScanScenarioManifestCandidates => "Scan a content folder for scenario candidates without making the scan authoritative.",
        ContentToolNames.ValidateScenarioManifest => "Validate a curated scenario manifest against content files and scanned unclassified candidates.",
        ContentToolNames.GetAuthoringGuide => "Return a fresh-agent content-authoring guide with workflow, docs, and safety rules.",
        ContentToolNames.DescribeSchema => "Describe an authoring concept schema, clear/null semantics, enum values, and examples.",
        ContentToolNames.ListWorkflows => "List common machine-readable ggg_content workflow recipes.",
        ContentToolNames.ListExamples => "List useful content example files and scenario IDs to inspect.",
        _ => "GameGameGame content editor tool."
    };

    public static JsonObject InputSchema(string name)
    {
        var properties = new JsonObject();
        var required = new JsonArray();
        void AddString(string property, bool isRequired = true, IReadOnlyList<string>? allowedValues = null, string? description = null, bool allowNull = false)
        {
            var schema = new JsonObject { ["type"] = allowNull ? new JsonArray("string", "null") : "string" };
            if (allowedValues is not null)
            {
                var values = allowedValues.Select(value => JsonValue.Create(value)).ToList<JsonNode?>();
                if (allowNull) values.Add(null);
                schema["enum"] = new JsonArray(values.ToArray());
            }
            if (description is not null) schema["description"] = description;
            properties[property] = schema;
            if (isRequired) required.Add(property);
        }
        void AddInteger(string property, bool isRequired = true, string? description = null, bool allowNull = false)
        {
            var schema = new JsonObject { ["type"] = allowNull ? new JsonArray("integer", "null") : "integer" };
            if (description is not null) schema["description"] = description;
            properties[property] = schema;
            if (isRequired) required.Add(property);
        }
        void AddObject(string property, bool isRequired = true, JsonObject? schema = null)
        {
            properties[property] = schema ?? new JsonObject { ["type"] = "object", ["additionalProperties"] = true };
            if (isRequired) required.Add(property);
        }
        void AddArray(string property, bool isRequired = true, JsonObject? itemSchema = null)
        {
            properties[property] = new JsonObject { ["type"] = "array", ["items"] = itemSchema ?? new JsonObject { ["type"] = "string" } };
            if (isRequired) required.Add(property);
        }

        var noSession = name is ContentToolNames.CreateNew
            or ContentToolNames.GetAuthoringGuide
            or ContentToolNames.DescribeSchema
            or ContentToolNames.ListWorkflows
            or ContentToolNames.ListExamples;

        if (!noSession)
        {
            if (name is ContentToolNames.OpenFile or ContentToolNames.OpenScenarioManifest or ContentToolNames.ValidateScenarioManifest)
            {
                AddString("path");
            }
            else if (name is ContentToolNames.ScanScenarioManifestCandidates)
            {
                AddString("folderPath");
            }
            else
            {
                AddString("sessionId");
            }
        }

        switch (name)
        {
            case ContentToolNames.SaveAs: AddString("path"); break;
            case ContentToolNames.GetEntityTemplate or ContentToolNames.ListCarriedEntities: AddString("entityTemplateId"); break;
            case ContentToolNames.CreateEntityTemplate or ContentToolNames.CreateActionPlan: AddString("name"); break;
            case ContentToolNames.DescribeSchema: AddString("concept", allowedValues: ["entityTemplateUpdate", "scenario", "coord", "behaviorStep", "cost"]); break;
            case ContentToolNames.UpdateEntityTemplate: AddString("entityTemplateId"); AddObject("update", schema: EntityTemplateUpdateSchema()); break;
            case ContentToolNames.PlaceCarriedEntity: AddString("parentTemplateId"); AddString("carriedTemplateId"); AddObject("coord", schema: CoordSchema()); break;
            case ContentToolNames.GetActionPlan or ContentToolNames.PreviewActionPlan: AddString("actionPlanTemplateId"); break;
            case ContentToolNames.SetActionPlanBehavior: AddString("actionPlanTemplateId"); AddArray("steps", itemSchema: EnumStringSchema<ActionPlanBehaviorStepKind>()); break;
            case ContentToolNames.AddActionPlanBehaviorStep: AddString("actionPlanTemplateId"); AddString("kind", allowedValues: EnumNames<ActionPlanBehaviorStepKind>()); break;
            case ContentToolNames.MoveActionPlanBehaviorStep: AddString("actionPlanTemplateId"); AddInteger("fromIndex"); AddInteger("toIndex"); break;
            case ContentToolNames.RemoveActionPlanBehaviorStep: AddString("actionPlanTemplateId"); AddInteger("stepIndex"); break;
            case ContentToolNames.SetBehaviorStepTargetLabel: AddString("actionPlanTemplateId"); AddInteger("stepIndex"); AddString("targetLabel", isRequired: false, description: "Omit or pass null to clear.", allowNull: true); break;
            case ContentToolNames.SetBehaviorStepTargetSlot: AddString("actionPlanTemplateId"); AddInteger("stepIndex"); AddInteger("targetSlot", isRequired: false, description: "Omit or pass null to clear.", allowNull: true); break;
            case ContentToolNames.SetBehaviorStepCounterpartyTargetLabel: AddString("actionPlanTemplateId"); AddInteger("stepIndex"); AddString("targetLabel", isRequired: false, description: "Omit or pass null to clear.", allowNull: true); break;
            case ContentToolNames.SetBehaviorStepCounterpartyTargetSlot: AddString("actionPlanTemplateId"); AddInteger("stepIndex"); AddInteger("targetSlot", isRequired: false, description: "Omit or pass null to clear.", allowNull: true); break;
            case ContentToolNames.SetBehaviorStepPlanId: AddString("actionPlanTemplateId"); AddInteger("stepIndex"); AddString("planId", isRequired: false, description: "Omit or pass null to clear.", allowNull: true); break;
            case ContentToolNames.SetBehaviorStepDirectionMode: AddString("actionPlanTemplateId"); AddInteger("stepIndex"); AddString("directionMode", isRequired: false, allowedValues: EnumNames<ActionPlanMoveDirectionMode>(), description: "Omit or pass null to clear.", allowNull: true); break;
            case ContentToolNames.SetBehaviorStepCosts: AddString("actionPlanTemplateId"); AddInteger("stepIndex"); AddArray("costs", itemSchema: CostSchema()); break;
            case ContentToolNames.SetBehaviorStepTargetPathMode: AddString("actionPlanTemplateId"); AddInteger("stepIndex"); AddString("pathMode", isRequired: false, allowedValues: EnumNames<ActionPlanTargetPathMode>(), description: "Omit or pass null to clear.", allowNull: true); break;
            case ContentToolNames.SetBehaviorStepDesiredDistance: AddString("actionPlanTemplateId"); AddInteger("stepIndex"); AddInteger("desiredDistance", isRequired: false, description: "Omit or pass null to clear.", allowNull: true); break;
            case ContentToolNames.SetBehaviorStepOrbitDirection: AddString("actionPlanTemplateId"); AddInteger("stepIndex"); AddString("orbitDirection", isRequired: false, allowedValues: EnumNames<ActionPlanOrbitDirection>(), description: "Omit or pass null to clear.", allowNull: true); break;
            case ContentToolNames.GetScenario or ContentToolNames.MaterializeScenario: AddString("scenarioId"); break;
            case ContentToolNames.UpsertScenario: AddObject("scenario", schema: ScenarioSchema()); break;
            case ContentToolNames.RunScenarioById or ContentToolNames.PreviewAndRunScenarioById or ContentToolNames.RunScenarioPlayerLogById:
                AddString("scenarioId");
                AddInteger("turnCount");
                if (name is ContentToolNames.RunScenarioPlayerLogById)
                {
                    AddString("observerEntityId", isRequired: false);
                }
                else
                {
                    AddObject("options", isRequired: false, schema: ScenarioRunOptionsSchema());
                }
                break;
            case ContentToolNames.ValidateScenarioManifest: AddString("folderPath"); break;
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required,
            ["additionalProperties"] = false
        };
    }

    private static IReadOnlyList<string> EnumNames<T>() where T : struct, Enum => Enum.GetNames<T>();

    private static JsonObject EnumStringSchema<T>() where T : struct, Enum => new()
    {
        ["type"] = "string",
        ["enum"] = new JsonArray(EnumNames<T>().Select(value => JsonValue.Create(value)).ToArray<JsonNode?>())
    };

    private static JsonObject NullableEnumSchema<T>(string description) where T : struct, Enum => new()
    {
        ["type"] = new JsonArray("string", "null"),
        ["enum"] = new JsonArray(EnumNames<T>().Select(value => JsonValue.Create(value)).Append(null).ToArray<JsonNode?>()),
        ["description"] = description
    };

    private static JsonObject CoordSchema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject { ["x"] = new JsonObject { ["type"] = "integer" }, ["y"] = new JsonObject { ["type"] = "integer" } },
        ["required"] = new JsonArray("x", "y"),
        ["additionalProperties"] = false
    };

    private static JsonObject CostSchema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject { ["templateId"] = new JsonObject { ["type"] = "string" }, ["quantity"] = new JsonObject { ["type"] = "integer" } },
        ["required"] = new JsonArray("templateId", "quantity"),
        ["additionalProperties"] = false
    };

    private static JsonObject EntityTemplateUpdateSchema() => new()
    {
        ["type"] = "object",
        ["description"] = "Set nullable fields to null to clear them where listed.",
        ["properties"] = new JsonObject
        {
            ["name"] = new JsonObject { ["type"] = "string" },
            ["inventoryWidth"] = new JsonObject { ["type"] = "integer" },
            ["inventoryHeight"] = new JsonObject { ["type"] = "integer" },
            ["bulk"] = new JsonObject { ["type"] = "integer" },
            ["aperture"] = new JsonObject { ["type"] = "integer" },
            ["material"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("metal", "wood", "stone"), ["description"] = "Presentation-only material; omit or use clearMaterial=true for undefined/debug fallback." },
            ["enterPolicy"] = NullableEnumSchema<EntityEnterPolicy>("Pass null with clearEnterPolicy=true to clear authored enter policy."),
            ["exitPolicy"] = NullableEnumSchema<EntityExitPolicy>("Pass null with clearExitPolicy=true to clear authored exit policy."),
            ["topologyPolicy"] = EnumStringSchema<EntityTopologyPolicy>(),
            ["clearEnterPolicy"] = new JsonObject { ["type"] = "boolean" },
            ["clearExitPolicy"] = new JsonObject { ["type"] = "boolean" },
            ["clearMaterial"] = new JsonObject { ["type"] = "boolean" },
            ["presentationId"] = new JsonObject { ["type"] = "string" },
            ["paletteId"] = new JsonObject { ["type"] = "string" },
            ["glyph"] = new JsonObject { ["type"] = "string", ["minLength"] = 1, ["maxLength"] = 1 },
            ["color"] = EnumStringSchema<PresentationColor>()
        },
        ["additionalProperties"] = false
    };

    private static JsonObject ScenarioSchema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["scenarioId"] = new JsonObject { ["type"] = "string" },
            ["name"] = new JsonObject { ["type"] = "string" },
            ["scenarioRootEntityTemplateId"] = new JsonObject { ["type"] = "string" },
            ["playerEntityTemplateId"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
            ["playerEntityId"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
            ["playerStart"] = CoordSchema(),
            ["playerControls"] = new JsonObject { ["type"] = "object", ["additionalProperties"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" } } }
        },
        ["required"] = new JsonArray("scenarioId", "name", "scenarioRootEntityTemplateId"),
        ["additionalProperties"] = false
    };

    private static JsonObject ScenarioRunOptionsSchema() => new()
    {
        ["type"] = "object",
        ["additionalProperties"] = false,
        ["properties"] = new JsonObject
        {
            ["ignorePlayerChoiceControl"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "When true, PlayerChoice-controlled actors run their authored/default plans automatically for headless debugging."
            },
            ["traceActorFilter"] = new JsonObject
            {
                ["type"] = new JsonArray("string", "null"),
                ["description"] = "Optional case-insensitive actor id/name substring used when includeAllTraces is false."
            },
            ["includeAllTraces"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "Default true. Set false with traceActorFilter to return only matching actor turn traces."
            }
        }
    };
}
