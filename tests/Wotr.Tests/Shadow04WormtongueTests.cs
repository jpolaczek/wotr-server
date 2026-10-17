using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards04;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow04Fixture;

namespace Wotr.Tests;

public class Shadow04WormtongueTests
{
    internal static MatchState Passive()
    {
        var state = State(Wormtongue);
        var rohan = state.Nations.Single(n => n.Id == "rohan");
        rohan.Active = false;
        rohan.StepsToWar = 1;
        return Play(state, Wormtongue);
    }

    [Theory]
    [InlineData("edoras", true)]
    [InlineData("helms-deep", true)]
    [InlineData("fords-of-isen", false)]
    [InlineData("eastemnet", false)]
    [InlineData("minas-tirith", false)]
    public void AttacksActivateOnlyAtTheTwoSpecifiedSettlementsAtActionEnd(string region, bool activates)
    {
        var state = Passive();
        var unit = BattleFixture.Put(state, "rohan", UnitKind.Regular, region);
        PoliticalRules.ArmyAttacked(state, [unit], PoliticalAttack.ArmyBattle);
        Assert.False(state.Nations.Single(n => n.Id == "rohan").Active);
        Assert.Contains(Wormtongue, state.Player(Side.Shadow).Table);
        PoliticalReactions.Complete(state);
        Assert.Equal(activates, state.Nations.Single(n => n.Id == "rohan").Active);
        Assert.Equal(activates ? 0 : 1, state.Nations.Single(n => n.Id == "rohan").StepsToWar);
        Assert.Equal(!activates, state.Player(Side.Shadow).Table.Contains(Wormtongue));
    }

    [Theory]
    [InlineData("edoras")]
    [InlineData("helms-deep")]
    [InlineData("eastemnet")]
    public void EnteringEmptyRohanCannotActivateAndCaptureCannotPassThePassiveLimit(string region)
    {
        var state = Passive();
        state.Nations.Single(n => n.Id == "rohan").StepsToWar = 2;
        PoliticalRules.EnemyEntered(state, region, Side.Shadow);
        PoliticalRules.SettlementCaptured(state, region, Side.FreePeoples, Side.Shadow);
        PoliticalReactions.Complete(state);
        Assert.False(state.Nations.Single(n => n.Id == "rohan").Active);
        Assert.Equal(region == "eastemnet" ? 2 : 1, state.Nations.Single(n => n.Id == "rohan").StepsToWar);
        Assert.Contains(Wormtongue, state.Player(Side.Shadow).Table);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeclarationRequiresAnUnconqueredRohanCityOrStronghold(bool captured)
    {
        var state = Passive();
        state.Control["edoras"] = captured ? Side.Shadow : Side.FreePeoples;
        PoliticalRules.FellowshipDeclared(state, "edoras");
        Assert.Equal(!captured, state.Nations.Single(n => n.Id == "rohan").Active);
        Assert.Equal(captured, state.Player(Side.Shadow).Table.Contains(Wormtongue));
    }

    [Theory]
    [InlineData(CharacterRules.Strider, false)]
    [InlineData(CharacterRules.Aragorn, true)]
    [InlineData("base.character.merry", true)]
    [InlineData(CharacterRules.Grey, true)]
    [InlineData("base.character.boromir", false)]
    public void OnlyAppropriateCompanionCanActivateIncludingAZeroMoveInsideSiege(string companion, bool activates)
    {
        var state = Passive();
        var figure = Put(state, companion, "helms-deep");
        figure.Garrison = true;
        BattleFixture.Put(state, "rohan", UnitKind.Regular, "helms-deep").Garrison = true;
        BattleFixture.Put(state, "isengard", UnitKind.Regular, "helms-deep");
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var payload = Cost(state, DieFace.Character);
        payload.CompanionMoves = [new() { Ids = [figure.Id], Route = ["helms-deep"] }];
        state = Do(state, CommandKind.MoveCompanions, payload);
        Assert.Equal(activates, state.Nations.Single(n => n.Id == "rohan").Active);
        Assert.Equal(!activates, state.Player(Side.Shadow).Table.Contains(Wormtongue));
        Assert.True(state.Characters.Single(c => c.Id == figure.Id).Garrison);
    }

    [Fact]
    public void EventAndWitchKingActivationAreBlockedButAdvancementIsStillAllowed()
    {
        var state = Passive();
        state.Nations.Single(n => n.Id == "rohan").StepsToWar = 3;
        PoliticalRules.Activate(state, "rohan");
        PoliticalRules.Advance(state, "rohan", PoliticalAdvanceCause.Event);
        PoliticalRules.Advance(state, "rohan", PoliticalAdvanceCause.Event);
        PoliticalRules.Advance(state, "rohan", PoliticalAdvanceCause.Event);
        Assert.False(state.Nations.Single(n => n.Id == "rohan").Active);
        Assert.Equal(1, state.Nations.Single(n => n.Id == "rohan").StepsToWar);
        Assert.Contains(Wormtongue, state.Player(Side.Shadow).Table);
        TableCardRules.Discard(state, Side.Shadow, Wormtongue);
        Assert.False(state.Nations.Single(n => n.Id == "rohan").Active);
    }

    [Fact]
    public void SarumanDeathDuringBattleUnblocksStoredReactionOnlyAtEndOfAction()
    {
        var state = Passive();
        for (var i = 0; i < 3; i++)
            BattleFixture.Put(state, "isengard", UnitKind.Regular, "orthanc");
        BattleFixture.Put(state, "rohan", UnitKind.Regular, "fords-of-isen");
        TurnFlow.SelectNext(state, Side.Shadow);
        var payload = Army(state, "orthanc", "fords-of-isen");
        payload.DieId = Cost(state, DieFace.Army).DieId;
        state = Do(state, CommandKind.AttackArmy, payload);
        Assert.False(state.Nations.Single(n => n.Id == "rohan").Active);
        CharacterLifecycle.Eliminate(state, CharacterRules.InPlay(state, CharacterRules.Saruman)!);
        state = BlankRound(StateCodec.Copy(state));
        Assert.DoesNotContain(Wormtongue, state.Player(Side.Shadow).Table);
        Assert.False(state.Nations.Single(n => n.Id == "rohan").Active);
        state = Do(state, CommandKind.StopBattle);
        Assert.True(state.Nations.Single(n => n.Id == "rohan").Active);
        Assert.Equal(0, state.Nations.Single(n => n.Id == "rohan").StepsToWar);
        Assert.Empty(state.PoliticalReactions);
    }

    [Fact]
    public void PlayingAfterRohanIsActiveEndsTheEffectImmediately()
    {
        var state = Play(State(Wormtongue), Wormtongue);
        Assert.True(state.Nations.Single(n => n.Id == "rohan").Active);
        Assert.DoesNotContain(Wormtongue, state.Player(Side.Shadow).Table);
        Assert.Contains(Wormtongue, state.Player(Side.Shadow).Discard);
    }
}
