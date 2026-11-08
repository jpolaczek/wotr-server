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

    private static MatchState DarkAt(int step)
    {
        var state = State();
        Tile(state, SovereignCatalog.Dain);
        Tile(state, SovereignCatalog.Dain, KingsTiles.One);
        Tile(state, SovereignCatalog.Denethor, "base.hunt.standard.1.0.2");
        state = Play(state, Dark);
        if (step >= 2)
            state = Choose(state, "north");
        if (step == 3)
            state = Choose(state, SovereignCatalog.Dain);
        return state;
    }

    [Theory]
    [InlineData(1, "Nation")]
    [InlineData(2, "Sovereign")]
    [InlineData(3, "InspectedCorruption")]
    public void EveryStepOfTheDarkInspectionIsMarkedWithTheCardOnly(int step, string presentation)
    {
        var state = DarkAt(step);

        Assert.Equal(presentation, state.Pending!.Presentation);
        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Equal(Dark, LordsFixture.Views().For(state, side).ActionBoundary?.DirectEventChoice?.CardId));
    }

    [Theory]
    [InlineData("nation")]
    [InlineData("sovereign")]
    [InlineData("trail")]
    public void TheDarkInspectionRefusesAnInconsistentTrail(string change)
    {
        var state = DarkAt(3);
        var execution = state.Resolution.Frames.Single().Effect!;
        switch (change)
        {
            case "nation": execution.NationId = "gondor"; break;
            case "sovereign": execution.InspectedSovereign = SovereignCatalog.Denethor; break;
            case "trail": execution.Committed.RemoveAt(1); break;
        }

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(LordsFixture.Views().For(state, side).ActionBoundary?.DirectEventChoice));
    }

    [Theory]
    [InlineData(1, "Sovereign")]
    [InlineData(2, "InspectedCorruption")]
    public void EveryStepOfTheWisdomInspectionIsMarkedWithTheCardOnly(int step, string presentation)
    {
        var state = Ready();
        if (step == 2)
            state = Choose(state, SovereignCatalog.Dain);

        Assert.Equal(presentation, state.Pending!.Presentation);
        Assert.Equal(WisdomBranchStep.Inspect, state.Resolution.Frames[^1].EffectId);
        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Equal(Wisdom, LordsFixture.Views().For(state, side).ActionBoundary?.DirectEventChoice?.CardId));
    }

    [Fact]
    public void WisdomsPoliticsBranchIsMarkedWithTheCardOnly()
    {
        var state = Choose(Play(State(), Wisdom), WisdomBranchStep.Politics);

        Assert.Equal("Nation", state.Pending!.Presentation);
        Assert.Equal(WisdomBranchStep.Politics, state.Resolution.Frames[^1].EffectId);
        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Equal(Wisdom, LordsFixture.Views().For(state, side).ActionBoundary?.DirectEventChoice?.CardId));
        state.Resolution.Frames[0].Effect!.Committed.RemoveAt(1);
        Assert.Null(LordsFixture.Views().For(state, Side.FreePeoples).ActionBoundary?.DirectEventChoice);
    }

    // Only Dain holds tiles, so the engine takes him without a choice in the trail.
    [Theory]
    [InlineData(Wisdom)]
    [InlineData(Dark)]
    public void ALoneSovereignIsInspectedWithoutAChoiceAndStillMarked(string card)
    {
        var state = State();
        Tile(state, SovereignCatalog.Dain);
        Tile(state, SovereignCatalog.Dain, KingsTiles.One);
        state = Choose(Play(state, card), card == Wisdom ? WisdomBranchStep.Inspect : "north");

        Assert.Equal("InspectedCorruption", state.Pending!.Presentation);
        Assert.Equal(2, state.Resolution.Frames[0].Effect!.Committed.Count);
        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Equal(card, LordsFixture.Views().For(state, side).ActionBoundary?.DirectEventChoice?.CardId));
        Tile(state, SovereignCatalog.Theoden);
        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(LordsFixture.Views().For(state, side).ActionBoundary?.DirectEventChoice));
    }

    [Theory]
    [InlineData("branch")]
    [InlineData("sovereign")]
    [InlineData("nested")]
    public void TheWisdomInspectionRefusesAnInconsistentTrail(string change)
    {
        var state = Choose(Ready(), SovereignCatalog.Dain);
        var root = state.Resolution.Frames[0].Effect!;
        var nested = state.Resolution.Frames[1].Effect!;
        switch (change)
        {
            case "branch": root.Committed[1] = root.Committed[1] with { ChoiceId = WisdomBranchStep.Politics }; break;
            case "sovereign": nested.InspectedSovereign = SovereignCatalog.Denethor; break;
            case "nested": nested.Committed.Add(new(Side.FreePeoples, Wisdom, "altered")); break;
        }

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(LordsFixture.Views().For(state, side).ActionBoundary?.DirectEventChoice));
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
