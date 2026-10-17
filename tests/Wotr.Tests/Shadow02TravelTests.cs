using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards02;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;

namespace Wotr.Tests;

public class Shadow02TravelTests
{
    internal static MatchState BeforeTravel(string card, string region = "hollin")
    {
        var state = BeforeEvent(card, region);
        state.Fellowship.Progress = 2;
        return state;
    }

    internal static CommandPayload Flight(MatchState state, string target, params string[] ids) => new()
    {
        CompanionMoves = [new() { Ids = [.. ids], Route = [NazgulEventTravel.Option(state).Travel.Single(t => t.Id == ids[0]).Origin, target] }]
    };

    [Theory]
    [InlineData(Search)]
    [InlineData(Strike)]
    public void CardFlightAllowsEmptyOrMultipleGroupsAndNeverChargesAnotherDie(string card)
    {
        var state = BeforeTravel(card);
        Put(state, CharacterRules.Mouth, "barad-dur");
        Put(state, CharacterRules.WitchKing, "barad-dur");
        state = Play(state, card);
        Assert.Equal(DecisionKind.CharacterEvent, state.Pending!.Kind);
        var options = new CharacterOptions().GetOptions(state, Side.Shadow).Single();
        Assert.True(options.AllowEmptySelection);
        Assert.Empty(options.EligibleDice);
        Assert.Contains(CharacterRules.WitchKing, options.EligibleIds);
        Assert.DoesNotContain(CharacterRules.Mouth, options.EligibleIds);
        Assert.DoesNotContain(CharacterRules.Saruman, options.EligibleIds);
        Assert.Empty(new CharacterOptions().GetOptions(state, Side.FreePeoples));
        var none = Do(state, CommandKind.MoveShadowCharacters);
        Assert.Empty(none.Resolution.Frames);
        Assert.Single(none.Dice, d => d.Location == DieLocation.Spent);
        var nazgul = options.Travel.First(o => o.Id != CharacterRules.WitchKing);
        var payload = Flight(state, "moria", nazgul.Id);
        payload.CompanionMoves.Add(new() { Ids = [CharacterRules.WitchKing], Route = ["barad-dur", "north-dunland"] });
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow, BattleFixture.Command(state, CommandKind.MoveShadowCharacters, payload));
        Assert.True(preview.Legal);
        Assert.Empty(preview.Costs);
        Assert.Equal(2, preview.Placements.Count);
        Assert.Equal(before, StateCodec.Write(state));
        state = Do(state, CommandKind.MoveShadowCharacters, payload);
        Assert.Equal("moria", state.Units.Single(u => u.Id == nazgul.Id).Location);
        Assert.Equal("north-dunland", state.Characters.Single(c => c.Id == CharacterRules.WitchKing).Location);
        Assert.Equal("barad-dur", state.Characters.Single(c => c.Id == CharacterRules.Mouth).Location);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(Search)]
    [InlineData(Strike)]
    public void CardFlightRejectsMinionsReserveDuplicateIdsWrongOriginsAndFreeStrongholds(string card)
    {
        var state = BeforeTravel(card);
        Put(state, CharacterRules.Mouth, "barad-dur");
        state = Play(state, card);
        var nazgul = NazgulEventTravel.Option(state).Travel.First();
        var valid = Flight(state, "hollin", nazgul.Id);
        Assert.NotNull(Engine().Validate(state, Side.FreePeoples, BattleFixture.Command(state, CommandKind.MoveShadowCharacters, valid)));
        var command = Flight(state, "hollin", nazgul.Id);
        command.DieId = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Id;
        Reject(state, CommandKind.MoveShadowCharacters, command);
        command.DieId = null!;
        Reject(state, CommandKind.MoveShadowCharacters, command);
        command = Flight(state, "hollin", nazgul.Id);
        command.CompanionMoves[0].Ids = [null!];
        Reject(state, CommandKind.MoveShadowCharacters, command);
        foreach (var id in new[] { CharacterRules.Mouth, CharacterRules.Saruman, "missing-nazgul" })
            Reject(state, CommandKind.MoveShadowCharacters, new() { CompanionMoves = [new() { Ids = [id], Route = ["barad-dur", "hollin"] }] });
        command = Flight(state, "hollin", nazgul.Id);
        command.CompanionMoves.Add(command.CompanionMoves[0]);
        Reject(state, CommandKind.MoveShadowCharacters, command);
        command = Flight(state, "hollin", nazgul.Id);
        command.CompanionMoves[0].Route[0] = "moria";
        Reject(state, CommandKind.MoveShadowCharacters, command);
        Reject(state, CommandKind.MoveShadowCharacters, Flight(state, "rivendell", nazgul.Id));
        Reject(state, CommandKind.MoveShadowCharacters, new() { CompanionMoves = [new() { Ids = [nazgul.Id], Route = [nazgul.Origin] }] });
    }

    [Fact]
    public void SearchRevealsAfterMovementWithFpRouteAndSeparateStrongholdHunt()
    {
        var state = BeforeTravel(Search, "moria");
        Pool(state, HuntTests.Three);
        state = Play(state, Search);
        var id = NazgulEventTravel.Option(state).EligibleIds[0];
        state = Do(state, CommandKind.MoveShadowCharacters, Flight(state, "moria", id));
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Equal(DecisionKind.FellowshipRoute, state.Pending.Kind);
        Assert.Equal(2, state.Fellowship.Progress);
        Assert.Empty(state.RandomSamples);
        state = Do(state, CommandKind.RevealFellowship, new() { Route = ["moria", "dimrill-dale"] }, new SequenceRandom(0));
        Assert.Equal(HuntDamageRule.Normal, HuntRuntime.Active(state)!.DamageRule);
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal("dimrill-dale", state.Fellowship.RegionId);
        Assert.Equal(0, state.Fellowship.Progress);
        Assert.False(state.Fellowship.Hidden);
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.Single(state.RandomSamples);
    }

    // UI-212: the map distributes one decision's flights from several regions, one group per origin and destination.
    [Theory]
    [InlineData(Search)]
    [InlineData(Strike)]
    public void OneDecisionFliesNazgulFromThreeRegions(string card)
    {
        var state = BeforeTravel(card);
        foreach (var unit in state.Units.Where(u => u.Kind == UnitKind.Nazgul))
            unit.Location = "reserve";
        var first = BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "barad-dur");
        var second = BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "minas-morgul");
        var king = Put(state, CharacterRules.WitchKing, "dol-guldur");
        state = Play(state, card);
        var payload = Flight(state, "moria", first.Id);
        payload.CompanionMoves.AddRange(Flight(state, "moria", second.Id).CompanionMoves);
        payload.CompanionMoves.AddRange(Flight(state, "north-dunland", king.Id).CompanionMoves);
        state = Do(state, CommandKind.MoveShadowCharacters, payload);
        Assert.Equal("moria", state.Units.Single(u => u.Id == first.Id).Location);
        Assert.Equal("moria", state.Units.Single(u => u.Id == second.Id).Location);
        Assert.Equal("north-dunland", state.Characters.Single(c => c.Id == king.Id).Location);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Fact]
    public void WitchKingCanRevealBesiegedFreeStrongholdAndNazgulCanLeaveShadowSiege()
    {
        var state = BeforeTravel(Search, "rivendell");
        foreach (var unit in state.Units.Where(u => u.Location == "rivendell" && BoardRules.Owner(u) == Side.FreePeoples))
            unit.Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "rivendell");
        var king = Put(state, CharacterRules.WitchKing, "moria");
        king.Garrison = true;
        state = Play(state, Search);
        var option = NazgulEventTravel.Option(state).Travel.Single(t => t.Id == king.Id);
        Assert.Contains("rivendell", option.Targets);
        state = Do(state, CommandKind.MoveShadowCharacters, Flight(state, "rivendell", king.Id));
        Assert.False(state.Characters.Single(c => c.Id == king.Id).Garrison);
        Assert.Equal(DecisionKind.FellowshipRoute, state.Pending!.Kind);
    }

    [Theory]
    [InlineData(Search)]
    [InlineData(Strike)]
    public void NoNazgulOnBoardCompletesWithoutAnEmptyDecision(string card)
    {
        var state = BeforeTravel(card);
        foreach (var unit in state.Units.Where(u => u.Kind == UnitKind.Nazgul))
            unit.Location = "casualties";
        state = Play(state, card);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WeatherOnlyDisplacesFigureAcrossLegalBorderAndPreviewsWithoutChangingState(bool hidden)
    {
        var state = BeforeTravel(Weather, "rivendell");
        state.Fellowship.Hidden = hidden;
        state = Play(state, Weather);
        Assert.Equal("Region", state.Pending!.Presentation);
        var choices = Choices(state).EligibleIds;
        Assert.Equal(BoardRules.Regions["rivendell"].Neighbors.Order(), choices.Order());
        Assert.DoesNotContain("moria", choices);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["moria"] });
        var target = choices[0];
        var metadata = Choices(state).EffectChoices.Single(c => c.Id == target);
        Assert.Equal("fellowship", metadata.SourceId);
        Assert.Equal(target, metadata.TargetId);
        var before = StateCodec.Write(state);
        var payload = new CommandPayload { Ids = [target] };
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow, BattleFixture.Command(state, CommandKind.ChooseEffect, payload));
        Assert.Equal(target, preview.Fellowship!.RegionId);
        Assert.Equal(2, preview.Fellowship.Progress);
        Assert.Equal(hidden, preview.Fellowship.Hidden);
        Assert.False(preview.HasUnknownResult);
        Assert.Equal(before, StateCodec.Write(state));
        state = Do(state, CommandKind.ChooseEffect, payload);
        Assert.Equal(target, state.Fellowship.RegionId);
        Assert.Equal(2, state.Fellowship.Progress);
        Assert.Equal(hidden, state.Fellowship.Hidden);
        Assert.Empty(state.RandomSamples);
        Assert.DoesNotContain(state.Events, e => e.Kind is "HuntRolled" or "HuntTileDrawn" or "FellowshipRevealed" or "FellowshipMoved");
    }
}
