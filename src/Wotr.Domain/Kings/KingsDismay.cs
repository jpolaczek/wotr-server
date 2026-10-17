using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsDismay
{
    public const string Location = "dismay";

    public static IEnumerable<RulerTokenState> Tokens(MatchState state) =>
        state.Kings?.Tokens.Where(t => t.Location == Location) ?? [];

    public static IEnumerable<DieState> Dice(MatchState state) => state.Dice.Where(d =>
        d.Source == SovereignCatalog.RulerSource && d.Location is DieLocation.Available or DieLocation.Hunt);

    public static void UsedRulerToken(MatchState state, RulerTokenState token)
    {
        token.Location = ConfigurationRules.DismayEnabled(state.Configuration) ? Location : "removed";
        if (token.Location == Location)
            RecordGrant(state, token);
    }

    public static void SovereignRemoved(MatchState state, SovereignState sovereign)
    {
        if (sovereign.Status != SovereignStatus.Awakened || !ConfigurationRules.DismayEnabled(state.Configuration))
            return;
        var token = state.Kings!.Tokens.Find(t => t.SovereignId == sovereign.Id);
        if (state.Configuration.RemovingRulerTokens && token is null)
        {
            token = new() { SovereignId = sovereign.Id, Location = Location };
            state.Kings.Tokens.Add(token);
            RecordGrant(state, token);
        }
        else if (token?.Location == "free-peoples")
            UsedRulerToken(state, token);
    }

    public static bool Begin(MatchState state)
    {
        if (!state.Configuration.Kings || !ConfigurationRules.DismayEnabled(state.Configuration) ||
            state.Phase != GamePhase.ActionRoll || state.Kings is null ||
            state.Kings.DismayDeclinedTurn == state.Turn || !Tokens(state).Any() || !Dice(state).Any())
            return false;
        var commands = new List<CommandKind> { CommandKind.UseDismay, CommandKind.DeclineDismay };
        if (LoreOfTheRings.CanBegin(state))
            commands.Add(CommandKind.UseLoreBeforeActions);
        state.Open(DecisionKind.Dismay, Side.Shadow, commands);
        return true;
    }

    public static void Use(MatchState state, string tokenId, string dieId, IRandomSource random)
    {
        Tokens(state).Single(t => KingsRulerTokens.Id(t.SovereignId) == tokenId).Location = "removed";
        var die = Dice(state).Single(d => d.Id == dieId);
        DiceRules.RollActionDie(die, random);
        state.Events.Add(new()
        {
            Revision = state.Revision, Kind = "DismayUsed",
            Text = $"Shadow used {tokenId} to re-roll {die.Owner}'s Ruler die: {die.Face}."
        });
    }

    private static void RecordGrant(MatchState state, RulerTokenState token) => state.Events.Add(new()
    {
        Revision = state.Revision, Kind = "DismayGranted",
        Text = $"Shadow received {SovereignCatalog.All[token.SovereignId].Title}'s Dismay token."
    });
}
