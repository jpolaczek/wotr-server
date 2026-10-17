using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record EngineResult(MatchState State, CommandReceipt Receipt);

public interface ICommandHandler
{
    bool Handles(CommandKind kind);
    string? Validate(MatchState state, Side actor, GameCommand command);
    void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random);
}

public interface ICommandPreviewProvider
{
    void DescribePreview(MatchState state, GameCommand command, ActionPreview preview);
}

public sealed class GameEngine(params ICommandHandler[] handlers)
{
    private readonly IResolutionContinuation[] continuations = handlers.OfType<IResolutionContinuation>().ToArray();
    public void DescribePreview(MatchState state, GameCommand command, ActionPreview preview)
    {
        if (handlers.Single(h => h.Handles(command.Kind)) is ICommandPreviewProvider provider)
            provider.DescribePreview(state, command, preview);
    }

    public EngineResult Execute(MatchState original, Side actor, GameCommand command, IRandomSource entropy)
    {
        EngineResult Reject(string code) => new(original, new()
        {
            CommandId = command.CommandId,
            Revision = original.Revision,
            Error = code
        });
        var error = Validate(original, actor, command);
        if (error is not null)
            return Reject(Visibility.SafeError(error));
        var handler = handlers.Single(h => h.Handles(command.Kind));
        var next = StateCodec.Copy(original);
        var preferenceChange = command.Kind == CommandKind.SetCombatCardPolicy;
        var publicChange = !preferenceChange || command.Payload.AutoDecision!.Enabled &&
            next.Pending is { Kind: DecisionKind.CombatCard } pending && pending.Owner == actor;
        if (publicChange)
            next.Revision++;
        if (publicChange)
            next.HistoryCapture = new(original, command);
        var random = new RecordingRandom(entropy, next);
        KingsRulerTokens.Apply(next, command.Payload);
        handler.Apply(next, actor, command, random);
        HuntReports.Capture(next);
        while (publicChange && next.Winner is null)
        {
            SovereignLifecycle.Reconcile(next);
            CouncilHobbitReturn.BeginPending(next);
            KingsPolitics.BeginPending(next);
            ActiveEffects.CheckConditions(next);
            var continuation = continuations.SingleOrDefault(c => c.CanContinue(next));
            if (continuation is null)
                break;
            continuation.Continue(next, random);
            HuntReports.Capture(next);
        }
        if (publicChange)
            ActiveEffects.CheckConditions(next);
        AutoDecisions.Reconcile(next);
        HuntReports.CompleteIfIdle(next);
        if (publicChange)
            ActionLogRecorder.Record(original, next, actor, command);
        next.HistoryCapture = null;
        return new(next, new()
        {
            CommandId = command.CommandId,
            Accepted = true,
            Revision = next.Revision,
            PublicStateChanged = publicChange,
            AutoDecision = preferenceChange ? AutoDecisions.View(next.Player(actor)) : null
        });
    }
    public string? Validate(MatchState original, Side actor, GameCommand command)
    {
        if (!CommandShape.IsValid(command))
            return CommandShape.MalformedCommand;
        if (command.MatchId != original.Id)
            return "NotAuthorized";
        if (original.Winner is not null || original.Phase == GamePhase.Finished)
            return "GameFinished";
        var handler = handlers.SingleOrDefault(h => h.Handles(command.Kind));
        if (command.Kind == CommandKind.SetCombatCardPolicy)
            return !string.IsNullOrEmpty(command.Payload.RulerTokenId) || !string.IsNullOrEmpty(command.Payload.CouncilTokenId) ? "IllegalChoice"
                : handler is null ? "UnsupportedAction" : handler.Validate(original, actor, command);
        if (command.ExpectedRevision != original.Revision)
            return "StaleRevision";
        if (original.Pending is null || command.DecisionId != original.Pending.Id)
            return "DecisionExpired";
        if (original.Pending.Owner != actor)
            return "NotAuthorized";
        if (!original.Pending.Commands.Contains(command.Kind))
            return "IllegalChoice";
        if (!KingsRulerTokens.Valid(original, actor, command) || !KingsCouncilEntry.ValidToken(original, actor, command))
            return "IllegalChoice";
        return handler is null ? "UnsupportedAction" : handler.Validate(KingsRulerTokens.ValidationState(original, command.Payload), actor, command);
    }
}

public sealed class DeclineReactionHandler : ICommandHandler
{
    public bool Handles(CommandKind kind) => kind == CommandKind.DeclineReaction;
    public string? Validate(MatchState state, Side actor, GameCommand command)
        => state.Pending?.Kind == DecisionKind.Reaction && state.Resolution.Frames.Count > 0 &&
            (EffectRuntime.Current(state) is not { } frame || frame.Effect!.Wait is EffectWait.Reaction or EffectWait.FreeEvent) ? null : "IllegalChoice";
    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
    {
        if (EffectRuntime.Current(state) is not null)
            EffectRuntime.Decline(state);
        else
            state.Resolution.Resume(state);
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = "ReactionCompleted",
            Text = "Reaction resolved."
        });
    }
}
