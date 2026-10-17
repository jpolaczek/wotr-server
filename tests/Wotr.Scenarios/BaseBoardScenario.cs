using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

internal static class BaseBoardScenario
{
    public static Dictionary<string, MatchState> Create(MatchState movement)
    {
        return new[] { "split", "two-moves", "temporary-excess", "final-excess", "recruit", "elite", "leader", "politics", "recruit-excess" }
            .ToDictionary(name => "board-" + name, name => Position(movement, name));
    }

    private static MatchState Position(MatchState movement, string name)
    {
        var state = StateCodec.Copy(movement);
        state.Id = Guid.NewGuid().ToString("N");
        foreach (var unit in state.Units.Where(u => BoardRules.Nations[u.Nation].Side == Side.FreePeoples))
            BoardRules.ReturnToReserve(unit);
        Place(state, "gondor", UnitKind.Regular, "minas-tirith", 3);
        Place(state, "gondor", UnitKind.Elite, "minas-tirith", 1);
        Place(state, "gondor", UnitKind.Leader, "minas-tirith", 1);
        Place(state, "rohan", UnitKind.Regular, "minas-tirith", 2);
        Place(state, "north", UnitKind.Regular, "minas-tirith", 2);
        Place(state, "north", UnitKind.Leader, "minas-tirith", 1);
        Place(state, "north", UnitKind.Regular, "dale", 2);
        Place(state, "gondor", UnitKind.Regular, "osgiliath", name == "split" ? 1 : 4);
        var strider = CharacterCatalog.Companion(state, "base.character.strider");
        state.Fellowship.Companions.Remove(strider.Id);
        strider.Location = "minas-tirith";
        state.Characters.Add(strider);
        state.Dice.Last(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Face = DieFace.WillOfTheWest;
        if (name == "recruit")
            foreach (var unit in state.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular && u.Location == "reserve").Skip(1))
                unit.Location = "casualties";
        if (name == "recruit-excess")
            Place(state, "gondor", UnitKind.Regular, "minas-tirith", 2);
        if (name == "politics")
        {
            state.Nations.Single(n => n.Id == "elves").StepsToWar = 2;
            var rohan = state.Nations.Single(n => n.Id == "rohan");
            rohan.StepsToWar = 1;
            rohan.Active = false;
            var north = state.Nations.Single(n => n.Id == "north");
            north.StepsToWar = 3;
            north.Active = false;
        }
        TurnFlow.SelectNext(state, Side.FreePeoples);
        return state;
    }

    private static void Place(MatchState state, string nation, UnitKind kind, string region, int count)
    {
        var pieces = state.Units.Where(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve").Take(count).ToArray();
        if (pieces.Length != count)
            throw new InvalidOperationException("Board fixture requires available pieces.");
        foreach (var piece in pieces)
            piece.Location = region;
    }
}
