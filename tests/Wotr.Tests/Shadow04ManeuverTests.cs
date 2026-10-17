using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards04;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow04Fixture;

namespace Wotr.Tests;

public class Shadow04ManeuverTests
{
    internal static MatchState Orders(string card = Ringwraiths)
    {
        var state = Played(card);
        if (card == Captain)
            state = Do(state, CommandKind.ChooseEffect, new() { Ids = [BlackCaptainStep.Fly] });
        return Do(state, CommandKind.MoveShadowCharacters, new());
    }

    [Theory]
    [InlineData(Ringwraiths)]
    [InlineData(Captain)]
    public void FlightAndArmyManeuverAreSeparateAndFinishingIsOptional(string card)
    {
        var state = State(card);
        var mouth = Put(state, CharacterRules.Mouth, "osgiliath");
        state = Play(state, card);
        if (card == Captain)
            state = Do(state, CommandKind.ChooseEffect, new() { Ids = [BlackCaptainStep.Fly] });
        Reject(state, CommandKind.MoveShadowCharacters, new() { CompanionMoves = [new() { Ids = [mouth.Id], Route = ["osgiliath", "lossarnach"] }] });
        state = Do(state, CommandKind.MoveShadowCharacters, new());
        Assert.Equal(DecisionKind.ArmyEvent, state.Pending!.Kind);
        Assert.Contains(CommandKind.FinishArmyEvent, state.Pending.Commands);
        Reject(state, CommandKind.FinishArmyEvent, new() { DieId = state.Dice.First(d => d.Location == DieLocation.Available).Id });
        state = Do(state, CommandKind.FinishArmyEvent);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(Ringwraiths)]
    [InlineData(Captain)]
    public void ActualMovingGroupMustIncludeNazgulOrWitchKingAndCannotPayAnotherDie(string card)
    {
        var state = Orders(card);
        var payload = Army(state, "osgiliath", "north-ithilien");
        var valid = StateCodec.Copy(state);
        var king = CharacterRules.InPlay(state, CharacterRules.WitchKing)!.Id;
        payload.Moves[0].CharacterIds.Remove(king);
        if (card == Ringwraiths)
            payload.Moves[0].UnitIds.RemoveAll(id => state.Units.Single(u => u.Id == id).Kind == UnitKind.Nazgul);
        Reject(state, CommandKind.MoveArmies, payload);
        payload = Army(state, "osgiliath", "north-ithilien");
        payload.DieId = state.Dice.First(d => d.Location == DieLocation.Available).Id;
        Reject(state, CommandKind.MoveArmies, payload);
        payload.DieId = "";
        state = Do(valid, CommandKind.MoveArmies, payload);
        Assert.Equal("north-ithilien", CharacterRules.InPlay(state, CharacterRules.WitchKing)!.Location);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Fact]
    public void TwoArmiesMayMergeButNoFigureMovesTwiceAndExcessWaitsForBothMoves()
    {
        var state = State();
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "south-ithilien");
        BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "south-ithilien");
        for (var i = 0; i < 8; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, "north-ithilien");
        state = Play(state, Ringwraiths);
        state = Do(state, CommandKind.MoveShadowCharacters, new());
        var payload = Army(state, "osgiliath", "north-ithilien");
        payload.Moves.Add(Army(state, "south-ithilien", "north-ithilien").Moves[0]);
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow, BattleFixture.Command(state, CommandKind.MoveArmies, payload));
        Assert.True(preview.Legal);
        Assert.Equal(2, Assert.Single(preview.Excess).Count);
        var duplicate = new CommandPayload { Moves = [payload.Moves[0], payload.Moves[0]] };
        Reject(state, CommandKind.MoveArmies, duplicate);
        state = Do(state, CommandKind.MoveArmies, payload);
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Equal(12, state.Units.Count(u => u.Location == "north-ithilien" && BoardRules.IsArmyUnit(u)));
        Assert.Single(state.Dice, d => d.Location == DieLocation.Resolving);
        state = Do(state, CommandKind.RemoveExcess, new() { Ids = state.Units.Where(u => u.Location == "north-ithilien" && BoardRules.IsArmyUnit(u)).Take(2).Select(u => u.Id).ToList() });
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void CaptainAttackRequiresKingAmongParticipantsAndCannotMoveTwoArmies()
    {
        var state = Orders(Captain);
        var payload = Army(state, "osgiliath", "minas-tirith");
        payload.Moves[0].CharacterIds.Clear();
        payload.Moves[0].UnitIds.Remove(state.Units.First(u => u.Location == "osgiliath" && u.Kind == UnitKind.Regular).Id);
        Reject(state, CommandKind.AttackArmy, payload);
        payload = Army(state, "osgiliath", "north-ithilien");
        payload.Moves.Add(payload.Moves[0]);
        Reject(state, CommandKind.MoveArmies, payload);
        state = Do(state, CommandKind.AttackArmy, Army(state, "osgiliath", "minas-tirith"));
        Assert.Contains(CharacterRules.InPlay(state, CharacterRules.WitchKing)!.Id, BattleRuntime.Active(state)!.Attacker.Participants);
        Assert.Equal(EffectReturn.Parent, BattleRuntime.Active(state)!.Return);
    }
}
