using Wotr.Contracts;

namespace Wotr.Domain;

public enum PoliticalTrigger { EnemyEntry, ArmyAttack, CompanionArrival, SettlementCapture }
public sealed record PoliticalReaction(PoliticalTrigger Trigger, string NationId, string RegionId);

public static class PoliticalReactions
{
    public static void Add(MatchState state, PoliticalTrigger trigger, string nation, string region) =>
        state.PoliticalReactions.Add(new(trigger, nation, region));

    public static void Complete(MatchState state)
    {
        foreach (var reaction in state.PoliticalReactions)
        {
            if (reaction.Trigger != PoliticalTrigger.SettlementCapture)
                PoliticalRules.Activate(state, reaction.NationId, reaction.Trigger, reaction.RegionId);
            if (reaction.Trigger is PoliticalTrigger.ArmyAttack or PoliticalTrigger.SettlementCapture)
                PoliticalRules.Advance(state, reaction.NationId, reaction.Trigger == PoliticalTrigger.ArmyAttack
                    ? PoliticalAdvanceCause.Attack : PoliticalAdvanceCause.SettlementCapture);
        }
        state.PoliticalReactions.Clear();
    }

    // Preview only: apply public consequences to a small detached state, never the live table or hand.
    public static IReadOnlyList<NationState> Preview(MatchState original, MatchState board)
    {
        var preview = new MatchState
        {
            Nations = board.Nations.Select(n => new NationState { Id = n.Id, Active = n.Active, StepsToWar = n.StepsToWar }).ToList(),
            PoliticalReactions = [.. board.PoliticalReactions],
            ActiveEffects = [.. original.ActiveEffects],
            Players = original.Players.Select(p => new PlayerState { Side = p.Side, Table = [.. p.Table] }).ToList()
        };
        Complete(preview);
        return preview.Nations;
    }
}
