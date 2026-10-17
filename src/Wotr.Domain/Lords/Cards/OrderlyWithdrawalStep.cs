using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record OrderlyWithdrawalCondition : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) =>
        battle.Mode is BattleMode.Field or BattleMode.Relief or BattleMode.Sortie &&
        (BattleRules.Units(state, battle.Force(actor)).Any(u => u.Kind == UnitKind.Leader) ||
         BattleRules.Characters(state, battle.Force(actor)).Any(CharacterRules.IsCompanion));
}

public sealed record OrderlyWithdrawalStep : EffectChoiceStep
{
    public const string Cancel = "withdrawal.cancel-hit";
    public const string Decline = "withdrawal.decline";
    public override string Presentation => "CombatEffect";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var battle = BattleRuntime.Active(state)!;
        return BattleRoll.HitCount(battle.Opponent(execution.Owner)) > battle.Force(execution.Owner).Round.CancelHits
            ? [Cancel, Decline] : [];
    }

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        Title = id == Cancel ? "Cancel hit · withdraw" : "Keep the hit",
        Cost = id == Cancel ? "End the attack or retreat at the end of this round, if possible." : "",
        Detail = id == Cancel ? "Resolve both armies' remaining casualties, then end the attack or retreat if possible." : "Continue the normal battle procedure."
    };

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.Summary = ids.Contains(Cancel) ? "Cancel one hit; end the attack or retreat after casualties, if possible." : "Keep the hit and the normal battle choices.";

    public override void Choose(EffectContext context, string id)
    {
        if (id != Cancel)
            return;
        var round = BattleRuntime.Active(context.State)!.Force(context.Owner).Round;
        round.CancelHits++;
        round.OrderlyWithdrawal = true;
        context.Record("OrderlyWithdrawal", "Free Peoples cancelled one hit and must withdraw at the end of this round, if possible.");
    }
}

public static class OrderlyWithdrawal
{
    public static bool EndRound(MatchState state, BattleState battle)
    {
        if (battle.Attacker.Round.OrderlyWithdrawal)
        {
            battle.Stage = BattleStage.Finish;
            return true;
        }
        if (battle.Defender.Round.OrderlyWithdrawal &&
            (BattleMovement.Retreats(state, battle).Any() || StrongholdRules.CanShelter(state, battle)))
        {
            battle.Stage = BattleStage.Retreat;
            return true;
        }
        return false;
    }
}
