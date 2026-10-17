namespace Wotr.Domain;

public static class CouncilStrider
{
    public static void RecordMove(MatchState state, string dieId)
    {
        if (state.Lords?.Council is not { } council)
            return;
        var first = council.LastFellowshipDieTurn != state.Turn;
        council.LastFellowshipDieTurn = state.Turn;
        council.FellowshipBoxDieId = first && !state.Fellowship.MordorStep.HasValue &&
            state.Fellowship.Progress >= 1 && CharacterRules.GuideDefinition(state) == CouncilCompanions.Strider ? dieId : "";
    }

    public static bool ToFellowshipBox(MatchState state, string id) => state.Lords?.Council?.FellowshipBoxDieId == id;
}
