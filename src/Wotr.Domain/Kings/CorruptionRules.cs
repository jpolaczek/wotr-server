using Wotr.Contracts;

namespace Wotr.Domain;

public static class CorruptionRules
{
    public static IEnumerable<DieState> Eyes(MatchState state) => state.Dice.Where(d =>
        DiceRules.IsShadowHuntDie(d) && d.Face == DieFace.Eye && d.HuntOrigin != "captain-of-despair");

    public static bool CanDraw(CorruptionAttempt attempt) =>
        attempt.Stage is CorruptionStage.Drawing or CorruptionStage.SequentialChoice;

    public static IReadOnlyList<string> Choices(CorruptionAttempt attempt) => attempt.Stage switch
    {
        CorruptionStage.Choosing => attempt.Drawn,
        CorruptionStage.SequentialChoice => [attempt.Drawn[^1]],
        _ => []
    };

    public static IEnumerable<SovereignState> Targets(MatchState state) =>
        state.Kings?.Sovereigns.Where(s => s.Status == SovereignStatus.Initial && s.LastAttemptTurn != state.Turn &&
            BoardRules.Regions.ContainsKey(SovereignLifecycle.Region(state, s))) ?? [];

    public static bool CanAttempt(MatchState state) => state.Configuration.Kings && state.Kings is not null &&
        !state.Player(Side.Shadow).RingActionPending && TurnFlow.Available(state, Side.FreePeoples) > 0 &&
        Eyes(state).Take(2).Count() == 2 && Targets(state).Any();

    public static void Attach(MatchState state, SovereignState sovereign, string tileId)
    {
        if (sovereign.Status != SovereignStatus.Initial || !KingsTiles.IsNumeric(tileId))
            throw new InvalidOperationException("Only an initial sovereign can receive a numeric Corruption tile.");
        sovereign.Tiles.Add(tileId);
        state.HuntTiles.Single(t => t.Id == tileId).Location = sovereign.Id;
        var description = KingsTiles.IsKings(tileId) ? "Kings tile" : state.Configuration.VisibleCorruption
            ? $"Corruption tile ({KingsTiles.CorruptionValue(tileId)})" : "hidden Corruption tile";
        state.Events.Add(new() { Revision = state.Revision, Kind = "CorruptionAdded",
            Activity = HistoryFacts.Sovereign(state, sovereign.Id),
            Text = $"A {description} was placed on {SovereignCatalog.All[sovereign.Id].Title}." });
        if (sovereign.Tiles.Sum(KingsTiles.CorruptionValue) >= SovereignCatalog.All[sovereign.Id].Resistance)
            SovereignLifecycle.Corrupt(state, sovereign);
    }
}
