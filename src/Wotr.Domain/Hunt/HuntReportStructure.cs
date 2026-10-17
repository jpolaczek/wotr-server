using Wotr.Contracts;

namespace Wotr.Domain;

internal static class HuntReportStructure
{
    public static void Validate(MatchState state)
    {
        var report = state.HuntReport;
        var frames = state.Resolution.Frames.Where(f => f.Hunt is not null).Select(f => f.Hunt!).ToArray();
        if (state.HuntReportSequence < 0 || frames.Any(h => h.ReportIndex < 0))
            throw new InvalidDataException("CorruptSave");
        if (report is null)
        {
            if (frames.Any(h => h.ReportIndex > 0))
                throw new InvalidDataException("CorruptSave");
            return;
        }
        if (string.IsNullOrWhiteSpace(report.Id) || report.Initial is null || report.Outcome is null ||
            report.Entries is null || report.Entries.Count == 0 || report.Entries.Any(e => e is null ||
                e.HuntId <= 0 || !Enum.IsDefined(e.Kind) || e.Outcome is null || e.Hunt is { } hunt &&
                (hunt.Dice is null || hunt.RerollSources is null || hunt.Damage < 0 || hunt.Healing < 0 ||
                    hunt.Dice.Any(d => d is null || d.First is < 1 or > 6 || d.Result is < 1 or > 6))))
            throw new InvalidDataException("CorruptSave");
        var ids = report.Entries.Where(e => e.Kind == HuntReportKind.Started).Select(e => e.HuntId).ToHashSet();
        if (report.Entries.Any(e => !ids.Contains(e.HuntId)) || frames.Any(h => h.ReportIndex > 0 && !ids.Contains(h.ReportIndex)))
            throw new InvalidDataException("CorruptSave");
    }
}
