using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.CorsairFixture;

namespace Wotr.Tests;

public class CorsairMovementTests
{
    [Theory]
    [InlineData(DieFace.Army)]
    [InlineData(DieFace.Event)]
    public void PrintedConditionAllowsPaymentWithoutArmyInUmbar(DieFace face)
    {
        var state = Play(State(0), face);
        Assert.Empty(state.Resolution.Frames);
        Assert.Contains(Card, state.Player(Side.Shadow).Discard);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Empty(state.RandomSamples);
    }

    [Fact]
    public void SouthronsAtWarIsThePlayConditionAndBesiegedUmbarCannotSail()
    {
        var state = State();
        state.Nations.Single(n => n.Id == "southrons").StepsToWar = 1;
        Reject(state, CommandKind.PlayEvent, EventCost(state));
        state.Nations.Single(n => n.Id == "southrons").StepsToWar = 0;
        foreach (var unit in state.Units.Where(u => u.Location == CorsairRules.Origin))
            unit.Garrison = true;
        BattleFixture.Put(state, "gondor", UnitKind.Regular, CorsairRules.Origin);
        state = Play(state);
        Assert.Empty(state.Resolution.Frames);
        Assert.Empty(state.RandomSamples);
        Assert.All(state.Units.Where(u => u.Location == CorsairRules.Origin && BoardRules.Owner(u) == Side.Shadow), u => Assert.True(u.Garrison));
    }

    [Fact]
    public void OptionsContainExactlyTheFourGondorCoastsIncludingLamedon()
    {
        var state = Play(State());
        var option = new MovementOptions().GetOptions(state, Side.Shadow).Single();
        Assert.Equal(new[] { "anfalas", "dol-amroth", "lamedon", "pelargir" }, option.Targets.Order());
        Assert.Equal(1, option.Maximum);
        Assert.False(option.ArmyEvent!.RouteMovement);
        Assert.Empty(option.EligibleDice);
        Assert.Empty(new MovementOptions().GetOptions(state, Side.FreePeoples));
    }

    [Theory]
    [InlineData("anfalas", false)]
    [InlineData("dol-amroth", true)]
    [InlineData("lamedon", true)]
    [InlineData("pelargir", true)]
    public void DirectArrivalCapturesEmptySettlementAndResolvesPoliticsOnce(string target, bool settlement)
    {
        var state = State();
        var gondor = state.Nations.Single(n => n.Id == "gondor");
        gondor.Active = false;
        gondor.StepsToWar = 3;
        var arrivals = state.Units.Where(u => u.Location == CorsairRules.Origin).Select(u => u.Id).ToArray();
        state = Land(state, target);
        Assert.All(arrivals, id => Assert.Equal(target, state.Units.Single(u => u.Id == id).Location));
        if (settlement)
            Assert.Equal(Side.Shadow, state.Control[target]);
        Assert.True(state.Nations.Single(n => n.Id == "gondor").Active);
        Assert.Equal(settlement ? 2 : 3, state.Nations.Single(n => n.Id == "gondor").StepsToWar);
        Assert.Empty(state.RandomSamples);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void APartialMixedArmySailsWithChosenNazgulAndLeavesOtherFiguresInUmbar()
    {
        var state = State();
        var regular = BattleFixture.Put(state, "sauron", UnitKind.Regular, CorsairRules.Origin);
        var nazgul = BattleFixture.Put(state, "sauron", UnitKind.Nazgul, CorsairRules.Origin);
        var witchKing = Put(state, CharacterRules.WitchKing, CorsairRules.Origin);
        state = Play(state);
        state = Do(state, CommandKind.MoveArmies, new()
        {
            Moves = [new() { From = CorsairRules.Origin, To = "lamedon", UnitIds = [regular.Id, nazgul.Id], CharacterIds = [witchKing.Id] }]
        });
        Assert.All(new[] { regular.Id, nazgul.Id }, id => Assert.Equal("lamedon", state.Units.Single(u => u.Id == id).Location));
        Assert.Equal("lamedon", state.Characters.Single(c => c.Id == witchKing.Id).Location);
        Assert.Equal(2, state.Units.Count(u => u.Location == CorsairRules.Origin));
    }

    [Fact]
    public void DepartingLastBesiegerReleasesUmbarGarrisonWithoutCapturingIntermediateRegions()
    {
        var state = State();
        var defender = BattleFixture.Put(state, "gondor", UnitKind.Regular, CorsairRules.Origin);
        defender.Garrison = true;
        state.Control[CorsairRules.Origin] = Side.FreePeoples;
        var before = state.Control["west-harondor"];
        state = Land(state, "anfalas");
        Assert.False(state.Units.Single(u => u.Id == defender.Id).Garrison);
        Assert.Equal(Side.FreePeoples, state.Control[CorsairRules.Origin]);
        Assert.Equal(before, state.Control["west-harondor"]);
    }

    [Fact]
    public void PeacefulArrivalMergesBeforeFinishingThePaidEvent()
    {
        var state = State(8);
        for (var i = 0; i < 4; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, "lamedon");
        state.Control["lamedon"] = Side.Shadow;
        state = Land(state, "lamedon");
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Null(BattleRuntime.Active(state));
        Assert.Single(state.Dice, d => d.Location == DieLocation.Resolving);
        var excess = state.Units.Where(u => u.Location == "lamedon").Take(2).Select(u => u.Id).ToList();
        state = Do(state, CommandKind.RemoveExcess, new() { Ids = excess });
        Assert.Equal(10, state.Units.Count(u => u.Location == "lamedon"));
        Assert.Empty(state.Resolution.Frames);
        Assert.Empty(state.RandomSamples);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Theory]
    [InlineData("inland")]
    [InlineData("foreign-coast")]
    [InlineData("wrong-origin")]
    [InlineData("duplicate")]
    [InlineData("extra-army")]
    [InlineData("extra-die")]
    [InlineData("ability")]
    [InlineData("global-route")]
    [InlineData("army-route")]
    [InlineData("foreign-unit")]
    [InlineData("garrison")]
    [InlineData("non-belligerent")]
    [InlineData("unit-as-character")]
    [InlineData("nazgul-alone")]
    [InlineData("immobile-character")]
    public void IllegalLandingPayloadIsRejectedWithoutStateChangeOrRandomness(string scenario)
    {
        var state = Play(State());
        var payload = Move(state);
        var group = payload.Moves.Single();
        switch (scenario)
        {
            case "inland": group.To = "minas-tirith"; break;
            case "foreign-coast": group.To = "grey-havens"; break;
            case "wrong-origin": group.From = "west-harondor"; break;
            case "duplicate": group.UnitIds.Add(group.UnitIds[0]); break;
            case "extra-army": payload.Moves.Add(new()); break;
            case "extra-die": payload.DieId = state.Dice.First().Id; break;
            case "ability": payload.AbilityId = "character.messenger"; break;
            case "global-route": payload.Route = ["pelargir"]; break;
            case "army-route": group.Route = ["pelargir"]; break;
            case "foreign-unit": group.UnitIds.Add(BattleFixture.Put(state, "gondor", UnitKind.Regular, CorsairRules.Origin).Id); break;
            case "garrison": state.Units.Single(u => u.Id == group.UnitIds[0]).Garrison = true; break;
            case "non-belligerent":
                group.UnitIds.Add(BattleFixture.Put(state, "sauron", UnitKind.Regular, CorsairRules.Origin).Id);
                state.Nations.Single(n => n.Id == "sauron").StepsToWar = 1;
                break;
            case "unit-as-character": group.CharacterIds.Add(group.UnitIds[0]); group.UnitIds.RemoveAt(0); break;
            case "nazgul-alone": group.UnitIds = [BattleFixture.Put(state, "sauron", UnitKind.Nazgul, CorsairRules.Origin).Id]; break;
            case "immobile-character": group.CharacterIds.Add(Put(state, CharacterRules.Saruman, CorsairRules.Origin).Id); break;
        }
        Reject(state, CommandKind.MoveArmies, payload);
    }

    [Fact]
    public void LandingPreviewShowsCaptureAndUnknownBattleWithoutMovingOrRolling()
    {
        foreach (var enemy in new[] { 0, 1 })
        {
            var state = Play(State(defenders: enemy));
            var before = StateCodec.Write(state);
            var preview = new PreviewService(Engine()).Preview(state, Side.Shadow,
                BattleFixture.Command(state, CommandKind.MoveArmies, Move(state)));
            Assert.True(preview.Legal);
            Assert.Equal(enemy == 1, preview.HasUnknownResult);
            Assert.Equal(2, preview.Placements.Count);
            Assert.Equal(enemy == 0, preview.ControlChanges.ContainsKey("pelargir"));
            Assert.Equal(before, StateCodec.Write(state));
            Assert.Empty(state.RandomSamples);
        }
    }
}
