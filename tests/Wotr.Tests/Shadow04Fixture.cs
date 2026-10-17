using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.ShadowCharacterCards04;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;

namespace Wotr.Tests;

internal static class Shadow04Fixture
{
    public static readonly string[] Cards = [Spells, Grond, Palantir, Wormtongue, Ringwraiths, Captain];

    public static MatchState State(string card = Ringwraiths, bool siege = false)
    {
        var state = BattleFixture.State(Side.Shadow);
        for (var i = 0; i < 3; i++)
        {
            BattleFixture.Put(state, "sauron", UnitKind.Regular, siege ? "minas-tirith" : "osgiliath");
            BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-tirith").Garrison = siege;
        }
        BattleFixture.Put(state, "sauron", UnitKind.Nazgul, siege ? "minas-tirith" : "osgiliath");
        Put(state, CharacterRules.WitchKing, siege ? "minas-tirith" : "osgiliath");
        Put(state, CharacterRules.Saruman, "orthanc");
        state.Control["minas-tirith"] = Side.FreePeoples;
        EventTests.GiveCard(state, Side.Shadow, card);
        return state;
    }

    public static MatchState Played(string card, bool siege = false, params int[] random) => Play(State(card, siege), card, random);

    public static CommandPayload Army(MatchState state, string from, string to) => new()
    {
        Moves = [new()
        {
            From = from, To = to,
            UnitIds = state.Units.Where(u => u.Location == from && !u.Garrison && BoardRules.Owner(u) == Side.Shadow).Select(u => u.Id).ToList(),
            CharacterIds = state.Characters.Where(c => c.Location == from && !c.Garrison && c.Side == Side.Shadow).Select(c => c.Id).ToList()
        }]
    };

    public static MatchState BeginGrond(bool elite = false, bool companion = false)
    {
        var state = State(Grond, true);
        if (elite)
            BattleFixture.Put(state, "sauron", UnitKind.Elite, "minas-tirith");
        if (companion)
            Put(state, "base.character.boromir", "minas-tirith").Garrison = true;
        state = Play(state, Grond);
        return Do(state, CommandKind.AttackArmy, Army(state, "minas-tirith", "minas-tirith"));
    }

    public static MatchState BlankRound(MatchState state)
    {
        state = Do(state, CommandKind.CommitCombatCard, random: new SequenceRandom(new int[10]));
        if (state.Pending?.Kind == DecisionKind.CombatCard)
            state = Do(state, CommandKind.CommitCombatCard, random: new SequenceRandom(new int[10]));
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Do(state, CommandKind.RerollBattle);
        return state;
    }

    public static void Table(MatchState state, string card)
    {
        Shadow03Fixture.Table(state, card);
        var effect = state.ActiveEffects.Single(e => e.SourceId == card);
        effect.ExpiresWhen = EffectExpiry.CharacterLeavesPlay;
        effect.TargetId = CharacterRules.InPlay(state, CharacterRules.Saruman)!.Id;
        effect.Modifier = card == Wormtongue ? EffectModifier.RestrictRohanActivation : EffectModifier.None;
    }
}
