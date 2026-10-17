using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class MovementCommands : ICommandHandler
{
    public bool Handles(CommandKind kind) => kind is CommandKind.MoveArmies or CommandKind.FinishArmyEvent;
    public string? Validate(MatchState state, Side actor, GameCommand command)
    {
        if (command.Payload.Stepwise)
            return ActionStepRules.Validate(state, actor, command);
        if (command.Kind == CommandKind.FinishArmyEvent)
            return ArmyEvents.Optional(ArmyEvents.Current(state, actor)) && command.Payload.DieId == "" &&
                command.Payload.Moves.Count == 0 ? null : "IllegalChoice";
        if (state.Phase != GamePhase.ActionResolution || state.Pending?.Kind is not (DecisionKind.Action or DecisionKind.ArmyEvent))
            return "IllegalChoice";
        if (ArmyEvents.Current(state, actor) is { } order && ShadowMarchRules.Supports(order))
        {
            ShadowMarchRules.Plan(state, actor, command.Payload, order, out var error);
            return error;
        }
        return MovementRules.Plan(state, actor, command.Payload) is not null ? null : "IllegalChoice";
    }

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
    {
        if (command.Payload.Stepwise)
        {
            ActionSequences.Apply(state, actor, command);
            return;
        }
        if (command.Kind == CommandKind.FinishArmyEvent)
        {
            ArmyEvents.CompleteChoice(state);
            return;
        }
        var plan = MovementRules.Plan(state, actor, command.Payload)
            ?? throw new InvalidOperationException("Movement must be validated before execution.");
        plan.Apply(state);
        if (ArmyEvents.Current(state, actor) == ArmyEventOrder.Corsairs)
        {
            CorsairRules.CompleteLanding(state, command.Payload);
            return;
        }
        if (ArmyEvents.Current(state, actor) is not null)
        {
            ArmyEvents.CompleteChoice(state);
            return;
        }
        CharacterCosts.PayArmy(state, actor, command.Payload);
        if (!StackLimits.OpenIfRequired(state, actor))
            TurnFlow.FinishAction(state, actor);
    }
}
