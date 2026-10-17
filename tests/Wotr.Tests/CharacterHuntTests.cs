using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

public class CharacterHuntTests
{
    public const string Merry = "base.character.merry";
    public const string Pippin = "base.character.pippin";

    internal static MatchState Hobbits(string tile = HuntTests.Three)
    {
        var state = HuntTests.State();
        state.Fellowship.Companions = [Merry, Pippin];
        state.Fellowship.Guide = Merry;
        HuntTests.Pool(state, tile);
        return state;
    }

    [Fact]
    public void BothGuidesSeparateAsOneGroupThenGollumRevealsWithoutASecondOrdinaryCasualty()
    {
        var state = HuntTests.Move(Hobbits(), new SequenceRandom(5, 0));
        Assert.Equal("hunt.guide", state.Pending!.Window);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.merry-guide" });
        Assert.Equal(Pippin, state.Fellowship.Guide);
        Assert.False(HuntRuntime.Active(state)!.CasualtyTaken);
        state = Do(StateCodec.Copy(state), CommandKind.RespondEffect, new() { ReactionId = "character.pippin-guide" });
        Assert.Equal(CharacterRules.Gollum, state.Fellowship.Guide);
        Assert.Empty(state.Fellowship.Companions);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.gollum-reveal" });
        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
        var option = Assert.Single(new FellowshipOptions().GetOptions(state, Side.FreePeoples));
        Assert.Equal(2, option.Minimum);
        Assert.All(option.Travel, t => Assert.Equal("hollin", t.Origin));
        Reject(state, CommandKind.SeparateCompanions, new() { Ids = [Merry], Route = ["hollin"] });
        state = Do(state, CommandKind.SeparateCompanions, new() { Ids = [Merry, Pippin], Route = ["hollin", "north-dunland", "south-dunland"] });
        Assert.Equal(DecisionKind.FellowshipRoute, state.Pending!.Kind);
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.All(state.Characters, c => Assert.Equal("south-dunland", c.Location));
        state = Do(state, CommandKind.RevealFellowship, new() { Route = ["hollin", "fords-of-bruinen"] });
        Assert.False(state.Fellowship.Hidden);
        Assert.Equal(0, state.Fellowship.Progress);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(DieLocation.Hunt, state.Dice[0].Location);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TakenAliveReturnsRandomOrGuideCasualtyAndStillConsumesTheOneLoss(bool random)
    {
        var state = HuntTests.Move(Hobbits(), new SequenceRandom(5, 0));
        state = Do(state, CommandKind.DeclineReaction);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = random ? HuntResponse.RandomCompanion : HuntResponse.Guide }, new SequenceRandom(0));
        Assert.True(HuntRuntime.Active(state)!.CasualtyTaken);
        Assert.Equal("separating", state.Characters.Single().Location);
        state = Do(state, CommandKind.DeclineReaction); // Keep Pippin; no second casualty prompt.
        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
        state = Do(state, CommandKind.SeparateCompanions, new() { Ids = [Merry], Route = ["hollin"] });
        Assert.Equal(2, state.Fellowship.Corruption);
        Assert.Equal("hollin", state.Characters.Single().Location);
        Assert.Equal(new[] { Pippin }, state.Fellowship.Companions);
    }

    [Fact]
    public void NonHuntEliminationAlsoReturnsHobbitAndMordorDisablesReturn()
    {
        var state = Hobbits();
        HobbitDeparture.EliminateFromFellowship(state, Merry);
        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
        state = Do(state, CommandKind.SeparateCompanions, new() { Ids = [Merry], Route = ["hollin", "north-dunland"] });
        Assert.Equal(Pippin, state.Fellowship.Guide);
        Assert.Equal("north-dunland", state.Characters.Single().Location);
        state.Fellowship.MordorStep = 0;
        HobbitDeparture.EliminateFromFellowship(state, Pippin);
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == Pippin).Location);
        Assert.Equal(CharacterRules.Gollum, state.Fellowship.Guide);
    }

    [Fact]
    public void HobbitGuideStillReducesDamageInMordorButCannotReturnToTheMap()
    {
        var state = Hobbits();
        state.Fellowship.MordorStep = 1;
        state.Fellowship.RegionId = MordorRules.Track;
        state = HuntTests.Move(state, new SequenceRandom(0));
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.merry-guide" });
        Assert.Equal("casualties", Assert.Single(state.Characters).Location);
        Assert.Equal(2, HuntRuntime.Active(state)!.Damage);
        Assert.False(HuntRuntime.Active(state)!.CasualtyTaken);
        state = Do(state, CommandKind.DeclineReaction);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal(2, state.Fellowship.Corruption);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(2, state.Fellowship.MordorStep);
    }

    [Fact]
    public void GollumMayVoluntarilyRevealForZeroDamageButCannotCreateNegativeCorruption()
    {
        var state = Hobbits("base.hunt.standard.0.1.1");
        state.Fellowship.Companions.Clear();
        state.Fellowship.Guide = CharacterRules.Gollum;
        state = HuntTests.Move(state, new SequenceRandom(5, 0));
        Assert.Equal("hunt.guide", state.Pending!.Window);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.gollum-reveal" });
        state = Do(state, CommandKind.RevealFellowship, new() { Route = ["hollin"] });
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.False(state.Fellowship.Hidden);
    }

    [Fact]
    public void GollumImmediatelyIgnoresNumberedRevealAfterHobbitLeavesButNotEyeOrSpecialReveal()
    {
        var state = Hobbits("base.hunt.standard.2.1.1");
        state.Fellowship.Companions = [Merry];
        state = HuntTests.Move(state, new SequenceRandom(5, 0));
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.merry-guide" });
        Assert.Equal(CharacterRules.Gollum, state.Fellowship.Guide);
        Assert.False(HuntRuntime.Active(state)!.Reveal);
        Assert.Contains(new EffectOptions().GetOptions(state, Side.FreePeoples).SelectMany(o => o.EligibleIds), id => id == "character.gollum-reveal");
        state = Do(state, CommandKind.DeclineReaction);
        state = Do(state, CommandKind.SeparateCompanions, new() { Ids = [Merry], Route = ["hollin"] });
        Assert.True(state.Fellowship.Hidden);
        Assert.Equal(1, state.Fellowship.Corruption);
        foreach (var tile in HuntPool.Definitions.Values.Where(t => t.Reveal && (t.Value == "eye" || t.Pool != "standard")))
        {
            var hunt = new HuntProcedure { TileId = tile.Id, Reveal = true };
            GollumHunt.IgnoreNumberedReveal(state, hunt);
            Assert.True(hunt.Reveal);
        }
    }

    [Fact]
    public void GollumCannotRevealAnAlreadyRevealedFellowshipOrAnUnconqueredSanctuaryAtProgressZero()
    {
        var state = Hobbits();
        state.Fellowship.Companions.Clear();
        state.Fellowship.Guide = CharacterRules.Gollum;
        state.Resolution.Frames.Add(new() { Hunt = new() { Damage = 3, Reveal = false } });
        state.Fellowship.RegionId = "lorien";
        Assert.False(GollumHunt.CanReveal(state));
        state.Fellowship.Progress = 1;
        Assert.True(GollumHunt.CanReveal(state));
        state.Fellowship.Hidden = false;
        Assert.False(GollumHunt.CanReveal(state));
        state.Fellowship.Hidden = true;
        state.Fellowship.MordorStep = 0;
        Assert.True(GollumHunt.CanReveal(state));
    }
}
