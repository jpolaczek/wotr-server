using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards04;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow04Fixture;

namespace Wotr.Tests;

public class Shadow04EventTests
{
    public static IEnumerable<object[]> EveryCard() => Cards.Select(c => new object[] { c });

    [Theory]
    [MemberData(nameof(EveryCard))]
    public void EveryEventValidatesActorOwnershipCostAndWindow(string card)
    {
        var state = State(card, true);
        var cost = EventCost(state, card);
        var command = BattleFixture.Command(state, CommandKind.PlayEvent, cost);
        Assert.Null(Engine().Validate(state, Side.Shadow, command));
        Assert.NotNull(Engine().Validate(state, Side.FreePeoples, command));
        cost.CardCost = DieFace.Army;
        Reject(state, CommandKind.PlayEvent, cost);
        cost.CardCost = DieFace.Character;
        state.Dice.Single(d => d.Id == cost.DieId).Location = DieLocation.Spent;
        Reject(state, CommandKind.PlayEvent, cost);
        state.Dice.Single(d => d.Id == cost.DieId).Location = DieLocation.Available;
        HandRules.Remove(state.Player(Side.Shadow), card);
        Reject(state, CommandKind.PlayEvent, cost);
        HandRules.Add(state.Player(Side.Shadow), card);
        state.Pending!.Kind = DecisionKind.Reaction;
        Reject(state, CommandKind.PlayEvent, cost);
    }

    [Theory]
    [InlineData(Palantir, CharacterRules.Saruman)]
    [InlineData(Wormtongue, CharacterRules.Saruman)]
    [InlineData(Captain, CharacterRules.WitchKing)]
    [InlineData(Grond, CharacterRules.WitchKing)]
    public void MinionMustCurrentlyBeInPlay(string card, string minion)
    {
        var state = State(card, true);
        state.MinionEverEntered = true;
        CharacterRules.InPlay(state, minion)!.Location = "casualties";
        Reject(state, CommandKind.PlayEvent, EventCost(state, card));
    }

    [Fact]
    public void RingwraithsCanResolveWithNoFiguresAndNoSecondaryManeuver()
    {
        var state = State();
        foreach (var unit in state.Units.Where(u => u.Kind == UnitKind.Nazgul))
            BoardRules.ReturnToReserve(unit);
        CharacterLifecycle.Eliminate(state, CharacterRules.InPlay(state, CharacterRules.WitchKing)!);
        state = Play(state, Ringwraiths);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void CaptainRecruitmentUsesAvailableNazgulAndKingsRegionWithoutNormalMusterRestrictions(int available)
    {
        var state = State(Captain);
        var king = CharacterRules.InPlay(state, CharacterRules.WitchKing)!;
        king.Location = "helms-deep";
        king.Garrison = true;
        foreach (var unit in state.Units.Where(u => u.Kind == UnitKind.Nazgul && u.Location == "reserve").Skip(available))
            unit.Location = "dol-guldur";
        state = Play(state, Captain);
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow,
            BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = [BlackCaptainStep.Recruit] }));
        Assert.Equal(available, preview.Placements.Count);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [BlackCaptainStep.Recruit] });
        Assert.Equal(available, state.Units.Count(u => u.Kind == UnitKind.Nazgul && u.Location == "helms-deep" && u.Garrison));
        Assert.Empty(state.RandomSamples);
        Assert.Empty(state.Resolution.Frames);
    }
}
