using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed class PrototypeContentRegistry(
    IReadOnlyDictionary<EntityTemplateId, EntityTemplate> entityTemplates,
    IReadOnlyDictionary<ActionPlanTemplateId, ActionPlanDescriptor> actionPlanTemplates,
    IReadOnlyDictionary<EntityTemplateId, EntityPresentation> presentations)
{
    private readonly Dictionary<EntityId, EntityTemplateId> _entityTemplateAssignments = [];

    public IReadOnlyDictionary<EntityTemplateId, EntityTemplate> EntityTemplates => entityTemplates;

    public IReadOnlyDictionary<ActionPlanTemplateId, ActionPlanDescriptor> ActionPlanDescriptors => actionPlanTemplates;

    public IReadOnlyDictionary<EntityTemplateId, EntityPresentation> Presentations => presentations;

    public EntityTemplate GetEntityTemplate(EntityTemplateId id) => entityTemplates[id];

    public EntityPresentation GetPresentation(EntityTemplateId id) => presentations[id];

    public EntityPresentation GetPresentationForEntity(EntityId entityId) =>
        presentations[GetTemplateIdForEntity(entityId)];

    public EntityTemplateId GetTemplateIdForEntity(EntityId entityId) =>
        _entityTemplateAssignments.TryGetValue(entityId, out var templateId)
            ? templateId
            : throw new InvalidOperationException($"No template assignment is registered for entity {entityId}.");

    public bool TryGetTemplateIdForEntity(EntityId entityId, out EntityTemplateId templateId) =>
        _entityTemplateAssignments.TryGetValue(entityId, out templateId);

    public ActionPlanDescriptor GetActionPlanDescriptor(ActionPlanTemplateId id) => actionPlanTemplates[id];

    public IEntityActionPlan CreateActionPlan(ActionPlanTemplateId id) =>
        CreateActionPlan(id, new Dictionary<string, PlanValueDescriptor>(), actionStateDefaults: null);

    public IEntityActionPlan CreateActionPlan(ActionPlanTemplateId id, IReadOnlyDictionary<string, PlanValueDescriptor> variables)
        => CreateActionPlan(id, variables, actionStateDefaults: null);

    public IEntityActionPlan CreateActionPlan(
        ActionPlanTemplateId id,
        IReadOnlyDictionary<string, PlanValueDescriptor> variables,
        ActorActionStateDefaults? actionStateDefaults)
    {
        var context = new ActionPlanContext();

        foreach (var (name, value) in variables)
        {
            context.Set(name, value.Materialize());
        }

        ApplyActionStateDefaults(context, actionStateDefaults);

        return new InterpretedEntityActionPlan(
            GetActionPlanDescriptor(id).Materialize(),
            context,
            BuildPlanRegistry());
    }

    public PrototypeContentRegistry WithEntityTemplate(EntityTemplateId id, EntityTemplate template)
    {
        var templates = new Dictionary<EntityTemplateId, EntityTemplate>(entityTemplates)
        {
            [id] = template
        };

        return new PrototypeContentRegistry(templates, actionPlanTemplates, presentations);
    }

    public PrototypeContentRegistry WithPresentation(EntityTemplateId id, EntityPresentation presentation)
    {
        var updated = new Dictionary<EntityTemplateId, EntityPresentation>(presentations)
        {
            [id] = presentation
        };

        return new PrototypeContentRegistry(entityTemplates, actionPlanTemplates, updated);
    }

    public PrototypeContentRegistry WithActionPlanDescriptor(ActionPlanTemplateId id, ActionPlanDescriptor descriptor)
    {
        var updated = new Dictionary<ActionPlanTemplateId, ActionPlanDescriptor>(actionPlanTemplates)
        {
            [id] = descriptor
        };

        return new PrototypeContentRegistry(entityTemplates, updated, presentations);
    }

    public ContentValidationResult Validate()
    {
        var errors = new List<string>();
        var diagnostics = new List<ContentDiagnostic>();
        ValidateEntityTemplates(errors, diagnostics);
        ValidateActionPlans(errors, diagnostics);

        diagnostics.AddRange(errors.Select(error => ContentDiagnostic.Error(ContentDiagnosticCode.General, error)));
        return new ContentValidationResult(diagnostics);
    }

    public EntitySpawnResult SpawnEntity(WorldState world, EntityTemplateId templateId, EntitySpawnOptions options)
    {
        var template = GetEntityTemplate(templateId);

        var result = SpawnEntity(world, template, options);
        RegisterTemplateAssignment(result.EntityId, templateId);

        return result;
    }

    private EntitySpawnResult SpawnEntity(WorldState world, EntityTemplate template, EntitySpawnOptions options)
    {
        template = options.ModifyTemplate?.Invoke(template) ?? template;

        var defaultActionPlanId = options.ActionPlanOverrideId ?? template.DefaultActionPlanId;

        var variables = MergePlanVariables(template.DefaultPlanVariables, options.PlanVariableOverrides);
        var actionStateDefaults = MergeActionStateDefaults(template.ActionStateDefaults, options.ActionStateOverrides);

        var carriedEntities = template.CarriedEntities;
        var parentResult = PrototypeContent.SpawnEntity(
            world,
            template with { CarriedEntities = null },
            options with { ModifyTemplate = null });
        ApplyActionStateDefaults(world, parentResult.EntityId, actionStateDefaults);
        var actionPlans = new Dictionary<EntityId, IEntityActionPlan>(parentResult.ActionPlans);
        IEntityActionPlan? actionPlan = null;

        if (defaultActionPlanId is { } actionPlanTemplateId)
        {
            actionPlan = CreateActionPlan(actionPlanTemplateId, variables, actionStateDefaults);
            actionPlans[parentResult.EntityId] = actionPlan;
        }

        if (carriedEntities is null || carriedEntities.Count == 0)
        {
            return new EntitySpawnResult(parentResult.EntityId, actionPlan, actionPlans);
        }

        if (world.GetInventoryPlaneId(options.EntityId) is not { } inventoryPlaneId)
        {
            throw new InvalidOperationException($"Cannot place carried entities for {options.EntityId}: template has no usable inventory.");
        }

        foreach (var carried in carriedEntities)
        {
            var carriedOptions = new EntitySpawnOptions(
                carried.EntityId,
                new PlaneCoord(inventoryPlaneId, carried.Coord));
            var carriedResult = carried.TemplateId is { } templateId
                ? SpawnEntity(world, templateId, carriedOptions)
                : carried.Template is { } carriedTemplate
                    ? SpawnEntity(world, carriedTemplate, carriedOptions)
                    : throw new InvalidOperationException($"Carried entity {carried.EntityId} has no template or template ID.");

            foreach (var (entityId, carriedActionPlan) in carriedResult.ActionPlans)
            {
                actionPlans[entityId] = carriedActionPlan;
            }
        }

        return new EntitySpawnResult(parentResult.EntityId, actionPlan, actionPlans);
    }

    private void RegisterTemplateAssignment(EntityId entityId, EntityTemplateId templateId)
    {
        _entityTemplateAssignments[entityId] = templateId;
    }

    private IReadOnlyDictionary<ActionPlanId, ActionPlanDefinition> BuildPlanRegistry() =>
        actionPlanTemplates.Values.ToDictionary(plan => plan.Id, plan => plan.Materialize());

    private void ValidateEntityTemplates(List<string> errors, List<ContentDiagnostic> diagnostics)
    {
        foreach (var (templateId, template) in entityTemplates)
        {
            if (!presentations.ContainsKey(templateId))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingPresentation,
                    $"Entity template {templateId} ({template.Name}) has no presentation.",
                    entityTemplateId: templateId));
            }

            ValidateActionPlanTemplateReference(errors, diagnostics, templateId, template, template.DefaultActionPlanId, nameof(template.DefaultActionPlanId));
            ValidateTargetingRules(diagnostics, templateId, template);

            if (template.DefaultPlanVariables is not null)
            {
                foreach (var (name, value) in template.DefaultPlanVariables)
                {
                    TryValidate(errors, $"Entity template {templateId} ({template.Name}) default variable {name}", () => value.Materialize());
                }
            }

            if (template.CarriedEntities is null)
            {
                continue;
            }

            ValidateCarriedEntityLayout(errors, diagnostics, templateId, template);

            foreach (var carried in template.CarriedEntities)
            {
                if (carried.TemplateId is { } carriedTemplateId && !entityTemplates.ContainsKey(carriedTemplateId))
                {
                    errors.Add($"Entity template {templateId} ({template.Name}) carries {carried.EntityId} with missing template {carriedTemplateId}.");
                }
            }
        }
    }

    private void ValidateTargetingRules(
        List<ContentDiagnostic> diagnostics,
        EntityTemplateId templateId,
        EntityTemplate template)
    {
        if (template.TargetingRules is null || template.TargetingRules.Count == 0)
        {
            return;
        }

        var slots = new HashSet<int>();
        var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in template.TargetingRules)
        {
            if (rule.Slot <= 0)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidTargetingRule,
                    $"Entity template {templateId} ({template.Name}) targeting rule slot must be greater than zero; found {rule.Slot}.",
                    entityTemplateId: templateId));
            }

            if (!slots.Add(rule.Slot))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidTargetingRule,
                    $"Entity template {templateId} ({template.Name}) has duplicate targeting rule slot {rule.Slot}.",
                    entityTemplateId: templateId));
            }

            if (rule.Label is { } label)
            {
                if (string.IsNullOrWhiteSpace(label))
                {
                    AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidTargetingRule,
                        $"Entity template {templateId} ({template.Name}) targeting rule slot {rule.Slot} label must not be blank.",
                        entityTemplateId: templateId));
                }
                else if (!labels.Add(label))
                {
                    AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidTargetingRule,
                        $"Entity template {templateId} ({template.Name}) has duplicate targeting rule label {label}.",
                        entityTemplateId: templateId));
                }
            }

            if (rule.Range < 0)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidTargetingRule,
                    $"Entity template {templateId} ({template.Name}) targeting rule slot {rule.Slot} range must be zero or greater; found {rule.Range}.",
                    entityTemplateId: templateId));
            }

            if (rule.TargetTemplateId is null && rule.TargetCapabilities.Count == 0)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidTargetingRule,
                    $"Entity template {templateId} ({template.Name}) targeting rule slot {rule.Slot} must declare a target template, at least one target capability, or both.",
                    entityTemplateId: templateId));
            }

            if (rule.TargetTemplateId is { } targetTemplateId && !entityTemplates.ContainsKey(targetTemplateId))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingTargetTemplateReference,
                    $"Entity template {templateId} ({template.Name}) targeting rule slot {rule.Slot} references missing target template {targetTemplateId}.",
                    entityTemplateId: templateId));
            }

            foreach (var capability in rule.TargetCapabilities)
            {
                if (!EntityInteractionAffordanceService.IsSupportedTargetCapability(capability))
                {
                    AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidTargetingRule,
                        $"Entity template {templateId} ({template.Name}) targeting rule slot {rule.Slot} references unsupported target capability {capability}.",
                        entityTemplateId: templateId));
                    continue;
                }

                if (!TemplatePlanUsesTargetCapability(template, rule, capability))
                {
                    AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidTargetingRule,
                        $"Entity template {templateId} ({template.Name}) targeting rule slot {rule.Slot} capability {capability} is not consumed by its default action plan with the same target label/slot.",
                        entityTemplateId: templateId));
                }
            }
        }
    }

    private bool TemplatePlanUsesTargetCapability(
        EntityTemplate template,
        EntityTargetingRule rule,
        ActionPlanBehaviorStepKind capability)
    {
        if (template.DefaultActionPlanId is not { } planId
            || !actionPlanTemplates.TryGetValue(planId, out var plan)
            || plan.Behavior?.Steps is not { Count: > 0 } steps)
        {
            return false;
        }

        return steps.Any(step =>
            step.Kind == capability
            && TargetReferenceMatchesRule(step, rule));
    }

    private static bool TargetReferenceMatchesRule(ActionPlanBehaviorStepDescriptor step, EntityTargetingRule rule)
    {
        if (!string.IsNullOrWhiteSpace(rule.Label)
            && string.Equals(step.TargetLabel, rule.Label, StringComparison.Ordinal))
        {
            return true;
        }

        return (step.TargetSlot ?? 1) == rule.Slot && string.IsNullOrWhiteSpace(step.TargetLabel);
    }

    private static void ValidateCarriedEntityLayout(
        List<string> errors,
        List<ContentDiagnostic> diagnostics,
        EntityTemplateId templateId,
        EntityTemplate template)
    {
        if (template.CarriedEntities is null || template.CarriedEntities.Count == 0)
        {
            return;
        }

        var entityIds = new HashSet<EntityId>();
        var occupiedCoords = new Dictionary<GridCoord, EntityId>();
        var hasUsableInventory = template.InventoryWidth > 0 && template.InventoryHeight > 0;

        foreach (var carried in template.CarriedEntities)
        {
            if (!entityIds.Add(carried.EntityId))
            {
                var message = $"Entity template {templateId} ({template.Name}) has duplicate carried entity ID {carried.EntityId}.";
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.DuplicateCarriedEntityId,
                    message,
                    entityTemplateId: templateId,
                    carriedEntityId: carried.EntityId));
            }

            if (!hasUsableInventory)
            {
                var message = $"Entity template {templateId} ({template.Name}) carries {carried.EntityId} but has no usable inventory.";
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.CarriedEntityWithoutUsableInventory,
                    message,
                    entityTemplateId: templateId,
                    carriedEntityId: carried.EntityId));
                continue;
            }

            if (carried.Coord.X < 0
                || carried.Coord.Y < 0
                || carried.Coord.X >= template.InventoryWidth
                || carried.Coord.Y >= template.InventoryHeight)
            {
                var message = $"Entity template {templateId} ({template.Name}) carries {carried.EntityId} at {carried.Coord}, outside inventory bounds {template.InventoryWidth}x{template.InventoryHeight}.";
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InventoryOutOfBounds,
                    message,
                    entityTemplateId: templateId,
                    carriedEntityId: carried.EntityId,
                    coord: carried.Coord));
                continue;
            }

            if (occupiedCoords.TryGetValue(carried.Coord, out var existingEntityId))
            {
                var message = $"Entity template {templateId} ({template.Name}) carried entities {existingEntityId} and {carried.EntityId} overlap at {carried.Coord}.";
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InventoryOverlap,
                    message,
                    entityTemplateId: templateId,
                    carriedEntityId: carried.EntityId,
                    relatedEntityId: existingEntityId,
                    coord: carried.Coord));
                continue;
            }

            occupiedCoords[carried.Coord] = carried.EntityId;
        }
    }

    private void ValidateActionPlanTemplateReference(
        List<string> errors,
        List<ContentDiagnostic> diagnostics,
        EntityTemplateId templateId,
        EntityTemplate template,
        ActionPlanTemplateId? actionPlanTemplateId,
        string fieldName)
    {
        if (actionPlanTemplateId is { } id && !actionPlanTemplates.ContainsKey(id))
        {
            AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                ContentDiagnosticCode.MissingActionPlanReference,
                $"Entity template {templateId} ({template.Name}) references missing {fieldName} {id}.",
                entityTemplateId: templateId,
                actionPlanTemplateId: id));
        }
    }

    private void ValidateActionPlans(List<string> errors, List<ContentDiagnostic> diagnostics)
    {
        var planIds = actionPlanTemplates.Values.Select(plan => plan.Id).ToHashSet();

        foreach (var (templateId, descriptor) in actionPlanTemplates)
        {
            TryValidate(errors, $"Action plan template {templateId} ({descriptor.Id})", () => descriptor.Materialize());

            ValidateActionPlanShape(diagnostics, templateId, descriptor);
            ValidateBehaviorTargetSlots(diagnostics, templateId, descriptor);
            ValidateBehaviorPlanReferences(diagnostics, templateId, descriptor);
            ValidatePrimitiveFallback(diagnostics, templateId, descriptor);

            foreach (var step in descriptor.Steps)
            {
                ValidateCalledPlan(errors, templateId, descriptor, step, step.OnSuccess);
                ValidateCalledPlan(errors, templateId, descriptor, step, step.OnFailure);
                ValidateMovementEffectDescriptor(diagnostics, templateId, descriptor, step, step.OnSuccess);
                ValidateMovementEffectDescriptor(diagnostics, templateId, descriptor, step, step.OnFailure);
            }
        }

        void ValidateCalledPlan(
            List<string> validationErrors,
            ActionPlanTemplateId actionPlanTemplateId,
            ActionPlanDescriptor descriptor,
            ActionPlanStepDescriptor step,
            PlanEffectDescriptor? effect)
        {
            if (effect?.Kind == PlanEffectKind.CallPlan && effect.PlanId is { } planId && !planIds.Contains(planId))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingCalledPlan,
                    $"Action plan {descriptor.Id} step {step.Label} calls missing plan {planId}.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    referencedActionPlanId: planId,
                    stepIndex: StepIndex(descriptor, step)));
            }
        }

        ValidateTemplateActionPlanVariables(errors, diagnostics);

        static void ValidateActionPlanShape(
            List<ContentDiagnostic> validationDiagnostics,
            ActionPlanTemplateId actionPlanTemplateId,
            ActionPlanDescriptor descriptor)
        {
            var shape = ActionPlanShapeClassifier.Classify(descriptor);
            if (shape == ActionPlanShape.InvalidMixedShape)
            {
                AddDiagnostic(validationDiagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidActionPlanShape,
                    $"Action plan {descriptor.Id} declares multiple behavior shapes. Use only one of behavior, primitive, or low-level steps.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id));
            }

            if (shape == ActionPlanShape.InvalidEmptyBehaviorChain)
            {
                AddDiagnostic(validationDiagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidActionPlanShape,
                    $"Action plan {descriptor.Id} declares an empty behavior chain. Omit behavior or add at least one Action Step.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id));
            }
        }

        static void ValidateBehaviorTargetSlots(
            List<ContentDiagnostic> validationDiagnostics,
            ActionPlanTemplateId actionPlanTemplateId,
            ActionPlanDescriptor descriptor)
        {
            if (descriptor.Behavior is not { } behavior)
            {
                return;
            }

            for (var index = 0; index < behavior.Steps.Count; index++)
            {
                var step = behavior.Steps[index];
                if (step.TargetSlot is <= 0)
                {
                    AddDiagnostic(validationDiagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidActionStepTargetSlot,
                        $"Action plan {descriptor.Id} action step {step.Kind} targetSlot must be greater than zero; found {step.TargetSlot}.",
                        actionPlanTemplateId: actionPlanTemplateId,
                        actionPlanId: descriptor.Id,
                        stepIndex: index));
                }

                if (step.TargetSlot is not null && !string.IsNullOrWhiteSpace(step.TargetLabel))
                {
                    AddDiagnostic(validationDiagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidActionStepTargetReference,
                        $"Action plan {descriptor.Id} action step {step.Kind} must use either targetLabel or targetSlot, not both.",
                        actionPlanTemplateId: actionPlanTemplateId,
                        actionPlanId: descriptor.Id,
                        stepIndex: index));
                }

                if (step.TargetLabel is { } label && string.IsNullOrWhiteSpace(label))
                {
                    AddDiagnostic(validationDiagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidActionStepTargetReference,
                        $"Action plan {descriptor.Id} action step {step.Kind} targetLabel must not be blank.",
                        actionPlanTemplateId: actionPlanTemplateId,
                        actionPlanId: descriptor.Id,
                        stepIndex: index));
                }
            }
        }

        void ValidateBehaviorPlanReferences(
            List<ContentDiagnostic> validationDiagnostics,
            ActionPlanTemplateId actionPlanTemplateId,
            ActionPlanDescriptor descriptor)
        {
            if (descriptor.Behavior is not { } behavior)
            {
                return;
            }

            for (var index = 0; index < behavior.Steps.Count; index++)
            {
                var step = behavior.Steps[index];
                if (!IsApplyPlanOverrideStep(step.Kind))
                {
                    continue;
                }

                if (step.PlanId is not { } planId)
                {
                    AddDiagnostic(validationDiagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.MissingActionPlanReference,
                        $"Action plan {descriptor.Id} action step {step.Kind} requires planId.",
                        actionPlanTemplateId: actionPlanTemplateId,
                        actionPlanId: descriptor.Id,
                        stepIndex: index));
                    continue;
                }

                if (!planIds.Contains(planId))
                {
                    AddDiagnostic(validationDiagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.MissingActionPlanReference,
                        $"Action plan {descriptor.Id} action step {step.Kind} references missing plan {planId}.",
                        actionPlanTemplateId: actionPlanTemplateId,
                        actionPlanId: descriptor.Id,
                        referencedActionPlanId: planId,
                        stepIndex: index));
                }
            }
        }

        static bool IsApplyPlanOverrideStep(ActionPlanBehaviorStepKind kind) =>
            kind is ActionPlanBehaviorStepKind.ApplyPrePlan
                or ActionPlanBehaviorStepKind.ApplyMainPlan
                or ActionPlanBehaviorStepKind.ApplyPostPlan;

        void ValidatePrimitiveFallback(
            List<ContentDiagnostic> validationDiagnostics,
            ActionPlanTemplateId actionPlanTemplateId,
            ActionPlanDescriptor descriptor)
        {
            if (descriptor.Primitive?.FallbackPlanId is { } fallbackPlanId && !planIds.Contains(fallbackPlanId))
            {
                AddDiagnostic(validationDiagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingCalledPlan,
                    $"Action plan {descriptor.Id} primitive {descriptor.Primitive.Kind} falls back to missing plan {fallbackPlanId}.",
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: descriptor.Id,
                    referencedActionPlanId: fallbackPlanId));
            }
        }
    }

    private static void ValidateMovementEffectDescriptor(
        List<ContentDiagnostic> diagnostics,
        ActionPlanTemplateId actionPlanTemplateId,
        ActionPlanDescriptor plan,
        ActionPlanStepDescriptor step,
        PlanEffectDescriptor? effect)
    {
        if (effect?.Kind is not (PlanEffectKind.Teleport or PlanEffectKind.Drop))
        {
            return;
        }

        if (effect.MovementTarget is null)
        {
            AddInvalidMovementDiagnostic(diagnostics, actionPlanTemplateId, plan, step, effect, "movementTarget is required.");
        }
        else if (GetMovementTargetError(effect.MovementTarget) is { } targetError)
        {
            AddInvalidMovementDiagnostic(diagnostics, actionPlanTemplateId, plan, step, effect, targetError);
        }

        if (effect.MovementDestination is null)
        {
            AddInvalidMovementDiagnostic(diagnostics, actionPlanTemplateId, plan, step, effect, "movementDestination is required.");
        }
        else if (GetMovementDestinationError(effect.MovementDestination) is { } destinationError)
        {
            AddInvalidMovementDiagnostic(diagnostics, actionPlanTemplateId, plan, step, effect, destinationError);
        }
    }

    private static string? GetMovementTargetError(MovementTargetDescriptor target) =>
        target.Kind switch
        {
            MovementTargetKind.Entity when target.EntityId is null => "movementTarget.entityId is required for Entity targets.",
            MovementTargetKind.CarriedInventoryCoord when target.InventoryCoord is null => "movementTarget.inventoryCoord is required for CarriedInventoryCoord targets.",
            _ => null
        };

    private static string? GetMovementDestinationError(MovementDestinationDescriptor destination) =>
        destination.Kind switch
        {
            MovementDestinationKind.PlaneCoord when destination.PlaneCoord is null => "movementDestination.planeCoord is required for PlaneCoord destinations.",
            MovementDestinationKind.InventorySlot when destination.OwnerId is null => "movementDestination.ownerId is required for InventorySlot destinations.",
            MovementDestinationKind.InventorySlot when destination.InventoryCoord is null => "movementDestination.inventoryCoord is required for InventorySlot destinations.",
            MovementDestinationKind.AdjacentToSelf when destination.Direction is null => "movementDestination.direction is required for AdjacentToSelf destinations.",
            MovementDestinationKind.AdjacentToEntity when destination.AnchorEntityId is null => "movementDestination.anchorEntityId is required for AdjacentToEntity destinations.",
            MovementDestinationKind.AdjacentToEntity when destination.Direction is null => "movementDestination.direction is required for AdjacentToEntity destinations.",
            MovementDestinationKind.AdjacentToCanonicalTarget when destination.Direction is null => "movementDestination.direction is required for AdjacentToCanonicalTarget destinations.",
            _ => null
        };

    private static void AddInvalidMovementDiagnostic(
        List<ContentDiagnostic> diagnostics,
        ActionPlanTemplateId actionPlanTemplateId,
        ActionPlanDescriptor plan,
        ActionPlanStepDescriptor step,
        PlanEffectDescriptor effect,
        string detail)
    {
        AddDiagnostic(diagnostics, ContentDiagnostic.Error(
            ContentDiagnosticCode.InvalidMovementDescriptor,
            $"Action plan {plan.Id} step {step.Label} has invalid {effect.Kind} movement descriptor: {detail}",
            actionPlanTemplateId: actionPlanTemplateId,
            actionPlanId: plan.Id,
            stepIndex: StepIndex(plan, step)));
    }

    private void ValidateTemplateActionPlanVariables(List<string> errors, List<ContentDiagnostic> diagnostics)
    {
        var plansById = actionPlanTemplates.Values.ToDictionary(plan => plan.Id);

        foreach (var (templateId, template) in entityTemplates)
        {
            if (template.DefaultActionPlanId is not { } actionPlanTemplateId
                || !actionPlanTemplates.TryGetValue(actionPlanTemplateId, out var plan))
            {
                continue;
            }

            var variables = template.DefaultPlanVariables is null
                ? new Dictionary<string, PlanValueKind>()
                : template.DefaultPlanVariables.ToDictionary(entry => entry.Key, entry => entry.Value.Kind);

            ValidatePlanVariables(
                errors,
                diagnostics,
                $"Entity template {templateId} ({template.Name}) action plan {plan.Id}",
                templateId,
                actionPlanTemplateId,
                plan,
                variables,
                plansById,
                []);

            var slots = GetInitialActionSlots(template);
            ValidatePlanSlots(
                diagnostics,
                $"Entity template {templateId} ({template.Name}) action plan {plan.Id}",
                templateId,
                actionPlanTemplateId,
                plan,
                slots,
                plansById,
                []);
        }
    }

    private static Dictionary<ActionPlanSlot, PlanValueKind> GetInitialActionSlots(EntityTemplate template)
    {
        var slots = new Dictionary<ActionPlanSlot, PlanValueKind>();

        if (template.ActionStateDefaults?.Facing is not null)
        {
            slots[ActionPlanSlot.Facing] = PlanValueKind.Direction;
        }

        if (template.ActionStateDefaults?.Target is not null)
        {
            slots[ActionPlanSlot.Target] = PlanValueKind.Entity;
        }

        if (template.TargetingRules is { Count: > 0 })
        {
            slots[ActionPlanSlot.Target] = PlanValueKind.Entity;
        }

        if (template.DefaultPlanVariables is not null)
        {
            foreach (var (name, value) in template.DefaultPlanVariables)
            {
                if (string.Equals(name, "facing", StringComparison.Ordinal) && value.Kind == PlanValueKind.Direction)
                {
                    slots[ActionPlanSlot.Facing] = PlanValueKind.Direction;
                }

                if (string.Equals(name, "target", StringComparison.Ordinal) && value.Kind == PlanValueKind.Entity)
                {
                    slots[ActionPlanSlot.Target] = PlanValueKind.Entity;
                }
            }
        }

        return slots;
    }

    private static void ValidatePlanSlots(
        List<ContentDiagnostic> diagnostics,
        string subject,
        EntityTemplateId? entityTemplateId,
        ActionPlanTemplateId? actionPlanTemplateId,
        ActionPlanDescriptor plan,
        Dictionary<ActionPlanSlot, PlanValueKind> slots,
        IReadOnlyDictionary<ActionPlanId, ActionPlanDescriptor> plansById,
        HashSet<ActionPlanId> callStack)
    {
        if (!callStack.Add(plan.Id))
        {
            return;
        }

        if (plan.Primitive is { } primitive)
        {
            ApplyDefaultableState(GetPrimitiveSlotDefaultable(primitive.Kind), slots);
            ValidatePrimitiveSlotReads(diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, primitive, GetPrimitiveSlotReads(primitive.Kind), slots);
            ApplySlotWrites(GetPrimitiveSlotWrites(primitive.Kind), slots);

            if (primitive.FallbackPlanId is { } fallbackPlanId
                && plansById.TryGetValue(fallbackPlanId, out var fallbackPlan))
            {
                ValidatePlanSlots(diagnostics, subject, entityTemplateId, actionPlanTemplateId, fallbackPlan, slots, plansById, callStack);
            }
        }

        if (plan.Behavior is { } behavior)
        {
            for (var index = 0; index < behavior.Steps.Count; index++)
            {
                var step = behavior.Steps[index];
                var metadata = ActionStepCatalog.Get(step.Kind);
                ApplyDefaultableState(metadata.DefaultableState, slots);
                ValidateBehaviorStepSlotReads(diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, step, index, metadata.RequiredState, slots);
                ApplySlotWrites(metadata.StateWrites, slots);
            }
        }

        foreach (var step in plan.Steps)
        {
            foreach (var check in step.Checks)
            {
                ValidateSlotReads(diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, step, PlanPrimitiveCatalog.GetCheck(check.Kind).SlotReads, slots);
                ApplySlotWrites(PlanPrimitiveCatalog.GetCheck(check.Kind).SlotWrites, slots);
            }

            ValidateEffectSlots(diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, step, step.OnSuccess, slots, plansById, callStack);
            ValidateEffectSlots(diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, step, step.OnFailure, slots, plansById, callStack);
        }

        callStack.Remove(plan.Id);
    }

    private static IReadOnlyList<PlanPrimitiveSlotDescriptor> GetPrimitiveSlotReads(ActionPlanPrimitiveKind kind) =>
        kind switch
        {
            ActionPlanPrimitiveKind.MoveFacing => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.Backstep => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.PickupTarget => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Target, PlanValueKind.Entity)],
            ActionPlanPrimitiveKind.TurnLeft => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.TurnRight => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.ReverseFacing => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            _ => []
        };

    private static IReadOnlyList<PlanPrimitiveSlotDescriptor> GetPrimitiveSlotWrites(ActionPlanPrimitiveKind kind) =>
        kind switch
        {
            ActionPlanPrimitiveKind.MoveFacing => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Target, PlanValueKind.Entity)],
            ActionPlanPrimitiveKind.Backstep => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Target, PlanValueKind.Entity)],
            ActionPlanPrimitiveKind.TurnLeft => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.TurnRight => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.ReverseFacing => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            _ => []
        };

    private static IReadOnlyList<PlanPrimitiveSlotDescriptor> GetPrimitiveSlotDefaultable(ActionPlanPrimitiveKind kind) =>
        kind switch
        {
            ActionPlanPrimitiveKind.MoveFacing => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.Backstep => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.PickupTarget => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Target, PlanValueKind.Entity)],
            ActionPlanPrimitiveKind.TurnLeft => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.TurnRight => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            ActionPlanPrimitiveKind.ReverseFacing => [new PlanPrimitiveSlotDescriptor(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            _ => []
        };

    private static void ValidatePrimitiveSlotReads(
        List<ContentDiagnostic> diagnostics,
        string subject,
        EntityTemplateId? entityTemplateId,
        ActionPlanTemplateId? actionPlanTemplateId,
        ActionPlanDescriptor plan,
        ActionPlanPrimitiveDescriptor primitive,
        IReadOnlyList<PlanPrimitiveSlotDescriptor> reads,
        Dictionary<ActionPlanSlot, PlanValueKind> slots)
    {
        foreach (var read in reads)
        {
            if (!slots.TryGetValue(read.Slot, out var actualKind))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingPlanSlot,
                    $"{subject} primitive {primitive.Kind} reads missing required slot {read.Slot}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    actionPlanSlot: read.Slot,
                    expectedValueKind: read.ValueKind));
                continue;
            }

            if (actualKind != read.ValueKind)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.PlanVariableTypeMismatch,
                    $"{subject} primitive {primitive.Kind} slot {read.Slot} expected {read.ValueKind} but found {actualKind}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    actionPlanSlot: read.Slot,
                    expectedValueKind: read.ValueKind,
                    actualValueKind: actualKind));
            }
        }
    }

    private static void ValidateBehaviorStepSlotReads(
        List<ContentDiagnostic> diagnostics,
        string subject,
        EntityTemplateId? entityTemplateId,
        ActionPlanTemplateId? actionPlanTemplateId,
        ActionPlanDescriptor plan,
        ActionPlanBehaviorStepDescriptor step,
        int stepIndex,
        IReadOnlyList<PlanPrimitiveSlotDescriptor> reads,
        Dictionary<ActionPlanSlot, PlanValueKind> slots)
    {
        foreach (var read in reads)
        {
            if (!slots.TryGetValue(read.Slot, out var actualKind))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingPlanSlot,
                    $"{subject} action step {step.Kind} reads missing required slot {read.Slot}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    stepIndex: stepIndex,
                    actionPlanSlot: read.Slot,
                    expectedValueKind: read.ValueKind));
                continue;
            }

            if (actualKind != read.ValueKind)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.PlanVariableTypeMismatch,
                    $"{subject} action step {step.Kind} slot {read.Slot} expected {read.ValueKind} but found {actualKind}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    stepIndex: stepIndex,
                    actionPlanSlot: read.Slot,
                    expectedValueKind: read.ValueKind,
                    actualValueKind: actualKind));
            }
        }
    }

    private static void ValidateEffectSlots(
        List<ContentDiagnostic> diagnostics,
        string subject,
        EntityTemplateId? entityTemplateId,
        ActionPlanTemplateId? actionPlanTemplateId,
        ActionPlanDescriptor plan,
        ActionPlanStepDescriptor step,
        PlanEffectDescriptor? effect,
        Dictionary<ActionPlanSlot, PlanValueKind> slots,
        IReadOnlyDictionary<ActionPlanId, ActionPlanDescriptor> plansById,
        HashSet<ActionPlanId> callStack)
    {
        if (effect is null)
        {
            return;
        }

        var fields = PlanPrimitiveCatalog.GetEffect(effect.Kind);
        ValidateSlotReads(diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, step, fields.SlotReads, slots);
        ApplySlotWrites(fields.SlotWrites, slots);

        if (effect.Kind == PlanEffectKind.CallPlan
            && effect.PlanId is { } planId
            && plansById.TryGetValue(planId, out var calledPlan))
        {
            ValidatePlanSlots(diagnostics, subject, entityTemplateId, actionPlanTemplateId, calledPlan, slots, plansById, callStack);
        }
    }

    private static void ValidateSlotReads(
        List<ContentDiagnostic> diagnostics,
        string subject,
        EntityTemplateId? entityTemplateId,
        ActionPlanTemplateId? actionPlanTemplateId,
        ActionPlanDescriptor plan,
        ActionPlanStepDescriptor step,
        IReadOnlyList<PlanPrimitiveSlotDescriptor> reads,
        Dictionary<ActionPlanSlot, PlanValueKind> slots)
    {
        foreach (var read in reads)
        {
            if (!slots.TryGetValue(read.Slot, out var actualKind))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingPlanSlot,
                    $"{subject} step {step.Label} reads missing required slot {read.Slot}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    stepIndex: StepIndex(plan, step),
                    actionPlanSlot: read.Slot,
                    expectedValueKind: read.ValueKind));
                continue;
            }

            if (actualKind != read.ValueKind)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.PlanVariableTypeMismatch,
                    $"{subject} step {step.Label} slot {read.Slot} expected {read.ValueKind} but found {actualKind}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    stepIndex: StepIndex(plan, step),
                    actionPlanSlot: read.Slot,
                    expectedValueKind: read.ValueKind,
                    actualValueKind: actualKind));
            }
        }
    }

    private static void ApplySlotWrites(
        IReadOnlyList<PlanPrimitiveSlotDescriptor> writes,
        Dictionary<ActionPlanSlot, PlanValueKind> slots)
    {
        foreach (var write in writes)
        {
            slots[write.Slot] = write.ValueKind;
        }
    }

    private static void ApplyDefaultableState(
        IReadOnlyList<PlanPrimitiveSlotDescriptor> defaults,
        Dictionary<ActionPlanSlot, PlanValueKind> slots)
    {
        foreach (var defaultable in defaults)
        {
            slots.TryAdd(defaultable.Slot, defaultable.ValueKind);
        }
    }

    private static void ValidatePlanVariables(
        List<string> errors,
        List<ContentDiagnostic> diagnostics,
        string subject,
        EntityTemplateId? entityTemplateId,
        ActionPlanTemplateId? actionPlanTemplateId,
        ActionPlanDescriptor plan,
        Dictionary<string, PlanValueKind> variables,
        IReadOnlyDictionary<ActionPlanId, ActionPlanDescriptor> plansById,
        HashSet<ActionPlanId> callStack)
    {
        if (!callStack.Add(plan.Id))
        {
            return;
        }

        foreach (var step in plan.Steps)
        {
            foreach (var check in step.Checks)
            {
                ValidatePrimitiveFields(errors, diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, step, PlanPrimitiveCatalog.GetCheck(check.Kind).Fields, check);
                ApplyPrimitiveWrites(PlanPrimitiveCatalog.GetCheck(check.Kind).Fields, check, variables);
            }

            ValidateEffectVariables(errors, diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, step, step.OnSuccess, variables, plansById, callStack);
            ValidateEffectVariables(errors, diagnostics, subject, entityTemplateId, actionPlanTemplateId, plan, step, step.OnFailure, variables, plansById, callStack);
        }

        callStack.Remove(plan.Id);

        void ValidatePrimitiveFields(
            List<string> validationErrors,
            List<ContentDiagnostic> validationDiagnostics,
            string validationSubject,
            EntityTemplateId? validationEntityTemplateId,
            ActionPlanTemplateId? validationActionPlanTemplateId,
            ActionPlanDescriptor validationPlan,
            ActionPlanStepDescriptor step,
            IReadOnlyList<PlanPrimitiveFieldDescriptor> fields,
            object descriptor)
        {
            foreach (var field in fields.Where(field => field.Kind == PlanPrimitiveFieldKind.VariableRead))
            {
                var variableName = GetVariableName(descriptor, field.Name);

                if (string.IsNullOrWhiteSpace(variableName) || field.ValueKind is not { } expectedKind)
                {
                    continue;
                }

                if (!variables.TryGetValue(variableName, out var actualKind))
                {
                    AddDiagnostic(validationDiagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.MissingPlanVariable,
                        $"{validationSubject} step {step.Label} reads missing required variable {variableName}.",
                        entityTemplateId: validationEntityTemplateId,
                        actionPlanTemplateId: validationActionPlanTemplateId,
                        actionPlanId: validationPlan.Id,
                        stepIndex: StepIndex(validationPlan, step),
                        variableName: variableName,
                        expectedValueKind: expectedKind));
                    continue;
                }

                if (actualKind != expectedKind)
                {
                    AddDiagnostic(validationDiagnostics, ContentDiagnostic.Error(
                        ContentDiagnosticCode.PlanVariableTypeMismatch,
                        $"{validationSubject} step {step.Label} variable {variableName} expected {expectedKind} but found {actualKind}.",
                        entityTemplateId: validationEntityTemplateId,
                        actionPlanTemplateId: validationActionPlanTemplateId,
                        actionPlanId: validationPlan.Id,
                        stepIndex: StepIndex(validationPlan, step),
                        variableName: variableName,
                        expectedValueKind: expectedKind,
                        actualValueKind: actualKind));
                }
            }
        }

        void ApplyPrimitiveWrites(
            IReadOnlyList<PlanPrimitiveFieldDescriptor> fields,
            object descriptor,
            Dictionary<string, PlanValueKind> knownVariables)
        {
            foreach (var field in fields.Where(field => field.Kind == PlanPrimitiveFieldKind.VariableWrite))
            {
                var variableName = GetVariableName(descriptor, field.Name);

                if (!string.IsNullOrWhiteSpace(variableName) && field.ValueKind is { } valueKind)
                {
                    knownVariables[variableName] = valueKind;
                }
            }
        }
    }

    private static void ValidateEffectVariables(
        List<string> errors,
        List<ContentDiagnostic> diagnostics,
        string subject,
        EntityTemplateId? entityTemplateId,
        ActionPlanTemplateId? actionPlanTemplateId,
        ActionPlanDescriptor plan,
        ActionPlanStepDescriptor step,
        PlanEffectDescriptor? effect,
        Dictionary<string, PlanValueKind> variables,
        IReadOnlyDictionary<ActionPlanId, ActionPlanDescriptor> plansById,
        HashSet<ActionPlanId> callStack)
    {
        if (effect is null)
        {
            return;
        }

        var fields = PlanPrimitiveCatalog.GetEffect(effect.Kind).Fields;

        foreach (var field in fields.Where(field => field.Kind == PlanPrimitiveFieldKind.VariableRead))
        {
            var variableName = GetVariableName(effect, field.Name);

            if (string.IsNullOrWhiteSpace(variableName) || field.ValueKind is not { } expectedKind)
            {
                continue;
            }

            if (!variables.TryGetValue(variableName, out var actualKind))
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.MissingPlanVariable,
                    $"{subject} step {step.Label} reads missing required variable {variableName}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    stepIndex: StepIndex(plan, step),
                    variableName: variableName,
                    expectedValueKind: expectedKind));
                continue;
            }

            if (actualKind != expectedKind)
            {
                AddDiagnostic(diagnostics, ContentDiagnostic.Error(
                    ContentDiagnosticCode.PlanVariableTypeMismatch,
                    $"{subject} step {step.Label} variable {variableName} expected {expectedKind} but found {actualKind}.",
                    entityTemplateId: entityTemplateId,
                    actionPlanTemplateId: actionPlanTemplateId,
                    actionPlanId: plan.Id,
                    stepIndex: StepIndex(plan, step),
                    variableName: variableName,
                    expectedValueKind: expectedKind,
                    actualValueKind: actualKind));
            }
        }

        foreach (var field in fields.Where(field => field.Kind == PlanPrimitiveFieldKind.VariableWrite))
        {
            var variableName = GetVariableName(effect, field.Name);

            if (string.IsNullOrWhiteSpace(variableName))
            {
                continue;
            }

            if (field.ValueKind is { } valueKind)
            {
                variables[variableName] = valueKind;
            }
            else if (effect.Kind == PlanEffectKind.SetVariable && effect.Value is not null)
            {
                variables[variableName] = GetPlanValueKind(effect.Value);
            }
        }

        if (effect.Kind == PlanEffectKind.CallPlan
            && effect.PlanId is { } planId
            && plansById.TryGetValue(planId, out var calledPlan))
        {
            ValidatePlanVariables(errors, diagnostics, subject, entityTemplateId, actionPlanTemplateId, calledPlan, variables, plansById, callStack);
        }
    }

    private static int StepIndex(ActionPlanDescriptor plan, ActionPlanStepDescriptor step)
    {
        for (var index = 0; index < plan.Steps.Count; index++)
        {
            if (ReferenceEquals(plan.Steps[index], step) || plan.Steps[index] == step)
            {
                return index;
            }
        }

        return -1;
    }

    private static void AddDiagnostic(List<ContentDiagnostic> diagnostics, ContentDiagnostic diagnostic)
    {
        if (!diagnostics.Contains(diagnostic))
        {
            diagnostics.Add(diagnostic);
        }
    }

    private static string? GetVariableName(object descriptor, string fieldName) =>
        descriptor switch
        {
            PlanCheckDescriptor check => fieldName switch
            {
                "directionVariable" => check.DirectionVariable,
                "targetVariable" => check.TargetVariable,
                _ => null
            },
            PlanEffectDescriptor effect => fieldName switch
            {
                "directionVariable" => effect.DirectionVariable,
                "targetVariable" => effect.TargetVariable,
                "variableName" => effect.VariableName,
                _ => null
            },
            _ => null
        };

    private static PlanValueKind GetPlanValueKind(PlanValue value) =>
        value switch
        {
            DirectionPlanValue => PlanValueKind.Direction,
            EntityPlanValue => PlanValueKind.Entity,
            CoordPlanValue => PlanValueKind.Coord,
            IntPlanValue => PlanValueKind.Int,
            _ => throw new InvalidOperationException($"Unsupported plan value type {value.GetType().Name}.")
        };

    private static void TryValidate(List<string> errors, string subject, Action materialize)
    {
        try
        {
            materialize();
        }
        catch (Exception ex)
        {
            errors.Add($"{subject} is invalid: {ex.Message}");
        }
    }

    private static IReadOnlyDictionary<string, PlanValueDescriptor> MergePlanVariables(
        IReadOnlyDictionary<string, PlanValueDescriptor>? defaults,
        IReadOnlyDictionary<string, PlanValueDescriptor>? overrides)
    {
        var merged = defaults is null
            ? new Dictionary<string, PlanValueDescriptor>()
            : new Dictionary<string, PlanValueDescriptor>(defaults);

        if (overrides is not null)
        {
            foreach (var (name, value) in overrides)
            {
                merged[name] = value;
            }
        }

        return merged;
    }

    private static ActorActionStateDefaults? MergeActionStateDefaults(
        ActorActionStateDefaults? defaults,
        ActorActionStateDefaults? overrides)
    {
        if (defaults is null)
        {
            return overrides;
        }

        if (overrides is null)
        {
            return defaults;
        }

        return new ActorActionStateDefaults(
            overrides.Facing ?? defaults.Facing,
            overrides.Target ?? defaults.Target);
    }

    private static void ApplyActionStateDefaults(ActionPlanContext context, ActorActionStateDefaults? defaults)
    {
        if (defaults?.Facing is { } facing)
        {
            context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(facing));
        }

        if (defaults?.Target is { } target)
        {
            context.Set(ActionPlanSlot.Target, new EntityPlanValue(target));
        }
    }

    private static void ApplyActionStateDefaults(WorldState world, EntityId entityId, ActorActionStateDefaults? defaults)
    {
        if (defaults?.Facing is { } facing)
        {
            world.SetActionFacing(entityId, facing);
        }

        if (defaults?.Target is { } target)
        {
            world.SetActionTarget(entityId, target);
        }
    }
}
