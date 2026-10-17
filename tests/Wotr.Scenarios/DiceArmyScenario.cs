using Wotr.Contracts;
using Wotr.Domain;

internal static class DiceArmyScenario
{
    public static Dictionary<string, MatchState> Create(MatchState movement, IRandomSource random)
    {
        var result = BaseBoardScenario.Create(movement)
            .ToDictionary(pair => "ui17-" + pair.Key, pair => pair.Value);
        var logMoves = StateCodec.Copy(result["ui17-board-two-moves"]);
        logMoves.Id = Guid.NewGuid().ToString("N");
        result.Add("ui47-log-increment", logMoves);
        result.Add("ui47-live-activity", LiveActivityScenario.Create(random));
        var battles = BattleScenario.Create(random);
        result.Add("ui17-attack-fp", battles["field"]);
        result.Add("ui17-attack-sh", battles["shelter-stay"]);
        var garrison = StateCodec.Copy(battles["field"]);
        garrison.Id = Guid.NewGuid().ToString("N");
        foreach (var unit in garrison.Units.Where(u => u.Location == "minas-tirith"))
            unit.Garrison = true;
        foreach (var unit in garrison.Units.Where(u => u.Location == "lossarnach"))
            unit.Location = "minas-tirith";
        TurnFlow.SelectNext(garrison, Side.FreePeoples);
        result.Add("ui17-sortie", garrison);
        return result;
    }
}
