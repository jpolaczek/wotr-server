using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record MightyAttackStep : EffectChoiceStep
{
    public override string Presentation => "LeadershipAndMiss";
    public override int SelectionCount => 2;

    private static IReadOnlyList<string> Companions(MatchState state, Side side)
    {
        var force = BattleRuntime.Active(state)!.Force(side);
        if (force.Round.RerollLimit == 0)
            return [];
        var leadership = BattleLeadership.Available(state, force);
        return BattleRules.Characters(state, force).Where(c => leadership.GetValueOrDefault(c.Id) > 0 &&
            CharacterCatalog.All.GetValueOrDefault(c.DefinitionId)?.Role == "Companion").Select(c => c.Id).ToArray();
    }

    private static IEnumerable<BattleDie> Misses(MatchState state, Side side) =>
        BattleRuntime.Active(state)!.Force(side).Round.Dice.Where(d => !d.Hit && !d.ConvertedHit);

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var companions = Companions(state, execution.Owner);
        var misses = Misses(state, execution.Owner).Select(d => d.Id).ToArray();
        return companions.Count > 0 && misses.Length > 0 ? [.. companions, .. misses] : [];
    }

    public override bool ValidSelection(MatchState state, EffectExecution execution, IReadOnlyList<string> ids) =>
        base.ValidSelection(state, execution, ids) && ids.Count(Companions(state, execution.Owner).Contains) == 1;

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id)
    {
        var die = Misses(state, execution.Owner).FirstOrDefault(d => d.Id == id);
        return die is null ? new()
        {
            Id = id,
            SelectionGroup = "Companion",
            Detail = $"Forfeit {BattleLeadership.Available(state, BattleRuntime.Active(state)!.Force(execution.Owner))[id]} Leadership for this round."
        }
            : new() { Id = id, SelectionGroup = "Miss", Title = $"{die.Result} → Hit", Detail = die.CannotReroll ? "This miss cannot be re-rolled." : "Convert this miss to a hit." };
    }

    public override void Choose(EffectContext context, string id) => throw new InvalidOperationException("Mighty Attack requires two selections.");

    public override void ChooseSelection(EffectContext context, IReadOnlyList<string> ids)
    {
        var force = BattleRuntime.Active(context.State)!.Force(context.Owner);
        var companion = ids.Single(Companions(context.State, context.Owner).Contains);
        var points = BattleLeadership.Available(context.State, force)[companion];
        BattleLeadership.Forfeit(context.State, force, new Dictionary<string, int> { [companion] = points });
        var die = force.Round.Dice.Single(d => ids.Contains(d.Id));
        die.ConvertedHit = true;
        context.Record("CombatMissConverted", $"{context.Execution.SourceId}: {companion} forfeited {points} Leadership to convert {die.Id} to a hit.");
    }
}
