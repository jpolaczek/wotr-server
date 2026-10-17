using Wotr.Contracts;

namespace Wotr.Domain;

public static class BattleMovement
{
    public static IEnumerable<string> Retreats(MatchState state, BattleState battle) => battle.Mode == BattleMode.Siege ? [] :
        BoardRules.Regions[battle.Defender.Region].Neighbors.Where(to => IsFree(state, battle.Defender.Side, to));

    public static bool IsFree(MatchState state, Side actor, string region)
    {
        if (RegionProtection.Blocks(state, actor, region) || BoardRules.HasEnemyArmy(state, region, actor, false))
            return false;
        if (BoardRules.Besieged(state, region, TurnFlow.Other(actor)))
            return true;
        return !BoardRules.HasEnemyArmy(state, region, actor) &&
            (!BoardRules.IsSettlement(BoardRules.Region(state, region)) || state.Control.GetValueOrDefault(region) != TurnFlow.Other(actor));
    }

    public static bool CanAdvance(MatchState state, BattleState battle, IReadOnlyList<string> ids)
    {
        if (ids.Count == 0)
            return true;
        var force = battle.Attacker;
        var units = BattleRules.Units(state, force).ToList();
        var characters = BattleRules.Characters(state, force).Where(CharacterRules.Mobile).ToList();
        if (!DraftSelection.IsSubset(ids, units.Select(u => u.Id).Concat(characters.Select(c => c.Id)).ToArray(), 1, units.Count + characters.Count) ||
            !units.Any(u => ids.Contains(u.Id) && BoardRules.IsArmyUnit(u)))
            return false;
        var remaining = state.Units.Where(u => u.Location == force.Region && u.Garrison == force.Garrison && BoardRules.Owner(u) == force.Side && !ids.Contains(u.Id)).ToList();
        return !remaining.Any(u => u.Kind == UnitKind.Leader) || remaining.Any(BoardRules.IsArmyUnit);
    }

    public static void Retreat(MatchState state, BattleState battle, string region)
    {
        var force = battle.Defender;
        var activity = MovementFacts.Event(state, ActionMovementKind.Retreat, force.Region, region,
            BattleRules.Units(state, force), BattleRules.Characters(state, force).Where(CharacterRules.Mobile));
        Relocate(state, force, BattleRules.Units(state, force).Select(u => u.Id)
            .Concat(BattleRules.Characters(state, force).Where(CharacterRules.Mobile).Select(c => c.Id)).ToArray(), region, false);
        force.Region = region;
        battle.DefenderRetreated = true;
        battle.Stage = BattleStage.Finish;
        state.Pending = null;
        StackLimits.OpenIfRequired(state, force.Side);
        activity.Facts.Battle = BattleProjection.Public(state, battle);
        state.Events.Add(new() { Revision = state.Revision, Kind = "BattleRetreat", Activity = activity,
            Text = $"{force.Side} retreated to {region}." });
    }

    public static void Advance(MatchState state, BattleState battle, IReadOnlyList<string> ids)
    {
        if (ids.Count > 0)
        {
            var activity = MovementFacts.Event(state, ActionMovementKind.Advance, battle.Attacker.Region, battle.Region,
                state.Units.Where(u => ids.Contains(u.Id)), state.Characters.Where(c => ids.Contains(c.Id)));
            Relocate(state, battle.Attacker, ids, battle.Region, true);
            activity.Facts.Battle = BattleProjection.Public(state, battle);
            state.Events.Add(new() { Revision = state.Revision, Kind = "BattleAdvance", Activity = activity,
                Text = $"{battle.Attacker.Side} advanced into {battle.Region}." });
        }
        battle.Stage = BattleStage.Complete;
        state.Pending = null;
        StrongholdRules.ReconcileRegion(state, battle.Region);
        if (!StackLimits.OpenIfRequired(state, battle.Defender.Side))
            StackLimits.OpenIfRequired(state, battle.Attacker.Side);
    }

    private static void Relocate(MatchState state, BattleForce force, IReadOnlyList<string> ids, string destination, bool activateNation)
    {
        foreach (var unit in state.Units.Where(u => ids.Contains(u.Id)))
        {
            unit.Location = destination;
            unit.Garrison = false;
        }
        foreach (var character in state.Characters.Where(c => ids.Contains(c.Id)))
        {
            character.Location = destination;
            character.Garrison = false;
            if (activateNation && character.Side == Side.FreePeoples)
                PoliticalRules.CompanionArrived(state, destination, character.NationIcon);
        }
        TerritoryRules.Arrive(state, force.Side, force.Region, destination, activateNation);
    }
}
