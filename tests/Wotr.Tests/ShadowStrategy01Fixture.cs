using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.ShadowStrategyCards01;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

internal static class ShadowStrategy01Fixture
{
    public static readonly string[] Cards = [Valinor, UrukHai, Denethor, Dawn, Threats, Stormcrow];
    public static MatchState State(string card)
    {
        var state = Shadow04Fixture.State(card, card is UrukHai or Denethor);
        if (card == Valinor)
        {
            state.Control["grey-havens"] = Side.Shadow;
            BattleFixture.Put(state, "elves", UnitKind.Regular, "lorien");
            BattleFixture.Put(state, "elves", UnitKind.Elite, "lorien");
        }
        if (card == UrukHai)
            BattleFixture.Put(state, "isengard", UnitKind.Regular, "minas-tirith");
        if (card == Denethor)
            foreach (var nation in new[] { "gondor", "north" })
                BattleFixture.Put(state, nation, UnitKind.Leader, "minas-tirith").Garrison = true;
        if (card == Stormcrow)
        {
            state.Fellowship.RegionId = "minas-tirith";
            state.Nations.Single(n => n.Id == "gondor").StepsToWar = 2;
        }
        return state;
    }

    public static CommandPayload EventCost(MatchState state, string card)
    {
        var face = card is Threats or Stormcrow ? DieFace.Muster : DieFace.Army;
        var payload = Cost(state, face);
        payload.CardId = card;
        payload.CardCost = face;
        if (card == Stormcrow)
            payload.NationId = "gondor";
        return payload;
    }

    public static MatchState Play(MatchState state, string card, params int[] dice) =>
        Do(state, CommandKind.PlayEvent, EventCost(state, card), new SequenceRandom(dice.Length == 0 ? new int[20] : dice));

    public static MatchState Combat(string card, bool shadowAttacks = true, int own = 3, int enemy = 3, bool elite = false)
    {
        var state = BattleFixture.State(shadowAttacks ? Side.Shadow : Side.FreePeoples);
        for (var i = 0; i < own; i++)
            BattleFixture.Put(state, "sauron", elite && i == 0 ? UnitKind.Elite : UnitKind.Regular, shadowAttacks ? BattleFixture.Origin : BattleFixture.Target);
        for (var i = 0; i < enemy; i++)
            BattleFixture.Put(state, "gondor", UnitKind.Regular, shadowAttacks ? BattleFixture.Target : BattleFixture.Origin);
        EventTests.GiveCard(state, Side.Shadow, card);
        return BattleFixture.Begin(state, state.Units.Where(u => u.Location == BattleFixture.Origin).Select(u => u.Id));
    }

    public static CommandPayload Sacrifice(MatchState state, int count) => new()
    {
        Losses = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.Shadow)).Where(BoardRules.IsArmyUnit)
            .Take(count).Select(u => BattleFixture.Loss(u.Id)).ToList()
    };

    public static void DenethorTable(MatchState state)
    {
        Shadow03Fixture.Table(state, Denethor);
        var effect = state.ActiveEffects.Single(e => e.SourceId == Denethor);
        effect.Modifier = EffectModifier.PreventMinasTirithCombatCards;
        effect.ExpiresWhen = EffectExpiry.MinasTirithSiegeEnds;
    }
}
