using Wotr.Contracts;

namespace Wotr.Domain;

public static class StrongholdRules
{
    public static bool CanShelter(MatchState state, BattleState battle) => battle.Mode == BattleMode.Field &&
        !battle.Defender.Garrison && BoardRules.Region(state, battle.Region).Settlement == "Stronghold" &&
        state.Control.GetValueOrDefault(battle.Region) == battle.Defender.Side;

    public static void Shelter(MatchState state, BattleState battle)
    {
        var force = battle.Defender;
        foreach (var unit in BattleRules.Units(state, force).ToArray())
            unit.Garrison = true;
        foreach (var character in BattleRules.Characters(state, force).ToArray())
            character.Garrison = true;
        force.Garrison = true;
        battle.DefenderRetreated = true;
        battle.DefenderSheltered = true;
        battle.Stage = BattleStage.Finish;
        state.Pending = null;
        state.Events.Add(new() { Revision = state.Revision, Kind = "StrongholdShelter", Text = $"{force.Side} withdrew into {battle.Region}." });
    }

    public static IEnumerable<string> Elites(MatchState state, BattleState battle) =>
        BattleRules.Units(state, battle.Attacker).Where(u => u.Kind == UnitKind.Elite).Select(u => u.Id);

    public static IEnumerable<string> ExtensionCosts(MatchState state, BattleState battle) =>
        Elites(state, battle).Concat(KingsWeaknesses.SiegeRegulars(state, battle));

    public static void Extend(MatchState state, BattleState battle, string eliteId)
    {
        var force = battle.Attacker;
        force.Participants = ReduceParticipant(state, force, eliteId);
        battle.Stage = BattleStage.NextRound;
        state.Pending = null;
        state.Events.Add(new() { Revision = state.Revision, Kind = "SiegeExtended", Text = $"{force.Side} reduced {eliteId} to extend the siege battle." });
    }

    public static List<string> ReduceParticipant(MatchState state, BattleForce force, string eliteId)
    {
        var participants = force.Participants.ToHashSet();
        var elite = state.Units.Single(u => u.Id == eliteId);
        if (UnitReduction.ReduceElite(state, elite) is { } replacement)
            participants.Add(replacement.Id);
        CasualtyRules.RemoveUnaccompanied(state, force.Region, force.Garrison, force.Side, CasualtyCause.Battle, participants);
        return [.. participants];
    }

    public static void ReconcileRegion(MatchState state, string region)
    {
        if (BoardRules.Region(state, region).Settlement != "Stronghold")
            return;
        var field = state.Units.Where(u => u.Location == region && !u.Garrison && BoardRules.IsArmyUnit(u)).Select(BoardRules.Owner).Distinct().ToArray();
        if (field.Length == 0)
        {
            TerritoryRules.ReleaseGarrison(state, region);
            return;
        }
        if (field.Length != 1)
            throw new InvalidOperationException("Opposing armies cannot occupy the same field outside a sortie.");
        if (!BoardRules.HasEnemyArmy(state, region, field[0], true))
        {
            TerritoryRules.ReleaseGarrison(state, region);
            TerritoryRules.Capture(state, field[0], region);
        }
    }

    public static void ReconcileOutsideBattle(MatchState state, IEnumerable<string> affectedRegions)
    {
        var battle = BattleRuntime.Active(state);
        foreach (var region in affectedRegions.Distinct())
            if (battle is null || region != battle.Region && region != battle.Attacker.Region)
                ReconcileRegion(state, region);
    }
}
