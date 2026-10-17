using Wotr.Contracts;
using Wotr.Domain;
using static CardPackScenario;

internal static class CardPackBattleScenario
{
    public static MatchState BeforeCards(IRandomSource random, string card)
    {
        var state = FellowshipScenario.Actions(random);
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        foreach (var nation in state.Nations)
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
        for (var i = 0; i < 3; i++)
        {
            Unit(state, "gondor", UnitKind.Regular, "minas-tirith");
            Unit(state, "sauron", UnitKind.Regular, "lossarnach");
        }
        Unit(state, "gondor", UnitKind.Leader, "minas-tirith");
        Unit(state, "sauron", UnitKind.Nazgul, "lossarnach");
        Character(state, CharacterRules.Aragorn, "minas-tirith");
        Character(state, "base.character.gimli", "minas-tirith");
        Character(state, CharacterRules.WitchKing, "lossarnach");
        Character(state, CharacterRules.Mouth, "lossarnach");
        Give(state, card);
        state.Dice[0].Face = DieFace.Army;
        return Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = state.Dice[0].Id,
            MovementCost = DieFace.Army,
            Moves = [new() { From = "minas-tirith", To = "lossarnach",
                UnitIds = state.Units.Where(u => u.Location == "minas-tirith").Select(u => u.Id).ToList(),
                CharacterIds = state.Characters.Where(c => c.Location == "minas-tirith").Select(c => c.Id).ToList() }]
        });
    }
}
