using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsCardRestrictions
{
    public const string Welcome = "kings.sh.character.25";
    public const string Folly = "kings.sh.strategy.03";
    public static bool OnTable(MatchState state, string id) => state.Configuration.Kings && state.Player(Side.Shadow).Table.Contains(id);
    public static bool CanRemoveCorruption(MatchState state) => !OnTable(state, Welcome);
    public static bool CanAwaken(MatchState state, string sovereign) =>
        !(sovereign == SovereignCatalog.Denethor && OnTable(state, Folly)) &&
        (!OnTable(state, Welcome) || state.Nations.Single(n => n.Id == SovereignCatalog.All[sovereign].Nation).StepsToWar == 0);
}
