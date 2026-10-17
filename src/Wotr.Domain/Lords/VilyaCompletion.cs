using Wotr.Contracts;

namespace Wotr.Domain;

public static class VilyaCompletion
{
    public static List<string> EligibleDice(MatchState state)
    {
        if (!KeeperRings.Available(state, LordsDiceSetup.VilyaRing) || CharacterRules.InPlay(state, KeeperRings.Elrond) is null)
            return [];
        return state.Resolution.PaidDieIds.Where(id => !state.Lords!.PaidWillOfWestIds.Contains(id) &&
            state.Dice.Any(d => d.Id == id && d.Owner == Side.FreePeoples && d.Location == DieLocation.Resolving)).ToList();
    }

    public static bool Offer(MatchState state, Side actor, bool fellowshipMove)
    {
        if (actor != Side.FreePeoples || state.Winner is not null || EligibleDice(state).Count == 0)
            return false;
        state.Lords!.ActionCompletion = new() { FellowshipMove = fellowshipMove };
        state.Open(DecisionKind.KeepActionDie, actor, [CommandKind.UseVilya, CommandKind.DeclineVilya]);
        state.Pending!.SourceId = LordsDiceSetup.VilyaRing;
        state.Pending.Window = "action.completed";
        return true;
    }

    public static void Complete(MatchState state, string? keepDieId)
    {
        var completion = state.Lords!.ActionCompletion ?? throw new InvalidOperationException("Missing Vilya continuation.");
        if (keepDieId is not null)
        {
            KeeperRings.Transfer(state, LordsDiceSetup.VilyaRing);
            var die = state.Dice.Single(d => d.Id == keepDieId);
            die.Location = DieLocation.Available;
            die.HuntOrigin = "";
            state.Resolution.PaidDieIds.Remove(keepDieId);
            state.Events.Add(new() { Revision = state.Revision, Kind = "ActionDieKept", Text = $"Vilya kept {die.Id} available." });
        }
        state.Lords.ActionCompletion = null;
        TurnFlow.CompleteActionCost(state, Side.FreePeoples, completion.FellowshipMove);
    }
}
