using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class BattleRuntime(EffectCatalog? catalog = null)
{
    private readonly EffectRuntime effects = new(catalog ?? EffectCatalog.All);
    private readonly BattleCards cards = new(catalog ?? EffectCatalog.All);
    private static readonly IReadOnlyDictionary<BattleStage, (int Initiative, BattleStage Next)> EffectWindows =
        new Dictionary<BattleStage, (int, BattleStage)>
        {
            [BattleStage.BeforeRoll] = (3, BattleStage.CombatRoll),
            [BattleStage.BeforeReroll] = (4, BattleStage.AttackerReroll),
            [BattleStage.AfterReroll] = (5, BattleStage.BeforeLosses),
            [BattleStage.BeforeLosses] = (6, BattleStage.AttackerLosses),
            [BattleStage.AfterLosses] = (7, BattleStage.RoundEnd)
        };

    public static BattleState? Current(MatchState state) => state.Resolution.Frames.LastOrDefault()?.Battle;
    public static BattleState? Active(MatchState state) => state.Resolution.Frames.LastOrDefault(f => f.Battle is not null)?.Battle;

    public static void Begin(MatchState state, BattleState battle, EffectReturn continuation = EffectReturn.FinishAction)
    {
        battle.Id = $"{state.Id}:battle:{state.Revision}";
        battle.Return = continuation;
        state.Pending = null;
        state.Resolution.Frames.Add(new() { Battle = battle });
        PoliticalRules.ArmyAttacked(state, BattleRules.Units(state, battle.Defender), PoliticalAttack.ArmyBattle);
        state.Events.Add(new() { Revision = state.Revision, Kind = "BattleStarted", Activity = HistoryFacts.BattleBoundary(state, battle),
            Text = $"{battle.Attacker.Side} attacked {battle.Region} from {battle.Attacker.Region}." });
    }

    public void Continue(MatchState state, IRandomSource random)
    {
        while (state.Pending is null && Current(state) is { } battle)
        {
            if (CouncilHobbitReturn.BeginPending(state))
                return;
            var dead = !BattleRules.Alive(state, battle.Attacker) || !BattleRules.Alive(state, battle.Defender);
            var mustResolveBothLosses = battle.Stage is BattleStage.BeforeReroll or BattleStage.AttackerReroll or BattleStage.DefenderReroll or
                BattleStage.LeaderReroll or BattleStage.AfterReroll or BattleStage.BeforeLosses or BattleStage.AttackerLosses or BattleStage.DefenderLosses;
            if (battle.Stage is not (BattleStage.Finish or BattleStage.Advance or BattleStage.Complete) &&
                (battle.EndedByEffect || dead && !mustResolveBothLosses))
                battle.Stage = BattleStage.Finish;
            Step(state, battle, random);
        }
    }

    private void Step(MatchState state, BattleState battle, IRandomSource random)
    {
        if (KingsBattle.OfferBrand(state, battle))
            return;
        if (EffectWindows.TryGetValue(battle.Stage, out var window))
        {
            RunEffects(state, battle, window.Initiative, window.Next);
            return;
        }
        if (OpenPhaseDecision(state, battle))
            return;
        switch (battle.Stage)
        {
            case BattleStage.BeforeReveal:
                cards.Reveal(state, battle);
                break;
            case BattleStage.CombatRoll:
                BattleRoll.Combat(state, battle, random);
                battle.Stage = BattleStage.BeforeReroll;
                break;
            case BattleStage.AttackerReroll:
                OpenReroll(state, battle, battle.Attacker, BattleStage.DefenderReroll);
                break;
            case BattleStage.DefenderReroll:
                OpenReroll(state, battle, battle.Defender, BattleStage.LeaderReroll);
                break;
            case BattleStage.LeaderReroll:
                BattleRoll.Reroll(state, battle, random);
                battle.Stage = BattleStage.AfterReroll;
                break;
            case BattleStage.AttackerLosses:
                battle.Stage = BattleStage.DefenderLosses;
                NormalLosses(state, battle.Attacker, battle.Defender);
                break;
            case BattleStage.DefenderLosses:
                battle.Stage = BattleStage.AfterLosses;
                NormalLosses(state, battle.Defender, battle.Attacker);
                break;
            case BattleStage.RoundEnd:
                EndRound(state, battle);
                break;
            case BattleStage.Retreat:
                OfferRetreat(state, battle);
                break;
            case BattleStage.Finish:
                Finish(state, battle);
                break;
            case BattleStage.NextRound:
                if (CloseRound(state, battle))
                    NextRound(battle);
                break;
            case BattleStage.Complete:
                Complete(state, battle);
                break;
            default:
                throw new InvalidOperationException("Unknown battle stage.");
        }
    }

    private static void EndRound(MatchState state, BattleState battle)
    {
        if (OrderlyWithdrawal.EndRound(state, battle))
            return;
        if (BattleLanding.MustContinue(battle))
        {
            battle.Stage = BattleStage.Retreat;
            return;
        }
        if (battle.Mode == BattleMode.Siege && battle.Round < battle.MandatorySiegeRounds)
        {
            battle.Stage = BattleStage.NextRound;
            return;
        }
        battle.Stage = battle.Mode != BattleMode.Siege ? BattleStage.Continue :
            StrongholdRules.ExtensionCosts(state, battle).Any() || EvilKnowledge.Available(state, battle) ? BattleStage.SiegeContinuation : BattleStage.Finish;
    }

    private static void OfferRetreat(MatchState state, BattleState battle)
    {
        var retreats = BattleMovement.Retreats(state, battle).Any();
        if (!retreats && !StrongholdRules.CanShelter(state, battle))
        {
            battle.Stage = BattleStage.NextRound;
            return;
        }
        var commands = new List<CommandKind>();
        if (retreats)
            commands.Add(CommandKind.RetreatBattle);
        if (!battle.Defender.Round.OrderlyWithdrawal)
            commands.Add(CommandKind.HoldBattle);
        state.Open(DecisionKind.BattleRetreat, battle.Defender.Side, commands);
        if (StrongholdRules.CanShelter(state, battle))
            state.Pending!.Commands.Add(CommandKind.RetreatIntoSiege);
    }

    private bool OpenPhaseDecision(MatchState state, BattleState battle)
    {
        switch (battle.Stage)
        {
            case BattleStage.AttackerCard:
                if (StrongholdRules.CanShelter(state, battle) && !battle.Defender.Round.FieldDefenseChosen)
                {
                    battle.Stage = BattleStage.StrongholdDefense;
                    state.Open(DecisionKind.StrongholdDefense, battle.Defender.Side, [CommandKind.FightFieldBattle, CommandKind.RetreatIntoSiege]);
                    return true;
                }
                if (!battle.WhiteRiderOffered)
                {
                    battle.WhiteRiderOffered = true;
                    if (effects.BeginPublicWindow(state, Side.FreePeoples, CharacterRules.White, "battle.start"))
                        return true;
                }
                if (BattleCards.PubliclyProhibited(state, battle, battle.Attacker.Side))
                    cards.Commit(state, battle, battle.Attacker.Side, "");
                else
                    state.Open(DecisionKind.CombatCard, battle.Attacker.Side, [CommandKind.CommitCombatCard]);
                return true;
            case BattleStage.DefenderCard:
                if (BattleCards.PubliclyProhibited(state, battle, battle.Defender.Side))
                    cards.Commit(state, battle, battle.Defender.Side, "");
                else
                    state.Open(DecisionKind.CombatCard, battle.Defender.Side, [CommandKind.CommitCombatCard]);
                return true;
            case BattleStage.Continue:
                state.Open(DecisionKind.BattleContinue, battle.Attacker.Side, [CommandKind.ContinueBattle, CommandKind.StopBattle]);
                return true;
            case BattleStage.Advance:
                state.Open(DecisionKind.BattleAdvance, battle.Attacker.Side, [CommandKind.AdvanceBattle]);
                return true;
            case BattleStage.SiegeContinuation:
                state.Open(DecisionKind.SiegeContinuation, battle.Attacker.Side, [CommandKind.ExtendSiege, CommandKind.StopBattle]);
                if (EvilKnowledge.Available(state, battle))
                    state.Pending!.Commands.Add(CommandKind.UseEvilKnowledge);
                return true;
            default:
                return false;
        }
    }

    private static void RunEffects(MatchState state, BattleState battle, int throughInitiative, BattleStage next)
    {
        while (battle.NextWork < battle.Work.Count && battle.Work[battle.NextWork].Initiative <= throughInitiative)
        {
            var work = battle.Work[battle.NextWork++];
            if (battle.Force(work.Owner).Round.CardCancelled)
                continue;
            EffectRuntime.BeginProgram(state, work.Owner, work.CardId, work.ProgramId);
            return;
        }
        battle.Stage = next;
    }

    private static void OpenReroll(MatchState state, BattleState battle, BattleForce force, BattleStage next)
    {
        if (BattleRoll.RerollMaximum(state, force) > 0)
            state.Open(DecisionKind.BattleReroll, force.Side, [CommandKind.RerollBattle]);
        else
            battle.Stage = next;
    }

    private static void NormalLosses(MatchState state, BattleForce target, BattleForce opponent) =>
        BattleLosses.Begin(state, target, Math.Max(0, BattleRoll.HitCount(opponent) - target.Round.CancelHits), "combat");

    public static void NextRound(BattleState battle)
    {
        battle.Round++;
        battle.Attacker.Round = new();
        battle.Defender.Round = new();
        battle.Defender.Round.FieldDefenseChosen = true;
        battle.CardsRevealed = false;
        battle.Work.Clear();
        battle.NextWork = 0;
        battle.SpecialAttacks.Clear();
        battle.Stage = BattleStage.AttackerCard;
    }

    private bool CloseRound(MatchState state, BattleState battle)
    {
        if (battle.Attacker.Round.Closed)
            return true;
        BattleProjection.RememberRound(state, battle);
        BattleCards.Discard(state, battle);
        ActiveEffects.ExpireBoundary(state, EffectDuration.EndCombatRound, battle.RoundScope);
        battle.Attacker.Round.Closed = true;
        battle.Defender.Round.Closed = true;
        if (battle.CardsRevealed)
            battle.CompletedRounds++;
        if (battle.Round == 1 && !battle.SorcererOffered)
        {
            battle.SorcererOffered = true;
            if (effects.BeginPublicWindow(state, Side.Shadow, CharacterRules.WitchKing, "battle.round.closed"))
                return false;
        }
        return true;
    }

    private void Finish(MatchState state, BattleState battle)
    {
        if (!CloseRound(state, battle))
            return;
        ActiveEffects.ExpireBoundary(state, EffectDuration.EndBattle, battle.Id);
        if (battle.ArrivalBeforeBattle)
        {
            BattleLanding.Finish(state, battle);
            return;
        }
        battle.Stage = battle.Mode is BattleMode.Field or BattleMode.Relief && BattleRules.Alive(state, battle.Attacker) &&
            (battle.DefenderRetreated || !BattleRules.Alive(state, battle.Defender)) ? BattleStage.Advance : BattleStage.Complete;
        if (!battle.DefenderSheltered)
            StrongholdRules.ReconcileRegion(state, battle.Region);
        if (battle.Attacker.Region != battle.Region)
            StrongholdRules.ReconcileRegion(state, battle.Attacker.Region);
    }

    private static void Complete(MatchState state, BattleState battle)
    {
        if (CardFlow.RequireDiscard(state, Side.Shadow, DiscardContinuation.ResumeEffect))
            return;
        state.Resolution.Frames.RemoveAt(state.Resolution.Frames.Count - 1);
        state.Events.Add(new() { Revision = state.Revision, Kind = "BattleEnded", Activity = HistoryFacts.BattleBoundary(state, battle),
            Text = $"Battle at {battle.Region} ended after {battle.CompletedRounds} round(s)." });
        if (battle.Return == EffectReturn.FinishAction)
            TurnFlow.FinishAction(state, battle.Attacker.Side);
        else if (battle.Return != EffectReturn.Parent)
            throw new InvalidOperationException("Unsupported battle continuation.");
    }
}
