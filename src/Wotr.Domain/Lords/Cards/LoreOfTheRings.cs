using Wotr.Contracts;

namespace Wotr.Domain;

public static class LoreOfTheRings
{
    public const string Program = "lords.lore-change-die";
    public const string Decline = "lore.keep-results";
    private static readonly IReadOnlyDictionary<(string Source, DieFace Face, bool Remove), IReadOnlyDictionary<string, SpecialDieFace>> ResultChoices = BuildResults();

    public static bool CanBegin(MatchState state) => state.Configuration.Lords && state.Lords is not null &&
        state.Phase == GamePhase.ActionRoll && state.Lords.LoreOfferedTurn != state.Turn &&
        state.Player(Side.Shadow).Table.Contains(LordsShadowCards.Lore) &&
        state.Dice.Any(d => d.Owner == Side.FreePeoples && d.Special is not null && d.Face == DieFace.Eye &&
            d.Location is DieLocation.Hunt or DieLocation.Discarded);

    public static bool Begin(MatchState state)
    {
        if (!CanBegin(state))
            return false;
        state.Lords!.LoreOfferedTurn = state.Turn;
        EffectRuntime.BeginProgram(state, Side.Shadow, LordsShadowCards.Lore, Program);
        EffectRuntime.Current(state)!.Effect!.Return = EffectReturn.BeginActions;
        return true;
    }

    public static IEnumerable<DieState> Dice(MatchState state) => state.Dice.Where(d => d.Owner == Side.Shadow &&
        (d.Location == DieLocation.Available || d.Location == DieLocation.Hunt && d.HuntOrigin is "rolled-eye" or LordsDiceCatalog.LesserEye));

    public static IReadOnlyDictionary<string, SpecialDieFace> Faces(DieState die) =>
        ResultChoices[(die.Special is null ? "standard" : die.Source, die.Face, die.Special?.RemoveSymbol ?? false)];

    private static IReadOnlyDictionary<(string, DieFace, bool), IReadOnlyDictionary<string, SpecialDieFace>> BuildResults()
    {
        var sources = LordsDiceCatalog.Definitions.Values.Where(d => d.Owner == Side.Shadow)
            .ToDictionary(d => d.Id, d => d.Faces.Distinct().ToArray());
        sources.Add("standard", RingRules.Faces(Side.Shadow).Select(f => new SpecialDieFace(f)).ToArray());
        var results = new Dictionary<(string, DieFace, bool), IReadOnlyDictionary<string, SpecialDieFace>>();
        foreach (var source in sources)
            foreach (var face in Enum.GetValues<DieFace>())
                foreach (var remove in new[] { false, true })
                    results.Add((source.Key, face, remove), source.Value.Where(f => f.Face != face || f.Remove != remove)
                        .ToDictionary(f => $"lore.{f.Face}.{(f.Remove ? "remove" : "keep")}"));
        return results;
    }
}
