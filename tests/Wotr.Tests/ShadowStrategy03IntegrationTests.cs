using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards03;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy03Fixture;

namespace Wotr.Tests;

public class ShadowStrategy03IntegrationTests
{
    [Theory]
    [InlineData(HalfOrcs)]
    [InlineData(OlogHai)]
    [InlineData(HillTrolls)]
    public void RecruitmentAndReplacementRemainLegalInsideProtectedRegion(string card)
    {
        var state = Strategy01Fixture.Play(BattleFixture.State(), FreePeoplesStrategyCards01.PowerTooGreat);
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "lorien");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "lorien");
        BattleFixture.Put(state, "elves", UnitKind.Regular, "lorien").Garrison = true;
        EventTests.GiveCard(state, Side.Shadow, card);
        Assert.True(RegionProtection.Blocks(state, Side.Shadow, "lorien"));
        state = Play(state, card);
        if (state.Pending!.Kind == DecisionKind.EffectChoice)
        {
            var id = state.Units.Single(u => state.Pending.PrivateChoices.Contains(u.Id) && u.Kind == UnitKind.Elite).Id;
            state = Do(state, CommandKind.ChooseEffect, new() { Ids = [id] });
        }
        Assert.Equal(card == HillTrolls ? 2 : 1, state.Events.Count(e => e.Kind is "EventRecruited" or "EventUnitReplaced"));
        Assert.True(RegionProtection.Blocks(state, Side.Shadow, "lorien"));
    }

    [Fact]
    public void NewPowerCanRecruitInsideBesiegedOrthancDespiteNormalSarumanRestriction()
    {
        var state = State(NewPower);
        state.Characters.Single(c => c.DefinitionId == CharacterRules.Saruman).Garrison = true;
        BattleFixture.Put(state, "isengard", UnitKind.Regular, "orthanc").Garrison = true;
        BattleFixture.Put(state, "rohan", UnitKind.Regular, "orthanc");
        state = Play(state, NewPower);
        state = Do(state, CommandKind.ChooseEffect, Plan(state,
            ("isengard", UnitKind.Regular, "north-dunland", 2), ("isengard", UnitKind.Regular, "south-dunland", 2),
            ("isengard", UnitKind.Elite, "orthanc", 2)));
        Assert.Equal(2, state.Units.Count(u => u.Kind == UnitKind.Elite && u.Location == "orthanc" && u.Garrison));
        Assert.Equal(6, state.Events.Count(e => e.Kind == "EventRecruited"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ManyKingsRecruitsOnEitherSideOfUmbarSiege(bool inside)
    {
        var state = State(ManyKings);
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "umbar").Garrison = inside;
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "umbar").Garrison = !inside;
        state.Control["umbar"] = inside ? Side.Shadow : Side.FreePeoples;
        state = Play(state, ManyKings);
        state = Do(state, CommandKind.ChooseEffect, Plan(state,
            ("southrons", UnitKind.Regular, "umbar", 2), ("southrons", UnitKind.Regular, "far-harad", 2),
            ("southrons", UnitKind.Regular, "near-harad", 2)));
        Assert.Equal(2, state.Units.Count(u => u.Nation == "southrons" && u.Location == "umbar" && u.Garrison == inside));
    }

    [Fact]
    public void MissingReserveNazgulDoesNotMoveAnExistingNazgulToMinasMorgul()
    {
        var state = State(KingRevealed);
        var nazgul = BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "orthanc").Id;
        Reserve(state, "sauron", UnitKind.Nazgul, 0);
        state = Play(state, KingRevealed);
        Assert.Equal("orthanc", state.Units.Single(u => u.Id == nazgul).Location);
        Assert.Equal(5, state.Events.Count(e => e.Kind == "EventRecruited"));
        Assert.DoesNotContain(state.Units, u => u.Kind == UnitKind.Nazgul && u.Location == "minas-morgul");
    }
}
