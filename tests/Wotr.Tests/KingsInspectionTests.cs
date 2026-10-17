using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.KingsFreePeoplesCards01;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsInspectionTests
{
    internal static MatchState Ready(string card = Wisdom)
    {
        var state = State();
        Tile(state, SovereignCatalog.Dain);
        Tile(state, SovereignCatalog.Dain, KingsTiles.One);
        Tile(state, SovereignCatalog.Denethor, "base.hunt.standard.1.0.2");
        state = Play(state, card);
        return Choose(state, card == Wisdom ? WisdomBranchStep.Inspect : "north");
    }

    [Theory]
    [InlineData(Wisdom)]
    [InlineData(Dark)]
    public void InspectionGrantsOnlySelectedSovereignAccessAndRevokesItAfterReturningATile(string card)
    {
        var state = Ready(card);
        Assert.DoesNotContain(One, Json(state, Side.FreePeoples));
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = ["unknown"] });
        state = Choose(StateCodec.Copy(state), SovereignCatalog.Dain);
        Assert.Equal("InspectedCorruption", state.Pending!.Presentation);
        var fp = LordsFixture.Views().For(state, Side.FreePeoples);
        Assert.Contains(One, Json(state, Side.FreePeoples));
        Assert.DoesNotContain("base.hunt.standard.1.0.2", Json(state, Side.FreePeoples));
        Assert.Equal(1, fp.Sovereigns.Single(s => s.Id == SovereignCatalog.Dain).Corruption);
        Assert.Null(LordsFixture.Views().For(state, Side.Shadow).Decision);
        Assert.Empty(LordsFixture.Views().For(state, Side.Shadow).Options);
        var command = BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = [One] });
        Assert.Equal("NotAuthorized", LordsFixture.Engine().Validate(state, Side.Shadow, command));
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [One, KingsTiles.One] });
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = ["base.hunt.standard.1.0.2"] });
        state = Choose(StateCodec.Copy(state), KingsTiles.One);
        Assert.Equal(new[] { One }, Sovereign(state, SovereignCatalog.Dain).Tiles);
        Assert.DoesNotContain(One, Json(state, Side.FreePeoples));
        Assert.Null(LordsFixture.Views().For(state, Side.FreePeoples).Sovereigns.Single(s => s.Id == SovereignCatalog.Dain).Corruption);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == KingsTiles.One).Location);
        Assert.Empty(state.RandomSamples);
    }

    [Theory]
    [InlineData(Wisdom, true)]
    [InlineData(Dark, false)]
    public void OnlyWisdomAllowsKeepingInspectedTiles(string card, bool canKeep)
    {
        var state = Choose(Ready(card), SovereignCatalog.Dain);
        Assert.Equal(canKeep, state.Pending!.PrivateChoices.Contains("keep"));
        if (canKeep)
        {
            state = Choose(state, "keep");
            Assert.Equal(2, Sovereign(state, SovereignCatalog.Dain).Tiles.Count);
            Assert.DoesNotContain(One, Json(state, Side.FreePeoples));
        }
        else
            LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = ["keep"] });
    }

    [Fact]
    public void AUniqueTileIsShownBeforeMandatoryReturnAndNoTilesCompletesPartially()
    {
        var state = State();
        Tile(state, SovereignCatalog.Dain);
        state = Choose(Play(state, Dark), "north");
        Assert.Equal("InspectedCorruption", state.Pending!.Presentation);
        Assert.Contains(One, Json(state, Side.FreePeoples));
        state = Choose(state, One);
        Assert.Empty(Sovereign(state, SovereignCatalog.Dain).Tiles);
        Assert.Empty(state.Resolution.Frames);
        state = Choose(Play(State(), Dark), "north");
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(Wisdom, true)]
    [InlineData(Dark, false)]
    public void PoliticsUsesTheSelectedNationsActualActivationAndWarBoundary(string card, bool activates)
    {
        var state = State();
        var nation = state.Nations.Single(n => n.Id == "north");
        nation.Active = false;
        nation.StepsToWar = 0;
        Tile(state, SovereignCatalog.Dain);
        state = Play(state, card);
        if (card == Wisdom)
            state = Choose(state, WisdomBranchStep.Politics);
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = ["sauron"] });
        state = Choose(state, "north");
        Assert.Equal(activates, state.Nations.Single(n => n.Id == "north").Active);
        Assert.Equal(0, state.Nations.Single(n => n.Id == "north").StepsToWar);
        Assert.Equal(card == Dark, state.Pending!.Presentation == "InspectedCorruption");
        if (card == Wisdom)
            Assert.DoesNotContain(One, Json(state, Side.FreePeoples));
    }

    [Fact]
    public void DarkAdvancesWithoutActivatingAndWelcomeAllowsInspectionButNoReturn()
    {
        var state = State();
        state.Nations.Single(n => n.Id == "north").StepsToWar = 2;
        state.Nations.Single(n => n.Id == "north").Active = false;
        state.Player(Side.Shadow).Table.Add(KingsCardRestrictions.Welcome);
        Tile(state, SovereignCatalog.Dain);
        state = Choose(Play(state, Dark), "north");
        Assert.Equal(1, state.Nations.Single(n => n.Id == "north").StepsToWar);
        Assert.False(state.Nations.Single(n => n.Id == "north").Active);
        Assert.Contains(One, Json(state, Side.FreePeoples));
        Assert.Equal(new[] { "keep" }, state.Pending!.PrivateChoices);
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [One] });
        state = Choose(state, "keep");
        Assert.DoesNotContain(One, Json(state, Side.FreePeoples));
        Assert.Single(Sovereign(state, SovereignCatalog.Dain).Tiles);
    }
}
