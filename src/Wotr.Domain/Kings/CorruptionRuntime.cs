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

    // UI-442 (owner, 2026-11-02): a standard attempt draws all its tiles in the command that starts it, as nothing can
    // come between the draws (KINGS p. 12, step 2); Sequential Corruption draws one tile per decision (KINGS p. 18).
    public static void Draw(MatchState state, IRandomSource random)
    {
        var attempt = Current(state)!;
        if (state.Configuration.SequentialCorruption)
            DrawNext(state, attempt, random);
        else
            DrawAll(state, attempt, random);
    }

    // Draws until the limit of numeric tiles, a Kings tile ending the attempt or a pool with no tile left.
    private static void DrawAll(MatchState state, CorruptionAttempt attempt, IRandomSource random)
    {
        while (attempt.Drawn.Count < attempt.DrawLimit && HuntPool.CanDrawPhysical(state))
        {
            if (DrawTile(state, attempt, random) == DrawnTile.Kings)
                return;
        }
        PlaceOrChoose(state, attempt);
    }

    // EXP-15 D8: one drawn tile is the only possible choice, so the server places it; two or more wait for the Shadow.
    private static void PlaceOrChoose(MatchState state, CorruptionAttempt attempt)
    {
        if (attempt.Drawn.Count == 0)
        {
            Complete(state, attempt);
            return;
        }
        if (attempt.Drawn.Count == 1)
        {
            Choose(state, attempt.Drawn[0]);
            return;
        }
        attempt.Stage = CorruptionStage.Choosing;
        OpenChoice(state, attempt);
    }

    private static void DrawNext(MatchState state, CorruptionAttempt attempt, IRandomSource random)
    {
        var drawn = DrawTile(state, attempt, random);
        if (drawn == DrawnTile.Kings)
            return;
        if (drawn == DrawnTile.Numeric && attempt.Drawn.Count == attempt.DrawLimit)
        {
            Choose(state, attempt.Drawn[^1]);
            return;
        }
        attempt.Stage = drawn == DrawnTile.Numeric ? CorruptionStage.SequentialChoice : CorruptionStage.Drawing;
        OpenChoice(state, attempt);
    }

    private enum DrawnTile { Kings, Ignored, Numeric }

    private static DrawnTile DrawTile(MatchState state, CorruptionAttempt attempt, IRandomSource random)
    {
        var tile = HuntPool.DrawPhysical(state, random);
        tile.Location = "corruption-draw";
        if (KingsTiles.IsKings(tile.Id))
        {
            AttachKingsTile(state, attempt, tile.Id);
            return DrawnTile.Kings;
        }
        if (!KingsTiles.IsNumeric(tile.Id))
        {
            Ignore(state, attempt, tile.Id);
            return DrawnTile.Ignored;
        }
        AddDrawn(state, attempt, tile.Id);
        return DrawnTile.Numeric;
    }

    private static void AttachKingsTile(MatchState state, CorruptionAttempt attempt, string tileId)
    {
        KingsTiles.Reveal(state, [tileId]);
        CorruptionRules.Attach(state, SovereignLifecycle.Find(state, attempt.SovereignId)!, tileId);
        Complete(state, attempt);
    }

    private static void Ignore(MatchState state, CorruptionAttempt attempt, string tileId)
    {
        attempt.Ignored.Add(tileId);
        KingsTiles.Reveal(state, [tileId]);
    }

    private static void AddDrawn(MatchState state, CorruptionAttempt attempt, string tileId)
    {
        attempt.Drawn.Add(tileId);
        var activity = HistoryFacts.At(state, ActionContextKind.Characters, attempt.SovereignId);
        activity.Facts.Targets.Add(tileId);
        state.Events.Add(new()
        {
            Revision = state.Revision, Kind = "OwnCorruptionTile", OnlyFor = Side.Shadow,
            Text = "Drew a Corruption tile.", Activity = activity
        });
    }

    public static void OpenChoice(MatchState state, CorruptionAttempt attempt)
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
