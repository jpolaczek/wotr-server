using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards02;
using static Wotr.Domain.FreePeoplesCharacterCards01;
using static Wotr.Domain.FreePeoplesCharacterCards02;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow02TravelTests;

namespace Wotr.Tests;

public class Shadow02StrikeTests
{
    internal static MatchState StrikeChoice(int huntDice = 0, string region = "hollin")
    {
        var state = BeforeTravel(Strike, region);
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow).TakeLast(huntDice))
        {
            die.Location = DieLocation.Hunt;
            die.HuntOrigin = "allocated";
        }
        state = Play(state, Strike);
        var id = NazgulEventTravel.Option(state).EligibleIds[0];
        return Do(state, CommandKind.MoveShadowCharacters, Flight(state, region, id));
    }

    [Fact]
    public void StrikeCanDiscardOnlyOnePublicCharacterTableCardIncludingItsLastingEffects()
    {
        var state = StrikeChoice();
        state.Player(Side.FreePeoples).Table.AddRange([Axe, Staff, "base.fp.strategy.21"]);
        state.ActiveEffects.Add(new() { Id = "axe-protection", Owner = Side.FreePeoples, SourceId = Axe });
        state.ActiveEffects.Add(new() { Id = "axe-marker", Owner = Side.FreePeoples, SourceId = Axe });
        EventTests.GiveCard(state, Side.FreePeoples, Mithril);
        var options = Choices(state);
        Assert.Equal(new[] { Axe, Staff, NazgulStrikeStep.Hunt, NazgulStrikeStep.Decline }, options.EligibleIds);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [Mithril] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["base.fp.strategy.21"] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [Axe, Staff] });
        var payload = new CommandPayload { Ids = [Axe] };
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow, BattleFixture.Command(state, CommandKind.ChooseEffect, payload));
        Assert.Equal(new[] { Axe }, preview.RemovedIds);
        state = Do(state, CommandKind.ChooseEffect, payload);
        Assert.DoesNotContain(Axe, state.Player(Side.FreePeoples).Table);
        Assert.Single(state.Player(Side.FreePeoples).Discard, id => id == Axe);
        Assert.DoesNotContain(state.ActiveEffects, e => e.SourceId == Axe);
        Assert.Contains(Staff, state.Player(Side.FreePeoples).Table);
        Assert.Empty(state.RandomSamples);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void OptionalStrikeCanFinishEvenWithLegalHuntAndDiscard()
    {
        var state = StrikeChoice(2);
        state.Player(Side.FreePeoples).Table.Add(Staff);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [NazgulStrikeStep.Decline] });
        Assert.Empty(state.RandomSamples);
        Assert.Contains(Staff, state.Player(Side.FreePeoples).Table);
        Assert.Equal(2, state.Fellowship.Progress);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void FailedStrikeHuntNeverDrawsTileAndDoesNotAdvanceOrAddFpHuntDie()
    {
        var state = StrikeChoice(1);
        var dice = state.Dice.Select(d => (d.Id, d.Location)).ToArray();
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [NazgulStrikeStep.Hunt] }, new SequenceRandom(0));
        Assert.True(HuntRuntime.Active(state)!.Rolled);
        Assert.False(HuntRuntime.Active(state)!.Movement);
        Assert.Equal(new[] { "nazgul" }, HuntRuntime.Active(state)!.RerollSources);
        state = Do(state, CommandKind.RerollHunt, new() { Ids = ["hunt-die-0"] }, new SequenceRandom(0));
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntTileDrawn");
        Assert.Equal(2, state.RandomSamples.Count);
        Assert.Equal(2, state.Fellowship.Progress);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Equal(dice.Count(d => d.Location == DieLocation.Hunt), state.Dice.Count(d => d.Location == DieLocation.Hunt));
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void StrikeUsesExistingFpBonusAllRerollSourcesAndEyeCountsSuccesses()
    {
        var state = StrikeChoice(3, "moria");
        var fpDie = state.Dice.First(d => d.Owner == Side.FreePeoples);
        fpDie.Location = DieLocation.Hunt;
        fpDie.HuntOrigin = DiceRules.FellowshipMove;
        Pool(state, HuntTests.Eye);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [NazgulStrikeStep.Hunt] }, new SequenceRandom(4, 0, 1));
        var hunt = HuntRuntime.Active(state)!;
        Assert.Equal(1, hunt.Bonus);
        var roll = HuntOptions.View(state)!;
        Assert.Equal(1, roll.MovementBonus);
        Assert.Equal(0, roll.EffectBonus);
        Assert.Equal(new[] { "stronghold", "army", "nazgul" }, hunt.RerollSources);
        Assert.Equal(2, new HuntOptions().GetOptions(state, Side.Shadow).Single().Maximum);
        state = Do(state, CommandKind.RerollHunt, new() { Ids = ["hunt-die-1"] }, new SequenceRandom(5, 0));
        hunt = HuntRuntime.Active(state)!;
        Assert.Equal(2, hunt.EyeDamage);
        Assert.Equal(2, hunt.Damage);
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        Assert.Equal(2, state.Fellowship.Progress);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal(2, state.Fellowship.Corruption);
        Assert.Equal(DecisionKind.FellowshipRoute, state.Pending!.Kind);
        Assert.Single(state.Dice, d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Hunt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StaffReactionExistsOnlyAfterSuccessfulHuntRoll(bool success)
    {
        var state = StrikeChoice(1);
        state.Player(Side.FreePeoples).Table.Add(Staff);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [NazgulStrikeStep.Hunt] }, new SequenceRandom(success ? 5 : 0));
        if (!success)
        {
            state = Do(state, CommandKind.RerollHunt);
            Assert.Empty(state.Resolution.Frames);
            Assert.Contains(Staff, state.Player(Side.FreePeoples).Table);
            return;
        }
        Assert.Equal("hunt.draw", state.Pending!.Window);
        var reaction = new EffectOptions().GetOptions(state, Side.FreePeoples).Single(o => o.Kind == CommandKind.RespondEffect).EligibleIds.Single();
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = reaction });
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntTileDrawn");
        Assert.Empty(state.Resolution.Frames);
    }
}
