namespace GameGameGame.Frontend.SadConsole;

internal sealed record FrontendTextMessage(string Id, IReadOnlyDictionary<string, string> Args)
{
    public static FrontendTextMessage Create(string id, params (string Key, object? Value)[] args) => new(
        id,
        args.ToDictionary(arg => arg.Key, arg => arg.Value?.ToString() ?? string.Empty));
}

internal static class FrontendTextIds
{
    public const string InspectionStatAperture = "inspection.stat.aperture";
    public const string InspectionStatBulk = "inspection.stat.bulk";
    public const string InspectionActionsHeader = "inspection.actions.header";
    public const string InspectionActionNoValidActions = "inspection.action.none";
    public const string InspectionActionPickup = "inspection.action.pickup";
    public const string InspectionActionDrop = "inspection.action.drop";
    public const string InspectionActionEnter = "inspection.action.enter";
    public const string InspectionActionPush = "inspection.action.push";
    public const string InspectionActionTransfer = "inspection.action.transfer";
    public const string InspectionActionGeneric = "inspection.action.generic";
    public const string InspectionActionUnavailable = "inspection.action.unavailable";
    public const string PlayActionNoSelection = "play.action.no-selection";
    public const string PlayActionUnavailable = "play.action.unavailable";
    public const string PlayActionPromptPickupDestination = "play.action.prompt.pickup-destination";
    public const string PlayActionPromptDestination = "play.action.prompt.destination";
    public const string PlayActionPromptPushDirection = "play.action.prompt.push-direction";
    public const string PlayActionPromptDirection = "play.action.prompt.direction";
    public const string PlayActionPromptTransferItem = "play.action.prompt.transfer-item";
    public const string PlayActionPromptTransferItemChoice = "play.action.prompt.transfer-item-choice";
}

internal sealed class FrontendTextResolver
{
    public static FrontendTextResolver InspectionPrototype { get; } = new(new Dictionary<string, string>
    {
        [FrontendTextIds.InspectionStatAperture] = "Aperture.text.id: {value}",
        [FrontendTextIds.InspectionStatBulk] = "Bulk.text.id: {value}",
        [FrontendTextIds.InspectionActionsHeader] = "Actions:",
        [FrontendTextIds.InspectionActionNoValidActions] = "No valid actions",
        [FrontendTextIds.InspectionActionPickup] = "Pickup {targetName}",
        [FrontendTextIds.InspectionActionDrop] = "Drop {targetName}",
        [FrontendTextIds.InspectionActionEnter] = "Enter {targetName}",
        [FrontendTextIds.InspectionActionPush] = "Push {targetName}",
        [FrontendTextIds.InspectionActionTransfer] = "Transfer with {targetName}",
        [FrontendTextIds.InspectionActionGeneric] = "{actionName} {targetName}",
        [FrontendTextIds.InspectionActionUnavailable] = "{action}: {reason}",
        [FrontendTextIds.PlayActionNoSelection] = "No action selected",
        [FrontendTextIds.PlayActionUnavailable] = "{reason}",
        [FrontendTextIds.PlayActionPromptPickupDestination] = "Choose pickup destination for {targetName}",
        [FrontendTextIds.PlayActionPromptDestination] = "to {coord}",
        [FrontendTextIds.PlayActionPromptPushDirection] = "Choose push direction for {targetName}",
        [FrontendTextIds.PlayActionPromptDirection] = "{direction}",
        [FrontendTextIds.PlayActionPromptTransferItem] = "Choose transfer item with {targetName}",
        [FrontendTextIds.PlayActionPromptTransferItemChoice] = "{entityId}"
    });

    private readonly IReadOnlyDictionary<string, string> _templates;

    public FrontendTextResolver(IReadOnlyDictionary<string, string> templates)
    {
        _templates = templates;
    }

    public string Resolve(FrontendTextMessage message)
    {
        if (!_templates.TryGetValue(message.Id, out var template))
        {
            return message.Args.Count == 0
                ? message.Id
                : $"{message.Id} {string.Join(" ", message.Args.Select(arg => $"{arg.Key}={arg.Value}"))}";
        }

        foreach (var (key, value) in message.Args)
        {
            template = template.Replace("{" + key + "}", value, StringComparison.Ordinal);
        }

        return template;
    }
}
