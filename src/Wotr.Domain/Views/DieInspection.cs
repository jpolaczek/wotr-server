using Wotr.Contracts;
using Wotr.Domain.Content;
using static Wotr.Domain.AvailabilityDescription;

namespace Wotr.Domain;

internal static class DieInspection
{
    public static string SourceTitle(DieState die) => LordsDiceCatalog.Definitions.TryGetValue(die.Source, out var special)
        ? CharacterCatalog.All[special.CharacterId].Title
        : die.Source == SovereignCatalog.RulerSource ? "Ruler" : die.Source == "standard" ? "Standard" : die.Source;

    // Recovery is conditional on the future board, never a prediction of its outcome.
    public static string Recovery(DieState die)
    {
        if (die.Special?.PermanentlyRemoved == true)
            return "Permanently removed; this die cannot return.";
        if (die.Source == SovereignCatalog.RulerSource)
            return die.Owner == Side.FreePeoples
                ? "At the next Recover, available only while an awakened sovereign remains in play; one shared Ruler die."
                : "At the next Recover, available only while a Dark Chieftain remains in play; one shared Ruler die.";
        if (die.Source == LordsDiceCatalog.Narya)
            return "At Recover, requires Gandalf the Keeper as Guide and a recovered Fellowship movement die (except the initial Guide in turn one).";
        if (die.Special is null)
            return "Recovered at the next Recover phase while its source remains eligible.";
        var text = die.Owner == Side.FreePeoples
            ? "At Recover, a chosen Remove rune permanently removes this die only if Gandalf the White is then in play."
            : "At Recover, a chosen Remove rune permanently removes this die only if either Witch-king version is then in play.";
        text += " An unchosen rune does not remove it. Its source character must remain in play.";
        if (die.Source == LordsDiceCatalog.Gothmog)
            text += " The Black Captain also permanently removes Gothmog's die at Recover.";
        return text;
    }

    public static IEnumerable<ActionAvailabilityView> For(MatchState state, Side viewer)
    {
        if (state.Configuration.Lords && state.Lords is not null)
        {
            var candidates = LordsDiceRoll.LegalChoices(state, viewer).ToHashSet(StringComparer.Ordinal);
            foreach (var die in state.Dice.Where(d => d.Owner == viewer && d.Special is not null))
            {
                var window = state.Phase == GamePhase.ActionRoll && state.Pending?.Kind == DecisionKind.SpecialDieChoice &&
                    state.Lords.ChoiceOrder.Count > 0 && state.Lords.ChoiceOrder[0] == viewer;
                yield return Create(state, viewer, die.Id, "keep-special", "Keep this special die", "LORDS pp.7–11",
                    "Set aside your other special dice until Recover.", "During your special-die choice after the Action Roll.", window,
                    Condition("Target", "special-candidate", "This is an unchosen rolled special die; if any special Eye was rolled, choose an Eye.", candidates.Contains(die.Id)));
            }
        }
        if (state.Configuration.Kings && state.Kings is not null)
        {
            var candidates = KingsDice.DiscardCandidates(state, viewer).ToHashSet(StringComparer.Ordinal);
            foreach (var die in state.Dice.Where(d => d.Owner == viewer))
            {
                var window = state.Phase == GamePhase.ActionRoll && state.Pending?.Kind == DecisionKind.RulerDiscard &&
                    state.Kings.RulerDiscardOrder.Count > 0 && state.Kings.RulerDiscardOrder[0] == viewer;
                yield return Create(state, viewer, die.Id, "discard-after-ruler", "Set aside after rolling the Ruler die", "KINGS pp.6,17",
                    "Set this die aside until the next Recover phase.", "After special-die choices: Shadow discards before Free Peoples.", window,
                    Condition("Target", "discard-candidate", "Requires a retained standard or Ruler die; Keeper/Lesser and standard Shadow Eyes are excluded.", candidates.Contains(die.Id)));
            }
        }
    }
}
