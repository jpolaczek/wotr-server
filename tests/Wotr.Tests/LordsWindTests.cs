using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsCardsFixture;

namespace Wotr.Tests;

public class LordsWindTests
{
    internal static MatchState HandWindow()
    {
        var state = State();
        EventTests.GiveCard(state, Side.Shadow, "base.sh.character.01");
        EventTests.GiveCard(state, Side.Shadow, "base.sh.strategy.02");
        state = Play(state, LordsFreePeoplesCards.Wind);
        return Choose(state, WindFromWestStep.Inspect);
    }

    [Fact]
    public void ATableCardForcesRemovalAndNeverGrantsHandAccess()
    {
        var state = State();
        EventTests.GiveCard(state, Side.Shadow, "base.sh.strategy.02");
        state.Player(Side.Shadow).Table.AddRange([SmeagolRules.WeShallGetIt, ShadowCharacterCards04.Palantir]);
        state = Play(state, LordsFreePeoplesCards.Wind);
        Assert.Equal(state.Player(Side.Shadow).Table.Order(), state.Pending!.PrivateChoices.Order());
        Assert.DoesNotContain("base.sh.strategy.02", Json(state, Side.FreePeoples));
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [WindFromWestStep.Inspect] });
        state = Choose(state, SmeagolRules.WeShallGetIt);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Player(Side.Shadow).Hand);
        Assert.Contains(ShadowCharacterCards04.Palantir, state.Player(Side.Shadow).Table);
    }

    [Fact]
    public void HandAccessIsPrivatePersistentAndRevokedAfterDiscard()
    {
        var state = HandWindow();
        Assert.Equal("InspectedHand", state.Pending!.Presentation);
        Assert.Equal(Json(state, Side.FreePeoples), Json(StateCodec.Copy(state), Side.FreePeoples));
        Assert.Contains("base.sh.strategy.02", Json(state, Side.FreePeoples));
        Assert.Empty(LordsFixture.Views().For(state, Side.Shadow).Options);
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [] });
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = ["base.sh.strategy.03"] });
        state = Choose(state, "base.sh.character.01");
        Assert.DoesNotContain("base.sh.strategy.02", Json(state, Side.FreePeoples));
        Assert.Contains("base.sh.strategy.02", state.Player(Side.Shadow).Hand);
        Assert.Contains("base.sh.character.01", state.Player(Side.Shadow).Discard);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(1, state.Player(Side.Shadow).HandVersion);
    }

    [Fact]
    public void InspectionCanBeDeclinedButTheDiscardAfterInspectionIsMandatory()
    {
        var state = State();
        EventTests.GiveCard(state, Side.Shadow, "base.sh.strategy.02");
        state = Play(state, LordsFreePeoplesCards.Wind);
        Assert.DoesNotContain("base.sh.strategy.02", Json(state, Side.FreePeoples));
        var declined = Choose(state, WindFromWestStep.Decline);
        Assert.Single(declined.Player(Side.Shadow).Hand);
        Assert.Empty(declined.Resolution.Frames);
        state = Choose(state, WindFromWestStep.Inspect);
        Assert.Equal("InspectedHand", state.Pending!.Presentation);
        Assert.Single(state.Pending.PrivateChoices);
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [WindFromWestStep.Decline] });
        Assert.Empty(Choose(state, "base.sh.strategy.02").Player(Side.Shadow).Hand);
    }

    [Fact]
    public void EmptyHandAndTableFinishWithoutCreatingAnUnanswerableDecision()
    {
        var state = Play(State(), LordsFreePeoplesCards.Wind);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
    }
}
