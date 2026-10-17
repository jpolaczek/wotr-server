using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Tests;

internal static class Strategy02Fixture
{
    public static MatchState March(string origin = "north-ithilien")
    {
        var state = BattleFixture.State();
        BattleFixture.Put(state, "gondor", UnitKind.Regular, origin);
        CharacterFixture.Put(state, "base.character.gimli", origin);
        return state;
    }

    public static MatchState Passage(bool siege = true)
    {
        var state = March("helms-deep");
        BattleFixture.Put(state, "rohan", UnitKind.Leader, "helms-deep");
        if (siege)
        {
            foreach (var unit in state.Units.Where(u => u.Location == "helms-deep"))
                unit.Garrison = true;
            state.Characters.Single().Garrison = true;
            BattleFixture.Put(state, "isengard", UnitKind.Regular, "helms-deep");
        }
        return state;
    }

    public static MatchState Relief(int garrison = 2, int shadow = 8)
    {
        var state = BattleFixture.State();
        for (var i = 0; i < 3; i++)
            BattleFixture.Put(state, "gondor", UnitKind.Regular, "osgiliath");
        for (var i = 0; i < garrison; i++)
            BattleFixture.Put(state, "gondor", i == 0 ? UnitKind.Elite : UnitKind.Regular, "minas-tirith").Garrison = true;
        BattleFixture.Put(state, "gondor", UnitKind.Leader, "minas-tirith").Garrison = true;
        CharacterFixture.Put(state, "base.character.boromir", "minas-tirith").Garrison = true;
        for (var i = 0; i < shadow; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, "minas-tirith");
        return state;
    }

    public static CommandPayload Move(MatchState state, string origin, params string[] route) => new()
    {
        Moves = [new()
        {
            From = origin,
            To = route[^1],
            UnitIds = state.Units.Where(u => u.Location == origin && BoardRules.Owner(u) == Side.FreePeoples).Select(u => u.Id).ToList(),
            CharacterIds = state.Characters.Where(c => c.Location == origin && c.Side == Side.FreePeoples).Select(c => c.Id).ToList()
        }],
        Route = [.. route]
    };

    public static CommandPayload Attack(MatchState state)
    {
        var payload = Move(state, "osgiliath", "minas-tirith");
        payload.Route.Clear();
        return payload;
    }
}
