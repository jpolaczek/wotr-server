using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record CouncilSeparationCondition : PublicEventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => actor == Side.FreePeoples &&
        HuntRuntime.Active(state) is { DamageRule: not HuntDamageRule.CorruptionOnly };
}

public sealed record CouncilHobbitCondition : EventCondition
{
    // The partner's version is private. Only the public tile may suppress the window.
    public override bool CanOpenPublicWindow(MatchState state, Side actor) => actor == Side.FreePeoples &&
        HuntRuntime.Active(state) is { } hunt && HuntPool.Definitions.TryGetValue(hunt.TileId, out var tile) && tile.Pool == "standard";

    public override bool CanPlay(MatchState state, Side actor) => CanOpenPublicWindow(state, actor) &&
        CompanionRules.Inside(state).Any(c => c.DefinitionId == CouncilCompanions.Merry) &&
        CompanionRules.Inside(state).Any(c => c.DefinitionId == CouncilCompanions.Pippin);
}

public sealed record CouncilHobbitCancelStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var state = context.State;
        var hunt = HuntRuntime.Active(state)!;
        var hobbits = CompanionRules.Inside(state).Where(c => c.DefinitionId is CouncilCompanions.Merry or CouncilCompanions.Pippin).ToArray();
        foreach (var hobbit in hobbits)
        {
            LordsCompanionSetup.Reveal(state, hobbit);
            state.Fellowship.Companions.Remove(hobbit.Id);
            if (!state.Characters.Contains(hobbit))
                state.Characters.Add(hobbit);
            CharacterLifecycle.Eliminate(state, hobbit);
            HuntReports.Record(state, hunt, HuntReportKind.CompanionLeft, context.Execution.SourceId, hobbit.Id, hobbit.Location);
        }
        hunt.Damage = 0;
        hunt.Healing = 0;
        hunt.Reveal = false;
        hunt.Stop = false;
        hunt.Stage = HuntStage.Corruption;
        context.Record("HuntTileCancelled", $"{hunt.TileId}: both Hobbits of the Shire were eliminated.");
        FellowshipFlow.CompositionChanged(state, FellowshipReturn.Parent);
    }
}

public static class CouncilGuideReactions
{
    public const string Cancel = "council.hobbits-cancel-hunt";
    private static readonly IReadOnlyList<string> Separating = ["boromir", "gimli", "legolas"];

    public static IEnumerable<EffectDefinition> Programs =>
        Separating.Select(name => new EffectDefinition("council." + name + "-guide", [new CharacterReactionStep(CharacterReaction.HobbitGuide)]))
            .Append(new(Cancel, [new CouncilHobbitCancelStep()]));

    public static IEnumerable<EffectReaction> Reactions => Separating.Select(name => new EffectReaction(
        "council." + name + "-guide", "hunt.guide", "council." + name + "-guide",
        new CouncilSeparationCondition(), RequiredGuideId: "lords.character." + name))
        .Concat(new[] { CouncilCompanions.Merry, CouncilCompanions.Pippin }.Select(id => new EffectReaction(
            Cancel + "." + id, "hunt.guide", Cancel, new CouncilHobbitCondition(), RequiredGuideId: id)));

    public static (string Title, string Detail)? Describe(EffectReaction reaction) => reaction.Condition switch
    {
        CouncilHobbitCondition => ("Cancel Hunt tile", "Eliminate both Hobbits of the Shire from the Fellowship to cancel this standard tile."),
        CouncilSeparationCondition => ("Separate · Reduce damage", "Separate this Companion. Reduce damage by one, to a minimum of zero; all departures against this tile move together."),
        _ => null
    };
}
