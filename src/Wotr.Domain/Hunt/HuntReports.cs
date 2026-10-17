using System.Text.Json;
using Wotr.Contracts;

namespace Wotr.Domain;

// A public record written while rules run, independent of the textual event log.
public static class HuntReports
{
    public static HuntReportView? View(MatchState state) => state.HuntReport is null ? null :
        JsonSerializer.Deserialize<HuntReportView>(JsonSerializer.Serialize(state.HuntReport));

    public static void Begin(MatchState state, HuntProcedure hunt)
    {
        if (state.HuntReport is null || state.HuntReport.Completed)
            state.HuntReport = new()
            {
                Id = state.Id + "/hunt/" + ++state.HuntReportSequence,
                StartedRevision = state.Revision,
                Initial = Outcome(state),
                Outcome = Outcome(state)
            };
        hunt.ReportIndex = state.HuntReport.Entries.Count(e => e.Kind == HuntReportKind.Started) + 1;
        Record(state, hunt, HuntReportKind.Started);
    }

    public static void Record(MatchState state, HuntProcedure? hunt, HuntReportKind kind, string source = "",
        string character = "", string destination = "", HuntResponse? response = null)
    {
        if (state.HuntReport is not { Completed: false } report || hunt is null || hunt.ReportIndex == 0)
            return;
        Add(state, report, new()
        {
            Revision = state.Revision,
            HuntId = hunt.ReportIndex,
            Kind = kind,
            SourceId = source.Length > 0 ? source : hunt.SourceId,
            CharacterId = character,
            Destination = destination,
            Response = response,
            Hunt = HuntOptions.View(state, hunt),
            Outcome = Outcome(state)
        });
    }

    public static void Capture(MatchState state, string source = "")
    {
        if (state.HuntReport is not { Completed: false } report)
            return;
        foreach (var hunt in state.Resolution.Frames.Where(f => f.Hunt?.ReportIndex > 0).Select(f => f.Hunt!))
            CaptureTile(state, hunt, source);
        CaptureOutcome(state, report);
        CapturePlacements(state, report);
    }

    private static void CaptureTile(MatchState state, HuntProcedure hunt, string source)
    {
        if (hunt.TileId.Length == 0)
            return;
        var previous = state.HuntReport!.Entries.Last(e => e.HuntId == hunt.ReportIndex && e.Hunt is not null).Hunt!;
        if (previous.TileId != hunt.TileId)
        {
            Record(state, hunt, previous.TileId.Length == 0 ? HuntReportKind.TileDrawn : HuntReportKind.TileReplaced, source);
            return;
        }
        if (HuntOptions.DamagePending(hunt))
            return;
        if (previous.DamagePending)
            Record(state, hunt, HuntReportKind.DamageDetermined, source);
        else if (previous.Damage != hunt.Damage || previous.Healing != hunt.Healing ||
            previous.Reveal != hunt.Reveal || previous.Stop != hunt.Stop)
            Record(state, hunt, HuntReportKind.DamageChanged, source);
    }

    private static void CaptureOutcome(MatchState state, HuntReportView report)
    {
        var previous = report.Outcome;
        var next = Outcome(state);
        if (previous.GuideId != next.GuideId && next.GuideId.Length > 0)
            OutcomeEntry(state, report, HuntReportKind.GuideChanged, next);
        if (previous.Corruption != next.Corruption)
            OutcomeEntry(state, report, HuntReportKind.Corruption, next);
        if (previous.Hidden != next.Hidden || previous.RegionId != next.RegionId)
            OutcomeEntry(state, report, HuntReportKind.Revealed, next);
        if (previous.Progress != next.Progress || previous.MordorStep != next.MordorStep)
            OutcomeEntry(state, report, HuntReportKind.Progressed, next);
        report.Outcome = next;
    }

    private static void OutcomeEntry(MatchState state, HuntReportView report, HuntReportKind kind, HuntOutcomeView outcome)
    {
        var hunt = HuntRuntime.Active(state);
        Add(state, report, new()
        {
            Revision = state.Revision,
            HuntId = hunt?.ReportIndex ?? report.Entries.Last().HuntId,
            Kind = kind,
            Outcome = outcome,
            CharacterId = kind == HuntReportKind.GuideChanged ? outcome.GuideId : ""
        });
    }

    private static void CapturePlacements(MatchState state, HuntReportView report)
    {
        // Only companions already publicly recorded as leaving the Fellowship.
        // Never enumerate the hidden versions still inside it.
        var departing = report.Entries.Where(e => e.Kind is HuntReportKind.CompanionLeft or HuntReportKind.CompanionPlaced)
            .GroupBy(e => e.CharacterId).Select(g => g.Last()).Where(e => e.Destination == "separating").ToArray();
        foreach (var entry in departing)
        {
            var character = state.Characters.FirstOrDefault(c => c.Id == entry.CharacterId);
            if (character is null || character.Location == "separating")
                continue;
            Record(state, HuntRuntime.Active(state), HuntReportKind.CompanionPlaced,
                character: character.Id, destination: character.Location);
        }
    }

    public static void CompleteIfIdle(MatchState state)
    {
        if (state.Resolution.Frames.Count > 0 && state.Winner is null || state.HuntReport is not { Completed: false } report)
            return;
        Capture(state);
        report.Completed = true;
        report.Winner = state.Winner;
        report.Victory = state.Victory;
        var last = report.Entries[^1];
        var activity = HistoryFacts.At(state, ActionContextKind.Hunt);
        activity.Context.HuntChainId = report.Id;
        activity.Facts.Hunt = new() { ChainId = report.Id, Entry = HistoryFacts.Copy(last), Completed = true };
        state.Events.Add(new()
        {
            Revision = state.Revision, Kind = "HuntChainCompleted", Text = "The Hunt chain is complete.", Activity = activity
        });
    }

    private static void Add(MatchState state, HuntReportView report, HuntReportEntryView entry)
    {
        report.Entries.Add(entry);
        var activity = HistoryFacts.At(state, ActionContextKind.Hunt, entry.SourceId);
        activity.Context.HuntChainId = report.Id;
        activity.Facts.Hunt = new() { ChainId = report.Id, Entry = HistoryFacts.Copy(entry) };
        state.Events.Add(new()
        {
            Revision = state.Revision, Kind = "HuntFact", Text = $"Hunt: {entry.Kind}.", Activity = activity
        });
    }

    private static HuntOutcomeView Outcome(MatchState state) => new()
    {
        RegionId = state.Fellowship.RegionId,
        Progress = state.Fellowship.Progress,
        MordorStep = state.Fellowship.MordorStep,
        Corruption = state.Fellowship.Corruption,
        Hidden = state.Fellowship.Hidden,
        GuideId = state.Fellowship.Guide
    };
}
