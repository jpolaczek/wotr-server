using Wotr.Contracts;
using static Wotr.Domain.AvailabilityDescription;

namespace Wotr.Domain;

internal static class ResourceAvailabilityProjection
{
    public static IEnumerable<ActionAvailabilityView> For(MatchState state, Side viewer, IReadOnlyList<RulerTokenActionOption> rulerActions)
    {
        foreach (var ring in state.Rings.Where(r => r.Owner == viewer))
        {
            yield return Ring(state, viewer, ring.Id);
            if (viewer == Side.FreePeoples && state.Configuration.Lords && state.Lords is not null)
            {
                var keeper = Keeper(state, ring.Id);
                if (keeper is not null)
                    yield return keeper;
            }
        }
        if (viewer == Side.Shadow && state.Configuration.Council && state.Lords?.Council is { } council)
            foreach (var token in council.GrantedTokens)
            {
                yield return Council(state, token);
                if (state.Configuration.Kings)
                    yield return CouncilEntry(state, token);
            }
        if (state.Configuration.Kings && state.Kings is not null)
            foreach (var token in state.Kings.Tokens)
            {
                if (viewer == Side.FreePeoples && token.Location == "free-peoples")
                    yield return Ruler(state, token, rulerActions);
                else if (viewer == Side.Shadow && token.Location == KingsDismay.Location)
                    yield return Dismay(state, token);
            }
    }

    private static ActionAvailabilityView Ring(MatchState state, Side viewer, string ring)
    {
        var dice = state.Dice.Where(d => RingRules.CanChange(d, viewer)).Select(d => d.Id).ToList();
        var result = Create(state, viewer, ring, "change-die", "Change die result", "BASE pp.21–22; KINGS p.10",
            viewer == Side.FreePeoples ? "Transfer this Ring to Shadow." : "Remove this Ring from the game.",
            "During your normal action, before using the changed die.", ActionWindow(state, viewer),
            Condition("Limit", "ring-turn", "At most one ordinary Ring change per player each turn.", !state.Player(viewer).RingUsedThisTurn),
            Condition("Limit", "ruler-token", "A Ruler token has not been used this turn.", KingsRulerTokens.RingAllowed(state, viewer)),
            Condition("Target", "changeable-die", "An unused non-Eye standard or Ruler die is available; Keeper and Lesser Minion dice cannot be changed.", dice.Count > 0));
        result.EligibleDice = dice;
        return result;
    }

    private static ActionAvailabilityView? Keeper(MatchState state, string ring)
    {
        var dice = new List<string>();
        var targets = new List<string>();
        var condition = false;
        var window = false;
        string title, timing, detail;
        if (ring == LordsDiceSetup.NaryaRing)
        {
            title = "Use Narya";
            timing = "During your normal action.";
            detail = "Gandalf the Keeper is in an unconquered Free Peoples settlement, with an unused action die.";
            dice = state.Dice.Where(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Select(d => d.Id).ToList();
            if (KeeperRings.NaryaNation(state) is { } nation)
                targets.Add(nation);
            condition = targets.Count > 0 && dice.Count > 0;
            window = ActionWindow(state, Side.FreePeoples);
        }
        else if (ring == LordsDiceSetup.NenyaRing)
        {
            title = "Use Nenya";
            timing = "In the replacement window of a just-drawn standard Eye Hunt tile.";
            detail = "Galadriel is in play and a standard Eye Hunt tile can be replaced.";
            condition = new NenyaCondition().CanPlay(state, Side.FreePeoples) && CharacterRules.InPlay(state, KeeperRings.Galadriel) is not null;
            window = state.Pending is { Kind: DecisionKind.Reaction, Window: "hunt.replace" };
        }
        else if (ring == LordsDiceSetup.VilyaRing)
        {
            title = "Use Vilya";
            timing = "After the entire paid action resolves, before completing its cost.";
            detail = "Elrond is in play and a paid die other than a Will of the West can be retained.";
            dice = VilyaCompletion.EligibleDice(state);
            condition = dice.Count > 0;
            window = state.Pending?.Kind == DecisionKind.KeepActionDie && state.Lords?.ActionCompletion is not null;
        }
        else
            return null;
        var result = Create(state, Side.FreePeoples, ring, "keeper", title, "LORDS pp.14–15",
            ring == LordsDiceSetup.NaryaRing ? "Transfer Narya and spend one available action die." : "Transfer this Ring to Shadow.",
            timing, window,
            Condition("Limit", "ruler-token", "A Ruler token has not been used this turn; ordinary Ring use does not consume this ability.", KingsRulerTokens.RingAllowed(state, Side.FreePeoples)),
            Condition("Condition", "keeper-conditions", detail, condition));
        result.EligibleDice = dice;
        result.Targets = targets;
        return result;
    }

    private static ActionAvailabilityView Council(MatchState state, string token)
    {
        var council = state.Lords!.Council!;
        var targets = token == CouncilTokens.Politics ? CouncilTokens.Nations(state)
            : ShadowTravel.Options(state).SelectMany(t => t.Targets).Distinct().ToList();
        var result = Create(state, Side.Shadow, token, "council", "Use Council token", "LORDS p.31",
            "Spend this token once in the game.", "Instead of a normal die action; at most one Council token per turn.",
            ActionWindow(state, Side.Shadow),
            Condition("Cost", "unspent-token", "This Council token has not been spent.", !council.SpentTokens.Contains(token)),
            Condition("Limit", "council-turn", "No Council token has been used this turn.", council.LastTokenTurn != state.Turn),
            Condition("Condition", "ring-action", "An action after using an Elven Ring is not pending.", !state.Player(Side.Shadow).RingActionPending),
            Condition("Target", "council-target", token == CouncilTokens.Politics ? "A Shadow nation can advance." : "A Nazgûl or Minion has a legal journey.", targets.Count > 0));
        result.Targets = targets;
        return result;
    }

    private static ActionAvailabilityView CouncilEntry(MatchState state, string token)
    {
        var candidates = KingsChieftains.Candidates(state, Side.Shadow, ignorePrerequisite: true).ToList();
        var dice = state.Dice.Where(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available &&
            (d.Source == SovereignCatalog.RulerSource && d.Face != DieFace.Eye || DiceRules.CanPay(d, DieFace.Muster)))
            .Select(d => d.Id).ToList();
        var result = Create(state, Side.Shadow, token, "council-chieftain", "Bring a Dark Chieftain into play", "KINGS p.18",
            "Spend this Council token together with an eligible Muster or non-Eye Ruler result.",
            "During your normal action; replaces only the chieftain's prerequisite.", ActionWindow(state, Side.Shadow),
            Condition("Limit", "council-available", "This Council token is unspent and none was used this turn.", CouncilTokens.Available(state, token)),
            Condition("Cost", "chieftain-die", "An eligible entry die is available.", dice.Count > 0),
            Condition("Target", "chieftain-region", "A chieftain has not entered before and has a legal entry region.", candidates.Count > 0));
        result.EligibleDice = dice;
        result.Targets = candidates.Select(c => c.Region).Distinct().ToList();
        return result;
    }

    private static ActionAvailabilityView Ruler(MatchState state, RulerTokenState token, IReadOnlyList<RulerTokenActionOption> actions)
    {
        var die = KingsDice.Ruler(state, Side.FreePeoples);
        var conditions = new List<AvailabilityConditionView>
        {
            Condition("Condition", "ruler-enabled", "Ruler tokens are enabled for this match.", !state.Configuration.RemovingRulerTokens),
            Condition("Limit", "ring-turn", "No Elven Ring has been used by Free Peoples this turn, including Keeper abilities.",
                !state.Player(Side.FreePeoples).RingUsedThisTurn && state.Kings?.ElvenRingUsedThisTurn != true),
            Condition("Cost", "ruler-die", "The Free Peoples Ruler die is available.", die?.Location == DieLocation.Available),
            Condition("Condition", "ruler-source", "The awakened sovereign is in play and meets its location and travel restrictions.", KingsRulerTokens.SourceReady(state, token))
        };
        // Inspect only plans targets after its cost, limit and source prerequisites pass.
        if (KingsRulerTokens.ConditionsMet(state, token))
            conditions.Add(Condition("Target", "ruler-action", "The bound ability has a legal target and its required figures or deck are available.",
                actions.Any(a => a.TokenId == KingsRulerTokens.Id(token.SovereignId))));
        var result = Create(state, Side.FreePeoples, KingsRulerTokens.Id(token.SovereignId), "ruler-token",
            "Use " + SovereignCatalog.All[token.SovereignId].Title + "'s Ruler token", "KINGS p.10",
            "Spend this token and the Ruler die together to perform the sovereign's second ability.",
            "Immediately before the bound Ruler action during your normal action.", ActionWindow(state, Side.FreePeoples),
            conditions.ToArray());
        if (die?.Location == DieLocation.Available)
            result.EligibleDice = [die.Id];
        return result;
    }

    private static ActionAvailabilityView Dismay(MatchState state, RulerTokenState token)
    {
        var dice = KingsDismay.Dice(state).Select(d => d.Id).ToList();
        var result = Create(state, Side.Shadow, KingsRulerTokens.Id(token.SovereignId), "dismay", "Use Dismay", "KINGS pp.17–18",
            "Remove this Dismay token and reroll one retained Ruler die.", "After both Ruler discards, before Action Resolution.",
            state.Phase == GamePhase.ActionRoll && state.Pending?.Kind == DecisionKind.Dismay,
            Condition("Condition", "dismay-enabled", "Dismay is enabled for this match.", ConfigurationRules.DismayEnabled(state.Configuration)),
            Condition("Limit", "dismay-declined", "The Dismay window has not been declined for this turn.", state.Kings!.DismayDeclinedTurn != state.Turn),
            Condition("Target", "retained-ruler", "Either side has a retained Ruler die; discarded and removed dice are excluded.", dice.Count > 0));
        result.EligibleDice = dice;
        return result;
    }
}
