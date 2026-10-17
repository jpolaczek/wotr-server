using Wotr.Contracts;

namespace Wotr.Domain;

public static class BattlePreviewRules
{
    public static void Describe(MatchState state, GameCommand command, ActionPreview preview)
    {
        if (command.Kind == CommandKind.MoveArmies && ArmyEvents.Current(state, Side.Shadow) == ArmyEventOrder.Corsairs)
            preview.HasUnknownResult = BoardRules.HasEnemyArmy(state, command.Payload.Moves.Single().To, Side.Shadow);
        if (command.Kind is CommandKind.AttackArmy or CommandKind.CommitCombatCard or CommandKind.RerollBattle or CommandKind.ContinueBattle or CommandKind.HoldBattle or CommandKind.ExtendSiege or CommandKind.UseEvilKnowledge)
            preview.HasUnknownResult = true;
        if (command.Kind == CommandKind.UseEvilKnowledge)
            preview.Costs.Add(LordsCharacters.EvilKnowledge);
        if (command.Kind == CommandKind.TakeBattleLosses && BattleLosses.Plan(state, command.Payload.Losses) is { } plan)
        {
            var current = BattleLosses.Current(state)!;
            DescribeLosses(state, plan, current.Hits, current.Voluntary, preview);
            if (BattleLosses.Current(state) is { Voluntary: true } loss)
                preview.HasUnknownResult = loss.AdditionalAttack ? plan.AppliedHits > 0 : plan.Board.Units
                    .Any(u => plan.Participants.Contains(u.Id) && BoardRules.Regions.ContainsKey(u.Location) && BoardRules.IsArmyUnit(u));
        }
        if (command.Kind == CommandKind.ExtendSiege)
        {
            var board = MovementPlan.CopyBoard(state);
            var battle = BattleRuntime.Current(state)!;
            var selected = command.Payload.Ids.Single();
            if (KingsWeaknesses.SiegeRegulars(state, battle).Contains(selected))
                CasualtyRules.Eliminate(board, [selected], CasualtyCause.Battle, battle.Attacker.Participants.ToHashSet());
            else
                StrongholdRules.ReduceParticipant(board, battle.Attacker, selected);
            DescribeBoard(state, board, preview);
        }
        if (command.Kind == CommandKind.AdvanceBattle && command.Payload.Ids.Count > 0)
        {
            var battle = BattleRuntime.Current(state)!;
            preview.Placements = command.Payload.Ids.Select(id => new UnitPlacementChoice { UnitId = id, RegionId = battle.Region }).ToList();
            if (BoardRules.IsSettlement(BoardRules.Region(state, battle.Region)) && state.Control.GetValueOrDefault(battle.Region) != battle.Attacker.Side &&
                !BoardRules.HasEnemyArmy(state, battle.Region, battle.Attacker.Side))
                preview.ControlChanges[battle.Region] = battle.Attacker.Side;
            if (battle.DefenderSheltered && BattleRules.Strength(state, battle.Defender) > 5)
                preview.Excess.Add(new() { RegionId = battle.Region, Garrison = true, Count = BattleRules.Strength(state, battle.Defender) - 5 });
            var friendlyArmy = state.Units.Count(u => u.Location == battle.Region && BoardRules.Owner(u) == battle.Attacker.Side && BoardRules.IsArmyUnit(u));
            var arrivingArmy = state.Units.Count(u => command.Payload.Ids.Contains(u.Id) && BoardRules.IsArmyUnit(u));
            if (friendlyArmy + arrivingArmy > 10)
                preview.Excess.Add(new() { RegionId = battle.Region, Count = friendlyArmy + arrivingArmy - 10 });
        }
    }

    internal static void DescribeBoard(MatchState state, MatchState board, ActionPreview preview)
    {
        var survivors = board.Units.Where(u => BoardRules.Regions.ContainsKey(u.Location)).Select(u => u.Id).ToHashSet();
        var survivingCharacters = board.Characters.Where(c => BoardRules.Regions.ContainsKey(c.Location)).Select(c => c.Id).ToHashSet();
        preview.RemovedIds = state.Units.Where(u => BoardRules.Regions.ContainsKey(u.Location) && !survivors.Contains(u.Id)).Select(u => u.Id)
            .Concat(state.Characters.Where(c => BoardRules.Regions.ContainsKey(c.Location) && !survivingCharacters.Contains(c.Id)).Select(c => c.Id)).ToList();
        preview.Placements = board.Units.Where(u => survivors.Contains(u.Id) && state.Units.Single(old => old.Id == u.Id).Location != u.Location)
            .Select(u => new UnitPlacementChoice { UnitId = u.Id, RegionId = u.Location }).ToList();
    }

    internal static void DescribeLosses(MatchState state, ArmyLossPlan plan, int hits, bool voluntary, ActionPreview preview)
    {
        DescribeBoard(state, plan.Board, preview);
        preview.Losses = new ArmyLossPreview
        {
            RequiredHits = hits,
            AppliedHits = plan.AppliedHits,
            Voluntary = voluntary,
            Operations = [.. plan.LossFacts ?? []]
        };
    }
}
