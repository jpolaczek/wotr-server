using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public sealed class DirectEventChoiceFamilyTests
{
    private static readonly string[] Cards =
    [
        FreePeoplesStrategyCards03.CelebornsGaladhrim,
        FreePeoplesStrategyCards01.SpiritOfMordor,
        FreePeoplesStrategyCards01.FaramirsRangers,
        KingsCards02.Villages,
        KingsCards04.Enemy,
        FateCards.Dwarves,
        ShadowStrategyCards03.HillTrolls,
        ShadowStrategyCards03.ManyKings,
        ShadowStrategyCards03.NewPower,
        ShadowStrategyCards04.MistyMountains,
        ShadowStrategyCards04.Horde
    ];

    public static TheoryData<string> EachCard()
    {
        var data = new TheoryData<string>();
        foreach (var card in Cards)
            data.Add(card);
        return data;
    }

    public static TheoryData<string, string> EachCardAndAlteration()
    {
        var data = new TheoryData<string, string>();
        foreach (var card in Cards)
            foreach (var change in new[] { "cursor", "payment", "public-play", "selection" })
                data.Add(card, change);
        return data;
    }

    [Theory]
    [MemberData(nameof(EachCard))]
    public void MarksTheFirstChoiceWithOnlyThePublicCardAndPayment(string card)
    {
        var state = Played(card);

        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        Assert.Equal(card, state.Pending.SourceId);
        var cost = CardCatalog.All[card].EventType;
        Assert.All(Enum.GetValues<Side>(), side =>
        {
            var view = GameRuntime.Projection(EffectCatalog.All).For(state, side);
            Assert.Equal(31, view.ActionBoundary?.Version);
            var context = Assert.IsType<DirectEventChoiceView>(view.ActionBoundary?.DirectEventChoice);
            Assert.Equal(card, context.CardId);
            Assert.Equal(cost, context.CardCost);
        });
    }

    [Theory]
    [MemberData(nameof(EachCardAndAlteration))]
    public void RefusesAlteredEvidence(string card, string change)
    {
        var state = Played(card);
        var execution = state.Resolution.Frames[0].Effect!;
        switch (change)
        {
            case "cursor": state.Resolution.Frames[0].NextStep = 0; break;
            case "payment":
                state.Dice.Single(die => die.Id == state.Resolution.PaidDieIds[0]).Location = DieLocation.Spent;
                break;
            case "public-play": state.Events.RemoveAll(item => item.Kind == "EventPlayed"); break;
            case "selection": execution.TargetIds = ["altered"]; break;
        }

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.DirectEventChoice));
    }

    [Theory]
    [InlineData(FreePeoplesStrategyCards01.SpiritOfMordor)]
    [InlineData(FreePeoplesStrategyCards01.FaramirsRangers)]
    public void TheMarkerEndsWithTheFirstChoice(string card)
    {
        var state = Played(card);
        var region = state.Pending!.PrivateChoices[0];

        state = KingsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [region] }, 5, 5, 5, 5, 5);

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, side).ActionBoundary?.DirectEventChoice));
    }

    private static MatchState Played(string card)
    {
        var state = Prepared(card);
        var side = CardCatalog.All[card].Side;
        EventTests.GiveCard(state, side, card);
        var die = state.Dice.First(item => item.Owner == side && item.Location == DieLocation.Available);
        var cost = CardCatalog.All[card].EventType;
        die.Face = cost;
        return KingsFixture.Do(state, CommandKind.PlayEvent, new() { CardId = card, DieId = die.Id, CardCost = cost });
    }

    private static MatchState Prepared(string card)
    {
        switch (card)
        {
            case KingsCards02.Villages: return KingsCardsFixture.State();
            case KingsCards04.Enemy: return KingsCardsFixture.State(Side.Shadow);
            case FateCards.Dwarves: return FateCardsFixture.State(card);
        }

        var state = BattleFixture.State(CardCatalog.All[card].Side);
        switch (card)
        {
            case FreePeoplesStrategyCards01.SpiritOfMordor:
                foreach (var region in new[] { "orthanc", "dol-guldur" })
                {
                    BattleFixture.Put(state, "isengard", UnitKind.Regular, region);
                    BattleFixture.Put(state, "sauron", UnitKind.Regular, region);
                }
                break;
            case FreePeoplesStrategyCards01.FaramirsRangers:
                BattleFixture.Put(state, "sauron", UnitKind.Regular, "osgiliath");
                BattleFixture.Put(state, "sauron", UnitKind.Regular, "north-ithilien");
                break;
            case ShadowStrategyCards03.HillTrolls:
                state.Nations.Single(nation => nation.Id == "sauron").StepsToWar = 0;
                for (var count = 0; count < 3; count++)
                    BattleFixture.Put(state, "sauron", UnitKind.Regular, "barad-dur");
                break;
            case ShadowStrategyCards03.NewPower:
                CharacterFixture.Put(state, CharacterRules.Saruman, "orthanc");
                break;
            case ShadowStrategyCards04.Horde:
                state.Nations.Single(nation => nation.Id == "southrons").StepsToWar = 0;
                break;
        }
        return state;
    }
}
