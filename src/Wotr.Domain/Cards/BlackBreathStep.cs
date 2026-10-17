using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record NazgulInBattle : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) =>
        BattleRules.Units(state, battle.Force(actor)).Any(u => u.Kind == UnitKind.Nazgul) ||
        BattleRules.Characters(state, battle.Force(actor)).Any(CharacterRules.IsWitchKing);
}

public sealed record BlackBreathStep : EffectChoiceStep
{
    public const string Decline = "black-breath.decline";
    public override string Presentation => "LeaderOrCompanion";

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var battle = BattleRuntime.Active(state)!;
        var hits = battle.Force(execution.Owner).Round.Dice.Count(d => d.RerollHit);
        if (hits == 0)
            return [];
        var opponent = battle.Opponent(execution.Owner);
        var targets = CombatCardFigures.Leaders(state, opponent).Select(u => u.Id)
            .Concat(CombatCardFigures.Companions(state, opponent).Where(c => CharacterRules.CombatLevel(c) <= hits).Select(c => c.Id)).ToArray();
        return targets.Length == 0 ? [] : [.. targets, Decline];
    }

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        Title = id == Decline ? "Keep all" : "",
        Exclusive = id == Decline,
        Detail = id == Decline ? "Do not use the additional elimination." : "Eliminate this figure. All Leader re-roll hits still inflict casualties."
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
        context.Record("BlackBreath", $"{context.Execution.SourceId}: additionally eliminated {id}.");
    }

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        if (!ids.Contains(Decline))
            preview.RemovedIds.Add(ids.Single());
    }
}

public sealed record FoulStenchStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var battle = BattleRuntime.Active(context.State)!;
        if (BattleLeadership.Nazgul(context.State, battle.Force(context.Owner)).Values.Sum() >=
            BattleLeadership.Total(context.State, battle.Opponent(context.Owner)))
            battle.Opponent(context.Owner).Round.RerollLimit = 0;
    }
}
