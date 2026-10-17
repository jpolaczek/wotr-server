using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.FellowshipEventCardTests;
using static Wotr.Domain.FreePeoplesCharacterCards04;

namespace Wotr.Tests;

public class DeadMenCardTests
{
    internal static MatchState BeforeJourney(int enemies = 3, bool aragorn = false)
    {
        var state = BattleFixture.State();
        var king = Put(state, CharacterRules.Strider, "edoras");
        if (aragorn)
            CharacterLifecycle.ApplyVersion(king, CharacterCatalog.Base[CharacterRules.Aragorn]);
        Put(state, "base.character.gimli", "edoras");
        BattleFixture.Put(state, "rohan", UnitKind.Regular, "edoras");
        BattleFixture.Put(state, "rohan", UnitKind.Leader, "edoras");
        for (var i = 0; i < enemies; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, "pelargir");
        Put(state, CharacterRules.WitchKing, "pelargir");
        BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "pelargir");
        state.Control["pelargir"] = Side.Shadow;
        return state;
    }

    internal static MatchState LossChoice()
    {
        var state = Play(BeforeJourney(), DeadMen);
        return Do(state, CommandKind.ChooseEffect, new() { Ids = ["pelargir", "base.character.gimli"] }, new SequenceRandom(0));
    }

    internal static MatchState RetreatChoice()
    {
        var state = LossChoice();
        return Do(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(EventLosses.Current(state)!.Target.Units(state).First(BoardRules.IsArmyUnit).Id)] });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void KingAndOptionalCompanionsCanLeaveSiegeButArmyCannotJoin(bool aragorn)
    {
        var state = BeforeJourney(aragorn: aragorn);
        foreach (var unit in state.Units.Where(u => u.Location == "edoras"))
        {
            unit.Location = "helms-deep";
            unit.Garrison = true;
        }
        foreach (var companion in state.Characters.Where(c => c.Location == "edoras"))
        {
            companion.Location = "helms-deep";
            companion.Garrison = true;
        }
        BattleFixture.Put(state, "isengard", UnitKind.Regular, "helms-deep");
        state = Play(state, DeadMen);
        var unitId = state.Units.First(u => u.Location == "helms-deep").Id;
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["pelargir", unitId] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["pelargir", "erech"] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["base.character.gimli"] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["pelargir", "base.character.gimli"] }, new SequenceRandom(5));
        Assert.Equal(6, EventLosses.Current(state)!.Hits);
        Assert.Equal("pelargir", KingArmyRules.King(state)!.Location);
        Assert.All(state.Characters.Where(c => c.Side == Side.FreePeoples), c => Assert.False(c.Garrison));
        Assert.Equal(2, state.Units.Count(u => u.Location == "helms-deep" && u.Garrison));
    }

    [Theory]
    [InlineData("fangorn")]
    [InlineData("orthanc")]
    [InlineData("minas-tirith")]
    public void PrintedRohanConditionIsRequired(string region)
    {
        var state = BeforeJourney();
        KingArmyRules.King(state)!.Location = region;
        EventTests.GiveCard(state, Side.FreePeoples, DeadMen);
        var payload = Cost(state, DieFace.Character);
        payload.CardId = DeadMen;
        payload.CardCost = DieFace.Character;
        Reject(state, CommandKind.PlayEvent, payload);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void RetreatPrecedesOptionalRecruitmentAndOnlyUnitsCapture(int recruits)
    {
        var state = RetreatChoice();
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.DoesNotContain("lamedon", state.Pending.PrivateChoices);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["lamedon"] });
        var destination = state.Pending.PrivateChoices[0];
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [destination] });
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Equal(destination, state.Characters.Single(c => c.DefinitionId == CharacterRules.WitchKing).Location);
        Assert.Equal(Side.Shadow, state.Control["pelargir"]);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = state.Pending.PrivateChoices.Take(recruits).ToList() });
        Assert.Equal(recruits, state.Units.Count(u => u.Nation == "gondor" && u.Location == "pelargir"));
        Assert.Equal(recruits > 0 ? Side.FreePeoples : Side.Shadow, state.Control["pelargir"]);
        Assert.Single(state.RandomSamples);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Fact]
    public void NoRetreatDestroysSurvivorsAndCommandersBeforeRecruitment()
    {
        var state = LossChoice();
        foreach (var region in BoardRules.Regions["pelargir"].Neighbors)
            BattleFixture.Put(state, "gondor", UnitKind.Regular, region);
        state = Do(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(EventLosses.Current(state)!.Target.Units(state).First(BoardRules.IsArmyUnit).Id)] });
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.DoesNotContain(state.Units, u => u.Location == "pelargir");
        Assert.Equal("casualties", state.Characters.Single(c => c.DefinitionId == CharacterRules.WitchKing).Location);
        Assert.Equal(0, state.Player(Side.Shadow).ExtraDiceNextRecovery);
    }

    [Fact]
    public void NoArmyDoesNotRollOrKillLoneCommandersAndEmptyReserveCannotCapture()
    {
        var state = BeforeJourney(0);
        foreach (var unit in state.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular))
            unit.Location = "casualties";
        state = Play(state, DeadMen);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["pelargir"] });
        Assert.Empty(state.RandomSamples);
        Assert.Equal("pelargir", KingArmyRules.King(state)!.Location);
        Assert.Equal("edoras", state.Characters.Single(c => c.DefinitionId == "base.character.gimli").Location);
        Assert.Equal("pelargir", state.Characters.Single(c => c.DefinitionId == CharacterRules.WitchKing).Location);
        Assert.Equal(Side.Shadow, state.Control["pelargir"]);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void RetreatResolvesShadowStackExcessBeforeReturningToFpRecruitment()
    {
        var state = RetreatChoice();
        var target = state.Pending!.PrivateChoices[0];
        for (var i = 0; i < 10; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, target);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [target] });
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        state = Do(state, CommandKind.RemoveExcess, new() { Ids = state.Pending.PrivateChoices.Take(2).ToList() });
        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
    }
}
