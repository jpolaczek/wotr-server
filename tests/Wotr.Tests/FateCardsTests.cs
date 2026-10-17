using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.FateCardsFixture;

namespace Wotr.Tests;

public class FateCardsTests
{
    public static IEnumerable<object[]> EveryCard() => Cards.Select(c => new object[] { c });

    [Theory]
    [MemberData(nameof(EveryCard))]
    public void EveryEventRequiresItsModuleOwnerHandActionWindowAndPrintedOrEventCost(string card)
    {
        var state = State(card);
        var actor = CardCatalog.All[card].Side;
        var engine = LordsFixture.Engine();
        foreach (var face in new[] { CardCatalog.All[card].EventType, DieFace.Event })
        {
            var command = BattleFixture.Command(state, CommandKind.PlayEvent, Cost(state, card, face));
            Assert.Null(engine.Validate(state, actor, command));
            Assert.NotNull(engine.Validate(state, TurnFlow.Other(actor), command));
            Assert.True(new PreviewService(engine).Preview(state, actor, command).Legal);
        }
        LordsFixture.Reject(state, CommandKind.PlayEvent, Cost(state, card, DieFace.Army));
        var payload = Cost(state, card);
        state.Configuration.Fate = false;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Configuration.Fate = true;
        state.Player(actor).Hand.Remove(card);
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Player(actor).Hand.Add(card);
        state.Pending!.Kind = DecisionKind.Losses;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
    }

    [Theory]
    [MemberData(nameof(EveryCard))]
    public void EveryCombatRequiresTheActualFateCardOwnerModuleAndCombatWindow(string card)
    {
        var owner = CardCatalog.All[card].Side;
        var state = Combat(card, fpAttacks: owner == Side.FreePeoples);
        var payload = new CommandPayload { CardId = card };
        var command = BattleFixture.Command(state, CommandKind.CommitCombatCard, payload);
        Assert.Null(LordsFixture.Engine().Validate(state, owner, command));
        Assert.NotNull(LordsFixture.Engine().Validate(state, TurnFlow.Other(owner), command));
        state.Configuration.Fate = false;
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, payload);
        state.Configuration.Fate = true;
        state.Player(owner).Hand.Remove(card);
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, payload);
        state.Player(owner).Hand.Add(card);
        state.Pending!.Kind = DecisionKind.Action;
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, payload);
    }

    [Theory]
    [MemberData(nameof(EveryCard))]
    public void ReplacedBaseCardsCannotBeSmuggledIntoEitherUse(string card)
    {
        var replaced = CardCatalog.Fate[card].Replaces;
        var state = State(card);
        var cost = Cost(state, replaced);
        LordsFixture.Reject(state, CommandKind.PlayEvent, cost);
        var owner = CardCatalog.All[card].Side;
        state = Combat(card, fpAttacks: owner == Side.FreePeoples);
        EventTests.GiveCard(state, owner, replaced);
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, new() { CardId = replaced });
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void SixExplicitReplacementsPreserveAllEightDeckSizesWithoutKingsDuplicates(bool fate, bool kings, bool lords)
    {
        var config = new MatchConfiguration { Fate = fate, Kings = kings, Lords = lords };
        var total = 96 + (kings ? 8 : 0) + (lords ? 12 : 0);
        var state = MatchFactory.Create("fate-decks", new SequenceRandom(new int[total - 4]), config);
        var ids = state.Players.SelectMany(p => p.Decks.Values).SelectMany(d => d).ToHashSet();
        Assert.Equal(total, ids.Count);
        Assert.Equal(total - 4, state.RandomSamples.Count);
        foreach (var card in CardCatalog.Fate.Values)
        {
            Assert.Equal(fate, ids.Contains(card.Id));
            var kingsSlot = kings && card.Id is FateCards.Bardings or FateCards.Dwarves;
            Assert.Equal(!fate && !kingsSlot, ids.Contains(card.Replaces));
        }
        foreach (var slot in new[] { "19", "22" })
            Assert.Equal(kings && !fate, ids.Contains("kings.fp.strategy." + slot));
        foreach (var id in ids)
        {
            Assert.Contains(id, EffectCatalog.All.Events.Keys);
            Assert.Contains(id, EffectCatalog.All.Combats.Keys);
        }
    }

    [Theory]
    [InlineData(FateCards.Book)]
    [InlineData(FateCards.Orcs)]
    [InlineData(FateCards.Monsters)]
    public void MissingTargetsAndReservesStillFinishWithoutAnUnpaidOrStrandedAction(string card)
    {
        var state = State(card);
        foreach (var unit in state.Units.Where(u => u.Location == "reserve"))
            unit.Location = "removed";
        var payload = Cost(state, card);
        state = KingsFixture.Do(state, CommandKind.PlayEvent, payload);
        if (card == FateCards.Monsters)
            state = Choose(state, FateCards.MonstersEast);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
        Assert.Contains(card, state.Player(CardCatalog.All[card].Side).Discard);
    }
}
