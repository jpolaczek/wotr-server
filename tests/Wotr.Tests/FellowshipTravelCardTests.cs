using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Domain.FreePeoplesCharacterCards02;
using static Wotr.Tests.FellowshipEventCardTests;

namespace Wotr.Tests;

public class FellowshipTravelCardTests
{
    internal static MatchState AnotherWayChoice(bool hidden = true, bool mordor = false)
    {
        var state = HuntTests.State(0);
        state.Fellowship.Companions.Clear();
        state.Fellowship.Guide = CharacterRules.Gollum;
        state.Fellowship.Hidden = hidden;
        state.Fellowship.Corruption = 3;
        if (mordor)
        {
            state.Fellowship.MordorStep = 0;
            state.Fellowship.RegionId = MordorRules.Track;
        }
        HuntTests.Pool(state, HuntTests.Three);
        return Play(state, AnotherWay);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void AnotherWayHealsBeforeOptionalLegalMoveOrHideAndSpendsEventDie(bool hidden, bool mordor)
    {
        var state = AnotherWayChoice(hidden, mordor);
        Assert.Equal(2, state.Fellowship.Corruption);
        var choice = hidden ? AnotherWayStep.Move : AnotherWayStep.Hide;
        Assert.Equal(new[] { AnotherWayStep.Stay, choice }, state.Pending!.PrivateChoices);
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = [choice] }));
        Assert.Equal(hidden && mordor, preview.HasUnknownResult);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [hidden ? AnotherWayStep.Hide : AnotherWayStep.Move] });
        state = Do(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = [choice] }, new SequenceRandom(0));
        if (hidden && mordor)
        {
            Assert.Equal("hunt.guide", state.Pending!.Window);
            state = Do(state, CommandKind.DeclineReaction);
        }
        Assert.True(state.Fellowship.Hidden);
        Assert.Equal(mordor && hidden ? 5 : 2, state.Fellowship.Corruption);
        Assert.Equal(mordor ? hidden ? 1 : 0 : null, state.Fellowship.MordorStep);
        Assert.Equal(mordor ? 0 : hidden ? 1 : 0, state.Fellowship.Progress);
        MordorProjectionTests.AssertActivity(state, mordor ? true : null);
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void AnotherWayMayStopAfterHealingAndOtherGuidesDoNotOfferMovement()
    {
        var state = Do(AnotherWayChoice(), CommandKind.ChooseEffect, new() { Ids = [AnotherWayStep.Stay] });
        Assert.Equal(0, state.Fellowship.Progress);
        Assert.Equal(2, state.Fellowship.Corruption);
        state = Play(TurnTests.Actions(), AnotherWay);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(0, state.Fellowship.Corruption);
    }

    internal static MatchState AloneChoice()
    {
        var state = TurnTests.Actions();
        state.Fellowship.Corruption = 2;
        return Play(state, Alone);
    }

    [Fact]
    public void AloneSeparatesOneGroupWithOneExtraRegionAndHealsOnlyAfterGuideSelection()
    {
        var state = AloneChoice();
        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
        Assert.Equal(2, state.Fellowship.Corruption);
        var option = new FellowshipOptions().GetOptions(state, Side.FreePeoples).Single();
        Assert.Equal(2, option.Travel.Single(t => t.Id == CharacterHuntTests.Merry).Range);
        Reject(state, CommandKind.SeparateCompanions, new() { Ids = [CharacterHuntTests.Merry], Route = ["rivendell", "fords-of-bruinen", "hollin", "moria"] });
        state = Do(StateCodec.Copy(state), CommandKind.SeparateCompanions,
            new() { Ids = [CharacterHuntTests.Merry, "base.character.pippin"], Route = ["rivendell", "fords-of-bruinen", "hollin"] });
        Assert.All(state.Characters, c => Assert.Equal("hollin", c.Location));
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        Assert.Equal(2, state.Fellowship.Corruption);
        state = Do(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider });
        Assert.Equal(1, state.Fellowship.Corruption);
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void AloneCannotCrossShadowStrongholdAndCannotReuseAnOutsideCompanion()
    {
        var state = TurnTests.Actions();
        state.Fellowship.RegionId = "hollin";
        Put(state, "base.character.gimli", "hollin");
        state = Play(state, Alone);
        Reject(state, CommandKind.SeparateCompanions, new() { Ids = [CharacterRules.Grey], Route = ["hollin", "moria", "dimrill-dale"] });
        Reject(state, CommandKind.SeparateCompanions, new() { Ids = ["base.character.gimli"], Route = ["hollin"] });
        state = Do(state, CommandKind.SeparateCompanions, new() { Ids = [CharacterRules.Grey], Route = ["hollin", "moria"] });
        Assert.Equal("moria", state.Characters.Single(c => c.Id == CharacterRules.Grey).Location);
    }

    [Fact]
    public void AloneInMordorRemovesGroupThenGollumBecomesGuideAndHealsEvenFromZero()
    {
        var state = TurnTests.Actions();
        state.Fellowship.MordorStep = 0;
        state.Fellowship.RegionId = MordorRules.Track;
        state = Play(state, Alone);
        var ids = state.Fellowship.Companions.ToList();
        state = Do(state, CommandKind.SeparateCompanions, new() { Ids = ids });
        Assert.Empty(state.Fellowship.Companions);
        Assert.Equal(CharacterRules.Gollum, state.Fellowship.Guide);
        Assert.All(state.Characters, c => Assert.Equal("casualties", c.Location));
        Assert.Equal(0, state.Fellowship.Corruption);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        EventTests.GiveCard(state, Side.FreePeoples, Alone);
        var payload = Cost(state, DieFace.Character);
        payload.CardId = Alone;
        payload.CardCost = DieFace.Character;
        Reject(state, CommandKind.PlayEvent, payload);
    }
}
