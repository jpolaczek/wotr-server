namespace Wotr.Domain;

public sealed record ConfigurationConflict(string Option, string RuleId, string Source, string Requirement)
{
    public override string ToString() => $"{Option} requires {Requirement} ({RuleId}, {Source}).";
}
