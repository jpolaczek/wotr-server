using Wotr.Contracts;

namespace Wotr.Domain;

internal static class BattleReportHistory
{
    public static void PreserveLegacy(MatchState before, MatchState after)
    {
        if (before.LastBattleRound is not { } legacy || HasRound(before, legacy))
            return;
        var context = new ActionContext { Kind = ActionContextKind.Battle, BattleId = legacy.Id,
            BattleRound = legacy.Round, Regions = [legacy.RegionId] };
        after.Events.Add(new()
        {
            Revision = after.Revision, Kind = "LegacyBattleRound", Text = "Preserved the available historical battle round.",
            Activity = new()
            {
                Automatic = true, Status = ActionLogStatus.Unknown, Context = context,
                Action = new()
                {
                    Id = EventIdentity.ForRevision(before.Id, after.Revision, "legacy-battle", 0),
                    PartialHistory = true, Context = context
                },
                Facts = new() { Battle = BattleReportProjection.PublicLegacy(legacy) }
            }
        });
    }

    private static bool HasRound(MatchState state, BattleView round)
    {
        // Current saves normally find the completed round in the most recent few facts.
        for (var i = state.Events.Count - 1; i >= 0; i--)
            if (state.Events[i].Activity?.Facts.Battle is { } fact && fact.Id == round.Id && fact.Round == round.Round)
                return true;
        return false;
    }
}
