using Wotr.Contracts;

namespace Wotr.Domain;

public enum CasualtyCause { Event, Battle, DestructiveEvent }

public static class CasualtyRules
{
    public static void Eliminate(MatchState state, IReadOnlyCollection<string> unitIds, CasualtyCause cause,
        IReadOnlySet<string>? battleParticipants = null)
    {
        if (!Enum.IsDefined(cause))
            throw new ArgumentOutOfRangeException(nameof(cause));
        if (cause != CasualtyCause.Event && (battleParticipants is null || unitIds.Any(id => !battleParticipants.Contains(id))))
            throw new InvalidOperationException("Battle casualties require an explicit participating force.");
        var units = state.Units.Where(u => unitIds.Contains(u.Id)).ToArray();
        if (units.Length != unitIds.Count || units.Any(u => !BoardRules.Regions.ContainsKey(u.Location)))
            throw new InvalidOperationException("Casualties must be distinct figures on the board.");
        var armies = units.Select(u => (u.Location, u.Garrison, Owner: BoardRules.Owner(u))).Distinct().ToArray();
        foreach (var unit in units)
            BoardRules.RemoveCasualty(state, unit);
        foreach (var army in armies)
            RemoveUnaccompanied(state, army.Location, army.Garrison, army.Owner, cause, battleParticipants);
        StrongholdRules.ReconcileOutsideBattle(state, armies.Select(a => a.Location));
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = "UnitsEliminated",
            Text = $"{cause}: {units.Length} figure(s) eliminated."
        });
        ActiveEffects.CheckConditions(state);
    }

    public static void RemoveUnaccompanied(MatchState state, string region, bool garrison, Side owner, CasualtyCause cause,
        IReadOnlySet<string>? battleParticipants = null)
    {
        bool Participates(string id) => cause == CasualtyCause.Event || battleParticipants?.Contains(id) == true;
        if (state.Units.Any(u => u.Location == region && u.Garrison == garrison &&
            BoardRules.Owner(u) == owner && BoardRules.IsArmyUnit(u) && Participates(u.Id)))
            return;
        foreach (var unit in state.Units.Where(u => u.Location == region && u.Garrison == garrison &&
            BoardRules.Owner(u) == owner && (u.Kind == UnitKind.Leader || cause != CasualtyCause.Event) && Participates(u.Id)))
            BoardRules.RemoveCasualty(state, unit);
        if (cause == CasualtyCause.Event)
            return;
        foreach (var character in state.Characters.Where(c => c.Location == region && c.Garrison == garrison &&
            c.Side == owner && Participates(c.Id)))
        {
            CharacterLifecycle.Eliminate(state, character);
        }
    }
}
