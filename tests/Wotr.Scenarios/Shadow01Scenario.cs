using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.ShadowCharacterCards01;
using static CardPackScenario;

internal static class Shadow01Scenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["shadow-patrol"] = Hunt(random, OrcPatrol),
        ["shadow-bane"] = Hunt(random, IsildursBane),
        ["shadow-cruel"] = Cruel(random)
    };

    private static MatchState Hunt(IRandomSource random, string card)
    {
        var state = EmptyBoard(random);
        state.Fellowship.RegionId = "hollin";
        state.Fellowship.Progress = 0;
        foreach (var protection in new[] { FreePeoplesCharacterCards01.Mithril, FreePeoplesCharacterCards01.Axe, FreePeoplesCharacterCards02.Horn })
        {
            Give(state, protection);
            HandRules.Remove(state.Player(Side.FreePeoples), protection);
            state.Player(Side.FreePeoples).Table.Add(protection);
        }
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id == "base.hunt.standard.3.0.1" ? "pool" : "removed";
        TurnFlow.SelectNext(state, Side.Shadow);
        return PlayEvent(state, card, 0);
    }

    private static MatchState Cruel(IRandomSource random)
    {
        var state = EmptyBoard(random);
        Unit(state, "gondor", UnitKind.Regular, "minas-tirith");
        Unit(state, "gondor", UnitKind.Leader, "minas-tirith");
        for (var i = 0; i < 3; i++)
        {
            Unit(state, "sauron", UnitKind.Regular, "lossarnach");
            Unit(state, "sauron", UnitKind.Nazgul, "lossarnach");
        }
        Character(state, CharacterRules.WitchKing, "lossarnach");
        Give(state, OrcPatrol);
        state.Dice[0].Face = DieFace.Army;
        state = Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = state.Dice[0].Id,
            Moves = [new() { From = "minas-tirith", To = "lossarnach", UnitIds = state.Units.Where(u => u.Location == "minas-tirith").Select(u => u.Id).ToList() }]
        });
        state = Execute(state, CommandKind.CommitCombatCard, new());
        return Execute(state, CommandKind.CommitCombatCard, new() { CardId = OrcPatrol });
    }
}
