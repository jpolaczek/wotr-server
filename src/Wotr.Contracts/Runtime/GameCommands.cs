using System;
using System.Collections.Generic;

namespace Wotr.Contracts
{
    public enum GamePhase
    {
        Setup, RecoverDraw, Fellowship, HuntAllocation, ActionRoll, ActionResolution, VictoryCheck, Finished
    }
    public enum DecisionKind
    {
        FellowshipPhase, AllocateHunt, Action, DiscardCards, Reaction, CombatCard, Losses, HuntDamage, CouncilVersions, CorruptionTile, RemoveExcess, EffectChoice,
        FellowshipRoute, Guide, CompanionSeparation, HuntReroll, BattleReroll, BattleContinue, BattleRetreat, BattleAdvance,
        StrongholdDefense, SiegeContinuation, ArmyEvent, CharacterEvent, SpecialDieChoice, KeepActionDie,
        CouncilHomelands, CouncilToken, RulerDiscard, Dismay, ActionSequence
    }
    public enum CommandKind
    {
        ContinueFellowship, AllocateHunt, DiscardCards, DiscardDie, Pass, DrawCard, UseRing, DeclineReaction,
        AdvancePolitics, Recruit, RemoveExcess, MoveArmies, PlayEvent, ChooseEffect, RespondEffect,
        DeclareFellowship, RevealFellowship, HideFellowship, SelectGuide, SeparateCompanions, MoveCompanions,
        MoveFellowship, RerollHunt, ResolveHuntDamage, EnterMordor,
        AttackArmy, CommitCombatCard, RerollBattle, TakeBattleLosses, ContinueBattle, StopBattle, RetreatBattle, HoldBattle, AdvanceBattle,
        FightFieldBattle, RetreatIntoSiege, ExtendSiege, SetCombatCardPolicy,
        EnterCharacter, UseCharacterAbility, MoveShadowCharacters, TakeEventLosses, DiscardProtection,
        FinishArmyEvent, DiscardPalantir, DiscardDenethor, ChooseSpecialDie, UseNarya, UseVilya, DeclineVilya, ChooseCompanionVersions,
        UseEvilKnowledge, ChooseCouncilHomelands, ChooseCouncilToken, UseCouncilToken, DiscardLore, DiscardGates,
        AwakenSovereign, RemoveCorruptedSovereign, AttemptCorruption, DrawCorruptionTile, ChooseCorruptionTile,
        DiscardRulerDie, DiscardWelcome, DiscardBaradPalantir, UseDismay, DeclineDismay, UseLoreBeforeActions, FinishActionSequence
    }
    public enum VictoryCondition { RingCorrupted, RingDestroyed, ShadowMilitary, FreePeoplesMilitary }
    public sealed class GameCommand
    {
        public string MatchId { get; set; } = "";
        public string CommandId { get; set; } = "";
        public long ExpectedRevision { get; set; }
        public string DecisionId { get; set; } = "";
        public CommandKind Kind { get; set; }
        public CommandPayload Payload { get; set; } = new CommandPayload();
    }
    public sealed class CommandPayload
    {
        public bool Stepwise { get; set; }
        public string DieId { get; set; } = "";
        public string RingId { get; set; } = "";
        public string RulerTokenId { get; set; } = "";
        public string CouncilTokenId { get; set; } = "";
        public string Deck { get; set; } = "";
        public DieFace? Face { get; set; }
        public List<string> Ids { get; set; } = new List<string>();
        public string NationId { get; set; } = "";
        public List<UnitPlacementChoice> Placements { get; set; } = new List<UnitPlacementChoice>();
        public DieFace MovementCost { get; set; } = DieFace.Army;
        public List<ArmyMoveChoice> Moves { get; set; } = new List<ArmyMoveChoice>();
        public string CardId { get; set; } = "";
        public DieFace CardCost { get; set; } = DieFace.Event;
        public string ReactionId { get; set; } = "";
        public string GuideId { get; set; } = "";
        public List<string> Route { get; set; } = new List<string>();
        public List<CompanionMoveChoice> CompanionMoves { get; set; } = new List<CompanionMoveChoice>();
        public HuntResponse HuntResponse { get; set; }
        public List<BattleLossChoice> Losses { get; set; } = new List<BattleLossChoice>();
        public string RegionId { get; set; } = "";
        public AutoDecisionChange? AutoDecision { get; set; }
        public string CharacterId { get; set; } = "";
        public string AbilityId { get; set; } = "";
    }
    public sealed class ArmyMoveChoice
    {
        public List<string> Route { get; set; } = new List<string>();
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public List<string> UnitIds { get; set; } = new List<string>();
        public List<string> CharacterIds { get; set; } = new List<string>();
    }
    public sealed class UnitPlacementChoice
    {
        public string UnitId { get; set; } = "";
        public string RegionId { get; set; } = "";
    }
    public sealed class CommandReceipt
    {
        public string CommandId { get; set; } = "";
        public bool Accepted { get; set; }
        public long Revision { get; set; }
        public string Error { get; set; } = "";
        public bool PublicStateChanged { get; set; }
        public AutoDecisionView? AutoDecision { get; set; }
    }
    public sealed class MatchConfiguration
    {
        public bool Lords { get; set; }
        public bool Kings { get; set; }
        public bool Fate { get; set; }
        public bool Council { get; set; }
        public bool? Dismay { get; set; }
        public bool VisibleCorruption { get; set; }
        public bool SequentialCorruption { get; set; }
        public bool RemovingRulerTokens { get; set; }
        public bool NewCities { get; set; }
    }
}
