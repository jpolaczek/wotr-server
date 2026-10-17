using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class FateGimliAbilityTests
{
    [Theory]
    [InlineData(FateGimli.Standard, "iron-hills")]
    [InlineData(FateGimli.Standard, "ered-luin")]
    [InlineData(FateGimli.Alternate, "iron-hills")]
    [InlineData(FateGimli.Alternate, "ered-luin")]
    public void BothPrintedLocationsAllowAnyActionDieIncludingUnderShadowControl(string definition, string region)
    {
        foreach (var face in new[] { DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.ArmyMuster, DieFace.Event, DieFace.WillOfTheWest })
        {
            var state = FateGimliFixture.Actions(definition, region);
            state.Control[region] = Side.Shadow;
            var nation = state.Nations.Single(n => n.Id == "dwarves");
            nation.Active = false;
            nation.StepsToWar = 2;
            var p = FateGimliFixture.Ability(state, face);
            var before = StateCodec.Write(state);
            var preview = new PreviewService(LordsFixture.Engine(EffectCatalog.All)).Preview(state, Side.FreePeoples,
                BattleFixture.Command(state, CommandKind.UseCharacterAbility, p));
            Assert.True(preview.Legal);
            Assert.Equal(definition == FateGimli.Alternate, Assert.Single(preview.PoliticsChanges).Active);
            Assert.Equal(before, StateCodec.Write(state));
            state = KingsFixture.Do(state, CommandKind.UseCharacterAbility, p);
            nation = state.Nations.Single(n => n.Id == "dwarves");
            Assert.Equal(definition == FateGimli.Alternate, nation.Active);
            Assert.Equal(1, nation.StepsToWar);
            Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == p.DieId).Location);
        }
    }

    [Theory]
    [InlineData(FateGimli.Standard, "erebor")]
    [InlineData(FateGimli.Standard, "carrock")]
    [InlineData(FateGimli.Alternate, "erebor")]
    [InlineData(FateGimli.Alternate, "carrock")]
    public void NeitherVersionUsesEreborOrOtherUnprintedRegions(string definition, string region)
    {
        var state = FateGimliFixture.Actions(definition, region);
        LordsFixture.Reject(state, CommandKind.UseCharacterAbility, FateGimliFixture.Ability(state));
    }

    [Theory]
    [InlineData(FateGimli.Standard)]
    [InlineData(FateGimli.Alternate)]
    public void AbilityRejectsUnavailableForeignMissingDiceAndWrongWindowsWithoutMutation(string definition)
    {
        foreach (var invalid in new[] { "spent", "foreign", "missing", "wrong-window", "wrong-side", "wrong-target", "old-ability" })
        {
            var state = FateGimliFixture.Actions(definition);
            var p = FateGimliFixture.Ability(state);
            switch (invalid)
            {
                case "spent": state.Dice.Single(d => d.Id == p.DieId).Location = DieLocation.Spent; break;
                case "foreign": p.DieId = state.Dice.First(d => d.Owner == Side.Shadow).Id; break;
                case "missing": p.DieId = ""; break;
                case "wrong-window": state.Pending!.Kind = DecisionKind.Guide; break;
                case "wrong-side": state.Pending!.Owner = Side.Shadow; break;
                case "wrong-target": p.Ids = ["erebor"]; break;
                case "old-ability": p.AbilityId = "politics-dwarves-at-erebor"; break;
            }
            LordsFixture.Reject(state, CommandKind.UseCharacterAbility, p);
        }
    }

    [Fact]
    public void RenownCannotActivateAnInactiveNationAtTheLastStepButMessengerCan()
    {
        var standard = FateGimliFixture.Actions(FateGimli.Standard);
        standard.Nations.Single(n => n.Id == "dwarves").Active = false;
        standard.Nations.Single(n => n.Id == "dwarves").StepsToWar = 1;
        LordsFixture.Reject(standard, CommandKind.UseCharacterAbility, FateGimliFixture.Ability(standard));
        var alternate = FateGimliFixture.Actions(FateGimli.Alternate);
        alternate.Nations.Single(n => n.Id == "dwarves").Active = false;
        alternate.Nations.Single(n => n.Id == "dwarves").StepsToWar = 1;
        alternate = KingsFixture.Do(alternate, CommandKind.UseCharacterAbility, FateGimliFixture.Ability(alternate));
        Assert.True(alternate.Nations.Single(n => n.Id == "dwarves").Active);
        Assert.Equal(0, alternate.Nations.Single(n => n.Id == "dwarves").StepsToWar);
        TurnFlow.SelectNext(alternate, Side.FreePeoples);
        LordsFixture.Reject(alternate, CommandKind.UseCharacterAbility, FateGimliFixture.Ability(alternate));
    }

    [Theory]
    [InlineData(FateGimli.Standard, 2, "dwarves")]
    [InlineData(FateGimli.Alternate, 1, "")]
    public void LevelsIconsAndCardNameIdentityRemainSpecificToTheChosenFront(string definition, int level, string icon)
    {
        var state = FateGimliFixture.Actions(definition, "hollin");
        var gimli = state.Characters.Single(c => c.Id == FateGimli.Figure);
        Assert.Equal(level, CompanionRules.Level(state, gimli));
        Assert.Equal(level, CharacterRules.CombatLevel(gimli));
        Assert.Equal(2, CharacterRules.MovementLevel(gimli));
        Assert.Equal(1, gimli.Leadership);
        Assert.Equal(icon, gimli.NationIcon);
        Assert.True(CharacterRules.MatchesCardName(gimli, FateGimli.Figure));
        Assert.False(CharacterRules.MatchesCardName(gimli, "base.character.legolas"));
        Assert.Equal(definition == FateGimli.Standard, CharacterRules.Has(gimli, CharacterRules.Captain));
        var p = CharacterFixture.Cost(state, DieFace.Character);
        p.CompanionMoves = [new() { Ids = [gimli.Id], Route = ["hollin", "north-dunland", "south-dunland"] }];
        Assert.Equal(2, new FellowshipOptions().GetOptions(state, Side.FreePeoples)
            .Single(o => o.Kind == CommandKind.MoveCompanions).Travel.Single(t => t.Id == gimli.Id).Range);
        state = KingsFixture.Do(state, CommandKind.MoveCompanions, p);
        Assert.Equal("south-dunland", state.Characters.Single(c => c.Id == gimli.Id).Location);
    }

    [Theory]
    [InlineData(FateGimli.Standard, true)]
    [InlineData(FateGimli.Alternate, false)]
    public void KingsReadsTheActualNationIconWithoutReintroducingDain(string definition, bool appropriate)
    {
        var state = FateGimliFixture.Actions(definition, "erebor", kings: true);
        Assert.DoesNotContain(state.Kings!.Sovereigns, s => s.Id == SovereignCatalog.Dain);
        // Isolated predicate check: this fixture is not added to the configured sovereign roster.
        var dain = new SovereignState { Id = SovereignCatalog.Dain, Status = SovereignStatus.Corrupted, CorruptedRegion = "erebor" };
        Assert.Equal(appropriate, SovereignAwakening.AppropriateCompanion(state, dain));
    }
}
