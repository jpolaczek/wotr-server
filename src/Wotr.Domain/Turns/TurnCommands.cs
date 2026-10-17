using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class TurnCommands : ICommandHandler
{
    public bool Handles(CommandKind kind) => kind is CommandKind.ContinueFellowship or CommandKind.AllocateHunt
        or CommandKind.DiscardCards or CommandKind.DiscardDie or CommandKind.Pass or CommandKind.DrawCard or CommandKind.UseRing or CommandKind.FinishActionSequence;

    public string? Validate(MatchState state, Side actor, GameCommand command)
    {
        var payload = command.Payload;
        return command.Kind switch
        {
            CommandKind.FinishActionSequence => ActionSequences.CanFinish(state, actor, payload) ? null : "IllegalChoice",
            CommandKind.ContinueFellowship => state.Phase == GamePhase.Fellowship && actor == Side.FreePeoples &&
                state.Pending?.Kind == DecisionKind.FellowshipPhase && CompanionRules.Guides(state).Contains(state.Fellowship.Guide) ? null : "IllegalChoice",
            CommandKind.AllocateHunt => ValidateHunt(state, actor, payload),
            CommandKind.DiscardCards => state.Pending?.Kind == DecisionKind.DiscardCards && state.AfterDiscard != DiscardContinuation.None &&
                DraftSelection.IsSubset(payload.Ids, state.Player(actor).Hand, 1, state.Pending.RequiredCount) ? null : "IllegalChoice",
            _ => state.Phase == GamePhase.ActionResolution && state.Pending?.Kind == DecisionKind.Action
                ? ValidateAction(state, actor, command) : "IllegalChoice"
        };
    }

    // BASE 2.4.1 p. 21: after using an Elven Ring the player "can also skip or pass the action, if eligible to do
    // so" (QUAL-04), so a pending action after the Ring does not forbid passing; only the unused dice decide.
    public static bool CanPass(MatchState state, Side actor) =>
        TurnFlow.Available(state, actor) < TurnFlow.Available(state, TurnFlow.Other(actor));

    private static string? ValidateHunt(MatchState state, Side actor, CommandPayload payload) =>
        state.Phase == GamePhase.HuntAllocation && actor == Side.Shadow &&
        DraftSelection.IsSubset(payload.Ids, DiceRules.HuntCandidates(state), DiceRules.HuntMinimum(state), DiceRules.HuntMaximum(state))
            ? null : "IllegalChoice";

    private static string? ValidateAction(MatchState state, Side actor, GameCommand command)
    {
        var die = DiceRules.AvailableDie(state, actor, command.Payload.DieId);
        return command.Kind switch
        {
            CommandKind.DiscardDie => die is not null ? null : "IllegalChoice",
            CommandKind.Pass => CanPass(state, actor) ? null : "IllegalChoice",
            CommandKind.DrawCard => CardDrawCost.Valid(state, actor, command.Payload) &&
                command.Payload.Deck is "character" or "strategy" && state.Player(actor).Decks[command.Payload.Deck].Count > 0 ? null : "IllegalChoice",
            CommandKind.UseRing => RingRules.Validate(state, actor, command.Payload),
            _ => "IllegalChoice"
        };
    }

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
    {
        switch (command.Kind)
        {
            case CommandKind.FinishActionSequence:
                ActionSequences.Finish(state, actor);
                break;
            case CommandKind.ContinueFellowship:
                TurnFlow.OpenHuntAllocation(state);
                break;
            case CommandKind.AllocateHunt:
                AllocateHunt(state, command.Payload.Ids, random);
                break;
            case CommandKind.DiscardCards:
                CardFlow.Discard(state, actor, command.Payload.Ids);
                break;
            case CommandKind.UseRing:
                RingRules.Apply(state, actor, command.Payload);
                break;
            case CommandKind.DrawCard:
                DrawCard(state, actor, command.Payload);
                break;
            case CommandKind.DiscardDie:
                DiceRules.AvailableDie(state, actor, command.Payload.DieId)!.Location = DieLocation.Discarded;
                state.Events.Add(new()
                {
                    Revision = state.Revision,
                    Kind = "DieDiscarded",
                    Text = $"{actor} discarded a die without effect."
                });
                TurnFlow.FinishAction(state, actor);
                break;
            case CommandKind.Pass:
                state.Events.Add(new()
                {
                    Revision = state.Revision,
                    Kind = "Passed",
                    Text = $"{actor} passed."
                });
                // The pass ends the action awaited after a Ring; the Ring stays used, passed on or removed.
                state.Player(actor).RingActionPending = false;
                TurnFlow.SelectNext(state, TurnFlow.Other(actor));
                break;
        }
    }

    private static void AllocateHunt(MatchState state, IReadOnlyCollection<string> ids, IRandomSource random)
    {
        foreach (var id in ids)
        {
            var die = state.Dice.Single(d => d.Id == id);
            die.Location = DieLocation.Hunt;
            die.Face = DieFace.Eye;
            die.HuntOrigin = "allocated";
        }
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = "HuntAllocated",
            Text = $"Shadow allocated {ids.Count} Hunt dice."
        });
        TurnFlow.RollAndBeginActions(state, random);
    }

    private static void DrawCard(MatchState state, Side actor, CommandPayload payload)
    {
        var die = DiceRules.AvailableDie(state, actor, payload.DieId)!;
        var anarion = KingsAnarion.CanUse(state, actor, die.Id, DieFace.Event);
        CardDrawCost.Pay(state, actor, payload);
        CardFlow.Draw(state, actor, payload.Deck);
        if (anarion && KingsAnarion.Begin(state, finishAction: true))
            return;
        if (!CardFlow.RequireDiscard(state, actor, DiscardContinuation.FinishAction))
            TurnFlow.FinishAction(state, actor);
    }
}
