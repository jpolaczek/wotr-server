using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.KingsFreePeoplesCards01;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsAidTests
{
    [Fact]
    public void AidMovesMultipleGroupsThenAwakensAtTheirFinalRegionWithNoNormalPoliticalRequirement()
    {
        var state = State();
        Companion(state, Gimli, "dale");
        Companion(state, Merry, "westemnet");
        Tile(state, SovereignCatalog.Dain);
        state = Play(state, Aid);
        var option = new FellowshipOptions().GetOptions(state, Side.FreePeoples).Single(o => o.Kind == CommandKind.MoveCompanions);
        Assert.Equal(0, option.MaximumGroups);
        Assert.DoesNotContain(state.Pending!.Commands, k => k == CommandKind.SeparateCompanions);
        LordsFixture.Reject(state, CommandKind.SeparateCompanions, new() { Ids = [Boromir], Route = ["rivendell"] });
        state = Do(state, CommandKind.MoveCompanions, new() { CompanionMoves =
        [
            new() { Ids = [Gimli], Route = ["dale", "erebor"] },
            new() { Ids = [Merry], Route = ["westemnet", "edoras"] }
        ] });
        Assert.Equal("Sovereign", state.Pending!.Presentation);
        Assert.Equal(new[] { SovereignCatalog.Dain, SovereignCatalog.Theoden }, state.Pending.PrivateChoices.Order());
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Brand] });
        state = Choose(StateCodec.Copy(state), SovereignCatalog.Dain);
        Assert.Equal(SovereignStatus.Awakened, Sovereign(state, SovereignCatalog.Dain).Status);
        Assert.Equal("erebor", state.Characters.Single(c => c.Id == SovereignCatalog.Dain).Location);
        Assert.Empty(Sovereign(state, SovereignCatalog.Dain).Tiles);
        Assert.Contains(state.Kings!.Tokens, t => t.SovereignId == SovereignCatalog.Dain);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == One).Location);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void AidMaySkipMovementAndAwakenAwayFromHomeWithoutRelocation()
    {
        var state = State();
        Companion(state, Merry, "hollin");
        state.Units.Single(u => u.Id == SovereignCatalog.Thranduil).Location = "hollin";
        state.Nations.Single(n => n.Id == "elves").Active = false;
        state.Nations.Single(n => n.Id == "elves").StepsToWar = 2;
        state = Play(state, Aid);
        state = Do(state, CommandKind.MoveCompanions, new());
        Assert.Equal(SovereignStatus.Awakened, Sovereign(state, SovereignCatalog.Thranduil).Status);
        Assert.Equal("hollin", state.Characters.Single(c => c.Id == SovereignCatalog.Thranduil).Location);
        Assert.False(state.Nations.Single(n => n.Id == "elves").Active);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(KingsCardRestrictions.Welcome, SovereignCatalog.Denethor, false, false)]
    [InlineData(KingsCardRestrictions.Welcome, SovereignCatalog.Denethor, true, true)]
    [InlineData(KingsCardRestrictions.Folly, SovereignCatalog.Denethor, true, false)]
    [InlineData(KingsCardRestrictions.Folly, SovereignCatalog.Theoden, false, true)]
    public void AidAndNormalAwakeningRespectExplicitTableRestrictions(string table, string sovereign, bool war, bool allowed)
    {
        var state = State();
        var definition = SovereignCatalog.All[sovereign];
        state.Player(Side.Shadow).Table.Add(table);
        var nation = state.Nations.Single(n => n.Id == definition.Nation);
        nation.Active = true;
        nation.StepsToWar = war ? 0 : 1;
        Companion(state, Merry, definition.Home);
        Assert.Equal(allowed, SovereignAwakening.Destinations(state, Sovereign(state, sovereign)).Count > 0);
        state = Do(Play(state, Aid), CommandKind.MoveCompanions, new());
        Assert.Equal(allowed ? SovereignStatus.Awakened : SovereignStatus.Initial, Sovereign(state, sovereign).Status);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void NoCompanionOrNoEligibleSovereignCompletesOnlyThePossiblePart()
    {
        var state = Play(State(), Aid);
        Assert.Empty(state.Resolution.Frames);
        state = State();
        Companion(state, Gimli, "hollin");
        state = Do(Play(state, Aid), CommandKind.MoveCompanions, new());
        Assert.Empty(state.Resolution.Frames);
        Assert.All(state.Kings!.Sovereigns, s => Assert.Equal(SovereignStatus.Initial, s.Status));
    }
}
