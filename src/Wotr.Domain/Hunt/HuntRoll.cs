using Wotr.Contracts;

namespace Wotr.Domain;

public static class HuntRoll
{
    public static bool Success(int value, int bonus) => value != 1 && value + bonus >= 6;
    public static bool Success(HuntProcedure hunt, HuntDie die) => Success(die.Result, hunt.Bonus + (die.Rerolled ? hunt.RerollBonus : 0));
    public static List<string> Failed(HuntProcedure hunt) => hunt.Dice.Where(d => !Success(hunt, d)).Select(d => d.Id).ToList();

    public static void Roll(MatchState state, HuntProcedure hunt, IRandomSource random)
    {
        hunt.Bonus = hunt.RollBonus + state.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Hunt && d.HuntOrigin == DiceRules.FellowshipMove);
        hunt.RerollBonus = KingsChieftainHunt.RerollBonus(state);
        var level = Math.Min(5, state.Dice.Count(DiceRules.IsShadowHuntDie));
        if (level > 0)
            level = Math.Max(1, level - hunt.DiceReduction);
        for (var i = 0; i < level; i++)
        {
            var value = random.Next(6) + 1;
            hunt.Dice.Add(new() { Id = "hunt-die-" + i, First = value, Result = value });
        }
        hunt.RerollSources = Sources(state);
        hunt.EyeDamage = hunt.Dice.Count(d => Success(hunt, d));
        Record(state, hunt, "HuntRolled");
        HuntReports.Record(state, hunt, HuntReportKind.Rolled);
        if (Math.Min(Failed(hunt).Count, hunt.RerollSources.Count) == 0)
            return;
        state.Open(DecisionKind.HuntReroll, Side.Shadow, [CommandKind.RerollHunt]);
        state.Pending!.PrivateChoices = Failed(hunt);
    }

    public static void Reroll(MatchState state, HuntProcedure hunt, IReadOnlyList<string> ids, IRandomSource random)
    {
        foreach (var die in hunt.Dice.Where(d => ids.Contains(d.Id)))
        {
            die.Result = random.Next(6) + 1;
            die.Rerolled = true;
        }
        hunt.EyeDamage = hunt.Dice.Count(d => Success(hunt, d));
        Record(state, hunt, "HuntRerolled");
        HuntReports.Record(state, hunt, HuntReportKind.Rerolled);
        state.Pending = null;
    }

    private static List<string> Sources(MatchState state)
    {
        var region = state.Fellowship.RegionId;
        var sources = new List<string>();
        if (TravelRules.ShadowStronghold(state, region))
            sources.Add("stronghold");
        if (state.Units.Any(u => u.Location == region && BoardRules.Owner(u) == Side.Shadow && BoardRules.IsArmyUnit(u)))
            sources.Add("army");
        if (state.Units.Any(u => u.Location == region && u.Kind == UnitKind.Nazgul) ||
            state.Characters.Any(c => c.Location == region && CharacterRules.IsWitchKing(c)))
            sources.Add("nazgul");
        return sources;
    }

    private static void Record(MatchState state, HuntProcedure hunt, string kind) => state.Events.Add(new()
    {
        Revision = state.Revision,
        Kind = kind,
        Text = $"Hunt: {string.Join(", ", hunt.Dice.Select(d => d.Result))}; bonus {hunt.Bonus}, successes {hunt.EyeDamage}."
    });
}
