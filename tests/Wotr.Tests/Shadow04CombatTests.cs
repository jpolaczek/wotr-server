using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards04;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow04Fixture;

namespace Wotr.Tests;

public class Shadow04CombatTests
{
    public static IEnumerable<object[]> EveryCard() => Cards.Select(c => new object[] { c });

    internal static MatchState Combat(string card, bool shadowAttacks = true)
    {
        var state = BeforeCombat(card, shadowAttacks);
        var battle = BattleRuntime.Active(state)!;
        var force = battle.Force(Side.Shadow);
        var unit = BattleFixture.Put(state, "isengard", UnitKind.Regular, force.Region);
        force.Participants.Add(unit.Id);
        if (card == Spells)
        {
            battle.Mode = BattleMode.Siege;
            battle.Defender.Garrison = true;
            foreach (var defender in state.Units.Where(u => battle.Defender.Participants.Contains(u.Id)))
                defender.Garrison = true;
            foreach (var defender in state.Characters.Where(c => battle.Defender.Participants.Contains(c.Id)))
                defender.Garrison = true;
        }
        return state;
    }

    [Theory]
    [MemberData(nameof(EveryCard))]
    public void EveryCombatValidatesOwnerCardAndWindow(string card)
    {
        var state = Combat(card);
        var payload = new CommandPayload { CardId = card };
        var command = BattleFixture.Command(state, CommandKind.CommitCombatCard, payload);
        Assert.Null(Engine().Validate(state, Side.Shadow, command));
        Assert.NotNull(Engine().Validate(state, Side.FreePeoples, command));
        HandRules.Remove(state.Player(Side.Shadow), card);
        Reject(state, CommandKind.CommitCombatCard, payload);
        HandRules.Add(state.Player(Side.Shadow), card);
        state.Pending!.Kind = DecisionKind.Action;
        Reject(state, CommandKind.CommitCombatCard, payload);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DevilryWorksForEitherSideOfASiegeButNotFieldSortieOrRelief(bool shadowAttacks)
    {
        var state = Combat(Spells, shadowAttacks);
        var battle = BattleRuntime.Active(state)!;
        var condition = EffectCatalog.Base.Combats[Spells].Condition;
        foreach (var mode in Enum.GetValues<BattleMode>())
        {
            battle.Mode = mode;
            Assert.Equal(mode == BattleMode.Siege, condition.CanPlay(state, battle, Side.Shadow));
        }
        battle.Mode = BattleMode.Siege;
        var isengard = state.Units.Single(u => battle.Force(Side.Shadow).Participants.Contains(u.Id) && u.Nation == "isengard");
        battle.Force(Side.Shadow).Participants.Remove(isengard.Id);
        Assert.False(condition.CanPlay(state, battle, Side.Shadow));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DevilryAddsOnlyCombatModifierAndNaturalOneRemainsMiss(bool shadowAttacks)
    {
        var state = Reveal(Combat(Spells, shadowAttacks), Spells, dice: [0, 3, 4, 0, 0, 0, 0, 0, 0]);
        var round = BattleRuntime.Active(state)!.Force(Side.Shadow).Round;
        Assert.Equal(1, round.CombatModifier);
        Assert.Equal(0, round.RerollModifier);
        Assert.All(round.Dice, die => Assert.Equal(1, die.Modifier));
        Assert.All(round.Dice.Where(d => d.Result == 1), die => Assert.False(die.Hit));
    }

    [Fact]
    public void SharedCombatProgramsRetainTheirDistinctWindowsAndCosts()
    {
        Assert.Equal("combat.dread-and-despair", EffectCatalog.Base.Combats[Grond].Effects.Single().ProgramId);
        Assert.Equal(3, EffectCatalog.Base.Combats[Palantir].Effects.Single().Initiative);
        var state = Reveal(Combat(Palantir), Palantir);
        Assert.Equal("NazgulLeadership", state.Pending!.Presentation);
        Assert.Equal(2, Choices(state).Minimum);
        Assert.Equal(2, Choices(state).Maximum);
        foreach (var card in new[] { Wormtongue, Captain })
        {
            state = Combat(card);
            var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
            force.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Nazgul, force.Region).Id);
            state = Reveal(state, card, dice: new int[10]);
            Assert.Equal(0, BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.RerollLimit);
        }
        state = Reveal(Combat(Ringwraiths), Ringwraiths);
        Assert.Equal("Companion", state.Pending!.Presentation);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["base.character.gimli"] }, new SequenceRandom(new int[10]));
        Assert.Contains("base.character.gimli", BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.CancelledAbilities);
    }
}
