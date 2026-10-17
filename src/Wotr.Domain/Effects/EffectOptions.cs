using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class EffectOptions : IActionOptionsProvider
{
    private readonly EffectCatalog catalog;
    private readonly EffectRuntime runtime;

    public EffectOptions(EffectCatalog? catalog = null)
    {
        this.catalog = catalog ?? EffectCatalog.All;
        runtime = new(this.catalog);
    }

    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (state.Pending?.Owner != actor)
            yield break;
        if (RegionProtection.Option(state, actor) is { } removal)
            yield return removal;
        if (PalantirRemoval.Option(state, actor) is { } palantirRemoval)
            yield return palantirRemoval;
        if (DenethorRemoval.Option(state, actor) is { } denethorRemoval)
            yield return denethorRemoval;
        if (LordsTableRemoval.Option(state, actor, CommandKind.DiscardLore) is { } loreRemoval)
            yield return loreRemoval;
        if (LordsTableRemoval.Option(state, actor, CommandKind.DiscardGates) is { } gatesRemoval)
            yield return gatesRemoval;
        if (KingsWelcomeRemoval.Option(state, actor) is { } welcomeRemoval)
            yield return welcomeRemoval;
        if (KingsPalantir.RemovalOption(state, actor) is { } baradRemoval)
            yield return baradRemoval;
        if (EventLosses.Current(state) is { } loss)
            yield return new()
            {
                Kind = CommandKind.TakeEventLosses,
                EligibleIds = loss.Target.Units(state).Where(BoardRules.IsArmyUnit).Select(u => u.Id).ToList(),
                Minimum = loss.Hits,
                Maximum = loss.Hits
            };
        if (state.Pending.Kind == DecisionKind.Action || EventRules.FreeCharacterWindow(state, actor))
        {
            var plays = catalog.Events.Values.Where(e => EventRules.CanPlay(state, actor, e)).Select(e => new EventPlayOption
            {
                CardId = e.CardId,
                Costs = EventRules.Costs(state, actor, e.CardId),
                NationTargets = [.. e.Condition.NationTargets(state, actor)]
            }).Where(e => e.Costs.Count > 0).ToList();
            if (plays.Count > 0)
                yield return new() { Kind = CommandKind.PlayEvent, Events = plays, EligibleIds = plays.Select(p => p.CardId).ToList() };
        }
        if (runtime.Choice(state) is { } choice)
            yield return choice.Option(state, EffectRuntime.Current(state)!.Effect!);
        if (EffectRuntime.Current(state)?.Effect?.Wait == EffectWait.Reaction)
        {
            var reactions = runtime.Reactions(state, actor).ToList();
            yield return new()
            {
                Kind = CommandKind.RespondEffect,
                EligibleIds = reactions.Select(r => r.Id).ToList(),
                EffectChoices = reactions.Select(ReactionChoice).ToList(),
                Enabled = reactions.Count > 0
            };
        }
    }

    private static EffectChoiceView ReactionChoice(EffectReaction reaction)
    {
        var description = HuntReactionDescriptions.Describe(reaction) ?? CouncilGuideReactions.Describe(reaction) ??
            ChiefReactions.Describe(reaction.Id) ?? CharacterReactions.Describe(reaction);
        return new()
        {
            Id = reaction.Id,
            SourceId = reaction.RequiredTableCard ?? reaction.RequiredGuideId ?? reaction.RequiredCharacterId ?? reaction.Id,
            Cost = reaction.Cost.ToString(),
            Title = description.Title,
            Detail = description.Detail
        };
    }
}
