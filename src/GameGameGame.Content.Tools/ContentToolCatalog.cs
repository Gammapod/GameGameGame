using System.Text.Json.Nodes;

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
        ContentToolNames.SetBehaviorStepPlanId,
        ContentToolNames.SetBehaviorStepDirectionMode,
        ContentToolNames.ListActionSteps,
        ContentToolNames.PreviewActionPlan,
        ContentToolNames.ListScenarios,
        ContentToolNames.GetScenario,
        ContentToolNames.UpsertScenario,
        ContentToolNames.MaterializeScenario,
        ContentToolNames.RunScenarioById,
        ContentToolNames.RunScenarioPlayerLogById,
        ContentToolNames.PreviewAndRunScenarioById
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
        ContentToolNames.SetBehaviorStepPlanId => "Set or clear a referenced Action Plan ID on an apply-plan Action Step.",
        ContentToolNames.SetBehaviorStepDirectionMode => "Set or clear a canonical Move Action Step directionMode.",
        ContentToolNames.ListActionSteps => "List stable canonical Action Step descriptors.",
        ContentToolNames.PreviewActionPlan => "Preview an action plan through ContentEditorService.",
        ContentToolNames.ListScenarios => "List persisted scenario definitions.",
        ContentToolNames.GetScenario => "Get one persisted scenario definition summary.",
        ContentToolNames.UpsertScenario => "Create or update a persisted scenario definition.",
        ContentToolNames.MaterializeScenario => "Materialize a persisted scenario by ID and report diagnostics.",
        ContentToolNames.RunScenarioById => "Run a persisted scenario by ID and report outcomes.",
        ContentToolNames.RunScenarioPlayerLogById => "Run a persisted scenario by ID and return compact player narrative projection message IDs/args without debug traces/final state/inventory summaries.",
        ContentToolNames.PreviewAndRunScenarioById => "Return validation, previews, materialization, and scenario run report for one scenario.",
        _ => "GameGameGame content editor tool."
    };

    public static JsonObject InputSchema(string name)
    {
        var properties = new JsonObject();
        var required = new JsonArray();
        void AddString(string property, bool isRequired = true)
        {
            properties[property] = new JsonObject { ["type"] = "string" };
            if (isRequired) required.Add(property);
        }
        void AddInteger(string property, bool isRequired = true)
        {
            properties[property] = new JsonObject { ["type"] = "integer" };
            if (isRequired) required.Add(property);
        }
        void AddObject(string property, bool isRequired = true)
        {
            properties[property] = new JsonObject { ["type"] = "object", ["additionalProperties"] = true };
            if (isRequired) required.Add(property);
        }
        void AddArray(string property, bool isRequired = true)
        {
            properties[property] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" } };
            if (isRequired) required.Add(property);
        }

        if (name is not ContentToolNames.CreateNew)
        {
            if (name is ContentToolNames.OpenFile)
            {
                AddString("path");
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
            case ContentToolNames.UpdateEntityTemplate: AddString("entityTemplateId"); AddObject("update"); break;
            case ContentToolNames.PlaceCarriedEntity: AddString("parentTemplateId"); AddString("carriedTemplateId"); AddObject("coord"); break;
            case ContentToolNames.GetActionPlan or ContentToolNames.PreviewActionPlan: AddString("actionPlanTemplateId"); break;
            case ContentToolNames.SetActionPlanBehavior: AddString("actionPlanTemplateId"); AddArray("steps"); break;
            case ContentToolNames.AddActionPlanBehaviorStep: AddString("actionPlanTemplateId"); AddString("kind"); break;
            case ContentToolNames.MoveActionPlanBehaviorStep: AddString("actionPlanTemplateId"); AddInteger("fromIndex"); AddInteger("toIndex"); break;
            case ContentToolNames.RemoveActionPlanBehaviorStep: AddString("actionPlanTemplateId"); AddInteger("stepIndex"); break;
            case ContentToolNames.SetBehaviorStepTargetLabel: AddString("actionPlanTemplateId"); AddInteger("stepIndex"); AddString("targetLabel", isRequired: false); break;
            case ContentToolNames.SetBehaviorStepTargetSlot: AddString("actionPlanTemplateId"); AddInteger("stepIndex"); AddInteger("targetSlot", isRequired: false); break;
            case ContentToolNames.SetBehaviorStepPlanId: AddString("actionPlanTemplateId"); AddInteger("stepIndex"); AddString("planId", isRequired: false); break;
            case ContentToolNames.SetBehaviorStepDirectionMode: AddString("actionPlanTemplateId"); AddInteger("stepIndex"); AddString("directionMode", isRequired: false); break;
            case ContentToolNames.GetScenario or ContentToolNames.MaterializeScenario: AddString("scenarioId"); break;
            case ContentToolNames.UpsertScenario: AddObject("scenario"); break;
            case ContentToolNames.RunScenarioById or ContentToolNames.PreviewAndRunScenarioById or ContentToolNames.RunScenarioPlayerLogById: AddString("scenarioId"); AddInteger("turnCount"); if (name is ContentToolNames.RunScenarioPlayerLogById) AddString("observerEntityId", isRequired: false); break;
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required,
            ["additionalProperties"] = false
        };
    }
}
