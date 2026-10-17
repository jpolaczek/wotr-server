using Wotr.Contracts;

namespace Wotr.Domain;

public static class SovereignAwakening
{
    public static List<string> Destinations(MatchState state, SovereignState sovereign)
    {
        if (sovereign.Status != SovereignStatus.Initial || !KingsCardRestrictions.CanAwaken(state, sovereign.Id))
            return [];
        var definition = SovereignCatalog.All[sovereign.Id];
        var nation = state.Nations.Single(n => n.Id == definition.Nation);
        if (!nation.Active || sovereign.Id == SovereignCatalog.Thranduil && nation.StepsToWar != 0)
            return [];
        var origin = SovereignLifecycle.Region(state, sovereign);
        if (!BoardRules.Regions.ContainsKey(origin))
            return [];
        return TravelRules.Reachable(state, origin, definition.Level, companions: true)
            .Where(id => ValidDestination(state, definition, id)).ToList();
    }

    private static bool ValidDestination(MatchState state, SovereignDefinition definition, string region) =>
        CharacterEntry.Unconquered(state, region, Side.FreePeoples) && (definition.Id == SovereignCatalog.Brand
            ? BoardRules.Region(state, region) is { OriginalOwner: Side.FreePeoples, Settlement: "City" or "Stronghold" } : region == definition.Home);

    public static bool Valid(MatchState state, CommandPayload payload) =>
        SovereignLifecycle.Find(state, payload.CharacterId) is { } sovereign && payload.Route.Count > 0 &&
        Destinations(state, sovereign).Contains(payload.Route[^1]) &&
        TravelRules.CompanionPath(state, payload.Route, SovereignLifecycle.Region(state, sovereign), SovereignCatalog.All[sovereign.Id].Level) &&
        DiceRules.AvailableDie(state, Side.FreePeoples, payload.DieId) is { } die && CanPay(state, die);

    public static bool CanPay(MatchState state, DieState die) => die.Source == SovereignCatalog.RulerSource ||
        !KingsCardRestrictions.OnTable(state, KingsCards03.Threats) && DiceRules.CanPay(die, DieFace.Muster);

    public static bool CanRemove(MatchState state, string id) => SovereignLifecycle.Find(state, id) is { Status: SovereignStatus.Corrupted } sovereign &&
        AppropriateCompanion(state, sovereign);

    public static bool AppropriateCompanion(MatchState state, SovereignState sovereign)
    {
        var region = SovereignLifecycle.Region(state, sovereign);
        var nation = SovereignCatalog.All[sovereign.Id].Nation;
        return state.Characters.Any(c => c.Location == region && CharacterRules.OnBoard(c) && CharacterRules.IsCompanion(c) &&
            c.Side == Side.FreePeoples && (c.NationIcon == nation || c.NationIcon == "free-peoples"));
    }
}
