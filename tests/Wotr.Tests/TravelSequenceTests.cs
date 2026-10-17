using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.ActionSequenceFixture;

namespace Wotr.Tests;

public class TravelSequenceTests
{
    [Fact]
    public void EachCompanionTravelsOnceAndCannotBorrowTheArrivedGroupsLevel()
    {
        var state = Board();
        state.Dice[0].Face = DieFace.Character;
        var gimli = CharacterFixture.Put(state, "base.character.gimli", "rivendell").Id;
        var hobbit = CharacterFixture.Put(state, "base.character.merry", "hollin").Id;
        var payload = Travel(state, gimli, "rivendell", "trollshaws", "hollin");
        state = Do(state, CommandKind.MoveCompanions, payload);
        state = StateCodec.Copy(state);
        Assert.Equal(1, state.Resolution.ActionSequence!.Remaining);
        var travel = ActionSequenceOptions.For(state, Side.FreePeoples).Single(o => o.Kind == CommandKind.MoveCompanions);
        Assert.Equal(hobbit, Assert.Single(travel.EligibleIds));
        Reject(state, CommandKind.MoveCompanions, Travel(state, hobbit, "hollin", "trollshaws", "rivendell"));
        var merged = Travel(state, hobbit, "hollin", "moria");
        merged.CompanionMoves[0].Ids.Add(gimli);
        Reject(state, CommandKind.MoveCompanions, merged);
        state = Do(state, CommandKind.MoveCompanions, Travel(state, hobbit, "hollin", "moria"));
        Assert.Null(state.Resolution.ActionSequence);
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
        Assert.Equal("hollin", state.Characters.Single(c => c.Id == gimli).Location);
    }

    [Fact]
    public void FlightsPersistIndependentlyAndPreserveForbiddenStrongholds()
    {
        var state = Board(Side.Shadow);
        state.Dice.First(d => d.Owner == Side.Shadow).Face = DieFace.Character;
        var first = Put(state, "sauron", "barad-dur", UnitKind.Nazgul).Id;
        var second = Put(state, "sauron", "barad-dur", UnitKind.Nazgul).Id;
        state = Do(state, CommandKind.MoveShadowCharacters, Travel(state, first, "barad-dur", "hollin"));
        state = StateCodec.Copy(state);
        Assert.Equal(1, state.Resolution.ActionSequence!.Remaining);
        Reject(state, CommandKind.MoveShadowCharacters, Travel(state, first, "hollin", "moria"));
        Reject(state, CommandKind.MoveShadowCharacters, Travel(state, second, "barad-dur", "rivendell"));
        state = Do(state, CommandKind.MoveShadowCharacters, Travel(state, second, "barad-dur", "moria"));
        Assert.Null(state.Resolution.ActionSequence);
        Assert.Equal(1, state.Dice.Count(d => d.Location == DieLocation.Spent));
    }

    internal static CommandPayload Travel(MatchState state, string id, params string[] route) => new()
    {
        Stepwise = true,
        DieId = state.Resolution.ActionSequence?.DieId ?? state.Dice.First(d => d.Owner == state.Pending!.Owner).Id,
        AbilityId = state.Resolution.ActionSequence?.AbilityId ?? "",
        CompanionMoves = [new() { Ids = [id], Route = [.. route] }]
    };
}
