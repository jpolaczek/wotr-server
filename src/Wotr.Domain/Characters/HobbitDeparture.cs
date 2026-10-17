using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class HobbitDeparture
{
    public static void UseGuide(MatchState state)
    {
        var hunt = HuntRuntime.Active(state)!;
        var companion = CharacterCatalog.Companion(state, state.Fellowship.Guide);
        hunt.Damage = Math.Max(0, hunt.Damage - 1);
        Leave(state, companion, hunt, true);
    }

    // Also used by non-Hunt events; returning is not limited to a casualty from a tile.
    public static void EliminateFromFellowship(MatchState state, string id, HuntProcedure? hunt = null)
    {
        var companion = CharacterCatalog.Companion(state, id);
        if (companion.DefinitionId == LordsCharacters.Smeagol)
        {
            SmeagolRules.Discard(state);
            HuntReports.Record(state, hunt, Wotr.Contracts.HuntReportKind.CompanionLeft, character: id, destination: "out-of-play");
            if (hunt is not null)
                hunt.Stage = HuntStage.Guide;
            FellowshipFlow.CompositionChanged(state, FellowshipReturn.Parent);
            return;
        }
        Leave(state, companion, hunt, CharacterRules.Has(companion, CharacterRules.TakenAlive));
    }

    private static void Leave(MatchState state, CharacterState companion, HuntProcedure? hunt, bool mayReturn)
    {
        LordsCompanionSetup.Reveal(state, companion);
        state.Fellowship.Companions.Remove(companion.Id);
        if (state.Characters.All(c => c.Id != companion.Id))
            state.Characters.Add(companion);
        var returns = mayReturn && !state.Fellowship.MordorStep.HasValue;
        companion.Location = returns ? "separating" : "casualties";
        companion.Garrison = false;
        HuntReports.Record(state, hunt, Wotr.Contracts.HuntReportKind.CompanionLeft, character: companion.Id, destination: companion.Location);
        CharacterLifecycle.ReconcileDice(state);
        if (returns && hunt is not null)
            hunt.DepartingCompanions.Add(companion.Id);
        if (returns && hunt is null)
        {
            FellowshipFlow.BeginPlacement(state, companion.Id, [companion.Id], state.Fellowship.RegionId,
                state.Fellowship.Progress + CharacterRules.MovementLevel(companion), true);
            return;
        }
        if (hunt is not null)
            hunt.Stage = HuntStage.Guide;
        FellowshipFlow.CompositionChanged(state, FellowshipReturn.Parent);
    }

    public static bool PlaceGroup(MatchState state, HuntProcedure hunt)
    {
        if (hunt.DepartingCompanions.Count == 0 || hunt.DeparturePlaced)
            return false;
        hunt.DeparturePlaced = true;
        var range = hunt.DepartureProgress + hunt.DepartingCompanions.Max(id => CharacterRules.MovementLevel(CharacterCatalog.Companion(state, id)));
        FellowshipFlow.BeginPlacement(state, hunt.TileId, hunt.DepartingCompanions, hunt.DepartureOrigin, range, false);
        return true;
    }
}
