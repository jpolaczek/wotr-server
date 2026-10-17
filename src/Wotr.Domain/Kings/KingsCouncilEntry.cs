using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsCouncilEntry
{
    public static List<string> Tokens(MatchState state) => state.Configuration.Kings
        ? CouncilTokens.All.Where(t => CouncilTokens.Available(state, t)).ToList() : [];

    public static bool ValidToken(MatchState state, Side actor, GameCommand command)
    {
        var p = command.Payload;
        return string.IsNullOrEmpty(p.CouncilTokenId) || actor == Side.Shadow &&
            command.Kind == CommandKind.EnterCharacter && state.Configuration.Kings &&
            KingsChieftains.IsChieftain(p.CharacterId) && CouncilTokens.Available(state, p.CouncilTokenId) &&
            p.RingId == "" && p.RulerTokenId == "";
    }

    public static IEnumerable<CharacterEntryPlan> Candidates(MatchState state, Side actor) =>
        actor == Side.Shadow && Tokens(state).Count > 0
            ? KingsChieftains.Candidates(state, actor, ignorePrerequisite: true) : [];
}
