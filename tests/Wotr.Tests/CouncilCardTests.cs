using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class CouncilCardTests
{
    [Fact]
    public void AthelasUsesAlternateStridersGuideBonus()
    {
        var state = CouncilStriderTests.State(1);
        state.Fellowship.Corruption = 4;
        EventTests.GiveCard(state, Side.FreePeoples, FreePeoplesCharacterCards02.Athelas);
        var command = EventTests.Play(state, FreePeoplesCharacterCards02.Athelas, DieFace.Character);
        state = LordsFixture.Do(state, command.Kind, command.Payload, new SequenceRandom(2, 3, 0));
        Assert.Equal(2, state.Fellowship.Corruption);
    }

    [Fact]
    public void AlternateStriderQualifiesForKingEventsAndAndurilWithoutGainingHideAbility()
    {
        var state = CouncilFixture.Actions();
        var strider = LordsCharacterFixture.Put(state, CouncilCompanions.Strider, "minas-tirith", CharacterRules.Strider);
        Assert.Same(strider, KingArmyRules.King(state));
        Assert.True(new GreyCompanyCondition().CanPlay(state, Side.FreePeoples));
        Assert.True(new ChallengeOfTheKingCondition().CanPlay(state, Side.FreePeoples));
        var force = new BattleForce { Side = Side.FreePeoples, Region = strider.Location,
            Participants = [strider.Id], Round = new() { Dice = [new() { Id = "miss", Result = 2 }] } };
        var battle = new BattleState { Region = "lossarnach", Attacker = force,
            Defender = new() { Side = Side.Shadow, Region = "lossarnach" } };
        state.Resolution.Frames.Add(new() { Battle = battle });
        Assert.True(new CharacterInBattle([CharacterRules.Strider, CharacterRules.Aragorn]).CanPlay(state, battle, Side.FreePeoples));
        Assert.Equal(1, BattleRules.CaptainBonus(state, force));
        var execution = new EffectExecution { Owner = Side.FreePeoples };
        Assert.Equal("miss", Assert.Single(new AndurilStep().Candidates(state, execution)));
        var frame = new ResolutionFrame { Effect = execution };
        new AndurilStep().Choose(new(state, frame, new SequenceRandom()), "miss");
        Assert.True(force.Round.Dice.Single().ConvertedHit);
        Assert.Equal(0, BattleLeadership.Total(state, force));
    }

    [Fact]
    public void AlternateBoromirQualifiesForStewardsAndHisHorn()
    {
        var state = CouncilFixture.Actions();
        var boromir = CouncilFixture.Inside(state, "boromir");
        Assert.True(EffectCatalog.Base.Events[FreePeoplesCharacterCards02.Horn].Condition.CanPlay(state, Side.FreePeoples));
        boromir.Location = "minas-tirith";
        state.Fellowship.Companions.Remove(boromir.Id);
        Assert.True(new StewardsCondition().CanPlay(state, Side.FreePeoples));
        Assert.Same(boromir, StewardsCondition.Boromir(state));
        Assert.False(EffectCatalog.Base.Events[FreePeoplesCharacterCards02.Horn].Condition.CanPlay(state, Side.FreePeoples));
    }

    [Theory]
    [InlineData("gimli")]
    [InlineData("legolas")]
    public void ThereAndBackUsesAlternateCompanionAfterActualSeparation(string name)
    {
        var state = CouncilFixture.Actions();
        var companion = CouncilFixture.Inside(state, name);
        state.Fellowship.RegionId = "dale";
        state.Nations.Single(n => n.Id == "dwarves").StepsToWar = 2;
        state.Nations.Single(n => n.Id == "north").StepsToWar = 2;
        EventTests.GiveCard(state, Side.FreePeoples, FreePeoplesCharacterCards03.ThereAndBack);
        var command = EventTests.Play(state, FreePeoplesCharacterCards03.ThereAndBack, DieFace.Character);
        state = LordsFixture.Do(state, command.Kind, command.Payload);
        state = LordsFixture.Do(state, CommandKind.SeparateCompanions, new() { Ids = [companion.Id], Route = ["dale"] });
        state = LordsFixture.Do(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Grey });
        foreach (var id in new[] { "dwarves", "north" })
        {
            var nation = state.Nations.Single(n => n.Id == id);
            Assert.True(nation.Active);
            Assert.Equal(1, nation.StepsToWar);
        }
        Assert.Equal("dale", state.Characters.Single(c => c.Id == companion.Id).Location);
        Assert.Empty(state.Resolution.Frames);
    }
}
