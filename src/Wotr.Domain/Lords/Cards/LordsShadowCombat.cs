using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record LordsDurinsBaneCondition : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) =>
        new NearMoriaBattle().CanPlay(state, battle, actor) ||
        BattleRules.Characters(state, battle.Force(actor)).Any(c => c.DefinitionId == LordsCharacters.Balrog);
}

public sealed record MinionInBattle : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) => actor == Side.Shadow &&
        BattleRules.Characters(state, battle.Force(actor)).Any(c => CharacterCatalog.All[c.DefinitionId].Role == "Minion");
}

public sealed record FearOfTheirMastersStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var battle = BattleRuntime.Active(context.State)!;
        var force = battle.Force(context.Owner);
        // The penalty is independent of paying the entire Leadership cost (Chapman, 39935225).
        battle.Opponent(context.Owner).Round.DoubleSixes = true;
        var cost = BattleRules.Units(context.State, force).Where(u => BattleLeadership.IsLeader(context.State, force, u))
            .ToDictionary(u => u.Id, _ => 1);
        foreach (var character in BattleRules.Characters(context.State, force).Where(c => c.Leadership > 0))
            cost.Add(character.Id, character.Leadership);
        if (!BattleLeadership.CanForfeit(context.State, force, cost))
            return;
        BattleLeadership.Forfeit(context.State, force, cost);
        force.Round.CombatModifier += 2;
    }
}

public sealed record ServantOfTheShadowStep : EffectChoiceStep
{
    public override string Presentation => "MinionLeadership";

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var force = BattleRuntime.Active(state)!.Force(execution.Owner);
        var available = BattleLeadership.Available(state, force);
        return BattleRules.Characters(state, force).Where(c => CharacterCatalog.All[c.DefinitionId].Role == "Minion" &&
            c.Leadership > 0 && available.GetValueOrDefault(c.Id) == c.Leadership).Select(c => c.Id).ToArray();
    }

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id)
    {
        var minion = state.Characters.Single(c => c.Id == id);
        return new()
        {
            Id = id, SourceId = id, Title = CharacterCatalog.All[minion.DefinitionId].Title,
            Detail = $"Forfeit {minion.Leadership} Leadership; Free Peoples roll that many fewer dice (minimum one)."
        };
    }

    public override void Choose(EffectContext context, string id)
    {
        var battle = BattleRuntime.Active(context.State)!;
        var leadership = context.State.Characters.Single(c => c.Id == id).Leadership;
        BattleLeadership.Forfeit(context.State, battle.Force(context.Owner), new Dictionary<string, int> { [id] = leadership });
        battle.Opponent(context.Owner).Round.CombatDicePenalty += leadership;
    }
}
