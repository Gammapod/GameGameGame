namespace GameGameGame.Content;

public sealed record ContentValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
