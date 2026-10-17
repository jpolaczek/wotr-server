using Wotr.Contracts;

namespace Wotr.Domain;

public static class CorruptionRuntime
{
    public static CorruptionAttempt? Current(MatchState state) => state.Resolution.Frames.LastOrDefault()?.Corruption;

    public static void Begin(MatchState state, string sovereignId, string dieId, IRandomSource random)
    {
        var sovereign = SovereignLifecycle.Find(state, sovereignId)!;
        var die = CorruptionRules.Eyes(state).Single(d => d.Id == dieId);
        state.HistoryCapture?.Pay(die, DieFace.Eye);
        die.Location = DieLocation.Resolving;
        sovereign.LastAttemptTurn = state.Turn;
        var attempt = new CorruptionAttempt
        {
            SovereignId = sovereignId, DieId = dieId, DrawLimit = Math.Min(3, CorruptionRules.Eyes(state).Count()),
            Return = EffectReturn.FinishAction
        };
        state.Resolution.Frames.Add(new() { Corruption = attempt });
        state.Events.Add(new() { Revision = state.Revision, Kind = "CorruptionAttempted",
            Text = $"Shadow spent an Eye to attempt to corrupt {SovereignCatalog.All[sovereignId].Title}." });
        Draw(state, random);
    }

    public static void BeginEvent(MatchState state, string sovereignId, IRandomSource random)
    {
        var limit = Math.Min(3, CorruptionRules.Eyes(state).Count());
        if (limit == 0)
            return;
        SovereignLifecycle.Find(state, sovereignId)!.LastAttemptTurn = state.Turn;
        state.Resolution.Frames.Add(new()
        {
            Corruption = new() { SovereignId = sovereignId, DrawLimit = limit, Return = EffectReturn.Parent }
        });
        state.Events.Add(new()
        {
            Revision = state.Revision, Kind = "CorruptionAttempted",
            Text = $"The West Has Failed: Shadow attempts to corrupt {SovereignCatalog.All[sovereignId].Title}."
        });
        Draw(state, random);
    }

    public static void Draw(MatchState state, IRandomSource random)
    {
        var attempt = Current(state)!;
        var tile = HuntPool.DrawPhysical(state, random);
        tile.Location = "corruption-draw";
        if (KingsTiles.IsKings(tile.Id))
        {
            KingsTiles.Reveal(state, [tile.Id]);
            CorruptionRules.Attach(state, SovereignLifecycle.Find(state, attempt.SovereignId)!, tile.Id);
            Complete(state, attempt);
            return;
        }
        if (!KingsTiles.IsNumeric(tile.Id))
        {
            attempt.Ignored.Add(tile.Id);
            KingsTiles.Reveal(state, [tile.Id]);
            attempt.Stage = CorruptionStage.Drawing;
        }
        else
        {
            attempt.Drawn.Add(tile.Id);
            var activity = HistoryFacts.At(state, ActionContextKind.Characters, attempt.SovereignId);
            activity.Facts.Targets.Add(tile.Id);
            state.Events.Add(new()
            {
                Revision = state.Revision, Kind = "OwnCorruptionTile", OnlyFor = Side.Shadow,
                Text = "Drew a Corruption tile.", Activity = activity
            });
            if (state.Configuration.SequentialCorruption && attempt.Drawn.Count == attempt.DrawLimit)
            {
                Choose(state, tile.Id);
                return;
            }
            attempt.Stage = state.Configuration.SequentialCorruption ? CorruptionStage.SequentialChoice
                : attempt.Drawn.Count == attempt.DrawLimit ? CorruptionStage.Choosing : CorruptionStage.Drawing;
        }
        OpenChoice(state, attempt);
    }

    private static void OpenChoice(MatchState state, CorruptionAttempt attempt)
    {
        var commands = new List<CommandKind>();
        if (CorruptionRules.CanDraw(attempt))
            commands.Add(CommandKind.DrawCorruptionTile);
        var choices = CorruptionRules.Choices(attempt);
        if (choices.Count > 0)
            commands.Add(CommandKind.ChooseCorruptionTile);
        state.Open(DecisionKind.CorruptionTile, Side.Shadow, commands);
        state.Pending!.SourceId = attempt.SovereignId;
        state.Pending.Presentation = attempt.Stage == CorruptionStage.SequentialChoice ? "CorruptionSequential"
            : attempt.Stage == CorruptionStage.Drawing ? "CorruptionDraw" : "CorruptionChoice";
        state.Pending.PrivateChoices = [.. choices];
    }

    public static void Choose(MatchState state, string tileId)
    {
        var attempt = Current(state)!;
        attempt.Drawn.Remove(tileId);
        KingsTiles.Reveal(state, attempt.Drawn);
        CorruptionRules.Attach(state, SovereignLifecycle.Find(state, attempt.SovereignId)!, tileId);
        Complete(state, attempt);
    }

    private static void Complete(MatchState state, CorruptionAttempt attempt)
    {
        KingsTiles.Return(state, attempt.Drawn.Concat(attempt.Ignored));
        if (attempt.DieId.Length > 0)
        {
            var die = state.Dice.Single(d => d.Id == attempt.DieId);
            die.Location = DieLocation.Spent;
            die.SpentWith = die.Special is not null ? Side.FreePeoples : null;
            die.HuntOrigin = "";
        }
        state.Resolution.Frames.RemoveAt(state.Resolution.Frames.Count - 1);
        state.Pending = null;
        if (attempt.Return == EffectReturn.FinishAction)
            TurnFlow.FinishAction(state, Side.Shadow);
    }
}
