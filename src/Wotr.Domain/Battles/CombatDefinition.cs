using Wotr.Contracts;

namespace Wotr.Domain;

public interface ICombatCondition
{
    bool CanPlay(MatchState state, BattleState battle, Side actor);
}

public sealed record UnconditionalCombat : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) => true;
}

public sealed record CombatEffect(int Initiative, string ProgramId);
public sealed record CombatDefinition(string CardId, ICombatCondition Condition, IReadOnlyList<CombatEffect> Effects);
