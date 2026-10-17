using Wotr.Contracts;

namespace Wotr.Domain;

public enum DiscardContinuation
{
    None, RecoverDraw, FinishAction, ResumeEffect
}

public static class TurnFlow
{
    public static Side Other(Side side) => side == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;
    public static int Available(MatchState state, Side side) => state.Dice.Count(d => d.Owner == side && d.Location == DieLocation.Available);

    public static void Start(MatchState state)
    {
        if (state.Phase != GamePhase.Setup || state.Pending is not null)
            throw new InvalidOperationException("A match can start only once.");
        if (state.Configuration.Kings && state.Kings is null)
            KingsSetup.Initialize(state);
        if (state.Configuration.Lords)
            LordsCompanionSetup.Begin(state);
        else
            BeginTurn(state);
        ActionLogRecorder.Setup(state);
    }

    public static void BeginTurn(MatchState state)
    {
        state.Turn++;
        Phase(state, GamePhase.RecoverDraw);
        DiceRules.Recover(state);
        KingsRulerTokens.BeginTurn(state);
        foreach (var player in state.Players)
        {
            player.RingUsedThisTurn = false;
            player.RingActionPending = false;
            CardFlow.Draw(state, player.Side, "character");
            CardFlow.Draw(state, player.Side, "strategy");
        }
        ContinueRecovery(state);
    }

    public static void ContinueRecovery(MatchState state)
    {
        foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
            if (CardFlow.RequireDiscard(state, side, DiscardContinuation.RecoverDraw))
                return;
        state.AfterDiscard = DiscardContinuation.None;
        Phase(state, GamePhase.Fellowship);
        OpenFellowshipPhase(state);
    }

    public static void OpenFellowshipPhase(MatchState state)
    {
        if (!CouncilSetup.BeginHomelands(state))
            state.Open(DecisionKind.FellowshipPhase, Side.FreePeoples,
                TurnDecisionCommands.Fellowship());
    }

    public static void OpenHuntAllocation(MatchState state)
    {
        Phase(state, GamePhase.HuntAllocation);
        state.Open(DecisionKind.AllocateHunt, Side.Shadow, [CommandKind.AllocateHunt]);
    }

    public static void RollAndBeginActions(MatchState state, IRandomSource random)
    {
        Phase(state, GamePhase.ActionRoll);
        DiceRules.Roll(state, random);
        if (!LordsDiceRoll.BeginChoices(state))
            CompleteSpecialDice(state);
    }

    public static void CompleteSpecialDice(MatchState state)
    {
        if (!KingsDice.BeginDiscards(state))
            BeginActions(state);
    }

    public static void BeginActions(MatchState state)
    {
        if (KingsDismay.Begin(state))
            return;
        if (LoreOfTheRings.Begin(state))
            return;
        Phase(state, GamePhase.ActionResolution);
        SelectNext(state, Side.FreePeoples);
    }

    public static void FinishAction(MatchState state, Side actor, bool fellowshipMove = false)
    {
        if (state.Resolution.ActionSequence is not null)
            throw new InvalidOperationException("Complete the paid action sequence before resolving its final effects.");
        SovereignLifecycle.Reconcile(state);
        PoliticalReactions.Complete(state);
        if (KingsPolitics.BeforeActionCompletion(state, actor, fellowshipMove))
            return;
        ActiveEffects.EndAction(state);
        HuntPool.CompleteAction(state);
        if (!VilyaCompletion.Offer(state, actor, fellowshipMove))
            CompleteActionCost(state, actor, fellowshipMove);
    }

    public static void CompleteActionCost(MatchState state, Side actor, bool fellowshipMove)
    {
        DiceRules.CompleteCost(state, actor, fellowshipMove);
        if (state.Kings is not null)
            state.Kings.TokenActionSovereign = "";
        if (state.Lords is not null)
            state.Lords.EvilKnowledgeUsed = false;
        state.Player(actor).RingActionPending = false;
        state.AfterDiscard = DiscardContinuation.None;
        HuntReports.CompleteIfIdle(state);
        SelectNext(state, Other(actor));
    }

    public static void SelectNext(MatchState state, Side preferred)
    {
        var side = CanTakeTurn(state, preferred) ? preferred : Other(preferred);
        if (!CanTakeTurn(state, side))
        {
            if (MordorRules.EndActions(state))
                return;
            ActiveEffects.ExpireBoundary(state, EffectDuration.EndTurn);
            Phase(state, GamePhase.VictoryCheck);
            state.Pending = null;
            if (!MilitaryVictory.Check(state))
                BeginTurn(state);
            return;
        }
        state.Actor = side;
        state.Open(DecisionKind.Action, side, TurnDecisionCommands.Actions(state.Configuration, side));
    }

    private static bool CanTakeTurn(MatchState state, Side side) => Available(state, side) > 0 ||
        side == Side.Shadow && (CorruptionRules.CanAttempt(state) ||
            Available(state, Side.FreePeoples) > 0 && CouncilTokens.CanAct(state));

    public static void Phase(MatchState state, GamePhase phase)
    {
        state.Phase = phase;
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = "PhaseChanged",
            Activity = new() { Automatic = true, Turn = state.Turn, Facts = new() { Phase = phase } },
            Text = $"Turn {state.Turn}: {phase}."
        });
    }
}
