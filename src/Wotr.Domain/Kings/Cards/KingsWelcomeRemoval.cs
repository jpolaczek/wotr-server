using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsWelcomeRemoval
{
    public static ActionOption? Option(MatchState state, Side actor)
    {
        if (!state.Configuration.Kings || actor != Side.FreePeoples || state.Pending is not { Kind: DecisionKind.Action, Owner: Side.FreePeoples } ||
            !state.Player(Side.Shadow).Table.Contains(KingsCards02.Welcome))
            return null;
        var characterAllowed = HasPair(state);
        var dice = state.Dice.Where(d => d.Owner == actor && d.Location == DieLocation.Available &&
            (d.Face == DieFace.WillOfTheWest || characterAllowed && d.Face == DieFace.Character)).Select(d => d.Id).ToList();
        return dice.Count == 0 ? null : new() { Kind = CommandKind.DiscardWelcome, EligibleDice = dice };
    }

    private static bool HasPair(MatchState state)
    {
        var companions = state.Characters.Where(c => CharacterRules.OnBoard(c) && CharacterRules.IsCompanion(c)).ToArray();
        return NonCorruptedSovereigns.OnBoard(state).Any(s => companions.Any(c =>
            c.Id != s.Id && c.Location == SovereignLifecycle.Region(state, s)));
    }

    public static bool Valid(MatchState state, Side actor, CommandPayload payload) =>
        payload.RingId == "" && Option(state, actor)?.EligibleDice.Contains(payload.DieId) == true;

    public static void Apply(MatchState state, CommandPayload payload)
    {
        CharacterRules.PayAny(state, Side.FreePeoples, payload.DieId);
        TableCardRules.Discard(state, Side.Shadow, KingsCards02.Welcome);
        state.Events.Add(new()
        {
            Revision = state.Revision, Kind = "TableCardRemoved",
            Text = "Free Peoples spent an action to discard Your Welcome Is Doubtful."
        });
        TurnFlow.FinishAction(state, Side.FreePeoples);
    }
}
