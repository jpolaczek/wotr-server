using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record FellowshipAtBattle : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) =>
        !state.Fellowship.MordorStep.HasValue && state.Fellowship.RegionId == battle.Region;
}

public sealed record CompanionInBattle(bool Hobbit = false) : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) => BattleRules.Characters(state, battle.Force(actor))
        .Any(c => CharacterCatalog.All.GetValueOrDefault(c.DefinitionId)?.Role == "Companion" &&
            (!Hobbit || CharacterRules.IsHobbit(c)));
}

public sealed record CombatModifierStep(int Combat, int Reroll, bool Opponent = false) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var battle = BattleRuntime.Active(context.State)!;
        var round = (Opponent ? battle.Opponent(context.Owner) : battle.Force(context.Owner)).Round;
        round.CombatModifier += Combat;
        round.RerollModifier += Reroll;
    }
}

public sealed record BladeOfWesternesseStep : EffectChoiceStep
{
    public override string Presentation => "Minion";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var battle = BattleRuntime.Active(state)!;
        if (!battle.Force(execution.Owner).Round.Dice.Any(d => d.RerollHit))
            return [];
        return BattleRules.Characters(state, battle.Opponent(execution.Owner))
            .Where(c => CharacterCatalog.All.GetValueOrDefault(c.DefinitionId)?.Role == "Minion").Select(c => c.Id).ToArray();
    }

    public override void Choose(EffectContext context, string id)
    {
        BattleRuntime.Active(context.State)!.Force(context.Owner).Round.SpentHits++;
        CharacterLifecycle.Eliminate(context.State, context.State.Characters.Single(c => c.Id == id));
        context.Record("MinionEliminated", $"{context.Execution.SourceId}: eliminated {id} using one Leader re-roll hit.");
    }
}
