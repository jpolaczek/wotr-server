using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.ShadowCharacterCards02;
using static CardPackScenario;

internal static class Shadow02Scenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["shadow-search"] = Travel(random, Search),
        ["shadow-strike"] = Travel(random, Strike),
        ["shadow-weather"] = WeatherChoice(random),
        ["shadow-dread"] = Combat(random, Candles),
        ["shadow-terrible"] = Combat(random, FoulThing),
        ["shadow-black-breath"] = Combat(random, Strike)
    };

    private static MatchState Travel(IRandomSource random, string card)
    {
        var state = EmptyBoard(random);
        state.Fellowship.RegionId = "hollin";
        state.Fellowship.Progress = 2;
        Unit(state, "sauron", UnitKind.Nazgul, "barad-dur");
        Character(state, CharacterRules.WitchKing, "barad-dur");
        Character(state, CharacterRules.Mouth, "barad-dur");
        foreach (var protection in new[] { FreePeoplesCharacterCards01.Mithril, FreePeoplesCharacterCards01.Axe, FreePeoplesCharacterCards02.Horn, FreePeoplesCharacterCards02.Staff })
        {
            Give(state, protection);
            HandRules.Remove(state.Player(Side.FreePeoples), protection);
            state.Player(Side.FreePeoples).Table.Add(protection);
            state.ActiveEffects.Add(new() { Id = "table-" + protection, SourceId = protection, Owner = Side.FreePeoples });
        }
        TurnFlow.SelectNext(state, Side.Shadow);
        return PlayEvent(state, card);
    }

    private static MatchState WeatherChoice(IRandomSource random)
    {
        var state = EmptyBoard(random);
        state.Fellowship.RegionId = "rivendell";
        state.Fellowship.Progress = 2;
        TurnFlow.SelectNext(state, Side.Shadow);
        return PlayEvent(state, Weather);
    }

    private static MatchState Combat(IRandomSource random, string card)
    {
        var state = EmptyBoard(random);
        for (var i = 0; i < 6; i++)
            Unit(state, "gondor", UnitKind.Regular, "minas-tirith");
        Unit(state, "gondor", UnitKind.Leader, "minas-tirith");
        Character(state, "base.character.gimli", "minas-tirith");
        for (var i = 0; i < 3; i++)
            Unit(state, "sauron", UnitKind.Regular, "lossarnach");
        for (var i = 0; i < 8; i++)
            Unit(state, "sauron", UnitKind.Nazgul, "lossarnach");
        Character(state, CharacterRules.WitchKing, "lossarnach");
        Give(state, card);
        state.Dice[0].Face = DieFace.Army;
        state = Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = state.Dice[0].Id,
            Moves = [new() { From = "minas-tirith", To = "lossarnach",
                UnitIds = state.Units.Where(u => u.Location == "minas-tirith").Select(u => u.Id).ToList(),
                CharacterIds = state.Characters.Where(c => c.Location == "minas-tirith").Select(c => c.Id).ToList() }]
        });
        state = Execute(state, CommandKind.CommitCombatCard, new());
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = card }, card == Candles ? [] : new int[8]);
        if (card != Strike)
            return state;
        state = Execute(state, CommandKind.RerollBattle, new());
        var dice = BattleRuntime.Active(state)!.Force(Side.Shadow).Round.Dice.Take(2).Select(d => d.Id).ToList();
        return Execute(state, CommandKind.RerollBattle, new() { Ids = dice }, 5, 5);
    }
}
