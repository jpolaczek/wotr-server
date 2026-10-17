using System.Collections.Generic;

namespace Wotr.Contracts
{
    public sealed class PlayerView
    {
        public MatchConfiguration Configuration { get; set; } = new MatchConfiguration();
        public List<SovereignView> Sovereigns { get; set; } = new List<SovereignView>();
        public List<RulerTokenView> RulerTokens { get; set; } = new List<RulerTokenView>();
        public List<CombatReplacementView> CombatReplacements { get; set; } = new List<CombatReplacementView>();
        public List<RulerTokenActionOption> RulerTokenActions { get; set; } = new List<RulerTokenActionOption>();
        public List<RegionDefinition> RegionOverrides { get; set; } = new List<RegionDefinition>();
        public CorruptionAttemptView? CorruptionAttempt { get; set; }
        public string MatchId { get; set; } = "";
        public long Revision { get; set; }
        public Side You { get; set; }
        public int Turn { get; set; }
        public GamePhase Phase { get; set; }
        public Side? DecisionOwner { get; set; }
        public Side? Winner { get; set; }
        public VictoryCondition? Victory { get; set; }
        public List<UnitView> Units { get; set; } = new List<UnitView>();
        public List<CharacterView> Characters { get; set; } = new List<CharacterView>();
        public List<NationView> Nations { get; set; } = new List<NationView>();
        public Dictionary<string, Side?> Control { get; set; } = new Dictionary<string, Side?>();
        public List<DieView> Dice { get; set; } = new List<DieView>();
        public List<RingView> Rings { get; set; } = new List<RingView>();
        public List<CouncilTokenView> CouncilTokens { get; set; } = new List<CouncilTokenView>();
        public List<PlayerSummary> Players { get; set; } = new List<PlayerSummary>();
        public List<string> Hand { get; set; } = new List<string>();
        public AutoDecisionView AutoDecision { get; set; } = new AutoDecisionView();
        public List<string> OwnCompanionVersions { get; set; } = new List<string>();
        public List<CharacterView> OwnCompanions { get; set; } = new List<CharacterView>();
        public List<CharacterRosterView> OwnCharacterRoster { get; set; } = new List<CharacterRosterView>();
        public FellowshipView Fellowship { get; set; } = new FellowshipView();
        public HuntView? Hunt { get; set; }
        public HuntReportView? HuntReport { get; set; }
        public HuntPoolView HuntPool { get; set; } = new HuntPoolView();
        public BattleView? Battle { get; set; }
        public EventLossView? EventLoss { get; set; }
        public BattleView? LastBattleRound { get; set; }
        public DecisionView? Decision { get; set; }
        public ActionSequenceView? ActionSequence { get; set; }
        public List<ActionOption> Options { get; set; } = new List<ActionOption>();
        public List<HistoryEntry> History { get; set; } = new List<HistoryEntry>();
        public List<ActiveEffectView> Effects { get; set; } = new List<ActiveEffectView>();
        public List<ActionAvailabilityView> Availability { get; set; } = new List<ActionAvailabilityView>();
        public List<MilitaryVictoryView> MilitaryVictory { get; set; } = new List<MilitaryVictoryView>();
        public List<EffectTrailView> EffectTrail { get; set; } = new List<EffectTrailView>();
    }
    public sealed class UnitView
    {
        public string Id { get; set; } = "";
        public string Nation { get; set; } = "";
        public Side Side { get; set; }
        public UnitKind Kind { get; set; }
        public string Location { get; set; } = "";
        public bool Garrison { get; set; }
    }
    public sealed class NationView
    {
        public string Id { get; set; } = "";
        public bool Active { get; set; }
        public int StepsToWar { get; set; }
    }
    public sealed class CharacterView
    {
        public string Id { get; set; } = "";
        public string DefinitionId { get; set; } = "";
        public Side Side { get; set; }
        public string Location { get; set; } = "";
        public bool Garrison { get; set; }
        public int Level { get; set; }
        public int Leadership { get; set; }
        public string NationIcon { get; set; } = "";
    }
    public sealed class DieView
    {
        public string Id { get; set; } = "";
        public Side Owner { get; set; }
        public string Source { get; set; } = "";
        public DieFace? Face { get; set; }
        public DieLocation Location { get; set; }
        public string HuntOrigin { get; set; } = "";
        public Side? SpentWith { get; set; }
        public bool RemoveSymbol { get; set; }
        public bool SpecialDieChosen { get; set; }
        public bool PermanentlyRemoved { get; set; }
        public string SourceTitle { get; set; } = "";
        public string RecoveryDescription { get; set; } = "";
    }
    public sealed class RingView
    {
        public string Id { get; set; } = "";
        public Side? Owner { get; set; }
    }
    public sealed class PlayerSummary
    {
        public List<string> TableCards { get; set; } = new List<string>();
        public int VictoryPoints { get; set; }
        public Side Side { get; set; }
        public int HandCount { get; set; }
        public int DiscardCount { get; set; }
        public Dictionary<string, int> DeckCounts { get; set; } = new Dictionary<string, int>();
        public bool CombatCardCommitted { get; set; }
        public string? VisibleCombatCard { get; set; }
        public string CombatCardBack { get; set; } = "";
        public int CorruptionTileCount { get; set; }
        public List<int> VisibleCorruptionTiles { get; set; } = new List<int>();
        public bool RingUsedThisTurn { get; set; }
    }
    public sealed class FellowshipView
    {
        public string RegionId { get; set; } = "";
        public int Progress { get; set; }
        public int Corruption { get; set; }
        public bool Hidden { get; set; }
        public string Guide { get; set; } = "";
        public int CompanionCount { get; set; }
        public List<string> CompanionIdentities { get; set; } = new List<string>();
        public Dictionary<string, string> RevealedCompanionVersions { get; set; } = new Dictionary<string, string>();
        public int? MordorStep { get; set; }
        public bool? MordorAttemptedThisTurn { get; set; }
    }
    public sealed class DecisionView
    {
        public string Id { get; set; } = "";
        public DecisionKind Kind { get; set; }
        public int RequiredCount { get; set; }
        public string Presentation { get; set; } = "";
        public string SourceId { get; set; } = "";
        public string Window { get; set; } = "";
        public List<string> Choices { get; set; } = new List<string>();
    }
    public sealed class ActionOption
    {
        public List<EventCostOption> DrawCosts { get; set; } = new List<EventCostOption>();
        public RulerTravelOption? RulerTravel { get; set; }
        public ArmyEventOption? ArmyEvent { get; set; }
        public EventRecruitmentOption? EventRecruitment { get; set; }
        public CommandKind Kind { get; set; }
        public bool Enabled { get; set; } = true;
        public string Reason { get; set; } = "";
        public List<string> EligibleIds { get; set; } = new List<string>();
        public List<string> EligibleDice { get; set; } = new List<string>();
        public List<string> DiceWithoutRing { get; set; } = new List<string>();
        public List<string> Targets { get; set; } = new List<string>();
        public List<DieFace> Faces { get; set; } = new List<DieFace>();
        public int Minimum { get; set; }
        public int Maximum { get; set; }
        public int MaximumGroups { get; set; }
        public bool AllowEmptySelection { get; set; }
        public List<RecruitmentOption> Recruitment { get; set; } = new List<RecruitmentOption>();
        public List<ArmyOption> Armies { get; set; } = new List<ArmyOption>();
        public List<MovementCostOption> MovementCosts { get; set; } = new List<MovementCostOption>();
        public List<EventPlayOption> Events { get; set; } = new List<EventPlayOption>();
        public List<EffectChoiceView> EffectChoices { get; set; } = new List<EffectChoiceView>();
        public List<TravelOption> Travel { get; set; } = new List<TravelOption>();
        public List<CharacterActionOption> CharacterActions { get; set; } = new List<CharacterActionOption>();
        public List<CompanionVersionOption> CompanionVersions { get; set; } = new List<CompanionVersionOption>();
        public List<HuntResponse> HuntResponses { get; set; } = new List<HuntResponse>();
    }
    public sealed class RecruitmentOption
    {
        public string Nation { get; set; } = "";
        public UnitKind Kind { get; set; }
        public List<string> ReserveIds { get; set; } = new List<string>();
        public List<string> Targets { get; set; } = new List<string>();
    }
    public sealed class ActionPreview
    {
        public long Revision { get; set; }
        public bool Legal { get; set; }
        public string Error { get; set; } = "";
        public string Summary { get; set; } = "";
        public List<string> Costs { get; set; } = new List<string>();
        public bool HasUnknownResult { get; set; }
        public List<string> RemovedIds { get; set; } = new List<string>();
        public List<UnitPlacementChoice> Placements { get; set; } = new List<UnitPlacementChoice>();
        public List<ArmyExcess> Excess { get; set; } = new List<ArmyExcess>();
        public Dictionary<string, Side?> ControlChanges { get; set; } = new Dictionary<string, Side?>();
        public FellowshipPreview? Fellowship { get; set; }
        public ArmyLossPreview? Losses { get; set; }
        public List<NationView> PoliticsChanges { get; set; } = new List<NationView>();
    }
    public sealed class HistoryEntry
    {
        public long Revision { get; set; }
        public string Kind { get; set; } = "";
        public string Text { get; set; } = "";
    }
}
