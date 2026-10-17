using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;

namespace Wotr.Tests;

internal static class Shadow03Fixture
{
    public static void Table(MatchState state, string card)
    {
        var owner = card.StartsWith("base.sh.", StringComparison.Ordinal) ? Side.Shadow : Side.FreePeoples;
        if (!state.Player(owner).Hand.Contains(card))
            EventTests.GiveCard(state, owner, card);
        HandRules.Remove(state.Player(owner), card);
        state.Player(owner).Table.Add(card);
        state.ActiveEffects.Add(new()
        {
            Id = card,
            SourceId = card,
            Owner = owner,
            ExpiresWhen = card is ShadowCharacterCards03.Worn or ShadowCharacterCards03.Crebain ? EffectExpiry.FellowshipDeclaresSanctuary : EffectExpiry.None
        });
    }

    public static MatchState RevealedEvent(string card, bool mordor = false)
    {
        var state = BeforeEvent(card, mordor: mordor);
        state.Fellowship.Hidden = false;
        return state;
    }

    public static MatchState ResolveGuide(MatchState state)
    {
        if (state.Pending?.Kind == DecisionKind.Guide)
            state = Do(state, CommandKind.SelectGuide, new() { GuideId = CompanionRules.Guides(state)[0] });
        return state;
    }

    public static MatchState Declaration(string destination = "lorien", bool captured = false)
    {
        var state = BeforeEvent(ShadowCharacterCards03.Balrog, "moria");
        Table(state, ShadowCharacterCards03.Balrog);
        state.Fellowship.Progress = 2;
        state.Fellowship.Corruption = 3;
        if (captured)
            state.Control[destination] = Side.Shadow;
        state.Phase = GamePhase.Fellowship;
        TurnFlow.OpenFellowshipPhase(state);
        return state;
    }

    public static MatchState MoriaBattle(bool shadowAttacks = false, string fpCard = "", bool table = false)
    {
        var state = BeforeCombat(ShadowCharacterCards03.Balrog, shadowAttacks, fpCard: fpCard.Length > 0 ? fpCard : "base.fp.character.01");
        var battle = BattleRuntime.Active(state)!;
        foreach (var force in new[] { battle.Attacker, battle.Defender })
        {
            var target = force == battle.Defender ? "moria" : "hollin";
            foreach (var unit in BattleRules.Units(state, force))
                unit.Location = target;
            foreach (var character in BattleRules.Characters(state, force))
                character.Location = target;
            force.Region = target;
        }
        battle.Region = "moria";
        // Do not introduce an unrelated stronghold-defense choice in this card fixture.
        battle.Defender.Round.FieldDefenseChosen = true;
        if (table)
            Table(state, ShadowCharacterCards03.Balrog);
        return state;
    }
}
