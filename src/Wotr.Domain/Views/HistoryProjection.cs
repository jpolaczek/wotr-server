using Wotr.Contracts;

namespace Wotr.Domain;

public static class HistoryProjection
{
    public static List<HistoryEntry> For(MatchState state, Side viewer, long afterRevision = -1, int limit = 50) =>
        state.Events.Where(e => e.Revision > afterRevision && Visibility.CanRead(viewer, e.OnlyFor))
            .TakeLast(limit)
            .Select(e => new HistoryEntry { Revision = e.Revision, Kind = e.Kind, Text = e.Text })
            .ToList();
}
