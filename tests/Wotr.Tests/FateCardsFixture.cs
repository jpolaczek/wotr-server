using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Tests;

internal static class FateCardsFixture
{
    public static readonly string[] Cards =
        [FateCards.ThereAndBack, FateCards.Book, FateCards.Bardings, FateCards.Dwarves, FateCards.Orcs, FateCards.Monsters];

    public static MatchState State(string card)
    {
        var state = FateFixture.Actions(CardCatalog.All[card].Side);
        state.Fellowship.Guide = CharacterRules.Strider;
        return state;
    }

    public static CommandPayload Cost(MatchState state, string card, DieFace? face = null)
    {
        var owner = CardCatalog.All[card].Side;
        if (!state.Player(owner).Hand.Contains(card))
            EventTests.GiveCard(state, owner, card);
        var cost = face ?? CardCatalog.All[card].EventType;
        var die = state.Dice.First(d => d.Owner == owner && d.Location == DieLocation.Available);
        die.Face = cost;
        return new() { CardId = card, DieId = die.Id, CardCost = cost };
    }

    public static MatchState Play(MatchState state, string card) => KingsFixture.Do(state, CommandKind.PlayEvent, Cost(state, card));
    public static MatchState Choose(MatchState state, string id) => KingsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [id] });
    public static ActionOption Option(MatchState state) => new EffectOptions(EffectCatalog.All)
        .GetOptions(state, state.Pending!.Owner).Single(o => o.Kind == CommandKind.ChooseEffect);

    public static CommandPayload Plan(MatchState state, params (string Nation, UnitKind Kind, string Region, int Count)[] allocations)
    {
        var available = Option(state).Recruitment.ToDictionary(g => (g.Nation, g.Kind), g => new Queue<string>(g.ReserveIds));
        return new()
        {
            Placements = allocations.SelectMany(a => Enumerable.Range(0, a.Count).Select(_ => new UnitPlacementChoice
            {
                UnitId = available[(a.Nation, a.Kind)].Dequeue(), RegionId = a.Region
            })).ToList()
        };
    }

    public static void Reserve(MatchState state, string nation, UnitKind kind, int count) =>
        ShadowStrategy04Fixture.Reserve(state, nation, kind, count);

    public static MatchState Combat(string card, bool fpAttacks = true, int fpUnits = 3, int shUnits = 3)
    {
        var state = BattleFixture.State(fpAttacks ? Side.FreePeoples : Side.Shadow);
        state.Configuration.Fate = true;
        var fpRegion = fpAttacks ? BattleFixture.Origin : BattleFixture.Target;
        var shRegion = fpAttacks ? BattleFixture.Target : BattleFixture.Origin;
        for (var i = 0; i < fpUnits; i++)
            BattleFixture.Put(state, "gondor", i == 0 ? UnitKind.Elite : UnitKind.Regular, fpRegion);
        for (var i = 0; i < shUnits; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, shRegion);
        CharacterFixture.Put(state, CharacterHuntTests.Merry, fpRegion);
        EventTests.GiveCard(state, CardCatalog.All[card].Side, card);
        var ids = state.Units.Where(u => u.Location == BattleFixture.Origin).Select(u => u.Id)
            .Concat(state.Characters.Where(c => c.Location == BattleFixture.Origin).Select(c => c.Id));
        return BattleFixture.Begin(state, ids, EffectCatalog.All);
    }

    public static MatchState Reveal(MatchState state, string card, string opponent = "", params int[] samples)
    {
        var owner = CardCatalog.All[card].Side;
        if (opponent.Length > 0)
            EventTests.GiveCard(state, TurnFlow.Other(owner), opponent);
        var first = state.Pending!.Owner;
        state = KingsFixture.Do(state, CommandKind.CommitCombatCard, new() { CardId = first == owner ? card : opponent });
        return KingsFixture.Do(state, CommandKind.CommitCombatCard, new() { CardId = first == owner ? opponent : card }, samples);
    }

    public static MatchState NoRerolls(MatchState state)
    {
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = KingsFixture.Do(state, CommandKind.RerollBattle);
        return state;
    }
}
