using Wotr.Contracts;

namespace Wotr.Domain;

public static class DenethorRules
{
    public const string Region = "minas-tirith";
    public static bool Siege(MatchState state) => state.Control.GetValueOrDefault(Region) == Side.FreePeoples &&
        state.Units.Any(u => u.Location == Region && u.Garrison && BoardRules.Owner(u) == Side.FreePeoples && BoardRules.IsArmyUnit(u)) &&
        BoardRules.HasEnemyArmy(state, Region, Side.FreePeoples, false);
    public static bool BlocksCards(MatchState state, BattleState battle, Side actor) => actor == Side.FreePeoples &&
        (battle.Region == Region && state.ActiveEffects.Any(e => e.Modifier == EffectModifier.PreventMinasTirithCombatCards) ||
         KingsDenethorRules.BlocksCards(state, battle));
    public static bool AnyDie(MatchState state) => state.Characters.Any(c => c.Location == Region && c.Garrison &&
        (CharacterRules.MatchesCardName(c, CharacterRules.Grey) || c.DefinitionId is CharacterRules.White or CharacterRules.Aragorn));
}

public sealed record DenethorCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => DenethorRules.Siege(state);
}

public sealed record DenethorLeaderStep : EffectChoiceStep
{
    public override string Presentation => "ArmyFigure";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => state.Units
        .Where(u => u.Location == DenethorRules.Region && u.Kind == UnitKind.Leader && BoardRules.Owner(u) == Side.FreePeoples)
        .Select(u => u.Id).ToList();
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        TargetId = DenethorRules.Region,
        Detail = "Eliminate this Leader. Companions cannot be chosen."
    };
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.RemovedIds.AddRange(ids);
    public override void Choose(EffectContext context, string id) => CasualtyRules.Eliminate(context.State, [id], CasualtyCause.Event);
}
