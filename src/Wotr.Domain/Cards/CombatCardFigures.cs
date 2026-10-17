using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class CombatCardFigures
{
    public static IEnumerable<CharacterState> Companions(MatchState state, BattleForce force) =>
        BattleRules.Characters(state, force).Where(c => CharacterCatalog.All.GetValueOrDefault(c.DefinitionId)?.Role == "Companion");

    public static IEnumerable<UnitState> Leaders(MatchState state, BattleForce force) =>
        BattleRules.Units(state, force).Where(u => BoardRules.Nations[u.Nation].Side == Side.FreePeoples && BattleLeadership.IsLeader(state, force, u));
}

public sealed record CharacterInBattle(IReadOnlyList<string> Definitions) : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) =>
        BattleRules.Characters(state, battle.Force(actor)).Any(c => Definitions.Any(id => CharacterRules.MatchesCardName(c, id)));
}

public sealed record LeaderOrCompanionInBattle : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) =>
        CombatCardFigures.Companions(state, battle.Force(actor)).Any() || CombatCardFigures.Leaders(state, battle.Force(actor)).Any();
}

public sealed record DaringDefianceStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var battle = BattleRuntime.Active(context.State)!;
        var opposing = battle.Opponent(context.Owner).Round;
        if (opposing.CardId.Length == 0 || opposing.CardCancelled)
            return;
        var force = battle.Force(context.Owner);
        var cost = CombatCardFigures.Companions(context.State, force)
            .ToDictionary(c => c.Id, c => KingsBattle.Leadership(context.State, force, c));
        if (!BattleLeadership.CanForfeit(context.State, force, cost))
            return;
        BattleLeadership.Forfeit(context.State, force, cost);
        opposing.CardCancelled = true;
        context.Record("CombatCardCancelled", $"{context.Execution.SourceId}: cancelled {opposing.CardId} by forfeiting all Companion Leadership.");
    }
}
