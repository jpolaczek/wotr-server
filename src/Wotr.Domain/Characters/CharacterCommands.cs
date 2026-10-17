using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class CharacterCommands : ICommandHandler
{
    public bool Handles(CommandKind kind) => kind is CommandKind.EnterCharacter or CommandKind.UseCharacterAbility or CommandKind.MoveShadowCharacters;

    public string? Validate(MatchState state, Side actor, GameCommand command)
    {
        if (command.Payload.Stepwise)
            return ActionStepRules.Validate(state, actor, command);
        if (command.Kind == CommandKind.MoveShadowCharacters && BalrogEventTravel.Active(state, actor))
            return BalrogEventTravel.Valid(state, command.Payload) ? null : "IllegalChoice";
        if (command.Kind == CommandKind.MoveShadowCharacters && NazgulEventTravel.Active(state, actor))
            return command.Payload.DieId == "" && ShadowTravel.Plan(state, command.Payload.CompanionMoves, nazgulOnly: true) is not null
                ? null : "IllegalChoice";
        if (state.Phase != GamePhase.ActionResolution || state.Pending?.Kind != DecisionKind.Action)
            return "IllegalChoice";
        var p = command.Payload;
        var valid = command.Kind switch
        {
            CommandKind.EnterCharacter => CharacterEntry.Plan(state, actor, p) is not null,
            CommandKind.UseCharacterAbility => CharacterAbilities.Plan(state, actor, p) is not null,
            CommandKind.MoveShadowCharacters => actor == Side.Shadow && TravelCost(state, actor, p) &&
                ShadowTravel.Plan(state, p.CompanionMoves) is not null,
            _ => false
        };
        return valid ? null : "IllegalChoice";
    }

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
    {
        if (command.Payload.Stepwise)
        {
            ActionSequences.Apply(state, actor, command);
            return;
        }
        var p = command.Payload;
        if (command.Kind == CommandKind.MoveShadowCharacters && BalrogEventTravel.Active(state, actor))
        {
            BalrogEventTravel.Complete(state, p);
            return;
        }
        if (command.Kind == CommandKind.MoveShadowCharacters && NazgulEventTravel.Active(state, actor))
        {
            NazgulEventTravel.Complete(state, p.CompanionMoves);
            return;
        }
        switch (command.Kind)
        {
            case CommandKind.EnterCharacter:
                var entry = CharacterEntry.Plan(state, actor, p)!;
                DiceRules.Pay(state, actor, p.DieId, entry.Cost);
                if (!string.IsNullOrEmpty(p.CouncilTokenId))
                    CouncilTokens.Spend(state, p.CouncilTokenId);
                CharacterEntry.Apply(state, entry);
                break;
            case CommandKind.UseCharacterAbility:
                var ability = CharacterAbilities.Plan(state, actor, p)!;
                if (ability.Cost.HasValue)
                    DiceRules.Pay(state, actor, p.DieId, ability.Cost.Value);
                else
                    CharacterRules.PayAny(state, actor, p.DieId);
                CharacterAbilities.Apply(state, ability);
                break;
            case CommandKind.MoveShadowCharacters:
                if (p.AbilityId == LordsArmyCosts.UseBalrog)
                    LordsArmyCosts.Pay(state, actor, p.DieId, p.AbilityId);
                else
                    DiceRules.Pay(state, actor, p.DieId, DieFace.Character);
                ShadowTravel.Apply(state, ShadowTravel.Plan(state, p.CompanionMoves)!);
                break;
        }
        if (!StackLimits.OpenIfRequired(state, actor))
            TurnFlow.FinishAction(state, actor);
    }

    internal static bool TravelCost(MatchState state, Side actor, CommandPayload payload) =>
        payload.AbilityId == "" ? CharacterCosts.CanPay(state, actor, payload.DieId, DieFace.Character) :
            LordsArmyCosts.ValidTravel(state, actor, payload);
}
