using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record LimitCombatDiceStep(int MaximumDice) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var round = BattleRuntime.Active(context.State)!.Opponent(context.Owner).Round;
        round.CombatDiceLimit = Math.Min(round.CombatDiceLimit, MaximumDice);
    }
}

public sealed record AdditionalCombatHitsStep(int Hits) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var round = BattleRuntime.Active(context.State)!.Force(context.Owner).Round;
        if (round.Dice.Any(d => d.Hit || d.ConvertedHit || d.RerollHit))
            round.ExtraHits += Hits;
    }
}

public sealed record FreeNationBattleCondition : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) =>
        BoardRules.Region(state, battle.Region).Nation is { } nation && BoardRules.Nations[nation].Side == Side.FreePeoples;
}

public sealed record ShieldWallStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var battle = BattleRuntime.Active(context.State)!;
        if (BattleRoll.HitCount(battle.Opponent(context.Owner)) >= 2)
            battle.Force(context.Owner).Round.CancelHits++;
    }
}

public sealed record ScoutsCondition : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) => battle.Defender.Side == actor &&
        battle.Mode is BattleMode.Field or BattleMode.Relief;
}

public sealed record ScoutsStep : EffectChoiceStep
{
    public const string Shelter = "scouts.shelter";
    public override string Presentation => "RegionOrShelter";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var battle = BattleRuntime.Active(state)!;
        return [.. BattleMovement.Retreats(state, battle), .. StrongholdRules.CanShelter(state, battle) ? new[] { Shelter } : []];
    }

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        TargetId = id == Shelter ? "" : id,
        Title = id == Shelter ? "Withdraw into siege" : "Retreat",
        Detail = "End this battle before the Combat roll."
    };

    public override void Choose(EffectContext context, string id)
    {
        var battle = BattleRuntime.Active(context.State)!;
        battle.EndedByEffect = true;
        if (id == Shelter)
            StrongholdRules.Shelter(context.State, battle);
        else
            BattleMovement.Retreat(context.State, battle, id);
    }
}

public sealed record ConfusionStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var battle = BattleRuntime.Active(context.State)!;
        var ones = battle.Opponent(context.Owner).Round.Dice.Where(d => d.Result == 1).ToList();
        foreach (var die in ones)
            die.CannotReroll = true;
        battle.Force(context.Owner).Round.ExtraHits += ones.Count;
    }
}
