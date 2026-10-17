using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.FellowshipEventCardTests;
using static Wotr.Domain.FreePeoplesCharacterCards03;

namespace Wotr.Tests;

public class EaglesCardTests
{
    internal const string Target = "south-anduin-vale";
    internal static MatchState BeforeAttack(int count = 3, bool multiple = true)
    {
        var state = BattleFixture.State();
        Put(state, "base.character.gimli", "dimrill-dale");
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "dimrill-dale");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, Target);
        Put(state, CharacterRules.WitchKing, Target);
        for (var i = 0; i < count; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Nazgul, Target);
        if (multiple)
        {
            BattleFixture.Put(state, "sauron", UnitKind.Regular, "moria");
            BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "moria");
        }
        return state;
    }

    internal static MatchState RetreatChoice()
    {
        var state = Play(BeforeAttack(), Eagles);
        return Do(state, CommandKind.ChooseEffect, new() { Ids = [Target] }, new SequenceRandom(0, 4, 3));
    }

    [Fact]
    public void EachChoiceTrailBelongsToItsChooserWhileGandalfsCompletionWindowIsOpen()
    {
        var state = BeforeAttack();
        EventTests.GiveCard(state, Side.FreePeoples, Eagles);
        var payload = Cost(state, DieFace.Event);
        payload.CardId = Eagles;
        payload.CardCost = DieFace.Event;
        state = Do(state, CommandKind.PlayEvent, payload);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [Target] }, new SequenceRandom(0, 0, 0));
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["dol-guldur"] });
        Assert.Equal("event.completed", state.Pending!.Window);
        var projection = HuntTests.Views();
        Assert.Contains(projection.For(state, Side.FreePeoples).EffectTrail, c => c.ChoiceId == Target);
        Assert.DoesNotContain(projection.For(state, Side.FreePeoples).EffectTrail, c => c.ChoiceId == "dol-guldur");
        Assert.Equal("dol-guldur", Assert.Single(projection.For(state, Side.Shadow).EffectTrail).ChoiceId);
        Assert.Null(projection.For(state, Side.Shadow).Decision);
    }

    [Fact]
    public void TargetRequiresBothArmiesAndAnActualCompanionAndOrdinaryNazgul()
    {
        var state = BeforeAttack(multiple: false);
        Assert.Single(EaglesRules.Targets(state));
        var companion = state.Characters.Single(c => c.DefinitionId == "base.character.gimli");
        companion.Garrison = true;
        Assert.Empty(EaglesRules.Targets(state));
        companion.Garrison = false;
        state.Units.Single(u => u.Location == "dimrill-dale").Kind = UnitKind.Leader;
        Assert.Empty(EaglesRules.Targets(state));
        state = BeforeAttack(0, false);
        Assert.Empty(EaglesRules.Targets(state));
        EventTests.GiveCard(state, Side.FreePeoples, Eagles);
        Cost(state, DieFace.Character);
        Reject(state, CommandKind.PlayEvent, EventTests.Play(state, Eagles, DieFace.Character).Payload);
    }

    [Fact]
    public void TargetIsChosenByFpButRetreatBelongsToShadowAndExcludesWrongStrongholds()
    {
        var state = Play(BeforeAttack(), Eagles);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Equal(new[] { Target, "moria" }.Order(), state.Pending.PrivateChoices.Order());
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["dol-guldur"] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [Target] }, new SequenceRandom(0, 4, 3));
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Equal(2, state.Units.Count(u => u.Kind == UnitKind.Nazgul && u.Location == Target));
        Assert.Equal(Target, state.Characters.Single(c => c.DefinitionId == CharacterRules.WitchKing).Location);
        Assert.Null(HuntTests.Views().For(state, Side.FreePeoples).Decision);
        Assert.Empty(HuntTests.Views().For(state, Side.Shadow).EffectTrail);
        BattleFixture.Rejected(state, BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = ["dol-guldur"] }), Side.FreePeoples);
        state.Control["minas-morgul"] = Side.FreePeoples;
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["minas-morgul"] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["orthanc"] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["umbar"] });
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow,
            BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = ["dol-guldur"] }));
        Assert.Equal(2, preview.Placements.Count);
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "dol-guldur");
        state = Do(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = ["dol-guldur"] });
        Assert.Equal(2, state.Units.Count(u => u.Kind == UnitKind.Nazgul && u.Location == "dol-guldur" && u.Garrison));
        Assert.Equal(1, state.Units.Count(u => u.Kind == UnitKind.Nazgul && u.Location == "moria"));
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
        Assert.DoesNotContain(state.Events, e => e.Kind == "PoliticsAdvanced");
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(8, 5)]
    public void RollUsesAtMostFiveDiceAndNeverCountsWitchKing(int count, int dice)
    {
        var state = Play(BeforeAttack(count, false), Eagles, Enumerable.Repeat(5, dice).ToArray());
        Assert.Equal(dice, state.RandomSamples.Count);
        Assert.Equal(count - dice, state.Units.Count(u => u.Kind == UnitKind.Nazgul && u.Location == Target));
        Assert.Equal(Target, state.Characters.Single(c => c.DefinitionId == CharacterRules.WitchKing).Location);
        if (count == dice)
            Assert.Empty(state.Resolution.Frames);
        else
            Assert.Equal(Side.Shadow, state.Pending!.Owner);
    }

    [Fact]
    public void SameRegionSiegeQualifiesButSurvivorsCannotStayInTheirCurrentStronghold()
    {
        var state = BeforeAttack(1, false);
        foreach (var unit in state.Units.Where(u => u.Location == Target))
        {
            unit.Location = "dol-guldur";
            unit.Garrison = true;
        }
        state.Characters.Single(c => c.DefinitionId == "base.character.gimli").Location = "dol-guldur";
        state.Units.Single(u => u.Location == "dimrill-dale").Location = "dol-guldur";
        state = Play(state, Eagles, 0);
        Assert.DoesNotContain("dol-guldur", state.Pending!.PrivateChoices);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["dol-guldur"] });
    }
}
