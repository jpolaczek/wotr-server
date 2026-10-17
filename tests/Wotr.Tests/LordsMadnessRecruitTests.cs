using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsFixture;
using static Wotr.Tests.LordsMadnessTests;

namespace Wotr.Tests;

public class LordsMadnessRecruitTests
{
    [Fact]
    public void RecruitmentIsAdjacentToTheTargetAndPlacesBothRegularsTogether()
    {
        var state = Attack(Window(), Target, 0, 1, 2);
        Assert.Equal(new[] { "carrock", "mount-gundabad", "old-ford" }, state.Pending!.PrivateChoices.Order());
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["mount-gram"] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["carrock", "old-ford"] });
        var command = BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = ["carrock"] });
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow, command);
        Assert.Equal(2, preview.Placements.Count);
        Assert.All(preview.Placements, p => Assert.Equal("carrock", p.RegionId));
        Assert.False(preview.HasUnknownResult);
        state = Do(StateCodec.Copy(state), command.Kind, command.Payload);
        Assert.Equal(2, state.Units.Count(u => u.Location == "carrock" && u.Nation == "sauron" && u.Kind == UnitKind.Regular && !u.Garrison));
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Dice, d => d.Owner == Side.Shadow && d.Location == DieLocation.Spent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BesiegedSauronStrongholdRecruitsOutsideEvenWhenTheGarrisonIsDestroyed(bool destroy)
    {
        var state = Siege();
        state = Attack(state, "moria", destroy ? 4 : 0, 0, 0);
        if (destroy)
        {
            var unit = EventLosses.Current(state)!.Target.Units(state).Single();
            state = Do(StateCodec.Copy(state), CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(unit.Id)] });
        }
        Assert.Equal(3, state.Units.Count(u => u.Nation == "sauron" && u.Location == "moria" && !u.Garrison));
        Assert.Equal(destroy ? Side.Shadow : Side.FreePeoples, state.Control["moria"]);
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Units, u => u.Nation == "sauron" && u.Location == "moria" && u.Garrison);
    }

    internal static MatchState Siege()
    {
        var state = State();
        state.Control["moria"] = Side.FreePeoples;
        state.Units.Single(u => u.Location == "moria").Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "moria");
        return LordsShadowCardsFixture.Play(state, LordsShadowCards.Madness);
    }

    [Fact]
    public void FieldArmyInACapturedStrongholdStillRecruitsInAnAdjacentRegion()
    {
        var state = State();
        state.Control["moria"] = Side.FreePeoples;
        state = Attack(LordsShadowCardsFixture.Play(state, LordsShadowCards.Madness), "moria", 0, 0, 0);
        Assert.False(EffectRuntime.Current(state)!.Effect!.MadnessTarget!.Besieged);
        Assert.DoesNotContain("moria", state.Pending!.PrivateChoices);
        Assert.All(state.Pending.PrivateChoices, id => Assert.Contains(id, BoardRules.Regions["moria"].Neighbors));
        var region = state.Pending.PrivateChoices[0];
        state = LordsCardsFixture.Choose(state, region);
        Assert.Equal(2, state.Units.Count(u => u.Nation == "sauron" && u.Location == region));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void PartialReserveDoesNotUndoTheAttack(int available)
    {
        var state = Window();
        var deployed = state.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "reserve").Skip(available).ToArray();
        string[] regions = ["barad-dur", "dol-guldur", "nurn", "gorgoroth"];
        for (var i = 0; i < deployed.Length; i++)
            deployed[i].Location = regions[i / 10];
        state = Attack(state, Target, 0, 0, 0);
        if (available > 0)
            state = LordsCardsFixture.Choose(state, "old-ford");
        Assert.Equal(available, state.Units.Count(u => u.Location == "old-ford"));
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Events, e => e.Kind == "EventArmyRoll");
    }

    [Fact]
    public void OccupiedAndEnemyControlledDestinationsAreExcludedAndNoFreeRegionSkipsRecruitment()
    {
        var state = Window();
        foreach (var region in BoardRules.Regions[Target].Neighbors)
            BattleFixture.Put(state, "gondor", UnitKind.Regular, region);
        state = Attack(state, Target, 0, 0, 0);
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Events, e => e.Kind == "EventRecruited");
        state = Window();
        state.Control["mount-gundabad"] = Side.FreePeoples;
        state = Attack(state, Target, 0, 0, 0);
        Assert.DoesNotContain("mount-gundabad", state.Pending!.PrivateChoices);
    }

    [Fact]
    public void FriendlyBesiegersMakeAdjacentEnemyStrongholdFreeButFriendlyGarrisonDoesNot()
    {
        var state = Window();
        state.Control["mount-gundabad"] = Side.FreePeoples;
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "mount-gundabad").Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "mount-gundabad");
        state = Attack(state, Target, 0, 0, 0);
        Assert.Contains("mount-gundabad", state.Pending!.PrivateChoices);
        state = LordsCardsFixture.Choose(state, "mount-gundabad");
        Assert.Equal(3, state.Units.Count(u => u.Location == "mount-gundabad" && !u.Garrison));
        state = Window();
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "mount-gundabad").Garrison = true;
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "mount-gundabad");
        state = Attack(state, Target, 0, 0, 0);
        Assert.DoesNotContain("mount-gundabad", state.Pending!.PrivateChoices);
    }

    [Fact]
    public void RecruitmentOverTenOpensStackLossesBeforeTheActionCostCompletes()
    {
        var state = Window();
        for (var i = 0; i < 9; i++)
            BattleFixture.Put(state, "southrons", UnitKind.Regular, "carrock");
        state = Attack(state, Target, 0, 0, 0);
        state = LordsCardsFixture.Choose(state, "carrock");
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Single(state.Resolution.PaidDieIds);
        Assert.Equal(11, state.Units.Count(u => u.Location == "carrock"));
    }
}
