using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record FatefulStrikeStep : EffectChoiceStep
{
    public const string Decline = "fateful-strike.decline";
    public override string Presentation => "NazgulOrMinion";

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var battle = BattleRuntime.Active(state)!;
        var hits = battle.Force(execution.Owner).Round.Dice.Count(d => d.RerollHit);
        if (hits == 0)
            return [];
        var opponent = battle.Opponent(execution.Owner);
        var nazgul = BattleRules.Units(state, opponent).Where(u => u.Kind == UnitKind.Nazgul).Select(u => u.Id).ToArray();
        if (hits == 1)
            return nazgul;
        var minions = BattleRules.Characters(state, opponent)
            .Where(c => CharacterCatalog.All.GetValueOrDefault(c.DefinitionId)?.Role == "Minion").Select(c => c.Id).ToArray();
        return nazgul.Length == 0 && minions.Length > 0 ? [.. minions, Decline] : [.. nazgul, .. minions];
    }

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        Title = id == Decline ? "Keep Minions" : "",
        Detail = id == Decline ? "Do not use the optional Minion elimination." : "Eliminate this figure. All Leader re-roll hits still inflict casualties."
    };

    public override void Choose(EffectContext context, string id)
    {
        if (id == Decline)
            return;
        var unit = context.State.Units.FirstOrDefault(u => u.Id == id);
        if (unit is not null)
            BoardRules.RemoveCasualty(context.State, unit);
        else
            CharacterLifecycle.Eliminate(context.State, context.State.Characters.Single(c => c.Id == id));
        context.Record("FatefulStrike", $"{context.Execution.SourceId}: additionally eliminated {id}.");
    }

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        if (!ids.Contains(Decline))
            preview.RemovedIds.Add(ids.Single());
    }
}
