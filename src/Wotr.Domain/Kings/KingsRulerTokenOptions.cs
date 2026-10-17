using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsRulerTokenOptions
{
    public static List<RulerTokenView> Tokens(MatchState state) => state.Kings?.Tokens.Select(t => new RulerTokenView
    {
        Id = KingsRulerTokens.Id(t.SovereignId), SovereignId = t.SovereignId, Location = t.Location,
        Face = KingsRulerTokens.Face(t.SovereignId)
    }).ToList() ?? [];

    public static List<RulerTokenActionOption> Actions(MatchState state, Side viewer)
    {
        if (viewer != Side.FreePeoples || state.Phase != GamePhase.ActionResolution ||
            state.Pending is not { Kind: DecisionKind.Action, Owner: Side.FreePeoples })
            return [];
        return Inspect(state);
    }

    // Inspect prerequisites even while another decision is active; never mutate that decision.
    internal static List<RulerTokenActionOption> Inspect(MatchState state)
    {
        if (state.Kings is null)
            return [];
        var copies = new Dictionary<DieFace, MatchState>();
        var result = new List<RulerTokenActionOption>();
        foreach (var token in state.Kings.Tokens.Where(t => KingsRulerTokens.ConditionsMet(state, t)))
        {
            var face = KingsRulerTokens.Face(token.SovereignId);
            if (!copies.TryGetValue(face, out var copy))
            {
                copy = KingsRulerTokens.ValidationState(state, new() { RulerTokenId = KingsRulerTokens.Id(token.SovereignId) });
                copy.Phase = GamePhase.ActionResolution;
                copy.Pending = new() { Kind = DecisionKind.Action, Owner = Side.FreePeoples };
                copies.Add(face, copy);
            }
            foreach (var option in Choices(copy, token.SovereignId).Where(o => o.Enabled))
                result.Add(new() { TokenId = KingsRulerTokens.Id(token.SovereignId),
                    AbilityId = token.SovereignId == SovereignCatalog.Denethor ? "" : KingsRulerTokens.Ability(token.SovereignId), Action = option });
        }
        return result;
    }

    private static IEnumerable<ActionOption> Choices(MatchState state, string sovereign)
    {
        var ruler = KingsDice.Ruler(state, Side.FreePeoples)!.Id;
        if (sovereign == SovereignCatalog.Denethor)
        {
            foreach (var option in Anarion(state, ruler))
                yield return option;
        }
        else if (sovereign == SovereignCatalog.Thranduil)
        {
            var travel = new FellowshipOptions().GetOptions(state, Side.FreePeoples)
                .Concat(new HuntOptions().GetOptions(state, Side.FreePeoples));
            foreach (var option in travel.Where(o => o.RulerTravel is not null))
            {
                option.EligibleDice = [ruler];
                if (option.Kind != CommandKind.MoveFellowship)
                    option.Travel = option.RulerTravel!.Travel;
                yield return option;
            }
        }
        else if (KingsRulerRecruitment.Options(state, Side.FreePeoples).FirstOrDefault(o => o.CharacterId == sovereign) is { } recruit)
            yield return new() { Kind = CommandKind.UseCharacterAbility, CharacterActions = [recruit], EligibleDice = [ruler], Targets = recruit.Targets };
    }

    private static IEnumerable<ActionOption> Anarion(MatchState state, string ruler)
    {
        var draw = new TurnOptions().GetOptions(state, Side.FreePeoples).Single(o => o.Kind == CommandKind.DrawCard);
        draw.EligibleDice = [ruler];
        yield return draw;
        var events = new EffectOptions().GetOptions(state, Side.FreePeoples).FirstOrDefault(o => o.Kind == CommandKind.PlayEvent);
        if (events is null)
            yield break;
        foreach (var play in events.Events)
            play.Costs = play.Costs.Where(c => c.DieId == ruler && c.Face == DieFace.Event).ToList();
        events.Events.RemoveAll(e => e.Costs.Count == 0);
        events.EligibleIds = events.Events.Select(e => e.CardId).ToList();
        events.EligibleDice = [ruler];
        if (events.Events.Count > 0)
            yield return events;
    }
}
