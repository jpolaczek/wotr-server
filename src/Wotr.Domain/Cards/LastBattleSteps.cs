using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record LastBattleCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) =>
        CharacterRules.InPlay(state, CharacterRules.Aragorn) is { } king &&
        (BoardRules.Region(state, king.Location).Nation is not { } nation || BoardRules.Nations[nation].Side != Side.FreePeoples) &&
        state.Units.Any(u => u.Location == king.Location && u.Garrison == king.Garrison &&
            BoardRules.Owner(u) == Side.FreePeoples && BoardRules.IsArmyUnit(u));
}
