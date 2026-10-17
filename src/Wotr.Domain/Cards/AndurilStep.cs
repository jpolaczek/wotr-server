using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record AndurilStep : EffectChoiceStep
{
    public const string KeepMisses = "anduril.keep-misses";
    public override string Presentation => "ConvertMisses";

    private static CharacterState? Leader(MatchState state, Side side)
    {
        var force = BattleRuntime.Active(state)!.Force(side);
        if (force.Round.RerollLimit == 0)
            return null;
        var available = BattleLeadership.Available(state, force);
        return BattleRules.Characters(state, force).FirstOrDefault(c =>
            CharacterRules.IsStriderOrAragorn(c) && available.GetValueOrDefault(c.Id) == c.Leadership);
    }

    private static IEnumerable<BattleDie> Misses(MatchState state, Side side) =>
        BattleRuntime.Active(state)!.Force(side).Round.Dice.Where(d => !d.Hit && !d.ConvertedHit);

    public override int Maximum(MatchState state, EffectExecution execution) =>
        Leader(state, execution.Owner)?.DefinitionId == CharacterRules.Aragorn ? 2 : 1;

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var leader = Leader(state, execution.Owner);
        var misses = Misses(state, execution.Owner).Select(d => d.Id).ToArray();
        if (leader is null || misses.Length == 0)
            return [];
        return leader.DefinitionId == CharacterRules.Aragorn ? [.. misses, KeepMisses] : misses;
    }

    public override bool ValidSelection(MatchState state, EffectExecution execution, IReadOnlyList<string> ids) =>
        base.ValidSelection(state, execution, ids) && (!ids.Contains(KeepMisses) || ids.Count == 1);

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id)
    {
        var leader = Leader(state, execution.Owner)!;
        var cost = $"Forfeit {leader.Leadership} Leadership of {CharacterCatalog.All[leader.DefinitionId].Title}.";
        if (id == KeepMisses)
            return new() { Id = id, Title = "Keep misses", Detail = cost + " Convert no dice.", Exclusive = true, SelectionGroup = "Miss" };
        var die = Misses(state, execution.Owner).Single(d => d.Id == id);
        return new()
        {
            Id = id,
            SelectionGroup = "Miss",
            GroupMaximum = Maximum(state, execution),
            Title = $"{die.Result} → Hit",
            Detail = cost + (die.CannotReroll ? " This miss cannot be re-rolled." : " Convert this miss to a hit.")
        };
    }

    public override void Choose(EffectContext context, string id) => ChooseSelection(context, [id]);
    public override void ChooseSelection(EffectContext context, IReadOnlyList<string> ids)
    {
        var leader = Leader(context.State, context.Owner)!;
        var force = BattleRuntime.Active(context.State)!.Force(context.Owner);
        BattleLeadership.Forfeit(context.State, force, new Dictionary<string, int> { [leader.Id] = leader.Leadership });
        foreach (var die in force.Round.Dice.Where(d => ids.Contains(d.Id)))
            die.ConvertedHit = true;
        context.Record("CombatMissConverted", $"{context.Execution.SourceId}: forfeited {leader.Leadership} Leadership; converted {ids.Count(id => id != KeepMisses)} misses.");
    }
}
