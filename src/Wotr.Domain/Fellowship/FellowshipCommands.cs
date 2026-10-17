using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class FellowshipCommands : ICommandHandler
{
    public bool Handles(CommandKind kind) => kind is CommandKind.DeclareFellowship or CommandKind.RevealFellowship or
        CommandKind.HideFellowship or CommandKind.SelectGuide or CommandKind.SeparateCompanions or CommandKind.MoveCompanions or CommandKind.EnterMordor;

    public string? Validate(MatchState state, Side actor, GameCommand command)
    {
        if (command.Payload.Stepwise)
            return ActionStepRules.Validate(state, actor, command);
        if (actor != Side.FreePeoples)
            return "IllegalChoice";
        var p = command.Payload;
        if (command.Kind is CommandKind.SeparateCompanions or CommandKind.MoveCompanions && !KingsFarSighted.Valid(state, p))
            return "IllegalChoice";
        var legal = command.Kind switch
        {
            CommandKind.DeclareFellowship => FellowshipFlow.CanDeclare(state) && FellowshipFlow.ValidRoute(state, p.Route, false),
            CommandKind.RevealFellowship => state.Pending?.Kind == DecisionKind.FellowshipRoute &&
                FellowshipFlow.Current(state)?.Fellowship?.Kind == FellowshipProcedureKind.Reveal && FellowshipFlow.ValidRoute(state, p.Route, true),
            CommandKind.SelectGuide => (state.Pending?.Kind == DecisionKind.FellowshipPhase ||
                state.Pending?.Kind == DecisionKind.Guide && FellowshipFlow.Current(state)?.Fellowship?.Kind == FellowshipProcedureKind.Guide) &&
                CompanionRules.Guides(state).Contains(p.GuideId),
            CommandKind.HideFellowship => state.Phase == GamePhase.ActionResolution && state.Pending?.Kind == DecisionKind.Action &&
                CharacterRules.CanHide(state, p.DieId) && !state.Fellowship.Hidden,
            CommandKind.EnterMordor => MordorRules.CanEnter(state),
            CommandKind.SeparateCompanions => CompanionRules.Journey(state)?.AllowSeparation != false && SeparationCost(state, p.DieId) &&
                CompanionRules.Separate(state, p.Ids, p.Route, state.Pending?.Kind == DecisionKind.CompanionSeparation, KingsFarSighted.Bonus(p)) is not null,
            CommandKind.MoveCompanions => (ActionCost(state, p.DieId) || JourneyCost(state)) && CompanionRules.Move(state, p.CompanionMoves, KingsFarSighted.Bonus(p)) is not null,
            _ => false
        };
        return legal ? null : "IllegalChoice";
    }

    private static bool ActionCost(MatchState state, string id) => state.Phase == GamePhase.ActionResolution &&
        state.Pending?.Kind == DecisionKind.Action && DiceRules.AvailableDie(state, Side.FreePeoples, id) is { } die && DiceRules.CanPay(die, DieFace.Character);

    private static bool SeparationCost(MatchState state, string id) => ActionCost(state, id) ||
        state.Pending?.Kind == DecisionKind.CompanionSeparation && FellowshipFlow.Current(state)?.Fellowship?.Kind == FellowshipProcedureKind.Separation;

    private static bool JourneyCost(MatchState state) => state.Pending?.Kind == DecisionKind.CompanionSeparation &&
        FellowshipFlow.Current(state)?.Fellowship is { Kind: FellowshipProcedureKind.Separation, Journey: not null };

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
    {
        if (command.Payload.Stepwise)
        {
            ActionSequences.Apply(state, actor, command);
            return;
        }
        var p = command.Payload;
        switch (command.Kind)
        {
            case CommandKind.DeclareFellowship:
                FellowshipFlow.Declare(state, p.Route);
                break;
            case CommandKind.RevealFellowship:
                FellowshipFlow.Reveal(state, p.Route);
                break;
            case CommandKind.SelectGuide:
                FellowshipFlow.SelectGuide(state, p.GuideId);
                break;
            case CommandKind.HideFellowship:
                if (CharacterRules.GuideDefinition(state) == CharacterRules.Strider)
                    CharacterRules.PayAny(state, actor, p.DieId);
                else
                    DiceRules.Pay(state, actor, p.DieId, DieFace.Character);
                FellowshipFlow.Hide(state);
                TurnFlow.FinishAction(state, actor);
                break;
            case CommandKind.EnterMordor:
                MordorRules.Enter(state);
                break;
            case CommandKind.SeparateCompanions:
                Separate(state, p);
                break;
            case CommandKind.MoveCompanions:
                Move(state, p);
                break;
        }
    }

    private static void Move(MatchState state, CommandPayload payload)
    {
        var journey = JourneyCost(state);
        if (!journey)
            DiceRules.Pay(state, Side.FreePeoples, payload.DieId, DieFace.Character);
        foreach (var plan in CompanionRules.Move(state, payload.CompanionMoves, KingsFarSighted.Bonus(payload))!)
            CompanionRules.Apply(state, plan, false);
        if (journey)
            FellowshipFlow.Complete(state);
        else
            TurnFlow.FinishAction(state, Side.FreePeoples);
    }

    private static void Separate(MatchState state, CommandPayload payload)
    {
        var effect = state.Pending!.Kind == DecisionKind.CompanionSeparation;
        if (!effect)
            DiceRules.Pay(state, Side.FreePeoples, payload.DieId, DieFace.Character);
        CompanionRules.Apply(state, CompanionRules.Separate(state, payload.Ids, payload.Route, effect, KingsFarSighted.Bonus(payload))!, true);
        state.Pending = null;
        if (effect && FellowshipFlow.Current(state)!.Fellowship!.FixedCompanions.Count > 0)
            FellowshipFlow.FinishPlacement(state);
        else
            FellowshipFlow.CompositionChanged(state, effect ? FellowshipReturn.Parent : FellowshipReturn.FinishAction);
    }
}
