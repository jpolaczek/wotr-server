namespace Wotr.Domain.Content;

public sealed class ContentConflictException(string slotId, string ruleId, string source, string reason)
    : InvalidOperationException($"Content conflict in '{slotId}' ({ruleId}, {source}): {reason}.")
{
    public string SlotId { get; } = slotId;
    public string RuleId { get; } = ruleId;
    public string SourceReference { get; } = source;
}
