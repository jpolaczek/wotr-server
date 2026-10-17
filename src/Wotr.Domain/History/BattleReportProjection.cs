using Wotr.Contracts;

namespace Wotr.Domain;

// Builds an archive only from persisted public facts. No access to today's figures or rules execution.
public static class BattleReportProjection
{
    public static BattleReportView? Create(string match, string battleId, IEnumerable<PositionedEvent> source, BattleView? legacy = null)
    {
        var publicRecords = source.Where(r => r.Event.OnlyFor is null && r.Event.Activity is not null).OrderBy(r => r.Position).ToList();
        var records = Select(publicRecords, battleId);
        if (records.Count == 0)
            return legacy?.Id == battleId ? Legacy(match, legacy) : null;
        var ids = records.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        var orders = new Dictionary<long, int>();
        var steps = records.Select(r =>
        {
            var order = orders.GetValueOrDefault(r.Event.Revision) + 1;
            orders[r.Event.Revision] = order;
            return ActionLogProjection.Step(r with { Order = order }, ids);
        }).ToList();
        var started = steps.Find(s => s.Kind == "BattleStarted");
        var root = records[0].Event.Activity!.Action;
        var ended = steps.Any(s => s.Kind == "BattleEnded");
        var settlement = ended ? publicRecords.LastOrDefault(r => r.ActionId == root.Id &&
            r.Event.Activity!.Status == ActionLogStatus.Completed) : null;
        var report = new BattleReportView
        {
            MatchId = match, Id = battleId, ActionId = root.Id, Initiator = root.Initiator, Turn = root.Turn,
            Version = settlement?.Id ?? steps[^1].Id, Costs = HistoryFacts.Copy(root.Costs), Steps = steps,
            Initial = started?.Facts.Battle,
            Latest = steps.LastOrDefault(s => s.Facts.Battle?.Id == battleId && s.Kind != "LegacyBattleRound")?.Facts.Battle
                ?? steps.LastOrDefault(s => s.Facts.Battle?.Id == battleId)?.Facts.Battle,
            Place = steps.LastOrDefault(s => s.Facts.BattlePlace is not null)?.Facts.BattlePlace,
            Outcome = AtCommandBoundary(steps).LastOrDefault(s => s.Facts.Outcome is not null)?.Facts.Outcome,
            PartialHistory = started is null || records.Any(r => r.Event.Activity!.Action.PartialHistory),
            Legacy = steps.All(s => s.Kind == "LegacyBattleRound"),
            Status = ended ? settlement is not null ? BattleReportStatus.Completed : BattleReportStatus.AwaitingSettlement
                : BattleReportStatus.InProgress
        };
        if (report.Legacy)
        {
            report.Status = BattleReportStatus.Unknown;
            report.Turn = null;
        }
        if (report.Place is { } place)
        {
            var control = AtCommandBoundary(steps).SelectMany(s => s.Facts.Control).LastOrDefault(c => c.RegionId == place.RegionId);
            if (control is not null)
                place.Controller = control.After;
        }
        report.Rounds = Rounds(steps, battleId);
        if (started is null && legacy?.Id == battleId && legacy.CardsRevealed && !report.Rounds.Any(r => r.Number == legacy.Round))
            report.Rounds.Insert(0, LegacyRound(legacy));
        return report;
    }

    // A command fact contains its final checkpoint, even when the primary event precedes its producers.
    private static IEnumerable<ActionLogStep> AtCommandBoundary(List<ActionLogStep> steps) =>
        steps.OrderBy(s => s.Revision).ThenBy(s => s.Command is not null);

    private static List<PositionedEvent> Select(IEnumerable<PositionedEvent> source, string battleId)
    {
        var result = new List<PositionedEvent>();
        var contextByAction = new Dictionary<string, string>();
        foreach (var record in source)
        {
            var activity = record.Event.Activity!;
            var context = activity.Context.BattleId;
            if (context.Length > 0)
                contextByAction[record.ActionId] = context;
            else
                context = contextByAction.GetValueOrDefault(record.ActionId) ?? activity.Action.Context.BattleId;
            if (context == battleId)
                result.Add(record);
        }
        return result;
    }

    private static List<BattleReportRound> Rounds(List<ActionLogStep> steps, string id)
    {
        var groups = steps.Where(s => s.Facts.Battle?.Id == id)
            .GroupBy(s => s.Facts.Battle!.Round).OrderBy(g => g.Key);
        var result = new List<BattleReportRound>();
        foreach (var group in groups)
        {
            var snapshots = group.Select(s => s.Facts.Battle!).ToList();
            if (!snapshots.Any(b => b.CardsRevealed || b.Attacker.Dice.Count > 0 || b.Defender.Dice.Count > 0))
                continue;
            result.Add(new()
            {
                Number = group.Key, Initial = snapshots[0], Result = snapshots[^1],
                Completed = group.Any(s => s.Kind is "BattleRoundCompleted" or "LegacyBattleRound"),
                StepIds = steps.Where(s => (s.Context.BattleRound ?? s.Facts.Battle?.Round) == group.Key).Select(s => s.Id).ToList()
            });
        }
        return result;
    }

    private static BattleReportView Legacy(string match, BattleView battle)
    {
        var safe = PublicLegacy(battle);
        return new()
        {
            MatchId = match, Id = battle.Id, Legacy = true, PartialHistory = true,
            Status = BattleReportStatus.Unknown, Latest = safe,
            Rounds = safe.CardsRevealed ? [LegacyRound(safe)] : []
        };
    }

    private static BattleReportRound LegacyRound(BattleView battle) => new()
    {
        Number = battle.Round, Completed = battle.Stage == "RoundComplete", Result = PublicLegacy(battle)
    };

    internal static BattleView PublicLegacy(BattleView battle)
    {
        var result = HistoryFacts.Copy(battle);
        result.OwnTableCombatCards.Clear();
        foreach (var force in new[] { result.Attacker, result.Defender })
        {
            force.OwnRerollSelection.Clear();
            if (!result.CardsRevealed)
                force.CardId = null;
        }
        return result;
    }
}
