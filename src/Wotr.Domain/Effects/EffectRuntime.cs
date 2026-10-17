using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class EffectRuntime(EffectCatalog catalog)
{
    public static ResolutionFrame? Current(MatchState state) => state.Resolution.Frames.LastOrDefault() is { Effect: not null } frame ? frame : null;

    public static void BeginProgram(MatchState state, Side owner, string source, string program)
    {
        state.Pending = null;
        state.Resolution.Frames.Add(new()
        {
            EffectId = program,
            Effect = new() { Owner = owner, SourceId = source, Return = EffectReturn.Parent }
        });
    }

    // Only public sources can suppress these windows. Never infer their presence from a private hand.
    public bool BeginPublicWindow(MatchState state, Side owner, string source, string window)
    {
        var availableSource = catalog.Reactions.Values.Any(r => r.Window == window && CanOpen(state, owner, r));
        if (!availableSource)
            return false;
        var frame = new ResolutionFrame
        {
            EffectId = EffectCatalog.ReactionProgram,
            Effect = new()
            {
                Owner = owner,
                SourceId = source,
                Return = EffectReturn.Parent,
                Wait = EffectWait.Reaction,
                ReactionWindow = window
            }
        };
        state.Resolution.Frames.Add(frame);
        state.Open(DecisionKind.Reaction, owner, [CommandKind.RespondEffect, CommandKind.DeclineReaction]);
        state.Pending!.Window = window;
        state.Pending.SourceId = source;
        OfferReactions(state, frame);
        return true;
    }

    public void BeginEvent(MatchState state, Side actor, CommandPayload payload)
    {
        var definition = catalog.Events[payload.CardId];
        var free = EventRules.FreeCharacterWindow(state, actor);
        var anarion = !free && KingsAnarion.CanUse(state, actor, payload.DieId, payload.CardCost);
        var palantir = !free && actor == Side.Shadow && payload.CardCost == DieFace.Event &&
            state.Player(actor).Table.Contains(ShadowCharacterCards04.Palantir);
        if (free)
            Current(state)!.Effect!.Wait = EffectWait.None;
        else
            CharacterCosts.Pay(state, actor, payload.DieId, payload.CardCost, payload.AbilityId);
        var player = state.Player(actor);
        HandRules.Remove(player, payload.CardId);
        (definition.StaysOnTable ? player.Table : player.Discard).Add(payload.CardId);
        state.Events.Add(new() { Revision = state.Revision, Kind = "EventPlayed",
            Activity = HistoryFacts.Card(state, actor, ActionCardUse.Event, payload.CardId),
            Text = $"{actor} played {Content.CardCatalog.Title(payload.CardId)}." });
        state.Pending = null;
        state.Resolution.Frames.Add(new()
        {
            EffectId = definition.ProgramId,
            Effect = new()
            {
                Owner = actor,
                SourceId = payload.CardId,
                NationId = payload.NationId,
                PaidFace = payload.CardCost,
                IsEvent = true,
                PalantirDrawPending = palantir,
                AnarionDrawPending = anarion,
                Committed = [new(actor, payload.CardId, payload.NationId)],
                Return = free ? EffectReturn.Parent : EffectReturn.FinishAction
            }
        });
        BeginPublicWindow(state, actor, payload.CardId, "event.played");
    }

    public void Continue(MatchState state, IRandomSource random)
    {
        while (state.Pending is null && Current(state) is { } frame)
        {
            if (CouncilHobbitReturn.BeginPending(state))
                return;
            if (KingsPolitics.BeginPending(state))
                continue;
            if (frame.Effect!.Wait != EffectWait.None)
                throw new InvalidOperationException("An effect cannot resume without its decision.");
            var program = catalog.Programs[frame.EffectId];
            if (frame.NextStep == program.Steps.Count)
            {
                if (Complete(state, frame))
                    continue;
                return;
            }
            var step = program.Steps[frame.NextStep++];
            step.Apply(new(state, frame, random));
            HuntReports.Capture(state, frame.Effect.SourceId);
            ActiveEffects.CheckConditions(state);
            if (frame.Effect.Wait == EffectWait.Reaction)
                OfferReactions(state, frame);
        }
    }

    private bool Complete(MatchState state, ResolutionFrame frame)
    {
        var execution = frame.Effect!;
        if (frame.EffectId == EffectCatalog.ReactionProgram && ReopenLocationWindow(state, execution))
            return false;
        if ((execution.IsEvent || execution.Return == EffectReturn.FinishAction) && StackLimits.OpenIfRequired(state, execution.Owner))
            return false;
        if (execution.PalantirDrawPending)
        {
            execution.PalantirDrawPending = false;
            if (state.Player(execution.Owner).Table.Contains(ShadowCharacterCards04.Palantir))
            {
                BeginProgram(state, execution.Owner, ShadowCharacterCards04.Palantir, ShadowCharacterCards04.PalantirDraw);
                return true;
            }
        }
        if (execution.AnarionDrawPending)
        {
            execution.AnarionDrawPending = false;
            if (KingsAnarion.Begin(state))
                return true;
        }
        if (execution.Return == EffectReturn.FinishAction && !execution.CompletionWindowOffered)
        {
            execution.CompletionWindowOffered = true;
            if (BeginPublicWindow(state, execution.Owner, execution.SourceId, "event.completed"))
                return false;
        }
        state.Resolution.Frames.RemoveAt(state.Resolution.Frames.Count - 1);
        ActiveEffects.CheckConditions(state);
        switch (execution.Return)
        {
            case EffectReturn.Parent:
                return true;
            case EffectReturn.Decision:
                state.Pending = frame.ReturnDecision ?? throw new InvalidOperationException("Missing return decision.");
                break;
            case EffectReturn.FinishAction:
                TurnFlow.FinishAction(state, execution.Owner, execution.FellowshipMove);
                break;
            case EffectReturn.BeginActions:
                TurnFlow.BeginActions(state);
                break;
        }
        return false;
    }

    public EffectChoiceStep? Choice(MatchState state)
    {
        var frame = Current(state);
        return frame?.Effect?.Wait == EffectWait.Choice && frame.NextStep > 0
            ? catalog.Programs[frame.EffectId].Steps[frame.NextStep - 1] as EffectChoiceStep : null;
    }

    public void Choose(MatchState state, CommandPayload payload, IRandomSource random)
    {
        var step = Choice(state)!;
        var frame = Current(state)!;
        var selections = step.Selections(state, frame.Effect!, payload).ToArray();
        frame.Effect!.Wait = EffectWait.None;
        state.Pending = null;
        foreach (var selection in selections)
            RecordSelection(state, selection);
        step.ChoosePayload(new(state, frame, random), payload);
    }

    public IEnumerable<EffectReaction> Reactions(MatchState state, Side actor)
    {
        var execution = Current(state)?.Effect;
        if (execution?.Wait != EffectWait.Reaction || state.Pending?.Owner != actor)
            return [];
        return execution.OfferedReactions.Select(id => catalog.Reactions[id])
            .Where(r => r.Condition.CanPlay(state, actor) &&
                (r.RequiredTableCard is null || state.Player(actor).Table.Contains(r.RequiredTableCard)) &&
                (r.RequiredGuideId is null || CharacterRules.GuideDefinition(state) == r.RequiredGuideId) &&
                (r.RequiredCharacterId is null || CharacterRules.InPlay(state, r.RequiredCharacterId) is not null));
    }

    private void OfferReactions(MatchState state, ResolutionFrame frame)
    {
        frame.Effect!.OfferedReactions = catalog.Reactions.Values
            .Where(r => r.Window == frame.Effect.ReactionWindow).Select(r => r.Id).ToList();
        state.Pending!.PrivateChoices = Reactions(state, state.Pending.Owner).Select(r => r.Id).ToList();
    }

    public void Respond(MatchState state, Side actor, string reactionId)
    {
        var parent = Current(state)!;
        parent.Effect!.Wait = EffectWait.None;
        state.Pending = null;
        var reaction = catalog.Reactions[reactionId];
        if (LocationWindow(state, parent.Effect))
            parent.Effect.OfferedReactions.Remove(reactionId);
        var source = reaction.RequiredTableCard ?? reaction.RequiredGuideId ?? reaction.RequiredCharacterId ?? reaction.Id;
        state.HistoryCapture?.React(actor, source, reaction.Cost == ReactionCost.DiscardTableCard);
        RecordSelection(state, new(actor, source));
        if (parent.Effect.ReactionWindow.StartsWith("hunt.", StringComparison.Ordinal))
            HuntReports.Record(state, HuntRuntime.Active(state), Wotr.Contracts.HuntReportKind.Reaction, source);
        if (reaction.Cost == ReactionCost.DiscardTableCard)
        {
            ActiveEffects.RemoveWhere(state, e => e.Owner == actor && e.SourceId == reaction.RequiredTableCard);
            var player = state.Player(actor);
            if (player.Table.Remove(reaction.RequiredTableCard!))
                player.Discard.Add(reaction.RequiredTableCard!);
        }
        state.Resolution.Frames.Add(new()
        {
            EffectId = reaction.ProgramId,
            Effect = new() { Owner = actor, SourceId = source, Return = EffectReturn.Parent }
        });
    }

    public static void Decline(MatchState state)
    {
        if (LocationWindow(state, Current(state)!.Effect!))
            Current(state)!.Effect!.OfferedReactions.Clear();
        Current(state)!.Effect!.Wait = EffectWait.None;
        state.Pending = null;
    }

    private static bool CanOpen(MatchState state, Side owner, EffectReaction reaction) =>
        (reaction.RequiredTableCard is not null && state.Player(owner).Table.Contains(reaction.RequiredTableCard) ||
         reaction.RequiredGuideId is not null && CharacterRules.GuideDefinition(state) == reaction.RequiredGuideId ||
         reaction.RequiredCharacterId is not null && CharacterRules.InPlay(state, reaction.RequiredCharacterId) is not null) &&
        reaction.Condition.CanOpenPublicWindow(state, owner);

    private static bool LocationWindow(MatchState state, EffectExecution execution) => state.Configuration.Lords &&
        execution.ReactionWindow is "fellowship.declared" or "fellowship.revealed";

    private bool ReopenLocationWindow(MatchState state, EffectExecution execution)
    {
        if (!LocationWindow(state, execution) ||
            !execution.OfferedReactions.Any(id => CanOpen(state, execution.Owner, catalog.Reactions[id])))
            return false;
        execution.Wait = EffectWait.Reaction;
        state.Open(DecisionKind.Reaction, execution.Owner, [CommandKind.RespondEffect, CommandKind.DeclineReaction]);
        state.Pending!.Window = execution.ReactionWindow;
        state.Pending.SourceId = execution.SourceId;
        state.Pending.PrivateChoices = Reactions(state, execution.Owner).Select(r => r.Id).ToList();
        return true;
    }

    private static void RecordSelection(MatchState state, EffectSelection selection)
    {
        state.Resolution.Frames.First(f => f.Effect is not null).Effect!.Committed.Add(selection);
        var activity = HistoryFacts.At(state, ActionContextKind.Cards, selection.SourceId);
        activity.Actor = selection.Owner;
        activity.Facts.Targets = new[] { selection.ChoiceId, selection.TargetId }.Where(id => id.Length > 0).Distinct().ToList();
        state.Events.Add(new()
        {
            Revision = state.Revision, Kind = "OwnEffectChoice", OnlyFor = selection.Owner,
            Text = "Selected an effect option.", Activity = activity
        });
    }
}
