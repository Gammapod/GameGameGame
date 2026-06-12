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

            foreach (var step in descriptor.Steps)
            {
                ValidateCalledPlan(errors, templateId, descriptor, step, step.OnSuccess);
                ValidateCalledPlan(errors, templateId, descriptor, step, step.OnFailure);
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
}
