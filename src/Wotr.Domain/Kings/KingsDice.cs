using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsDice
{
    public const string FreePeoples = "kings.die.fp.ruler";
    public const string Shadow = "kings.die.sh.ruler";

    public static DieState? Ruler(MatchState state, Side side) => state.Configuration.Kings
        ? state.Dice.Find(d => d.Owner == side && d.Source == SovereignCatalog.RulerSource) : null;

    public static void Setup(MatchState state)
    {
        foreach (var side in Enum.GetValues<Side>())
            state.Dice.Add(new() { Id = side == Side.FreePeoples ? FreePeoples : Shadow, Owner = side,
                Source = SovereignCatalog.RulerSource, Location = DieLocation.Removed });
    }

    public static void Recover(MatchState state)
    {
        if (!state.Configuration.Kings || state.Kings is null)
            return;
        state.Kings.RulerDiscardOrder.Clear();
        foreach (var side in Enum.GetValues<Side>())
        {
            var die = Ruler(state, side)!;
            var present = side == Side.FreePeoples
                ? state.Kings.Sovereigns.Any(s => s.Status == SovereignStatus.Awakened &&
                    KingsRulerAbilities.Awakened(state, s.Id) is not null)
                : state.Characters.Any(c => KingsChieftains.IsChieftain(c.DefinitionId) && CharacterRules.OnBoard(c));
            die.Location = present ? DieLocation.Unrolled : DieLocation.Removed;
            die.HuntOrigin = "";
        }
    }

    public static bool BeginDiscards(MatchState state)
    {
        if (!state.Configuration.Kings || state.Kings is null)
            return false;
        state.Kings.RulerDiscardOrder = new[] { Side.Shadow, Side.FreePeoples }
            .Where(s => Ruler(state, s)?.Location is DieLocation.Available or DieLocation.AwaitingChoice).ToList();
        return OpenNext(state);
    }

    public static bool OpenNext(MatchState state)
    {
        if (state.Kings!.RulerDiscardOrder.Count == 0)
            return false;
        state.Open(DecisionKind.RulerDiscard, state.Kings.RulerDiscardOrder[0], [CommandKind.DiscardRulerDie]);
        return true;
    }

    public static List<string> DiscardCandidates(MatchState state, Side side) => state.Dice.Where(d => d.Owner == side &&
        (d.Source == "standard" || d.Source == SovereignCatalog.RulerSource) &&
        d.Location is DieLocation.Available or DieLocation.AwaitingChoice &&
        (side == Side.FreePeoples || d.Face != DieFace.Eye || d.Source == SovereignCatalog.RulerSource))
        .Select(d => d.Id).ToList();

    public static void Discard(MatchState state, Side side, string id)
    {
        state.Dice.Single(d => d.Id == id).Location = DieLocation.Discarded;
        if (side == Side.Shadow)
            foreach (var die in state.Dice.Where(d => d.Owner == side && d.Location == DieLocation.AwaitingChoice &&
                d.Special is null && d.Face == DieFace.Eye))
            {
                die.Location = DieLocation.Hunt;
                die.HuntOrigin = "rolled-eye";
            }
        state.Kings!.RulerDiscardOrder.RemoveAt(0);
        state.Events.Add(new() { Revision = state.Revision, Kind = "RulerDieDiscarded", Text = $"{side} discarded {id} after the Action Roll." });
    }
}
