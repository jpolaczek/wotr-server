using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards03;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow03Fixture;

namespace Wotr.Tests;

public class Shadow03HuntTests
{
    [Fact]
    public void CrebainAndEarlierMovementHaveSeparatePublicBonusesWithoutTheResolvingCost()
    {
        var state = HuntTests.State(2, "moria");
        var previous = state.Dice.Where(d => d.Owner == Side.FreePeoples).Skip(1).First();
        previous.Location = DieLocation.Hunt;
        previous.HuntOrigin = DiceRules.FellowshipMove;
        Table(state, Crebain);
        Pool(state, HuntTests.Three);
        state = HuntTests.Move(state, new SequenceRandom());
        Assert.Null(HuntOptions.View(state)!.MovementBonus);
        Assert.Null(HuntOptions.View(state)!.EffectBonus);
        Assert.Null(state.HuntReport!.Entries.Single(e => e.Kind == HuntReportKind.Started).Hunt!.MovementBonus);
        Assert.Null(state.HuntReport.Entries.Single(e => e.Kind == HuntReportKind.Started).Hunt!.EffectBonus);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = CrebainReaction }, new SequenceRandom(3, 0));
        Assert.Contains(state.Dice, d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Resolving);
        var before = StateCodec.Write(state);
        var projection = new Projection(new HuntOptions());
        var first = projection.For(state, Side.FreePeoples).Hunt!;
        var second = projection.For(state, Side.Shadow).Hunt!;
        Assert.Equal(2, first.Bonus);
        Assert.Equal(1, first.MovementBonus);
        Assert.Equal(1, first.EffectBonus);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.Equal(before, StateCodec.Write(state));
        var restored = StateCodec.Copy(state);
        var report = restored.HuntReport!.Entries.Single(e => e.Kind == HuntReportKind.Rolled).Hunt!;
        Assert.Equal(1, report.MovementBonus);
        Assert.Equal(1, report.EffectBonus);
    }

    internal static MatchState WornChoice(string companion = "base.character.gimli")
    {
        var state = BeforeEvent(ShadowCharacterCards02.FoulThing);
        Table(state, Worn);
        EventTests.GiveCard(state, Side.FreePeoples, FreePeoplesCharacterCards02.Bilbo);
        Pool(state, HuntTests.Three, HuntTests.Eye);
        state = Play(state, ShadowCharacterCards02.FoulThing, 0, state.Fellowship.Companions.IndexOf(companion));
        state = ResolveGuide(state);
        return state;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WornCanDiscardPublicTableOrRandomPrivateCharacterAndStaysInPlay(bool table)
    {
        var state = WornChoice();
        Assert.Equal("hunt.casualty", state.Pending!.Window);
        if (table)
            Table(state, FreePeoplesCharacterCards01.Mithril);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = WornReaction });
        var card = FreePeoplesCharacterCards01.Mithril;
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [table ? card : WornDiscardStep.RandomHand] }, new SequenceRandom(0));
        Assert.Contains(Worn, state.Player(Side.Shadow).Table);
        Assert.Equal(1, state.Fellowship.Corruption);
        Assert.Empty(state.Resolution.Frames);
        if (table)
        {
            Assert.DoesNotContain(card, state.Player(Side.FreePeoples).Table);
            Assert.DoesNotContain(state.ActiveEffects, e => e.SourceId == card);
        }
        else
        {
            var actual = state.Events.Single(e => e.Kind == "CardDiscarded");
            Assert.Equal(Side.FreePeoples, actual.OnlyFor);
            Assert.DoesNotContain(new Projection().For(state, Side.Shadow).History, e => e.Kind == "CardDiscarded");
        }
    }

    [Fact]
    public void WornRevealsOnlyTheHandsDecksAndNeverDiscardsAStrategyCard()
    {
        var first = WornChoice();
        first = Do(first, CommandKind.RespondEffect, new() { ReactionId = WornReaction });
        first.Player(Side.FreePeoples).Hand = [FreePeoplesCharacterCards02.Bilbo];
        var projection = new Projection(new EffectOptions());
        // UI-361: a hand card's deck is public, as the decks' backs differ (BASE 2.4.1 pp. 14, 29); which card it is is not.
        var same = StateCodec.Copy(first);
        same.Player(Side.FreePeoples).Hand = ["base.fp.character.05"];
        Assert.Equal(JsonSerializer.Serialize(projection.For(first, Side.Shadow)), JsonSerializer.Serialize(projection.For(same, Side.Shadow)));
        var second = StateCodec.Copy(first);
        second.Player(Side.FreePeoples).Hand = [FreePeoplesStrategyCards01.LastBattle];
        Assert.Equal(new Dictionary<string, int> { ["strategy"] = 1 },
            projection.For(second, Side.Shadow).Players.Single(p => p.Side == Side.FreePeoples).HandDeckCounts);
        var before = second.RandomSamples.Count;
        second = Do(second, CommandKind.ChooseEffect, new() { Ids = [WornDiscardStep.RandomHand] });
        Assert.Single(second.Player(Side.FreePeoples).Hand);
        Assert.Equal(before, second.RandomSamples.Count);
    }

    [Fact]
    public void RandomHobbitTriggersWornButGuideSeparationDoesNot()
    {
        var state = WornChoice("base.character.merry");
        Assert.Equal("hunt.casualty", state.Pending!.Window);
        state = Do(state, CommandKind.DeclineReaction);
        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
        state = Do(state, CommandKind.SeparateCompanions, new() { Ids = ["base.character.merry"], Route = ["hollin"] });
        Assert.Equal("hollin", state.Characters.Single(c => c.Id == "base.character.merry").Location);
        state = BeforeEvent(ShadowCharacterCards02.FoulThing);
        state.Fellowship.Companions = ["base.character.merry"];
        state.Fellowship.Guide = "base.character.merry";
        Table(state, Worn);
        Pool(state, RevealOne);
        state = Play(state, ShadowCharacterCards02.FoulThing, 0);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.merry-guide" });
        Assert.NotEqual("hunt.casualty", state.Pending!.Window);
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntCasualty");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CrebainDecidesBeforeAnyRollAndAddsBonusToRollAndReroll(bool use)
    {
        var state = HuntTests.State(2, "moria");
        state.RandomSamples.Clear();
        Table(state, Crebain);
        Pool(state, HuntTests.Three);
        state = HuntTests.Move(state, new SequenceRandom());
        Assert.Equal("hunt.roll", state.Pending!.Window);
        Assert.Empty(state.RandomSamples);
        state = Do(state, use ? CommandKind.RespondEffect : CommandKind.DeclineReaction,
            use ? new() { ReactionId = CrebainReaction } : new(), new SequenceRandom(4, 0));
        Assert.Equal(use ? 1 : 0, HuntRuntime.Active(state)!.Bonus);
        Assert.Equal(use ? 1 : 0, HuntRuntime.Active(state)!.EyeDamage);
        Assert.Equal(DecisionKind.HuntReroll, state.Pending!.Kind);
        state = Do(state, CommandKind.RerollHunt, new() { Ids = ["hunt-die-1"] }, new SequenceRandom(use ? [4, 0] : [4]));
        if (use)
        {
            Assert.Equal(2, HuntRuntime.Active(state)!.EyeDamage);
            Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        }
        Assert.Equal(!use, state.Player(Side.Shadow).Table.Contains(Crebain));
    }

    [Fact]
    public void CrebainAlsoAppliesToStrikeButNotToAutomaticTilesOrMordor()
    {
        var state = Shadow02StrikeTests.StrikeChoice(1);
        Table(state, Crebain);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [NazgulStrikeStep.Hunt] });
        Assert.Equal("hunt.roll", state.Pending!.Window);
        state = BeforeEvent(ShadowCharacterCards01.OrcPatrol);
        Table(state, Crebain);
        Pool(state, HuntTests.Eye);
        state = Play(state, ShadowCharacterCards01.OrcPatrol, 0);
        Assert.Contains(Crebain, state.Player(Side.Shadow).Table);
        Assert.Empty(state.Resolution.Frames);
        state = HuntTests.State(2);
        state.Fellowship.MordorStep = 1;
        state.Fellowship.RegionId = MordorRules.Track;
        Table(state, Crebain);
        Pool(state, HuntTests.Three);
        state = HuntTests.Move(state, new SequenceRandom(0));
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        Assert.Contains(Crebain, state.Player(Side.Shadow).Table);
    }
}
