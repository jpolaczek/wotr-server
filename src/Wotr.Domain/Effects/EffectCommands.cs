using Wotr.Contracts;

namespace Wotr.Domain;

public interface IResolutionContinuation
{
    bool CanContinue(MatchState state);
    void Continue(MatchState state, IRandomSource random);
}

public sealed class EffectCommands : ICommandHandler, IResolutionContinuation, ICommandPreviewProvider
{
    private readonly EffectCatalog catalog;
    private readonly EffectRuntime runtime;

    public EffectCommands(EffectCatalog? catalog = null)
    {
        this.catalog = catalog ?? EffectCatalog.All;
        runtime = new(this.catalog);
    }

    public bool Handles(CommandKind kind) => kind is CommandKind.DiscardWelcome or CommandKind.DiscardBaradPalantir || LordsTableRemoval.Handles(kind) || kind is CommandKind.PlayEvent or CommandKind.ChooseEffect or CommandKind.RespondEffect or CommandKind.TakeEventLosses or CommandKind.DiscardProtection or CommandKind.DiscardPalantir or CommandKind.DiscardDenethor;

    public void DescribePreview(MatchState state, GameCommand command, ActionPreview preview)
    {
        if (command.Kind == CommandKind.DiscardProtection)
            preview.Costs.AddRange(command.Payload.Ids);
        if (command.Kind == CommandKind.ChooseEffect)
            runtime.Choice(state)!.PreviewPayload(state, EffectRuntime.Current(state)!.Effect!, command.Payload, preview);
        if (command.Kind == CommandKind.TakeEventLosses)
            EventLosses.DescribePreview(state, command.Payload.Losses, preview);
        if (command.Kind == CommandKind.RespondEffect)
            ChiefReactions.DescribePreview(state, command.Payload.ReactionId, preview);
        if (command.Kind == CommandKind.RespondEffect && command.Payload.ReactionId.StartsWith(LordsShadowCards.GatesReaction, StringComparison.Ordinal))
        {
            preview.Costs.Add(LordsShadowCards.Gates);
            preview.Summary = "Discard The Gates are Closed; draw an additional Hunt tile, ignoring Eye and Reveal.";
        }
        if (command.Kind == CommandKind.DiscardWelcome)
            preview.Summary = "Spend the selected die; discard Your Welcome Is Doubtful.";
        if (command.Kind == CommandKind.DiscardLore)
            preview.Summary = "Spend the selected die" + (command.Payload.RingId.Length == 0 ? "" : " and transfer the selected Ring to Shadow") + "; discard The Lore of the Elven Rings.";
        if (command.Kind == CommandKind.DiscardGates)
            preview.Summary = "Spend the selected die; discard The Gates are Closed.";
        if (command.Kind == CommandKind.RespondEffect && command.Payload.ReactionId == NenyaReaction.Id)
        {
            preview.Costs.Add(LordsDiceSetup.NenyaRing);
            preview.Summary = "Use Nenya to remove this standard Eye permanently and draw a replacement tile.";
        }
        if (command.Kind == CommandKind.RespondEffect && command.Payload.ReactionId == WeShallGetIt.Reaction)
        {
            preview.Costs.Add(SmeagolRules.WeShallGetIt);
            preview.Summary = "Discard We Shall Get It; replace this Hunt tile before resolving its effects.";
        }
    }

    public string? Validate(MatchState state, Side actor, GameCommand command) => command.Kind switch
    {
        CommandKind.DiscardBaradPalantir => KingsPalantir.ValidRemoval(state, actor, command.Payload) ? null : "IllegalChoice",
        CommandKind.DiscardWelcome => KingsWelcomeRemoval.Valid(state, actor, command.Payload) ? null : "IllegalChoice",
        CommandKind.DiscardLore or CommandKind.DiscardGates => LordsTableRemoval.Valid(state, actor, command) ? null : "IllegalChoice",
        CommandKind.DiscardDenethor => DenethorRemoval.Valid(state, actor, command.Payload) ? null : "IllegalChoice",
        CommandKind.DiscardPalantir => PalantirRemoval.Valid(state, actor, command.Payload) ? null : "IllegalChoice",
        CommandKind.DiscardProtection => RegionProtection.Valid(state, actor, command.Payload) ? null : "IllegalChoice",
        CommandKind.PlayEvent => ValidateEvent(state, actor, command.Payload) ? null : "IllegalChoice",
        CommandKind.TakeEventLosses => EventLosses.Plan(state, command.Payload.Losses) is not null ? null : "IllegalChoice",
        CommandKind.ChooseEffect => state.Pending?.Kind == DecisionKind.EffectChoice && runtime.Choice(state) is { } choice &&
            choice.ValidPayload(state, EffectRuntime.Current(state)!.Effect!, command.Payload) ? null : "IllegalChoice",
        CommandKind.RespondEffect => state.Pending?.Kind == DecisionKind.Reaction &&
            runtime.Reactions(state, actor).Any(r => r.Id == command.Payload.ReactionId) ? null : "IllegalChoice",
        _ => "IllegalChoice"
    };

    private bool ValidateEvent(MatchState state, Side actor, CommandPayload payload) =>
        !string.IsNullOrEmpty(payload.CardId) && catalog.Events.TryGetValue(payload.CardId, out var definition) &&
        EventRules.CanPlay(state, actor, definition) && definition.Condition.ValidTarget(state, actor, payload) &&
        EventRules.ValidCost(state, actor, payload.CardId, payload.DieId, payload.CardCost, payload.AbilityId);

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
    {
        switch (command.Kind)
        {
            case CommandKind.DiscardBaradPalantir:
                KingsPalantir.Remove(state, command.Payload);
                break;
            case CommandKind.DiscardWelcome:
                KingsWelcomeRemoval.Apply(state, command.Payload);
                break;
            case CommandKind.DiscardLore:
            case CommandKind.DiscardGates:
                LordsTableRemoval.Apply(state, command);
                break;
            case CommandKind.DiscardDenethor:
                DenethorRemoval.Apply(state, command.Payload);
                break;
            case CommandKind.DiscardPalantir:
                PalantirRemoval.Apply(state, command.Payload);
                break;
            case CommandKind.DiscardProtection:
                RegionProtection.Remove(state, command.Payload);
                break;
            case CommandKind.TakeEventLosses:
                EventLosses.Apply(state, EventLosses.Plan(state, command.Payload.Losses)!);
                break;
            case CommandKind.PlayEvent:
                runtime.BeginEvent(state, actor, command.Payload);
                break;
            case CommandKind.ChooseEffect:
                runtime.Choose(state, command.Payload, random);
                break;
            case CommandKind.RespondEffect:
                runtime.Respond(state, actor, command.Payload.ReactionId);
                break;
        }
    }

    public bool CanContinue(MatchState state) => state.Pending is null && EffectRuntime.Current(state) is not null;
    public void Continue(MatchState state, IRandomSource random) => runtime.Continue(state, random);
}
