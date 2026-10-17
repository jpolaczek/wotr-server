using Wotr.Contracts;
using static Wotr.Domain.AvailabilityDescription;

namespace Wotr.Domain;

internal static class SovereignAvailabilityProjection
{
    public static IEnumerable<ActionAvailabilityView> For(MatchState state, Side viewer)
    {
        if (!state.Configuration.Kings || state.Kings is null)
            yield break;
        foreach (var sovereign in state.Kings.Sovereigns)
        {
            if (viewer == Side.FreePeoples)
            {
                yield return Awaken(state, sovereign);
                yield return Remove(state, sovereign);
                yield return Cleanse(state, sovereign);
            }
            else
                yield return Corrupt(state, sovereign);
        }
    }

    private static ActionAvailabilityView Awaken(MatchState state, SovereignState sovereign)
    {
        var targets = SovereignAwakening.Destinations(state, sovereign);
        var dice = state.Dice.Where(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available &&
            SovereignAwakening.CanPay(state, d)).Select(d => d.Id).ToList();
        var definition = SovereignCatalog.All[sovereign.Id];
        var nation = state.Nations.Single(n => n.Id == definition.Nation);
        var result = Create(state, Side.FreePeoples, sovereign.Id, "awaken", "Awaken", "KINGS p.10",
            "One Muster result (including Army/Muster or Will of the West), or any available Ruler result.",
            "During your normal action.", ActionWindow(state, Side.FreePeoples),
            Condition("Condition", "initial", "The sovereign is in its initial state.", sovereign.Status == SovereignStatus.Initial),
            Condition("Condition", "nation", definition.Title + " requires an active nation" +
                (sovereign.Id == SovereignCatalog.Thranduil ? " at war." : "."),
                nation.Active && (sovereign.Id != SovereignCatalog.Thranduil || nation.StepsToWar == 0)),
            Condition("Target", "awakening-route", "A permitted destination is reachable within the sovereign's movement, respecting sieges and card restrictions.", targets.Count > 0),
            Condition("Cost", "awakening-die", "An eligible awakening die is available; current table-card restrictions apply.", dice.Count > 0));
        result.EligibleDice = dice;
        result.Targets = targets;
        return result;
    }

    private static ActionAvailabilityView Remove(MatchState state, SovereignState sovereign)
    {
        var dice = state.Dice.Where(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available &&
            DiceRules.CanPay(d, DieFace.Character)).Select(d => d.Id).ToList();
        var result = Create(state, Side.FreePeoples, sovereign.Id, "remove-corrupted", "Remove corrupted sovereign",
            "KINGS p.13", "One Character or Will of the West result.", "During your normal action.",
            ActionWindow(state, Side.FreePeoples),
            Condition("Condition", "corrupted", "The sovereign is corrupted.", sovereign.Status == SovereignStatus.Corrupted),
            Condition("Target", "companion-icon", "A Companion with the matching nation or Free Peoples icon is in this region.", SovereignAwakening.AppropriateCompanion(state, sovereign)),
            Condition("Cost", "character-die", "An eligible Character result is available.", dice.Count > 0));
        result.EligibleDice = dice;
        if (SovereignAwakening.CanRemove(state, sovereign.Id))
            result.Targets = [SovereignLifecycle.Region(state, sovereign)];
        return result;
    }

    private static ActionAvailabilityView Corrupt(MatchState state, SovereignState sovereign)
    {
        var dice = CorruptionRules.Eyes(state).Select(d => d.Id).ToList();
        var result = Create(state, Side.Shadow, sovereign.Id, "attempt-corruption", "Attempt corruption", "KINGS pp.11–12,17",
            "Spend one eligible Hunt Eye; draw up to three tiles using the remaining eligible Eyes. Captain of Despair dice do not count.",
            "Instead of your normal action.", ActionWindow(state, Side.Shadow),
            Condition("Condition", "initial", "The sovereign is in its initial state on the board.",
                sovereign.Status == SovereignStatus.Initial && BoardRules.Regions.ContainsKey(SovereignLifecycle.Region(state, sovereign))),
            Condition("Limit", "attempt-turn", "This sovereign has not been targeted by a normal attempt this turn.", sovereign.LastAttemptTurn != state.Turn),
            Condition("Condition", "fp-unused-die", "Free Peoples still has an unused action die.", TurnFlow.Available(state, Side.FreePeoples) > 0),
            Condition("Cost", "hunt-eyes", "At least two eligible Eyes are in the Hunt Box before paying the cost.", dice.Count > 1),
            Condition("Condition", "ring-action", "An action after using an Elven Ring is not pending.", !state.Player(Side.Shadow).RingActionPending));
        result.EligibleDice = dice;
        result.Targets = CorruptionRules.Targets(state).Where(s => s.Id == sovereign.Id).Select(s => s.Id).ToList();
        return result;
    }

    private static ActionAvailabilityView Cleanse(MatchState state, SovereignState sovereign)
    {
        var effect = EffectRuntime.Current(state);
        var window = effect?.EffectId == KingsPolitics.ProgramId && effect.Effect?.SourceId == sovereign.Id &&
            state.Pending?.Presentation == "CorruptionRemoval";
        return Create(state, Side.FreePeoples, sovereign.Id, "cleanse", "Return one corruption tile", "KINGS p.13",
            "No additional die; the political activation or advance has already been paid.",
            "After a real activation or political advance of this nation; returning the tile is optional and random.", window,
            Condition("Condition", "cleansing-conditions", "An initial sovereign has an eligible non-Kings tile and a Companion with the matching nation or Free Peoples icon in its region; current card restrictions apply.",
                KingsPolitics.Eligible(state, sovereign)));
    }
}
