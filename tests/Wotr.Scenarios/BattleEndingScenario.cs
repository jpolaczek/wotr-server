using Wotr.Contracts;
using Wotr.Domain;

internal static class BattleEndingScenario
{
    internal static MatchState Report(IRandomSource random)
    {
        var state = BattleScenario.Retreat(random);
        state = ExpansionUiFixture.Execute(state, CommandKind.ContinueBattle);
        state = ExpansionUiFixture.Execute(state, CommandKind.HoldBattle);
        state = ExpansionUiFixture.Execute(state, CommandKind.CommitCombatCard);
        state = ExpansionUiFixture.Execute(state, CommandKind.CommitCombatCard, null, new int[6]);
        state = ExpansionUiFixture.Execute(state, CommandKind.RerollBattle);
        return ExpansionUiFixture.Execute(state, CommandKind.StopBattle);
    }

    internal static MatchState Siege(IRandomSource random, bool noRegular = false, bool weakness = false)
    {
        var state = ExpansionUiFixture.Actions(random, new() { Kings = weakness });
        foreach (var unit in state.Units) BoardRules.ReturnToReserve(unit);
        foreach (var nation in state.Nations) { nation.Active = true; nation.StepsToWar = 0; }
        foreach (var die in state.Dice) die.Face = DieFace.Army;
        Put(state, "gondor", UnitKind.Regular, "minas-tirith", 3, true);
        Put(state, "sauron", UnitKind.Elite, "minas-tirith", noRegular ? 1 : 2);
        if (weakness)
        {
            Put(state, "sauron", UnitKind.Regular, "minas-tirith", 1);
            var sovereign = ExpansionUiFixture.Sovereign(state, SovereignCatalog.Denethor);
            sovereign.Status = SovereignStatus.Corrupted;
            sovereign.CorruptedRegion = "minas-tirith";
        }
        if (noRegular)
            foreach (var unit in state.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular))
                unit.Location = "gorgoroth";
        TurnFlow.SelectNext(state, Side.Shadow);
        state = ExpansionUiFixture.Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = ExpansionUiFixture.Die(state, Side.Shadow).Id, MovementCost = DieFace.Army,
            Moves = [new() { From = "minas-tirith", To = "minas-tirith",
                UnitIds = state.Units.Where(u => u.Location == "minas-tirith" && BoardRules.Owner(u) == Side.Shadow).Select(u => u.Id).ToList() }]
        });
        state = ExpansionUiFixture.Execute(state, CommandKind.CommitCombatCard);
        return ExpansionUiFixture.Execute(state, CommandKind.CommitCombatCard, null, new int[6]);
    }

    private static void Put(MatchState state, string nation, UnitKind kind, string region, int count, bool garrison = false)
    {
        foreach (var unit in state.Units.Where(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve" && !u.Id.StartsWith("kings.")).Take(count))
        {
            unit.Location = region;
            unit.Garrison = garrison;
        }
    }
}
