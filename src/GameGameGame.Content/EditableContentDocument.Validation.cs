using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed partial class EditableContentDocument
{
    public ContentValidationResult ValidateCanonicalAuthoring()
    {
        var diagnostics = SourceYaml is null
            ? new List<ContentDiagnostic>()
            : StrictYamlPropertyValidator.ValidateContentDocument(SourceYaml).ToList();

        foreach (var (templateId, template) in EntityTemplates)
        {
            if (template.DefaultPlanVariables is null)
            {
                continue;
            }

            foreach (var variableName in template.DefaultPlanVariables.Keys)
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    ContentDiagnosticCode.ArbitraryPlanVariableField,
                    $"Entity template {templateId} declares arbitrary default plan variable {variableName}.",
                    entityTemplateId: new EntityTemplateId(templateId),
                    variableName: variableName));
            }
        }

        foreach (var (planId, plan) in ActionPlans)
        {
            AddActionPlanShapeDiagnostics(diagnostics, planId, plan);
            var steps = plan.Steps ?? [];
            for (var stepIndex = 0; stepIndex < steps.Count; stepIndex++)
            {
                var step = steps[stepIndex];
                foreach (var check in step.Checks ?? [])
                {
                    AddVariableFieldDiagnostics(diagnostics, planId, stepIndex, check.DirectionVariable, "directionVariable");
                    AddVariableFieldDiagnostics(diagnostics, planId, stepIndex, check.TargetVariable, "targetVariable");
                }

                if (step.OnSuccess is not null)
                {
                    AddEffectVariableFieldDiagnostics(diagnostics, planId, stepIndex, step.OnSuccess);
                }

                if (step.OnFailure is not null)
                {
                    AddEffectVariableFieldDiagnostics(diagnostics, planId, stepIndex, step.OnFailure);
                }
            }
        }

        foreach (var (scenarioId, scenario) in Scenarios)
        {
            AddScenarioDiagnostics(diagnostics, scenarioId, scenario);
        }

        return new ContentValidationResult(diagnostics);
    }

    private void AddScenarioDiagnostics(List<ContentDiagnostic> diagnostics, string scenarioId, ScenarioDefinitionDto scenario)
    {
        if (string.IsNullOrWhiteSpace(scenario.ScenarioRootEntityTemplateId) || !EntityTemplates.ContainsKey(scenario.ScenarioRootEntityTemplateId))
        {
            diagnostics.Add(ContentDiagnostic.Error(
                ContentDiagnosticCode.InvalidScenarioDefinition,
                $"Scenario {scenarioId} references missing scenario root template {scenario.ScenarioRootEntityTemplateId}.",
                entityTemplateId: string.IsNullOrWhiteSpace(scenario.ScenarioRootEntityTemplateId) ? null : new EntityTemplateId(scenario.ScenarioRootEntityTemplateId)));
        }

        if (!string.IsNullOrWhiteSpace(scenario.PlayerEntityTemplateId) && !EntityTemplates.ContainsKey(scenario.PlayerEntityTemplateId))
        {
            diagnostics.Add(ContentDiagnostic.Error(
                ContentDiagnosticCode.InvalidScenarioDefinition,
                $"Scenario {scenarioId} references missing player template {scenario.PlayerEntityTemplateId}.",
                entityTemplateId: string.IsNullOrWhiteSpace(scenario.PlayerEntityTemplateId) ? null : new EntityTemplateId(scenario.PlayerEntityTemplateId)));
        }

        if (scenario.ScenarioRootEntityTemplateId is not null && EntityTemplates.TryGetValue(scenario.ScenarioRootEntityTemplateId, out var root))
        {
            if (root.InventoryWidth <= 0 || root.InventoryHeight <= 0)
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidScenarioDefinition,
                    $"Scenario {scenarioId} root template {scenario.ScenarioRootEntityTemplateId} has no usable inventory/play plane.",
                    entityTemplateId: new EntityTemplateId(scenario.ScenarioRootEntityTemplateId)));
            }
            else if (scenario.PlayerStart is { } playerStart)
            {
                var start = ToCoord(playerStart);
                if (start.X < 0 || start.Y < 0 || start.X >= root.InventoryWidth || start.Y >= root.InventoryHeight)
                {
                    diagnostics.Add(ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidScenarioDefinition,
                        $"Scenario {scenarioId} player start {start.X},{start.Y} is outside scenario root bounds {root.InventoryWidth}x{root.InventoryHeight}.",
                        entityTemplateId: new EntityTemplateId(scenario.ScenarioRootEntityTemplateId),
                        coord: start));
                }
                else if ((root.CarriedEntities ?? []).FirstOrDefault(carried => carried.Coord?.X == start.X && carried.Coord.Y == start.Y) is { } occupant)
                {
                    diagnostics.Add(ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidScenarioDefinition,
                        $"Scenario {scenarioId} player start {start.X},{start.Y} is occupied by carried entity {occupant.EntityId}.",
                        entityTemplateId: new EntityTemplateId(scenario.ScenarioRootEntityTemplateId),
                        carriedEntityId: string.IsNullOrWhiteSpace(occupant.EntityId) ? null : new EntityId(occupant.EntityId),
                        coord: start));
                }
            }

            if (!string.IsNullOrWhiteSpace(scenario.PlayerEntityId)
                && (root.CarriedEntities ?? []).Any(carried => carried.EntityId == scenario.PlayerEntityId))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidScenarioDefinition,
                    $"Scenario {scenarioId} player entity ID {scenario.PlayerEntityId} conflicts with an entity already carried by scenario root {scenario.ScenarioRootEntityTemplateId}.",
                    entityTemplateId: new EntityTemplateId(scenario.ScenarioRootEntityTemplateId),
                    relatedEntityId: new EntityId(scenario.PlayerEntityId)));
            }

            AddPlayerControlDiagnostics(diagnostics, scenarioId, scenario, root);
        }
    }

    private void AddPlayerControlDiagnostics(List<ContentDiagnostic> diagnostics, string scenarioId, ScenarioDefinitionDto scenario, EntityTemplateDto root)
    {
        if (scenario.PlayerControls is null || scenario.PlayerControls.Count == 0)
        {
            return;
        }

        if (HasAuthoredPlayerController(root, new HashSet<string>(StringComparer.Ordinal)))
        {
            return;
        }

        var materializedEntityIds = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(scenario.PlayerEntityId))
        {
            materializedEntityIds.Add(scenario.PlayerEntityId);
        }

        AddCarriedEntityIds(root, materializedEntityIds, new HashSet<string>(StringComparer.Ordinal));
        var assignedEntities = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (playerId, controlledEntityIds) in scenario.PlayerControls)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidScenarioDefinition,
                    $"Scenario {scenarioId} declares a player control binding with an empty player ID."));
                continue;
            }

            if (controlledEntityIds is null || controlledEntityIds.Count == 0)
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidScenarioDefinition,
                    $"Scenario {scenarioId} player control {playerId} has no controlled entities."));
                continue;
            }

            var seenForPlayer = new HashSet<string>(StringComparer.Ordinal);

            foreach (var controlledEntityId in controlledEntityIds ?? [])
            {
                if (string.IsNullOrWhiteSpace(controlledEntityId))
                {
                    diagnostics.Add(ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidScenarioDefinition,
                        $"Scenario {scenarioId} player control {playerId} references an empty entity ID."));
                    continue;
                }

                if (!seenForPlayer.Add(controlledEntityId))
                {
                    diagnostics.Add(ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidScenarioDefinition,
                        $"Scenario {scenarioId} player control {playerId} lists entity {controlledEntityId} more than once.",
                        relatedEntityId: new EntityId(controlledEntityId)));
                    continue;
                }

                if (!materializedEntityIds.Contains(controlledEntityId))
                {
                    diagnostics.Add(ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidScenarioDefinition,
                        $"Scenario {scenarioId} player control {playerId} references missing entity {controlledEntityId}.",
                        relatedEntityId: new EntityId(controlledEntityId)));
                    continue;
                }

                if (assignedEntities.TryGetValue(controlledEntityId, out var previousPlayerId) && previousPlayerId != playerId)
                {
                    diagnostics.Add(ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidScenarioDefinition,
                        $"Scenario {scenarioId} controlled entity {controlledEntityId} is assigned to both {previousPlayerId} and {playerId}.",
                        relatedEntityId: new EntityId(controlledEntityId)));
                    continue;
                }

                assignedEntities[controlledEntityId] = playerId;
            }
        }
    }

    private void AddCarriedEntityIds(EntityTemplateDto template, HashSet<string> entityIds, HashSet<string> visitedTemplateIds)
    {
        foreach (var carried in template.CarriedEntities ?? [])
        {
            if (!string.IsNullOrWhiteSpace(carried.EntityId))
            {
                entityIds.Add(carried.EntityId);
            }

            if (!string.IsNullOrWhiteSpace(carried.TemplateId)
                && EntityTemplates.TryGetValue(carried.TemplateId, out var carriedTemplate)
                && visitedTemplateIds.Add(carried.TemplateId))
            {
                AddCarriedEntityIds(carriedTemplate, entityIds, visitedTemplateIds);
            }
        }
    }

    private bool HasAuthoredPlayerController(EntityTemplateDto template, HashSet<string> visitedTemplateIds)
    {
        foreach (var carried in template.CarriedEntities ?? [])
        {
            if (carried.Controller == EntityController.Player)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(carried.TemplateId)
                && EntityTemplates.TryGetValue(carried.TemplateId, out var carriedTemplate)
                && visitedTemplateIds.Add(carried.TemplateId)
                && HasAuthoredPlayerController(carriedTemplate, visitedTemplateIds))
            {
                return true;
            }
        }

        return false;
    }

    private static void AddActionPlanShapeDiagnostics(List<ContentDiagnostic> diagnostics, string planId, ActionPlanDescriptorDto plan)
    {
        var descriptor = plan.ToDescriptor(planId);
        var shape = ActionPlanShapeClassifier.Classify(descriptor);
        if (shape == ActionPlanShape.InvalidMixedShape)
        {
            diagnostics.Add(ContentDiagnostic.Error(
                ContentDiagnosticCode.InvalidActionPlanShape,
                $"Action plan {planId} declares multiple behavior shapes. Use only one of behavior, primitive, or low-level steps.",
                actionPlanTemplateId: new ActionPlanTemplateId(planId),
                actionPlanId: new ActionPlanId(planId)));
        }

        if (shape == ActionPlanShape.InvalidEmptyBehaviorChain)
        {
            diagnostics.Add(ContentDiagnostic.Error(
                ContentDiagnosticCode.InvalidActionPlanShape,
                $"Action plan {planId} declares an empty behavior chain. Omit behavior or add at least one Action Step.",
                actionPlanTemplateId: new ActionPlanTemplateId(planId),
                actionPlanId: new ActionPlanId(planId)));
        }
    }

    private static void AddEffectVariableFieldDiagnostics(List<ContentDiagnostic> diagnostics, string planId, int stepIndex, PlanEffectDescriptorDto effect)
    {
        AddVariableFieldDiagnostics(diagnostics, planId, stepIndex, effect.DirectionVariable, "directionVariable");
        AddVariableFieldDiagnostics(diagnostics, planId, stepIndex, effect.TargetVariable, "targetVariable");
        AddVariableFieldDiagnostics(diagnostics, planId, stepIndex, effect.VariableName, "variableName");
    }

    private static void AddVariableFieldDiagnostics(List<ContentDiagnostic> diagnostics, string planId, int stepIndex, string? variableName, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(variableName))
        {
            return;
        }

        diagnostics.Add(ContentDiagnostic.Error(
            ContentDiagnosticCode.ArbitraryPlanVariableField,
            $"Action plan {planId} step {stepIndex} declares arbitrary {fieldName} {variableName}.",
            actionPlanTemplateId: new ActionPlanTemplateId(planId),
            actionPlanId: new ActionPlanId(planId),
            stepIndex: stepIndex,
            variableName: variableName));
    }
}
