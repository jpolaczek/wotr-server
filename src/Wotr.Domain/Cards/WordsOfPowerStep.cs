using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record WordsOfPowerStep : EffectChoiceStep
{
    public override string Presentation => "Companion";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        CombatCardFigures.Companions(state, BattleRuntime.Active(state)!.Opponent(execution.Owner)).Select(c => c.Id).ToList();

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        SourceId = id,
        Title = CharacterCatalog.All[state.Characters.Single(c => c.Id == id).DefinitionId].Title,
        Detail = "Cancel this Companion's Leadership and special abilities for this Combat round."
    };

    public override void Choose(EffectContext context, string id)
    {
        var round = BattleRuntime.Active(context.State)!.Opponent(context.Owner).Round;
        round.CancelledLeadership.Add(id);
        round.CancelledAbilities.Add(id);
        context.Record("CompanionSilenced", $"Words of Power: {id} loses Leadership and abilities this round.");
    }
}
