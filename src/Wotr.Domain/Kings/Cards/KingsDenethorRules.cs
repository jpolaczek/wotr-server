using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsDenethorRules
{
    public static bool InPlay(MatchState state) => SovereignLifecycle.Find(state, SovereignCatalog.Denethor) is
        { Status: SovereignStatus.Initial or SovereignStatus.Corrupted } sovereign &&
        BoardRules.Regions.ContainsKey(SovereignLifecycle.Region(state, sovereign));
    public static string Region(MatchState state) => SovereignLifecycle.Find(state, SovereignCatalog.Denethor) is { } sovereign
        ? SovereignLifecycle.Region(state, sovereign) : "";
    public static bool BlocksCards(MatchState state, BattleState battle) => KingsCardRestrictions.OnTable(state, KingsCards03.Denethor) &&
        (battle.Region == DenethorRules.Region || battle.Region == Region(state));
    public static bool AnyDie(MatchState state)
    {
        var region = Region(state);
        return BoardRules.Regions.ContainsKey(region) && state.Characters.Any(c => c.Location == region &&
            (CharacterRules.MatchesCardName(c, CharacterRules.Grey) ||
             c.DefinitionId is CharacterRules.White or CharacterRules.Aragorn || CharacterRules.MatchesCardName(c, "base.character.boromir")));
    }
}

public sealed record KingsDenethorCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => KingsDenethorRules.InPlay(state);
}
