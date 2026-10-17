using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class Visibility
{
    public static bool CanRead(Side viewer, Side? onlyFor) => onlyFor is null || onlyFor == viewer;
    public static bool CanReadCorruption(MatchState state, Side viewer, Side owner)
        => viewer == owner || state.Configuration.VisibleCorruption;
    public static string SafeError(string? code) => code is null ? "" : code switch
    {
        "NotAuthorized" or "StaleRevision" or "DecisionExpired" or "IllegalChoice" or "InsufficientResources" or "UnsupportedAction" or "GameFinished" or
            "MoreArmyMovesRequired" or CommandShape.MalformedCommand => code,
        _ => "IllegalChoice"
    };
}
