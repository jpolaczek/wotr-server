using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class KingsSovereignCombat
{
    public static IEnumerable<CharacterState> Candidates(MatchState state, BattleState battle, Side owner, bool aragorn = false) =>
        BattleRules.Characters(state, battle.Force(owner)).Where(c =>
            aragorn && c.DefinitionId == CharacterRules.Aragorn ||
            SovereignLifecycle.Find(state, c.Id)?.Status == SovereignStatus.Awakened);
}

public sealed record SovereignInBattle(bool Aragorn = false) : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) =>
        KingsSovereignCombat.Candidates(state, battle, actor, Aragorn).Any();
}

public sealed record KingsBannerStep : EffectChoiceStep
{
    public override string Presentation => "CharacterLeadership";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var battle = BattleRuntime.Active(state)!;
        var force = battle.Force(execution.Owner);
        return KingsSovereignCombat.Candidates(state, battle, execution.Owner, true)
            .Where(c => BattleLeadership.CanForfeit(state, force, Cost(state, force, c))).Select(c => c.Id).ToArray();
    }
    private static Dictionary<string, int> Cost(MatchState state, BattleForce force, CharacterState character) =>
        new() { [character.Id] = KingsBattle.Leadership(state, force, character) };
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, SourceId = id, Title = CharacterCatalog.All[state.Characters.Single(c => c.Id == id).DefinitionId].Title,
        Detail = "Forfeit all this character's Leadership to cancel Shadow's Leader re-roll."
    };
    public override void Choose(EffectContext context, string id)
    {
        var battle = BattleRuntime.Active(context.State)!;
        var force = battle.Force(context.Owner);
        var character = context.State.Characters.Single(c => c.Id == id);
        BattleLeadership.Forfeit(context.State, force, Cost(context.State, force, character));
        battle.Opponent(context.Owner).Round.RerollLimit = 0;
        context.Record("KingsBanner", $"The King's Banner: {id} forfeited Leadership and cancelled Shadow's Leader re-roll.");
    }
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        var character = state.Characters.Single(c => c.Id == ids.Single());
        var force = BattleRuntime.Active(state)!.Force(execution.Owner);
        preview.Costs.Add($"{CharacterCatalog.All[character.DefinitionId].Title}: forfeit {KingsBattle.Leadership(state, force, character)} Leadership.");
    }
}

public sealed record GodOfOldStep : EffectChoiceStep
{
    public override string Presentation => "CharacterLeadership";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        KingsSovereignCombat.Candidates(state, BattleRuntime.Active(state)!, execution.Owner).Select(c => c.Id).ToArray();
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, SourceId = id, Title = SovereignCatalog.All[id].Title,
        Detail = "Special attack: roll one die per point of his Leadership; each 3+ hits the Shadow army."
    };
    public override void Choose(EffectContext context, string id)
    {
        var battle = BattleRuntime.Active(context.State)!;
        var dice = BattleLeadership.Available(context.State, battle.Force(context.Owner)).GetValueOrDefault(id);
        BattleRoll.Special(context.State, battle, context.Owner, context.Execution.SourceId, dice, context.Random, target: 3);
    }
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.HasUnknownResult = true;
}
