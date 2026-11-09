using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.FateScenarioDriver;

namespace Wotr.Tests;

// TEST-14: every ability of both Gimli versions acts in its scenario: Captain of the West and the Dwarven Champion add
// their die in the Iron Hills, the Dwarven Lord moves two regions at level 1, and the UI-349 scenarios' Renown,
// Messenger, Guide response and homeland, offered there, also run through the engine with their result.
public class FateGimliEndToEndScenarioTests
{
    private const string IronHills = "iron-hills";
    private const string EredLuin = "ered-luin";

    [Theory]
    [InlineData("fate-gimli-captain", FateGimli.Standard)]
    [InlineData("fate-gimli-champion", FateGimli.Alternate)]
    public void GimliAddsOneCombatDieToTheIronHillsDefence(string id, string definition)
    {
        var state = Create(id);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        var battle = BattleRuntime.Active(state)!;
        var free = battle.Force(Side.FreePeoples);
        Assert.Equal(IronHills, battle.Region);
        Assert.Contains(FateGimli.Figure, free.Participants);
        Assert.Equal(definition, Gimli(state).DefinitionId);
        Assert.Equal(1, BattleRules.CaptainBonus(state, free));
        Assert.Equal(BattleRules.Strength(state, free) + 1, BattleRoll.DiceCount(state, free));
        Assert.Equal(6, BattleRules.HitTarget(state, battle, battle.Attacker));
        var dice = BattleRoll.DiceCount(state, free);
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = "" }, new LowestFace());
        Assert.Equal(dice, BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.Dice.Count);
    }

    [Fact]
    public void TheDwarvenLordAtLevelOneMovesTwoRegions()
    {
        var state = Create("fate-gimli-journey");
        var gimli = Gimli(state);
        Assert.Equal((FateGimli.Alternate, EredLuin), (gimli.DefinitionId, gimli.Location));
        Assert.Equal(1, CompanionRules.Level(state, gimli));
        Assert.Equal(2, Option(state, CommandKind.MoveCompanions).Travel.Single(t => t.Id == gimli.Id).Range);
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Character);
        state = Do(state, CommandKind.MoveCompanions, new()
        {
            DieId = die.Id,
            CompanionMoves = [new() { Ids = [gimli.Id], Route = [EredLuin, "tower-hills", "the-shire"] }]
        });
        Assert.Equal("the-shire", Gimli(state).Location);
    }

    [Fact]
    public void RenownAdvancesTheActiveDwarvesFromTheIronHills()
    {
        var state = Create("fate-gimli-renown");
        var steps = Nation(state, "dwarves").StepsToWar;
        state = Ability(state, FateGimli.Renown);
        Assert.Equal(steps - 1, Nation(state, "dwarves").StepsToWar);
    }

    [Fact]
    public void MessengerOfTheDwarvesActivatesAndAdvancesThePassiveDwarves()
    {
        var state = Ability(Create("fate-gimli-messenger"), "messenger-dwarves");
        Assert.True(Nation(state, "dwarves").Active);
        Assert.Equal(1, Nation(state, "dwarves").StepsToWar);
    }

    [Fact]
    public void TheDwarvenLordsGuideResponseLeavesTheFellowshipAndReducesTheDamage()
    {
        var state = Create("fate-gimli-guide");
        var damage = HuntRuntime.Active(state)!.Damage;
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = FateGimli.GuideReaction });
        Assert.Equal(Math.Max(0, damage - 1), HuntRuntime.Active(state)!.Damage);
        Assert.DoesNotContain(FateGimli.Figure, state.Fellowship.Companions);
    }

    [Fact]
    public void TheDwarvenLordStartsInTheIronHills()
    {
        var state = Create("fate-gimli-homeland");
        state = Do(state, CommandKind.ChooseCouncilHomelands, new()
        {
            Placements = [new() { UnitId = FateGimli.Figure, RegionId = IronHills }]
        });
        Assert.Equal(IronHills, Gimli(state).Location);
    }

    private static CharacterState Gimli(MatchState state) => state.Characters.Single(c => c.Id == FateGimli.Figure);

    // Gimli's political ability in his region, paid with the first die the projection offers for it.
    private static MatchState Ability(MatchState state, string abilityId)
    {
        var action = Option(state, CommandKind.UseCharacterAbility).CharacterActions
            .Single(c => c.CharacterId == FateGimli.Figure && c.AbilityId == abilityId);
        return Do(state, CommandKind.UseCharacterAbility, new()
        {
            DieId = action.Dice[0],
            CharacterId = FateGimli.Figure,
            AbilityId = abilityId,
            Ids = [Gimli(state).Location]
        });
    }
}
