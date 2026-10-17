using Wotr.Contracts;

namespace Wotr.Domain;

public static class FireAndShadow
{
    public static bool Applies(MatchState state, IReadOnlyList<string> route) => state.Configuration.Lords &&
        CharacterRules.InPlay(state, LordsCharacters.Balrog) is { } balrog && route.Contains(balrog.Location);

    public static void Begin(MatchState state, FellowshipProcedure revelation)
    {
        revelation.FireAndShadowPending = false;
        HuntRuntime.BeginTile(state, LordsCharacters.Balrog);
        var hunt = HuntRuntime.Active(state)!;
        hunt.FireAndShadow = true;
        hunt.IgnoreRevealIcon = true;
        hunt.DeclarationHunt = revelation.Kind == FellowshipProcedureKind.Declaration;
    }

    public static bool ResolveEye(MatchState state, HuntProcedure hunt)
    {
        if (!hunt.FireAndShadow || HuntPool.Definitions[hunt.TileId].Value != "eye")
            return false;
        HuntPool.Discard(state, hunt.TileId);
        hunt.Stage = HuntStage.Done;
        if (CharacterRules.GuideDefinition(state) == CharacterRules.Gollum)
        {
            state.Events.Add(new() { Revision = state.Revision, Kind = "HuntTileIgnored", Text = "Fire and Shadow: the Eye has no effect while Gollum is Guide." });
            return true;
        }
        var balrog = CharacterRules.InPlay(state, LordsCharacters.Balrog)!;
        CharacterLifecycle.Eliminate(state, balrog);
        HuntReports.Record(state, hunt, HuntReportKind.CharacterEliminated, character: balrog.Id, destination: balrog.Location);
        state.Events.Add(new() { Revision = state.Revision, Kind = "CharacterEliminated", Text = "Fire and Shadow: the Balrog and the Guide are eliminated." });
        HobbitDeparture.EliminateFromFellowship(state, state.Fellowship.Guide, hunt);
        // A returning Hobbit and the new Guide still resolve through their normal saved decisions.
        hunt.Stage = HuntStage.Corruption;
        return true;
    }
}
