using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class CouncilAbilityTests
{
    [Theory]
    [InlineData("boromir", "minas-tirith", "gondor", "messenger-gondor", "")]
    [InlineData("gimli", "erebor", "dwarves", "messenger-dwarves", "")]
    [InlineData("merry", "the-shire", "north", "messenger-north", "")]
    [InlineData("pippin", "dale", "north", "messenger-north", "")]
    [InlineData("legolas", "lorien", "elves", "prince-of-mirkwood", "elves")]
    public void PoliticalAbilityUsesAnyDieWithExactIconAndNoUnconqueredRequirement(string name, string region, string nationId, string ability, string icon)
    {
        var state = CouncilFixture.Actions();
        var companion = LordsCharacterFixture.Put(state, "lords.character." + name, region, "base.character." + name);
        Assert.Equal(icon, companion.NationIcon);
        state.Control[region] = Side.Shadow;
        var nation = state.Nations.Single(n => n.Id == nationId);
        nation.Active = false;
        nation.StepsToWar = name == "legolas" ? 2 : 1;
        PoliticalRules.CompanionArrived(state, region, companion.NationIcon);
        PoliticalReactions.Complete(state);
        Assert.False(nation.Active);
        var payload = CharacterFixture.Cost(state, DieFace.Event);
        payload.CharacterId = companion.Id;
        payload.AbilityId = ability;
        payload.Ids = [region];
        var command = BattleFixture.Command(state, CommandKind.UseCharacterAbility, payload);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples, command);
        Assert.True(preview.Legal);
        Assert.Equal(name != "legolas", Assert.Single(preview.PoliticsChanges).Active);
        state = LordsFixture.Do(state, command.Kind, payload);
        nation = state.Nations.Single(n => n.Id == nationId);
        Assert.Equal(name != "legolas", nation.Active);
        Assert.Equal(name == "legolas" ? 1 : 0, nation.StepsToWar);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
    }

    [Theory]
    [InlineData("boromir", "osgiliath", "messenger-gondor")]
    [InlineData("gimli", "iron-hills", "messenger-dwarves")]
    [InlineData("legolas", "west-harondor", "prince-of-mirkwood")]
    [InlineData("merry", "ered-luin", "messenger-north")]
    public void PoliticalAbilityRejectsTheWrongPrintedLocation(string name, string region, string ability)
    {
        var state = CouncilFixture.Actions();
        var companion = LordsCharacterFixture.Put(state, "lords.character." + name, region);
        var payload = CharacterFixture.Cost(state, DieFace.Character);
        payload.CharacterId = companion.Id;
        payload.AbilityId = ability;
        payload.Ids = [region];
        LordsFixture.Reject(state, CommandKind.UseCharacterAbility, payload);
    }

    [Theory]
    [InlineData("boromir")]
    [InlineData("gimli")]
    [InlineData("legolas")]
    public void LevelTwoAppliesOnlyToMovementIncludingProjectedRoutes(string name)
    {
        var state = CouncilFixture.Actions();
        var companion = CouncilFixture.Inside(state, name);
        state.Fellowship.RegionId = "hollin";
        Assert.Equal(1, CompanionRules.Level(state, companion));
        Assert.Equal(1, CharacterRules.CombatLevel(companion));
        Assert.Equal(2, CompanionRules.Range(state, [companion], true));
        var payload = CharacterFixture.Cost(state, DieFace.Character);
        payload.Ids = [companion.Id];
        payload.Route = ["hollin", "north-dunland", "south-dunland"];
        state = LordsFixture.Do(state, CommandKind.SeparateCompanions, payload);
        state = LordsFixture.Do(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Grey });
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var option = new FellowshipOptions().GetOptions(state, Side.FreePeoples).Single(o => o.Kind == CommandKind.MoveCompanions);
        Assert.Equal(2, option.Travel.Single(t => t.Id == companion.Id).Range);
        payload = CharacterFixture.Cost(state, DieFace.Character);
        payload.CompanionMoves = [new() { Ids = [companion.Id], Route = ["south-dunland", "north-dunland", "hollin"] }];
        state = LordsFixture.Do(state, CommandKind.MoveCompanions, payload);
        Assert.Equal("hollin", state.Characters.Single(c => c.Id == companion.Id).Location);
    }

    [Theory]
    [InlineData("boromir", "osgiliath")]
    [InlineData("boromir", "lossarnach")]
    [InlineData("gimli", "iron-hills")]
    [InlineData("legolas", "woodland-realm")]
    [InlineData("merry", "bree")]
    [InlineData("pippin", "bree")]
    public void ChampionUsesDefendersRegionForEitherSideAndCapsCombatAtFive(string name, string region)
    {
        foreach (var fpAttacks in new[] { true, false })
        {
            var state = CouncilFixture.Actions();
            foreach (var unit in state.Units)
                BoardRules.ReturnToReserve(unit);
            var fpRegion = fpAttacks ? "hollin" : region;
            var companion = LordsCharacterFixture.Put(state, "lords.character." + name, fpRegion);
            var unitFp = BattleFixture.Put(state, "gondor", UnitKind.Regular, fpRegion);
            var fp = new BattleForce { Side = Side.FreePeoples, Region = fpRegion, Participants = [unitFp.Id, companion.Id] };
            var sh = new BattleForce { Side = Side.Shadow, Region = region };
            var battle = new BattleState { Region = region, Attacker = fpAttacks ? fp : sh, Defender = fpAttacks ? sh : fp };
            state.Resolution.Frames.Add(new() { Battle = battle });
            Assert.Equal(2, BattleRoll.DiceCount(state, fp));
            fp.Round.CancelledAbilities.Add(companion.Id);
            Assert.Equal(1, BattleRoll.DiceCount(state, fp));
            fp.Round.CancelledAbilities.Clear();
            if (fpAttacks)
            {
                sh.Region = "hollin";
                Assert.Equal(1, BattleRoll.DiceCount(state, fp));
                sh.Region = region;
            }
            for (var i = 0; i < 5; i++)
                fp.Participants.Add(BattleFixture.Put(state, "gondor", UnitKind.Regular, fpRegion).Id);
            Assert.Equal(5, BattleRoll.DiceCount(state, fp));
        }
    }

    [Fact]
    public void AlternateHobbitsStillQualifyForHobbitCardsAndShadowfaxWithoutTakenAlive()
    {
        var state = CouncilFixture.Actions();
        var merry = LordsCharacterFixture.Put(state, CouncilCompanions.Merry, "hollin");
        var white = LordsCharacterFixture.Put(state, CharacterRules.White, "hollin");
        Assert.False(CharacterRules.Has(merry, CharacterRules.TakenAlive));
        Assert.Equal(4, CharacterRules.CompanionRange([merry, white]));
        var battle = new BattleState { Attacker = new() { Side = Side.FreePeoples, Region = "hollin", Participants = [merry.Id] } };
        Assert.True(new CompanionInBattle(true).CanPlay(state, battle, Side.FreePeoples));
        Assert.True(CharacterRules.MatchesCardName(merry, "base.character.merry"));
    }
}
