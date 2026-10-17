using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class MusterCommands : ICommandHandler
{
    public bool Handles(CommandKind kind) => kind is CommandKind.AdvancePolitics or CommandKind.Recruit or CommandKind.RemoveExcess;

    public string? Validate(MatchState state, Side actor, GameCommand command)
    {
        if (command.Payload.Stepwise)
            return ActionStepRules.Validate(state, actor, command);
        if (command.Kind == CommandKind.RemoveExcess)
            return state.Pending?.Kind == DecisionKind.RemoveExcess && DraftSelection.IsSubset(command.Payload.Ids,
                state.Pending.PrivateChoices, state.Pending.RequiredCount, state.Pending.RequiredCount) ? null : "IllegalChoice";
        var die = DiceRules.AvailableDie(state, actor, command.Payload.DieId);
        if (state.Phase != GamePhase.ActionResolution || state.Pending?.Kind != DecisionKind.Action ||
            die == null || !DiceRules.CanPay(die, DieFace.Muster))
            return "IllegalChoice";
        if (command.Kind == CommandKind.Recruit)
            return RecruitmentRules.ValidateMuster(state, actor, command.Payload.Placements);
        var nation = state.Nations.Find(n => n.Id == command.Payload.NationId);
        return nation != null && BoardRules.Nations[nation.Id].Side == actor && PoliticalRules.CanMusterAdvance(state, nation) ? null : "IllegalChoice";
    }

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
    {
        if (command.Payload.Stepwise)
        {
            ActionSequences.Apply(state, actor, command);
            return;
        }
        if (command.Kind == CommandKind.RemoveExcess)
        {
            StackLimits.Remove(state, actor, command.Payload.Ids);
            return;
        }
        DiceRules.Pay(state, actor, command.Payload.DieId, DieFace.Muster);
        if (command.Kind == CommandKind.AdvancePolitics)
            PoliticalRules.Advance(state, command.Payload.NationId);
        else
            RecruitmentRules.Place(state, actor, command.Payload.Placements);
        if (!StackLimits.OpenIfRequired(state, actor))
            TurnFlow.FinishAction(state, actor);
    }
}
