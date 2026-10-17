using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class HuntDamage
{
    public static void Resolve(MatchState state, HuntProcedure hunt, HuntResponse response, IRandomSource random)
    {
        state.Pending = null;
        HuntReports.Record(state, hunt, HuntReportKind.Response, response: response);
        if (response == HuntResponse.Ring)
        {
            hunt.Stage = HuntStage.Corruption;
            return;
        }
        var id = response == HuntResponse.Guide ? state.Fellowship.Guide
            : state.Fellowship.Companions[random.Next(state.Fellowship.Companions.Count)];
        var companion = CharacterCatalog.Companion(state, id);
        hunt.Damage = Math.Max(0, hunt.Damage - CompanionRules.Level(state, companion));
        hunt.CasualtyTaken = true;
        hunt.CasualtyEffectsPending = true;
        hunt.Stage = HuntStage.Guide;
        state.Events.Add(new() { Revision = state.Revision, Kind = "HuntCasualty", Text = $"{id} was taken as a Hunt casualty." });
        HobbitDeparture.EliminateFromFellowship(state, id, hunt);
    }

    public static void ApplyCorruption(MatchState state, HuntProcedure hunt)
    {
        state.Fellowship.Corruption = Math.Max(0, state.Fellowship.Corruption + hunt.Damage - hunt.Healing);
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = "HuntCorruption",
            Text = $"Hunt damage {hunt.Damage}, healing {hunt.Healing}; Corruption {state.Fellowship.Corruption}."
        });
        RingVictory.CheckCorruption(state);
    }
}
