using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

public class CharacterActionTests
{
    [Theory]
    [InlineData("gimli", "erebor", "dwarves", "politics-dwarves-at-erebor")]
    [InlineData("boromir", "pelargir", "gondor", "politics-gondor-at-city-or-stronghold")]
    [InlineData("legolas", "lorien", "elves", "politics-elves-at-stronghold")]
    public void NativeCompanionAdvancesPoliticsWithAnyDieOnlyInUnconqueredListedSettlement(string id, string region, string nation, string ability)
    {
        var state = TurnTests.Actions();
        var companion = Put(state, "base.character." + id, region);
        var politics = state.Nations.Single(n => n.Id == nation);
        var steps = politics.StepsToWar;
        var p = Cost(state, DieFace.Event);
        p.CharacterId = companion.Id;
        p.AbilityId = ability;
        p.Ids = [region];
        state.Control[region] = Side.Shadow;
        Reject(state, CommandKind.UseCharacterAbility, p);
        state.Control[region] = Side.FreePeoples;
        state = Do(state, CommandKind.UseCharacterAbility, p);
        Assert.Equal(steps - 1, state.Nations.Single(n => n.Id == nation).StepsToWar);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == p.DieId).Location);
    }

    [Fact]
    public void StriderGuideHidesWithMusterAndShadowfaxWorksAloneOrWithExactlyOneHobbit()
    {
        var state = TurnTests.Actions();
        state.Fellowship.Guide = CharacterRules.Strider;
        state.Fellowship.Hidden = false;
        var p = Cost(state, DieFace.Muster);
        Assert.Contains(new FellowshipOptions().GetOptions(state, Side.FreePeoples), o => o.Kind == CommandKind.HideFellowship && o.EligibleDice.Contains(p.DieId));
        state = Do(state, CommandKind.HideFellowship, p);
        Assert.True(state.Fellowship.Hidden);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == p.DieId).Location);
        var white = Put(state, CharacterRules.White, "rivendell");
        var merry = Put(state, "base.character.merry", "rivendell");
        var pippin = Put(state, "base.character.pippin", "rivendell");
        List<string> route = ["rivendell", "fords-of-bruinen", "hollin", "north-dunland", "south-dunland"];
        Assert.NotNull(CompanionRules.Move(state, [new() { Ids = [white.Id], Route = route }]));
        Assert.NotNull(CompanionRules.Move(state, [new() { Ids = [white.Id, merry.Id], Route = route }]));
        Assert.Null(CompanionRules.Move(state, [new() { Ids = [white.Id, merry.Id, pippin.Id], Route = route }]));
        Assert.Equal(3, white.Level);
    }

    [Fact]
    public void VoiceUsesRealReservesSkipsCapturedSettlementsAndCannotWorkUnderSiege()
    {
        var state = BattleFixture.State(Side.Shadow);
        var saruman = Put(state, CharacterRules.Saruman, "orthanc");
        state.Control["north-dunland"] = Side.FreePeoples;
        var p = Cost(state, DieFace.Muster);
        p.CharacterId = saruman.Id;
        p.AbilityId = CharacterAbilities.VoiceRecruit;
        p.Ids = ["orthanc", "south-dunland"];
        var invader = BattleFixture.Put(state, "rohan", UnitKind.Regular, "orthanc");
        Reject(state, CommandKind.UseCharacterAbility, p);
        BoardRules.ReturnToReserve(invader);
        state = Do(state, CommandKind.UseCharacterAbility, p);
        Assert.Equal(2, state.Units.Count(u => u.Nation == "isengard" && u.Location is "orthanc" or "south-dunland"));
        Assert.DoesNotContain(state.Units, u => u.Location == "north-dunland");
        Assert.Equal("orthanc", Assert.Single(state.Characters).Location);
    }

    [Fact]
    public void VoiceUpgradeReturnsTwoRegularsAndWhiteHandLeadershipEndsImmediatelyAtDeath()
    {
        var state = BattleFixture.State(Side.Shadow);
        var saruman = Put(state, CharacterRules.Saruman, "orthanc");
        var regulars = Enumerable.Range(0, 2).Select(_ => BattleFixture.Put(state, "isengard", UnitKind.Regular, "orthanc").Id).ToList();
        var p = Cost(state, DieFace.Muster);
        p.CharacterId = saruman.Id;
        p.AbilityId = CharacterAbilities.VoiceUpgrade;
        p.Ids = ["orthanc"];
        state = Do(state, CommandKind.UseCharacterAbility, p);
        Assert.All(state.Units.Where(u => regulars.Contains(u.Id)), u => Assert.Equal("reserve", u.Location));
        var elite = state.Units.First(u => u.Kind == UnitKind.Elite && u.Location == "orthanc");
        Assert.Equal(2, state.Units.Count(u => u.Kind == UnitKind.Elite && u.Location == "orthanc"));
        var force = new BattleForce { Side = Side.Shadow, Region = "orthanc", Participants = [elite.Id] };
        Assert.Equal(1, BattleLeadership.Total(state, force));
        var army = new ArmyMoveChoice { From = "orthanc", To = "gap-of-rohan", UnitIds = [elite.Id] };
        TurnFlow.SelectNext(state, Side.Shadow);
        var move = Cost(state, DieFace.Character);
        move.Moves = [army];
        move.MovementCost = DieFace.Character;
        Assert.NotNull(MovementRules.Plan(state, Side.Shadow, move));
        CharacterLifecycle.Eliminate(state, state.Characters.Single());
        Assert.Equal(0, BattleLeadership.Total(state, force));
        Assert.Null(MovementRules.Plan(state, Side.Shadow, move));
    }

    [Fact]
    public void VoiceShortageLetsOwnerChooseSettlementsAndPreviewsPersistedExcessWithoutRepaying()
    {
        var state = BattleFixture.State(Side.Shadow);
        var saruman = Put(state, CharacterRules.Saruman, "orthanc");
        for (var index = 0; index < 10; index++)
            BattleFixture.Put(state, "isengard", UnitKind.Regular, "orthanc");
        var option = CharacterAbilities.Options(state, Side.Shadow).Single(o => o.AbilityId == CharacterAbilities.VoiceRecruit);
        Assert.Equal(2, option.TargetCount);
        Assert.Equal(3, option.Targets.Count);
        var p = Cost(state, DieFace.Muster);
        p.CharacterId = saruman.Id;
        p.AbilityId = CharacterAbilities.VoiceRecruit;
        p.Ids = ["orthanc", "south-dunland"];
        var command = BattleFixture.Command(state, CommandKind.UseCharacterAbility, p);
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow, command);
        Assert.True(preview.Legal);
        Assert.Equal("orthanc", Assert.Single(preview.Excess).RegionId);
        Assert.Equal(1, preview.Excess[0].Count);
        state = Do(state, command.Kind, p);
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        var committed = state.Dice.Count(d => d.Location is DieLocation.Spent or DieLocation.Resolving);
        var removed = state.Pending.PrivateChoices.First();
        state = Do(StateCodec.Copy(state), CommandKind.RemoveExcess, new() { Ids = [removed] });
        Assert.Equal(committed, state.Dice.Count(d => d.Location == DieLocation.Spent));
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == p.DieId).Location);
        Assert.Equal("reserve", state.Units.Single(u => u.Id == removed).Location);
        Assert.DoesNotContain(state.Units, u => u.Location == "north-dunland");
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
    }

    [Theory]
    [InlineData(2, 2, 2)]
    [InlineData(1, 2, 1)]
    [InlineData(2, 1, 1)]
    [InlineData(0, 2, 0)]
    [InlineData(2, 0, 0)]
    public void VoiceUpgradeUsesOnlyAvailableReplacementPairs(int regularCount, int eliteCount, int expected)
    {
        var state = BattleFixture.State(Side.Shadow);
        var saruman = Put(state, CharacterRules.Saruman, "orthanc");
        for (var index = 0; index < regularCount; index++)
            BattleFixture.Put(state, "isengard", UnitKind.Regular, "orthanc");
        foreach (var elite in state.Units.Where(u => u.Nation == "isengard" && u.Kind == UnitKind.Elite).Skip(eliteCount))
            elite.Location = "gap-of-rohan";
        var p = Cost(state, DieFace.Muster);
        p.CharacterId = saruman.Id;
        p.AbilityId = CharacterAbilities.VoiceUpgrade;
        p.Ids = ["orthanc"];
        if (expected == 0)
        {
            Reject(state, CommandKind.UseCharacterAbility, p);
            return;
        }
        state = Do(state, CommandKind.UseCharacterAbility, p);
        Assert.Equal(expected, state.Units.Count(u => u.Location == "orthanc" && u.Kind == UnitKind.Elite));
        Assert.Equal(regularCount - expected, state.Units.Count(u => u.Location == "orthanc" && u.Kind == UnitKind.Regular));
    }

    [Fact]
    public void MessengerPaysAttackAndArmyEventInTheirOwnCommands()
    {
        var state = BattleFixture.State(Side.Shadow);
        var mouth = Put(state, CharacterRules.Mouth, "barad-dur");
        var unit = BattleFixture.Put(state, "sauron", UnitKind.Regular, "osgiliath");
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "north-ithilien");
        var p = Cost(state, DieFace.Muster);
        p.AbilityId = CharacterRules.MouthArmy;
        p.MovementCost = DieFace.Army;
        p.Moves = [new() { From = "osgiliath", To = "north-ithilien", UnitIds = [unit.Id] }];
        state = Do(state, CommandKind.AttackArmy, p);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Equal(state.Turn, state.Characters.Single(c => c.Id == mouth.Id).LastArmyConversionTurn);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == p.DieId).Location);

        // The event body is a fixture; this verifies payment and consumption independently of T21 card packs.
        const string card = "base.sh.strategy.01";
        var catalog = new EffectCatalog([new(card, [])], [new(card, card, new UnconditionalEvent())], []);
        state = BattleFixture.State(Side.Shadow);
        mouth = Put(state, CharacterRules.Mouth, "barad-dur");
        HandRules.Add(state.Player(Side.Shadow), card);
        p = Cost(state, DieFace.Muster);
        p.CardId = card;
        p.CardCost = DieFace.Army;
        p.AbilityId = CharacterRules.MouthArmy;
        state = Do(state, CommandKind.PlayEvent, p, catalog: catalog);
        Assert.Contains(card, state.Player(Side.Shadow).Discard);
        Assert.Equal(state.Turn, state.Characters.Single().LastArmyConversionTurn);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == p.DieId).Location);
    }

    [Fact]
    public void MessengerPaysTwoArmyMovesOnlyOnceInATurnAndSurvivesBoardCopy()
    {
        var state = BattleFixture.State(Side.Shadow);
        var mouth = Put(state, CharacterRules.Mouth, "barad-dur");
        var first = BattleFixture.Put(state, "sauron", UnitKind.Regular, "minas-morgul");
        var second = BattleFixture.Put(state, "isengard", UnitKind.Regular, "orthanc");
        var p = Cost(state, DieFace.Muster);
        p.AbilityId = CharacterRules.MouthArmy;
        p.Moves = [new() { From = first.Location, To = "south-ithilien", UnitIds = [first.Id] },
            new() { From = second.Location, To = "gap-of-rohan", UnitIds = [second.Id] }];
        state = Do(state, CommandKind.MoveArmies, p);
        Assert.Equal("south-ithilien", state.Units.Single(u => u.Id == first.Id).Location);
        Assert.Equal("gap-of-rohan", state.Units.Single(u => u.Id == second.Id).Location);
        Assert.Equal(state.Turn, state.Characters.Single(c => c.Id == mouth.Id).LastArmyConversionTurn);
        TurnFlow.SelectNext(state, Side.Shadow);
        var cost = Cost(state, DieFace.Muster);
        Assert.False(CharacterCosts.CanPay(state, Side.Shadow, cost.DieId, DieFace.Army, CharacterRules.MouthArmy));
        state.Turn++;
        Assert.True(CharacterCosts.CanPay(state, Side.Shadow, cost.DieId, DieFace.Army, CharacterRules.MouthArmy));
        Assert.True(EventRules.ValidCost(state, Side.Shadow, "base.sh.strategy.01", cost.DieId, DieFace.Army, CharacterRules.MouthArmy));
        Assert.False(EventRules.ValidCost(state, Side.Shadow, "base.sh.character.01", cost.DieId, DieFace.Character, CharacterRules.MouthArmy));
    }
}
