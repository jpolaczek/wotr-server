using Wotr.Contracts;

namespace Wotr.Domain;

public static class ActionSequences
{
    public static void Apply(MatchState state, Side actor, GameCommand command)
    {
        var first = state.Resolution.ActionSequence is null;
        var sequence = state.Resolution.ActionSequence ?? Create(state, actor, command);
        var cost = StepCost(state, command);
        ApplyStep(state, actor, command);
        if (first)
            Pay(state, actor, command);
        sequence.CompletedSteps++;
        sequence.Remaining -= cost;
        if (state.HistoryCapture is { } history)
            history.Budget = new() { Completed = sequence.CompletedSteps, Remaining = sequence.Remaining };
        sequence.UsedIds.AddRange(ActionStepRules.FigureIds(command));
        sequence.UsedRegions.AddRange(command.Payload.Placements.Select(p => p.RegionId));
        state.Resolution.ActionSequence = sequence;
        if (sequence.Remaining == 0)
            Finish(state, actor);
        else
        {
            state.Open(DecisionKind.ActionSequence, actor, [command.Kind, CommandKind.FinishActionSequence]);
            state.Pending!.Presentation = "PaidActionSteps";
        }
    }

    private static ActionSequenceState Create(MatchState state, Side actor, GameCommand command)
    {
        var figures = InitialFigures(state, actor, command).ToList();
        var face = command.Kind == CommandKind.MoveArmies ? command.Payload.MovementCost
            : command.Kind == CommandKind.Recruit ? DieFace.Muster : DieFace.Character;
        return new()
        {
            Kind = command.Kind,
            Owner = actor,
            DieId = command.Payload.DieId,
            Face = face,
            AbilityId = command.Payload.AbilityId,
            InitialFigures = figures,
            Remaining = command.Kind switch
            {
                CommandKind.MoveArmies => face == DieFace.Army ? 2 : 1,
                CommandKind.Recruit => 2,
                _ => figures.Count
            }
        };
    }

    private static IEnumerable<string> InitialFigures(MatchState state, Side actor, GameCommand command) => command.Kind switch
    {
        CommandKind.MoveCompanions => state.Characters.Where(c => c.Side == actor && c.Level > 0 && CharacterRules.OnBoard(c)).Select(c => c.Id),
        CommandKind.MoveShadowCharacters => command.Payload.AbilityId == LordsArmyCosts.UseBalrog
            ? command.Payload.CompanionMoves[0].Ids : ShadowTravel.Options(state).Select(t => t.Id),
        _ => []
    };

    private static int StepCost(MatchState state, GameCommand command) => command.Kind switch
    {
        CommandKind.MoveArmies => 1,
        CommandKind.Recruit => ActionStepRules.RecruitmentCost(state, command.Payload.Placements[0]),
        _ => command.Payload.CompanionMoves[0].Ids.Count
    };

    private static void ApplyStep(MatchState state, Side actor, GameCommand command)
    {
        var payload = command.Payload;
        switch (command.Kind)
        {
            case CommandKind.MoveArmies:
                MovementRules.Plan(state, actor, payload)!.Apply(state);
                break;
            case CommandKind.Recruit:
                RecruitmentRules.Place(state, actor, payload.Placements);
                break;
            case CommandKind.MoveCompanions:
                foreach (var plan in CompanionRules.Move(state, payload.CompanionMoves, KingsFarSighted.Bonus(payload))!)
                    CompanionRules.Apply(state, plan, false);
                break;
            case CommandKind.MoveShadowCharacters:
                ShadowTravel.Apply(state, ShadowTravel.Plan(state, payload.CompanionMoves)!);
                break;
        }
    }

    private static void Pay(MatchState state, Side actor, GameCommand command)
    {
        if (command.Kind == CommandKind.MoveArmies)
            CharacterCosts.PayArmy(state, actor, command.Payload);
        else if (command.Kind == CommandKind.MoveShadowCharacters && command.Payload.AbilityId == LordsArmyCosts.UseBalrog)
            LordsArmyCosts.Pay(state, actor, command.Payload.DieId, command.Payload.AbilityId);
        else
            DiceRules.Pay(state, actor, command.Payload.DieId, command.Kind == CommandKind.Recruit ? DieFace.Muster : DieFace.Character);
    }

    public static bool CanFinish(MatchState state, Side actor, CommandPayload payload) =>
        state.Pending?.Kind == DecisionKind.ActionSequence && state.Pending.Owner == actor &&
        state.Resolution.ActionSequence is { CompletedSteps: > 0 } sequence && sequence.Owner == actor &&
        !payload.Stepwise && payload.DieId == "" && payload.AbilityId == "" && payload.RingId == "" &&
        payload.RulerTokenId == "" && payload.CouncilTokenId == "" && payload.Moves.Count == 0 &&
        payload.Placements.Count == 0 && payload.CompanionMoves.Count == 0;

    public static void Finish(MatchState state, Side actor)
    {
        state.Resolution.ActionSequence = null;
        if (!StackLimits.OpenIfRequired(state, actor))
            TurnFlow.FinishAction(state, actor);
    }
}
