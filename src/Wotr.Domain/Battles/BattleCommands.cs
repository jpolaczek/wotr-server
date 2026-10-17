using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class BattleCommands(EffectCatalog? catalog = null) : ICommandHandler, IResolutionContinuation
{
    private readonly BattleCards cards = new(catalog ?? EffectCatalog.All);
    private readonly BattleRuntime runtime = new(catalog);

    public bool Handles(CommandKind kind) => kind is CommandKind.AttackArmy or CommandKind.CommitCombatCard or CommandKind.RerollBattle or
        CommandKind.TakeBattleLosses or CommandKind.ContinueBattle or CommandKind.StopBattle or CommandKind.RetreatBattle or CommandKind.HoldBattle or CommandKind.AdvanceBattle or CommandKind.SetCombatCardPolicy;

    public string? Validate(MatchState state, Side actor, GameCommand command)
    {
        if (command.Kind == CommandKind.SetCombatCardPolicy)
            return AutoDecisions.Validate(state, actor, command.Payload.AutoDecision);
        if (state.Phase != GamePhase.ActionResolution)
            return "IllegalChoice";
        var battle = BattleRuntime.Current(state);
        var payload = command.Payload;
        var legal = command.Kind switch
        {
            CommandKind.AttackArmy => state.Phase == GamePhase.ActionResolution &&
                (state.Pending?.Kind == DecisionKind.Action && BattleRules.PaidAttack(state, actor, payload) is not null ||
                 ArmyEvents.Attack(state, actor, payload) is not null),
            CommandKind.CommitCombatCard => battle is not null && state.Pending?.Kind == DecisionKind.CombatCard && cards.CanCommit(state, battle, actor, payload.CardId),
            CommandKind.RerollBattle => battle is not null && state.Pending?.Kind == DecisionKind.BattleReroll &&
                DraftSelection.IsSubset(payload.Ids, BattleRoll.Failed(battle.Force(actor)), 0, BattleRoll.RerollMaximum(state, battle.Force(actor))),
            CommandKind.TakeBattleLosses => state.Pending?.Kind == DecisionKind.Losses && BattleLosses.Plan(state, payload.Losses) is not null,
            CommandKind.ContinueBattle => battle is { Stage: BattleStage.Continue } && state.Pending?.Kind == DecisionKind.BattleContinue,
            CommandKind.StopBattle => battle is not null && !BattleLanding.MustContinue(battle) &&
                state.Pending?.Kind is DecisionKind.BattleContinue or DecisionKind.SiegeContinuation,
            CommandKind.RetreatBattle => battle is { Stage: BattleStage.Retreat } && state.Pending?.Kind == DecisionKind.BattleRetreat && BattleMovement.Retreats(state, battle).Contains(payload.RegionId),
            CommandKind.HoldBattle => battle is { Stage: BattleStage.Retreat } && state.Pending?.Kind == DecisionKind.BattleRetreat &&
                !battle.Defender.Round.OrderlyWithdrawal,
            CommandKind.AdvanceBattle => battle is { Stage: BattleStage.Advance } && state.Pending?.Kind == DecisionKind.BattleAdvance && BattleMovement.CanAdvance(state, battle, payload.Ids),
            _ => false
        };
        return legal ? null : "IllegalChoice";
    }

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
    {
        var battle = BattleRuntime.Current(state);
        var payload = command.Payload;
        switch (command.Kind)
        {
            case CommandKind.SetCombatCardPolicy:
                AutoDecisions.Set(state, actor, payload.AutoDecision!);
                break;
            case CommandKind.AttackArmy:
                if (ArmyEvents.Attack(state, actor, payload) is { } relief)
                {
                    ArmyEvents.CompleteChoice(state);
                    BattleRuntime.Begin(state, relief, EffectReturn.Parent);
                    break;
                }
                var attacking = BattleRules.PaidAttack(state, actor, payload)!;
                CharacterCosts.PayArmy(state, actor, payload);
                BattleRuntime.Begin(state, attacking);
                break;
            case CommandKind.CommitCombatCard:
                cards.Commit(state, battle!, actor, payload.CardId);
                break;
            case CommandKind.RerollBattle:
                battle!.Force(actor).Round.SelectedRerolls = [.. payload.Ids];
                battle.Stage = actor == battle.Attacker.Side ? BattleStage.DefenderReroll : BattleStage.LeaderReroll;
                state.Pending = null;
                break;
            case CommandKind.TakeBattleLosses:
                BattleLosses.Apply(state, BattleLosses.Plan(state, payload.Losses)!);
                break;
            case CommandKind.ContinueBattle:
                battle!.Stage = BattleStage.Retreat;
                state.Pending = null;
                break;
            case CommandKind.StopBattle:
                battle!.Stage = BattleStage.Finish;
                state.Pending = null;
                break;
            case CommandKind.RetreatBattle:
                BattleMovement.Retreat(state, battle!, payload.RegionId);
                break;
            case CommandKind.HoldBattle:
                battle!.Stage = BattleStage.NextRound;
                state.Pending = null;
                break;
            case CommandKind.AdvanceBattle:
                BattleMovement.Advance(state, battle!, payload.Ids);
                break;
        }
    }

    public bool CanContinue(MatchState state) => BattleRuntime.Current(state) is not null && (state.Pending is null || AutoDecisions.CanAnswer(state));
    public void Continue(MatchState state, IRandomSource random)
    {
        if (AutoDecisions.CanAnswer(state))
        {
            var battle = BattleRuntime.Current(state)!;
            var actor = state.Pending!.Owner;
            AutoDecisions.RecordAnswer(state, battle, actor);
            cards.Commit(state, battle, actor, "");
        }
        runtime.Continue(state, random);
    }
}
